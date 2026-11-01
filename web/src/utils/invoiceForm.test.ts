import { describe, expect, it } from 'vitest'
import { buildInvoiceRequest, emptyInvoiceForm, invoiceToForm, prefillPayment, validateInvoiceForm } from './invoiceForm'
import type { Invoice } from '@/types'

function valid() {
  const f = emptyInvoiceForm('2026-03-01')
  f.billToName = 'Green Market Ltd'
  f.lineItems = [{ description: 'Palm oil 25L', rate: 45000, quantity: 2 }]
  return f
}

describe('emptyInvoiceForm', () => {
  it('starts today, due in 14 days, with one blank line and 0% VAT', () => {
    const f = emptyInvoiceForm('2026-03-01')
    expect(f.date).toBe('2026-03-01')
    expect(f.dueDate).toBe('2026-03-15')
    expect(f.lineItems).toHaveLength(1)
    expect(f.vatPercent).toBe(0)
    expect(f.templateKey).toBe('ngf')
  })
})

describe('validateInvoiceForm', () => {
  it('passes a complete form', () => {
    expect(validateInvoiceForm(valid())).toEqual([])
  })
  it('requires a bill-to name', () => {
    const f = valid(); f.billToName = '   '
    expect(validateInvoiceForm(f)).toContain('Enter who the invoice is billed to.')
  })
  it('rejects a due date before the invoice date', () => {
    const f = valid(); f.dueDate = '2026-02-28'
    expect(validateInvoiceForm(f).join()).toMatch(/due date cannot be before/)
  })
  it('checks each line: description, rate >= 0, quantity > 0', () => {
    const f = valid()
    f.lineItems = [
      { description: '', rate: 10, quantity: 1 },
      { description: 'ok', rate: -1, quantity: 1 },
      { description: 'ok', rate: 0, quantity: 0 },
      { description: 'free item', rate: 0, quantity: 1 }
    ]
    const errs = validateInvoiceForm(f)
    expect(errs).toContain('Line 1: enter a description.')
    expect(errs).toContain('Line 2: enter a rate of 0 or more.')
    expect(errs).toContain('Line 3: enter a quantity greater than 0.')
    expect(errs.some(e => e.startsWith('Line 4'))).toBe(false)
  })
  it('requires at least one line item', () => {
    const f = valid(); f.lineItems = []
    expect(validateInvoiceForm(f)).toContain('Add at least one line item.')
  })
  it('limits VAT to 0..100 and checks the email shape', () => {
    const f = valid(); f.vatPercent = 101; f.billToEmail = 'nope'
    const errs = validateInvoiceForm(f)
    expect(errs.join()).toMatch(/VAT must be between 0 and 100/)
    expect(errs.join()).toMatch(/valid bill-to email/)
    f.vatPercent = 100; f.billToEmail = 'a@b.co'
    expect(validateInvoiceForm(f)).toEqual([])
  })
})

describe('validateInvoiceForm server limits and dates', () => {
  it.each([
    [100_000_000, 1, /rate must be less than 100,000,000/],
    [1, 1_000_000, /quantity must be less than 1,000,000/],
    [99_999_999, 999_999, /rate x quantity must be less than 1,000,000,000/]
  ])('rejects rate %s x quantity %s', (rate, quantity, message) => {
    const f = valid(); f.lineItems = [{ description: 'x', rate, quantity }]
    expect(validateInvoiceForm(f).join()).toMatch(message)
  })
  it('accepts values just under the limits', () => {
    const f = valid(); f.lineItems = [{ description: 'x', rate: 99_999_999.99, quantity: 9 }]
    expect(validateInvoiceForm(f)).toEqual([])
  })
  it('rejects a subtotal of 10 billion or more', () => {
    const f = valid()
    f.lineItems = Array.from({ length: 11 }, () => ({ description: 'x', rate: 99_000_000, quantity: 10 }))
    expect(validateInvoiceForm(f).join()).toMatch(/subtotal must be less than 10,000,000,000/)
  })
  it('does not misreport huge numbers as a zero total', () => {
    const f = valid(); f.lineItems = [{ description: 'x', rate: 1e30, quantity: 1 }]
    expect(validateInvoiceForm(f).join()).toMatch(/rate must be less than/)
  })
  it('rejects dates outside 2000-2100, such as a 6-digit year typo', () => {
    const f = valid(); f.date = '202026-03-01'; f.dueDate = '202026-03-15'
    const errs = validateInvoiceForm(f)
    expect(errs.some(e => /invoice date must be a real date between 1 Jan 2000 and 31 Dec 2100/.test(e))).toBe(true)
    expect(errs.some(e => /due date must be a real date/.test(e))).toBe(true)
    const g = valid(); g.date = '1999-12-31'
    expect(validateInvoiceForm(g).join()).toMatch(/invoice date must be a real date/)
  })
})

describe('buildInvoiceRequest', () => {
  it('trims text, turns blanks into null and numbers into numbers', () => {
    const f = valid()
    f.billToName = '  Green Market Ltd '
    f.billToPhone = '  '
    f.vatPercent = ''
    f.signatoryName = ' A. Bello '
    const body = buildInvoiceRequest(f)
    expect(body.billToName).toBe('Green Market Ltd')
    expect(body.billToPhone).toBeNull()
    expect(body.vatPercent).toBe(0)
    expect(body.signatoryName).toBe('A. Bello')
    expect(body.lineItems).toEqual([{ description: 'Palm oil 25L', rate: 45000, quantity: 2 }])
    expect(body.date).toBe('2026-03-01')
    expect(body.templateKey).toBe('ngf')
  })
})

describe('prefillPayment', () => {
  it('copies bank details from the business profile only into empty fields', () => {
    const f = emptyInvoiceForm('2026-03-01')
    f.paymentBankName = 'Typed Bank'
    prefillPayment(f, { bankAccountName: 'Farm Ltd', bankAccountNumber: '0123456789', bankName: 'Profile Bank' })
    expect(f.paymentAccountName).toBe('Farm Ltd')
    expect(f.paymentAccountNumber).toBe('0123456789')
    expect(f.paymentBankName).toBe('Typed Bank')
  })
  it('does nothing when there is no profile', () => {
    const f = emptyInvoiceForm('2026-03-01')
    prefillPayment(f, null)
    expect(f.paymentAccountName).toBe('')
  })
})

describe('invoiceToForm', () => {
  it('maps a saved invoice back into editable fields', () => {
    const inv = {
      id: '1', farmId: 'f', number: 2, reference: 'INV-0002', date: '2026-03-01', dueDate: '2026-03-15',
      billToName: 'X', billToPhone: null, billToEmail: null, billToAddress: null,
      lineItems: [{ serialNumber: 1, description: 'A', rate: 5, quantity: 2, amount: 10 }],
      subtotal: 10, vatPercent: 7.5, vatAmount: 0.75, total: 10.75,
      paymentAccountName: null, paymentAccountNumber: '1', paymentBankName: null,
      termsAndNotes: null, signatoryName: null, templateKey: 'ngf', createdAt: '', updatedAt: ''
    } as Invoice
    const f = invoiceToForm(inv)
    expect(f.billToPhone).toBe('')
    expect(f.lineItems).toEqual([{ description: 'A', rate: 5, quantity: 2 }])
    expect(f.vatPercent).toBe(7.5)
    expect(f.paymentAccountNumber).toBe('1')
  })
})
