import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import type { Order, VendorInvoice } from '@/types'

vi.mock('@/services/api', () => ({
  vendorInvoiceApi: {
    listForOrder: vi.fn(),
    upload: vi.fn(),
    list: vi.fn(),
    file: vi.fn(),
    setStatus: vi.fn(),
    remove: vi.fn()
  },
  logoApi: {},
  setFarmId: vi.fn()
}))

import { vendorInvoiceApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import VendorInvoiceCard from './VendorInvoiceCard.vue'
import VendorInvoiceUploadForm from './VendorInvoiceUploadForm.vue'
import VendorInvoicesPanel from './VendorInvoicesPanel.vue'
import OrderDetailModal from '@/components/modal/OrderDetailModal.vue'

const api = vendorInvoiceApi as unknown as Record<string, ReturnType<typeof vi.fn>>

const order = (status: Order['status'] = 'Received'): Order => ({
  id: 'o1', farmId: 'f1', supplierId: 's1', supplierName: 'AgroSupplies', reference: 'ORD-0007',
  status, total: 50000, isPaid: false, createdAt: '2026-03-01T00:00:00Z', lineItems: []
} as unknown as Order)

const bill = (over: Partial<VendorInvoice> = {}): VendorInvoice => ({
  id: 'v1', farmId: 'f1', orderId: 'o1', orderReference: 'ORD-0007', orderTotal: 50000, orderIsPaid: false,
  supplierId: 's1', vendorName: 'AgroSupplies', invoiceNumber: 'SUP-100', amount: 50000,
  invoiceDate: '2026-03-02', dueDate: '2026-03-20', status: 'Unpaid', paidAt: null, isOverdue: false,
  notes: null, fileName: 'bill.pdf', contentType: 'application/pdf', fileSize: 2048, uploadedByUserId: 'u1',
  createdAt: '2026-03-02T00:00:00Z', ...over
})

function signIn(role: string) {
  const store = useAgroStore()
  store.currentUser = { id: 'u1', email: 'a@b.c', fullName: 'Test', companyId: 'c1', role } as never
}

beforeEach(() => {
  setActivePinia(createPinia())
  Object.values(api).forEach(fn => fn.mockReset())
})

describe('VendorInvoiceCard', () => {
  it('shows an Overdue badge only for unpaid overdue bills', () => {
    signIn('Admin')
    const unpaid = mount(VendorInvoiceCard, { props: { invoice: bill({ isOverdue: true }) } })
    expect(unpaid.text()).toContain('Overdue')
    const paid = mount(VendorInvoiceCard, { props: { invoice: bill({ isOverdue: true, status: 'Paid', paidAt: '2026-03-10T00:00:00Z' }) } })
    expect(paid.text()).not.toContain('Overdue')
    expect(paid.text()).toContain('Paid')
  })

  it('shows a mismatch hint when the amount differs from the order total', () => {
    signIn('Admin')
    const w = mount(VendorInvoiceCard, { props: { invoice: bill({ amount: 52000 }) } })
    expect(w.find('.vi-mismatch').text()).toMatch(/₦2,000.00 more than the order total of ₦50,000.00/)
    const ok = mount(VendorInvoiceCard, { props: { invoice: bill() } })
    expect(ok.find('.vi-mismatch').exists()).toBe(false)
  })

  it('offers pay/delete to managers and hides them from supervisors', () => {
    signIn('FarmManager')
    const mgr = mount(VendorInvoiceCard, { props: { invoice: bill() } })
    expect(mgr.text()).toContain('Mark paid')
    expect(mgr.text()).toContain('Delete')

    setActivePinia(createPinia())
    signIn('Supervisor')
    const sup = mount(VendorInvoiceCard, { props: { invoice: bill() } })
    expect(sup.text()).not.toContain('Mark paid')
    expect(sup.text()).not.toContain('Delete')
    expect(sup.text()).toContain('Download')
  })

  it('labels the toggle as Mark unpaid for a paid bill', () => {
    signIn('Admin')
    const w = mount(VendorInvoiceCard, { props: { invoice: bill({ status: 'Paid' }) } })
    expect(w.text()).toContain('Mark unpaid')
  })
})

describe('VendorInvoiceUploadForm', () => {
  function pickFile(w: ReturnType<typeof mount>, file: File) {
    const input = w.find('input[type="file"]')
    Object.defineProperty(input.element, 'files', { value: [file], configurable: true })
    return input.trigger('change')
  }
  const pdf = () => new File(['%PDF-1.4'], 'bill.pdf', { type: 'application/pdf' })

  it('rejects an unsupported file type before any upload', async () => {
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await pickFile(w, new File(['GIF89a'], 'pic.gif', { type: 'image/gif' }))
    expect(w.text()).toContain('PDF, PNG or JPEG')
    await w.find('form').trigger('submit')
    expect(api.upload).not.toHaveBeenCalled()
  })

  it('rejects a file over 10 MB', async () => {
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    const big = new File(['x'], 'big.pdf', { type: 'application/pdf' })
    Object.defineProperty(big, 'size', { value: 10 * 1024 * 1024 + 1 })
    await pickFile(w, big)
    expect(w.text()).toMatch(/larger than the 10 MB limit/)
  })

  it('requires the file, number and amount', async () => {
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await w.find('form').trigger('submit')
    const alert = w.find('[role="alert"]').text()
    expect(alert).toContain('Choose the invoice file')
    expect(alert).toContain('Enter the invoice number')
    expect(alert).toContain('amount as a plain number')
    expect(api.upload).not.toHaveBeenCalled()
  })

  it('warns live when the amount differs from the order total', async () => {
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await w.find('input[type="number"]').setValue(45000)
    expect(w.text()).toMatch(/₦5,000.00 less than the order total/)
  })

  it('rejects a year typo in the invoice date and an over-precise amount before any request', async () => {
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await pickFile(w, pdf())
    const l = (t: string) => w.find(`#${w.findAll('label').find(x => x.text().startsWith(t))!.attributes('for')}`)
    await l('Invoice number').setValue('X1')
    await l('Amount').setValue(0.0000001)
    await l('Invoice date').setValue('202026-03-02')
    await w.find('form').trigger('submit')
    await flushPromises()
    const alert = w.find('[role="alert"]').text()
    expect(alert).toContain('2 decimal places')
    expect(alert).toContain('between 1 Jan 2000 and 31 Dec 2100')
    expect(api.upload).not.toHaveBeenCalled()
  })

  it('closes on Escape', async () => {
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await w.find('form').trigger('keydown', { key: 'Escape' })
    expect(w.emitted('cancel')).toHaveLength(1)
  })

  it('uploads multipart fields and emits the created invoice', async () => {
    api.upload.mockResolvedValue({ data: bill() })
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await pickFile(w, pdf())
    const inputs = w.findAll('input')
    const byLabel = (label: string) => {
      const l = w.findAll('label').find(x => x.text().startsWith(label))!
      return w.find(`#${l.attributes('for')}`)
    }
    await byLabel('Invoice number').setValue('  SUP-100 ')
    await byLabel('Amount').setValue(50000)
    await byLabel('Invoice date').setValue('2026-03-02')
    await byLabel('Due date').setValue('2026-03-20')
    await byLabel('Notes').setValue('Delivered in full')
    expect(inputs.length).toBeGreaterThan(4)
    await w.find('form').trigger('submit')
    await flushPromises()

    expect(api.upload).toHaveBeenCalledTimes(1)
    const [orderId, form] = api.upload.mock.calls[0] as [string, FormData]
    expect(orderId).toBe('o1')
    expect(form.get('file')).toBeInstanceOf(File)
    expect(form.get('invoiceNumber')).toBe('SUP-100')
    expect(form.get('amount')).toBe('50000.00')
    expect(form.get('invoiceDate')).toBe('2026-03-02')
    expect(form.get('dueDate')).toBe('2026-03-20')
    expect(form.get('notes')).toBe('Delivered in full')
    expect(form.has('vendorName')).toBe(false) // blank: the server defaults to the order's supplier
    expect(w.emitted('uploaded')?.[0]).toEqual([bill()])
  })

  it('shows the 422 detail from the API (for example a duplicate number)', async () => {
    api.upload.mockRejectedValue({ response: { status: 422, data: { detail: 'Invoice SUP-100 already exists for AgroSupplies.' } } })
    const w = mount(VendorInvoiceUploadForm, { props: { order: order() } })
    await pickFile(w, pdf())
    const l = (t: string) => w.find(`#${w.findAll('label').find(x => x.text().startsWith(t))!.attributes('for')}`)
    await l('Invoice number').setValue('SUP-100')
    await l('Amount').setValue(10)
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(w.find('[role="alert"]').text()).toContain('already exists for AgroSupplies')
    expect(w.emitted('uploaded')).toBeUndefined()
  })
})

describe('VendorInvoicesPanel', () => {
  it('lists the order bills and offers upload to a supervisor', async () => {
    signIn('Supervisor')
    api.listForOrder.mockResolvedValue({ data: [bill()] })
    const w = mount(VendorInvoicesPanel, { props: { order: order() } })
    await flushPromises()
    expect(api.listForOrder).toHaveBeenCalledWith('o1')
    expect(w.text()).toContain('SUP-100')
    expect(w.text()).toContain('Upload vendor invoice')
  })

  it('hides the upload action from members and shows an empty state', async () => {
    signIn('Member')
    api.listForOrder.mockResolvedValue({ data: [] })
    const w = mount(VendorInvoicesPanel, { props: { order: order() } })
    await flushPromises()
    expect(w.text()).not.toContain('+ Upload vendor invoice')
    expect(w.text()).toContain('No vendor invoice attached')
  })

  it('shows an error with a retry when loading fails', async () => {
    signIn('Admin')
    api.listForOrder.mockRejectedValueOnce({ response: { data: { detail: 'Boom' } } })
    const w = mount(VendorInvoicesPanel, { props: { order: order() } })
    await flushPromises()
    expect(w.find('[role="alert"]').text()).toContain('Boom')
    api.listForOrder.mockResolvedValue({ data: [bill()] })
    await w.find('.vp-state button').trigger('click')
    await flushPromises()
    expect(w.text()).toContain('SUP-100')
  })

  it('moves focus into the upload form when opened and back to the button when cancelled', async () => {
    signIn('Admin')
    api.listForOrder.mockResolvedValue({ data: [] })
    const w = mount(VendorInvoicesPanel, { props: { order: order() }, attachTo: document.body })
    await flushPromises()
    const open = w.findAll('button').find(b => b.text().includes('Upload vendor invoice'))!
    ;(open.element as HTMLElement).focus()
    await open.trigger('click')
    await flushPromises()
    expect(document.activeElement).toBe(w.find('input[type="file"]').element)
    await w.find('form').trigger('keydown', { key: 'Escape' })
    await flushPromises()
    expect(w.find('form').exists()).toBe(false)
    expect(document.activeElement?.textContent).toContain('Upload vendor invoice')
    w.unmount()
  })

  it('marks a bill paid through the API and updates it in place', async () => {
    signIn('Admin')
    api.listForOrder.mockResolvedValue({ data: [bill()] })
    api.setStatus.mockResolvedValue({ data: bill({ status: 'Paid', paidAt: '2026-03-10T00:00:00Z' }) })
    const w = mount(VendorInvoicesPanel, { props: { order: order() } })
    await flushPromises()
    const btn = w.findAll('button').find(b => b.text() === 'Mark paid')!
    await btn.trigger('click')
    await flushPromises()
    expect(api.setStatus).toHaveBeenCalledWith('v1', 'Paid', expect.any(String))
    expect(w.text()).toContain('Mark unpaid')
  })
})

describe('OrderDetailModal', () => {
  const mountModal = (status: Order['status']) =>
    mount(OrderDetailModal, { props: { order: order(status) }, global: { stubs: { teleport: true } } })

  it('offers vendor invoices on a Received order', async () => {
    signIn('Admin')
    api.listForOrder.mockResolvedValue({ data: [] })
    const w = mountModal('Received')
    await flushPromises()
    expect(w.text()).toContain('Vendor invoices')
    expect(api.listForOrder).toHaveBeenCalled()
  })

  it.each(['Draft', 'Placed'] as const)('does not offer vendor invoices on a %s order', async status => {
    signIn('Admin')
    const w = mountModal(status)
    await flushPromises()
    expect(w.text()).not.toContain('Vendor invoices')
    expect(api.listForOrder).not.toHaveBeenCalled()
  })
})
