import { describe, expect, it } from 'vitest'
import {
  canAccessCommissionPool,
  canSeeAllCommissionPoolRows,
  isCommissionPoolPurchaser,
  isCommissionPoolSalesperson
} from '@/utils/commissionPoolAccess'

describe('commissionPoolAccess', () => {
  it('管理员与 bypass 可进且看全部', () => {
    expect(canAccessCommissionPool({ isSysAdmin: true, identityType: 6 })).toBe(true)
    expect(canAccessCommissionPool({ isSysManager: true, identityType: 5 })).toBe(true)
    expect(canAccessCommissionPool({ hasBizDataBypass: true, identityType: 4 })).toBe(true)
    expect(canSeeAllCommissionPoolRows({ isSysAdmin: true })).toBe(true)
  })

  it('业务员采购员可进，只看本人', () => {
    expect(canAccessCommissionPool({ identityType: 1 })).toBe(true)
    expect(canAccessCommissionPool({ identityType: 2 })).toBe(true)
    expect(canAccessCommissionPool({ identityType: 3 })).toBe(true)
    expect(canAccessCommissionPool({ identityType: 0, belongsToPurchaseDept: true })).toBe(true)
    expect(canSeeAllCommissionPoolRows({ identityType: 1 })).toBe(false)
    expect(isCommissionPoolSalesperson({ identityType: 1 })).toBe(true)
    expect(isCommissionPoolPurchaser({ identityType: 2 })).toBe(true)
  })

  it('财务物流客服不可进', () => {
    expect(canAccessCommissionPool(null)).toBe(false)
    expect(canAccessCommissionPool({ identityType: 0 })).toBe(false)
    expect(canAccessCommissionPool({ identityType: 4 })).toBe(false)
    expect(canAccessCommissionPool({ identityType: 5 })).toBe(false)
    expect(canAccessCommissionPool({ identityType: 6 })).toBe(false)
  })
})
