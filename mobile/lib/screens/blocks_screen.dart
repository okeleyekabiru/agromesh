import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../stores/app_store.dart';
import '../theme/app_theme.dart';
import '../widgets/widgets.dart';
import '../models/models.dart';
import 'blocks/add_block_screen.dart';
import 'blocks/boundary_capture_screen.dart';
import 'more/satellite_screen.dart';
import '../services/api_client.dart';
import '../widgets/block_coverage_sheet.dart';

class BlocksScreen extends StatefulWidget {
  const BlocksScreen({super.key});

  @override
  State<BlocksScreen> createState() => _BlocksScreenState();
}

class _BlocksScreenState extends State<BlocksScreen> {
  bool _requestedInitialLoad = false;
  List<Map<String, dynamic>> _progress = [];

  Future<void> _loadProgress() async {
    final store = context.read<AppStore>();
    try {
      final j = await ApiClient.get('/api/farms/${store.farmId}/blocks/coverage-progress');
      final list = (j is List ? j : const []).whereType<Map>().map((m) => m.cast<String, dynamic>()).toList();
      if (mounted) setState(() => _progress = list);
    } catch (_) {
      // Offline or not allowed: the strip just stays hidden.
    }
  }

  void _openProgress(Map<String, dynamic> p) {
    final store = context.read<AppStore>();
    final matches = store.blocks.where((b) => b.id == p['blockId']);
    if (matches.isEmpty) return;
    final t = p['activityType'];
    final idx = t is int ? t : 0;
    showBlockCoverageSheet(context, matches.first,
        initialType: idx >= 0 && idx < ActivityType.values.length ? ActivityType.values[idx] : null);
  }

  String _typeLabel(dynamic t) => switch (t) { 0 => 'Weeding', 1 => 'Spraying', 2 => 'Fertilizer', 4 => 'Harvest', _ => 'Work' };

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_requestedInitialLoad) {
      _requestedInitialLoad = true;
      final store = context.read<AppStore>();
      Future.microtask(() async { await store.refreshBlocks(); await _loadProgress(); });
    }
  }

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();

    return Scaffold(
      backgroundColor: AgroColors.mist,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: store.isOnline
          ? () => Navigator.push(context, MaterialPageRoute(builder: (_) => const AddBlockScreen()))
          : null,
        backgroundColor: store.isOnline ? AgroColors.leaf : AgroColors.stone,
        foregroundColor: AgroColors.white,
        icon: const Icon(Icons.add),
        label: const Text('Add Block', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
      ),
      body: RefreshIndicator(
        color: AgroColors.palm,
        onRefresh: () async { await store.refreshBlocks(); await _loadProgress(); },
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            if (store.blocksError != null)
              Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(color: AgroColors.harvest.withOpacity(0.1), borderRadius: BorderRadius.circular(8)),
                child: Row(
                  children: [
                    const Icon(Icons.info_outline, size: 14, color: AgroColors.harvest),
                    const SizedBox(width: 8),
                    Expanded(child: Text(store.blocksError!, style: const TextStyle(fontSize: 12, color: AgroColors.ink))),
                  ],
                ),
              ),
            if (_progress.isNotEmpty) ...[
              Text('Blocks in progress (${_progress.length})',
                  style: GoogleFonts.syne(fontSize: 13, fontWeight: FontWeight.w700, color: AgroColors.forest)),
              const SizedBox(height: 8),
              for (final p in _progress)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: InkWell(
                    borderRadius: BorderRadius.circular(12),
                    onTap: () => _openProgress(p),
                    child: Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AgroColors.white,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: AgroColors.fog),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(children: [
                            Expanded(child: Text('${p['blockName']}', style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w700))),
                            Text(_typeLabel(p['activityType']), style: GoogleFonts.inter(fontSize: 11, color: AgroColors.leaf, fontWeight: FontWeight.w600)),
                          ]),
                          const SizedBox(height: 6),
                          LinearProgressIndicator(
                            value: (((p['percent'] as num?) ?? 0) / 100).clamp(0, 1).toDouble(),
                            color: AgroColors.palm,
                            backgroundColor: AgroColors.fog,
                            minHeight: 6,
                          ),
                          const SizedBox(height: 4),
                          Text(
                            '${p['percent']}% · ${((p['coveredHectares'] as num?) ?? 0).toStringAsFixed(2)} of ${((p['blockHectares'] as num?) ?? 0).toStringAsFixed(2)} ha',
                            style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              const SizedBox(height: 8),
            ],
            InkWell(
              borderRadius: BorderRadius.circular(10),
              onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const SatelliteScreen())),
              child: Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                decoration: BoxDecoration(color: AgroColors.white, borderRadius: BorderRadius.circular(10), border: Border.all(color: AgroColors.fog)),
                child: Row(children: [
                  const Text('🛰️', style: TextStyle(fontSize: 16)),
                  const SizedBox(width: 8),
                  Expanded(child: Text('Satellite crop monitoring', style: GoogleFonts.inter(fontSize: 12.5, color: AgroColors.ink))),
                  const Icon(Icons.chevron_right, size: 18, color: AgroColors.stone),
                ]),
              ),
            ),
            if (store.blocks.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 48),
                child: Column(
                  children: [
                    const Icon(Icons.grass_outlined, size: 40, color: AgroColors.fog),
                    const SizedBox(height: 12),
                    Text('No blocks yet', style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                    const SizedBox(height: 4),
                    Text('Tap Add Block to start tracking one here.',
                      style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone), textAlign: TextAlign.center),
                  ],
                ),
              )
            else
              ...store.blocks.map((s) => Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: _BlockCard(block: s),
              )),
          ],
        ),
      ),
    );
  }
}

