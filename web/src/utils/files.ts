// Client-side checks before an upload. The API validates by magic bytes and answers 413/415/422,
// these just save a round trip and give a clear message.

export interface FileCheck { ok: boolean; message: string }
type FileLike = { name: string; size: number; type: string }

const MB = 1024 * 1024

export const LOGO_TYPES = ['image/png', 'image/jpeg', 'image/webp']
export const LOGO_MAX_BYTES = 2 * MB
export const LOGO_MAX_DIMENSION = 4096

export const VENDOR_FILE_TYPES = ['application/pdf', 'image/png', 'image/jpeg']
export const VENDOR_FILE_MAX_BYTES = 10 * MB

const EXT_TYPES: Record<string, string> = {
  png: 'image/png', jpg: 'image/jpeg', jpeg: 'image/jpeg', webp: 'image/webp', pdf: 'application/pdf'
}

function extOf(name: string): string {
  const i = name.lastIndexOf('.')
  return i < 0 ? '' : name.slice(i + 1).toLowerCase()
}

/** Browsers sometimes leave `type` empty; fall back to the extension. */
function typeOf(file: FileLike): string {
  return file.type || EXT_TYPES[extOf(file.name)] || ''
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < MB) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / MB).toFixed(1)} MB`
}

function check(file: FileLike, types: string[], max: number, typeMsg: string, maxLabel: string): FileCheck {
  if (!file.size) return { ok: false, message: 'That file is empty.' }
  if (!types.includes(typeOf(file))) return { ok: false, message: typeMsg }
  if (file.size > max) return { ok: false, message: `That file is larger than the ${maxLabel} limit (${file.size.toLocaleString('en-NG')} bytes).` }
  return { ok: true, message: '' }
}

export function validateLogoFile(file: FileLike): FileCheck {
  return check(file, LOGO_TYPES, LOGO_MAX_BYTES, 'The logo must be a PNG, JPEG or WebP image.', '2 MB')
}

export function validateVendorFile(file: FileLike): FileCheck {
  return check(file, VENDOR_FILE_TYPES, VENDOR_FILE_MAX_BYTES, 'The invoice file must be a PDF, PNG or JPEG.', '10 MB')
}

/** Reads width/height of an image file. Resolves null when the browser cannot decode it. */
export function readImageSize(file: File): Promise<{ width: number; height: number } | null> {
  return new Promise(resolve => {
    const url = URL.createObjectURL(file)
    const img = new Image()
    img.onload = () => { URL.revokeObjectURL(url); resolve({ width: img.naturalWidth, height: img.naturalHeight }) }
    img.onerror = () => { URL.revokeObjectURL(url); resolve(null) }
    img.src = url
  })
}
