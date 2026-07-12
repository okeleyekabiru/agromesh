import 'package:flutter/foundation.dart';
import '../models/models.dart';
import '../services/auth_service.dart';

enum AuthStatus { bootstrapping, loggedOut, needsFarmSelection, ready }

/// Session/company/farm state. Mobile only ever logs in — accounts, companies
/// and farms are created on the web app — so this store's job is: authenticate,
/// remember the session, and resolve which farm is "active" (auto-picked if
/// there's exactly one, otherwise the user chooses).
class AuthStore extends ChangeNotifier {
  AuthStatus status = AuthStatus.bootstrapping;
  AppUser? user;
  String? companyId;
  List<Farm> farms = [];
  Farm? activeFarm;
  String? error;
  bool isSubmitting = false;

  Future<void> bootstrap() async {
    final session = await AuthService.loadSession();
    if (session == null) {
      status = AuthStatus.loggedOut;
      notifyListeners();
      return;
    }

    user = session.user;
    companyId = session.companyId;
    farms = session.farms;
    await _resolveActiveFarm(persistedFarmId: session.activeFarmId);
    notifyListeners();
  }

  Future<bool> login(String email, String password) async {
    isSubmitting = true;
    error = null;
    notifyListeners();

    try {
      final session = await AuthService.login(email, password);
      user = session.user;
      companyId = session.companyId;
      farms = session.farms;
      await _resolveActiveFarm(persistedFarmId: null);
      return true;
    } catch (e) {
      error = _friendlyError(e);
      status = AuthStatus.loggedOut;
      return false;
    } finally {
      isSubmitting = false;
      notifyListeners();
    }
  }

  Future<void> _resolveActiveFarm({required String? persistedFarmId}) async {
    if (farms.isEmpty) {
      activeFarm = null;
      status = AuthStatus.ready; // logged in; MainShell/AppGate handles the empty state
      return;
    }
    if (persistedFarmId != null) {
      activeFarm = farms.firstWhere((f) => f.id == persistedFarmId, orElse: () => farms.first);
      status = AuthStatus.ready;
      return;
    }
    if (farms.length == 1) {
      activeFarm = farms.first;
      await AuthService.setActiveFarmId(activeFarm!.id);
      status = AuthStatus.ready;
      return;
    }
    status = AuthStatus.needsFarmSelection;
  }

  Future<void> selectFarm(Farm farm) async {
    activeFarm = farm;
    await AuthService.setActiveFarmId(farm.id);
    status = AuthStatus.ready;
    notifyListeners();
  }

  /// Lets the user go back to the farm picker (e.g. from the account menu) —
  /// only relevant when they have more than one farm.
  void switchFarm() {
    if (farms.length <= 1) return;
    status = AuthStatus.needsFarmSelection;
    notifyListeners();
  }

  Future<void> logout() async {
    await AuthService.logout();
    user = null;
    companyId = null;
    farms = [];
    activeFarm = null;
    error = null;
    status = AuthStatus.loggedOut;
    notifyListeners();
  }

  String _friendlyError(Object e) {
    final s = e.toString();
    if (s.contains('401') || s.contains('400') || s.contains('404')) {
      return 'Invalid email or password.';
    }
    if (s.contains('SocketException') || s.contains('TimeoutException') || s.contains('ClientException')) {
      return 'Could not reach the server. Check your connection and try again.';
    }
    return 'Login failed. Please try again.';
  }
}
