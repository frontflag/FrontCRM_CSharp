import { describe, expect, it } from 'vitest'
import { canManualAssignRfqPurchaser } from '@/constants/rfqPurchaserAssign'

describe('canManualAssignRfqPurchaser', () => {
  it('采购部总监可分配', () => {
    expect(
      canManualAssignRfqPurchaser({ identityType: 2, roleCodes: ['DEPT_DIRECTOR'] })
    ).toBe(true)
  })

  it('采购运营总监可分配', () => {
    expect(
      canManualAssignRfqPurchaser({ identityType: 3, roleCodes: ['DEPT_DIRECTOR'] })
    ).toBe(true)
  })

  it('采购部经理不可分配', () => {
    expect(
      canManualAssignRfqPurchaser({ identityType: 2, roleCodes: ['DEPT_MANAGER'] })
    ).toBe(false)
  })

  it('销售总监不可分配', () => {
    expect(
      canManualAssignRfqPurchaser({ identityType: 1, roleCodes: ['DEPT_DIRECTOR'] })
    ).toBe(false)
  })

  it('Admin / SuperAdmin / bypass 可分配', () => {
    expect(canManualAssignRfqPurchaser({ identityType: 1, isSysManager: true })).toBe(true)
    expect(canManualAssignRfqPurchaser({ identityType: 1, isSysAdmin: true })).toBe(true)
    expect(canManualAssignRfqPurchaser({ identityType: 1, hasBizDataBypass: true })).toBe(true)
  })
})
