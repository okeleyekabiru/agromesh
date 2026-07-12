import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';
import '../models/models.dart';

// ── AGRO BADGE ──
class AgroBadge extends StatelessWidget {
  final String label;
  final AgroBadgeVariant variant;

  const AgroBadge({super.key, required this.label, this.variant = AgroBadgeVariant.stone});

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = switch (variant) {
      AgroBadgeVariant.green   => (AgroColors.palm.withOpacity(0.12),   AgroColors.leaf),
      AgroBadgeVariant.harvest => (AgroColors.harvest.withOpacity(0.12), const Color(0xFF8A6000)),
      AgroBadgeVariant.red     => (AgroColors.alert.withOpacity(0.1),   AgroColors.alert),
      AgroBadgeVariant.stone   => (AgroColors.fog,                       AgroColors.stone),
      AgroBadgeVariant.blue    => (const Color(0x193B82F6),              const Color(0xFF2563EB)),
      AgroBadgeVariant.data    => (AgroColors.data.withOpacity(0.1),    const Color(0xFF007A5E)),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(4)),
      child: Text(
        label.toUpperCase(),
        style: GoogleFonts.jetBrainsMono(fontSize: 9, fontWeight: FontWeight.w500, color: fg, letterSpacing: 0.5),
      ),
    );
  }
}

enum AgroBadgeVariant { green, harvest, red, stone, blue, data }

// ── AGRO CARD ──
class AgroCard extends StatelessWidget {
  final Widget child;
  final EdgeInsets? padding;
  final Color? color;

  const AgroCard({super.key, required this.child, this.padding, this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: color ?? AgroColors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AgroColors.fog),
      ),
      clipBehavior: Clip.antiAlias,
      child: padding != null ? Padding(padding: padding!, child: child) : child,
    );
  }
}

// ── AGRO CARD HEADER ──
class AgroCardHeader extends StatelessWidget {
  final String title;
  final String? subtitle;
  final Widget? trailing;

  const AgroCardHeader({super.key, required this.title, this.subtitle, this.trailing});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(title, style: Theme.of(context).textTheme.headlineSmall),
          if (subtitle != null)
            Text(subtitle!, style: Theme.of(context).textTheme.labelSmall?.copyWith(letterSpacing: 1)),
          if (trailing != null) trailing!,
        ],
      ),
    );
  }
}

// ── KPI CARD ──
class KpiCard extends StatelessWidget {
  final String label;
  final String value;
  final String delta;
  final KpiDeltaType deltaType;
  final bool dark;

  const KpiCard({
    super.key,
    required this.label,
    required this.value,
    required this.delta,
    this.deltaType = KpiDeltaType.mute,
    this.dark = false,
  });

  @override
  Widget build(BuildContext context) {
    final bg = dark ? AgroColors.forest : AgroColors.white;
    final labelColor = dark ? AgroColors.stone.withOpacity(0.8) : AgroColors.stone;
    final valueColor = dark ? AgroColors.white : AgroColors.forest;
    final deltaColor = switch (deltaType) {
      KpiDeltaType.up   => AgroColors.palm,
      KpiDeltaType.down => AgroColors.alert,
      KpiDeltaType.warn => AgroColors.harvest,
      KpiDeltaType.mute => AgroColors.stone,
    };

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: dark ? AgroColors.canopy : AgroColors.fog),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label.toUpperCase(), style: GoogleFonts.jetBrainsMono(fontSize: 8, color: labelColor, letterSpacing: 0.8)),
          const SizedBox(height: 6),
          Text(value, style: GoogleFonts.syne(fontSize: 22, fontWeight: FontWeight.w700, color: valueColor, letterSpacing: -0.5)),
          const SizedBox(height: 2),
          Text(delta, style: GoogleFonts.inter(fontSize: 10, fontWeight: FontWeight.w500, color: deltaColor)),
        ],
      ),
    );
  }
}

enum KpiDeltaType { up, down, warn, mute }

// ── SECTION LIST TILE ──
class SectionTile extends StatelessWidget {
  final Section section;
  final VoidCallback? onTap;

  const SectionTile({super.key, required this.section, this.onTap});

  Color get _statusColor {
    return switch (section.status) {
      SectionStatus.active      => AgroColors.palm,
      SectionStatus.harvestSoon => AgroColors.harvest,
      SectionStatus.idle        => AgroColors.stone,
    };
  }

