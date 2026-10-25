import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';
import 'ops_common.dart';

/// Every logged activity, newest first, 20 at a time. Tap one for its details.
class ActivitiesScreen extends StatefulWidget {
  const ActivitiesScreen({super.key});

  @override
  State<ActivitiesScreen> createState() => _ActivitiesScreenState();
}

class _ActivitiesScreenState extends State<ActivitiesScreen> {
  static const _pageSize = 20;
  final List<Map<String, dynamic>> _items = [];
  int _page = 0;
  int _total = 0;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load(reset: true));
  }

  Future<void> _load({bool reset = false}) async {
    if (_loading) return;
    final next = reset ? 1 : _page + 1;
    setState(() { _loading = true; _error = null; });
    try {
      final json = await ApiClient.get('/api/farms/${farmIdFrom(context)}/activities?page=$next&pageSize=$_pageSize');
      final m = json is Map ? json.cast<String, dynamic>() : <String, dynamic>{};
      final rows = asList(m['items']);
      if (!mounted) return;
      setState(() {
        if (reset) _items.clear();
        _items.addAll(rows);
        _page = next;
        _total = (m['total'] as num?)?.toInt() ?? _items.length;
      });
    } catch (e) {
      if (mounted) setState(() => _error = errText(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: const Text('Activities', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      body: RefreshIndicator(
        color: AgroColors.palm,
        onRefresh: () => _load(reset: true),
        child: _items.isEmpty
            ? ListView(children: [
                if (_loading) const Padding(padding: EdgeInsets.all(48), child: Center(child: CircularProgressIndicator(color: AgroColors.palm)))
                else if (_error != null)
                  Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(children: [
                      Text(_error!, textAlign: TextAlign.center, style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone)),
                      const SizedBox(height: 12),
                      OutlinedButton(onPressed: () => _load(reset: true), child: const Text('Retry')),
                    ]),
                  )
                else emptyNote('No activities logged yet.'),
              ])
            : ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  AgroCard(
                    child: Column(children: [
                      for (final a in _items) _ActivityRow(a: a, onTap: () => _open(a)),
                    ]),
                  ),
                  if (_error != null)
                    Padding(padding: const EdgeInsets.only(top: 10), child: Text(_error!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
                  if (_items.length < _total)
                    Padding(
                      padding: const EdgeInsets.only(top: 10),
                      child: OutlinedButton(
                        onPressed: _loading ? null : _load,
                        child: Text(_loading ? 'Loading…' : 'Load more (${_items.length} of $_total)'),
                      ),
                    ),
                  const SizedBox(height: 40),
                ],
              ),
      ),
    );
  }

  void _open(Map<String, dynamic> a) {
    showSheet(context, _ActivityDetail(id: '${a['id']}', summary: a));
  }
}

class _ActivityRow extends StatelessWidget {
  final Map<String, dynamic> a;
  final VoidCallback onTap;
  const _ActivityRow({required this.a, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cost = dbl(a['labourCost']) + dbl(a['inputCost']);
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
        child: Row(children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(activityName(a['activityType']), style: GoogleFonts.inter(fontSize: 13.5, fontWeight: FontWeight.w600, color: AgroColors.ink)),
              const SizedBox(height: 2),
              Text('${blockName(context, a['blockId'] as String?)} · ${dateText(a['date'])}',
                  style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
            ]),
          ),
          Text(naira(cost), style: GoogleFonts.jetBrainsMono(fontSize: 12, color: AgroColors.forest)),
          const Icon(Icons.chevron_right, size: 18, color: AgroColors.stone),
        ]),
      ),
    );
  }
}

class _ActivityDetail extends StatefulWidget {
  final String id;
  final Map<String, dynamic> summary;
  const _ActivityDetail({required this.id, required this.summary});

  @override
  State<_ActivityDetail> createState() => _ActivityDetailState();
}

class _ActivityDetailState extends State<_ActivityDetail> {
  Map<String, dynamic>? _a;
  String? _error;

  @override
  void initState() {
    super.initState();
    _a = widget.summary;
    WidgetsBinding.instance.addPostFrameCallback((_) => _fetch());
  }

  Future<void> _fetch() async {
    try {
      final json = await ApiClient.get('/api/farms/${farmIdFrom(context)}/activities/${widget.id}');
      if (mounted && json is Map) setState(() => _a = json.cast<String, dynamic>());
    } catch (e) {
      if (mounted) setState(() => _error = errText(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    final a = _a!;
    final workers = asList(a['workers']);
    final inputs = asList(a['inputs']);
    final area = a['workedArea'] is Map ? (a['workedArea'] as Map)['displayValue'] : null;
    final unit = a['workedArea'] is Map ? (a['workedArea'] as Map)['displayUnit'] : null;
    final notes = (a['notes'] as String?)?.trim() ?? '';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        sheetTitle(activityName(a['activityType']), '${blockName(context, a['blockId'] as String?)} · ${dateText(a['date'])}'),
        if (a['chemicalProductName'] != null) KvRow('Chemical', '${a['chemicalProductName']}'),
        if (area != null) KvRow('Area worked', '$area ${unit ?? ''}'),
        KvRow('Labour cost', naira(dbl(a['labourCost']))),
        KvRow('Input cost', naira(dbl(a['inputCost']))),
        KvRow('Total', naira(dbl(a['labourCost']) + dbl(a['inputCost']))),
        if (a['createdOffline'] == true) const KvRow('Logged', 'Offline, synced later'),
        if (notes.isNotEmpty) ...[
          const SizedBox(height: 8),
          Text('Notes', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
          Text(notes, style: GoogleFonts.inter(fontSize: 13, color: AgroColors.ink)),
        ],
        if (workers.isNotEmpty) ...[
          const SizedBox(height: 12),
          Text('Workers', style: GoogleFonts.inter(fontSize: 12, fontWeight: FontWeight.w700, color: AgroColors.forest)),
          for (final w in workers) KvRow('${w['workerName']}', '${qty(w['hoursWorked'])} h'),
        ],
        if (inputs.isNotEmpty) ...[
          const SizedBox(height: 12),
          Text('Inputs used', style: GoogleFonts.inter(fontSize: 12, fontWeight: FontWeight.w700, color: AgroColors.forest)),
          for (final i in inputs) KvRow('${i['item']}', '${qty(i['quantity'])} ${i['unit'] ?? ''}'),
        ],
        if (_error != null)
          Padding(padding: const EdgeInsets.only(top: 8), child: Text('Showing the summary only: $_error', style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone))),
        const SizedBox(height: 8),
      ],
    );
  }
}
