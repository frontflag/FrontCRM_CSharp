/** 采购订单明细与销售订单明细关联规则（与 CRM.Core.Utilities.PurchaseOrderItemLinkRules 对齐） */

export const PO_TYPE_CUSTOMER = 1
export const PO_TYPE_STOCKING = 2
export const PO_TYPE_SAMPLE = 3

export const EMPTY_SELL_ORDER_ITEM_ID = '00000000-0000-0000-0000-000000000000'

export type PoLineSellLinkDraft = { sellOrderItemId?: string }

export function isLinkedSellOrderItemId(id?: string): boolean {
  const t = id?.trim()
  if (!t) return false
  return t.toLowerCase() !== EMPTY_SELL_ORDER_ITEM_ID.toLowerCase()
}

/** 有销售明细关联 → 客单(1)；否则备货(2)；无销售关联且请求为样品 → 3 */
export function resolvePoHeaderType(requestedType: number, items: PoLineSellLinkDraft[]): number {
  if (items.some((i) => isLinkedSellOrderItemId(i.sellOrderItemId))) return PO_TYPE_CUSTOMER
  if (requestedType === PO_TYPE_SAMPLE) return PO_TYPE_SAMPLE
  return PO_TYPE_STOCKING
}

export type PoCustomerOrderValidateErrorCode =
  | 'customerOrderMinOneItem'
  | 'customerOrderLineSellItemRequired'

export function validateCustomerOrderItemsForSave(
  requestedType: number,
  items: PoLineSellLinkDraft[]
): PoCustomerOrderValidateErrorCode | null {
  const headerType = resolvePoHeaderType(requestedType, items)
  if (headerType !== PO_TYPE_CUSTOMER) return null
  if (!items.length) return 'customerOrderMinOneItem'
  for (const item of items) {
    if (!isLinkedSellOrderItemId(item.sellOrderItemId)) return 'customerOrderLineSellItemRequired'
  }
  return null
}

export function messageKeyForPoCustomerOrderValidateError(code: PoCustomerOrderValidateErrorCode): string {
  return `purchaseOrderCreate.validate.${code}`
}
