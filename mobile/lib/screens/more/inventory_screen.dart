import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';
import 'ops_common.dart';

/// Stock levels with pick, adjust (needs approval), set price (needs approval), movements and add item.
class InventoryScreen extends StatelessWidget {
  const InventoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final farm = farmIdFrom(context);
    return LoadScaffold<List<Map<String, dynamic>>>(
      title: 'Inventory',
      load: () async => asList(await ApiClient.get('/api/farms/$farm/inventory')),
      builder: (context, items, reload) => _Body(items: items, reload: reload),
      // The FAB lives outside the builder, so it asks the body to reload via the route.
      fab: Builder(
        builder: (ctx) => FloatingActionButton.extended(
          backgroundColor: AgroColors.leaf,
          foregroundColor: Colors.white,
          onPressed: () async {
            final added = await showSheet<bool>(ctx, _AddItemForm(farm: farm));
            if (added == true && ctx.mounted) {
              Navigator.pushReplacement(ctx, MaterialPageRoute(builder: (_) => const InventoryScreen()));
            }
          },
          icon: const Icon(Icons.add),
          label: const Text('Add item'),
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  final List<Map<String, dynamic>> items;
  final Future<void> Function() reload;
  const _Body({required this.items, required this.reload});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return ListView(children: [emptyNote('No inventory yet. Tap Add item to start.')]);
    final low = items.where((i) => i['isLowStock'] == true).length;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (low > 0)
          Container(
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: AgroColors.harvest.withOpacity(0.08),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: AgroColors.harvest.withOpacity(0.25)),
            ),
            child: Text('$low item${low > 1 ? 's' : ''} below the reorder level.',
                style: GoogleFonts.inter(fontSize: 12, color: AgroColors.ink)),
          ),
        AgroCard(
          child: Column(children: [
            for (final i in items)
              InkWell(
                onTap: () => showSheet(context, _ItemActions(item: i, reload: reload)),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
                  child: Row(children: [
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text('${i['name']}', style: GoogleFonts.inter(fontSize: 13.5, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                        const SizedBox(height: 2),
                        Text('${i['categoryName'] ?? ''}${dbl(i['unitCost']) > 0 ? ' · ${naira(dbl(i['unitCost']))}/${i['unit']}' : ' · no price'}',
                            style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
                      ]),
                    ),
                    Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                      Text('${qty(i['currentStock'])} ${i['unit']}',
                          style: GoogleFonts.jetBrainsMono(fontSize: 12.5, fontWeight: FontWeight.w600,
                              color: i['isLowStock'] == true ? AgroColors.alert : AgroColors.forest)),
                      if (i['isLowStock'] == true) const AgroBadge(label: 'Low', variant: AgroBadgeVariant.red),
                    ]),
                  ]),
                ),
              ),
          ]),
        ),
        const SizedBox(height: 90),
      ],
    );
  }
}

class _ItemActions extends StatelessWidget {
  final Map<String, dynamic> item;
  final Future<void> Function() reload;
  const _ItemActions({required this.item, required this.reload});

  Future<void> _then(BuildContext ctx, Widget form) async {
    Navigator.pop(ctx);
    final done = await showSheet<String>(ctx, form);
    if (done != null && ctx.mounted) {
      toast(ctx, done);
      await reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final farm = farmIdFrom(context);
    final id = '${item['id']}';
    return Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
      sheetTitle('${item['name']}', '${qty(item['currentStock'])} ${item['unit']} in store · reorder at ${qty(item['reorderThreshold'])}'),
      if (dbl(item['unitCost']) > 0)
        KvRow('Price', '${naira(dbl(item['unitCost']))} per ${item['unit']}  (${item['packLabel']} of ${qty(item['packSize'])})'),
      const SizedBox(height: 6),
      _act(Icons.output, 'Pick for a block', () => _then(context, _PickForm(farm: farm, item: item))),
      _act(Icons.tune, 'Adjust stock (needs approval)', () => _then(context, _AdjustForm(farm: farm, item: item))),
      _act(Icons.sell_outlined, 'Set price (needs approval)', () => _then(context, _PriceForm(farm: farm, item: item))),
      _act(Icons.history, 'Stock movements', () {
        Navigator.pop(context);
        showSheet(context, _Movements(farm: farm, id: id, name: '${item['name']}'));
      }),
    ]);
  }

  Widget _act(IconData icon, String label, VoidCallback onTap) => ListTile(
        contentPadding: EdgeInsets.zero,
        leading: Icon(icon, color: AgroColors.leaf),
        title: Text(label, style: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w500)),
        trailing: const Icon(Icons.chevron_right, size: 18),
        onTap: onTap,
      );
}

