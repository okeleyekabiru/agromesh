import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import '../blocks/add_block_screen.dart';
import 'common.dart';

/// Finishes a draft farm: where it is, its blocks, then marks setup complete (the web onboarding, in three steps).
class FarmSetupScreen extends StatefulWidget {
  const FarmSetupScreen({super.key});

  @override
  State<FarmSetupScreen> createState() => _FarmSetupScreenState();
}

class _FarmSetupScreenState extends State<FarmSetupScreen> {
  List<Map<String, dynamic>> _states = [];
  String? _stateId;
  final _lga = TextEditingController();
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _lga.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final j = await ApiClient.get('/api/reference/states');
      _states = asList(j);
    } catch (e) {
      _error = errText(e);
    }
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _saveLocation() async {
    if (_stateId == null) return;
    setState(() { _saving = true; _error = null; });
    try {
      final store = context.read<AppStore>();
      await ApiClient.patch('/api/farms/${store.farmId}/location', {
        'stateId': _stateId,
        'lgaFreetext': _lga.text.trim().isEmpty ? null : _lga.text.trim(),
      });
      await store.refreshFarmDetails();
      await store.refreshWeather();
      if (mounted) toast(context, 'Location saved.');
    } catch (e) {
      _error = errText(e);
    }
    if (mounted) setState(() => _saving = false);
  }

  Future<void> _finish() async {
    final store = context.read<AppStore>();
    if (store.blocks.isEmpty) {
      toast(context, 'Add at least one block first.');
      return;
    }
    setState(() { _saving = true; _error = null; });
    try {
      await ApiClient.patch('/api/farms/${store.farmId}/setup-status');
      await store.refreshFarmDetails();
      await store.refreshWeather();
      if (mounted) Navigator.pop(context);
      return;
    } catch (e) {
      _error = errText(e);
    }
    if (mounted) setState(() => _saving = false);
  }

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: const Text('Finish farm setup', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AgroColors.palm))
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                AgroCard(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('1 · Where is the farm?', style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                        const SizedBox(height: 4),
                        Text('Used for the weather forecast and the spraying schedule.',
                            style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String>(
                          value: _stateId,
                          isExpanded: true,
                          decoration: const InputDecoration(labelText: 'State', border: OutlineInputBorder()),
                          items: [
                            for (final s in _states)
                              DropdownMenuItem(value: s['id'] as String, child: Text('${s['name']}')),
                          ],
                          onChanged: (v) => setState(() => _stateId = v),
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _lga,
                          decoration: const InputDecoration(labelText: 'Town or LGA (optional)', border: OutlineInputBorder()),
                        ),
                        const SizedBox(height: 10),
                        Align(
                          alignment: Alignment.centerRight,
                          child: FilledButton(
                            onPressed: _saving || _stateId == null ? null : _saveLocation,
                            child: const Text('Save location'),
                          ),
                        ),
                        if (store.farmLocation.isNotEmpty)
                          Text('Current: ${store.farmLocation}', style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.leaf)),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                AgroCard(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('2 · Blocks and boundaries', style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                        const SizedBox(height: 4),
                        Text('${store.blocks.length} block${store.blocks.length == 1 ? '' : 's'} added. Walk the edge of each block to capture its boundary.',
                            style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                        const SizedBox(height: 10),
                        OutlinedButton.icon(
                          onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const AddBlockScreen())),
                          icon: const Icon(Icons.add),
                          label: const Text('Add a block'),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                AgroCard(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('3 · Done', style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                        const SizedBox(height: 10),
                        FilledButton(onPressed: _saving ? null : _finish, child: const Text('Mark setup complete')),
                      ],
                    ),
                  ),
                ),
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert)),
                  ),
              ],
            ),
    );
  }
}
