import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';

const _freq = ['Daily', 'Weekly', 'Monthly'];

Future<List<Map<String, dynamic>>> _loadRoles(BuildContext c) async =>
    asList(await ApiClient.get('/api/farms/${farmIdFrom(c)}/worker-roles'));

/// Pay rates per worker role (wage bands). Managers can edit rates and add bands.
class WageBandsScreen extends StatefulWidget {
  const WageBandsScreen({super.key});

  @override
  State<WageBandsScreen> createState() => _WageBandsScreenState();
}

class _WageBandsScreenState extends State<WageBandsScreen> {
  int _version = 0;

  @override
  Widget build(BuildContext context) {
    final manager = isManager(context);
    return LoadScaffold<List<Map<String, dynamic>>>(
      key: ValueKey(_version),
      title: 'Wage bands',
      load: () => _loadRoles(context),
      actions: [
        if (manager)
          TextButton(
            onPressed: () async {
              await showAddWorkerSheet(context);
              if (mounted) setState(() => _version++);
            },
            child: const Text('Add worker', style: TextStyle(color: AgroColors.white)),
          ),
      ],
      fab: manager
          ? FloatingActionButton.extended(
              backgroundColor: AgroColors.leaf,
              onPressed: () async {
                final saved = await showSheet<bool>(context, const _BandForm());
                if (saved == true && mounted) setState(() => _version++);
              },
              icon: const Icon(Icons.add),
              label: const Text('Add band'),
            )
          : null,
      builder: (ctx, roles, reload) {
        if (roles.isEmpty) return ListView(children: [emptyNote('No wage bands yet.')]);
        return ListView(
          padding: const EdgeInsets.all(16),
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            AgroCard(
              child: Column(
                children: [
                  for (final r in roles)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('${r['title']}', style: GoogleFonts.inter(fontSize: 14, fontWeight: FontWeight.w600)),
                                Text('${r['workerCount'] ?? 0} worker(s) · ${(r['defaultType'] ?? 0) == 1 ? 'Contractor' : 'Employee'}',
                                    style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                              ],
                            ),
                          ),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text(naira(r['payRate'] as num?), style: GoogleFonts.jetBrainsMono(fontSize: 13, fontWeight: FontWeight.w600)),
                              Text(_freq[((r['payFrequency'] as num?)?.toInt() ?? 0).clamp(0, 2)],
                                  style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone)),
                            ],
                          ),
                          if (manager)
                            IconButton(
                              icon: const Icon(Icons.edit_outlined, size: 18, color: AgroColors.leaf),
                              onPressed: () async {
                                final saved = await showSheet<bool>(ctx, _BandForm(role: r));
                                if (saved == true) await reload();
                              },
                            ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 80),
          ],
        );
      },
    );
  }
}

class _BandForm extends StatefulWidget {
  final Map<String, dynamic>? role;
  const _BandForm({this.role});

  @override
  State<_BandForm> createState() => _BandFormState();
}

class _BandFormState extends State<_BandForm> {
  late final TextEditingController _title = TextEditingController(text: '${widget.role?['title'] ?? ''}');
  late final TextEditingController _rate =
      TextEditingController(text: widget.role == null ? '' : '${(widget.role!['payRate'] as num?)?.round() ?? ''}');
  late int _frequency = ((widget.role?['payFrequency'] as num?)?.toInt() ?? 0).clamp(0, 2);
  int _type = 0;
  bool _saving = false;
  String? _error;

  Future<void> _save() async {
    final rate = double.tryParse(_rate.text.trim()) ?? 0;
    if (rate <= 0 || (widget.role == null && _title.text.trim().isEmpty)) {
      setState(() => _error = 'Enter a title and a pay rate above zero.');
      return;
    }
    setState(() { _saving = true; _error = null; });
    try {
      final base = '/api/farms/${farmIdFrom(context)}/worker-roles';
      if (widget.role != null) {
        await ApiClient.patch('$base/${widget.role!['id']}/rate', {'payRate': rate, 'payFrequency': _frequency});
      } else {
        await ApiClient.post(base, {'title': _title.text.trim(), 'defaultType': _type, 'payRate': rate, 'payFrequency': _frequency});
      }
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _saving = false; _error = errText(e); });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(widget.role == null ? 'Add a wage band' : 'Edit rate · ${widget.role!['title']}',
            style: GoogleFonts.syne(fontSize: 15, fontWeight: FontWeight.w700, color: AgroColors.forest)),
        const SizedBox(height: 12),
        if (widget.role == null) ...[
          TextField(controller: _title, decoration: const InputDecoration(labelText: 'Role title (e.g. Harvester)')),
          const SizedBox(height: 10),
          DropdownButtonFormField<int>(
            value: _type,
            decoration: const InputDecoration(labelText: 'Default type'),
            items: const [DropdownMenuItem(value: 0, child: Text('Employee')), DropdownMenuItem(value: 1, child: Text('Contractor'))],
            onChanged: (v) => setState(() => _type = v ?? 0),
          ),
          const SizedBox(height: 10),
        ],
        TextField(controller: _rate, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Pay rate (₦)')),
        const SizedBox(height: 10),
        DropdownButtonFormField<int>(
          value: _frequency,
          decoration: const InputDecoration(labelText: 'Paid'),
          items: [for (var i = 0; i < 3; i++) DropdownMenuItem(value: i, child: Text(_freq[i]))],
          onChanged: (v) => setState(() => _frequency = v ?? 0),
        ),
        if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12))),
        const SizedBox(height: 14),
        SizedBox(
          width: double.infinity,
          child: FilledButton(onPressed: _saving ? null : _save, child: Text(_saving ? 'Saving…' : 'Save')),
        ),
      ],
    );
  }
}

