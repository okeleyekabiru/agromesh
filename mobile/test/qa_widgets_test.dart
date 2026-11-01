// QA (independent) widget tests: layout at small width / large font, tap targets, role gating, date picker range,
// logo decode cost. Screens that need sqflite (lists, branding) are NOT covered here (no DB in flutter test).
import 'dart:convert';
import 'dart:ui' as ui;

import 'package:agromesh_mobile/models/billing_models.dart';
import 'package:agromesh_mobile/screens/more/billing_common.dart';
import 'package:agromesh_mobile/screens/more/invoice_form_screen.dart';
import 'package:agromesh_mobile/screens/more/invoices_screen.dart';
import 'package:agromesh_mobile/screens/more/vendor_invoice_upload_screen.dart';
import 'package:agromesh_mobile/screens/more/vendor_invoices_screen.dart';
import 'package:agromesh_mobile/theme/app_theme.dart';
import 'package:agromesh_mobile/widgets/farm_mark.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

const _longName = 'Okeleye & Sons Agricultural Supplies and General Merchandise Nigeria Limited (Ibadan Branch)';

Map<String, dynamic> _invoiceJson() => {
      'id': 'i1', 'reference': 'INV-0042', 'date': '2026-03-05', 'dueDate': '2026-03-19',
      'billToName': _longName, 'billToPhone': '+234 803 000 0000', 'billToEmail': 'someone.with.a.very.long.address@a-really-long-domain-name.example.com',
      'billToAddress': '12 Some Extremely Long Street Name, Off Another Extremely Long Road, Ibadan, Oyo State, Nigeria',
      'lineItems': [
        for (var i = 0; i < 3; i++)
          {'serialNumber': i + 1, 'description': 'Premium NPK 15-15-15 fertiliser, 50 kg bags, delivered to farm gate - item $i $_longName', 'rate': 1234567.89, 'quantity': 12.5, 'amount': 15432098.63},
      ],
      'subtotal': 46296295.89, 'vatPercent': 7.5, 'vatAmount': 3472222.19, 'total': 49768518.08,
      'paymentAccountName': _longName, 'paymentAccountNumber': '0123456789', 'paymentBankName': 'Guaranty Trust Bank',
      'termsAndNotes': 'Payment is due within 14 days. ' * 8, 'signatoryName': 'A. Very Long Signatory Name Okeleye-Adebayo', 'templateKey': 'ngf',
    };

Future<void> _small(WidgetTester t) async {
  t.view.physicalSize = const Size(320, 568);
  t.view.devicePixelRatio = 1.0;
  addTearDown(t.view.reset);
}

Widget _app(Widget home, {double scale = 1.0}) => MaterialApp(
      theme: AgroTheme.theme,
      builder: (c, child) => MediaQuery(data: MediaQuery.of(c).copyWith(textScaler: TextScaler.linear(scale)), child: child!),
      home: home,
    );

