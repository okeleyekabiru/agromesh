import type { VendorInvoice } from '@/types'
import { amountsDiffer, formatNaira, round2 } from '@/utils/money'

/**
 * A short warning when the bill does not match what the order cost, or null when it does.
 * Takes plain numbers so it can be used on a saved bill and on a form still being filled in.
 */
export function mismatchHint(billAmount: number, orderTotal: number | null | undefined): string | null {
  if (orderTotal == null || !Number.isFinite(billAmount) || billAmount <= 0) return null
  if (!amountsDiffer(billAmount, orderTotal)) return null
  const diff = round2(Math.abs(billAmount - orderTotal))
  const dir = billAmount > orderTotal ? 'more' : 'less'
  return `This bill is ${formatNaira(billAmount)}, ${formatNaira(diff)} ${dir} than the order total of ${formatNaira(orderTotal)}.`
}

export function vendorMismatch(inv: Pick<VendorInvoice, 'amount' | 'orderTotal'>): string | null {
  return mismatchHint(inv.amount, inv.orderTotal)
}

/** A bill is only flagged overdue while it is still unpaid. */
export function isShownOverdue(inv: Pick<VendorInvoice, 'isOverdue' | 'status'>): boolean {
  return inv.isOverdue && inv.status === 'Unpaid'
}

export const MAX_VENDOR_AMOUNT = 1_000_000_000_000 // the server needs < 1e12

/**
 * Why a typed vendor-bill amount cannot be sent, or null when it is fine. The value comes from a
 * number input, so non-numeric text arrives as ''.
 */
export function vendorAmountError(value: number | ''): string | null {
  if (value === '' || typeof value !== 'number' || !Number.isFinite(value)) {
    return 'Enter the amount as a plain number, for example 25000.50.'
  }
  if (value <= 0) return 'Enter an amount greater than 0.'
  if (value >= MAX_VENDOR_AMOUNT) return `The amount must be less than ${MAX_VENDOR_AMOUNT.toLocaleString('en-NG')}.`
  const cents = value * 100
  if (Math.abs(cents - Math.round(cents)) > 1e-6) return 'The amount can have at most 2 decimal places.'
  if (Math.round(cents) === 0) return 'Enter an amount of at least 0.01.'
  return null
}

/** A plain decimal string with 2 places and never an exponent ("1e-7" or "1e+21" would be rejected by the API). */
export function formatAmountForUpload(value: number): string {
  return value.toFixed(2)
}
