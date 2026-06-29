import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../stores/app_store.dart';
import '../theme/app_theme.dart';
import '../widgets/widgets.dart';
import 'dashboard/dashboard_screen.dart';
import 'activity/log_activity_screen.dart';
import 'sections_screen.dart';
import 'workers_screen.dart';

class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _currentIndex = 0;

  final _screens = const [
    DashboardScreen(),
    SectionsScreen(),
    WorkersScreen(),
  ];

  final _titles = ['Farm Overview', 'Sections', 'Workers'];

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        title: Row(
          children: [
            _AgroLogo(),
            const SizedBox(width: 10),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(_titles[_currentIndex],
                  style: const TextStyle(color: AgroColors.white, fontSize: 16, fontWeight: FontWeight.w800)),
                const Text('Kabiru Farms · Oyo State',
                  style: TextStyle(color: AgroColors.stone, fontSize: 10)),
              ],
            ),
          ],
        ),
        actions: [
          // Sync indicator
          if (store.isSyncing)
            const Padding(
              padding: EdgeInsets.only(right: 8),
              child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: AgroColors.palm, strokeWidth: 2)),
            )
          else if (!store.isOnline)
            const Padding(
              padding: EdgeInsets.only(right: 12),
              child: Icon(Icons.cloud_off_outlined, color: AgroColors.harvest, size: 20),
            )
          else
            Padding(
              padding: const EdgeInsets.only(right: 12),
              child: Row(
                children: [
                  Container(width: 7, height: 7, decoration: const BoxDecoration(color: AgroColors.data, shape: BoxShape.circle)),
                  const SizedBox(width: 5),
                  const Text('ONLINE', style: TextStyle(color: AgroColors.data, fontSize: 9, fontFamily: 'JetBrainsMono')),
                ],
              ),
            ),
        ],
      ),
      body: Column(
        children: [
          // Offline banner
          if (!store.isOnline)
            OfflineBanner(pendingCount: store.pendingSyncCount),

          // Main content
          Expanded(child: _screens[_currentIndex]),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (i) => setState(() => _currentIndex = i),
        backgroundColor: AgroColors.forest,
        selectedItemColor: AgroColors.palm,
        unselectedItemColor: AgroColors.stone,
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.grid_view_rounded), label: 'Dashboard'),
          BottomNavigationBarItem(icon: Icon(Icons.map_outlined), label: 'Sections'),
          BottomNavigationBarItem(icon: Icon(Icons.people_outline), label: 'Workers'),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => const LogActivityScreen()),
        ),
        backgroundColor: AgroColors.leaf,
        foregroundColor: AgroColors.white,
        icon: const Icon(Icons.add),
        label: const Text('Log Activity', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
      ),
      floatingActionButtonLocation: FloatingActionButtonLocation.centerFloat,
    );
  }
}

class _AgroLogo extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      width: 28, height: 28,
      decoration: BoxDecoration(color: AgroColors.canopy, borderRadius: BorderRadius.circular(7)),
      child: const Center(
        child: Text('A', style: TextStyle(color: AgroColors.palm, fontWeight: FontWeight.w800, fontSize: 14)),
      ),
    );
  }
}
