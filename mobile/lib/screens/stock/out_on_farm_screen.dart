import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';

/// Stock that was picked out of the store for a block but not yet used or returned,
/// with a way to hand the leftover back (e.g. carried 50 kg, used 38, return 12).
class OutOnFarmScreen extends StatefulWidget {
  const OutOnFarmScreen({super.key});

  @override
  State<OutOnFarmScreen> createState() => _OutOnFarmScreenState();
}

class _OutOnFarmScreenState extends State<OutOnFarmScreen> {
  @override
  void initState() {
    super.initState();
    final store = context.read<AppStore>();
    Future.microtask(store.refreshStock);
  }

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();

    // A return settles by item + block, so show one row per pair.
    final groups = <String, _Group>{};
    for (final o in store.outstanding) {
      final key = '${o.inventoryItemId}|${o.blockId}';
      final g = groups[key];
      groups[key] = g == null ? _Group(o, o.quantityOutstanding) : _Group(g.sample, g.total + o.quantityOutstanding);
    }
    final rows = groups.values.toList();

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(title: const Text('Out on the farm')),
      body: RefreshIndicator(
        color: AgroColors.palm,
        onRefresh: store.refreshStock,
        child: rows.isEmpty
            ? ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(32),
                children: [
                  const SizedBox(height: 60),
                  Center(
                    child: Text('Nothing is out on the farm.\nPick stock on the web app to see it here.',
                      textAlign: TextAlign.center,
                      style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone, height: 1.5)),
                  ),
                ],
              )
            : ListView.separated(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                itemCount: rows.length,
                separatorBuilder: (_, __) => const SizedBox(height: 10),
                itemBuilder: (context, i) {
                  final g = rows[i];
                  final blockMatches = store.blocks.where((b) => b.id == g.sample.blockId);
                  final blockName = blockMatches.isEmpty ? 'Unknown block' : blockMatches.first.name;
                  return AgroCard(
                    child: Padding(
                      padding: const EdgeInsets.all(14),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(g.sample.inventoryItemName,
                                  style: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                                const SizedBox(height: 2),
                                Text(blockName,
                                  style: GoogleFonts.jetBrainsMono(fontSize: 10, color: AgroColors.stone)),
                                const SizedBox(height: 6),
                                Text('${formatQty(g.total)} ${g.sample.unit} left',
                                  style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600, color: AgroColors.leaf)),
                              ],
                            ),
                          ),
                          OutlinedButton(
                            onPressed: () => _showReturnDialog(context, g, blockName),
                            child: const Text('Return'),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
      ),
    );
  }

  Future<void> _showReturnDialog(BuildContext context, _Group g, String blockName) async {
    final returned = await showDialog<bool>(
      context: context,
      builder: (_) => _ReturnDialog(group: g, blockName: blockName),
    );
    if (returned == true && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Returned to stock — ${g.sample.inventoryItemName}'),
          backgroundColor: AgroColors.leaf,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }
}

class _Group {
  final OutstandingStock sample;
  final double total;
  const _Group(this.sample, this.total);
}

class _ReturnDialog extends StatefulWidget {
  final _Group group;
  final String blockName;
  const _ReturnDialog({required this.group, required this.blockName});

  @override
  State<_ReturnDialog> createState() => _ReturnDialogState();
}

class _ReturnDialogState extends State<_ReturnDialog> {
  final _qtyCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  bool _saving = false;
  String? _error;

  @override
  void dispose() {
    _qtyCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final qty = double.tryParse(_qtyCtrl.text);
    if (qty == null || qty <= 0) {
      setState(() => _error = 'Enter how much you are returning.');
      return;
    }
    if (qty > widget.group.total) {
      setState(() => _error = 'Only ${formatQty(widget.group.total)} ${widget.group.sample.unit} is still out.');
      return;
    }
    setState(() { _saving = true; _error = null; });
    try {
      await context.read<AppStore>().returnStock(
        inventoryItemId: widget.group.sample.inventoryItemId,
        blockId: widget.group.sample.blockId,
        quantity: qty,
        notes: _notesCtrl.text,
      );
      if (mounted) Navigator.pop(context, true);
    } on ApiException catch (e) {
      if (mounted) setState(() { _saving = false; _error = e.message; });
    } catch (e) {
      if (mounted) setState(() { _saving = false; _error = e.toString().replaceFirst('Exception: ', ''); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final unit = widget.group.sample.unit;
    return AlertDialog(
      title: Text('Return ${widget.group.sample.inventoryItemName}'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${widget.blockName}: ${formatQty(widget.group.total)} $unit still out.',
              style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
            const SizedBox(height: 12),
            TextField(
              controller: _qtyCtrl,
              autofocus: true,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(labelText: 'Quantity returning ($unit)'),
            ),
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton(
                onPressed: _saving ? null : () => setState(() => _qtyCtrl.text = formatQty(widget.group.total)),
                child: Text('Return everything left (${formatQty(widget.group.total)} $unit)'),
              ),
            ),
            TextField(
              controller: _notesCtrl,
              decoration: const InputDecoration(labelText: 'Note (optional)'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 10),
              Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert)),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: _saving ? null : () => Navigator.pop(context, false), child: const Text('Cancel')),
        ElevatedButton(
          onPressed: _saving ? null : _submit,
          child: _saving
              ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              : const Text('Return to stock'),
        ),
      ],
    );
  }
}
