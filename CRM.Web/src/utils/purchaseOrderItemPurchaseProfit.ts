/** 与后端 PurchaseOrderItemPurchaseProfitCalc 一致：四舍五入远离 0 到分。 */
function roundAwayFromZeroToCents(value: number): number {
  const scaled = Math.abs(value) * 100
  const rounded = Math.round(scaled)
  return ((Math.sign(value) || 1) * rounded) / 100
}

/**
 * 采购订单明细「采购利润」（审批桌面与列表同一公式）：
 * (销售折算美金单价 − 采购折算美金单价) × 采购数量。
 * 任一侧折算美金单价缺失或 ≤ 0 时返回 null（界面「—」）。
 */
export function computePurchaseOrderItemPurchaseProfitUsd(
  sellConvertUsdUnitPrice: number | null | undefined,
  purchaseConvertUsdUnitPrice: number | null | undefined,
  qty: number
): number | null {
  const sell = Number(sellConvertUsdUnitPrice)
  const purchase = Number(purchaseConvertUsdUnitPrice)
  const q = Number(qty)
  if (!(sell > 0) || !(purchase > 0) || !Number.isFinite(q)) return null
  return roundAwayFromZeroToCents((sell - purchase) * q)
}

/** 采购利润率（单价比）：销售单价折算USD ÷ 采购单价折算USD。 */
export function computePurchaseOrderItemPurchaseProfitRate(
  sellConvertUsdUnitPrice: number | null | undefined,
  purchaseConvertUsdUnitPrice: number | null | undefined
): number | null {
  const sell = Number(sellConvertUsdUnitPrice)
  const purchase = Number(purchaseConvertUsdUnitPrice)
  if (!(sell > 0) || !(purchase > 0)) return null
  return sell / purchase
}
