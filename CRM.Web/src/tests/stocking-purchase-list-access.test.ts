import { describe, expect, it } from 'vitest'
import { canAccessStockingPurchaseList } from '@/utils/stockingPurchaseListAccess'

describe('stockingPurchaseListAccess', () => {
  it('allows sys admin', () => {
    expect(canAccessStockingPurchaseList({ isSysAdmin: true }, false)).toBe(true)
  })

  it('allows purchase dept with po read', () => {
    expect(
      canAccessStockingPurchaseList({ belongsToPurchaseDept: true, purchaseDataScope: 1 }, true)
    ).toBe(true)
  })

  it('allows purchase ops role with po read', () => {
    expect(
      canAccessStockingPurchaseList({ roleCodes: ['purchase_ops_operator'], purchaseDataScope: 1 }, true)
    ).toBe(true)
  })

  it('rejects logistics with po read', () => {
    expect(canAccessStockingPurchaseList({ identityType: 6, purchaseDataScope: 0 }, true)).toBe(false)
  })

  it('rejects purchase dept without po read', () => {
    expect(canAccessStockingPurchaseList({ belongsToPurchaseDept: true }, false)).toBe(false)
  })

  it('rejects forbidden purchase scope', () => {
    expect(
      canAccessStockingPurchaseList({ belongsToPurchaseDept: true, purchaseDataScope: 4 }, true)
    ).toBe(false)
  })
})
