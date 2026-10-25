import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../screens/more/common.dart';
import '../services/api_client.dart';
import '../stores/app_store.dart';
import 'package:provider/provider.dart';
import '../theme/app_theme.dart';
import 'widgets.dart';

const _activityNames = ['Weeding', 'Spraying', 'Fertilizer', 'Maintenance', 'Harvesting', 'Clearing', 'Pruning', 'Pest control'];
const _monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

String _activityName(dynamic v) {
  if (v is num) return _activityNames[v.toInt().clamp(0, _activityNames.length - 1)];
  return '$v';
}

/// Dashboard cards fed by GET /api/farms/{id}/dashboard: low stock, upcoming tasks, block costs, crop P&L,
/// spend trend. Shows nothing while loading or when the request fails.
class DashboardExtras extends StatefulWidget {
  const DashboardExtras({super.key});

  @override
  State<DashboardExtras> createState() => _DashboardExtrasState();
}

class _DashboardExtrasState extends State<DashboardExtras> {
  Map<String, dynamic>? _data;
  String _farm = '';

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final id = context.read<AppStore>().farmId;
    if (id.isNotEmpty && id != _farm) {
      _farm = id;
      _load(id);
    }
  }

  Future<void> _load(String id) async {
    try {
      final json = await ApiClient.get('/api/farms/$id/dashboard');
      if (mounted && json is Map) setState(() => _data = json.cast<String, dynamic>());
    } catch (_) {/* optional cards: stay hidden */}
  }

  @override
  Widget build(BuildContext context) {
    final d = _data;
    if (d == null) return const SizedBox.shrink();
    final manager = isManager(context);
    final low = asList(d['lowStockAlerts']);
    final tasks = asList(d['upcomingTasks']);
    final blocks = asList(d['blocksMtd']);
    final pnl = asList(d['cropPnl']);
    final trend = asList(d['spendTrend']);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(height: 16),
        if (low.isNotEmpty) ...[
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AgroColors.alert.withOpacity(0.07),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: AgroColors.alert.withOpacity(0.25)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${low.length} item${low.length == 1 ? '' : 's'} below reorder level',
                    style: GoogleFonts.inter(fontSize: 12, fontWeight: FontWeight.w700, color: AgroColors.alert)),
                const SizedBox(height: 4),
                for (final a in low)
                  Text('${a['inventoryItemName']}: ${num1(a['currentStock'] as num?)} ${a['unit']} left (reorder at ${num1(a['reorderThreshold'] as num?)})',
                      style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.ink)),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
        if (tasks.isNotEmpty) ...[
          AgroCard(
            child: Column(
              children: [
                const AgroCardHeader(title: 'Upcoming Tasks', subtitle: 'Suggested by the weather and AI schedule'),
                for (final t in tasks.take(8)) _TaskTile(task: t),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
        if (blocks.isNotEmpty) ...[
          AgroCard(
            child: Column(
              children: [
                const AgroCardHeader(title: 'Block Costs', subtitle: 'This month'),
                for (final b in blocks)
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 9),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('${b['blockName']}', style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600)),
                              Text('${b['activityCount'] ?? 0} activities',
                                  style: GoogleFonts.inter(fontSize: 10.5, color: AgroColors.stone)),
                            ],
                          ),
                        ),
                        Text(naira(b['cost'] as num?), style: GoogleFonts.jetBrainsMono(fontSize: 12, fontWeight: FontWeight.w600)),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
        if (manager && pnl.isNotEmpty) ...[
          AgroCard(
            child: Column(
              children: [
                const AgroCardHeader(title: 'Crop P&L', subtitle: 'This month'),
                for (final c in pnl)
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 9),
                    child: Row(
                      children: [
                        Expanded(child: Text('${c['cropType']}', style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600))),
                        Text('${naira(c['revenue'] as num?)} in · ${naira(c['cost'] as num?)} out',
                            style: GoogleFonts.inter(fontSize: 10.5, color: AgroColors.stone)),
                        const SizedBox(width: 10),
                        Text(naira(c['netProfit'] as num?),
                            style: GoogleFonts.jetBrainsMono(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: ((c['netProfit'] as num?) ?? 0) < 0 ? AgroColors.alert : AgroColors.leaf,
                            )),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],
        if (manager && trend.length > 1) _TrendCard(points: trend),
      ],
    );
  }
}

class _TaskTile extends StatelessWidget {
  final Map<String, dynamic> task;
  const _TaskTile({required this.task});

  @override
  Widget build(BuildContext context) {
    final days = (task['daysUntilDue'] as num?)?.toInt() ?? 0;
    final overdue = days < 0;
    final due = overdue ? '${-days} day${days == -1 ? '' : 's'} overdue' : (days == 0 ? 'Due today' : 'In $days day${days == 1 ? '' : 's'}');
    final chem = task['recommendedChemical'];
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text('${_activityName(task['activityType'])} · ${task['blockName']}',
                    style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600)),
              ),
              AgroBadge(label: due, variant: overdue ? AgroBadgeVariant.red : (days <= 3 ? AgroBadgeVariant.harvest : AgroBadgeVariant.green)),
            ],
          ),
          if ('${task['reasoning'] ?? ''}'.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 3),
              child: Text('${task['reasoning']}',
                  maxLines: 3, overflow: TextOverflow.ellipsis, style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone, height: 1.35)),
            ),
          if (chem != null && '$chem'.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 3),
              child: Text('Suggested chemical: $chem', style: GoogleFonts.inter(fontSize: 11, color: AgroColors.leaf, fontWeight: FontWeight.w600)),
            ),
        ],
      ),
    );
  }
}

class _TrendCard extends StatelessWidget {
  final List<Map<String, dynamic>> points;
  const _TrendCard({required this.points});

  @override
  Widget build(BuildContext context) {
    final max = points.map((p) => (p['amount'] as num?)?.toDouble() ?? 0).fold<double>(0, (a, b) => a > b ? a : b);
    return AgroCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const AgroCardHeader(title: 'Spend Trend', subtitle: 'Last months'),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 14),
            child: SizedBox(
              height: 110,
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  for (final p in points)
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 3),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            Container(
                              height: max == 0 ? 2 : 80 * (((p['amount'] as num?)?.toDouble() ?? 0) / max) + 2,
                              decoration: BoxDecoration(color: AgroColors.palm, borderRadius: BorderRadius.circular(3)),
                            ),
                            const SizedBox(height: 4),
                            Text(_monthNames[(((p['month'] as num?)?.toInt() ?? 1) - 1).clamp(0, 11)],
                                style: GoogleFonts.jetBrainsMono(fontSize: 8.5, color: AgroColors.stone)),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