/// Add-worker form. Resolves to true when a worker was added (the store's worker list is reloaded).
Future<bool?> showAddWorkerSheet(BuildContext context) => showSheet<bool>(context, const _WorkerForm());

class _WorkerForm extends StatefulWidget {
  const _WorkerForm();

  @override
  State<_WorkerForm> createState() => _WorkerFormState();
}

class _WorkerFormState extends State<_WorkerForm> {
  final _name = TextEditingController();
  List<Map<String, dynamic>> _roles = [];
  String? _roleId;
  int _type = 0;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadAll());
  }

  Future<void> _loadAll() async {
    try {
      final roles = await _loadRoles(context);
      if (mounted) setState(() { _roles = roles; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _loading = false; });
    }
  }

  String _initials(String n) {
    final parts = n.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
    if (parts.isEmpty) return '';
    return (parts.first[0] + (parts.length > 1 ? parts.last[0] : '')).toUpperCase();
  }

  Future<void> _save() async {
    if (_name.text.trim().isEmpty || _roleId == null) {
      setState(() => _error = 'Enter a name and pick a wage band.');
      return;
    }
    setState(() { _saving = true; _error = null; });
    try {
      final store = context.read<AppStore>();
      await ApiClient.post('/api/farms/${store.farmId}/workers', {
        'name': _name.text.trim(),
        'initials': _initials(_name.text),
        'workerRoleId': _roleId,
        'type': _type,
      });
      await store.reloadWorkers();
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _saving = false; _error = errText(e); });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Add worker', style: GoogleFonts.syne(fontSize: 15, fontWeight: FontWeight.w700, color: AgroColors.forest)),
        const SizedBox(height: 12),
        if (_loading)
          const Center(child: Padding(padding: EdgeInsets.all(16), child: CircularProgressIndicator(color: AgroColors.palm)))
        else if (_roles.isEmpty)
          Text('Add a wage band first (Wage bands, then Add band).', style: GoogleFonts.inter(fontSize: 12.5, color: AgroColors.stone))
        else ...[
          TextField(controller: _name, textCapitalization: TextCapitalization.words, decoration: const InputDecoration(labelText: 'Full name')),
          const SizedBox(height: 10),
          DropdownButtonFormField<String>(
            value: _roleId,
            decoration: const InputDecoration(labelText: 'Wage band'),
            items: [for (final r in _roles) DropdownMenuItem(value: '${r['id']}', child: Text('${r['title']} · ${naira(r['payRate'] as num?)}'))],
            onChanged: (v) {
              final r = _roles.firstWhere((x) => '${x['id']}' == v, orElse: () => {});
              setState(() { _roleId = v; _type = ((r['defaultType'] as num?)?.toInt() ?? 0).clamp(0, 1); });
            },
          ),
          const SizedBox(height: 10),
          DropdownButtonFormField<int>(
            value: _type,
            decoration: const InputDecoration(labelText: 'Type'),
            items: const [DropdownMenuItem(value: 0, child: Text('Employee')), DropdownMenuItem(value: 1, child: Text('Contractor'))],
            onChanged: (v) => setState(() => _type = v ?? 0),
          ),
        ],
        if (_error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12))),
        const SizedBox(height: 14),
        SizedBox(
          width: double.infinity,
          child: FilledButton(onPressed: (_saving || _roles.isEmpty) ? null : _save, child: Text(_saving ? 'Saving…' : 'Add worker')),
        ),
      ],
    );
  }
}
