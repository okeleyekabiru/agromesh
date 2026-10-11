import 'dart:convert';
import 'package:flutter/foundation.dart' show kIsWeb, defaultTargetPlatform, TargetPlatform;
import 'package:http/http.dart' as http;
import 'auth_service.dart';

/// Thrown by [ApiClient] for any non-2xx response.
class ApiException implements Exception {
  final int? statusCode;
  final String message;

  const ApiException(this.message, [this.statusCode]);

  @override
  String toString() => message;
}

/// Small shared HTTP helper: resolves the configurable base URL once, and
/// attaches `Authorization: Bearer <token>` to every request unless explicitly
/// opted out (only the login call itself runs unauthenticated).
class ApiClient {
  ApiClient._();

  /// Override at build/run time, e.g.:
  ///   flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
  ///
  /// Notes on the defaults (`http://10.0.2.2:5000` on Android, `http://localhost:5000` elsewhere; 5000 is the Docker API port;
  /// documented local dev port — see api/AgroMesh.API/Properties/launchSettings.json
  /// and web/.env.development):
  ///   - Android emulators can't reach the host machine via `localhost` — it
  ///     resolves to the emulator itself. Use `10.0.2.2` instead.
  ///   - iOS simulator can use `localhost` directly.
  ///   - Physical devices need the host machine's LAN IP.
  static const String _override = String.fromEnvironment('API_BASE_URL');

  /// With no override, an Android build assumes the emulator and reaches the host PC at 10.0.2.2; other
  /// platforms use localhost. A physical phone must pass --dart-define=API_BASE_URL=http://<PC LAN IP>:5000.
  static final String baseUrl = _override.isNotEmpty
      ? _override
      : (!kIsWeb && defaultTargetPlatform == TargetPlatform.android ? 'http://10.0.2.2:5000' : 'http://localhost:5000');

  static const _timeout = Duration(seconds: 15);

  static Future<Map<String, String>> _headers({bool auth = true}) async {
    final headers = <String, String>{'Content-Type': 'application/json'};
    if (auth) {
      final token = await AuthService.getToken();
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
    }
    return headers;
  }

  static Future<dynamic> get(String path, {bool auth = true}) async {
    final res = await http
        .get(Uri.parse('$baseUrl$path'), headers: await _headers(auth: auth))
        .timeout(_timeout);
    return _handle(res);
  }

  static Future<dynamic> post(String path, Map<String, dynamic> body, {bool auth = true}) async {
    final res = await http
        .post(Uri.parse('$baseUrl$path'), headers: await _headers(auth: auth), body: jsonEncode(body))
        .timeout(_timeout);
    return _handle(res);
  }

  static dynamic _handle(http.Response res) {
    if (res.statusCode >= 200 && res.statusCode < 300) {
      if (res.body.isEmpty) return null;
      return jsonDecode(res.body);
    }
    // The API replies with RFC 7807 problem JSON; its `detail` is the human-readable reason
    // (e.g. "Not enough 'NPK Fertilizer'..."), which beats a bare status code in the UI.
    String message = 'Request failed (HTTP ${res.statusCode})';
    try {
      final body = jsonDecode(res.body);
      final detail = body is Map ? body['detail'] : null;
      if (detail is String && detail.isNotEmpty) message = detail;
    } catch (_) {/* not JSON — keep the generic message */}
    throw ApiException(message, res.statusCode);
  }
}
