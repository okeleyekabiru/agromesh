import { ref, watch, onScopeDispose, type Ref } from 'vue'
import { logoApi } from '@/services/api'

// GET /api/farms/{id}/logo needs the bearer token, so an <img src> cannot load it. The bytes are
// fetched through the axios client as a blob and shown through an object URL. Blobs (not URLs) are
// cached per farm + version so the sidebar and the settings preview share one request; each user of
// the composable makes, and revokes, its own object URL.

const MAX_CACHED = 8
const blobCache = new Map<string, Blob>()

function cacheKey(farmId: string, version: string | null | undefined) {
  return `${farmId}|${version ?? ''}`
}

function remember(key: string, blob: Blob) {
  blobCache.delete(key)
  blobCache.set(key, blob)
  while (blobCache.size > MAX_CACHED) {
    const oldest = blobCache.keys().next().value
    if (oldest === undefined) break
    blobCache.delete(oldest)
  }
}

/** Forget cached logo bytes (all farms, or one farm). */
export function clearLogoCache(farmId?: string) {
  if (!farmId) { blobCache.clear(); return }
  for (const key of [...blobCache.keys()]) if (key.startsWith(`${farmId}|`)) blobCache.delete(key)
}

export interface LogoSource {
  farmId?: string | null
  hasLogo?: boolean | null
  /** farm.logoUpdatedAt: changes whenever the logo changes, which busts the cache. */
  version?: string | null
}

export interface FarmLogoState {
  /** Object URL of the logo, or null when there is none / it could not be loaded. */
  url: Ref<string | null>
  loading: Ref<boolean>
  /** True when the farm should have a logo but loading it failed (callers fall back to AgroMesh). */
  failed: Ref<boolean>
}

export function useFarmLogo(source: () => LogoSource): FarmLogoState {
  const url = ref<string | null>(null)
  const loading = ref(false)
  const failed = ref(false)
  let current: string | null = null
  let ticket = 0

  function release() {
    if (current) URL.revokeObjectURL(current)
    current = null
    url.value = null
  }

  async function load(src: LogoSource) {
    const mine = ++ticket
    failed.value = false
    if (!src.farmId || !src.hasLogo) {
      release()
      loading.value = false
      return
    }
    const key = cacheKey(src.farmId, src.version)
    loading.value = true
    try {
      let blob = blobCache.get(key)
      if (!blob) {
        const res = await logoApi.get(src.farmId)
        blob = res.data as Blob
        remember(key, blob)
      }
      if (mine !== ticket) return // a newer request superseded this one
      release()
      current = URL.createObjectURL(blob)
      url.value = current
    } catch {
      if (mine !== ticket) return
      release()
      failed.value = true
    } finally {
      if (mine === ticket) loading.value = false
    }
  }

  watch(
    () => {
      const s = source()
      return [s.farmId ?? null, !!s.hasLogo, s.version ?? null] as const
    },
    () => { void load(source()) },
    { immediate: true }
  )

  onScopeDispose(() => {
    ticket++
    release()
  })

  return { url, loading, failed }
}
