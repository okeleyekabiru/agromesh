import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import '../../services/api_client.dart';
import 'boundary_capture_screen.dart';

/// Quick add for a block: name, area, what is growing (one crop, or a mix of crops with each one's
/// share of the block) and the year it was planted. Fertilizer timing depends on the crop, so a mixed
/// block has to say which crops are in it. Boundary capture happens afterwards. Needs connectivity.
class AddBlockScreen extends StatefulWidget {
  /// When set, the form edits this block instead of adding a new one.
  final Block? editing;
  const AddBlockScreen({super.key, this.editing});

  @override
  State<AddBlockScreen> createState() => _AddBlockScreenState();
}

class _AddBlockScreenState extends State<AddBlockScreen> {

  final _nameCtrl = TextEditingController();
  final _areaCtrl = TextEditingController();
  final _yearCtrl = TextEditingController();
  bool _mixed = false;
  String _single = 'OilPalm';
  final Map<String, int> _mix = {};
  bool _isSubmitting = false;
  String? _error;
  int? _treeCount;

  bool get _editing => widget.editing != null;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) { if (mounted) context.read<AppStore>().fetchCrops(); });
    final b = widget.editing;
    if (b == null) return;
    _nameCtrl.text = b.name;
    _areaCtrl.text = b.hectares.toStringAsFixed(2);
    _treeCount = b.treeCount;
    // "Mixed" is not a crop, it is several of these: a legacy Mixed entry is dropped so the user is asked.
    final real = b.crops.where((c) => c.cropId != 'Mixed').toList();
    if (real.length > 1) {
      _mixed = true;
      for (final c in real) {
        _mix[c.cropId] = c.percentage.round();
      }
    } else if (real.length == 1) {
      _single = real.first.cropId;
    }
    // The year planted is not kept on the phone, so read it from the server.
    ApiClient.get('/api/farms/${b.farmId}/blocks').then((j) {
      for (final m in (j is List ? j : const [])) {
        if (m is Map && m['id'] == b.id && m['plantingYear'] != null && mounted) {
          setState(() => _yearCtrl.text = '${m['plantingYear']}');
        }
      }
    }).catchError((_) {});
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _areaCtrl.dispose();
    _yearCtrl.dispose();
    super.dispose();
  }

  /// The farm's crops, plus any crop already on the block being edited that is no longer in the list.
  List<Crop> _cropList(AppStore store) {
    final list = [...store.crops.where((c) => c.id != 'Mixed')];
    for (final c in widget.editing?.crops ?? const <BlockCrop>[]) {
      if (c.cropId != 'Mixed' && !list.any((x) => x.id == c.cropId)) list.add(Crop(id: c.cropId, name: c.cropName));
    }
    return list;
  }

  String _nameOf(String id) {
    final store = context.read<AppStore>();
    for (final c in _cropList(store)) {
      if (c.id == id) return c.name;
    }
    return id;
  }

  /// Asks for a name and adds it to the farm's crop list, then selects it.
  Future<void> _addCrop() async {
    final ctrl = TextEditingController();
    final name = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Add new crop'),
        content: TextField(controller: ctrl, autofocus: true, maxLength: 100, decoration: const InputDecoration(hintText: 'e.g. Yam')),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(ctx, ctrl.text.trim()), child: const Text('Add')),
        ],
      ),
    );
    ctrl.dispose();
    if (name == null || name.isEmpty || !mounted) return;
    try {
      final crop = await context.read<AppStore>().createCrop(name);
      if (!mounted) return;
      setState(() {
        if (_mixed) {
          _toggle(crop.id);
        } else {
          _single = crop.id;
        }
        _error = null;
      });
    } catch (e) {
      if (mounted) setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    }
  }

  int get _mixTotal => _mix.values.fold(0, (a, b) => a + b);

  void _splitEvenly() {
    if (_mix.isEmpty) return;
    final base = 100 ~/ _mix.length;
    var first = true;
    for (final k in _mix.keys.toList()) {
      _mix[k] = first ? base + (100 - base * _mix.length) : base;
      first = false;
    }
  }

  void _toggle(String c) {
    setState(() {
      if (_mix.containsKey(c)) {
        _mix.remove(c);
      } else {
        _mix[c] = 0;
      }
      _splitEvenly();
    });
  }

  Future<void> _submit() async {
    final name = _nameCtrl.text.trim();
    if (name.isEmpty) {
      setState(() => _error = 'Enter a block name.');
      return;
    }
    if (_mixed && _mix.length < 2) {
      setState(() => _error = 'Choose at least two crops for a mixed block.');
      return;
    }
    if (_mixed && _mixTotal != 100) {
      setState(() => _error = 'The crop shares must add up to 100% (now $_mixTotal%).');
      return;
    }
    final year = _yearCtrl.text.trim().isEmpty ? null : int.tryParse(_yearCtrl.text.trim());
    if (_yearCtrl.text.trim().isNotEmpty && year == null) {
      setState(() => _error = 'Enter the year planted as a number, e.g. 2019.');
      return;
    }

    final crops = _mixed
        ? _mix.entries.map((e) => BlockCrop(cropId: e.key, cropName: _nameOf(e.key), percentage: e.value.toDouble())).toList()
        : [BlockCrop(cropId: _single, cropName: _nameOf(_single), percentage: 100)];

    setState(() {
      _isSubmitting = true;
      _error = null;
    });

    try {
      final store = context.read<AppStore>();
      if (_editing) {
        await store.updateBlock(
          widget.editing!.id,
          name,
          crops: crops,
          areaValue: double.tryParse(_areaCtrl.text.trim()) ?? 0,
          plantingYear: year,
          treeCount: _treeCount,
        );
      } else {
        final created = await store.addBlock(
          name,
          crops: crops,
          areaValue: double.tryParse(_areaCtrl.text.trim()) ?? 0,
          plantingYear: year,
        );
        // Straight on to the boundary: the block is not much use for scheduling or coverage without one.
        if (created != null && mounted) {
          final messenger = ScaffoldMessenger.of(context);
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (_) => BoundaryCaptureScreen(blockId: created.id, blockName: created.name)),
          );
          messenger.showSnackBar(SnackBar(
            content: Text('Block "$name" added. Now mark its boundary, or go back to do it later.'),
            backgroundColor: IlevoraColors.leaf,
            behavior: SnackBarBehavior.floating,
          ));
          return;
        }
      }
      if (mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(_editing ? 'Block "$name" saved' : 'Block "$name" added'),
            backgroundColor: IlevoraColors.leaf,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isOnline = context.watch<AppStore>().isOnline;
    final crops = _cropList(context.watch<AppStore>());

    return Scaffold(
      backgroundColor: IlevoraColors.mist,
      appBar: AppBar(title: Text(_editing ? 'Edit Block' : 'Add Block')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (!isOnline)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: IlevoraColors.harvest.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
                child: const Row(
                  children: [
                    Icon(Icons.cloud_off_outlined, size: 16, color: IlevoraColors.harvest),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'You are offline. Adding a block requires an internet connection.',
                        style: TextStyle(fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
            AgroTextField(label: 'Block Name', hint: 'e.g. North Block', controller: _nameCtrl),
            const SizedBox(height: 14),
            AgroTextField(
              label: 'Area',
              hint: 'In your farm\'s unit (acres or hectares)',
              controller: _areaCtrl,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),
            const SizedBox(height: 18),
            const Text('What is growing here?', style: TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w600, color: IlevoraColors.forest)),
            const SizedBox(height: 8),
            SegmentedButton<bool>(
              segments: const [
                ButtonSegment(value: false, label: Text('One crop')),
                ButtonSegment(value: true, label: Text('Mixed')),
              ],
              selected: {_mixed},
              onSelectionChanged: (s) => setState(() => _mixed = s.first),
            ),
            const SizedBox(height: 12),
            if (!_mixed)
              Wrap(
                spacing: 8,
                children: [
                  ...crops.map((c) => ChoiceChip(
                    label: Text(c.name),
                    selected: _single == c.id,
                    onSelected: (_) => setState(() => _single = c.id),
                  )),
                  ActionChip(avatar: const Icon(Icons.add, size: 16), label: const Text('Add new crop'), onPressed: _addCrop),
                ],
              )
            else ...[
              const Text('Pick every crop in this block, then its share of the block. Fertilizer timing follows the crops.',
                style: TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12, color: IlevoraColors.stone)),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                children: [
                  ...crops.map((c) => FilterChip(
                    label: Text(c.name),
                    selected: _mix.containsKey(c.id),
                    onSelected: (_) => _toggle(c.id),
                  )),
                  ActionChip(avatar: const Icon(Icons.add, size: 16), label: const Text('Add new crop'), onPressed: _addCrop),
                ],
              ),
              for (final c in _mix.keys.toList())
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Row(
                    children: [
                      Expanded(child: Text(_nameOf(c), style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, fontWeight: FontWeight.w600, color: IlevoraColors.forest))),
                      SizedBox(
                        width: 90,
                        child: TextFormField(
                          key: ValueKey('$c-${_mix[c]}'),
                          initialValue: '${_mix[c]}',
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(suffixText: '%'),
                          onChanged: (v) => _mix[c] = int.tryParse(v) ?? 0,
                        ),
                      ),
                    ],
                  ),
                ),
              if (_mix.length > 1)
                TextButton(onPressed: () => setState(_splitEvenly), child: const Text('Split evenly')),
            ],
            const SizedBox(height: 18),
            AgroTextField(
              label: 'Year planted (optional)',
              hint: 'e.g. 2019 — young crops are fed more often',
              controller: _yearCtrl,
              keyboardType: TextInputType.number,
            ),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text(_error!, style: const TextStyle(color: IlevoraColors.alert, fontSize: 13)),
              ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: (!isOnline || _isSubmitting) ? null : _submit,
              child: _isSubmitting
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : Text(_editing ? 'Save changes' : 'Add Block'),
            ),
          ],
        ),
      ),
    );
  }
}
