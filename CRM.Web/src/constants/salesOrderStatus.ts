/**
 * 销售订单主状态（与 CRM.Core SellOrderMainStatus 一致）
 * 1=新建 2=待审核 10=审核通过 20=进行中 100=完成 -1=审核失败 -2=取消
 */
export const SALES_ORDER_STATUS_TEXT: Record<number, string> = {
  1: '新建',
  2: '待审核',
  10: '审核通过',
  20: '进行中',
  100: '完成',
  [-1]: '审核失败',
  [-2]: '取消'
}

export function salesOrderStatusText(status: number): string {
  return SALES_ORDER_STATUS_TEXT[status] ?? '未知'
}

/** i18n keys under salesOrderList.status.* */
export const SALES_ORDER_STATUS_I18N_KEY: Record<number, string> = {
  1: 'salesOrderList.status.new',
  2: 'salesOrderList.status.pendingReview',
  10: 'salesOrderList.status.approved',
  20: 'salesOrderList.status.inProgress',
  100: 'salesOrderList.status.completed',
  [-1]: 'salesOrderList.status.reviewFailed',
  [-2]: 'salesOrderList.status.cancelled'
}

export function translateSalesOrderStatus(status: number, t: (key: string) => string): string {
  const key = SALES_ORDER_STATUS_I18N_KEY[status]
  if (key) return t(key)
  return t('salesOrderList.status.unknown')
}

export function salesOrderStatusTagType(status: number): 'success' | 'warning' | 'info' | 'danger' | 'primary' {
  const map: Record<number, 'success' | 'warning' | 'info' | 'danger' | 'primary'> = {
    1: 'info',
    2: 'warning',
    10: 'success',
    20: 'primary',
    100: 'success',
    [-1]: 'danger',
    [-2]: 'info'
  }
  return map[status] ?? 'info'
}

/** 主表为审核通过 / 进行中 / 完成时，明细方可「申请采购」「申请出库」 */
export function salesOrderMainAllowsPurchaseAndStockOut(status: number): boolean {
  const s = Number(status)
  return s === 10 || s === 20 || s === 100
}

/**
 * 销售明细「申请出库」置灰（与列表/详情 extend 三态一致：0=待 1=部分 2=完成）
 * - 出库状态=完成(2)
 * - 或 采购状态=待采购(0)
 */
export function salesOrderLineApplyStockOutDisabled(row: {
  purchaseProgressStatus?: unknown
  stockOutProgressStatus?: unknown
}): boolean {
  const so = row.stockOutProgressStatus
  if (so !== undefined && so !== null && Number(so) === 2) return true
  const po = row.purchaseProgressStatus
  if (po !== undefined && po !== null && Number(po) === 0) return true
  return false
}

/** 扩展表 PurchasedStock_AvailableQty：同 PN+品牌备货可用量之和 > 0 时放宽申请出库限制 */
export function salesOrderLinePurchasedStockReliefOk(row: { purchasedStockAvailableQty?: unknown }): boolean {
  const n = Number(row.purchasedStockAvailableQty)
  return Number.isFinite(n) && n > 0
}

/**
 * 申请出库按钮禁用：出库完成后一律禁用；备货可用量 > 0 时不禁用；
 * 否则与列表原逻辑一致（待采购/出库完成进度 或 未过采购门槛）。
 */
export function salesOrderLineApplyStockOutButtonDisabled(row: {
  purchaseProgressStatus?: unknown
  stockOutProgressStatus?: unknown
  stockOutApplyPurchaseGateOk?: unknown
  purchasedStockAvailableQty?: unknown
}): boolean {
  const so = row.stockOutProgressStatus
  if (so !== undefined && so !== null && Number(so) === 2) return true
  if (salesOrderLinePurchasedStockReliefOk(row)) return false
  return salesOrderLineApplyStockOutDisabled(row) || row.stockOutApplyPurchaseGateOk !== true
}
