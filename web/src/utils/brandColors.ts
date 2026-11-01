// Brand colours for invoices: validation, normalisation, preview URL building and messages.
// Free of Vue so it can be unit tested. The API re-validates everything (422 on a bad colour).

/** The original NGF template colours, used when the template list has not loaded. */
export const DEFAULT_PRIMARY_COLOR = '#138A44'
export const DEFAULT_ACCENT_COLOR = '#F5C90C'

const HEX_RE = /^#[0-9A-Fa-f]{6}$/

export type ColorSource = 'invoice' | 'farm' | 'default'

/** True only for exactly #RRGGBB (6 hex digits and a leading #), in any case. */
export function isHexColor(value: string | null | undefined): boolean {
  return typeof value === 'string' && HEX_RE.test(value.trim())
}

/** Blank means "not set" and becomes null; anything else is trimmed and upper-cased (as the API returns it). */
export function normalizeColor(value: string | null | undefined): string | null {
  const v = (value ?? '').trim()
  return v ? v.toUpperCase() : null
}

/** '' when the colour is blank (not set) or valid; otherwise a message naming the field. */
export function colorError(label: string, value: string | null | undefined): string {
  const v = (value ?? '').trim()
  if (!v || HEX_RE.test(v)) return ''
  return `${label} must be a colour in the form #RRGGBB, for example #138A44.`
}

/** What a native <input type="color"> can show: always lower-case #rrggbb. */
export function pickerValue(value: string | null | undefined, fallback: string): string {
  return (isHexColor(value) ? value!.trim() : isHexColor(fallback) ? fallback : '#000000').toLowerCase()
}

/** Where the colour on an invoice comes from, in words. */
export function describeColorSource(source: ColorSource | string | null | undefined): string {
  if (source === 'invoice') return 'set on this invoice'
  if (source === 'farm') return "your farm's brand colour"
  return 'the template default'
}

export interface PreviewColors { primaryColor?: string | null; accentColor?: string | null }

/** The preview route for a template of a farm, used when the template list carries no previewUrl. */
export function defaultPreviewPath(farmId: string, templateKey: string): string {
  return `/api/farms/${encodeURIComponent(farmId)}/invoice-templates/${encodeURIComponent(templateKey)}/preview`
}

/**
 * Adds the colours to a preview path as ?primaryColor=%23RRGGBB&accentColor=%23RRGGBB. Blank or invalid
 * colours are left out so the server falls back to the farm brand colour, then the template default.
 */
export function buildPreviewUrl(basePath: string, colors: PreviewColors = {}): string {
  const params: string[] = []
  const p = normalizeColor(colors.primaryColor)
  const a = normalizeColor(colors.accentColor)
  if (p && HEX_RE.test(p)) params.push(`primaryColor=${encodeURIComponent(p)}`)
  if (a && HEX_RE.test(a)) params.push(`accentColor=${encodeURIComponent(a)}`)
  if (!params.length) return basePath
  return `${basePath}${basePath.includes('?') ? '&' : '?'}${params.join('&')}`
}

/** A friendly message for a failed preview request (the error body has already been read from the blob). */
export function previewErrorMessage(e: unknown): string {
  const err = e as { response?: { status?: number; data?: { detail?: string } } }
  switch (err?.response?.status) {
    case 401: return 'Your session has expired. Please sign in again to preview.'
    case 404: return 'This template could not be found for your farm, so there is nothing to preview.'
    case 422: return err.response?.data?.detail || 'One of the colours is not valid. Use the form #RRGGBB, for example #138A44.'
    default: return 'Could not load the preview. Please try again.'
  }
}
