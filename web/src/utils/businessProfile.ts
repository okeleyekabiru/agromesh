// Form model and request building for the farm business profile card. Kept free of Vue for unit tests.
import type { FarmBusinessProfile } from '@/types'
import { colorError, normalizeColor } from '@/utils/brandColors'

export type BusinessProfileForm = Record<keyof FarmBusinessProfile, string>

export function blankBusinessProfileForm(): BusinessProfileForm {
  return {
    address: '', phone: '', email: '', website: '', bankAccountName: '', bankAccountNumber: '', bankName: '',
    brandPrimaryColor: '', brandAccentColor: ''
  }
}

const COLOR_KEYS: (keyof FarmBusinessProfile)[] = ['brandPrimaryColor', 'brandAccentColor']

/** Copies a profile from the API into the form, turning null/missing into ''. */
export function profileToForm(p: Partial<FarmBusinessProfile> | null | undefined): BusinessProfileForm {
  const f = blankBusinessProfileForm()
  for (const k of Object.keys(f) as (keyof FarmBusinessProfile)[]) f[k] = p?.[k] ?? ''
  return f
}

/** Problems with the two brand colours (empty when fine). Blank is allowed: it means "use the default". */
export function validateBrandColors(f: Pick<BusinessProfileForm, 'brandPrimaryColor' | 'brandAccentColor'>): { primary: string; accent: string } {
  return {
    primary: colorError('Primary colour', f.brandPrimaryColor),
    accent: colorError('Accent colour', f.brandAccentColor)
  }
}

/**
 * PUT replaces the whole profile, so every field is sent: a blank text field and a blank (or reset)
 * colour are null, which clears them. Colours are upper-cased to match what the API stores.
 */
export function buildBusinessProfileRequest(f: BusinessProfileForm): Record<keyof FarmBusinessProfile, string | null> {
  const out = {} as Record<keyof FarmBusinessProfile, string | null>
  for (const k of Object.keys(f) as (keyof FarmBusinessProfile)[]) {
    out[k] = COLOR_KEYS.includes(k) ? normalizeColor(f[k]) : f[k].trim() || null
  }
  return out
}
