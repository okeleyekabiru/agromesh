import 'dart:convert';
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
  ///   flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5013
  ///
  /// Notes on the default (`http://localhost:5013`, matching the API's
  /// documented local dev port — see api/AgroMesh.API/Properties/launchSettings.json
  /// and web/.env.development):
  ///   - Android emulators can't reach the host machine via `localhost` — it
  ///     resolves to the emulator itself. Use `10.0.2.2` instead.
  ///   - iOS simulator can use `localhost` directly.
  ///   - Physical devices need the host machine's LAN IP.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5013',
  );

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
    throw ApiException('Request failed (HTTP ${res.statusCode})', res.statusCode);
  }
}
