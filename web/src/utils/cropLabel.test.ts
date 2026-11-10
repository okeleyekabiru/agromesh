import { describe, it, expect } from 'vitest'
import { cropLabel, normalizeBlock } from './enums'
import type { Block } from '@/types'

describe('cropLabel', () => {
  it('spells out built-in codes', () => {
    expect(cropLabel('OilPalm')).toBe('Oil palm')
  })
  it('leaves single words and custom names alone', () => {
    expect(cropLabel('Maize')).toBe('Maize')
    expect(cropLabel('Sweet Potato')).toBe('Sweet Potato')
    expect(cropLabel('Yam')).toBe('Yam')
  })
})

describe('normalizeBlock', () => {
  it('keeps the crop id and name the API sends', () => {
    const b = normalizeBlock({ crops: [{ cropId: 'x1', cropName: 'Yam', percentage: 100 }] } as unknown as Block)
    expect(b.crops[0]).toMatchObject({ cropId: 'x1', cropName: 'Yam' })
  })
})
