import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/billing_models.dart';
import '../../services/api_client.dart';
import '../../services/database_service.dart';
import '../../services/upload_rules.dart';
import '../../stores/auth_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'billing_common.dart';
import 'common.dart';
import 'ops_common.dart';
import 'vendor_invoice_upload_screen.dart';

enum VendorFilter { all, unpaid, paid }

/// Applies the Paid / Unpaid filter to a list of bills (kept apart from the widget so it can be tested).
List<VendorInvoice> filterVendorInvoices(List<VendorInvoice> all, VendorFilter f) => switch (f) {
      VendorFilter.all => all,
      VendorFilter.unpaid => all.where((v) => !v.isPaid).toList(),
      VendorFilter.paid => all.where((v) => v.isPaid).toList(),
    };

/// The hint shown when a bill's amount differs from the order total, or null when they match.
String? mismatchHint(VendorInvoice v) {
  if (!v.amountMismatch) return null;
  final diff = v.amount - v.orderTotal;
  return 'Order total is ${nairaExact(v.orderTotal)} but this bill is ${nairaExact(v.amount)} '
      '(${diff > 0 ? '${nairaExact(diff)} more' : '${nairaExact(-diff)} less'}). Check it against the order.';
}

/// Bills received from suppliers. Farm-wide from More > Vendor invoices; for one order when [orderId] is given
/// (from a received order), where [canUpload] adds the Upload button. Lists are cached for offline viewing.
class VendorInvoicesScreen extends StatefulWidget {
  final String? orderId;
  final String? orderLabel;
  final double orderTotal;
  final String supplierName;
  final bool canUpload;
  const VendorInvoicesScreen({
    super.key,
    this.orderId,
    this.orderLabel,
    this.orderTotal = 0,
    this.supplierName = '',
    this.canUpload = false,
  });

  @override
  State<VendorInvoicesScreen> createState() => _VendorInvoicesScreenState();
}

class _VendorInvoicesScreenState extends State<VendorInvoicesScreen> {
  List<VendorInvoice>? _all;
  VendorFilter _filter = VendorFilter.all;
  bool _offline = false;
  bool _loading = true;
  String? _error;
  late final String _farm;
  late final bool _manager;

  String get _cacheKey => 'vendorinv:$_farm:${widget.orderId ?? 'all'}';

  @override
  void initState() {
    super.initState();
    _farm = farmIdFrom(context);
    _manager = isManager(context);
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = _all == null; _error = null; });
    try {
      final q = widget.orderId == null ? '' : '?orderId=${widget.orderId}';
      final rows = asList(await ApiClient.get('/api/farms/$_farm/vendor-invoices$q'));
      await DatabaseService.saveCache(_cacheKey, rows);
      _all = rows.map(VendorInvoice.fromJson).toList();
      _offline = false;
    } catch (e) {
      final cached = !shouldUseOfflineCache(e) ? <Map<String, dynamic>>[] : await DatabaseService.readCacheList(_cacheKey);
      if (cached.isNotEmpty) {
        _all = cached.map(VendorInvoice.fromJson).toList();
        _offline = true;
      } else if (_all == null) {
        _error = failText(e);
      }
    }
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _upload() async {
    final saved = await Navigator.push<VendorInvoice>(
      context,
      MaterialPageRoute(
        builder: (_) => VendorInvoiceUploadScreen(
          farmId: _farm,
          orderId: widget.orderId!,
          orderLabel: widget.orderLabel ?? '',
          orderTotal: widget.orderTotal,
          supplierName: widget.supplierName,
        ),
      ),
    );
    if (saved != null && mounted) {
      toast(context, 'Invoice ${saved.invoiceNumber} uploaded.');
      await _load();
    }
  }

  Future<void> _openDetail(VendorInvoice v) async {
    final msg = await showSheet<String>(context, _BillDetail(farm: _farm, bill: v, canManage: _manager));
    if (msg != null && mounted) {
      toast(context, msg);
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final all = _all;
    final title = widget.orderId == null ? 'Vendor invoices' : 'Invoices for ${widget.orderLabel ?? 'order'}';
    Widget body;
    if (_loading) {
      body = const Center(child: CircularProgressIndicator(color: IlevoraColors.palm));
    } else if (all == null) {
      body = ListView(children: [
        Padding(
          padding: const EdgeInsets.all(32),
          child: Column(children: [
            const Icon(Icons.cloud_off_outlined, color: IlevoraColors.stone, size: 36),
            const SizedBox(height: 10),
            Text(_error ?? 'Nothing to show.', textAlign: TextAlign.center, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, color: IlevoraColors.stone)),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Retry')),
          ]),
        ),
      ]);
    } else {
      final shown = filterVendorInvoices(all, _filter);
      body = ListView(padding: const EdgeInsets.all(16), children: [
        if (_offline) offlineNote('Offline: showing the saved list. Connect to refresh it.'),
        Wrap(spacing: 8, children: [
          for (final f in VendorFilter.values)
            ChoiceChip(
              label: Text(switch (f) { VendorFilter.all => 'All', VendorFilter.unpaid => 'Unpaid', VendorFilter.paid => 'Paid' }),
              selected: _filter == f,
              onSelected: (_) => setState(() => _filter = f),
              materialTapTargetSize: MaterialTapTargetSize.padded,
              selectedColor: IlevoraColors.palm.withValues(alpha: 0.2),
              labelStyle: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, color: IlevoraColors.ink),
            ),
        ]),
        const SizedBox(height: 10),
        if (shown.isEmpty)
          emptyNote(all.isEmpty
              ? (widget.orderId != null
                  ? (widget.canUpload ? 'No vendor invoices for this order yet. Tap Upload invoice to add one.' : 'No vendor invoices for this order yet.')
                  : 'No vendor invoices yet. Open a received order and upload the supplier\'s invoice.')
              : 'No ${_filter == VendorFilter.paid ? 'paid' : 'unpaid'} invoices.')
        else
          AgroCard(
            child: Column(children: [for (final v in shown) VendorBillTile(bill: v, onTap: () => _openDetail(v))]),
          ),
        const SizedBox(height: 90),
      ]);
    }

    return Scaffold(
      backgroundColor: IlevoraColors.mist,
      appBar: AppBar(
        backgroundColor: IlevoraColors.forest,
        foregroundColor: IlevoraColors.white,
        title: Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      floatingActionButton: widget.orderId != null && widget.canUpload && _canUploadRole(context) && all != null
          ? FloatingActionButton.extended(
              backgroundColor: IlevoraColors.leaf,
              foregroundColor: Colors.white,
              onPressed: _offline ? null : _upload,
              icon: const Icon(Icons.upload_file),
              label: const Text('Upload invoice'),
            )
          : null,
      body: RefreshIndicator(color: IlevoraColors.palm, onRefresh: _load, child: body),
    );
  }
}

