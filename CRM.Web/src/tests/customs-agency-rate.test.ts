import { describe, expect, it } from 'vitest'
import { isValidCustomsAgencyRate } from '@/utils/customsAgencyRate'

describe('isValidCustomsAgencyRate', () => {
  it('accepts 1 and 1.025', () => {
    expect(isValidCustomsAgencyRate(1)).toBe(true)
    expect(isValidCustomsAgencyRate(1.025)).toBe(true)
    expect(isValidCustomsAgencyRate('1.025000')).toBe(true)
  })

  it('rejects below 1 and non-numeric', () => {
    expect(isValidCustomsAgencyRate(0.999)).toBe(false)
    expect(isValidCustomsAgencyRate(0)).toBe(false)
    expect(isValidCustomsAgencyRate(NaN)).toBe(false)
    expect(isValidCustomsAgencyRate('abc')).toBe(false)
    expect(isValidCustomsAgencyRate(null)).toBe(false)
  })

  it('has no upper bound', () => {
    expect(isValidCustomsAgencyRate(2)).toBe(true)
    expect(isValidCustomsAgencyRate(10)).toBe(true)
  })
})
