import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

vi.mock('@/services/api', () => ({
  businessProfileApi: { get: vi.fn(), update: vi.fn() },
  invoiceApi: { templates: vi.fn() },
  logoApi: {},
  setFarmId: vi.fn()
}))

import { businessProfileApi, invoiceApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import BusinessProfileCard from './BusinessProfileCard.vue'

const bp = businessProfileApi as unknown as Record<string, ReturnType<typeof vi.fn>>
const tpl = invoiceApi.templates as unknown as ReturnType<typeof vi.fn>

async function mountCard(role = 'Admin') {
  const pinia = createPinia()
  setActivePinia(pinia)
  const store = useAgroStore()
  store.currentUser = { id: 'u1', email: 'a@b.c', fullName: 'T', companyId: 'c', role } as never
  store.activeFarmId = 'f1'
  const w = mount(BusinessProfileCard, { global: { plugins: [pinia] } })
  await flushPromises()
  return w
}

beforeEach(() => {
  bp.get.mockReset(); bp.update.mockReset(); tpl.mockReset()
  bp.get.mockResolvedValue({ data: { phone: '0803', bankName: 'GTBank', brandPrimaryColor: '#B91C1C', brandAccentColor: null } })
  bp.update.mockResolvedValue({ data: {} })
  tpl.mockResolvedValue({ data: [{ key: 'ngf', name: 'NGF', description: null, isDefault: true, defaultPrimaryColor: '#138A44', defaultAccentColor: '#F5C90C' }] })
})

describe('BusinessProfileCard brand colours', () => {
  it('shows the saved colour and the template defaults', async () => {
    const w = await mountCard()
    expect((w.find('#bp-brand-primary').element as HTMLInputElement).value).toBe('#B91C1C')
    expect((w.find('#bp-brand-accent').element as HTMLInputElement).placeholder).toBe('#F5C90C')
    expect(w.text()).toContain('#138A44')
  })

  it('saves the whole profile with the edited colour', async () => {
    const w = await mountCard()
    await w.find('#bp-brand-accent').setValue('#f5c90c')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(bp.update).toHaveBeenCalledWith('f1', expect.objectContaining({
      phone: '0803', bankName: 'GTBank', brandPrimaryColor: '#B91C1C', brandAccentColor: '#F5C90C', email: null
    }))
  })

  it('reset to default clears both colours and saves them as null', async () => {
    const w = await mountCard()
    await w.findAll('button').find(b => b.text() === 'Reset to default')!.trigger('click')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(bp.update).toHaveBeenCalledWith('f1', expect.objectContaining({ brandPrimaryColor: null, brandAccentColor: null, phone: '0803' }))
  })

  it('refuses an invalid colour without calling the API', async () => {
    const w = await mountCard()
    await w.find('#bp-brand-primary').setValue('red')
    await w.find('form').trigger('submit')
    await flushPromises()
    expect(bp.update).not.toHaveBeenCalled()
    expect(w.text()).toContain('Primary colour must be a colour in the form #RRGGBB')
  })

  it('is read-only for members', async () => {
    const w = await mountCard('Member')
    expect(w.find('#bp-brand-primary').attributes('disabled')).toBeDefined()
    expect(w.text()).not.toContain('Reset to default')
  })
})
