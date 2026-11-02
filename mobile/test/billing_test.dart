import 'dart:io';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:ilevora_mobile/models/billing_models.dart';
import 'package:ilevora_mobile/models/models.dart';
import 'package:ilevora_mobile/screens/more/billing_common.dart';
import 'package:ilevora_mobile/screens/more/invoice_form_screen.dart';
import 'package:ilevora_mobile/screens/more/vendor_invoices_screen.dart';
import 'package:ilevora_mobile/services/farm_logo_service.dart';
import 'package:ilevora_mobile/services/upload_rules.dart';
import 'package:ilevora_mobile/theme/app_theme.dart';
import 'package:ilevora_mobile/widgets/farm_mark.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

// 1x1 transparent PNG.
final _png = Uint8List.fromList(const [
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01,
  0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
  0x54, 0x78, 0x9C, 0x63, 0xF8, 0xFF, 0xFF, 0x3F, 0x00, 0x05, 0xFE, 0x02, 0xFE, 0xA7, 0x35, 0x81, 0x84, 0x00, 0x00, 0x00,
  0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
]);

void main() {
  group('upload rules', () {
    test('sniffs the real type from the first bytes', () {
      expect(sniffUpload(_png), UploadKind.png);
      expect(sniffUpload([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31]), UploadKind.pdf);
      expect(sniffUpload([0xFF, 0xD8, 0xFF, 0xE0]), UploadKind.jpeg);
      expect(sniffUpload([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50]), UploadKind.webp);
      expect(sniffUpload('<svg></svg>'.codeUnits), isNull);
      expect(sniffUpload(const []), isNull);
    });

    test('logo: png/jpeg/webp up to 2 MB, no pdf', () {
      expect(uploadProblem(UploadRule.logo, _png), isNull);
      expect(uploadProblem(UploadRule.logo, [0x25, 0x50, 0x44, 0x46, 0x2D]), contains('PNG, JPEG, WebP'));
      expect(uploadProblem(UploadRule.logo, const []), contains('empty'));
      final big = Uint8List(2 * 1024 * 1024 + 1)..setRange(0, _png.length, _png);
      expect(uploadProblem(UploadRule.logo, big), contains('2 MB'));
    });

    test('vendor invoice: pdf/png/jpeg up to 10 MB, no webp or text', () {
      expect(uploadProblem(UploadRule.vendorInvoice, [0x25, 0x50, 0x44, 0x46, 0x2D]), isNull);
      expect(uploadProblem(UploadRule.vendorInvoice, _png), isNull);
      expect(uploadProblem(UploadRule.vendorInvoice, [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50]), isNotNull);
      expect(uploadProblem(UploadRule.vendorInvoice, 'hello'.codeUnits), isNotNull);
      final big = Uint8List(10 * 1024 * 1024 + 1)..setRange(0, 5, [0x25, 0x50, 0x44, 0x46, 0x2D]);
      expect(uploadProblem(UploadRule.vendorInvoice, big), contains('10 MB'));
      final exactly = Uint8List(10 * 1024 * 1024)..setRange(0, 5, [0x25, 0x50, 0x44, 0x46, 0x2D]);
      expect(uploadProblem(UploadRule.vendorInvoice, exactly), isNull);
    });
  });

  group('invoice maths', () {
    test('line amounts round to 2dp and VAT is on the subtotal', () {
      const lines = [
        InvoiceLine(description: 'Cocoa', rate: 1250.50, quantity: 3), // 3751.50
        InvoiceLine(description: 'Bags', rate: 0.333, quantity: 3), // 0.999 -> 1.00
      ];
      expect(lines[0].amount, 3751.50);
      expect(lines[1].amount, 1.0);
      final t = computeInvoiceTotals(lines, 7.5);
      expect(t.subtotal, 3752.50);
      expect(t.vatAmount, 281.44); // 281.4375
      expect(t.total, 4033.94);
    });

    test('zero VAT and empty list', () {
      final t = computeInvoiceTotals(const [], 0);
      expect(t.total, 0);
      expect(computeInvoiceTotals(const [InvoiceLine(description: 'x', rate: 100, quantity: 2)], 0).total, 200);
    });

    test('request body sends dates as yyyy-MM-dd and blanks as null', () {
      final body = invoiceRequestBody(
        date: DateTime(2026, 3, 5),
        dueDate: DateTime(2026, 3, 19),
        billToName: ' Acme ',
        billToPhone: '  ',
        lineItems: const [InvoiceLine(description: 'Item', rate: 10, quantity: 2)],
        vatPercent: 5,
        templateKey: 'ngf',
      );
      expect(body['date'], '2026-03-05');
      expect(body['dueDate'], '2026-03-19');
      expect(body['billToName'], 'Acme');
      expect(body['billToPhone'], isNull);
      expect((body['lineItems'] as List).single, {'description': 'Item', 'rate': 10.0, 'quantity': 2.0});
    });

    test('parses a full invoice from the API shape', () {
      final inv = Invoice.fromJson({
        'id': 'i1', 'reference': 'INV-0002', 'date': '2026-03-05', 'dueDate': '2026-03-19', 'billToName': 'Acme',
        'lineItems': [{'serialNumber': 1, 'description': 'Item', 'rate': 10, 'quantity': 2, 'amount': 20}],
        'subtotal': 20, 'vatPercent': 5, 'vatAmount': 1, 'total': 21, 'paymentBankName': 'GTB', 'templateKey': 'ngf',
      });
      expect(inv.reference, 'INV-0002');
      expect(inv.lineItems.single.amount, 20);
      expect(inv.total, 21);
      expect(inv.paymentBankName, 'GTB');
      expect(inv.billToPhone, isNull);
    });
  });

  group('vendor invoices', () {
    Map<String, dynamic> json({String status = 'Unpaid', bool overdue = false, num amount = 5000, num orderTotal = 5000}) => {
          'id': 'v1', 'orderId': 'o1', 'orderReference': 'PO-0001', 'orderTotal': orderTotal, 'orderIsPaid': false,
          'vendorName': 'AgroSupply', 'invoiceNumber': 'A-77', 'amount': amount, 'invoiceDate': '2026-03-01',
          'dueDate': '2026-03-15', 'status': status, 'paidAt': null, 'isOverdue': overdue, 'notes': null,
          'fileName': 'a.pdf', 'contentType': 'application/pdf', 'fileSize': 1234,
        };

    test('parses status, overdue and the pdf flag', () {
      final v = VendorInvoice.fromJson(json(overdue: true));
      expect(v.isPaid, isFalse);
      expect(v.isOverdue, isTrue);
      expect(v.isPdf, isTrue);
      expect(v.dueDate, DateTime(2026, 3, 15));
    });

    test('a paid bill is never shown as overdue', () {
      expect(VendorInvoice.fromJson(json(status: 'Paid', overdue: true)).isOverdue, isFalse);
    });

    test('mismatch hint only when the amount differs from the order total', () {
      expect(mismatchHint(VendorInvoice.fromJson(json())), isNull);
      final more = mismatchHint(VendorInvoice.fromJson(json(amount: 5500)))!;
      expect(more, contains('₦500.00 more'));
      expect(mismatchHint(VendorInvoice.fromJson(json(amount: 4000)))!, contains('₦1,000.00 less'));
    });

    test('Paid / Unpaid filter', () {
      final bills = [VendorInvoice.fromJson(json()), VendorInvoice.fromJson(json(status: 'Paid'))];
      expect(filterVendorInvoices(bills, VendorFilter.all), hasLength(2));
      expect(filterVendorInvoices(bills, VendorFilter.unpaid).single.isPaid, isFalse);
      expect(filterVendorInvoices(bills, VendorFilter.paid).single.isPaid, isTrue);
    });

    testWidgets('tile shows Unpaid, Overdue and the differs badge', (tester) async {
      await tester.pumpWidget(MaterialApp(
        theme: IlevoraTheme.theme,
        home: Scaffold(body: VendorBillTile(bill: VendorInvoice.fromJson(json(overdue: true, amount: 6000)))),
      ));
      expect(find.text('UNPAID'), findsOneWidget);
      expect(find.text('OVERDUE'), findsOneWidget);
      expect(find.text('AMOUNT DIFFERS'), findsOneWidget);
      expect(find.text('AgroSupply'), findsOneWidget);
      expect(find.text('₦6,000.00'), findsOneWidget);
    });
  });

  group('roles and models', () {
    test('who may upload a vendor bill', () {
      AppUser u(String role) => AppUser(id: '1', email: 'a@b.c', fullName: 'A', role: role);
      expect(u('Admin').canUploadVendorBill, isTrue);
      expect(u('FarmManager').canUploadVendorBill, isTrue);
      expect(u('Supervisor').canUploadVendorBill, isTrue);
      expect(u('Member').canUploadVendorBill, isFalse);
    });

    test('Farm carries hasLogo / logoUpdatedAt and defaults to no logo', () {
      final withLogo = Farm.fromJson({
        'id': 'f1', 'name': 'Farm', 'area': {'value': 1, 'unit': 0}, 'setupStatus': 'Configured',
        'hasLogo': true, 'logoUpdatedAt': '2026-03-01T10:00:00Z',
      });
      expect(withLogo.hasLogo, isTrue);
      expect(withLogo.logoUpdatedAt, '2026-03-01T10:00:00Z');
      expect(Farm.fromLoginSummary({'id': 'f1', 'name': 'Farm', 'setupStatus': 'Configured'}).hasLogo, isFalse);
    });

    test('nairaExact keeps kobo and groups thousands', () {
      expect(nairaExact(1250.5), '₦1,250.50');
      expect(nairaExact(0), '₦0.00');
      expect(parseNumber('1,250.50'), 1250.5);
      expect(parseNumber('abc'), isNull);
    });
  });

  group('farm logo cache', () {
    late Directory dir;
    late int fetches;
    late FarmLogoService service;
    var fail = false;

    setUp(() async {
      dir = await Directory.systemTemp.createTemp('logo_test');
      fetches = 0;
      fail = false;
      service = FarmLogoService(
        cache: FarmLogoCache(dirProvider: () async => dir),
        fetch: (farmId) async {
          fetches++;
          if (fail) throw Exception('offline');
          return _png;
        },
      );
    });
    tearDown(() async => dir.delete(recursive: true));

    test('fetches once, then serves the disk copy while logoUpdatedAt is unchanged', () async {
      expect(await service.resolve('f1', hasLogo: true, updatedAt: 'v1'), _png);
      expect(await service.resolve('f1', hasLogo: true, updatedAt: 'v1'), _png);
      expect(fetches, 1);
      expect(await service.loadCached('f1'), _png);
    });

    test('refetches when logoUpdatedAt changes', () async {
      await service.resolve('f1', hasLogo: true, updatedAt: 'v1');
      await service.resolve('f1', hasLogo: true, updatedAt: 'v2');
      expect(fetches, 2);
    });

    test('keeps the cached logo when the refetch fails (offline)', () async {
      await service.resolve('f1', hasLogo: true, updatedAt: 'v1');
      fail = true;
      expect(await service.resolve('f1', hasLogo: true, updatedAt: 'v2'), _png);
    });

    test('no cache and a failed fetch means no logo (header falls back to the mark)', () async {
      fail = true;
      expect(await service.resolve('f1', hasLogo: true, updatedAt: 'v1'), isNull);
    });

    test('a farm without a logo clears the disk copy', () async {
      await service.resolve('f1', hasLogo: true, updatedAt: 'v1');
      expect(await service.resolve('f1', hasLogo: false), isNull);
      expect(await service.loadCached('f1'), isNull);
    });

    test('logos are kept per farm', () async {
      await service.resolve('f1', hasLogo: true, updatedAt: 'v1');
      expect(await service.loadCached('f2'), isNull);
    });
  });

  group('widgets', () {
    testWidgets('FarmMarkImage shows the Ilevora mark when there is no logo', (tester) async {
      await tester.pumpWidget(const MaterialApp(home: Scaffold(body: FarmMarkImage(bytes: null))));
      expect(find.byType(IlevoraMark), findsOneWidget);
      expect(find.byType(Image), findsNothing);
    });

    testWidgets('FarmMarkImage shows the logo image when it has bytes', (tester) async {
      await tester.pumpWidget(MaterialApp(home: Scaffold(body: FarmMarkImage(bytes: _png))));
      expect(find.byType(Image), findsOneWidget);
      expect(find.byType(IlevoraMark), findsNothing);
    });

    testWidgets('FarmMarkImage falls back to the mark when the bytes are not an image', (tester) async {
      await tester.pumpWidget(MaterialApp(home: Scaffold(body: FarmMarkImage(bytes: Uint8List.fromList(const [1, 2, 3, 4])))));
      await tester.pump();
      expect(find.byType(IlevoraMark), findsOneWidget);
    });

    testWidgets('InvoiceTotalsCard shows subtotal, VAT and total', (tester) async {
      final totals = computeInvoiceTotals(const [InvoiceLine(description: 'x', rate: 1000, quantity: 2)], 7.5);
      await tester.pumpWidget(MaterialApp(theme: IlevoraTheme.theme, home: Scaffold(body: InvoiceTotalsCard(totals: totals, vatPercent: 7.5))));
      expect(find.text('₦2,000.00'), findsOneWidget);
      expect(find.text('VAT (7.5%)'), findsOneWidget);
      expect(find.text('₦150.00'), findsOneWidget);
      expect(find.text('₦2,150.00'), findsOneWidget);
    });
  });

  group('API limits mirrored on the phone', () {
    InvoiceLine line(double rate, double qty) => InvoiceLine(description: 'x', rate: rate, quantity: qty);

    test('rate, quantity and line amount limits (below, not at)', () {
      expect(invoiceLineProblem(1, line(99999999.99, 1)), isNull);
      expect(invoiceLineProblem(1, line(100000000, 1)), contains('rate of item 1 must be below ₦100,000,000'));
      expect(invoiceLineProblem(2, line(1, 1000000)), contains('quantity of item 2 must be below 1,000,000'));
      expect(invoiceLineProblem(3, line(50000000, 20)), contains('amount of item 3'));
      expect(invoiceLineProblem(3, line(50000000, 19.99)), isNull);
      expect(invoiceLineProblem(1, line(-1, 1)), contains('negative'));
      expect(invoiceLineProblem(1, line(1, 0)), contains('more than zero'));
    });

    test('subtotal limit', () {
      final ok = List.generate(10, (_) => line(99999999, 9)); // 899,999,991 each, 8,999,999,910 in all
      expect(invoiceSubtotalProblem(ok), isNull);
      final big = List.generate(12, (_) => line(99999999, 9));
      expect(invoiceSubtotalProblem(big), contains('subtotal must be below ₦10,000,000,000'));
    });

    test('dates must be in 2000..2100', () {
      expect(dateRangeProblem('The invoice date', DateTime(2000, 1, 1)), isNull);
      expect(dateRangeProblem('The invoice date', DateTime(2100, 12, 31)), isNull);
      expect(dateRangeProblem('The invoice date', DateTime(1999, 12, 31)), contains('2000 and 2100'));
      expect(dateRangeProblem('The due date', DateTime(2101, 1, 1)), contains('The due date'));
    });

    test('vendor bill amount limit', () {
      expect(billAmountProblem(0), isNotNull);
      expect(billAmountProblem(999999999999.99), isNull);
      expect(billAmountProblem(1e12), contains('below ₦1,000,000,000,000'));
    });
  });

  group('portrait and wide logos', () {
    Future<Uint8List> makePng(WidgetTester t, int w, int h) async {
      late Uint8List out;
      await t.runAsync(() async {
        final rec = ui.PictureRecorder();
        Canvas(rec).drawRect(Rect.fromLTWH(0, 0, w.toDouble(), h.toDouble()), Paint()..color = const Color(0xFF2D7A4F));
        final img = await rec.endRecording().toImage(w, h);
        out = (await img.toByteData(format: ui.ImageByteFormat.png))!.buffer.asUint8List();
      });
      return out;
    }

    for (final dims in [(1000, 4000), (4000, 1000), (4096, 4096)]) {
      testWidgets('${dims.$1}x${dims.$2} logo is decoded small and drawn without error', (t) async {
        final png = await makePng(t, dims.$1, dims.$2);
        PaintingBinding.instance.imageCache.clear();
        await t.pumpWidget(MaterialApp(home: Scaffold(body: Center(child: FarmMarkImage(bytes: png, size: 32)))));
        await t.runAsync(() => Future<void>.delayed(const Duration(seconds: 2)));
        await t.pump();
        expect(t.takeException(), isNull);
        expect(find.byType(IlevoraMark), findsNothing, reason: 'the logo, not the fallback mark, is shown');
        expect(PaintingBinding.instance.imageCache.currentSizeBytes, lessThan(1024 * 1024));
      });
    }
  });
}
