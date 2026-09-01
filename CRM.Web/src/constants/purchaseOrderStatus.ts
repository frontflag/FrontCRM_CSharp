/**
 * 采购订单主状态（与采购列表 PurchaseOrderList、后端约定一致）
 * 草稿 0 · 新建 1 · 待审核 2 · 审核通过 10 · 待确认 20 · 已确认 30 · 进行中 50 · 采购完成 100 · 审核失败 -1 · 取消 -2
 */

/** 审核通过起算的状态值 */
export const PO_STATUS_AUDIT_PASSED = 10

/** 待供应商确认 */
export const PO_STATUS_PENDING_CONFIRM = 20

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

function isCancelledOrAuditFailed(status: number): boolean {
  return status === -1 || status === -2
}

/**
 * 是否允许创建到货通知（与后端 CreateArrivalNoticeAsync 对齐）。
 * - 优先看采购主单 status ≥ 30（已确认及之后：30/50/100）
 * - 无主单字段时，回退明细里程碑 status ≥ 30（含已付款 40 / 已发货 50 / 已入库 60）
 * - 明细或主单为审核失败/取消则不允许
 */
export function purchaseOrderAllowsArrivalNotice(source: unknown): boolean {
  if (source == null || typeof source !== 'object') return false
  const row = source as Record<string, unknown>

  const itemStatus = Number(row.itemStatus ?? row.ItemStatus)
  if (Number.isFinite(itemStatus) && isCancelledOrAuditFailed(itemStatus)) return false

  const orderStatus = Number(
    row.orderStatus ?? row.OrderStatus ?? row.purchaseOrderStatus ?? row.PurchaseOrderStatus
  )
  if (Number.isFinite(orderStatus)) {
    if (isCancelledOrAuditFailed(orderStatus)) return false
    return orderStatus >= PO_STATUS_VENDOR_CONFIRMED
  }

  if (!Number.isFinite(itemStatus)) return false
  return itemStatus >= PO_STATUS_VENDOR_CONFIRMED
}

function readPoItemStatus(row: Record<string, unknown>): number {
  return Number(row.itemStatus ?? row.ItemStatus)
}

function readPoOrderStatus(row: Record<string, unknown>): number {
  return Number(
    row.orderStatus ?? row.OrderStatus ?? row.purchaseOrderStatus ?? row.PurchaseOrderStatus
  )
}

/**
 * 明细或主单已取消 / 审核失败（申请付款禁用的直接原因）。
 */
export function purchaseOrderApplyPaymentIsCancelled(source: unknown): boolean {
  if (source == null || typeof source !== 'object') return false
  const row = source as Record<string, unknown>
  const itemStatus = readPoItemStatus(row)
  if (Number.isFinite(itemStatus) && isCancelledOrAuditFailed(itemStatus)) return true
  const orderStatus = readPoOrderStatus(row)
  return Number.isFinite(orderStatus) && isCancelledOrAuditFailed(orderStatus)
}

/**
 * 明细付款已完成：优先扩展表进度（已核销 vs 行总额），缺省再看 FinancePaymentStatus。
 * 字段缺失时不视为已完成。
 */
export function purchaseOrderFinancePaymentIsComplete(source: unknown): boolean {
  if (source == null || typeof source !== 'object') return false
  const row = source as Record<string, unknown>
  const progress = row.paymentProgressStatus ?? row.PaymentProgressStatus
  if (progress != null && progress !== '') {
    const p = Number(progress)
    if (Number.isFinite(p)) return p >= 2
  }
  const raw = row.financePaymentStatus ?? row.FinancePaymentStatus
  if (raw == null || raw === '') return false
  const n = Number(raw)
  return Number.isFinite(n) && n >= 2
}

/**
 * 是否满足申请付款的状态门槛（与后端 canApplyPayment 状态段对齐，不含权限/余额）。
 * 主单或明细任一 ≥ 已确认(30) 即可（进行中 50 / 完成 100 仍可分批请款）。
 */
export function purchaseOrderAllowsApplyPayment(source: unknown): boolean {
  if (source == null || typeof source !== 'object') return false
  const row = source as Record<string, unknown>

  const itemStatus = readPoItemStatus(row)
  if (Number.isFinite(itemStatus) && isCancelledOrAuditFailed(itemStatus)) return false

  const orderStatus = readPoOrderStatus(row)
  if (Number.isFinite(orderStatus) && isCancelledOrAuditFailed(orderStatus)) return false

  const itemOk = Number.isFinite(itemStatus) && itemStatus >= PO_STATUS_VENDOR_CONFIRMED
  const orderOk = Number.isFinite(orderStatus) && orderStatus >= PO_STATUS_VENDOR_CONFIRMED
  return itemOk || orderOk
}

const PO_MAIN_STATUS_I18N_KEY: Record<number, string> = {
  0: 'purchaseOrderList.status.draft',
  1: 'purchaseOrderList.status.new',
  2: 'purchaseOrderList.status.pendingReview',
  10: 'purchaseOrderList.status.approved',
  20: 'purchaseOrderList.status.pendingConfirm',
  30: 'purchaseOrderList.status.confirmed',
  50: 'purchaseOrderList.status.inProgress',
  100: 'purchaseOrderList.status.completed',
  [-1]: 'purchaseOrderList.status.reviewFailed',
  [-2]: 'purchaseOrderList.status.cancelled'
}

/** 采购单主状态文案（i18n）。 */
export function purchaseOrderMainStatusLabel(
  t: (key: string) => string,
  status: unknown
): string {
  const s = Number(status)
  if (!Number.isFinite(s)) return t('purchaseOrderList.status.unknown')
  const key = PO_MAIN_STATUS_I18N_KEY[s]
  return key ? t(key) : t('purchaseOrderList.status.unknown')
}

/** 审核通过(10)、待确认(20)：等待供应商确认。 */
export function purchaseOrderMainStatusAwaitingVendorConfirm(status: unknown): boolean {
  const s = Number(status)
  return s === PO_STATUS_AUDIT_PASSED || s === PO_STATUS_PENDING_CONFIRM
}
