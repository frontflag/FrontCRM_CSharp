/**
 * 采购订单主状态（与采购列表 PurchaseOrderList、后端约定一致）
 * 草稿 0 · 新建 1 · 待审核 2 · 审核通过 10 · 待确认 20 · 已确认 30 · 进行中 50 · 采购完成 100 · 审核失败 -1 · 取消 -2
 */

/** 审核通过起算的状态值 */
export const PO_STATUS_AUDIT_PASSED = 10

/** 供应商已确认起算的状态值；大于等于此才可生成/预览采购单报表 */
export const PO_STATUS_VENDOR_CONFIRMED = 30

/** 列表/详情/报表中的行或 order 对象，兼容 JSON 字段 status（camelCase）与 Status（PascalCase） */
export function normalizePurchaseOrderMainStatus(source: unknown): number {
  if (source == null || typeof source !== 'object') {
    const n = Number(source)
    return Number.isFinite(n) ? n : Number.NaN
  }
  const row = source as Record<string, unknown>
  const v = row.status ?? row.Status
  const n = Number(v)
  return Number.isFinite(n) ? n : Number.NaN
}

/** 仅供应商已确认(30)及之后可生成/预览采购单报表 */
export function purchaseOrderReportAllowed(status: unknown): boolean {
  const s = Number(status)
  return Number.isFinite(s) && s >= PO_STATUS_VENDOR_CONFIRMED
}