class _BlockCard extends StatelessWidget {
  final Block block;
  const _BlockCard({required this.block});

  Color get _statusColor => switch (block.status) {
    BlockStatus.active      => AgroColors.palm,
    BlockStatus.harvestSoon => AgroColors.harvest,
    BlockStatus.idle        => AgroColors.stone,
  };

  AgroBadgeVariant get _badgeVariant => switch (block.status) {
    BlockStatus.active      => AgroBadgeVariant.green,
    BlockStatus.harvestSoon => AgroBadgeVariant.harvest,
    BlockStatus.idle        => AgroBadgeVariant.stone,
  };

  @override
  Widget build(BuildContext context) {
    final cropLabel = block.crops.isEmpty
      ? '—'
      : block.crops.map((c) => c.cropType.label).toSet().join(', ');

    return AgroCard(
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
            child: Row(
              children: [
                Container(width: 10, height: 10, decoration: BoxDecoration(color: _statusColor, shape: BoxShape.circle)),
                const SizedBox(width: 10),
                Expanded(child: Text(block.name, style: Theme.of(context).textTheme.displaySmall?.copyWith(fontSize: 16))),
                AgroBadge(label: block.status.label, variant: _badgeVariant),
              ],
            ),
          ),
          _Row('Crops',          cropLabel),
          _Row('Hectares',      '${block.hectares.toStringAsFixed(1)} ha'),
          if (block.treeCount != null) _Row('Trees', '${block.treeCount}'),
          _Row('Last Activity', block.lastActivityDate ?? 'Never'),
          _Row('Boundary',      block.hasBoundary ? 'Captured' : 'Not captured yet'),
          Padding(
            padding: const EdgeInsets.all(12),
            child: SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => BoundaryCaptureScreen(blockId: block.id, blockName: block.name),
                  ),
                ),
                icon: const Icon(Icons.add_location_alt_outlined, size: 16),
                label: Text(block.hasBoundary ? 'Re-capture Boundary' : 'Mark Boundary'),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  final String label;
  final String value;
  const _Row(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label.toUpperCase(), style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone, letterSpacing: 0.8)),
          Text(value, style: GoogleFonts.jetBrainsMono(fontSize: 10, fontWeight: FontWeight.w500, color: AgroColors.forest)),
        ],
      ),
    );
  }
}