/// Admin, FarmManager and Supervisor may upload; Member is read-only.
bool _canUploadRole(BuildContext c) => c.read<AuthStore>().user?.canUploadVendorBill ?? false;

/// One bill in a list: vendor, numbers, amount, Paid / Unpaid / Overdue badges and the mismatch hint.
class VendorBillTile extends StatelessWidget {
  final VendorInvoice bill;
  final VoidCallback? onTap;
  const VendorBillTile({super.key, required this.bill, this.onTap});

  @override
  Widget build(BuildContext context) {
    final hint = mismatchHint(bill);
    return InkWell(
      onTap: onTap,
      child: Container(
        constraints: const BoxConstraints(minHeight: 56),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: IlevoraColors.fog))),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Expanded(flex: 3,
              child: Text(bill.vendorName, maxLines: 1, overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13.5, fontWeight: FontWeight.w600, color: IlevoraColors.ink)),
            ),
            const SizedBox(width: 8),
            amountCell(nairaExact(bill.amount), const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w600, color: IlevoraColors.forest)),
          ]),
          const SizedBox(height: 2),
          Text(
            '#${bill.invoiceNumber}${bill.orderReference.isEmpty ? '' : ' · order ${bill.orderReference}'}',
            style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: IlevoraColors.stone),
          ),
          Text(
            'Dated ${dateText(bill.invoiceDate == null ? null : dateOnly(bill.invoiceDate!))}'
            '${bill.dueDate == null ? '' : ' · due ${dateText(dateOnly(bill.dueDate!))}'}',
            style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: IlevoraColors.stone),
          ),
          const SizedBox(height: 6),
          Wrap(spacing: 6, runSpacing: 4, children: [
            AgroBadge(label: bill.isPaid ? 'Paid' : 'Unpaid', variant: bill.isPaid ? AgroBadgeVariant.green : AgroBadgeVariant.harvest),
            if (bill.isOverdue) const AgroBadge(label: 'Overdue', variant: AgroBadgeVariant.red),
            if (hint != null) const AgroBadge(label: 'Amount differs', variant: AgroBadgeVariant.blue),
          ]),
        ]),
      ),
    );
  }
}

/// Bottom sheet for one bill: details, view / download the file, mark paid / unpaid and delete (managers).
/// Closes with a message when something changed, so the list reloads.
class _BillDetail extends StatefulWidget {
  final String farm;
  final VendorInvoice bill;
  final bool canManage;
  const _BillDetail({required this.farm, required this.bill, required this.canManage});

