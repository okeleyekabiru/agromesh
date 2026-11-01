import 'dart:convert';
import 'package:flutter/foundation.dart' show kIsWeb, kReleaseMode, defaultTargetPlatform, TargetPlatform;
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart' show MediaType;
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

  /// Hosted API that release builds talk to unless API_BASE_URL says otherwise.
  static const String _productionUrl = 'https://agromesh-api.onrender.com';

  /// Release builds use the hosted API. Debug runs with no override assume the emulator on Android (host PC at
  /// 10.0.2.2) and localhost elsewhere. A physical phone in debug must pass --dart-define=API_BASE_URL=http://<PC LAN IP>:5000.
  static final String baseUrl = _override.isNotEmpty
      ? _override
      : kReleaseMode
          ? _productionUrl
          : (!kIsWeb && defaultTargetPlatform == TargetPlatform.android ? 'http://10.0.2.2:5000' : 'http://localhost:5000');

  // Long enough for the API to wake from sleep (a hosted API that scales to zero takes a while on its first request).
  static const _timeout = Duration(seconds: 120);

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

  static Future<dynamic> patch(String path, [Map<String, dynamic>? body]) async {
    final res = await http
        .patch(Uri.parse('$baseUrl$path'), headers: await _headers(), body: jsonEncode(body ?? {}))
        .timeout(_timeout);
    return _handle(res);
  }

  static Future<dynamic> put(String path, Map<String, dynamic> body) async {
    final res = await http
        .put(Uri.parse('$baseUrl$path'), headers: await _headers(), body: jsonEncode(body))
        .timeout(_timeout);
    return _handle(res);
  }

  /// Sends a `multipart/form-data` request (file uploads). Reuses the bearer token and timeout of the JSON calls; the
  /// multipart Content-Type (with its boundary) is set by the request itself. [fileBytes] goes in the part named
  /// [fileField]; [fields] are the plain form fields (null or blank values are left out). Throws [ApiException] on a
  /// non-2xx reply, so a 413 / 415 / 422 `detail` reaches the caller.
  static Future<dynamic> sendMultipart(
    String method,
    String path, {
    required String fileField,
    required List<int> fileBytes,
    required String fileName,
    required String contentType,
    Map<String, String?> fields = const {},
  }) async {
    final request = http.MultipartRequest(method, Uri.parse('$baseUrl$path'));
    final token = await AuthService.getToken();
    if (token != null && token.isNotEmpty) request.headers['Authorization'] = 'Bearer $token';
    request.fields.addAll({
      for (final e in fields.entries)
        if (e.value != null && e.value!.trim().isNotEmpty) e.key: e.value!,
    });
    request.files.add(http.MultipartFile.fromBytes(
      fileField,
      fileBytes,
      filename: fileName,
      contentType: MediaType.parse(contentType),
    ));
    final streamed = await request.send().timeout(_timeout);
    final res = await http.Response.fromStream(streamed).timeout(_timeout);
    return _handle(res);
  }

  static Future<dynamic> delete(String path) async {
    final res = await http.delete(Uri.parse('$baseUrl$path'), headers: await _headers()).timeout(_timeout);
    return _handle(res);
  }

  /// For binary responses (e.g. satellite pictures). Throws [ApiException] on a non-2xx reply.
  static Future<List<int>> getBytes(String path) async {
    final res = await http.get(Uri.parse('$baseUrl$path'), headers: await _headers()).timeout(_timeout);
    if (res.statusCode >= 200 && res.statusCode < 300) return res.bodyBytes;
    _handle(res);
    return const [];
  }

  /// "InvoiceNumber" -> "Invoice number"; "$.lineItems[0].rate" -> "lineItems[0].rate".
  static String _humanField(String f) {
    final name = f.startsWith(r'$.') ? f.substring(2) : f;
    if (name.contains('.') || name.contains('[')) return name;
    final spaced = name.replaceAllMapped(RegExp(r'(?<=[a-z0-9])(?=[A-Z])'), (_) => ' ');
    return spaced.isEmpty ? spaced : spaced[0].toUpperCase() + spaced.substring(1).toLowerCase();
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
      if (detail is String && detail.isNotEmpty) {
        message = detail;
      } else if (body is Map && body['errors'] is Map) {
        // A 400 validation problem has no `detail`, only `errors: {Field: [messages]}`: show those instead of a bare code.
        final parts = <String>[];
        (body['errors'] as Map).forEach((field, msgs) {
          final text = msgs is List ? msgs.whereType<String>().join(' ') : '$msgs';
          if (text.isNotEmpty) parts.add('${_humanField('$field')}: $text');
        });
        if (parts.isNotEmpty) message = parts.join(' ');
      }
    } catch (_) {/* not JSON — keep the generic message */}
    throw ApiException(message, res.statusCode);
  }
}
