import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import '../../services/api_client.dart';

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
  /// Real crops only: "Mixed" is not a crop, it is several of these.
  static const _crops = [CropType.oilPalm, CropType.maize, CropType.cocoa, CropType.cassava];

  final _nameCtrl = TextEditingController();
  final _areaCtrl = TextEditingController();
  final _yearCtrl = TextEditingController();
  bool _mixed = false;
  CropType _single = CropType.oilPalm;
  final Map<CropType, int> _mix = {};
  bool _isSubmitting = false;
  String? _error;
  int? _treeCount;

  bool get _editing => widget.editing != null;

  @override
  void initState() {
    super.initState();
    final b = widget.editing;
    if (b == null) return;
    _nameCtrl.text = b.name;
    _areaCtrl.text = b.hectares.toStringAsFixed(2);
    _treeCount = b.treeCount;
    final real = b.crops.where((c) => _crops.contains(c.cropType)).toList();
    if (real.length > 1) {
      _mixed = true;
      for (final c in real) {
        _mix[c.cropType] = c.percentage.round();
      }
    } else if (real.length == 1) {
      _single = real.first.cropType;
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

  void _toggle(CropType c) {
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
        ? _mix.entries.map((e) => BlockCrop(cropType: e.key, percentage: e.value.toDouble())).toList()
        : [BlockCrop(cropType: _single, percentage: 100)];

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
        await store.addBlock(
          name,
          crops: crops,
          areaValue: double.tryParse(_areaCtrl.text.trim()) ?? 0,
          plantingYear: year,
        );
      }
      if (mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(_editing ? 'Block "$name" saved' : 'Block "$name" added'),
            backgroundColor: AgroColors.leaf,
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

    return Scaffold(
      backgroundColor: AgroColors.mist,
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
                decoration: BoxDecoration(color: AgroColors.harvest.withOpacity(0.1), borderRadius: BorderRadius.circular(8)),
                child: const Row(
                  children: [
                    Icon(Icons.cloud_off_outlined, size: 16, color: AgroColors.harvest),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'You are offline. Adding a block requires an internet connection.',
                        style: TextStyle(fontSize: 12),
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
            Text('What is growing here?', style: GoogleFonts.inter(fontSize: 12, fontWeight: FontWeight.w600, color: AgroColors.forest)),
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
                children: _crops.map((c) => ChoiceChip(
                  label: Text(c.label),
                  selected: _single == c,
                  onSelected: (_) => setState(() => _single = c),
                )).toList(),
              )
            else ...[
              Text('Pick every crop in this block, then its share of the block. Fertilizer timing follows the crops.',
                style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                children: _crops.map((c) => FilterChip(
                  label: Text(c.label),
                  selected: _mix.containsKey(c),
                  onSelected: (_) => _toggle(c),
                )).toList(),
              ),
              for (final c in _mix.keys.toList())
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Row(
                    children: [
                      Expanded(child: Text(c.label, style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600, color: AgroColors.forest))),
                      SizedBox(
                        width: 90,
                        child: TextFormField(
                          key: ValueKey('${c.name}-${_mix[c]}'),
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
                child: Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12)),
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
