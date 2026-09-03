import { describe, expect, it } from 'vitest'
import { isValidCustomsCostUsd } from '@/utils/customsCostUsd'

describe('isValidCustomsCostUsd', () => {
  it('accepts positive numbers', () => {
    expect(isValidCustomsCostUsd(0.000001)).toBe(true)
    expect(isValidCustomsCostUsd(99999.123456)).toBe(true)
  })

  it('rejects zero and negative', () => {
    expect(isValidCustomsCostUsd(0)).toBe(false)
    expect(isValidCustomsCostUsd(-1)).toBe(false)
  })

  it('rejects non-finite', () => {
    expect(isValidCustomsCostUsd(Number.NaN)).toBe(false)
    expect(isValidCustomsCostUsd('abc')).toBe(false)
  })
})
