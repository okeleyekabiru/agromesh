import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { InvoiceSummary } from '@/types'

vi.mock('@/services/api', () => ({
  invoiceApi: { list: vi.fn(), get: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn(), pdf: vi.fn(), templates: vi.fn(), preview: vi.fn() },
  businessProfileApi: { get: vi.fn() },
  logoApi: {},
  setFarmId: vi.fn()
}))

import { businessProfileApi, invoiceApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import InvoicesView from './InvoicesView.vue'
import InvoiceFormView from './InvoiceFormView.vue'

const inv = invoiceApi as unknown as Record<string, ReturnType<typeof vi.fn>>
const profile = businessProfileApi.get as unknown as ReturnType<typeof vi.fn>

const row = (over: Partial<InvoiceSummary> = {}): InvoiceSummary => ({
  id: 'i1', number: 2, reference: 'INV-0002', date: '2026-03-01', dueDate: '2026-03-15',
  billToName: 'Green Market Ltd', total: 90000, templateKey: 'ngf', createdAt: '2026-03-01T00:00:00Z', ...over
})

function makeRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/invoices', name: 'invoices', component: InvoicesView },
      { path: '/invoices/new', name: 'invoice-new', component: InvoiceFormView },
      { path: '/invoices/:id/edit', name: 'invoice-edit', component: InvoiceFormView }
    ]
  })
}

async function mountView(component: typeof InvoicesView | typeof InvoiceFormView, role: string, path: string) {
  const pinia = createPinia()
  setActivePinia(pinia)
  const store = useAgroStore()
  store.currentUser = { id: 'u1', email: 'a@b.c', fullName: 'T', companyId: 'c', role } as never
  store.activeFarmId = 'f1'
  const router = makeRouter()
  router.push(path)
  await router.isReady()
  const w = mount(component, { global: { plugins: [pinia, router] } })
  await flushPromises()
  return { w, router }
}

beforeEach(() => {
  Object.values(inv).forEach(f => f.mockReset())
  profile.mockReset()
  inv.templates.mockResolvedValue({ data: [{ key: 'ngf', name: 'NGF - Invoice', description: 'Nature\'s Gold Farm layout', isDefault: true }] })
  profile.mockResolvedValue({ data: { bankAccountName: 'Farm Ltd', bankAccountNumber: '0123456789', bankName: 'GTBank' } })
})

describe('InvoicesView', () => {
  it('lists reference, bill-to, dates and total', async () => {
    inv.list.mockResolvedValue({ data: [row()] })
    const { w } = await mountView(InvoicesView, 'Admin', '/invoices')
    const text = w.find('tbody').text()
    expect(text).toContain('INV-0002')
    expect(text).toContain('Green Market Ltd')
    expect(text).toContain('1 Mar 2026')
    expect(text).toContain('15 Mar 2026')
    expect(text).toContain('₦90,000.00')
  })

  it('shows a helpful empty state with a create action for managers', async () => {
    inv.list.mockResolvedValue({ data: [] })
    const { w } = await mountView(InvoicesView, 'FarmManager', '/invoices')
    expect(w.text()).toContain('No invoices yet')
    expect(w.text()).toContain('Create your first invoice')
  })

  it('hides create/edit/delete from users who cannot write', async () => {
    inv.list.mockResolvedValue({ data: [row()] })
    const { w } = await mountView(InvoicesView, 'Member', '/invoices')
    expect(w.text()).not.toContain('New invoice')
    expect(w.text()).not.toContain('Edit')
    expect(w.text()).not.toContain('Delete')
    expect(w.text()).toContain('Download') // anyone in the company can read the PDF
  })

  it('shows a retryable error when the list fails', async () => {
    inv.list.mockRejectedValueOnce({ response: { data: { detail: 'Server unavailable' } } })
    const { w } = await mountView(InvoicesView, 'Admin', '/invoices')
    expect(w.find('[role="alert"]').text()).toContain('Server unavailable')
    inv.list.mockResolvedValue({ data: [row()] })
    await w.find('.state-box button').trigger('click')
    await flushPromises()
    expect(w.text()).toContain('INV-0002')
  })

  it('closes the dialog and refreshes the list when the invoice was already deleted (404)', async () => {
    inv.list.mockResolvedValueOnce({ data: [row()] })
    inv.remove.mockRejectedValue({ response: { status: 404, data: { detail: 'Invoice 3f2c0000-aaaa not found.' } } })
    const { w } = await mountView(InvoicesView, 'Admin', '/invoices')
    inv.list.mockResolvedValue({ data: [] })
    await w.find('button.del').trigger('click')
    await w.find('.cd-confirm').trigger('click')
    await flushPromises()
    expect(w.find('[role="alertdialog"]').exists()).toBe(false)
    expect(w.text()).not.toContain('3f2c0000')
    expect(w.text()).toContain('already deleted')
    expect(w.text()).toContain('No invoices yet')
  })

  it('deletes after confirmation', async () => {
    inv.list.mockResolvedValue({ data: [row()] })
    inv.remove.mockResolvedValue({})
    const { w } = await mountView(InvoicesView, 'Admin', '/invoices')
    await w.find('button.del').trigger('click')
    expect(inv.remove).not.toHaveBeenCalled()
    expect(w.find('[role="alertdialog"]').exists()).toBe(true)
    await w.find('.cd-confirm').trigger('click')
    await flushPromises()
    expect(inv.remove).toHaveBeenCalledWith('i1')
    expect(w.text()).toContain('No invoices yet')
  })
})

