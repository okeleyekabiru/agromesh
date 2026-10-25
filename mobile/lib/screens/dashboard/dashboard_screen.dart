import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import '../../widgets/weather_card.dart';
import '../activity/log_activity_screen.dart';
import '../stock/out_on_farm_screen.dart';
import '../blocks/add_block_screen.dart';
import '../more/activities_screen.dart';
import '../../widgets/dashboard_extras.dart';

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

            // Farm setup not finished
            if (store.setupDraft) ...[
              InkWell(
                borderRadius: BorderRadius.circular(10),
                onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const AddBlockScreen())),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                  decoration: BoxDecoration(
                    color: AgroColors.harvest.withOpacity(0.08),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: AgroColors.harvest.withOpacity(0.25)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.agriculture_outlined, color: AgroColors.harvest, size: 16),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          "Your farm setup isn't finished. Add your blocks and boundaries to unlock scheduling and reporting.",
                          style: GoogleFonts.inter(fontSize: 12, color: AgroColors.ink),
                        ),
                      ),
                      Text('Continue setup', style: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w600, color: AgroColors.leaf)),
                      const Icon(Icons.chevron_right, color: AgroColors.leaf, size: 16),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 14),
            ],

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

            // Stock picked out of the store and not yet used or returned
            if (store.outstanding.isNotEmpty) ...[
              InkWell(
                borderRadius: BorderRadius.circular(10),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(builder: (_) => const OutOnFarmScreen()),
                ),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                  decoration: BoxDecoration(
                    color: AgroColors.leaf.withOpacity(0.08),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: AgroColors.leaf.withOpacity(0.25)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.inventory_2_outlined, color: AgroColors.leaf, size: 16),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          '${store.outstanding.length} pick${store.outstanding.length == 1 ? '' : 's'} of stock out on the farm',
                          style: GoogleFonts.inter(fontSize: 12, color: AgroColors.ink),
                        ),
                      ),
                      Text('Return leftover', style: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w600, color: AgroColors.leaf)),
                      const Icon(Icons.chevron_right, color: AgroColors.leaf, size: 16),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 14),
            ],

            // Weather + 7-day forecast
            if (store.weather != null && store.weather!.forecast.isNotEmpty) ...[
              WeatherCard(weather: store.weather!),
              const SizedBox(height: 14),
            ] else ...[
              AgroCard(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      const Text('🌤️', style: TextStyle(fontSize: 28)),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          store.weatherFailed
                              ? 'Weather forecast is not available right now. Check the farm location is set, then pull down to retry.'
                              : 'Loading the weather forecast…',
                          style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
                        ),
                      ),
                    ],
                  ),
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
                KpiCard(label: 'Active Blocks', value: '${store.blocks.where((s) => s.status == BlockStatus.active).length}/${store.blocks.length}', delta: 'Today', deltaType: KpiDeltaType.up, dark: true),
                KpiCard(label: 'Activities Logged', value: '${store.activities.length}', delta: 'All time', deltaType: KpiDeltaType.mute),
                KpiCard(label: 'Workers', value: '${store.workers.length}', delta: '${store.workers.where((w) => w.type == WorkerType.employee).length} employees', deltaType: KpiDeltaType.mute),
                KpiCard(label: 'Pending Sync', value: '${store.pendingSyncCount}', delta: store.isOnline ? 'Will sync' : 'Offline', deltaType: store.pendingSyncCount > 0 ? KpiDeltaType.warn : KpiDeltaType.up),
              ],
            ),
            const DashboardExtras(),
            const SizedBox(height: 16),

            // Recent activities
            AgroCard(
              child: Column(
                children: [
                  AgroCardHeader(
                    title: 'Recent Activities',
                    trailing: TextButton(
                      onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const ActivitiesScreen())),
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

            // Blocks summary
            AgroCard(
              child: Column(
                children: [
                  const AgroCardHeader(title: 'Blocks', subtitle: 'Status'),
                  ...store.blocks.map((s) => BlockTile(block: s, work: store.blockWork[s.id])),
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