class _PickForm extends StatefulWidget {
  final String farm;
  final Map<String, dynamic> item;
  const _PickForm({required this.farm, required this.item});
  @override
  State<_PickForm> createState() => _PickFormState();
}

class _PickFormState extends State<_PickForm> {
  final _qty = TextEditingController();
  final _purpose = TextEditingController();
  String? _block;
  bool _busy = false;
  String? _error;

  Future<void> _submit() async {
    final q = double.tryParse(_qty.text.trim());
    if (q == null || q <= 0 || _block == null || _purpose.text.trim().isEmpty) {
      setState(() => _error = 'Enter a quantity, pick a block and say what it is for.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      final res = await ApiClient.post('/api/farms/${widget.farm}/inventory/${widget.item['id']}/pick',
          {'quantity': q, 'blockId': _block, 'purpose': _purpose.text.trim()});
      final warn = res is Map ? res['lowStockWarning'] as String? : null;
      if (mounted) Navigator.pop(context, warn ?? 'Picked ${qty(q)} ${widget.item['unit']}.');
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final blocks = context.read<AppStore>().blocks;
    return Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
      sheetTitle('Pick ${widget.item['name']}', 'Take stock out of the store for a block.'),
      numField(_qty, 'Quantity (${widget.item['unit']})'),
      DropdownButtonFormField<String>(
        value: _block,
        decoration: const InputDecoration(labelText: 'Block', border: OutlineInputBorder()),
        items: [for (final b in blocks) DropdownMenuItem(value: b.id, child: Text(b.name))],
        onChanged: (v) => setState(() => _block = v),
      ),
      const SizedBox(height: 10),
      textField(_purpose, 'Purpose', hint: 'e.g. Spraying block A'),
      if (_error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
      primaryButton('Pick', _submit, busy: _busy),
    ]);
  }
}

class _AdjustForm extends StatefulWidget {
  final String farm;
  final Map<String, dynamic> item;
  const _AdjustForm({required this.farm, required this.item});
  @override
  State<_AdjustForm> createState() => _AdjustFormState();
}

class _AdjustFormState extends State<_AdjustForm> {
  final _qty = TextEditingController();
  final _notes = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _submit() async {
    final q = double.tryParse(_qty.text.trim());
    if (q == null || q < 0) {
      setState(() => _error = 'Enter the counted quantity.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      await ApiClient.patch('/api/farms/${widget.farm}/inventory/${widget.item['id']}/adjust',
          {'newQuantity': q, 'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim()});
      if (mounted) Navigator.pop(context, 'Adjustment sent for approval.');
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) => Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        sheetTitle('Adjust ${widget.item['name']}', 'Now ${qty(widget.item['currentStock'])} ${widget.item['unit']}. An admin approves the new count.'),
        numField(_qty, 'Counted quantity (${widget.item['unit']})'),
        textField(_notes, 'Reason', hint: 'e.g. stock count', lines: 2),
        if (_error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
        primaryButton('Send for approval', _submit, busy: _busy),
      ]);
}

class _PriceForm extends StatefulWidget {
  final String farm;
  final Map<String, dynamic> item;
  const _PriceForm({required this.farm, required this.item});
  @override
  State<_PriceForm> createState() => _PriceFormState();
}

class _PriceFormState extends State<_PriceForm> {
  late final _label = TextEditingController(text: '${widget.item['packLabel'] ?? ''}');
  late final _size = TextEditingController(text: dbl(widget.item['packSize']) > 0 ? qty(widget.item['packSize']) : '');
  final _price = TextEditingController();
  bool _backfill = false;
  bool _busy = false;
  String? _error;

  Future<void> _submit() async {
    final size = double.tryParse(_size.text.trim());
    final price = double.tryParse(_price.text.trim());
    if (size == null || size <= 0 || price == null || price <= 0 || _label.text.trim().isEmpty) {
      setState(() => _error = 'Enter the pack name, the pack size and the price per pack.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      await apiPut('/api/farms/${widget.farm}/inventory/${widget.item['id']}/pricing',
          {'packSize': size, 'packLabel': _label.text.trim(), 'pricePerPack': price, 'backfillActivities': _backfill});
      if (mounted) Navigator.pop(context, 'Price sent for approval.');
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) => Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        sheetTitle('Price ${widget.item['name']}', 'An admin approves the price before it applies.'),
        textField(_label, 'Pack name', hint: 'e.g. bag, drum, carton'),
        numField(_size, 'Units in one pack (${widget.item['unit']})'),
        numField(_price, 'Price per pack (₦)'),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          value: _backfill,
          onChanged: (v) => setState(() => _backfill = v),
          title: Text('Also cost past activities that used this item', style: GoogleFonts.inter(fontSize: 12.5)),
        ),
        if (_error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
        primaryButton('Send for approval', _submit, busy: _busy),
      ]);
}

class _Movements extends StatelessWidget {
  final String farm;
  final String id;
  final String name;
  const _Movements({required this.farm, required this.id, required this.name});

