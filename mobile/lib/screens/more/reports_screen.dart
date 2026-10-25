import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';

const _monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const _short = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

String _activityLabel(String t) {
  switch (t) {
    case 'FertilizerApplication':
      return 'Fertilizer Application';
    case 'PestControl':
      return 'Pest Control';
    default:
      return t;
  }
}

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  late int _year;
  late int _month;
  late final int _nowYear;
  late final int _nowMonth;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _year = _nowYear = now.year;
    _month = _nowMonth = now.month;
  }

  bool get _isCurrent => _year == _nowYear && _month == _nowMonth;

  void _shift(int delta) {
    var m = _month + delta;
    var y = _year;
    if (m < 1) { m = 12; y--; }
    if (m > 12) { m = 1; y++; }
    setState(() { _year = y; _month = m; });
  }

  @override
  Widget build(BuildContext context) {
    final farm = farmIdFrom(context);
    return LoadScaffold<Map<String, dynamic>>(
      key: ValueKey('rep-$_year-$_month'),
      title: 'Reports',
      load: () async {
        final r = await ApiClient.get('/api/farms/$farm/reports?year=$_year&month=$_month');
        return r is Map ? r.cast<String, dynamic>() : <String, dynamic>{};
      },
      builder: (ctx, r, reload) {
        final unit = (r['areaUnit'] as String?) ?? 'ha';
        final total = (r['totalExpenditure'] as num?) ?? 0;
        final categories = asList(r['categories']);
        final blocks = asList(r['blocks']);
        final byActivity = asList(r['byActivityType']);
        final trend = asList(r['spendTrend']);
        final maxTrend = trend.fold<num>(0, (m, e) => ((e['amount'] as num?) ?? 0) > m ? (e['amount'] as num) : m);

        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                IconButton(onPressed: () => _shift(-1), icon: const Icon(Icons.chevron_left)),
                Text('${_monthNames[_month - 1]} $_year',
                    style: GoogleFonts.syne(fontSize: 15, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                IconButton(onPressed: _isCurrent ? null : () => _shift(1), icon: const Icon(Icons.chevron_right)),
              ],
            ),
            Row(children: [
              Expanded(child: KpiCard(label: 'Total spend', value: naira(total), delta: '${r['activityCount'] ?? 0} activities', deltaType: KpiDeltaType.mute, dark: true)),
              const SizedBox(width: 10),
              Expanded(child: KpiCard(label: 'Labour', value: naira(r['labourCost'] as num?), delta: 'Wages', deltaType: KpiDeltaType.mute)),
            ]),
            const SizedBox(height: 10),
            Row(children: [
              Expanded(child: KpiCard(label: 'Inputs', value: naira(r['inputCost'] as num?), delta: 'Stock used', deltaType: KpiDeltaType.mute)),
              const SizedBox(width: 10),
              Expanded(child: KpiCard(label: 'Bank spend', value: naira(r['bankSpend'] as num?), delta: 'From transactions', deltaType: KpiDeltaType.mute)),
            ]),
            const SizedBox(height: 14),
            AgroCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const AgroCardHeader(title: '6-Month Spend Trend'),
                  if (trend.isEmpty)
                    emptyNote('No spending recorded yet.')
                  else
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
                      child: SizedBox(
                        height: 130,
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            for (final t in trend)
                              Expanded(
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(horizontal: 4),
                                  child: Column(
                                    mainAxisAlignment: MainAxisAlignment.end,
                                    children: [
                                      Text(naira(t['amount'] as num?), style: GoogleFonts.jetBrainsMono(fontSize: 8, color: AgroColors.stone)),
                                      const SizedBox(height: 3),
                                      Container(
                                        height: maxTrend <= 0 ? 2 : (((t['amount'] as num?) ?? 0) / maxTrend * 80).clamp(2, 80).toDouble(),
                                        decoration: BoxDecoration(
                                          color: (t['year'] == _year && t['month'] == _month) ? AgroColors.palm : AgroColors.leaf.withOpacity(0.45),
                                          borderRadius: BorderRadius.circular(3),
                                        ),
                                      ),
                                      const SizedBox(height: 4),
                                      Text(_short[(((t['month'] as num?) ?? 1).toInt() - 1).clamp(0, 11)],
                                          style: GoogleFonts.inter(fontSize: 10, color: AgroColors.stone)),
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
            ),
            const SizedBox(height: 14),
            AgroCard(
              child: Column(
                children: [
                  const AgroCardHeader(title: 'Cost by Activity'),
                  if (byActivity.isEmpty) emptyNote('No activities this month.'),
                  for (final a in byActivity)
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      child: Row(children: [
                        Expanded(child: Text('${_activityLabel('${a['activityType']}')} · ${a['count']}', style: GoogleFonts.inter(fontSize: 12.5, color: AgroColors.ink))),
                        Text(naira(a['cost'] as num?), style: GoogleFonts.jetBrainsMono(fontSize: 12, fontWeight: FontWeight.w600, color: AgroColors.forest)),
                      ]),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            AgroCard(
              child: Column(
                children: [
                  const AgroCardHeader(title: 'Cost by Category'),
                  if (categories.isEmpty) emptyNote('No categorised spend this month.'),
                  for (final c in categories)
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(children: [
                            Expanded(child: Text('${c['label']}', style: GoogleFonts.inter(fontSize: 12.5, color: AgroColors.ink))),
                            Text('${naira(c['amount'] as num?)} · ${num1(c['percent'] as num?, 0)}%',
                                style: GoogleFonts.jetBrainsMono(fontSize: 11, color: AgroColors.stone)),
                          ]),
                          const SizedBox(height: 4),
                          ClipRRect(
                            borderRadius: BorderRadius.circular(3),
                            child: LinearProgressIndicator(
                              value: (((c['percent'] as num?) ?? 0) / 100).clamp(0, 1).toDouble(),
                              minHeight: 6,
                              backgroundColor: AgroColors.fog,
                              color: AgroColors.palm,
                            ),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            AgroCard(
              child: Column(
                children: [
                  AgroCardHeader(title: 'Cost per $unit', subtitle: 'By block'),
                  if (blocks.isEmpty) emptyNote('No blocks yet.'),
                  for (final b in blocks)
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      child: Row(children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('${b['blockName']}', style: GoogleFonts.inter(fontSize: 12.5, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                              Text('${num1(b['area'] as num?, 2)} $unit · ${b['activityCount'] ?? 0} activities',
                                  style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                            ],
                          ),
                        ),
                        Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                          Text(naira(b['cost'] as num?), style: GoogleFonts.jetBrainsMono(fontSize: 12, fontWeight: FontWeight.w600, color: AgroColors.forest)),
                          Text(b['costPerArea'] == null ? '—' : '${naira(b['costPerArea'] as num?)}/$unit',
                              style: GoogleFonts.jetBrainsMono(fontSize: 10, color: AgroColors.stone)),
                        ]),
                      ]),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 40),
          ],
        );
      },
    );
  }
}
