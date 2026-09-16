import { describe, expect, it } from 'vitest'
import { canAccessQuoteDesktop, canQuoteRfqItem } from '@/utils/rfqItemQuoteAccessRules'

const otherRow = { assignedPurchaserUserId1: 'other' }

describe('canQuoteRfqItem', () => {
  it('SYS_ADMIN 可报价未分配给自己的行', () => {
    expect(canQuoteRfqItem({ id: 'u1', isSysAdmin: true, identityType: 1 }, otherRow)).toBe(true)
  })

  it('SYS_MANAGER 可报价未分配给自己的行', () => {
    expect(
      canQuoteRfqItem({ id: 'u1', isSysManager: true, identityType: 1, roleCodes: ['SYS_MANAGER'] }, otherRow)
    ).toBe(true)
  })

  it('销售业务员不可报价未分配行', () => {
    expect(canQuoteRfqItem({ id: 'u1', identityType: 1 }, otherRow)).toBe(false)
  })

  it('分配采购员可报价', () => {
    expect(
      canQuoteRfqItem({ id: 'u1', identityType: 2 }, { assignedPurchaserUserId1: 'u1' })
    ).toBe(true)
  })
})

describe('canAccessQuoteDesktop', () => {
  it('SYS_MANAGER 显示报价桌面入口', () => {
    expect(canAccessQuoteDesktop({ isSysManager: true, identityType: 1, roleCodes: ['SYS_MANAGER'] })).toBe(
      true
    )
  })

  it('纯销售业务员不显示报价桌面入口', () => {
    expect(canAccessQuoteDesktop({ identityType: 1 })).toBe(false)
  })
})
