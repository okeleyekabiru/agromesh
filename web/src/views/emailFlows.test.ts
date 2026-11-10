import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'

vi.mock('@/services/api', () => ({
  authApi: { login: vi.fn(), register: vi.fn(), forgotPassword: vi.fn(), resetPassword: vi.fn() },
  invitationApi: { list: vi.fn(), create: vi.fn(), resend: vi.fn(), revoke: vi.fn(), preview: vi.fn(), accept: vi.fn() },
  api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
  featuresApi: { getForFarm: vi.fn() },
  setFarmId: vi.fn()
}))

import { api, authApi, invitationApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import AcceptInviteView from './AcceptInviteView.vue'
import ForgotPasswordView from './ForgotPasswordView.vue'
import ResetPasswordView from './ResetPasswordView.vue'
import TeamView from './TeamView.vue'

const inv = invitationApi as unknown as Record<string, ReturnType<typeof vi.fn>>
const auth = authApi as unknown as Record<string, ReturnType<typeof vi.fn>>
const rawApi = api as unknown as Record<string, ReturnType<typeof vi.fn>>

async function mountAt(component: object, path: string, role = 'Admin') {
  const pinia = createPinia()
  setActivePinia(pinia)
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', name: 'landing', component: { template: '<div/>' } },
      { path: '/dashboard', name: 'dashboard', component: { template: '<div/>' } },
      { path: '/onboarding', name: 'onboarding', component: { template: '<div/>' } },
      { path: '/accept-invite', component: component as never },
      { path: '/reset-password', component: component as never },
      { path: '/forgot-password', component: component as never },
      { path: '/team', component: component as never }
    ]
  })
  router.push(path)
  await router.isReady()
  const store = useAgroStore()
  store.currentUser = { id: 'u1', email: 'o@f.com', fullName: 'Owner', companyId: 'c1', role } as never
  const wrapper = mount(component as never, { global: { plugins: [pinia, router] } })
  await flushPromises()
  return { wrapper, router }
}

beforeEach(() => { vi.clearAllMocks() })

describe('AcceptInviteView', () => {
  it('shows who invited and accepts with a password', async () => {
    inv.preview.mockResolvedValue({ data: { email: 'ada@farm.com', farmName: 'Green Acres', role: 'Supervisor' } })
    inv.accept.mockResolvedValue({ data: { token: 't', user: { id: 'u2', email: 'ada@farm.com', fullName: 'Ada', companyId: 'c1', role: 'Supervisor' }, companyId: 'c1', farms: [] } })
    const { wrapper } = await mountAt(AcceptInviteView, '/accept-invite?token=abc')

    expect(inv.preview).toHaveBeenCalledWith('abc')
    expect(wrapper.text()).toContain('Green Acres')
    expect((wrapper.find('input[type="email"]').element as HTMLInputElement).value).toBe('ada@farm.com')

    await wrapper.find('input[name="fullName"]').setValue('Ada Okafor')
    await wrapper.find('input[name="password"]').setValue('password123')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(inv.accept).toHaveBeenCalledWith({ token: 'abc', fullName: 'Ada Okafor', password: 'password123' })
  })

  it('shows the error and no form when the link is invalid', async () => {
    inv.preview.mockRejectedValue({ response: { data: { detail: 'This invitation link is invalid or has expired.' } } })
    const { wrapper } = await mountAt(AcceptInviteView, '/accept-invite?token=bad')
    expect(wrapper.text()).toContain('invalid or has expired')
    expect(wrapper.find('form').exists()).toBe(false)
  })

  it('refuses a short password without calling the API', async () => {
    inv.preview.mockResolvedValue({ data: { email: 'ada@farm.com', farmName: 'Green Acres', role: 'Supervisor' } })
    const { wrapper } = await mountAt(AcceptInviteView, '/accept-invite?token=abc')
    await wrapper.find('input[name="fullName"]').setValue('Ada')
    await wrapper.find('input[name="password"]').setValue('short')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(inv.accept).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('at least 8')
  })
})

describe('ForgotPasswordView', () => {
  it('submits the email and shows a neutral confirmation', async () => {
    auth.forgotPassword.mockResolvedValue({ data: {} })
    const { wrapper } = await mountAt(ForgotPasswordView, '/forgot-password')
    await wrapper.find('input[type="email"]').setValue('ada@farm.com')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(auth.forgotPassword).toHaveBeenCalledWith({ email: 'ada@farm.com' })
    expect(wrapper.text()).toContain('If an account exists')
  })
})

describe('ResetPasswordView', () => {
  it('sets a new password using the token from the link', async () => {
    auth.resetPassword.mockResolvedValue({ data: {} })
    const { wrapper } = await mountAt(ResetPasswordView, '/reset-password?token=tok')
    await wrapper.find('input[name="password"]').setValue('brandnewpass')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(auth.resetPassword).toHaveBeenCalledWith({ token: 'tok', newPassword: 'brandnewpass' })
    expect(wrapper.text()).toContain('password has been changed')
  })

  it('shows the API error for an expired link', async () => {
    auth.resetPassword.mockRejectedValue({ response: { data: { detail: 'This password reset link is invalid or has expired.' } } })
    const { wrapper } = await mountAt(ResetPasswordView, '/reset-password?token=tok')
    await wrapper.find('input[name="password"]').setValue('brandnewpass')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('invalid or has expired')
  })
})

describe('TeamView invitations', () => {
  const pending = { id: 'i1', email: 'p@farm.com', role: 'Supervisor', status: 'Pending', createdAt: '2026-11-01T00:00:00Z', expiresAt: '2026-11-08T00:00:00Z' }

  beforeEach(() => {
    rawApi.get.mockResolvedValue({ data: [] })
    inv.list.mockResolvedValue({ data: [pending] })
  })

  it('lists invitations and sends a new one by email and role', async () => {
    inv.create.mockResolvedValue({ data: { ...pending, id: 'i2', email: 'new@farm.com' } })
    const { wrapper } = await mountAt(TeamView, '/team')
    expect(wrapper.text()).toContain('p@farm.com')

    await wrapper.find('input[type="email"]').setValue('new@farm.com')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(inv.create).toHaveBeenCalledWith({ email: 'new@farm.com', role: 'FarmManager' })
    expect(wrapper.text()).toContain('Invitation sent')
  })

  it('resends and revokes a pending invitation', async () => {
    inv.resend.mockResolvedValue({ data: pending })
    inv.revoke.mockResolvedValue({ data: { ...pending, status: 'Revoked' } })
    const { wrapper } = await mountAt(TeamView, '/team')

    await wrapper.find('[data-test="resend-i1"]').trigger('click')
    await flushPromises()
    expect(inv.resend).toHaveBeenCalledWith('i1')

    await wrapper.find('[data-test="revoke-i1"]').trigger('click')
    await flushPromises()
    expect(inv.revoke).toHaveBeenCalledWith('i1')
  })

  it('does not let a farm manager pick the farm manager role', async () => {
    const { wrapper } = await mountAt(TeamView, '/team', 'FarmManager')
    const options = wrapper.findAll('select option').map(o => o.element.getAttribute('value'))
    expect(options).toEqual(['Supervisor'])
  })
})
