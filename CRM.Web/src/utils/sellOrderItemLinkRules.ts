/** 销售订单明细与报价关联规则（与 CRM.Core.Utilities.SellOrderItemLinkRules 对齐） */

export const SO_TYPE_CUSTOMER = 1
export const SO_TYPE_STOCKING = 2
export const SO_TYPE_SAMPLE = 3

export const EMPTY_QUOTE_ID = '00000000-0000-0000-0000-000000000000'

export type SoLineQuoteLinkDraft = { quoteId?: string }

export function isLinkedQuoteId(id?: string): boolean {
  const t = id?.trim()
  if (!t) return false
  return t.toLowerCase() !== EMPTY_QUOTE_ID.toLowerCase()
}

export function shouldAllowManualAddSoItem(headerType: number): boolean {
  return headerType !== SO_TYPE_CUSTOMER
}

export type SoCustomerOrderValidateErrorCode =
  | 'customerOrderMinOneItem'
  | 'customerOrderLineQuoteRequired'

export function validateCustomerOrderItemsForSave(
  headerType: number,
  items: SoLineQuoteLinkDraft[]
): SoCustomerOrderValidateErrorCode | null {
  if (headerType !== SO_TYPE_CUSTOMER) return null
  if (!items.length) return 'customerOrderMinOneItem'
  for (const item of items) {
    if (!isLinkedQuoteId(item.quoteId)) return 'customerOrderLineQuoteRequired'
  }
  return null
}

export function messageKeyForSoCustomerOrderValidateError(code: SoCustomerOrderValidateErrorCode): string {
  return `salesOrderCreate.validate.${code}`
}
