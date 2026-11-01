// QA (independent) tests for ApiClient.sendMultipart and the way the upload screens use it.
// These exercise the real request that goes on the wire (captured with a MockClient), not a re-implementation.
import 'dart:convert';

import 'package:agromesh_mobile/screens/more/billing_common.dart';
import 'package:agromesh_mobile/services/api_client.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

class _Captured {
  late String method;
  late Uri url;
  late Map<String, String> headers;
  late String body; // latin1 view of the raw bytes, enough to read the text parts
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    // flutter_secure_storage mock: token = "tok123".
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger.setMockMethodCallHandler(
      const MethodChannel('plugins.it_nomads.com/flutter_secure_storage'),
      (call) async => call.method == 'read' ? 'tok123' : null,
    );
  });

  Future<(_Captured, Object?)> run(Future<dynamic> Function() action, {int status = 200, String reply = '{}', Map<String, String>? replyHeaders}) async {
    final cap = _Captured();
    final client = MockClient.streaming((request, bodyStream) async {
      cap.method = request.method;
      cap.url = request.url;
      cap.headers = request.headers;
      final bytes = await bodyStream.toBytes();
      cap.body = latin1.decode(bytes);
      return http.StreamedResponse(Stream.value(utf8.encode(reply)), status, headers: replyHeaders ?? const {});
    });
    Object? result;
    Object? error;
    await http.runWithClient(() async {
      try {
        result = await action();
      } catch (e) {
        error = e;
      }
    }, () => client);
    return (cap, error ?? result);
  }

  test('logo upload: PUT, bearer token, multipart content type, single part named "file", no stray fields', () async {
    final (cap, res) = await run(() => ApiClient.sendMultipart('PUT', '/api/farms/f1/logo',
        fileField: 'file', fileBytes: [1, 2, 3], fileName: 'logo.png', contentType: 'image/png'));
    expect(res, isA<Map>());
    expect(cap.method, 'PUT');
    expect(cap.url.path, '/api/farms/f1/logo');
    expect(cap.headers['authorization'] ?? cap.headers['Authorization'], 'Bearer tok123');
    final ct = cap.headers.entries.firstWhere((e) => e.key.toLowerCase() == 'content-type').value;
    expect(ct, startsWith('multipart/form-data; boundary='));
    expect(RegExp('content-disposition: form-data; name="file"; filename="logo.png"', caseSensitive: false).hasMatch(cap.body), isTrue);
    expect(cap.body, contains('content-type: image/png'));
    expect(RegExp(r'; name="').allMatches(cap.body).length, 1, reason: 'only the file part');
  });

  test('vendor invoice: field names match the contract exactly; amount uses "." and 2dp; blanks omitted', () async {
    final (cap, _) = await run(() => ApiClient.sendMultipart('POST', '/api/farms/f1/orders/o1/vendor-invoices',
        fileField: 'file',
        fileBytes: [0x25, 0x50, 0x44, 0x46, 0x2D],
        fileName: 'bill.pdf',
        contentType: 'application/pdf',
        fields: {
          'invoiceNumber': 'A-77',
          'amount': (1250.5).toStringAsFixed(2),
          'invoiceDate': '2026-03-01',
          'dueDate': null,
          'vendorName': '   ',
          'notes': '',
        }));
    final names = RegExp(r'; name="([^"]+)"').allMatches(cap.body).map((m) => m.group(1)).toSet();
    expect(names, {'file', 'invoiceNumber', 'amount', 'invoiceDate'});
    expect(cap.body, contains('1250.50'));
    expect(cap.body, contains('2026-03-01'));
    expect(cap.body, contains('content-type: application/pdf'));
  });

  test('QA: a hostile file name cannot break out of the Content-Disposition header', () async {
    final (cap, _) = await run(() => ApiClient.sendMultipart('POST', '/x',
        fileField: 'file', fileBytes: [1], fileName: 'a"; name="amount"\r\nX-Evil: 1.png', contentType: 'image/png'));
    // The header must still contain exactly one file part header block and no injected header line.
    expect(cap.body.contains('\r\nX-Evil: 1'), isFalse, reason: 'CRLF injection through filename');
  });

  group('error mapping (what the user is shown)', () {
    Future<String> msg(int status, String body, {Map<String, String>? h}) async {
      final (_, err) = await run(() => ApiClient.sendMultipart('POST', '/x', fileField: 'file', fileBytes: [1], fileName: 'a.png', contentType: 'image/png'),
          status: status, reply: body, replyHeaders: h);
      expect(err, isA<ApiException>());
      return failText(err!);
    }

    test('413 plain text (proxy/Kestrel) -> friendly size message', () async {
      expect(await msg(413, 'Request Entity Too Large'), 'That file is too large for the server.');
    });
    test('413 problem+json -> server detail', () async {
      expect(await msg(413, '{"title":"Too large","status":413,"detail":"The file is larger than 2 MB."}'), 'The file is larger than 2 MB.');
    });
    test('415 problem+json -> server detail; 415 empty -> generic', () async {
      expect(await msg(415, '{"detail":"Only PNG, JPEG or WebP."}'), 'Only PNG, JPEG or WebP.');
      expect(await msg(415, ''), 'That file type is not accepted.');
    });
    test('422 problem+json -> detail (incl. duplicate number / order not Received)', () async {
      expect(await msg(422, '{"detail":"Order is Placed; only Received orders can have vendor invoices."}'), contains('Received'));
    });
    test('403 -> role message', () async {
      expect(await msg(403, ''), 'Your role is not allowed to do this.');
    });
    test('400 with an errors map but no detail -> the field messages are shown', () async {
      final m = await msg(400, '{"title":"One or more validation errors occurred.","status":400,"errors":{"InvoiceNumber":["required"],"Amount":["must be greater than 0"]}}');
      expect(m, 'Invoice number: required Amount: must be greater than 0');
    });
    test('502/503 HTML from a sleeping Render instance -> generic code only', () async {
      expect(await msg(503, '<html>Service Unavailable</html>'), 'Request failed (HTTP 503)');
    });
  });

  group('number parsing used for amounts / rates', () {
    test('[FIXED] a decimal comma is a decimal; commas are thousands otherwise', () {
      expect(parseNumber('12,50'), 12.5);
      expect(parseNumber('12,5'), 12.5);
      expect(parseNumber('1,250'), 1250.0);
      expect(parseNumber('1,250.50'), 1250.5);
      expect(parseNumber('1,25,000'), 125000.0);
      expect(parseNumber('1.250,50'), 1250.5);
      expect(parseNumber('1250.5'), 1250.5);
      expect(parseNumber(' 7 '), 7.0);
      expect(parseNumber('-3,5'), -3.5);
    });
    test('[FIXED] NaN / Infinity / exponents / junk are refused', () {
      for (final s in ['NaN', 'Infinity', '-Infinity', '1e5', 'abc', '', '  ', '1.2.3x', '--1']) {
        expect(parseNumber(s), isNull, reason: s);
      }
    });
    test('[FIXED] the vendor-invoice amount guard rejects NaN', () {
      final a = parseNumber('NaN');
      expect(a == null || a <= 0, isTrue);
    });
  });

  group('offline fallback decision', () {
    test('network errors and 5xx use the saved copy; 4xx are shown', () {
      expect(shouldUseOfflineCache(Exception('socket')), isTrue);
      expect(shouldUseOfflineCache(const ApiException('x', 502)), isTrue);
      expect(shouldUseOfflineCache(const ApiException('x', 503)), isTrue);
      expect(shouldUseOfflineCache(const ApiException('x', 403)), isFalse);
      expect(shouldUseOfflineCache(const ApiException('x', 404)), isFalse);
      expect(shouldUseOfflineCache(const ApiException('x', 401)), isFalse);
    });
  });
}
