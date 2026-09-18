import { describe, expect, it } from 'vitest'
import zhCN from '@/locales/zh-CN'
import enUS from '@/locales/en-US'
import {
  computePurchaseOrderItemPurchaseProfitRate,
  computePurchaseOrderItemPurchaseProfitUsd
} from '@/utils/purchaseOrderItemPurchaseProfit'

describe('computePurchaseOrderItemPurchaseProfitUsd', () => {
  it('uses USD unit spread times purchase qty', () => {
    expect(computePurchaseOrderItemPurchaseProfitUsd(1.2, 0.8, 100)).toBe(40)
  })

  it('allows negative when purchase is higher', () => {
    expect(computePurchaseOrderItemPurchaseProfitUsd(0.5, 0.8, 10)).toBe(-3)
  })

  it('returns null when sell or purchase convert price is missing', () => {
    expect(computePurchaseOrderItemPurchaseProfitUsd(null, 1, 5)).toBeNull()
    expect(computePurchaseOrderItemPurchaseProfitUsd(0, 1, 5)).toBeNull()
    expect(computePurchaseOrderItemPurchaseProfitUsd(1, null, 5)).toBeNull()
    expect(computePurchaseOrderItemPurchaseProfitUsd(1, 0, 5)).toBeNull()
  })

  it('rounds away from zero to cents', () => {
    expect(computePurchaseOrderItemPurchaseProfitUsd(1.004, 1, 1)).toBe(0)
    expect(computePurchaseOrderItemPurchaseProfitUsd(1.006, 1, 1)).toBe(0.01)
  })
})

describe('computePurchaseOrderItemPurchaseProfitRate', () => {
  it('is unit price ratio of convert USD prices', () => {
    expect(computePurchaseOrderItemPurchaseProfitRate(12, 8)).toBe(1.5)
    expect(computePurchaseOrderItemPurchaseProfitRate(0, 8)).toBeNull()
    expect(computePurchaseOrderItemPurchaseProfitRate(12, 0)).toBeNull()
  })
})

describe('approvalDesktop purchase profit tip copy', () => {
  it('uses convert-USD wording in zh and en', () => {
    const zh = zhCN.approvalDesktop.orderRef
    const en = enUS.approvalDesktop.orderRef
    expect(zh.purchaseProfitTipFormula).toContain('销售单价折算USD')
    expect(zh.purchaseProfitTipFormula).toContain('采购单价折算USD')
    expect(zh.purchaseProfitRateTipFormula).toContain('销售单价折算USD ÷ 采购单价折算USD')
    expect(en.purchaseProfitTipFormula).toContain('Sell unit price converted to USD')
    expect(en.purchaseProfitRateTipFormula).toContain(
      'Sell unit price converted to USD ÷ Purchase unit price converted to USD'
    )
  })
})
