// Form model, validation and request building for the sales-invoice editor. Kept free of Vue so it
// can be unit tested. The API re-validates everything; these checks only save a round trip.
import type { FarmBusinessProfile, Invoice, InvoiceRequest } from '@/types'
import { addDays, computeInvoiceTotals, isDateInRange, round2 } from '@/utils/money'

export interface InvoiceFormLine { description: string; rate: number | ''; quantity: number | '' }

export interface InvoiceForm {
  billToName: string
  billToPhone: string
  billToEmail: string
  billToAddress: string
  date: string
  dueDate: string
  lineItems: InvoiceFormLine[]
  vatPercent: number | ''
  paymentAccountName: string
  paymentAccountNumber: string
  paymentBankName: string
  termsAndNotes: string
  signatoryName: string
  templateKey: string
}

export const MAX_LINE_ITEMS = 200
// Server limits, mirrored so the user hears about them before the request.
export const MAX_RATE = 100_000_000
export const MAX_QUANTITY = 1_000_000
export const MAX_LINE_AMOUNT = 1_000_000_000
export const MAX_SUBTOTAL = 10_000_000_000
export const DATE_RANGE_MESSAGE = (what: string) => `${what} must be a real date between 1 Jan 2000 and 31 Dec 2100 (check the year).`
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function blankLine(): InvoiceFormLine {
  return { description: '', rate: '', quantity: 1 }
}

/** A new invoice dated `today`, due 14 days later (the API's own default). */
export function emptyInvoiceForm(today: string, templateKey = 'ngf'): InvoiceForm {
  return {
    billToName: '', billToPhone: '', billToEmail: '', billToAddress: '',
    date: today, dueDate: addDays(today, 14),
    lineItems: [blankLine()],
    vatPercent: 0,
    paymentAccountName: '', paymentAccountNumber: '', paymentBankName: '',
    termsAndNotes: '', signatoryName: '',
    templateKey
  }
}

/** Fills payment details from the farm's business profile, but never over what is already typed. */
export function prefillPayment(form: InvoiceForm, profile: Partial<FarmBusinessProfile> | null | undefined): void {
  if (!profile) return
  if (!form.paymentAccountName) form.paymentAccountName = profile.bankAccountName ?? ''
  if (!form.paymentAccountNumber) form.paymentAccountNumber = profile.bankAccountNumber ?? ''
  if (!form.paymentBankName) form.paymentBankName = profile.bankName ?? ''
}

export function invoiceToForm(inv: Invoice): InvoiceForm {
  return {
    billToName: inv.billToName ?? '',
    billToPhone: inv.billToPhone ?? '',
    billToEmail: inv.billToEmail ?? '',
    billToAddress: inv.billToAddress ?? '',
    date: inv.date,
    dueDate: inv.dueDate,
    lineItems: inv.lineItems.length
      ? inv.lineItems.map(l => ({ description: l.description, rate: l.rate, quantity: l.quantity }))
      : [blankLine()],
    vatPercent: inv.vatPercent ?? 0,
    paymentAccountName: inv.paymentAccountName ?? '',
    paymentAccountNumber: inv.paymentAccountNumber ?? '',
    paymentBankName: inv.paymentBankName ?? '',
    termsAndNotes: inv.termsAndNotes ?? '',
    signatoryName: inv.signatoryName ?? '',
    templateKey: inv.templateKey || 'ngf'
  }
}

/** Human-readable problems, in form order. Empty when the form can be sent. */
export function validateInvoiceForm(f: InvoiceForm): string[] {
  const errors: string[] = []
  if (!f.billToName.trim()) errors.push('Enter who the invoice is billed to.')
  if (f.billToName.trim().length > 200) errors.push('The bill-to name can be at most 200 characters.')
  if (f.billToEmail.trim() && !EMAIL_RE.test(f.billToEmail.trim())) errors.push('Enter a valid bill-to email address.')
  if (!f.date) errors.push('Choose the invoice date.')
  else if (!isDateInRange(f.date)) errors.push(DATE_RANGE_MESSAGE('The invoice date'))
  if (!f.dueDate) errors.push('Choose the due date.')
  else if (!isDateInRange(f.dueDate)) errors.push(DATE_RANGE_MESSAGE('The due date'))
  else if (f.date && f.dueDate < f.date) errors.push('The due date cannot be before the invoice date.')

  if (f.lineItems.length < 1) errors.push('Add at least one line item.')
  if (f.lineItems.length > MAX_LINE_ITEMS) errors.push(`An invoice can have at most ${MAX_LINE_ITEMS} line items.`)
  f.lineItems.forEach((l, i) => {
    const n = i + 1
    if (!l.description.trim()) errors.push(`Line ${n}: enter a description.`)
    else if (l.description.trim().length > 500) errors.push(`Line ${n}: the description can be at most 500 characters.`)
    const rateOk = l.rate !== '' && Number(l.rate) >= 0
    const qtyOk = l.quantity !== '' && Number(l.quantity) > 0
    if (!rateOk) errors.push(`Line ${n}: enter a rate of 0 or more.`)
    else if (Number(l.rate) >= MAX_RATE) errors.push(`Line ${n}: the rate must be less than ${MAX_RATE.toLocaleString('en-NG')}.`)
    if (!qtyOk) errors.push(`Line ${n}: enter a quantity greater than 0.`)
    else if (Number(l.quantity) >= MAX_QUANTITY) errors.push(`Line ${n}: the quantity must be less than ${MAX_QUANTITY.toLocaleString('en-NG')}.`)
    if (rateOk && qtyOk && Number(l.rate) < MAX_RATE && Number(l.quantity) < MAX_QUANTITY
      && round2(Number(l.rate) * Number(l.quantity)) >= MAX_LINE_AMOUNT) {
      errors.push(`Line ${n}: rate x quantity must be less than ${MAX_LINE_AMOUNT.toLocaleString('en-NG')}.`)
    }
  })
  if (computeInvoiceTotals(f.lineItems, 0).subtotal >= MAX_SUBTOTAL) {
    errors.push(`The invoice subtotal must be less than ${MAX_SUBTOTAL.toLocaleString('en-NG')}.`)
  }

  const vat = f.vatPercent === '' ? 0 : Number(f.vatPercent)
  if (!(vat >= 0 && vat <= 100)) errors.push('VAT must be between 0 and 100 percent.')
  if (f.termsAndNotes.length > 2000) errors.push('Terms and notes can be at most 2000 characters.')
  return errors
}

const blankToNull = (s: string) => s.trim() || null

export function buildInvoiceRequest(f: InvoiceForm): InvoiceRequest {
  return {
    date: f.date,
    dueDate: f.dueDate,
    billToName: f.billToName.trim(),
    billToPhone: blankToNull(f.billToPhone),
    billToEmail: blankToNull(f.billToEmail),
    billToAddress: blankToNull(f.billToAddress),
    lineItems: f.lineItems.map(l => ({
      description: l.description.trim(),
      rate: Number(l.rate),
      quantity: Number(l.quantity)
    })),
    vatPercent: f.vatPercent === '' ? 0 : Number(f.vatPercent),
    paymentAccountName: blankToNull(f.paymentAccountName),
    paymentAccountNumber: blankToNull(f.paymentAccountNumber),
    paymentBankName: blankToNull(f.paymentBankName),
    termsAndNotes: blankToNull(f.termsAndNotes),
    signatoryName: blankToNull(f.signatoryName),
    templateKey: f.templateKey || 'ngf'
  }
}
