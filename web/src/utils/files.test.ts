import { describe, expect, it } from 'vitest'
import { formatBytes, validateLogoFile, validateVendorFile } from './files'

const MB = 1024 * 1024
const f = (name: string, size: number, type: string) => ({ name, size, type })

describe('validateVendorFile', () => {
  it('accepts PDF, PNG and JPEG', () => {
    expect(validateVendorFile(f('a.pdf', 1000, 'application/pdf')).ok).toBe(true)
    expect(validateVendorFile(f('a.png', 1000, 'image/png')).ok).toBe(true)
    expect(validateVendorFile(f('a.jpg', 1000, 'image/jpeg')).ok).toBe(true)
  })
  it('accepts exactly 10 MB and rejects anything over', () => {
    expect(validateVendorFile(f('a.pdf', 10 * MB, 'application/pdf')).ok).toBe(true)
    const big = validateVendorFile(f('a.pdf', 10 * MB + 1, 'application/pdf'))
    expect(big.ok).toBe(false)
    expect(big.message).toMatch(/larger than the 10 MB limit/)
  })
  it('rejects other types, including GIF, HTML and executables', () => {
    for (const [n, t] of [['a.gif', 'image/gif'], ['a.html', 'text/html'], ['a.exe', 'application/x-msdownload'], ['a.webp', 'image/webp']]) {
      const r = validateVendorFile(f(n, 100, t))
      expect(r.ok, n).toBe(false)
      expect(r.message).toMatch(/PDF, PNG or JPEG/)
    }
  })
  it('falls back to the extension when the browser gives no type', () => {
    expect(validateVendorFile(f('scan.JPEG', 100, '')).ok).toBe(true)
    expect(validateVendorFile(f('scan.txt', 100, '')).ok).toBe(false)
  })
  it('rejects an empty file', () => {
    expect(validateVendorFile(f('a.pdf', 0, 'application/pdf')).ok).toBe(false)
  })
})

describe('validateLogoFile', () => {
  it('accepts PNG, JPEG and WebP up to 2 MB', () => {
    expect(validateLogoFile(f('l.png', 2 * MB, 'image/png')).ok).toBe(true)
    expect(validateLogoFile(f('l.webp', 10, 'image/webp')).ok).toBe(true)
  })
  it('rejects SVG, PDF and files over 2 MB', () => {
    expect(validateLogoFile(f('l.svg', 10, 'image/svg+xml')).ok).toBe(false)
    expect(validateLogoFile(f('l.pdf', 10, 'application/pdf')).ok).toBe(false)
    const big = validateLogoFile(f('l.png', 2 * MB + 1, 'image/png'))
    expect(big.ok).toBe(false)
    expect(big.message).toBe('That file is larger than the 2 MB limit (2,097,153 bytes).')
  })
})

describe('formatBytes', () => {
  it('uses B, KB and MB', () => {
    expect(formatBytes(512)).toBe('512 B')
    expect(formatBytes(2048)).toBe('2.0 KB')
    expect(formatBytes(3 * MB)).toBe('3.0 MB')
  })
})
