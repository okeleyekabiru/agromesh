import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import '../activity/log_activity_screen.dart';

class DashboardScreen extends StatelessWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();

    return RefreshIndicator(
      color: AgroColors.palm,
      onRefresh: store.syncNow,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [

            // Pending review banner
            if (store.pendingSyncCount > 0) ...[
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                decoration: BoxDecoration(
                  color: AgroColors.harvest.withOpacity(0.08),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AgroColors.harvest.withOpacity(0.25)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.upload_outlined, color: AgroColors.harvest, size: 16),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        '${store.pendingSyncCount} activities queued for sync',
                        style: GoogleFonts.inter(fontSize: 12, color: AgroColors.ink),
                      ),
                    ),
                    GestureDetector(
                      onTap: store.syncNow,
                      child: Text('Sync now', style: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w600, color: AgroColors.leaf)),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 14),
            ],

            // KPI grid
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisSpacing: 10,
              mainAxisSpacing: 10,
              childAspectRatio: 1.5,
              children: [
                KpiCard(label: 'Active Sections', value: '${store.sections.where((s) => s.status == SectionStatus.active).length}/${store.sections.length}', delta: 'Today', deltaType: KpiDeltaType.up, dark: true),
                KpiCard(label: 'Activities Logged', value: '${store.activities.length}', delta: 'All time', deltaType: KpiDeltaType.mute),
                KpiCard(label: 'Workers', value: '${store.workers.length}', delta: '${store.workers.where((w) => w.type == WorkerType.employee).length} employees', deltaType: KpiDeltaType.mute),
                KpiCard(label: 'Pending Sync', value: '${store.pendingSyncCount}', delta: store.isOnline ? 'Will sync' : 'Offline', deltaType: store.pendingSyncCount > 0 ? KpiDeltaType.warn : KpiDeltaType.up),
              ],
            ),
            const SizedBox(height: 16),

            // Recent activities
            AgroCard(
              child: Column(
                children: [
                  AgroCardHeader(
                    title: 'Recent Activities',
                    trailing: TextButton(
                      onPressed: () {},
                      child: Text('View all', style: GoogleFonts.inter(fontSize: 11, color: AgroColors.leaf, fontWeight: FontWeight.w600)),
                    ),
                  ),
                  if (store.activities.isEmpty)
                    Padding(
                      padding: const EdgeInsets.all(32),
                      child: Column(
                        children: [
                          const Icon(Icons.assignment_outlined, size: 40, color: AgroColors.fog),
                          const SizedBox(height: 12),
                          Text('No activities yet', style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                          const SizedBox(height: 4),
                          Text('Tap Log Activity to record today\'s farm work',
                            style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone), textAlign: TextAlign.center),
                          const SizedBox(height: 16),
                          SizedBox(
                            width: 180,
                            child: ElevatedButton.icon(
                              onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const LogActivityScreen())),
                              icon: const Icon(Icons.add, size: 16),
                              label: const Text('Log Activity'),
                            ),
                          ),
                        ],
                      ),
                    )
                  else
                    ...store.activities.take(5).map((a) => ActivityTile(activity: a)),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // Sections summary
            AgroCard(
              child: Column(
                children: [
                  const AgroCardHeader(title: 'Sections', subtitle: 'Status'),
                  ...store.sections.map((s) => SectionTile(section: s)),
                ],
              ),
            ),

            // Bottom padding for FAB
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }
}