describe('InvoiceFormView', () => {
  const byLabel = (w: ReturnType<typeof mount>, text: string) => {
    const l = w.findAll('label').find(x => x.text().startsWith(text))
    if (!l) throw new Error(`no label ${text}`)
    return w.find(`#${l.attributes('for')}`)
  }

  it('prefills payment details from the business profile and the default template', async () => {
    const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
    expect((byLabel(w, 'Account name').element as HTMLInputElement).value).toBe('Farm Ltd')
    expect((byLabel(w, 'Account number').element as HTMLInputElement).value).toBe('0123456789')
    expect((byLabel(w, 'Bank name').element as HTMLInputElement).value).toBe('GTBank')
    expect((w.find('input[name="invoice-template"]:checked').element as HTMLInputElement).value).toBe('ngf')
  })

  it('previews line amounts and totals live, with VAT', async () => {
    const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
    await byLabel(w, 'Rate, line 1').setValue(1000)
    await byLabel(w, 'Quantity, line 1').setValue(3)
    await byLabel(w, 'VAT').setValue(7.5)
    const totals = w.find('.totals').text()
    expect(w.find('.amount').text()).toBe('₦3,000.00')
    expect(totals).toContain('₦3,000.00')
    expect(totals).toContain('₦225.00')
    expect(totals).toContain('₦3,225.00')
  })

  it('adds and removes line items, never going below one', async () => {
    const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
    expect(w.findAll('.li-row')).toHaveLength(1)
    expect(w.find('.li-remove').attributes('disabled')).toBeDefined()
    await w.find('.add-line').trigger('click')
    expect(w.findAll('.li-row')).toHaveLength(2)
    await w.findAll('.li-remove')[0].trigger('click')
    expect(w.findAll('.li-row')).toHaveLength(1)
  })

  it('does not call the API when required fields are missing', async () => {
    const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(inv.create).not.toHaveBeenCalled()
    expect(w.find('[role="alert"]').text()).toContain('Enter who the invoice is billed to.')
  })

  it('creates the invoice and returns to the list', async () => {
    inv.create.mockResolvedValue({ data: { id: 'i9', reference: 'INV-0009' } })
    inv.list.mockResolvedValue({ data: [] })
    const { w, router } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
    await byLabel(w, 'Customer name').setValue('Green Market Ltd')
    await byLabel(w, 'Description, line 1').setValue('Palm oil 25L')
    await byLabel(w, 'Rate, line 1').setValue(45000)
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(inv.create).toHaveBeenCalledTimes(1)
    const body = inv.create.mock.calls[0][0]
    expect(body).toMatchObject({
      billToName: 'Green Market Ltd', templateKey: 'ngf', vatPercent: 0,
      lineItems: [{ description: 'Palm oil 25L', rate: 45000, quantity: 1 }],
      paymentAccountNumber: '0123456789'
    })
    expect(body.date).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(router.currentRoute.value.name).toBe('invoices')
    expect(router.currentRoute.value.query.saved).toBe('INV-0009') // the list shows a saved notice from this
  })

  it('surfaces a 422 problem detail', async () => {
    inv.create.mockRejectedValue({ response: { status: 422, data: { detail: 'Unknown invoice template "zzz".' } } })
    const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
    await byLabel(w, 'Customer name').setValue('X')
    await byLabel(w, 'Description, line 1').setValue('Item')
    await byLabel(w, 'Rate, line 1').setValue(10)
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(w.find('[role="alert"]').text()).toContain('Unknown invoice template "zzz".')
  })

  it('loads an existing invoice for editing and saves with PUT', async () => {
    inv.get.mockResolvedValue({
      data: {
        id: 'i1', farmId: 'f1', number: 2, reference: 'INV-0002', date: '2026-03-01', dueDate: '2026-03-20',
        billToName: 'Old Name', billToPhone: null, billToEmail: null, billToAddress: null,
        lineItems: [{ serialNumber: 1, description: 'Eggs', rate: 100, quantity: 5, amount: 500 }],
        subtotal: 500, vatPercent: 0, vatAmount: 0, total: 500,
        paymentAccountName: null, paymentAccountNumber: null, paymentBankName: null,
        termsAndNotes: null, signatoryName: null, templateKey: 'ngf', createdAt: '', updatedAt: ''
      }
    })
    inv.update.mockResolvedValue({ data: { id: 'i1', reference: 'INV-0002' } })
    inv.list.mockResolvedValue({ data: [] })
    const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/i1/edit')
    expect(w.find('h1').text()).toBe('Edit INV-0002')
    expect((byLabel(w, 'Customer name').element as HTMLInputElement).value).toBe('Old Name')
    expect(profile).not.toHaveBeenCalled() // the saved payment block is kept, not overwritten from the profile
    await byLabel(w, 'Customer name').setValue('New Name')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(inv.update).toHaveBeenCalledWith('i1', expect.objectContaining({ billToName: 'New Name', dueDate: '2026-03-20' }))
    expect(inv.create).not.toHaveBeenCalled()
  })

  it('blocks users without write access', async () => {
    const { w } = await mountView(InvoiceFormView, 'Supervisor', '/invoices/new')
    expect(w.text()).toContain('Only farm owners and managers can create or edit invoices.')
    expect(w.find('form').exists()).toBe(false)
  })

  describe('template and colours', () => {
    const tpls = ['ngf', 'band', 'cards'].map((key, i) => ({
      key, name: key, description: `About ${key}`, isDefault: i === 0,
      defaultPrimaryColor: i === 1 ? '#4F46E5' : '#138A44', defaultAccentColor: i === 1 ? '#F59E0B' : '#F5C90C',
      previewUrl: `/api/farms/f1/invoice-templates/${key}/preview`
    }))
    const fill = async (w: ReturnType<typeof mount>) => {
      await byLabel(w, 'Customer name').setValue('X')
      await byLabel(w, 'Description, line 1').setValue('Item')
      await byLabel(w, 'Rate, line 1').setValue(10)
    }

    beforeEach(() => {
      inv.templates.mockResolvedValue({ data: tpls })
      inv.create.mockResolvedValue({ data: { id: 'i9', reference: 'INV-0009' } })
      inv.list.mockResolvedValue({ data: [] })
    })

    it('lists every template and saves the chosen key with null colours by default', async () => {
      profile.mockResolvedValue({ data: { brandPrimaryColor: '#B91C1C' } })
      const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
      expect(w.findAll('input[name="invoice-template"]')).toHaveLength(3)
      await w.find('input[name="invoice-template"][value="band"]').setValue()
      await fill(w)
      await w.find('form').trigger('submit')
      await flushPromises()
      expect(inv.create.mock.calls[0][0]).toMatchObject({ templateKey: 'band', primaryColor: null, accentColor: null })
      expect(w.find('.src-list').exists()).toBe(true)
    })

    it('shows the farm brand colour as the source, and sends custom colours when chosen', async () => {
      profile.mockResolvedValue({ data: { brandPrimaryColor: '#b91c1c' } })
      const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
      expect(w.find('.src-list').text()).toContain('Primary #B91C1C: your farm')
      expect(w.find('.src-list').text()).toContain('Accent #F5C90C: the template default')
      await w.find('input[name="color-mode"][value="custom"]').setValue()
      expect((byLabel(w, 'Primary colour').element as HTMLInputElement).value).toBe('#B91C1C') // pickers start on the colours in use
      await byLabel(w, 'Primary colour').setValue('#0a7b3e')
      await byLabel(w, 'Accent colour').setValue('#F5C90C')
      await fill(w)
      await w.find('form').trigger('submit')
      await flushPromises()
      expect(inv.create.mock.calls[0][0]).toMatchObject({ primaryColor: '#0A7B3E', accentColor: '#F5C90C' })
    })

    it('blocks a bad custom colour with a clear message', async () => {
      const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
      await w.find('input[name="color-mode"][value="custom"]').setValue()
      await byLabel(w, 'Primary colour').setValue('#FFF')
      await fill(w)
      await w.find('form').trigger('submit')
      await flushPromises()
      expect(inv.create).not.toHaveBeenCalled()
      expect(w.text()).toContain('Primary colour must be a colour in the form #RRGGBB')
    })

    it('on edit shows the server colour sources and sends the loaded colours back', async () => {
      inv.get.mockResolvedValue({
        data: {
          id: 'i1', farmId: 'f1', number: 2, reference: 'INV-0002', date: '2026-03-01', dueDate: '2026-03-20',
          billToName: 'Old', billToPhone: null, billToEmail: null, billToAddress: null,
          lineItems: [{ serialNumber: 1, description: 'Eggs', rate: 100, quantity: 5, amount: 500 }],
          subtotal: 500, vatPercent: 0, vatAmount: 0, total: 500, paymentAccountName: null, paymentAccountNumber: null, paymentBankName: null,
          termsAndNotes: null, signatoryName: null, templateKey: 'band', createdAt: '', updatedAt: '',
          primaryColor: '#B91C1C', accentColor: null, resolvedPrimaryColor: '#B91C1C', resolvedAccentColor: '#F5C90C',
          primaryColorSource: 'invoice', accentColorSource: 'default'
        }
      })
      inv.update.mockResolvedValue({ data: { id: 'i1', reference: 'INV-0002' } })
      const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/i1/edit')
      expect(w.find('.src-list').text()).toContain('Primary #B91C1C: set on this invoice')
      expect(w.find('.src-list').text()).toContain('Accent #F5C90C: the template default')
      await w.find('form').trigger('submit')
      await flushPromises()
      expect(inv.update).toHaveBeenCalledWith('i1', expect.objectContaining({ templateKey: 'band', primaryColor: '#B91C1C', accentColor: null }))
    })

    describe('Preview', () => {
      const previewBtn = (w: ReturnType<typeof mount>) => w.findAll('button').find(b => b.text() === 'Preview')!
      const fakeTab = { document: { title: '' }, location: { href: '' }, closed: false, close: vi.fn() }
      beforeEach(() => {
        fakeTab.close.mockReset()
        fakeTab.location.href = ''
        vi.stubGlobal('open', vi.fn(() => fakeTab))
        URL.createObjectURL = vi.fn(() => 'blob:preview')
        URL.revokeObjectURL = vi.fn()
      })

      it('fetches the PDF with the custom colours and opens it in the tab', async () => {
        inv.preview.mockResolvedValue({ data: new Blob(['%PDF'], { type: 'application/pdf' }) })
        const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
        await w.find('input[name="invoice-template"][value="band"]').setValue()
        await w.find('input[name="color-mode"][value="custom"]').setValue()
        await byLabel(w, 'Primary colour').setValue('#0A7B3E')
        await previewBtn(w).trigger('click')
        await flushPromises()
        expect(inv.preview).toHaveBeenCalledWith('band', { primaryColor: '#0A7B3E', accentColor: '#F59E0B' }, '/api/farms/f1/invoice-templates/band/preview')
        expect(fakeTab.location.href).toBe('blob:preview')
      })

      it('sends no colours in brand mode', async () => {
        inv.preview.mockResolvedValue({ data: new Blob(['%PDF']) })
        const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
        await previewBtn(w).trigger('click')
        await flushPromises()
        expect(inv.preview).toHaveBeenCalledWith('ngf', {}, '/api/farms/f1/invoice-templates/ngf/preview')
      })

      it('does not request a preview for an invalid colour', async () => {
        const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
        await w.find('input[name="color-mode"][value="custom"]').setValue()
        await byLabel(w, 'Accent colour').setValue('blue')
        await previewBtn(w).trigger('click')
        await flushPromises()
        expect(inv.preview).not.toHaveBeenCalled()
        expect(w.find('.preview-error').text()).toContain('Accent colour must be a colour')
      })

      it.each([
        [401, /session has expired/],
        [404, /could not be found/]
      ])('shows a message and closes the tab on %i', async (status, msg) => {
        inv.preview.mockRejectedValue({ response: { status, data: new Blob([JSON.stringify({ status })]) } })
        const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
        await previewBtn(w).trigger('click')
        await flushPromises()
        expect(w.find('.preview-error').text()).toMatch(msg)
        expect(fakeTab.close).toHaveBeenCalled()
      })

      it('shows the API detail on 422', async () => {
        inv.preview.mockRejectedValue({ response: { status: 422, data: new Blob([JSON.stringify({ detail: 'primaryColor must be a colour in the form #RRGGBB (for example #138A44).' })]) } })
        const { w } = await mountView(InvoiceFormView, 'Admin', '/invoices/new')
        await previewBtn(w).trigger('click')
        await flushPromises()
        expect(w.find('.preview-error').text()).toContain('primaryColor must be a colour')
      })
    })
  })
})
