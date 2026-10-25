import 'package:flutter/material.dart';
import 'dart:io';
import 'package:google_fonts/google_fonts.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';
import 'ops_common.dart';

/// Purchase orders: view, place, receive, and create new ones.
class OrdersScreen extends StatelessWidget {
  const OrdersScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final farm = farmIdFrom(context);
    return LoadScaffold<List<Map<String, dynamic>>>(
      title: 'Orders',
      load: () async => asList(await ApiClient.get('/api/farms/$farm/orders')),
      builder: (context, orders, reload) => orders.isEmpty
          ? ListView(children: [emptyNote('No orders yet. Tap New order to create one.')])
          : ListView(padding: const EdgeInsets.all(16), children: [
              AgroCard(
                child: Column(children: [
                  for (final o in orders)
                    InkWell(
                      onTap: () async {
                        final msg = await showSheet<String>(context, _OrderDetail(farm: farm, order: o));
                        if (msg != null && context.mounted) {
                          toast(context, msg);
                          await reload();
                        }
                      },
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                        decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
                        child: Row(children: [
                          Expanded(
                            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                              Text('${o['reference'] ?? ''}'.isEmpty ? '${o['supplierName']}' : '${o['reference']} · ${o['supplierName']}',
                                  style: GoogleFonts.inter(fontSize: 13.5, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                              const SizedBox(height: 2),
                              Text(dateText(o['createdAt']), style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
                            ]),
                          ),
                          Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                            Text(naira(dbl(o['total'])), style: GoogleFonts.jetBrainsMono(fontSize: 12, color: AgroColors.forest)),
                            Row(mainAxisSize: MainAxisSize.min, children: [
                              statusBadge('${o['status']}'),
                              if (o['isPaid'] == true) ...[const SizedBox(width: 4), const AgroBadge(label: 'Paid', variant: AgroBadgeVariant.green)],
                            ]),
                          ]),
                        ]),
                      ),
                    ),
                ]),
              ),
              const SizedBox(height: 90),
            ]),
      fab: Builder(
        builder: (ctx) => FloatingActionButton.extended(
          backgroundColor: AgroColors.leaf,
          foregroundColor: Colors.white,
          onPressed: () async {
            final ok = await showSheet<bool>(ctx, _NewOrder(farm: farm));
            if (ok == true && ctx.mounted) {
              Navigator.pushReplacement(ctx, MaterialPageRoute(builder: (_) => const OrdersScreen()));
            }
          },
          icon: const Icon(Icons.add),
          label: const Text('New order'),
        ),
      ),
    );
  }
}

class _OrderDetail extends StatefulWidget {
  final String farm;
  final Map<String, dynamic> order;
  const _OrderDetail({required this.farm, required this.order});
  @override
  State<_OrderDetail> createState() => _OrderDetailState();
}

class _OrderDetailState extends State<_OrderDetail> {
  bool _busy = false;
  bool _invoicing = false;
  String? _error;

  Future<void> _invoice() async {
    setState(() { _invoicing = true; _error = null; });
    try {
      final id = '${widget.order['id']}';
      final bytes = await ApiClient.getBytes('/api/farms/${widget.farm}/orders/$id/invoice');
      final dir = await getTemporaryDirectory();
      final file = File('${dir.path}/invoice-$id.pdf');
      await file.writeAsBytes(bytes, flush: true);
      final r = await OpenFilex.open(file.path);
      if (r.type != ResultType.done && mounted) {
        setState(() => _error = 'Saved the invoice, but no app could open a PDF. Install a PDF viewer and try again.');
      }
    } catch (e) {
      if (mounted) setState(() => _error = errText(e));
    }
    if (mounted) setState(() => _invoicing = false);
  }

  Future<void> _move(String action, String done) async {
    setState(() { _busy = true; _error = null; });
    try {
      await ApiClient.patch('/api/farms/${widget.farm}/orders/${widget.order['id']}/$action');
      if (mounted) Navigator.pop(context, done);
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final o = widget.order;
    final status = '${o['status']}';
    final lines = asList(o['lineItems']);
    return Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
      sheetTitle('${o['supplierName']}', '${o['reference'] ?? ''} · ${dateText(o['createdAt'])}'),
      Row(children: [statusBadge(status), if (o['isPaid'] == true) ...[const SizedBox(width: 6), const AgroBadge(label: 'Paid', variant: AgroBadgeVariant.green)]]),
      const SizedBox(height: 10),
      for (final l in lines) KvRow('${l['inventoryItemName']}  ${qty(l['quantity'])} ${l['unit']} × ${naira(dbl(l['unitPrice']))}', naira(dbl(l['lineTotal']))),
      const Divider(),
      KvRow('Total', naira(dbl(o['total']))),
      if (status == 'Placed' && o['isPaid'] != true && (o['reference'] ?? '') != '')
        Padding(
          padding: const EdgeInsets.only(top: 6),
          child: Text('Pay by bank transfer and put ${o['reference']} in the narration. It is marked paid when the bank sync matches it.',
              style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
        ),
      if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
      const SizedBox(height: 12),
      OutlinedButton.icon(
        onPressed: _invoicing ? null : _invoice,
        icon: const Icon(Icons.picture_as_pdf_outlined, size: 16),
        label: Text(_invoicing ? 'Preparing…' : 'Download invoice'),
      ),
      const SizedBox(height: 8),
      if (status == 'Draft') primaryButton('Place order', () => _move('place', 'Order placed.'), busy: _busy),
      if (status == 'Placed') primaryButton('Mark received (adds to stock)', () => _move('receive', 'Order received. Stock updated.'), busy: _busy),
    ]);
  }
}

