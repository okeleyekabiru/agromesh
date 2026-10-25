import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/models.dart';
import '../services/api_client.dart';
import '../theme/app_theme.dart';
import 'geo_sketch.dart';

/// Shows what has been done and what is left of one kind of work on a block, in its current cycle,
/// drawn on the block's outline (green = done, amber = left to do). Needs a connection: the server
/// does the geometry.
void showBlockCoverageSheet(BuildContext context, Block block, {ActivityType? initialType}) {
  showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    backgroundColor: AgroColors.white,
    shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
    builder: (_) => _CoverageSheet(block: block, initialType: initialType),
  );
}

class _CoverageSheet extends StatefulWidget {
  final Block block;
  final ActivityType? initialType;
  const _CoverageSheet({required this.block, this.initialType});

  @override
  State<_CoverageSheet> createState() => _CoverageSheetState();
}

class _CoverageSheetState extends State<_CoverageSheet> {
  static const _types = [
    (ActivityType.weeding, 'Weeding'),
    (ActivityType.spraying, 'Spraying'),
    (ActivityType.fertilizerApplication, 'Fertilizer'),
    (ActivityType.harvesting, 'Harvest'),
  ];

  ActivityType _type = ActivityType.weeding;
  BlockCoverage? _cov;
  bool _loading = true;
  String? _error;
  bool _forcing = false;

  @override
  void initState() {
    super.initState();
    if (widget.initialType != null) _type = widget.initialType!;
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final json = await ApiClient.get(
        '/api/farms/${widget.block.farmId}/blocks/${widget.block.id}/coverage?activityType=${_type.index}',
      );
      if (!mounted) return;
      setState(() => _cov = BlockCoverage.fromApiJson(json as Map<String, dynamic>));
    } catch (e) {
      if (!mounted) return;
      setState(() { _cov = null; _error = 'Could not load the progress. Check your connection and try again.'; });
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// The marked areas can stop a few metres short of the block's edge and never reach the end: finish it by hand.
  Future<void> _forceComplete() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Mark as complete?'),
        content: Text('${(_cov?.remainingHectares ?? 0).toStringAsFixed(2)} ha is still shown as left. '
            'Treat the whole block as done and start a new cycle?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Yes, complete it')),
        ],
      ),
    );
    if (ok != true) return;
    setState(() => _forcing = true);
    try {
      await ApiClient.post(
        '/api/farms/${widget.block.farmId}/blocks/${widget.block.id}/coverage/complete?activityType=${_type.index}',
        {},
      );
      await _load();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Could not complete this cycle. Check your connection and try again.')));
    } finally {
      if (mounted) setState(() => _forcing = false);
    }
  }

  static const _palette = [Color(0xFFE4572E), Color(0xFF17BEBB), Color(0xFF9B5DE5), Color(0xFFF15BB5), Color(0xFF3A86FF), Color(0xFFFB8500), Color(0xFF2A9D8F), Color(0xFF8338EC)];
  Color _sectionColour(int n) => _palette[(n - 1) % _palette.length];

  String _date(DateTime? d) => d == null ? '' : '${d.day}/${d.month}';

  @override
  Widget build(BuildContext context) {
    final c = _cov;
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('${widget.block.name} · work progress',
                style: GoogleFonts.syne(fontSize: 16, fontWeight: FontWeight.w700, color: AgroColors.forest)),
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                children: [
                  for (final t in _types)
                    ChoiceChip(
                      label: Text(t.$2),
                      selected: _type == t.$1,
                      onSelected: (_) { setState(() => _type = t.$1); _load(); },
                    ),
                ],
              ),
              const SizedBox(height: 14),
              if (_loading)
                const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()))
              else if (_error != null)
                Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12))
              else if (c != null) ...[
                Row(
                  children: [
                    Text(c.hasBoundary ? '${c.percent.toStringAsFixed(0)}%' : '—',
                      style: GoogleFonts.jetBrainsMono(fontSize: 28, fontWeight: FontWeight.w600, color: AgroColors.forest)),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            c.status == 'Complete' ? 'Cycle complete' : c.status == 'InProgress' ? 'In progress' : 'Not started this cycle',
                            style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w700),
                          ),
                          if (c.hasBoundary)
                            Text('${c.coveredHectares.toStringAsFixed(2)} ha done · ${(c.remainingHectares ?? 0).toStringAsFixed(2)} ha left',
                              style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                          if (c.lastWorkedOn != null)
                            Text('Started ${_date(c.cycleStartedOn)} · last worked ${_date(c.lastWorkedOn)}',
                              style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                if (c.hasBoundary) ...[
                  GeoSketch(
                    block: geoJsonRings(c.blockGeoJson),
                    covered: geoJsonRings(c.coveredGeoJson),
                    remaining: geoJsonRings(c.remainingGeoJson),
                    sections: [for (final s in c.sections) geoJsonRings(s.geoJson)],
                    height: 260,
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Container(width: 12, height: 12, decoration: BoxDecoration(color: const Color(0xFF3FB56D), borderRadius: BorderRadius.circular(3))),
                      const SizedBox(width: 6),
                      Text('Done', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                      const SizedBox(width: 16),
                      Container(width: 12, height: 12, decoration: BoxDecoration(color: const Color(0xFFF5A623), borderRadius: BorderRadius.circular(3))),
                      const SizedBox(width: 6),
                      Text('Left to do', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                    ],
                  ),
                  if (c.sections.isNotEmpty) ...[
                    const SizedBox(height: 10),
                    Text('Areas marked this cycle (${c.sections.length})',
                      style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                    for (final s in c.sections)
                      Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Row(
                          children: [
                            CircleAvatar(radius: 9, backgroundColor: _sectionColour(s.number),
                              child: Text('${s.number}', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700))),
                            const SizedBox(width: 10),
                            Text(_date(s.date), style: GoogleFonts.inter(fontSize: 12.5)),
                            const Spacer(),
                            Text('${s.hectares.toStringAsFixed(2)} ha', style: GoogleFonts.jetBrainsMono(fontSize: 12, color: AgroColors.stone)),
                          ],
                        ),
                      ),
                  ],
                  if (c.status == 'InProgress') ...[
                    const SizedBox(height: 10),
                    OutlinedButton(
                      onPressed: _forcing ? null : _forceComplete,
                      child: Text(_forcing ? 'Saving…' : 'Mark as complete'),
                    ),
                    Text('Use when the work is finished but the marked areas fall a little short of the block.',
                      style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                  ],
                ] else
                  Text('This block has no boundary yet, so what is done and what is left cannot be drawn.',
                    style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
