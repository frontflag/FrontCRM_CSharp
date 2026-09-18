import { describe, expect, it } from 'vitest'
import { formatCommissionDocCode } from '@/api/commission'

describe('formatCommissionDocCode', () => {
  it('优先明细单号，没有则回退单据号', () => {
    expect(formatCommissionDocCode(' STO1-2 ', 'STO1')).toBe('STO1-2')
    expect(formatCommissionDocCode('  ', 'STO1')).toBe('STO1')
    expect(formatCommissionDocCode(null, null)).toBe('')
  })
})
