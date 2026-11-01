import { describe, expect, it } from 'vitest'
import {
  buildPreviewUrl, colorError, defaultPreviewPath, describeColorSource, isHexColor, normalizeColor, pickerValue, previewErrorMessage
} from './brandColors'
import { buildBusinessProfileRequest, blankBusinessProfileForm, profileToForm, validateBrandColors } from './businessProfile'
import { buildInvoiceRequest, emptyInvoiceForm, invoiceToForm, validateInvoiceForm } from './invoiceForm'
import type { Invoice } from '@/types'

describe('isHexColor', () => {
  it.each(['#138A44', '#f5c90c', '#000000', ' #FFFFFF '])('accepts %s', v => expect(isHexColor(v)).toBe(true))
  it.each(['red', '#FFF', 'FFFFFF', '#12345G', '#11223344', '', '   ', null, undefined])('rejects %s', v => expect(isHexColor(v as string)).toBe(false))
})

describe('normalizeColor', () => {
  it('upper-cases and trims, and treats blank as not set', () => {
    expect(normalizeColor(' #b91c1c ')).toBe('#B91C1C')
    expect(normalizeColor('')).toBeNull()
    expect(normalizeColor('   ')).toBeNull()
    expect(normalizeColor(null)).toBeNull()
  })
})

describe('colorError', () => {
  it('is empty for blank or valid colours', () => {
    expect(colorError('Primary colour', '')).toBe('')
    expect(colorError('Primary colour', '#138A44')).toBe('')
  })
  it('names the field and the expected form otherwise', () => {
    expect(colorError('Accent colour', '#FFF')).toBe('Accent colour must be a colour in the form #RRGGBB, for example #138A44.')
  })
})

describe('pickerValue', () => {
  it('lower-cases a valid colour and falls back otherwise', () => {
    expect(pickerValue('#B91C1C', '#138A44')).toBe('#b91c1c')
    expect(pickerValue('nope', '#138A44')).toBe('#138a44')
    expect(pickerValue('', 'bad')).toBe('#000000')
  })
})

describe('describeColorSource', () => {
  it('words each source', () => {
    expect(describeColorSource('invoice')).toContain('this invoice')
    expect(describeColorSource('farm')).toContain('farm')
    expect(describeColorSource('default')).toContain('default')
  })
})

describe('preview URL', () => {
  const base = defaultPreviewPath('farm 1', 'ngf')
  it('builds the route from the farm and template key', () => {
    expect(base).toBe('/api/farms/farm%201/invoice-templates/ngf/preview')
  })
  it('encodes # as %23 and upper-cases the colours', () => {
    expect(buildPreviewUrl(base, { primaryColor: '#b91c1c', accentColor: '#F5C90C' }))
      .toBe(`${base}?primaryColor=%23B91C1C&accentColor=%23F5C90C`)
  })
  it('leaves out blank or invalid colours so the server falls back', () => {
    expect(buildPreviewUrl(base)).toBe(base)
    expect(buildPreviewUrl(base, { primaryColor: '', accentColor: 'red' })).toBe(base)
    expect(buildPreviewUrl(base, { accentColor: '#123456' })).toBe(`${base}?accentColor=%23123456`)
  })
  it('appends to a previewUrl that already has a query', () => {
    expect(buildPreviewUrl('/x/preview?a=1', { primaryColor: '#111111' })).toBe('/x/preview?a=1&primaryColor=%23111111')
  })
})

