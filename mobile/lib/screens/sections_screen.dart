import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../stores/app_store.dart';
import '../theme/app_theme.dart';
import '../widgets/widgets.dart';
import '../models/models.dart';

class SectionsScreen extends StatelessWidget {
  const SectionsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        ...store.sections.map((s) => Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: _SectionCard(section: s),
        )),
        const SizedBox(height: 80),
      ],
    );
  }
}

class _SectionCard extends StatelessWidget {
  final Section section;
  const _SectionCard({required this.section});

  Color get _statusColor => switch (section.status) {
    SectionStatus.active      => AgroColors.palm,
    SectionStatus.harvestSoon => AgroColors.harvest,
    SectionStatus.idle        => AgroColors.stone,
  };

  AgroBadgeVariant get _badgeVariant => switch (section.status) {
    SectionStatus.active      => AgroBadgeVariant.green,
    SectionStatus.harvestSoon => AgroBadgeVariant.harvest,
    SectionStatus.idle        => AgroBadgeVariant.stone,
  };

  @override
  Widget build(BuildContext context) {
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
                Expanded(child: Text(section.name, style: Theme.of(context).textTheme.displaySmall?.copyWith(fontSize: 16))),
                AgroBadge(label: section.status.label, variant: _badgeVariant),
              ],
            ),
          ),
          _Row('Crop',          section.crop.label),
          _Row('Hectares',      '${section.hectares.toStringAsFixed(0)} ha'),
          _Row('Blocks',        '${section.blockCount}'),
          _Row('Last Activity', section.lastActivityDate ?? 'Never'),
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
