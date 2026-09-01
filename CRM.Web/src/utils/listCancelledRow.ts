/** 销售/采购主单「取消」状态码，与后端 purchaseorder / sellorder.status 一致。 */
export const CANCELLED_ORDER_HEADER_STATUS = -2

export const LIST_ROW_CANCELLED_CLASS = 'list-row--cancelled'

export function isCancelledOrderHeaderStatus(status: unknown): boolean {
  const n = Number(status)
  return Number.isFinite(n) && n === CANCELLED_ORDER_HEADER_STATUS
}

/** 主单列表用 `status`；明细列表用 `orderStatus`（只看主单取消，不看明细状态）。 */
export function cancelledOrderListRowClass(
  row: Record<string, unknown> | null | undefined,
  field: 'status' | 'orderStatus' = 'status'
): string {
  if (!row) return ''
  const raw = row[field] ?? (field === 'status' ? row.Status : row.OrderStatus)
  return isCancelledOrderHeaderStatus(raw) ? LIST_ROW_CANCELLED_CLASS : ''
}

export function joinRowClassNames(...parts: Array<string | false | null | undefined>): string {
  return parts.filter((p): p is string => typeof p === 'string' && p.trim().length > 0).join(' ')
}
