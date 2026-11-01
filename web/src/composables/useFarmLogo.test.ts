import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, reactive } from 'vue'
import { flushPromises, mount } from '@vue/test-utils'

vi.mock('@/services/api', () => ({
  logoApi: { get: vi.fn() }
}))

import { logoApi } from '@/services/api'
import { clearLogoCache, useFarmLogo } from './useFarmLogo'
import FarmLogo from '@/components/ui/FarmLogo.vue'

const get = logoApi.get as unknown as ReturnType<typeof vi.fn>
let counter = 0
let created: string[] = []
let revoked: string[] = []

beforeEach(() => {
  counter = 0
  created = []
  revoked = []
  clearLogoCache()
  get.mockReset()
  get.mockImplementation(async () => ({ data: new Blob(['png'], { type: 'image/png' }) }))
  URL.createObjectURL = vi.fn(() => { const u = `blob:logo-${++counter}`; created.push(u); return u })
  URL.revokeObjectURL = vi.fn((u: string) => { revoked.push(u) })
})
afterEach(() => vi.clearAllMocks())

function host(src: { farmId?: string | null; hasLogo?: boolean; version?: string | null }) {
  const source = reactive({ ...src })
  const Comp = defineComponent({
    setup() {
      const logo = useFarmLogo(() => source)
      return () => h('div', { 'data-url': logo.url.value ?? '', 'data-failed': String(logo.failed.value) })
    }
  })
  return { source, wrapper: mount(Comp) }
}

describe('useFarmLogo', () => {
  it('fetches the logo as a blob and exposes an object URL', async () => {
    const { wrapper } = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    expect(get).toHaveBeenCalledWith('f1')
    expect(wrapper.attributes('data-url')).toBe('blob:logo-1')
  })

  it('does not call the API when the farm has no logo', async () => {
    const { wrapper } = host({ farmId: 'f1', hasLogo: false })
    await flushPromises()
    expect(get).not.toHaveBeenCalled()
    expect(wrapper.attributes('data-url')).toBe('')
    expect(wrapper.attributes('data-failed')).toBe('false')
  })

  it('refetches and revokes the previous object URL when logoUpdatedAt changes', async () => {
    const { source, wrapper } = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    source.version = 'v2'
    await flushPromises()
    expect(get).toHaveBeenCalledTimes(2)
    expect(revoked).toContain('blob:logo-1')
    expect(wrapper.attributes('data-url')).toBe('blob:logo-2')
  })

  it('drops the image and revokes the URL when the logo is removed', async () => {
    const { source, wrapper } = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    source.hasLogo = false
    await flushPromises()
    expect(revoked).toContain('blob:logo-1')
    expect(wrapper.attributes('data-url')).toBe('')
  })

  it('revokes its object URL on unmount', async () => {
    const { wrapper } = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    wrapper.unmount()
    expect(revoked).toEqual(['blob:logo-1'])
  })

  it('reports failure (so callers fall back to AgroMesh) when the request fails', async () => {
    get.mockRejectedValueOnce(new Error('404'))
    const { wrapper } = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    expect(wrapper.attributes('data-url')).toBe('')
    expect(wrapper.attributes('data-failed')).toBe('true')
  })

  it('shares fetched bytes for the same farm and version', async () => {
    host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    const second = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await flushPromises()
    expect(get).toHaveBeenCalledTimes(1)
    expect(second.wrapper.attributes('data-url')).toBe('blob:logo-2')
  })

  it('ignores a slow response that a newer change superseded', async () => {
    let release!: (v: unknown) => void
    get.mockImplementationOnce(() => new Promise(r => { release = r }))
    const { source, wrapper } = host({ farmId: 'f1', hasLogo: true, version: 'v1' })
    await nextTick()
    source.version = 'v2' // second request resolves immediately
    await flushPromises()
    release({ data: new Blob(['old']) }) // first, stale, finally arrives
    await flushPromises()
    expect(wrapper.attributes('data-url')).toBe('blob:logo-1')
    expect(created).toHaveLength(1) // the stale blob never became an object URL
  })
})

describe('FarmLogo component', () => {
  it('shows the farm logo image with an accessible name when loaded', async () => {
    const w = mount(FarmLogo, { props: { farmId: 'f1', hasLogo: true, version: 'v1', farmName: 'Green Acres' } })
    await flushPromises()
    const img = w.find('img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe('blob:logo-1')
    expect(img.attributes('alt')).toBe('Green Acres logo')
  })

  it('falls back to the AgroMesh mark when the farm has no logo', async () => {
    const w = mount(FarmLogo, { props: { farmId: 'f1', hasLogo: false } })
    await flushPromises()
    expect(w.find('img').exists()).toBe(false)
    expect(w.find('svg').exists()).toBe(true)
  })

  it('falls back to the AgroMesh mark when loading fails', async () => {
    get.mockRejectedValueOnce(new Error('500'))
    const w = mount(FarmLogo, { props: { farmId: 'f1', hasLogo: true, version: 'v1' } })
    await flushPromises()
    expect(w.find('img').exists()).toBe(false)
    expect(w.find('svg').exists()).toBe(true)
  })

  it('falls back to the AgroMesh mark when the image cannot be decoded (200 OK, not an image)', async () => {
    const w = mount(FarmLogo, { props: { farmId: 'f1', hasLogo: true, version: 'v1' } })
    await flushPromises()
    expect(w.find('img').exists()).toBe(true)
    await w.find('img').trigger('error')
    // The undecodable image stays out of sight (display: none) and the AgroMesh mark takes its place.
    expect(w.find('img').isVisible()).toBe(false)
    expect(w.find('svg').exists()).toBe(true)
  })

  it('renders a custom fallback slot', async () => {
    const w = mount(FarmLogo, {
      props: { farmId: 'f1', hasLogo: false },
      slots: { fallback: '<span class="custom">AgroMesh wordmark</span>' }
    })
    expect(w.find('.custom').text()).toBe('AgroMesh wordmark')
  })
})
