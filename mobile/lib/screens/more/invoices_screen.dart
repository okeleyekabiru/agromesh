import 'package:flutter/material.dart';
import '../../models/billing_models.dart';
import '../../services/api_client.dart';
import '../../services/database_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'billing_common.dart';
import 'common.dart';
import 'invoice_form_screen.dart';
import 'ops_common.dart';

/// Sales invoices the farm has issued: list, create, edit, delete and download the PDF.
/// The list is kept on the phone, so it still shows offline (creating, editing and PDFs need a connection).
class InvoicesScreen extends StatefulWidget {
  const InvoicesScreen({super.key});

  @override
  State<InvoicesScreen> createState() => _InvoicesScreenState();
}

class _InvoicesScreenState extends State<InvoicesScreen> {
  List<InvoiceSummary>? _items;
  bool _offline = false;
  bool _loading = true;
  String? _error;
  late final String _farm;
  late final bool _canEdit;

  @override
  void initState() {
    super.initState();
    _farm = farmIdFrom(context);
    _canEdit = isManager(context);
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = _items == null; _error = null; });
    try {
      final json = await ApiClient.get('/api/farms/$_farm/invoices');
      final rows = asList(json);
      await DatabaseService.saveCache('invoices:$_farm', rows);
      _items = rows.map(InvoiceSummary.fromJson).toList();
      _offline = false;
    } catch (e) {
      final cached = !shouldUseOfflineCache(e) ? <Map<String, dynamic>>[] : await DatabaseService.readCacheList('invoices:$_farm');
      if (cached.isNotEmpty) {
        _items = cached.map(InvoiceSummary.fromJson).toList();
        _offline = true;
      } else if (_items == null) {
        _error = failText(e);
      }
    }
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _create() async {
    final saved = await Navigator.push<Invoice>(context, MaterialPageRoute(builder: (_) => InvoiceFormScreen(farmId: _farm)));
    if (saved != null && mounted) {
      toast(context, '${saved.reference} created.');
      await _load();
      if (mounted) _open(InvoiceSummary(id: saved.id, reference: saved.reference, billToName: saved.billToName, total: saved.total));
    }
  }

  Future<void> _open(InvoiceSummary s) async {
    final changed = await Navigator.push<bool>(context, MaterialPageRoute(builder: (_) => InvoiceDetailScreen(farmId: _farm, summary: s, canEdit: _canEdit)));
    if (changed == true) await _load();
  }

  @override
  Widget build(BuildContext context) {
    final items = _items;
    Widget body;
    if (_loading) {
      body = const Center(child: CircularProgressIndicator(color: IlevoraColors.palm));
    } else if (items == null) {
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
    } else if (items.isEmpty) {
      body = ListView(children: [emptyNote(_canEdit ? 'No invoices yet. Tap New invoice to create one.' : 'No invoices yet.')]);
    } else {
      body = ListView(padding: const EdgeInsets.all(16), children: [
        if (_offline) offlineNote('Offline: showing the saved list. Connect to refresh it.'),
        AgroCard(
          child: Column(children: [
            for (final s in items)
              InkWell(
                onTap: () => _open(s),
                child: Container(
                  constraints: const BoxConstraints(minHeight: 56),
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: IlevoraColors.fog))),
                  child: Row(children: [
                    Expanded(flex: 3,
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text('${s.reference} · ${s.billToName}',
                            maxLines: 1, overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13.5, fontWeight: FontWeight.w600, color: IlevoraColors.ink)),
                        const SizedBox(height: 2),
                        Text('${dateText(s.date == null ? null : dateOnly(s.date!))} · due ${dateText(s.dueDate == null ? null : dateOnly(s.dueDate!))}',
                            style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: IlevoraColors.stone)),
                      ]),
                    ),
                    const SizedBox(width: 8),
                    amountCell(nairaExact(s.total), const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w600, color: IlevoraColors.forest)),
                    const Icon(Icons.chevron_right, color: IlevoraColors.stone, size: 18),
                  ]),
                ),
              ),
          ]),
        ),
        const SizedBox(height: 90),
      ]);
    }

    return Scaffold(
      backgroundColor: IlevoraColors.mist,
      appBar: AppBar(
        backgroundColor: IlevoraColors.forest,
        foregroundColor: IlevoraColors.white,
        title: const Text('Invoices', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      floatingActionButton: _canEdit && items != null
          ? FloatingActionButton.extended(
              backgroundColor: IlevoraColors.leaf,
              foregroundColor: Colors.white,
              onPressed: _offline ? null : _create,
              icon: const Icon(Icons.add),
              label: const Text('New invoice'),
            )
          : null,
      body: RefreshIndicator(color: IlevoraColors.palm, onRefresh: _load, child: body),
    );
  }
}

