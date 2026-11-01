import { afterEach, describe, expect, it } from 'vitest'
import type { InternalAxiosRequestConfig } from 'axios'
import { api, logoApi, vendorInvoiceApi, invoiceApi, FARM_ID } from './api'

// Captures the request after axios has run its request transforms, without touching the network.
function capture() {
  const seen: InternalAxiosRequestConfig[] = []
  const original = api.defaults.adapter
  api.defaults.adapter = async config => {
    seen.push(config)
    return { data: {}, status: 200, statusText: 'OK', headers: {}, config }
  }
  return { seen, restore: () => { api.defaults.adapter = original } }
}

describe('api requests', () => {
  let restore = () => {}
  afterEach(() => restore())

  it('sends the logo as multipart FormData, not JSON (the client defaults to a JSON content type)', async () => {
    const c = capture(); restore = c.restore
    const file = new File([new Uint8Array([1, 2, 3])], 'logo.png', { type: 'image/png' })
    await logoApi.upload('farm-1', file)
    const req = c.seen[0]
    expect(req.method).toBe('put')
    expect(req.url).toBe('/api/farms/farm-1/logo')
    expect(req.data).toBeInstanceOf(FormData)
    expect((req.data as FormData).get('file')).toBeInstanceOf(File)
  })

  it('sends a vendor invoice as FormData with the documented field names', async () => {
    const c = capture(); restore = c.restore
    const form = new FormData()
    form.append('file', new File(['%PDF-'], 'bill.pdf', { type: 'application/pdf' }))
    form.append('invoiceNumber', 'V-1')
    form.append('amount', '1234.5')
    await vendorInvoiceApi.upload('order-9', form)
    const req = c.seen[0]
    expect(req.method).toBe('post')
    expect(req.url).toBe(`/api/farms/${FARM_ID}/orders/order-9/vendor-invoices`)
    expect(req.data).toBeInstanceOf(FormData)
    expect((req.data as FormData).get('amount')).toBe('1234.5')
  })

  it('requests files and PDFs as blobs so the bearer token is attached', async () => {
    const c = capture(); restore = c.restore
    await invoiceApi.pdf('inv-1')
    await vendorInvoiceApi.file('vi-1', true)
    await logoApi.get('farm-1')
    expect(c.seen.map(r => r.responseType)).toEqual(['blob', 'blob', 'blob'])
    expect(c.seen[1].url).toBe(`/api/farms/${FARM_ID}/vendor-invoices/vi-1/file?inline=true`)
  })

  it('builds the vendor invoice list filters', async () => {
    const c = capture(); restore = c.restore
    await vendorInvoiceApi.list({ status: 'Unpaid', orderId: 'o 1' })
    await vendorInvoiceApi.list()
    expect(c.seen[0].url).toBe(`/api/farms/${FARM_ID}/vendor-invoices?status=Unpaid&orderId=o%201`)
    expect(c.seen[1].url).toBe(`/api/farms/${FARM_ID}/vendor-invoices`)
  })

  it('sends paidAt only when marking paid', async () => {
    const c = capture(); restore = c.restore
    await vendorInvoiceApi.setStatus('v1', 'Paid', '2026-03-01T10:00:00.000Z')
    await vendorInvoiceApi.setStatus('v1', 'Unpaid')
    expect(JSON.parse(c.seen[0].data)).toEqual({ status: 'Paid', paidAt: '2026-03-01T10:00:00.000Z' })
    expect(JSON.parse(c.seen[1].data)).toEqual({ status: 'Unpaid' })
  })

  it('attaches the bearer token from localStorage', async () => {
    const c = capture(); restore = c.restore
    localStorage.setItem('agro_token', 'jwt-123')
    await invoiceApi.list()
    localStorage.removeItem('agro_token')
    expect(String(c.seen[0].headers.Authorization)).toBe('Bearer jwt-123')
  })
})
