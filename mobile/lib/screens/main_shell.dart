import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../stores/app_store.dart';
import '../stores/auth_store.dart';
import '../theme/app_theme.dart';
import '../widgets/widgets.dart';
import 'dashboard/dashboard_screen.dart';
import 'activity/log_activity_screen.dart';
import 'blocks_screen.dart';
import 'workers_screen.dart';

class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _currentIndex = 0;
  static const _blocksTab = 1; // index of Blocks in the bottom bar

  final _screens = const [
    DashboardScreen(),
    BlocksScreen(),
    WorkersScreen(),
  ];

  final _titles = ['Farm Overview', 'Blocks', 'Workers'];

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
                Text(
                  store.farmName.isEmpty
                    ? 'AgroMesh'
                    : (store.farmLocation.isEmpty ? store.farmName : '${store.farmName} · ${store.farmLocation}'),
                  style: const TextStyle(color: AgroColors.stone, fontSize: 10),
                  overflow: TextOverflow.ellipsis,
                ),
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
          const _AccountMenu(),
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
          BottomNavigationBarItem(icon: Icon(Icons.map_outlined), label: 'Blocks'),
          BottomNavigationBarItem(icon: Icon(Icons.people_outline), label: 'Workers'),
        ],
      ),
      // The Blocks tab has its own button (Add Block), so Log Activity is not offered there.
      floatingActionButton: _currentIndex == _blocksTab
          ? null
          : FloatingActionButton.extended(
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

/// Account affordance in the AppBar — surfaces the logged-in user, lets them
/// switch farms (if they have more than one), and log out.
class _AccountMenu extends StatelessWidget {
  const _AccountMenu();

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthStore>();

    return PopupMenuButton<String>(
      icon: const Icon(Icons.account_circle_outlined, color: AgroColors.white),
      onSelected: (value) {
        final authStore = context.read<AuthStore>();
        if (value == 'logout') {
          authStore.logout();
        } else if (value == 'switch_farm') {
          authStore.switchFarm();
        }
      },
      itemBuilder: (context) => [
        PopupMenuItem<String>(
          enabled: false,
          child: Text(
            auth.user?.email ?? '',
            style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AgroColors.stone),
          ),
        ),
        if (auth.farms.length > 1)
          const PopupMenuItem<String>(value: 'switch_farm', child: Text('Switch farm')),
        const PopupMenuItem<String>(value: 'logout', child: Text('Log out')),
      ],
    );
  }
}
