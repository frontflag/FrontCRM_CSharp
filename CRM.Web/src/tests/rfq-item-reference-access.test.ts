import { describe, expect, it } from 'vitest'
import { canAccessRfqItemReference } from '@/utils/rfqItemReferenceAccess'

describe('canAccessRfqItemReference', () => {
  it('销售采购采购运营可进', () => {
    expect(canAccessRfqItemReference({ identityType: 1 })).toBe(true)
    expect(canAccessRfqItemReference({ identityType: 2 })).toBe(true)
    expect(canAccessRfqItemReference({ identityType: 3 })).toBe(true)
    expect(canAccessRfqItemReference({ identityType: 0, belongsToPurchaseDept: true })).toBe(true)
  })

  it('财务物流客服不可进', () => {
    expect(canAccessRfqItemReference({ identityType: 4 })).toBe(false)
    expect(canAccessRfqItemReference({ identityType: 5 })).toBe(false)
    expect(canAccessRfqItemReference({ identityType: 6 })).toBe(false)
    expect(canAccessRfqItemReference(null)).toBe(false)
  })

  it('系统角色与 bypass 可进', () => {
    expect(canAccessRfqItemReference({ identityType: 6, hasBizDataBypass: true })).toBe(true)
    expect(canAccessRfqItemReference({ identityType: 6, isSysAdmin: true })).toBe(true)
    expect(canAccessRfqItemReference({ identityType: 6, isSysManager: true })).toBe(true)
    expect(canAccessRfqItemReference({ identityType: 6, isBizManager: true })).toBe(true)
  })
})