/// One invoice in full, with Download PDF, Edit and Delete. Pops `true` when something changed.
class InvoiceDetailScreen extends StatefulWidget {
  final String farmId;
  final InvoiceSummary summary;
  final bool canEdit;
  const InvoiceDetailScreen({super.key, required this.farmId, required this.summary, required this.canEdit});

  @override
  State<InvoiceDetailScreen> createState() => _InvoiceDetailScreenState();
}

class _InvoiceDetailScreenState extends State<InvoiceDetailScreen> {
  Invoice? _invoice;
  bool _loading = true;
  bool _pdfBusy = false;
  bool _busy = false;
  bool _changed = false;
  String? _error;
  String? _actionError;

  String get _base => '/api/farms/${widget.farmId}/invoices/${widget.summary.id}';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final j = await ApiClient.get(_base);
      _invoice = Invoice.fromJson((j as Map).cast<String, dynamic>());
    } catch (e) {
      _error = failText(e);
    }
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _pdf() async {
    setState(() { _pdfBusy = true; _actionError = null; });
    try {
      final bytes = await ApiClient.getBytes('$_base/pdf');
      final ref = (_invoice?.reference.isNotEmpty ?? false) ? _invoice!.reference : widget.summary.reference;
      final err = await saveAndOpen(bytes, '${ref.isEmpty ? 'invoice' : ref}.pdf');
      if (err != null && mounted) setState(() => _actionError = err);
    } catch (e) {
      if (mounted) setState(() => _actionError = failText(e));
    }
    if (mounted) setState(() => _pdfBusy = false);
  }

  Future<void> _edit() async {
    final inv = _invoice;
    if (inv == null) return;
    final saved = await Navigator.push<Invoice>(context, MaterialPageRoute(builder: (_) => InvoiceFormScreen(farmId: widget.farmId, existing: inv)));
    if (saved != null && mounted) {
      setState(() { _invoice = saved; _changed = true; });
      toast(context, 'Invoice saved.');
    }
  }

  Future<void> _delete() async {
    final ok = await confirmDialog(context, 'Delete ${widget.summary.reference}?', 'This removes the invoice for good. Its number is not reused.', 'Delete');
    if (!ok || !mounted) return;
    setState(() { _busy = true; _actionError = null; });
    try {
      await ApiClient.delete(_base);
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _actionError = failText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final inv = _invoice;
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.pop(context, _changed);
      },
      child: Scaffold(
        backgroundColor: IlevoraColors.mist,
        appBar: AppBar(
          backgroundColor: IlevoraColors.forest,
          foregroundColor: IlevoraColors.white,
          title: Text(widget.summary.reference.isEmpty ? 'Invoice' : widget.summary.reference, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator(color: IlevoraColors.palm))
            : inv == null
                ? ListView(children: [
                    Padding(
                      padding: const EdgeInsets.all(32),
                      child: Column(children: [
                        const Icon(Icons.cloud_off_outlined, color: IlevoraColors.stone, size: 36),
                        const SizedBox(height: 10),
                        Text(_error ?? 'Could not load the invoice.', textAlign: TextAlign.center, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, color: IlevoraColors.stone)),
                        const SizedBox(height: 12),
                        OutlinedButton(onPressed: _load, child: const Text('Retry')),
                      ]),
                    ),
                  ])
                : ListView(padding: const EdgeInsets.all(16), children: [
                    AgroCard(
                      padding: const EdgeInsets.all(16),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        sectionLabel('Billed to'),
                        Text(inv.billToName, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 15, fontWeight: FontWeight.w700, color: IlevoraColors.ink)),
                        for (final line in [inv.billToPhone, inv.billToEmail, inv.billToAddress])
                          if (line != null) Text(line, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, color: IlevoraColors.stone)),
                        const SizedBox(height: 8),
                        KvRow('Invoice date', dateText(inv.date == null ? null : dateOnly(inv.date!))),
                        KvRow('Due date', dateText(inv.dueDate == null ? null : dateOnly(inv.dueDate!))),
                        KvRow('Template', inv.templateKey.toUpperCase()),
                      ]),
                    ),
                    const SizedBox(height: 14),
                    AgroCard(
                      padding: const EdgeInsets.all(16),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        sectionLabel('Items'),
                        for (var i = 0; i < inv.lineItems.length; i++)
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 4),
                            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              Expanded(flex: 3,
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                  Text('${i + 1}. ${inv.lineItems[i].description}', style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, color: IlevoraColors.ink)),
                                  Text('${nairaExact(inv.lineItems[i].rate)} × ${qtyText(inv.lineItems[i].quantity)}', style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: IlevoraColors.stone)),
                                ]),
                              ),
                              const SizedBox(width: 8),
                              amountCell(nairaExact(inv.lineItems[i].amount), const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w600, color: IlevoraColors.ink)),
                            ]),
                          ),
                        InvoiceTotalsCard(totals: InvoiceTotals(inv.subtotal, inv.vatAmount, inv.total), vatPercent: inv.vatPercent),
                      ]),
                    ),
                    if (inv.paymentAccountName != null || inv.paymentAccountNumber != null || inv.paymentBankName != null || inv.termsAndNotes != null || inv.signatoryName != null) ...[
                      const SizedBox(height: 14),
                      AgroCard(
                        padding: const EdgeInsets.all(16),
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          if (inv.paymentAccountName != null || inv.paymentAccountNumber != null || inv.paymentBankName != null) ...[
                            sectionLabel('Payment details'),
                            if (inv.paymentAccountName != null) KvRow('Account name', inv.paymentAccountName!),
                            if (inv.paymentAccountNumber != null) KvRow('Account number', inv.paymentAccountNumber!),
                            if (inv.paymentBankName != null) KvRow('Bank', inv.paymentBankName!),
                          ],
                          if (inv.termsAndNotes != null) ...[
                            sectionLabel('Terms & notes'),
                            Text(inv.termsAndNotes!, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, color: IlevoraColors.ink)),
                          ],
                          if (inv.signatoryName != null) KvRow('Signatory', inv.signatoryName!),
                        ]),
                      ),
                    ],
                    const SizedBox(height: 14),
                    errorLine(_actionError),
                    primaryButton(_pdfBusy ? 'Preparing PDF…' : 'Download PDF', _pdf, busy: _pdfBusy),
                    if (widget.canEdit) ...[
                      const SizedBox(height: 8),
                      Row(children: [
                        Expanded(
                          child: OutlinedButton.icon(
                            style: OutlinedButton.styleFrom(minimumSize: const Size(44, 48)),
                            onPressed: _busy ? null : _edit,
                            icon: const Icon(Icons.edit_outlined, size: 18),
                            label: const Text('Edit'),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: OutlinedButton.icon(
                            style: OutlinedButton.styleFrom(minimumSize: const Size(44, 48), foregroundColor: IlevoraColors.alert, side: const BorderSide(color: IlevoraColors.alert)),
                            onPressed: _busy ? null : _delete,
                            icon: const Icon(Icons.delete_outline, size: 18),
                            label: const Text('Delete'),
                          ),
                        ),
                      ]),
                    ],
                    const SizedBox(height: 24),
                  ]),
      ),
    );
  }
}

String qtyText(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();