MockClient _api() => MockClient((req) async {
      if (req.url.path.endsWith('/invoice-templates')) {
        return http.Response(jsonEncode([{'key': 'ngf', 'name': 'NGF - Invoice', 'isDefault': true}]), 200);
      }
      if (req.url.path.contains('/invoices/i1')) return http.Response(jsonEncode(_invoiceJson()), 200);
      return http.Response('{}', 404);
    });

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger.setMockMethodCallHandler(
      const MethodChannel('plugins.it_nomads.com/flutter_secure_storage'),
      (call) async => call.method == 'read' ? 'tok' : null,
    );
  });

  realisticOverflowTests();

  group('layout: 320x568 at 2.0x font scale', () {
    testWidgets('[CONFIRMED BUG at 2.0x font] VendorBillTile with long vendor / numbers does not overflow', (t) async {
      await _small(t);
      final bill = VendorInvoice.fromJson({
        'id': 'v', 'orderId': 'o', 'orderReference': 'PO-0001-VERY-LONG-REFERENCE', 'orderTotal': 123456789.5, 'vendorName': _longName,
        'invoiceNumber': 'INVOICE-NUMBER-THAT-IS-REALLY-QUITE-LONG-0000000001', 'amount': 987654321.25, 'invoiceDate': '2026-03-01',
        'dueDate': '2026-03-15', 'status': 'Unpaid', 'isOverdue': true, 'fileName': 'a.pdf', 'contentType': 'application/pdf', 'fileSize': 1,
      });
      await t.pumpWidget(_app(Scaffold(body: SingleChildScrollView(child: VendorBillTile(bill: bill))), scale: 2.0));
      await t.pump();
      expect(t.takeException(), isNull);
    });

    testWidgets('InvoiceTotalsCard with 9-digit totals does not overflow', (t) async {
      await _small(t);
      await t.pumpWidget(_app(
          const Scaffold(body: InvoiceTotalsCard(totals: InvoiceTotals(987654321.25, 74074074.1, 1061728395.35), vatPercent: 7.5)),
          scale: 2.0));
      await t.pump();
      expect(t.takeException(), isNull);
    });

    testWidgets('InvoiceFormScreen (edit mode, long text) does not overflow', (t) async {
      await _small(t);
      final inv = Invoice.fromJson(_invoiceJson());
      await http.runWithClient(() async {
        await t.pumpWidget(_app(InvoiceFormScreen(farmId: 'f1', existing: inv), scale: 2.0));
        await t.pump();
        await t.pump(const Duration(milliseconds: 200));
      }, _api);
      expect(t.takeException(), isNull);
      // Scroll through the whole form to build every row.
      await t.drag(find.byType(ListView), const Offset(0, -3000));
      await t.pump();
      expect(t.takeException(), isNull);
    });

    testWidgets('[CONFIRMED BUG at 2.0x font] InvoiceDetailScreen (long text, 3 lines) does not overflow', (t) async {
      await _small(t);
      await http.runWithClient(() async {
        await t.pumpWidget(_app(
            const InvoiceDetailScreen(farmId: 'f1', summary: InvoiceSummary(id: 'i1', reference: 'INV-0042', billToName: 'x', total: 1), canEdit: true),
            scale: 2.0));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
      }, _api);
      expect(t.takeException(), isNull);
      await t.drag(find.byType(ListView), const Offset(0, -4000));
      await t.pump();
      expect(t.takeException(), isNull);
    });

    testWidgets('VendorInvoiceUploadScreen (long supplier) does not overflow', (t) async {
      await _small(t);
      await t.pumpWidget(_app(
          const VendorInvoiceUploadScreen(farmId: 'f1', orderId: 'o1', orderLabel: 'PO-0001-WITH-A-VERY-LONG-LABEL', orderTotal: 123456789.5, supplierName: _longName),
          scale: 2.0));
      await t.pump();
      expect(t.takeException(), isNull);
      await t.drag(find.byType(ListView), const Offset(0, -3000));
      await t.pump();
      expect(t.takeException(), isNull);
    });
  });

  group('role gating on InvoiceDetailScreen', () {
    Future<void> open(WidgetTester t, bool canEdit) async {
      t.view.physicalSize = const Size(400, 3600);
      t.view.devicePixelRatio = 1.0;
      addTearDown(t.view.reset);
      await http.runWithClient(() async {
        await t.pumpWidget(_app(InvoiceDetailScreen(farmId: 'f1', summary: const InvoiceSummary(id: 'i1', reference: 'INV-0042', billToName: 'x', total: 1), canEdit: canEdit)));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
      }, _api);
    }

    testWidgets('manager sees Download PDF, Edit, Delete', (t) async {
      await open(t, true);
      await t.pump();
      expect(find.text('Download PDF'), findsOneWidget);
      expect(find.text('Edit'), findsOneWidget);
      expect(find.text('Delete'), findsOneWidget);
    });

    testWidgets('read-only role sees Download PDF only', (t) async {
      await open(t, false);
      expect(find.text('Download PDF'), findsOneWidget);
      expect(find.text('Edit'), findsNothing);
      expect(find.text('Delete'), findsNothing);
    });
  });

  group('accessibility guidelines', () {
    testWidgets('VendorInvoiceUploadScreen: tap targets, labels, contrast', (t) async {
      final h = t.ensureSemantics();
      await t.pumpWidget(_app(const VendorInvoiceUploadScreen(farmId: 'f1', orderId: 'o1', orderLabel: 'PO-1', orderTotal: 100, supplierName: 'S')));
      await t.pump();
      await expectLater(t, meetsGuideline(androidTapTargetGuideline));
      await expectLater(t, meetsGuideline(labeledTapTargetGuideline));
      await expectLater(t, meetsGuideline(textContrastGuideline));
      h.dispose();
    });

    testWidgets('InvoiceFormScreen (2 items): tap targets, labels, contrast', (t) async {
      final h = t.ensureSemantics();
      final inv = Invoice.fromJson(_invoiceJson());
      await http.runWithClient(() async {
        await t.pumpWidget(_app(InvoiceFormScreen(farmId: 'f1', existing: inv)));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
      }, _api);
      await expectLater(t, meetsGuideline(androidTapTargetGuideline));
      await expectLater(t, meetsGuideline(labeledTapTargetGuideline));
      await expectLater(t, meetsGuideline(textContrastGuideline));
      h.dispose();
    });
  });

  group('form behaviour', () {
    testWidgets('InvoiceFormScreen: removing an item hides the remove button when one item is left; add button works', (t) async {
      t.view.physicalSize = const Size(400, 2400);
      t.view.devicePixelRatio = 1.0;
      addTearDown(t.view.reset);
      final inv = Invoice.fromJson(_invoiceJson());
      await http.runWithClient(() async {
        await t.pumpWidget(_app(InvoiceFormScreen(farmId: 'f1', existing: inv)));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
      }, _api);
      expect(find.byTooltip('Remove item 3'), findsOneWidget);
      await t.tap(find.byTooltip('Remove item 3'));
      await t.pump();
      await t.tap(find.byTooltip('Remove item 2'));
      await t.pump();
      expect(find.textContaining('Remove item'), findsNothing, reason: 'cannot remove the last item');
      expect(find.text('Item 1'), findsOneWidget);
    });

    testWidgets('InvoiceFormScreen: empty description is refused before any request', (t) async {
      t.view.physicalSize = const Size(400, 2400);
      t.view.devicePixelRatio = 1.0;
      addTearDown(t.view.reset);
      var posts = 0;
      final client = MockClient((req) async {
        if (req.method != 'GET') posts++;
        if (req.url.path.endsWith('/invoice-templates')) return http.Response('[]', 200);
        return http.Response('{}', 404);
      });
      await http.runWithClient(() async {
        await t.pumpWidget(_app(InvoiceFormScreen(farmId: 'f1', existing: Invoice.fromJson({..._invoiceJson(), 'lineItems': []}))));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
        await t.enterText(find.widgetWithText(TextField, 'Customer name'), 'Acme');
        await t.ensureVisible(find.text('Save changes'));
        await t.tap(find.text('Save changes'));
        await t.pump();
      }, () => client);
      expect(find.text('Item 1 needs a description.'), findsOneWidget);
      expect(posts, 0);
    });

    testWidgets('InvoiceFormScreen: save sends PUT with ISO dates and numeric (not string) money', (t) async {
      t.view.physicalSize = const Size(400, 2400);
      t.view.devicePixelRatio = 1.0;
      addTearDown(t.view.reset);
      Map<String, dynamic>? sent;
      String? method;
      final client = MockClient((req) async {
        if (req.url.path.endsWith('/invoice-templates')) return http.Response('[]', 200);
        method = req.method;
        sent = jsonDecode(req.body) as Map<String, dynamic>;
        return http.Response(jsonEncode(_invoiceJson()), 200);
      });
      await http.runWithClient(() async {
        await t.pumpWidget(_app(InvoiceFormScreen(farmId: 'f1', existing: Invoice.fromJson(_invoiceJson()))));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
        await t.ensureVisible(find.text('Save changes'));
        await t.tap(find.text('Save changes'));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
      }, () => client);
      expect(method, 'PUT');
      expect(sent!['date'], '2026-03-05');
      expect(sent!['dueDate'], '2026-03-19');
      expect(sent!['vatPercent'], 7.5);
      expect((sent!['lineItems'] as List).first['rate'], 1234567.89);
      expect(sent!.containsKey('number'), isFalse);
    });
  });

  group('date picker range', () {
    testWidgets('[CONFIRMED BUG] DateField with value before `first` (vendor due date older than a later invoice date) asserts when opened', (t) async {
      DateTime? picked;
      await t.pumpWidget(_app(Scaffold(
        body: DateField(label: 'Due date', value: DateTime(2026, 1, 5), first: DateTime(2026, 1, 10), clearable: true, onChanged: (d) => picked = d),
      )));
      await t.tap(find.text('Jan 5, 2026').evaluate().isNotEmpty ? find.text('Jan 5, 2026') : find.byType(InkWell).first);
      await t.pump();
      await t.pump(const Duration(milliseconds: 300));
      final ex = t.takeException();
      expect(ex, isNull, reason: 'showDatePicker initialDate (Jan 5) is before firstDate (Jan 10): $ex');
      expect(picked, isNull);
    });
  });

  group('logo decode cost', () {
    testWidgets('[CONFIRMED BUG] a 4096x4096 logo is decoded at full size for a 32dp header mark', (t) async {
      late Uint8List big;
      await t.runAsync(() async {
        final rec = ui.PictureRecorder();
        final canvas = Canvas(rec);
        canvas.drawRect(const Rect.fromLTWH(0, 0, 4096, 4096), Paint()..color = const Color(0xFF2D7A4F));
        final img = await rec.endRecording().toImage(4096, 4096);
        big = (await img.toByteData(format: ui.ImageByteFormat.png))!.buffer.asUint8List();
      });
      PaintingBinding.instance.imageCache.clear();
      await t.pumpWidget(MaterialApp(home: Scaffold(body: Center(child: FarmMarkImage(bytes: big, size: 32)))));
      await t.runAsync(() => Future<void>.delayed(const Duration(seconds: 2)));
      await t.pump();
      final bytes = PaintingBinding.instance.imageCache.currentSizeBytes;
      // 4096*4096*4 = 67,108,864 bytes held for a 32x32 widget.
      expect(bytes, lessThan(4 * 1024 * 1024), reason: 'image cache holds $bytes bytes for a 32dp logo (no cacheWidth/cacheHeight)');
    });
  });
}

