import { describe, expect, it } from 'vitest'
import { addDays, amountsDiffer, computeInvoiceTotals, formatDay, formatNaira, isDateInRange, round2, toDateInput } from './money'

describe('round2', () => {
  it('rounds halves away from zero without the 1.005 floating point trap', () => {
    expect(round2(1.005)).toBe(1.01)
    expect(round2(2.675)).toBe(2.68)
    expect(round2(-1.005)).toBe(-1.01)
    expect(round2(0.1 * 3)).toBe(0.3)
  })
  it('treats non-finite values as 0', () => {
    expect(round2(NaN)).toBe(0)
    expect(round2(Infinity)).toBe(0)
  })
  it('does not break on huge values (no exponent-notation NaN)', () => {
    expect(round2(1e21)).toBe(1e21)
    expect(round2(5e30)).toBe(5e30)
    expect(computeInvoiceTotals([{ description: 'x', rate: 1e12, quantity: 1e12 }], 0).total).toBe(1e24)
  })
})

describe('computeInvoiceTotals', () => {
  it('computes line amounts, subtotal, VAT on the subtotal and total', () => {
    const t = computeInvoiceTotals(
      [
        { description: 'Eggs', rate: 1500.5, quantity: 3 },
        { description: 'Manure', rate: 200, quantity: 2.5 }
      ],
      7.5
    )
    expect(t.lines.map(l => l.amount)).toEqual([4501.5, 500])
    expect(t.lines.map(l => l.serialNumber)).toEqual([1, 2])
    expect(t.subtotal).toBe(5001.5)
    expect(t.vatAmount).toBe(375.11) // 5001.5 x 7.5% = 375.1125
    expect(t.total).toBe(5376.61)
  })
  it('rounds each line to 2dp before summing', () => {
    const t = computeInvoiceTotals([{ description: 'x', rate: 0.333, quantity: 3 }, { description: 'y', rate: 0.333, quantity: 3 }], 0)
    expect(t.lines.map(l => l.amount)).toEqual([1, 1])
    expect(t.total).toBe(2)
  })
  it('ignores blank rate/quantity and blank VAT', () => {
    const t = computeInvoiceTotals([{ description: '', rate: '', quantity: '' }], '')
    expect(t).toMatchObject({ subtotal: 0, vatAmount: 0, total: 0 })
  })
  it('handles 0% and 100% VAT', () => {
    const lines = [{ description: 'a', rate: 100, quantity: 1 }]
    expect(computeInvoiceTotals(lines, 0).total).toBe(100)
    expect(computeInvoiceTotals(lines, 100).total).toBe(200)
  })
})

describe('isDateInRange', () => {
  it('accepts real days from 2000 to 2100 only', () => {
    expect(isDateInRange('2000-01-01')).toBe(true)
    expect(isDateInRange('2100-12-31')).toBe(true)
    expect(isDateInRange('1999-12-31')).toBe(false)
    expect(isDateInRange('2101-01-01')).toBe(false)
    expect(isDateInRange('202026-03-01')).toBe(false) // a year typo the date input allows
    expect(isDateInRange('2026-02-30')).toBe(false)
    expect(isDateInRange('')).toBe(false)
  })
})

describe('dates and money formatting', () => {
  it('adds days across month and year ends', () => {
    expect(addDays('2026-01-20', 14)).toBe('2026-02-03')
    expect(addDays('2026-12-25', 14)).toBe('2027-01-08')
    expect(addDays('2028-02-20', 14)).toBe('2028-03-05') // leap year
  })
  it('formats a local date as yyyy-MM-dd', () => {
    expect(toDateInput(new Date(2026, 2, 5))).toBe('2026-03-05')
  })
  it('formats day-only strings without time zone drift', () => {
    expect(formatDay('2026-03-05')).toBe('5 Mar 2026')
    // A timestamp is an instant: it shows the viewer's local calendar day, not the UTC one.
    const fmt = (d: Date) => d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
    expect(formatDay('2026-10-31T23:30:00Z')).toBe(fmt(new Date('2026-10-31T23:30:00Z')))
    // No offset on the timestamp means UTC (the API's convention), so it matches the Z form.
    expect(formatDay('2026-10-31T23:30:00')).toBe(fmt(new Date('2026-10-31T23:30:00Z')))
    expect(formatDay('2026-10-31T23:30:00+01:00')).toBe(fmt(new Date('2026-10-31T22:30:00Z')))
    expect(formatDay(null)).toBe('-')
    expect(formatDay('not a date')).toBe('-')
  })
  it('formats naira with two decimals', () => {
    expect(formatNaira(12345.6)).toBe('₦12,345.60')
    expect(formatNaira(0)).toBe('₦0.00')
    expect(formatNaira(null)).toBe('₦0.00')
  })
  it('detects amounts that differ by at least a kobo', () => {
    expect(amountsDiffer(100, 100)).toBe(false)
    expect(amountsDiffer(100.004, 100)).toBe(false)
    expect(amountsDiffer(100.01, 100)).toBe(true)
  })
})