/// Opens the new-order form, optionally with one item already on the first line. Returns true if an order was created.
Future<bool?> showNewOrderSheet(BuildContext context, {String? itemId}) =>
    showSheet<bool>(context, _NewOrder(farm: farmIdFrom(context), initialItemId: itemId));

class _NewOrder extends StatefulWidget {
  final String farm;
  final String? initialItemId;
  const _NewOrder({required this.farm, this.initialItemId});
  @override
  State<_NewOrder> createState() => _NewOrderState();
}

class _Line {
  String? itemId;
  final qty = TextEditingController();
  final price = TextEditingController();
}

class _NewOrderState extends State<_NewOrder> {
  List<Map<String, dynamic>> _suppliers = [];
  List<Map<String, dynamic>> _items = [];
  String? _supplier;
  final _newSupplier = TextEditingController();
  final _lines = [_Line()];
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.initialItemId != null) _lines.first.itemId = widget.initialItemId;
    ApiClient.get('/api/farms/${widget.farm}/suppliers').then((j) { if (mounted) setState(() => _suppliers = asList(j)); }).catchError((_) {});
    ApiClient.get('/api/farms/${widget.farm}/inventory').then((j) { if (mounted) setState(() => _items = asList(j)); }).catchError((_) {});
  }

  Future<void> _submit() async {
    setState(() { _busy = true; _error = null; });
    try {
      var supplierId = _supplier;
      if (supplierId == null && _newSupplier.text.trim().isNotEmpty) {
        final s = await ApiClient.post('/api/farms/${widget.farm}/suppliers', {'name': _newSupplier.text.trim(), 'phone': null, 'email': null});
        supplierId = s is Map ? '${s['id']}' : null;
      }
      final lines = <Map<String, dynamic>>[];
      for (final l in _lines) {
        final q = double.tryParse(l.qty.text.trim());
        final p = double.tryParse(l.price.text.trim());
        if (l.itemId == null || q == null || q <= 0 || p == null || p < 0) {
          throw const ApiException('Each line needs an item, a quantity and a unit price.');
        }
        lines.add({'inventoryItemId': l.itemId, 'quantity': q, 'unitPrice': p});
      }
      if (supplierId == null) throw const ApiException('Choose a supplier or type a new one.');
      await ApiClient.post('/api/farms/${widget.farm}/orders', {'supplierId': supplierId, 'lineItems': lines});
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
      sheetTitle('New order', 'Saved as a draft. Place it from the order.'),
      DropdownButtonFormField<String>(
        value: _supplier,
        decoration: const InputDecoration(labelText: 'Supplier', border: OutlineInputBorder()),
        items: [for (final s in _suppliers) DropdownMenuItem(value: '${s['id']}', child: Text('${s['name']}'))],
        onChanged: (v) => setState(() => _supplier = v),
      ),
      const SizedBox(height: 10),
      textField(_newSupplier, 'Or a new supplier name'),
      for (var n = 0; n < _lines.length; n++) ...[
        DropdownButtonFormField<String>(
          value: _lines[n].itemId,
          isExpanded: true,
          decoration: InputDecoration(labelText: 'Item ${n + 1}', border: const OutlineInputBorder()),
          items: [for (final i in _items) DropdownMenuItem(value: '${i['id']}', child: Text('${i['name']} (${i['unit']})'))],
          onChanged: (v) => setState(() => _lines[n].itemId = v),
        ),
        const SizedBox(height: 8),
        Row(children: [
          Expanded(child: numField(_lines[n].qty, 'Quantity')),
          const SizedBox(width: 10),
          Expanded(child: numField(_lines[n].price, 'Unit price (₦)')),
        ]),
      ],
      TextButton.icon(onPressed: () => setState(() => _lines.add(_Line())), icon: const Icon(Icons.add), label: const Text('Add line')),
      if (_error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
      primaryButton('Create order', _submit, busy: _busy),
    ]);
  }
}
