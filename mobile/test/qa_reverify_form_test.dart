// QA (independent): the invoice form refuses NaN / Infinity / over-limit input before any request is sent.
import 'dart:convert';

import 'package:agromesh_mobile/models/billing_models.dart';
import 'package:agromesh_mobile/screens/more/invoice_form_screen.dart';
import 'package:agromesh_mobile/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  setUp(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger.setMockMethodCallHandler(
      const MethodChannel('plugins.it_nomads.com/flutter_secure_storage'),
      (call) async => call.method == 'read' ? 'tok' : null,
    );
  });

  final inv = Invoice.fromJson({
    'id': 'i1', 'reference': 'INV-1', 'date': '2026-03-05', 'dueDate': '2026-03-19', 'billToName': 'Acme',
    'lineItems': [{'description': 'Item', 'rate': 10, 'quantity': 2}], 'vatPercent': 0, 'templateKey': 'ngf',
  });

  Future<(String?, int)> attempt(WidgetTester t, {String? rate, String? qty, String? vat}) async {
    t.view.physicalSize = const Size(400, 3000);
    t.view.devicePixelRatio = 1.0;
    addTearDown(t.view.reset);
    var writes = 0;
    final client = MockClient((req) async {
      if (req.method != 'GET') writes++;
      if (req.url.path.endsWith('/invoice-templates')) return http.Response('[]', 200);
      return http.Response(jsonEncode({'id': 'i1', 'billToName': 'x', 'lineItems': []}), 200);
    });
    await http.runWithClient(() async {
      await t.pumpWidget(MaterialApp(theme: AgroTheme.theme, home: InvoiceFormScreen(farmId: 'f1', existing: inv)));
      await t.pump();
      await t.pump(const Duration(milliseconds: 100));
      if (rate != null) await t.enterText(find.widgetWithText(TextField, 'Rate (₦)'), rate);
      if (qty != null) await t.enterText(find.widgetWithText(TextField, 'Quantity'), qty);
      if (vat != null) await t.enterText(find.widgetWithText(TextField, 'VAT %'), vat);
      await t.ensureVisible(find.text('Save changes'));
      await t.tap(find.text('Save changes'));
      await t.pump();
      await t.pump(const Duration(milliseconds: 100));
    }, () => client);
    final err = find.byWidgetPredicate((w) => w is Text && w.style?.color == const Color(0xFFDC3C3C));
    final shown = err.evaluate().isEmpty ? null : (err.evaluate().first.widget as Text).data;
    return (shown, writes);
  }

  testWidgets('rate NaN', (t) async {
    final (msg, w) = await attempt(t, rate: 'NaN');
    expect(w, 0);
    expect(msg, contains('needs a rate and a quantity'));
  });
  testWidgets('quantity Infinity', (t) async {
    final (msg, w) = await attempt(t, qty: 'Infinity');
    expect(w, 0);
    expect(msg, contains('needs a rate and a quantity'));
  });
  testWidgets('VAT NaN', (t) async {
    final (msg, w) = await attempt(t, vat: 'NaN');
    expect(w, 0);
    expect(msg, contains('VAT must be a number'));
  });
  testWidgets('VAT 100.01', (t) async {
    final (msg, w) = await attempt(t, vat: '100.01');
    expect(w, 0);
    expect(msg, contains('VAT must be'));
  });
  testWidgets('rate 100,000,000', (t) async {
    final (msg, w) = await attempt(t, rate: '100000000');
    expect(w, 0);
    expect(msg, contains('below'));
  });
  testWidgets('rate 99,999,999.99 x 11 -> line amount over 1e9', (t) async {
    final (msg, w) = await attempt(t, rate: '99999999.99', qty: '11');
    expect(w, 0);
    expect(msg, contains('amount of item 1'));
  });
  testWidgets('comma decimal rate "12,50" x 2 is accepted and sent as 12.5', (t) async {
    final (msg, w) = await attempt(t, rate: '12,50');
    expect(msg, isNull);
    expect(w, 1);
  });
}
