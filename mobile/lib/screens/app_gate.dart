import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../stores/auth_store.dart';
import '../stores/app_store.dart';
import '../theme/app_theme.dart';
import 'auth/login_screen.dart';
import 'auth/select_farm_screen.dart';
import 'main_shell.dart';

/// Root-level gate: shows the login screen until a session exists, the farm
/// picker if the account has more than one farm, and otherwise kicks off
/// [AppStore.init] for the resolved active farm before showing [MainShell].
class AppGate extends StatefulWidget {
  const AppGate({super.key});

  @override
  State<AppGate> createState() => _AppGateState();
}

class _AppGateState extends State<AppGate> {
  String? _initializedFarmId;

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthStore>();

    switch (auth.status) {
      case AuthStatus.bootstrapping:
        return const _Splash();

      case AuthStatus.loggedOut:
        _initializedFarmId = null;
        return const LoginScreen();

      case AuthStatus.needsFarmSelection:
        return const SelectFarmScreen();

      case AuthStatus.ready:
        final farm = auth.activeFarm;
        if (farm == null) {
          return const _NoFarmsScreen();
        }
        if (_initializedFarmId != farm.id) {
          _initializedFarmId = farm.id;
          // Fire-and-forget: AppStore reports its own loading state, so we
          // don't need to block this build on the future completing. Grab the
          // store reference now (context is still valid here, synchronously)
          // rather than inside the microtask closure.
          final appStore = context.read<AppStore>();
          Future.microtask(() => appStore.init(
            farm.id,
            farmName: farm.name,
            farmLocation: farm.location,
          ));
        }
        return const MainShell();
    }
  }
}

class _Splash extends StatelessWidget {
  const _Splash();

  @override
  Widget build(BuildContext context) => const Scaffold(
    backgroundColor: AgroColors.forest,
    body: Center(child: CircularProgressIndicator(color: AgroColors.palm)),
  );
}

class _NoFarmsScreen extends StatelessWidget {
  const _NoFarmsScreen();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AgroColors.mist,
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.agriculture_outlined, size: 48, color: AgroColors.stone),
              const SizedBox(height: 16),
              const Text(
                'No farms found on your account.\nSet up a farm on the AgroMesh web app first.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 20),
              OutlinedButton(
                onPressed: () => context.read<AuthStore>().logout(),
                child: const Text('Log out'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
