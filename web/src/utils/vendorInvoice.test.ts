import { describe, expect, it } from 'vitest'
import { formatAmountForUpload, isShownOverdue, mismatchHint, vendorAmountError, vendorMismatch } from './vendorInvoice'
import { extractProblemMessage } from './http'

describe('mismatchHint', () => {
  it('is null when the bill matches the order total', () => {
    expect(mismatchHint(50000, 50000)).toBeNull()
    expect(mismatchHint(50000.004, 50000)).toBeNull()
  })
  it('explains how far off the bill is, and in which direction', () => {
    expect(mismatchHint(52000, 50000)).toBe('This bill is ₦52,000.00, ₦2,000.00 more than the order total of ₦50,000.00.')
    expect(mismatchHint(48000.5, 50000)).toMatch(/₦1,999.50 less than/)
  })
  it('stays quiet when there is nothing to compare', () => {
    expect(mismatchHint(0, 100)).toBeNull()
    expect(mismatchHint(NaN, 100)).toBeNull()
    expect(mismatchHint(100, null)).toBeNull()
  })
  it('works on a saved vendor invoice', () => {
    expect(vendorMismatch({ amount: 10, orderTotal: 10 })).toBeNull()
    expect(vendorMismatch({ amount: 10, orderTotal: 20 })).not.toBeNull()
  })
})

describe('vendorAmountError / formatAmountForUpload', () => {
  it('accepts ordinary amounts', () => {
    expect(vendorAmountError(25000.5)).toBeNull()
    expect(vendorAmountError(0.01)).toBeNull()
    expect(vendorAmountError(999_999_999_999.99)).toBeNull()
  })
  it('explains non-numeric, zero, negative, too large and too precise input', () => {
    expect(vendorAmountError('')).toMatch(/plain number/)
    expect(vendorAmountError(NaN)).toMatch(/plain number/)
    expect(vendorAmountError(0)).toMatch(/greater than 0/)
    expect(vendorAmountError(-5)).toMatch(/greater than 0/)
    expect(vendorAmountError(1e12)).toMatch(/less than 1,000,000,000,000/)
    expect(vendorAmountError(1e21)).toMatch(/less than/)
    expect(vendorAmountError(0.0000001)).toMatch(/2 decimal places/)
    expect(vendorAmountError(10.005)).toMatch(/2 decimal places/)
  })
  it('formats a plain 2dp decimal, never an exponent', () => {
    expect(formatAmountForUpload(50000)).toBe('50000.00')
    expect(formatAmountForUpload(0.1 + 0.2)).toBe('0.30')
    expect(formatAmountForUpload(123456789012.5)).toBe('123456789012.50')
    expect(formatAmountForUpload(1e11)).not.toMatch(/e/i)
  })
})

describe('isShownOverdue', () => {
  it('only flags unpaid bills', () => {
    expect(isShownOverdue({ isOverdue: true, status: 'Unpaid' })).toBe(true)
    expect(isShownOverdue({ isOverdue: true, status: 'Paid' })).toBe(false)
    expect(isShownOverdue({ isOverdue: false, status: 'Unpaid' })).toBe(false)
  })
})

describe('extractProblemMessage', () => {
  const err = (data: unknown) => ({ response: { data } })
  it('prefers the 422 detail', () => {
    expect(extractProblemMessage(err({ detail: 'Order is Placed', title: 'x' }), 'fb')).toBe('Order is Placed')
  })
  it('joins 400 field errors', () => {
    expect(extractProblemMessage(err({ errors: { Amount: ['Too small'], File: ['Missing'] } }), 'fb')).toBe('Too small Missing')
  })
  it('falls back to title, then the fallback', () => {
    expect(extractProblemMessage(err({ title: 'Payload Too Large' }), 'fb')).toBe('Payload Too Large')
    expect(extractProblemMessage(new Error('network'), 'fb')).toBe('fb')
  })
})