  @override
  State<_BillDetail> createState() => _BillDetailState();
}

class _BillDetailState extends State<_BillDetail> {
  bool _fileBusy = false;
  bool _busy = false;
  String? _error;

  String get _base => '/api/farms/${widget.farm}/vendor-invoices/${widget.bill.id}';

  Future<void> _view() async {
    setState(() { _fileBusy = true; _error = null; });
    try {
      final bytes = await ApiClient.getBytes('$_base/file?inline=true');
      final b = widget.bill;
      final name = b.fileName.isNotEmpty ? b.fileName : 'vendor-invoice-${b.invoiceNumber}.${b.isPdf ? 'pdf' : 'jpg'}';
      final err = await saveAndOpen(bytes, name);
      if (err != null && mounted) setState(() => _error = err);
    } catch (e) {
      if (mounted) setState(() => _error = failText(e));
    }
    if (mounted) setState(() => _fileBusy = false);
  }

  Future<void> _setPaid(bool paid) async {
    setState(() { _busy = true; _error = null; });
    try {
      await ApiClient.patch('$_base/status', {
        'status': paid ? 'Paid' : 'Unpaid',
        if (paid) 'paidAt': DateTime.now().toUtc().toIso8601String(),
      });
      if (mounted) Navigator.pop(context, paid ? 'Marked as paid.' : 'Marked as unpaid.');
    } catch (e) {
      if (mounted) setState(() { _error = failText(e); _busy = false; });
    }
  }

  Future<void> _delete() async {
    final ok = await confirmDialog(context, 'Delete this invoice?', 'The uploaded file and its details are removed. To correct a mistake, upload it again.', 'Delete');
    if (!ok || !mounted) return;
    setState(() { _busy = true; _error = null; });
    try {
      await ApiClient.delete(_base);
      if (mounted) Navigator.pop(context, 'Invoice deleted.');
    } catch (e) {
      if (mounted) setState(() { _error = failText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final b = widget.bill;
    final hint = mismatchHint(b);
    return Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
      sheetTitle(b.vendorName, 'Invoice #${b.invoiceNumber}'),
      Wrap(spacing: 6, runSpacing: 4, children: [
        AgroBadge(label: b.isPaid ? 'Paid' : 'Unpaid', variant: b.isPaid ? AgroBadgeVariant.green : AgroBadgeVariant.harvest),
        if (b.isOverdue) const AgroBadge(label: 'Overdue', variant: AgroBadgeVariant.red),
      ]),
      const SizedBox(height: 8),
      KvRow('Amount', nairaExact(b.amount)),
      if (b.orderReference.isNotEmpty) KvRow('Order', '${b.orderReference} (${nairaExact(b.orderTotal)})'),
      KvRow('Invoice date', dateText(b.invoiceDate == null ? null : dateOnly(b.invoiceDate!))),
      if (b.dueDate != null) KvRow('Due date', dateText(dateOnly(b.dueDate!))),
      if (b.isPaid && b.paidAt != null) KvRow('Paid on', dateText(b.paidAt!.toIso8601String())),
      if (b.fileName.isNotEmpty) KvRow('File', '${b.fileName} · ${formatBytes(b.fileSize)}'),
      if (b.notes != null) KvRow('Notes', b.notes!),
      if (hint != null)
        Container(
          margin: const EdgeInsets.only(top: 8),
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(color: const Color(0x193B82F6), borderRadius: BorderRadius.circular(8)),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Icon(Icons.info_outline, size: 16, color: Color(0xFF2563EB)),
            const SizedBox(width: 8),
            Expanded(child: Text(hint, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: IlevoraColors.ink))),
          ]),
        ),
      const SizedBox(height: 12),
      if (_error != null) errorLine(_error),
      OutlinedButton.icon(
        style: OutlinedButton.styleFrom(minimumSize: const Size(44, 48)),
        onPressed: _fileBusy ? null : _view,
        icon: Icon(b.isPdf ? Icons.picture_as_pdf_outlined : Icons.image_outlined, size: 18),
        label: Text(_fileBusy ? 'Opening…' : 'View / download file'),
      ),
      if (widget.canManage) ...[
        const SizedBox(height: 8),
        primaryButton(b.isPaid ? 'Mark as unpaid' : 'Mark as paid', () => _setPaid(!b.isPaid), busy: _busy),
        const SizedBox(height: 4),
        Center(
          child: TextButton.icon(
            style: TextButton.styleFrom(foregroundColor: IlevoraColors.alert, minimumSize: const Size(44, 48)),
            onPressed: _busy ? null : _delete,
            icon: const Icon(Icons.delete_outline, size: 18),
            label: const Text('Delete invoice'),
          ),
        ),
      ],
    ]);
  }
}
