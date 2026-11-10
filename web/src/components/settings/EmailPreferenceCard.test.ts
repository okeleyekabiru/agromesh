import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

vi.mock('@/services/api', () => ({
  emailPreferenceApi: { get: vi.fn(), set: vi.fn() }
}))

import { emailPreferenceApi } from '@/services/api'
import EmailPreferenceCard from './EmailPreferenceCard.vue'

const pref = emailPreferenceApi as unknown as Record<string, ReturnType<typeof vi.fn>>

beforeEach(() => { vi.clearAllMocks() })

describe('EmailPreferenceCard', () => {
  it('shows the current setting and saves a change', async () => {
    pref.get.mockResolvedValue({ data: { enabled: true } })
    pref.set.mockResolvedValue({ data: { enabled: false } })
    const w = mount(EmailPreferenceCard)
    await flushPromises()

    const box = w.find('input[type="checkbox"]')
    expect((box.element as HTMLInputElement).checked).toBe(true)
    expect(w.text()).toContain('always sent')

    await box.setValue(false)
    await flushPromises()
    expect(pref.set).toHaveBeenCalledWith(false)
    expect((box.element as HTMLInputElement).checked).toBe(false)
  })

  it('puts the box back and shows an error when saving fails', async () => {
    pref.get.mockResolvedValue({ data: { enabled: true } })
    pref.set.mockRejectedValue({ response: { data: { detail: 'Nope.' } } })
    const w = mount(EmailPreferenceCard)
    await flushPromises()
    await w.find('input[type="checkbox"]').setValue(false)
    await flushPromises()
    expect((w.find('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(true)
    expect(w.text()).toContain('Nope.')
  })
})
