// Number and date helpers shared by the invoice screens.

/**
 * Rounds to 2 decimals, halves away from zero, the way the API does. The exponent trick avoids the
 * classic 1.005 * 100 = 100.49999999999999 trap of Math.round(x * 100) / 100.
 */
export function round2(value: number): number {
  if (!Number.isFinite(value)) return 0
  const abs = Math.abs(value)
  // Beyond 2^53 / 100 a double has no cents left, and toFixed would switch to exponent notation at 1e21.
  if (abs >= 1e15) return value
  const scaled = Number(`${abs.toFixed(10)}e2`)
  const rounded = Number(`${Math.round(scaled)}e-2`)
  return value < 0 ? -rounded : rounded
}

export interface DraftLine { description: string; rate: number | ''; quantity: number | '' }
export interface LineTotal { serialNumber: number; amount: number }
export interface InvoiceTotals {
  lines: LineTotal[]
  subtotal: number
  vatAmount: number
  total: number
}

/** Mirrors the server: amount = rate x quantity (2dp per line), VAT on the subtotal. Preview only. */
export function computeInvoiceTotals(items: DraftLine[], vatPercent: number | ''): InvoiceTotals {
  const lines = items.map((l, i) => {
    const rate = Number(l.rate)
    const qty = Number(l.quantity)
    return { serialNumber: i + 1, amount: round2((Number.isFinite(rate) ? rate : 0) * (Number.isFinite(qty) ? qty : 0)) }
  })
  const subtotal = round2(lines.reduce((s, l) => s + l.amount, 0))
  const vat = Number(vatPercent)
  const vatAmount = round2(subtotal * (Number.isFinite(vat) ? vat : 0) / 100)
  return { lines, subtotal, vatAmount, total: round2(subtotal + vatAmount) }
}

/** ₦12,345.60 */
export function formatNaira(value: number | null | undefined): string {
  const n = Number(value ?? 0)
  return '₦' + n.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const pad = (n: number) => String(n).padStart(2, '0')

/** Local calendar date as yyyy-MM-dd. */
export function toDateInput(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** yyyy-MM-dd plus N days, in calendar terms (no time-zone drift). */
export function addDays(isoDate: string, days: number): string {
  const [y, m, d] = isoDate.split('-').map(Number)
  if (!y || !m || !d) return isoDate
  return toDateInput(new Date(y, m - 1, d + days))
}

const DAY_ONLY = /^(\d{4})-(\d{2})-(\d{2})$/

/**
 * "12 Mar 2026". A date-only value (yyyy-MM-dd: invoice date, due date) is a calendar day and is shown as
 * written, in any time zone. A timestamp (paidAt, createdAt) is an instant, so it is shown as the user's
 * local calendar day: 23:30 UTC on 31 Oct is 1 Nov in Lagos.
 */
export function formatDay(value: string | null | undefined): string {
  if (!value) return '-'
  const m = DAY_ONLY.exec(value)
  let date: Date
  if (m) {
    date = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]))
  } else {
    // The API sends UTC; a timestamp without an offset is read as UTC rather than as local time.
    const hasZone = /(Z|[+-]\d{2}:?\d{2})$/i.test(value)
    date = new Date(/T/.test(value) && !hasZone ? value + 'Z' : value)
  }
  if (Number.isNaN(date.getTime())) return '-'
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

export const MIN_DATE = '2000-01-01'
export const MAX_DATE = '2100-12-31'

/** A real yyyy-MM-dd calendar day in the years 2000 to 2100 (what the API accepts). */
export function isDateInRange(value: string): boolean {
  const m = DAY_ONLY.exec(value)
  if (!m) return false
  const y = Number(m[1]), mo = Number(m[2]), d = Number(m[3])
  if (y < 2000 || y > 2100) return false
  const check = new Date(y, mo - 1, d)
  return check.getFullYear() === y && check.getMonth() === mo - 1 && check.getDate() === d
}

/** True when the bill and the order differ by at least a kobo. */
export function amountsDiffer(billAmount: number, orderTotal: number): boolean {
  return Math.abs(round2(billAmount) - round2(orderTotal)) >= 0.01
}
