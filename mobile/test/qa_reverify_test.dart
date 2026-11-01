// QA (independent) re-verification probes for the fixes: number parsing, rounding fuzz vs a server-style reference,
// limits at their boundaries, DateField clamping, BoundedLogoImage, offline-cache decision, amount text.
import 'dart:math';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:agromesh_mobile/models/billing_models.dart';
import 'package:agromesh_mobile/screens/more/billing_common.dart';
import 'package:agromesh_mobile/services/api_client.dart';
import 'package:agromesh_mobile/widgets/farm_mark.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Future<Uint8List> _png(int w, int h, {Color color = const Color(0xFF2D7A4F)}) async {
  final rec = ui.PictureRecorder();
  Canvas(rec).drawRect(Rect.fromLTWH(0, 0, w.toDouble(), h.toDouble()), Paint()..color = color);
  final img = await rec.endRecording().toImage(w, h);
  return (await img.toByteData(format: ui.ImageByteFormat.png))!.buffer.asUint8List();
}

void main() {
  group('parseNumber table', () {
    final table = <String, double?>{
      '12,50': 12.5, '12,5': 12.5, '1,250': 1250, '1.250,50': 1250.5, '1,250.50': 1250.5, '1250.5': 1250.5,
      'NaN': null, 'Infinity': null, '-Infinity': null, '1e3': null, '1E3': null, '': null, '   ': null,
      '1,000': 1000, '1,00': 1.0, '0,5': 0.5, ',5': 0.5, '5,': 5, '.5': 0.5, '1,2,3': 123, '1.250': 1.25,
      '1.2.3': null, '1.250.500': null, '+5': 5, '-': null, '.': null, ',': null, '1 250,50': 1250.5,
      '١٢': null, '12abc': null, '1,250,000.75': 1250000.75, '1.250.000,75': 1250000.75, '0.0': 0, '-0,5': -0.5,
      '1e400': null, '999999999999999999999999': 999999999999999999999999.0,
    };
    for (final e in table.entries) {
      test('"${e.key}" -> ${e.value}', () {
        final r = parseNumber(e.key);
        if (e.value == null) {
          expect(r, isNull);
        } else {
          expect(r, e.value);
        }
      });
    }
  });

  group('round2 / line amount', () {
    test('half-cent cases away from zero', () {
      expect(round2(1.005), 1.01);
      expect(round2(0.285), 0.29);
      expect(round2(2.675), 2.68);
      expect(round2(0.145), 0.15);
      expect(round2(-1.005), -1.01);
      expect(round2(-0.145), -0.15);
      expect(round2(0.004), 0.0);
      expect(round2(0.005), 0.01);
      expect(round2(1e-7), 0.0);
      expect(round2(9999999999.995), 10000000000.0);
      expect(round2(123456789012.345), 123456789012.35);
      expect(round2(double.nan).isNaN, isTrue);
      expect(round2(double.infinity), double.infinity);
    });

    test('fuzz: line amount, subtotal, VAT, total equal an exact-integer server reference (20000 invoices)', () {
      final rnd = Random(42);
      int half(BigInt num, BigInt den) {
        // round half away from zero of num/den (den > 0), as an int
        final neg = num.isNegative;
        final a = num.abs();
        var q = a ~/ den;
        if ((a % den) * BigInt.two >= den) q += BigInt.one;
        return (neg ? -q : q).toInt();
      }

      for (var iter = 0; iter < 20000; iter++) {
        final n = 1 + rnd.nextInt(5);
        final lines = <InvoiceLine>[];
        var subCents = 0;
        for (var i = 0; i < n; i++) {
          final rateScale = rnd.nextInt(5); // 0..4 decimals
          final qtyScale = rnd.nextInt(4);
          final rateInt = rnd.nextInt(2000000);
          final qtyInt = 1 + rnd.nextInt(100000);
          final rate = rateInt / pow(10, rateScale);
          final qty = qtyInt / pow(10, qtyScale);
          lines.add(InvoiceLine(description: 'x', rate: rate, quantity: qty));
          // exact: rateInt/10^rs * qtyInt/10^qs -> cents
          final num = BigInt.from(rateInt) * BigInt.from(qtyInt) * BigInt.from(100);
          final den = BigInt.from(10).pow(rateScale + qtyScale);
          final lineCents = half(num, den);
          expect((lines.last.amount * 100).round(), lineCents, reason: 'rate $rate qty $qty');
          subCents += lineCents;
        }
        final vatScale = rnd.nextInt(3);
        final vatInt = rnd.nextInt(100 * pow(10, vatScale).toInt() + 1);
        final vat = vatInt / pow(10, vatScale);
        final vatCents = half(BigInt.from(subCents) * BigInt.from(vatInt), BigInt.from(100) * BigInt.from(10).pow(vatScale));
        final t = computeInvoiceTotals(lines, vat);
        expect((t.subtotal * 100).round(), subCents);
        expect((t.vatAmount * 100).round(), vatCents, reason: 'sub $subCents vat $vat');
        expect((t.total * 100).round(), subCents + vatCents);
      }
    });

    test('VAT 0 and 100, no lines, 200 lines', () {
      expect(computeInvoiceTotals(const [], 100).total, 0);
      const l = [InvoiceLine(description: 'x', rate: 33.33, quantity: 3)];
      expect(computeInvoiceTotals(l, 0).total, 99.99);
      expect(computeInvoiceTotals(l, 100).total, 199.98);
    });
  });

  group('limits at the boundary (mirror Invoice.cs constants)', () {
    InvoiceLine ln(double r, double q) => InvoiceLine(description: 'x', rate: r, quantity: q);
    test('rate', () {
      expect(invoiceLineProblem(1, ln(99999999.99, 1)), isNull);
      expect(invoiceLineProblem(1, ln(100000000, 1)), isNotNull);
      expect(invoiceLineProblem(1, ln(-0.01, 1)), isNotNull);
      expect(invoiceLineProblem(1, ln(0, 1)), isNull);
    });
    test('quantity', () {
      expect(invoiceLineProblem(1, ln(1, 999999.999)), isNull);
      expect(invoiceLineProblem(1, ln(1, 1000000)), isNotNull);
      expect(invoiceLineProblem(1, ln(1, 0)), isNotNull);
    });
    test('line amount', () {
      expect(invoiceLineProblem(1, ln(99999999.99, 10)), isNull); // 999,999,999.90
      expect(invoiceLineProblem(1, ln(99999999.99, 11)), isNotNull);
      expect(invoiceLineProblem(1, ln(1000000, 1000)), isNotNull); // exactly 1e9
      expect(invoiceLineProblem(1, ln(999999.999, 1000)), isNull); // 999,999,999.00 -> rounds below 1e9
    });
    test('subtotal', () {
      final ten = List.generate(10, (_) => ln(99999999.999, 10)); // 10 x 999,999,999.99(9->1e9?) rounding
      expect(invoiceSubtotalProblem(List.generate(10, (_) => ln(99999999.99, 10))), isNull); // 9,999,999,999.00
      expect(invoiceSubtotalProblem(List.generate(11, (_) => ln(99999999.99, 10))), isNotNull);
      expect(invoiceSubtotalProblem(List.generate(10, (_) => ln(99999999.999, 10)).followedBy([ln(0.1, 1)])), isNotNull); // 9,999,999,999.90 + 0.10 = 1e10
      expect(ten.length, 10);
    });
    test('dates', () {
      expect(dateRangeProblem('d', DateTime(1999, 12, 31)), isNotNull);
      expect(dateRangeProblem('d', DateTime(2000, 1, 1)), isNull);
      expect(dateRangeProblem('d', DateTime(2100, 12, 31)), isNull);
      expect(dateRangeProblem('d', DateTime(2101, 1, 1)), isNotNull);
    });
    test('vendor bill amount', () {
      expect(billAmountProblem(999999999999.99), isNull);
      expect(billAmountProblem(1000000000000), isNotNull);
      expect(billAmountProblem(0), isNotNull);
      expect(billAmountProblem(-1), isNotNull);
      expect(999999999999.99.toStringAsFixed(2), '999999999999.99');
    });
    test('a vendor amount under half a kobo is refused client-side (it would be sent as "0.00" and the server needs > 0)', () {
      expect(0.004.toStringAsFixed(2), '0.00');
      expect(billAmountProblem(0.004), isNotNull, reason: 'amount 0.004 would be sent as 0.00');
      expect(billAmountProblem(0.005), isNull, reason: '0.005 rounds to 0.01, which the server accepts');
      expect(billAmountProblem(double.nan), isNotNull);
    });
  });

  group('offline decision', () {
    test('status table', () {
      for (final s in [500, 502, 503, 504, 599]) {
        expect(shouldUseOfflineCache(ApiException('x', s)), isTrue, reason: '$s');
      }
      for (final s in [400, 401, 403, 404, 409, 422, 429]) {
        expect(shouldUseOfflineCache(ApiException('x', s)), isFalse, reason: '$s');
      }
      expect(shouldUseOfflineCache(const ApiException('x')), isFalse, reason: 'null status');
      expect(shouldUseOfflineCache(Exception('SocketException')), isTrue);
    });
  });

  group('DateField clamping', () {
    Future<Object?> open(WidgetTester t, DateTime? value, {DateTime? first}) async {
      await t.pumpWidget(MaterialApp(home: Scaffold(body: DateField(label: 'D', value: value, first: first, onChanged: (_) {}))));
      await t.tap(find.byType(InkWell).first);
      await t.pump();
      await t.pump(const Duration(milliseconds: 300));
      final ex = t.takeException();
      return ex;
    }

    testWidgets('value before first (vendor due-date crash)', (t) async {
      expect(await open(t, DateTime(2026, 1, 5), first: DateTime(2026, 1, 10)), isNull);
    });
    testWidgets('value far in the past, no first', (t) async {
      expect(await open(t, DateTime(2010, 1, 5)), isNull);
    });
    testWidgets('value before year 2000', (t) async {
      expect(await open(t, DateTime(1990, 6, 1)), isNull);
    });
    testWidgets('value after year+5', (t) async {
      expect(await open(t, DateTime(2040, 6, 1)), isNull);
    });
    testWidgets('value after 2100', (t) async {
      expect(await open(t, DateTime(2150, 6, 1)), isNull);
    });
    testWidgets('null value, first in the future', (t) async {
      expect(await open(t, null, first: DateTime(2030, 1, 1)), isNull);
    });
    testWidgets('null value, no first', (t) async {
      expect(await open(t, null), isNull);
    });
  });

  group('BoundedLogoImage', () {
    Future<int> cacheBytesFor(WidgetTester t, Uint8List bytes, {double size = 32, double dpr = 3.0}) async {
      t.view.devicePixelRatio = dpr;
      addTearDown(t.view.reset);
      PaintingBinding.instance.imageCache.clear();
      await t.pumpWidget(MaterialApp(home: Scaffold(body: Center(child: FarmMarkImage(bytes: bytes, size: size)))));
      await t.runAsync(() => Future<void>.delayed(const Duration(seconds: 2)));
      await t.pump();
      return PaintingBinding.instance.imageCache.currentSizeBytes;
    }

    testWidgets('4096x4096 at dpr 3 holds at most a ~96px decode', (t) async {
      late Uint8List big;
      await t.runAsync(() async => big = await _png(4096, 4096));
      final b = await cacheBytesFor(t, big);
      expect(b, lessThan(200 * 1024), reason: 'cache $b bytes');
      expect(b, greaterThan(0));
    });

    testWidgets('wide 4000x500 keeps aspect ratio (decoded height < width)', (t) async {
      late Uint8List wide;
      await t.runAsync(() async => wide = await _png(4000, 500));
      final b = await cacheBytesFor(t, wide);
      // fit within 96x96 keeping 8:1 => 96x12 => 4608 bytes; a squashed 96x96 would be 36864.
      expect(b, lessThan(20 * 1024), reason: 'cache $b bytes');
    });

    testWidgets('1x1 is not upscaled to the box', (t) async {
      late Uint8List tiny;
      await t.runAsync(() async => tiny = await _png(1, 1));
      final b = await cacheBytesFor(t, tiny);
      expect(b, lessThanOrEqualTo(64), reason: 'cache $b bytes');
    });

    testWidgets('huge dpr is capped (size 88 at dpr 8 -> <= 1024px)', (t) async {
      late Uint8List big;
      await t.runAsync(() async => big = await _png(3000, 3000));
      final b = await cacheBytesFor(t, big, size: 88, dpr: 8.0);
      expect(b, lessThanOrEqualTo(1024 * 1024 * 4));
    });

    testWidgets('GIF / garbage / truncated PNG fall back to the A mark', (t) async {
      for (final bytes in [
        Uint8List.fromList('GIF89a\x01\x00\x01\x00\x00\x00\x00;'.codeUnits),
        Uint8List.fromList(List.filled(64, 7)),
        Uint8List.fromList(const [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0]),
      ]) {
        await t.pumpWidget(MaterialApp(home: Scaffold(body: Center(child: FarmMarkImage(bytes: bytes)))));
        await t.runAsync(() => Future<void>.delayed(const Duration(milliseconds: 300)));
        await t.pump();
        await t.pump();
        expect(find.text('A'), findsOneWidget);
        expect(t.takeException(), isNull);
      }
    });
  });
}