describe('previewErrorMessage', () => {
  it('maps 401, 404 and 422', () => {
    expect(previewErrorMessage({ response: { status: 401 } })).toMatch(/session has expired/)
    expect(previewErrorMessage({ response: { status: 404 } })).toMatch(/could not be found/)
    expect(previewErrorMessage({ response: { status: 422, data: { detail: 'primaryColor must be a colour' } } })).toBe('primaryColor must be a colour')
    expect(previewErrorMessage({ response: { status: 422 } })).toMatch(/#RRGGBB/)
    expect(previewErrorMessage(new Error('network'))).toMatch(/Could not load the preview/)
  })
})

describe('invoice colours in the form and request', () => {
  const filled = () => {
    const f = emptyInvoiceForm('2026-03-01')
    f.billToName = 'Green Market Ltd'
    f.lineItems = [{ description: 'Eggs', rate: 100, quantity: 2 }]
    return f
  }

  it('sends null colours by default (farm brand colours)', () => {
    const r = buildInvoiceRequest(filled())
    expect(r.primaryColor).toBeNull()
    expect(r.accentColor).toBeNull()
  })
  it('ignores typed colours while in brand mode', () => {
    const f = filled(); f.primaryColor = '#B91C1C'
    expect(buildInvoiceRequest(f).primaryColor).toBeNull()
  })
  it('sends normalised custom colours, and null for a blank one', () => {
    const f = filled(); f.colorMode = 'custom'; f.primaryColor = '#b91c1c'; f.accentColor = ''
    const r = buildInvoiceRequest(f)
    expect(r.primaryColor).toBe('#B91C1C')
    expect(r.accentColor).toBeNull()
  })
  it('rejects a bad custom colour but not a bad hidden one', () => {
    const f = filled(); f.primaryColor = 'red'
    expect(validateInvoiceForm(f)).toEqual([])
    f.colorMode = 'custom'
    expect(validateInvoiceForm(f)).toEqual(['Primary colour must be a colour in the form #RRGGBB, for example #138A44.'])
    f.primaryColor = '#B91C1C'; f.accentColor = '#12345G'
    expect(validateInvoiceForm(f)).toHaveLength(1)
  })
  it('loads stored colours as custom and sends them back on edit', () => {
    const inv = {
      id: 'i1', farmId: 'f', number: 1, reference: 'INV-0001', date: '2026-03-01', dueDate: '2026-03-15', billToName: 'A',
      billToPhone: null, billToEmail: null, billToAddress: null,
      lineItems: [{ serialNumber: 1, description: 'x', rate: 1, quantity: 1, amount: 1 }],
      subtotal: 1, vatPercent: 0, vatAmount: 0, total: 1, paymentAccountName: null, paymentAccountNumber: null, paymentBankName: null,
      termsAndNotes: null, signatoryName: null, templateKey: 'band', primaryColor: '#B91C1C', accentColor: null,
      createdAt: '', updatedAt: ''
    } as Invoice
    const f = invoiceToForm(inv)
    expect(f.colorMode).toBe('custom')
    expect(buildInvoiceRequest(f)).toMatchObject({ templateKey: 'band', primaryColor: '#B91C1C', accentColor: null })
    expect(invoiceToForm({ ...inv, primaryColor: null }).colorMode).toBe('brand')
    expect(invoiceToForm({ ...inv, primaryColor: undefined, accentColor: undefined }).colorMode).toBe('brand')
  })
})

describe('business profile brand colours', () => {
  it('sends every field, with blank colours as null (reset to default)', () => {
    const f = blankBusinessProfileForm()
    f.address = ' 1 Farm Road '
    const r = buildBusinessProfileRequest(f)
    expect(r).toEqual({
      address: '1 Farm Road', phone: null, email: null, website: null,
      bankAccountName: null, bankAccountNumber: null, bankName: null,
      brandPrimaryColor: null, brandAccentColor: null
    })
  })
  it('sends custom colours upper-cased', () => {
    const f = profileToForm({ phone: '0803', brandPrimaryColor: '#b91c1c', brandAccentColor: null })
    const r = buildBusinessProfileRequest(f)
    expect(r.brandPrimaryColor).toBe('#B91C1C')
    expect(r.brandAccentColor).toBeNull()
    expect(r.phone).toBe('0803') // existing fields are kept so the full replace clears nothing
  })
  it('validates both colours', () => {
    expect(validateBrandColors({ brandPrimaryColor: '#FFF', brandAccentColor: '' }).primary).toMatch(/Primary colour must be/)
    expect(validateBrandColors({ brandPrimaryColor: '', brandAccentColor: 'FFFFFF' }).accent).toMatch(/Accent colour must be/)
    expect(validateBrandColors({ brandPrimaryColor: '#123456', brandAccentColor: '' })).toEqual({ primary: '', accent: '' })
  })
})