// Realistic values (a 1.25M naira bill) at common accessibility font scales on a common 360dp phone.
void realisticOverflowTests() {
  for (final scale in [1.3, 1.5, 2.0]) {
    testWidgets('[BUG only at 2.0x] realistic: VendorBillTile ₦1,250,000.00 at ${scale}x on 360dp', (t) async {
      t.view.physicalSize = const Size(360, 740);
      t.view.devicePixelRatio = 1.0;
      addTearDown(t.view.reset);
      final bill = VendorInvoice.fromJson({
        'id': 'v', 'orderId': 'o', 'orderReference': 'PO-0001', 'orderTotal': 1250000, 'vendorName': 'Agro Supply Ltd',
        'invoiceNumber': 'A-77', 'amount': 1250000, 'invoiceDate': '2026-03-01', 'dueDate': '2026-03-15', 'status': 'Unpaid',
        'isOverdue': true, 'fileName': 'a.pdf', 'contentType': 'application/pdf', 'fileSize': 1,
      });
      await t.pumpWidget(_app(Scaffold(body: SingleChildScrollView(child: VendorBillTile(bill: bill))), scale: scale));
      await t.pump();
      expect(t.takeException(), isNull);
    });

    testWidgets('[BUG only at 2.0x] realistic: invoice detail line ₦1,250,000.00 at ${scale}x on 360dp', (t) async {
      t.view.physicalSize = const Size(360, 3000);
      t.view.devicePixelRatio = 1.0;
      addTearDown(t.view.reset);
      final json = {
        ..._invoiceJson(),
        'billToName': 'Acme Ltd',
        'lineItems': [
          {'serialNumber': 1, 'description': 'Cocoa beans', 'rate': 250000, 'quantity': 5, 'amount': 1250000},
        ],
      };
      final client = MockClient((r) async => http.Response(jsonEncode(json), 200));
      await http.runWithClient(() async {
        await t.pumpWidget(_app(
            const InvoiceDetailScreen(farmId: 'f1', summary: InvoiceSummary(id: 'i1', reference: 'INV-1', billToName: 'x', total: 1), canEdit: true),
            scale: scale));
        await t.pump();
        await t.pump(const Duration(milliseconds: 100));
      }, () => client);
      expect(t.takeException(), isNull);
    });
  }
}
