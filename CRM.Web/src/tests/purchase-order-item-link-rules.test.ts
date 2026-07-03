import { describe, expect, it } from 'vitest'
import {
  isLinkedSellOrderItemId,
  resolvePoHeaderType,
  validateCustomerOrderItemsForSave,
  PO_TYPE_CUSTOMER,
  PO_TYPE_STOCKING,
  PO_TYPE_SAMPLE,
  EMPTY_SELL_ORDER_ITEM_ID
} from '@/utils/purchaseOrderItemLinkRules'

describe('purchaseOrderItemLinkRules', () => {
  it('isLinkedSellOrderItemId rejects empty and sentinel', () => {
    expect(isLinkedSellOrderItemId(undefined)).toBe(false)
    expect(isLinkedSellOrderItemId(EMPTY_SELL_ORDER_ITEM_ID)).toBe(false)
    expect(isLinkedSellOrderItemId('so-line-1')).toBe(true)
  })

  it('resolvePoHeaderType: all manual → stocking', () => {
    expect(resolvePoHeaderType(PO_TYPE_CUSTOMER, [{ sellOrderItemId: undefined }])).toBe(PO_TYPE_STOCKING)
  })

  it('resolvePoHeaderType: any linked → customer', () => {
    expect(
      resolvePoHeaderType(PO_TYPE_STOCKING, [{ sellOrderItemId: 'a' }, { sellOrderItemId: undefined }])
    ).toBe(PO_TYPE_CUSTOMER)
  })

  it('resolvePoHeaderType: sample without sell link', () => {
    expect(resolvePoHeaderType(PO_TYPE_SAMPLE, [])).toBe(PO_TYPE_SAMPLE)
  })

  it('validateCustomerOrderItemsForSave: mixed lines fail', () => {
    expect(
      validateCustomerOrderItemsForSave(PO_TYPE_CUSTOMER, [
        { sellOrderItemId: 'a' },
        { sellOrderItemId: undefined }
      ])
    ).toBe('customerOrderLineSellItemRequired')
  })

  it('validateCustomerOrderItemsForSave: stocking passes without sell id', () => {
    expect(validateCustomerOrderItemsForSave(PO_TYPE_STOCKING, [{ sellOrderItemId: undefined }])).toBeNull()
  })
})
