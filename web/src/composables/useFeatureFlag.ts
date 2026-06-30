import { useAgroStore } from '@/stores/agro'

export function useFeatureFlag() {
  const store = useAgroStore()

  function hasFeature(name: string): boolean {
    // No flags loaded yet (API not called, or dev mode) → show everything
    if (store.enabledFeatures.length === 0) return true
    return store.enabledFeatures.includes(name)
  }

  return { hasFeature }
}