  AgroBadgeVariant get _badgeVariant {
    return switch (section.status) {
      SectionStatus.active      => AgroBadgeVariant.green,
      SectionStatus.harvestSoon => AgroBadgeVariant.harvest,
      SectionStatus.idle        => AgroBadgeVariant.stone,
    };
  }

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
        child: Row(
          children: [
            Container(
              width: 8, height: 8,
              decoration: BoxDecoration(color: _statusColor, shape: BoxShape.circle),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(section.name, style: Theme.of(context).textTheme.titleMedium),
                  Text(section.crop.label, style: Theme.of(context).textTheme.labelSmall),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                AgroBadge(label: section.status.label, variant: _badgeVariant),
                const SizedBox(height: 4),
                Text('${section.hectares.toStringAsFixed(0)} ha',
                  style: GoogleFonts.jetBrainsMono(fontSize: 10, color: AgroColors.stone)),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

// ── ACTIVITY TILE ──
class ActivityTile extends StatelessWidget {
  final ActivityLog activity;

  const ActivityTile({super.key, required this.activity});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36, height: 36,
            decoration: BoxDecoration(
              color: AgroColors.palm.withOpacity(0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Center(child: Text(activity.activityType.icon, style: const TextStyle(fontSize: 16))),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(activity.activityType.label, style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 2),
                Text(
                  '${activity.sectionName} · ${activity.blockName} · ${activity.workers.length} workers',
                  style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone),
                ),
                if (activity.inputs.isNotEmpty) ...[
                  const SizedBox(height: 1),
                  Text(
                    activity.inputs.map((i) => i.toString()).join(', '),
                    style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              if (activity.totalCost > 0)
                Text(
                  '₦${activity.totalCost.toStringAsFixed(0)}',
                  style: GoogleFonts.syne(fontSize: 13, fontWeight: FontWeight.w700, color: AgroColors.forest),
                ),
              const SizedBox(height: 4),
              _SyncIndicator(status: activity.syncStatus),
            ],
          ),
        ],
      ),
    );
  }
}

class _SyncIndicator extends StatelessWidget {
  final SyncStatus status;
  const _SyncIndicator({required this.status});

  @override
  Widget build(BuildContext context) {
    final (icon, color) = switch (status) {
      SyncStatus.synced  => (Icons.cloud_done_outlined, AgroColors.data),
      SyncStatus.pending => (Icons.cloud_upload_outlined, AgroColors.harvest),
      SyncStatus.failed  => (Icons.cloud_off_outlined, AgroColors.alert),
    };
    return Icon(icon, size: 14, color: color);
  }
}

// ── WORKER CHIP ──
class WorkerChip extends StatelessWidget {
  final String label;
  final VoidCallback? onRemove;

  const WorkerChip({super.key, required this.label, this.onRemove});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: AgroColors.mist,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AgroColors.fog),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text('👤', style: TextStyle(fontSize: 11)),
          const SizedBox(width: 5),
          Text(label, style: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w500, color: AgroColors.forest)),
          if (onRemove != null) ...[
            const SizedBox(width: 4),
            GestureDetector(
              onTap: onRemove,
              child: const Icon(Icons.close, size: 13, color: AgroColors.stone),
            ),
          ],
        ],
      ),
    );
  }
}

// ── OFFLINE BANNER ──
class OfflineBanner extends StatelessWidget {
  final int pendingCount;

  const OfflineBanner({super.key, required this.pendingCount});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      color: AgroColors.harvest.withOpacity(0.1),
      child: Row(
        children: [
          const Icon(Icons.cloud_off_outlined, size: 14, color: AgroColors.harvest),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              pendingCount > 0
                ? 'Offline · $pendingCount activities queued for sync'
                : 'Offline · Activities saved locally',
              style: GoogleFonts.inter(fontSize: 12, color: AgroColors.ink),
            ),
          ),
        ],
      ),
    );
  }
}

// ── SECTION HEADER LABEL ──
class MonoLabel extends StatelessWidget {
  final String text;
  final Color? color;

  const MonoLabel(this.text, {super.key, this.color});

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      style: GoogleFonts.jetBrainsMono(
        fontSize: 9, fontWeight: FontWeight.w500,
        color: color ?? AgroColors.stone, letterSpacing: 1,
      ),
    );
  }
}

// ── AGRO TEXT FIELD ──
class AgroTextField extends StatelessWidget {
  final String label;
  final String? hint;
  final TextEditingController? controller;
  final TextInputType? keyboardType;
  final bool readOnly;
  final bool obscureText;
  final Widget? suffixIcon;
  final VoidCallback? onTap;
  final String? Function(String?)? validator;
  final void Function(String)? onChanged;

  const AgroTextField({
    super.key,
    required this.label,
    this.hint,
    this.controller,
    this.keyboardType,
    this.readOnly = false,
    this.obscureText = false,
    this.suffixIcon,
    this.onTap,
    this.validator,
    this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        MonoLabel(label),
        const SizedBox(height: 6),
        TextFormField(
          controller: controller,
          keyboardType: keyboardType,
          readOnly: readOnly,
          obscureText: obscureText,
          onTap: onTap,
          validator: validator,
          onChanged: onChanged,
          style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
          decoration: InputDecoration(hintText: hint, suffixIcon: suffixIcon),
        ),
      ],
    );
  }
}