  String _type(dynamic t) => t is int ? (const ['In', 'Out', 'Adjustment'][t.clamp(0, 2)]) : '$t';

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<dynamic>(
      future: ApiClient.get('/api/farms/$farm/inventory/$id/movements'),
      builder: (context, snap) {
        Widget body;
        if (snap.connectionState != ConnectionState.done) {
          body = const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator(color: AgroColors.palm)));
        } else if (snap.hasError) {
          body = emptyNote(errText(snap.error!));
        } else {
          final rows = asList(snap.data);
          body = rows.isEmpty
              ? emptyNote('No movements yet.')
              : Column(children: [
                  for (final m in rows)
                    KvRow('${dateText(m['date'])} · ${_type(m['type'])}${(m['notes'] ?? '') != '' ? ' · ${m['notes']}' : ''}', qty(m['quantity'])),
                ]);
        }
        return Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          sheetTitle('$name · movements'),
          body,
        ]);
      },
    );
  }
}

class _AddItemForm extends StatefulWidget {
  final String farm;
  const _AddItemForm({required this.farm});
  @override
  State<_AddItemForm> createState() => _AddItemFormState();
}

class _AddItemFormState extends State<_AddItemForm> {
  final _name = TextEditingController();
  final _unit = TextEditingController();
  final _qty = TextEditingController();
  final _reorder = TextEditingController(text: '0');
  List<Map<String, dynamic>> _cats = [];
  String? _cat;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    ApiClient.get('/api/farms/${widget.farm}/inventory-categories').then((j) {
      if (mounted) setState(() => _cats = asList(j));
    }).catchError((_) {});
  }

  Future<void> _submit() async {
    final q = double.tryParse(_qty.text.trim());
    final r = double.tryParse(_reorder.text.trim()) ?? 0;
    if (_name.text.trim().isEmpty || _unit.text.trim().isEmpty || _cat == null || q == null || q < 0) {
      setState(() => _error = 'Fill in the name, unit, category and opening quantity.');
      return;
    }
    setState(() { _busy = true; _error = null; });
    try {
      await ApiClient.post('/api/farms/${widget.farm}/inventory', {
        'name': _name.text.trim(), 'unit': _unit.text.trim(), 'categoryId': _cat,
        'quantity': q, 'reorderThreshold': r, 'chemicalProductId': null,
      });
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) => Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        sheetTitle('Add stock', 'A new item, or more of one that exists (same name).'),
        textField(_name, 'Item name'),
        textField(_unit, 'Unit', hint: 'kg, litres, bags'),
        DropdownButtonFormField<String>(
          value: _cat,
          decoration: const InputDecoration(labelText: 'Category', border: OutlineInputBorder()),
          items: [for (final c in _cats) DropdownMenuItem(value: '${c['id']}', child: Text('${c['name']}'))],
          onChanged: (v) => setState(() => _cat = v),
        ),
        const SizedBox(height: 10),
        numField(_qty, 'Quantity being added'),
        numField(_reorder, 'Reorder when stock falls to'),
        if (_error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
        primaryButton('Add', _submit, busy: _busy),
      ]);
}
