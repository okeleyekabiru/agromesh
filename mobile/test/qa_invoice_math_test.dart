// QA (independent) tests: client-side invoice maths vs the server's decimal rounding (half away from zero, 2dp).
import 'package:ilevora_mobile/models/billing_models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  // Reference implementation with exact integer arithmetic on decimal strings (what C# decimal does).
  // value given as (units of 1e-6) to avoid floats: rate 1.005 -> 1005000.
  int roundHalfAwayCents(int scaled6) {
    // scaled6 is value * 1e6; round to 2dp = value*1e2
    final neg = scaled6 < 0;
    final a = scaled6.abs();
    var cents = a ~/ 10000;
    if (a % 10000 >= 5000) cents++;
    return neg ? -cents : cents;
  }

  test('[CONFIRMED BUG] line amount: halves round away from zero like the server (rate 1.005 x 1 -> 1.01)', () {
    // server: 1.005m * 1m = 1.005 -> 1.01
    expect(roundHalfAwayCents(1005000), 101);
    final client = const InvoiceLine(description: 'x', rate: 1.005, quantity: 1).amount;
    expect(client, 1.01, reason: 'client preview differs from server because 1.005*100 = 100.49999999999999 in binary');
  });

  test('line amount: 2.675 x 1 -> 2.68 on the server', () {
    expect(roundHalfAwayCents(2675000), 268);
    expect(const InvoiceLine(description: 'x', rate: 2.675, quantity: 1).amount, 2.68);
  });

  test('[CONFIRMED BUG] line amount: 0.285 x 1 -> 0.29 on the server', () {
    expect(const InvoiceLine(description: 'x', rate: 0.285, quantity: 1).amount, 0.29);
  });

  test('integers and 2dp values are exact', () {
    expect(const InvoiceLine(description: 'x', rate: 1250.50, quantity: 3).amount, 3751.5);
    expect(const InvoiceLine(description: 'x', rate: 0.1, quantity: 3).amount, 0.3);
  });

  test('VAT 0 and 100', () {
    const l = [InvoiceLine(description: 'x', rate: 100.10, quantity: 1)];
    expect(computeInvoiceTotals(l, 0).total, 100.10);
    final t = computeInvoiceTotals(l, 100);
    expect(t.vatAmount, 100.10);
    expect(t.total, 200.20);
  });

  test('VAT half-cent: subtotal 0.05 x 10% = 0.005 -> 0.01 on the server', () {
    const l = [InvoiceLine(description: 'x', rate: 0.05, quantity: 1)];
    expect(computeInvoiceTotals(l, 10).vatAmount, 0.01);
  });

  test('VAT 7.5% on 10.10 = 0.7575 -> 0.76', () {
    const l = [InvoiceLine(description: 'x', rate: 10.10, quantity: 1)];
    expect(computeInvoiceTotals(l, 7.5).vatAmount, 0.76);
  });

  test('200 lines of 0.01 x 1 sum to exactly 2.00', () {
    final l = List.generate(200, (_) => const InvoiceLine(description: 'x', rate: 0.01, quantity: 1));
    expect(computeInvoiceTotals(l, 0).subtotal, 2.0);
  });

  test('dateOnly pads and is locale/timezone independent', () {
    expect(dateOnly(DateTime(2026, 1, 2)), '2026-01-02');
    expect(dateOnly(DateTime(5, 1, 2)), '0005-01-02');
  });

  test('due-date default of date + 14 days stays on calendar day (local midnight add Duration)', () {
    // On a machine whose local zone has no DST change inside the window this is exact; documents the construct used in
    // invoice_form_screen.dart:96 (`_date.add(Duration(days: 14))`) which is NOT calendar-safe across a DST change.
    final d = DateTime(2026, 3, 5);
    expect(dateOnly(d.add(const Duration(days: 14))), '2026-03-19');
  });
}
