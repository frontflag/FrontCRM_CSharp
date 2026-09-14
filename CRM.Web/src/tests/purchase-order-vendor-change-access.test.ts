import { describe, expect, it } from 'vitest'
import {
  canChangePurchaseOrderVendor,
  canChangePurchaseOrderVendorOnOrder
} from '@/utils/purchaseOrderStaffPickRules'

describe('canChangePurchaseOrderVendor', () => {
  it('SYS_ADMIN 可换', () => {
    expect(canChangePurchaseOrderVendor({ isSysAdmin: true, identityType: 1 })).toBe(true)
  })

  it('SYS_MANAGER 主部门为销售部也可换', () => {
    expect(
      canChangePurchaseOrderVendor({
        isSysManager: true,
        identityType: 1,
        roleCodes: ['SYS_MANAGER', 'DEPT_DIRECTOR']
      })
    ).toBe(true)
  })

  it('采购部总监可换', () => {
    expect(canChangePurchaseOrderVendor({ identityType: 2, roleCodes: ['DEPT_DIRECTOR'] })).toBe(true)
  })

  it('采购运营总监可换', () => {
    expect(canChangePurchaseOrderVendor({ identityType: 3, roleCodes: ['DEPT_DIRECTOR'] })).toBe(true)
  })

  it('销售部总监不可换', () => {
    expect(canChangePurchaseOrderVendor({ identityType: 1, roleCodes: ['DEPT_DIRECTOR'] })).toBe(false)
  })

  it('仅 Manager 即使 hasPermission 全放行也不可换', () => {
    expect(
      canChangePurchaseOrderVendor({
        identityType: 1,
        roleCodes: ['SYS_BIZ_MANAGER'],
        hasPermission: () => true
      })
    ).toBe(false)
  })

  it('显式 change-vendor 权限可换', () => {
    expect(
      canChangePurchaseOrderVendor({
        identityType: 2,
        permissionCodes: ['purchase-order.change-vendor']
      })
    ).toBe(true)
  })
})

describe('canChangePurchaseOrderVendorOnOrder', () => {
  it('采购员审核前可换', () => {
    expect(
      canChangePurchaseOrderVendorOnOrder(
        { identityType: 2, permissionCodes: ['purchase-order.write'] },
        2
      )
    ).toBe(true)
  })

  it('采购员审核通过后不可换', () => {
    expect(
      canChangePurchaseOrderVendorOnOrder(
        { identityType: 2, permissionCodes: ['purchase-order.write'] },
        10
      )
    ).toBe(false)
  })

  it('SYS_MANAGER 审核通过后可换', () => {
    expect(
      canChangePurchaseOrderVendorOnOrder(
        { isSysManager: true, identityType: 1, roleCodes: ['SYS_MANAGER'] },
        10
      )
    ).toBe(true)
  })
})
