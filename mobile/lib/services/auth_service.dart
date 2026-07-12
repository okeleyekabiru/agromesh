import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/models.dart';
import 'api_client.dart';

/// A logged-in session as loaded from (or written to) secure storage.
class AuthSession {
  final String token;
  final AppUser user;
  final String? companyId;
  final List<Farm> farms;
  final String? activeFarmId;

  const AuthSession({
    required this.token,
    required this.user,
    required this.companyId,
    required this.farms,
    this.activeFarmId,
  });
}

/// Handles login against the real backend and persists the session (bearer
/// token + user + company + farms + chosen active farm) in secure storage.
/// Mobile does login only — there is no registration/company/farm creation
/// here; those are web-only. Static-methods style mirrors [DatabaseService].
class AuthService {
  AuthService._();

  static const _storage = FlutterSecureStorage();

  static const _kToken = 'agromesh_token';
  static const _kUser = 'agromesh_user';
  static const _kCompanyId = 'agromesh_company_id';
  static const _kFarms = 'agromesh_farms';
  static const _kActiveFarmId = 'agromesh_active_farm_id';

  static Future<AuthSession> login(String email, String password) async {
    final json = await ApiClient.post(
      '/api/auth/login',
      {'email': email, 'password': password},
      auth: false,
    ) as Map<String, dynamic>;

    final token = json['token'] as String;
    final user = AppUser.fromJson(json['user'] as Map<String, dynamic>);
    final companyId = json['companyId'] as String?;
    final farms = ((json['farms'] as List?) ?? const [])
        .map((f) => Farm.fromLoginSummary(f as Map<String, dynamic>))
        .toList();

    await _storage.write(key: _kToken, value: token);
    await _storage.write(key: _kUser, value: jsonEncode(user.toJson()));
    await _storage.write(key: _kCompanyId, value: companyId ?? '');
    await _storage.write(key: _kFarms, value: jsonEncode(farms.map((f) => f.toJson()).toList()));
    await _storage.delete(key: _kActiveFarmId);

    return AuthSession(token: token, user: user, companyId: companyId, farms: farms);
  }

  /// Loads a previously-persisted session, if any. Returns null (rather than
  /// throwing) on missing/corrupted data so app boot always degrades to the
  /// login screen instead of crashing.
  static Future<AuthSession?> loadSession() async {
    try {
      final token = await _storage.read(key: _kToken);
      if (token == null || token.isEmpty) return null;

      final userJson = await _storage.read(key: _kUser);
      if (userJson == null) return null;

      final farmsJson = await _storage.read(key: _kFarms);
      final companyId = await _storage.read(key: _kCompanyId);
      final activeFarmId = await _storage.read(key: _kActiveFarmId);

      final user = AppUser.fromJson(jsonDecode(userJson) as Map<String, dynamic>);
      final farms = farmsJson == null
          ? <Farm>[]
          : (jsonDecode(farmsJson) as List).map((f) => Farm.fromJson(f as Map<String, dynamic>)).toList();

      return AuthSession(
        token: token,
        user: user,
        companyId: (companyId == null || companyId.isEmpty) ? null : companyId,
        farms: farms,
        activeFarmId: (activeFarmId == null || activeFarmId.isEmpty) ? null : activeFarmId,
      );
    } catch (_) {
      return null;
    }
  }

  /// Persists the farms list as returned by the (richer) `GET /api/farms` call,
  /// so subsequent app launches start with full farm details rather than the
  /// thin login summary.
  static Future<void> cacheFarms(List<Farm> farms) =>
      _storage.write(key: _kFarms, value: jsonEncode(farms.map((f) => f.toJson()).toList()));

  static Future<void> setActiveFarmId(String farmId) =>
      _storage.write(key: _kActiveFarmId, value: farmId);

  static Future<String?> getToken() => _storage.read(key: _kToken);

  static Future<String?> getActiveFarmId() => _storage.read(key: _kActiveFarmId);

  static Future<void> logout() => _storage.deleteAll();
}
