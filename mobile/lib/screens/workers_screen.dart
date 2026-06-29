import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../stores/app_store.dart';
import '../theme/app_theme.dart';
import '../widgets/widgets.dart';
import '../models/models.dart';

class WorkersScreen extends StatelessWidget {
  const WorkersScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();
    final employees   = store.workers.where((w) => w.type == WorkerType.employee).toList();
    final contractors = store.workers.where((w) => w.type == WorkerType.contractor).toList();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Summary row
        Row(
          children: [
            Expanded(child: KpiCard(label: 'Employees', value: '${employees.length}', delta: 'Permanent', deltaType: KpiDeltaType.mute, dark: true)),
            const SizedBox(width: 10),
            Expanded(child: KpiCard(label: 'Contractors', value: '${contractors.length}', delta: 'Seasonal', deltaType: KpiDeltaType.mute)),
          ],
        ),
        const SizedBox(height: 16),

        AgroCard(
          child: Column(
            children: [
              const AgroCardHeader(title: 'All Workers', subtitle: 'Active'),
              ...store.workers.map((w) => _WorkerTile(worker: w)),
            ],
          ),
        ),
        const SizedBox(height: 80),
      ],
    );
  }
}

class _WorkerTile extends StatelessWidget {
  final Worker worker;
  const _WorkerTile({required this.worker});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
      child: Row(
        children: [
          Container(
            width: 36, height: 36,
            decoration: const BoxDecoration(color: AgroColors.canopy, shape: BoxShape.circle),
            child: Center(
              child: Text(worker.initials,
                style: GoogleFonts.syne(fontSize: 13, fontWeight: FontWeight.w700, color: AgroColors.palm)),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(worker.name, style: Theme.of(context).textTheme.titleMedium),
                Text(worker.role, style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone)),
              ],
            ),
          ),
          AgroBadge(
            label: worker.type.label,
            variant: worker.type == WorkerType.employee ? AgroBadgeVariant.green : AgroBadgeVariant.blue,
          ),
          const SizedBox(width: 10),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text('₦${worker.dailyRate.toStringAsFixed(0)}',
                style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
              Text('per day', style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone)),
            ],
          ),
        ],
      ),
    );
  }
}
