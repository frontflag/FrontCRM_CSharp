import { describe, expect, it } from 'vitest'
import {
  isLinkedQuoteId,
  shouldAllowManualAddSoItem,
  validateCustomerOrderItemsForSave,
  SO_TYPE_CUSTOMER,
  SO_TYPE_STOCKING,
  EMPTY_QUOTE_ID
} from '@/utils/sellOrderItemLinkRules'

describe('sellOrderItemLinkRules', () => {
  it('isLinkedQuoteId rejects empty and sentinel', () => {
    expect(isLinkedQuoteId(undefined)).toBe(false)
    expect(isLinkedQuoteId(EMPTY_QUOTE_ID)).toBe(false)
    expect(isLinkedQuoteId('quote-1')).toBe(true)
  })

  it('shouldAllowManualAddSoItem: customer disallows', () => {
    expect(shouldAllowManualAddSoItem(SO_TYPE_CUSTOMER)).toBe(false)
    expect(shouldAllowManualAddSoItem(SO_TYPE_STOCKING)).toBe(true)
  })

  it('validateCustomerOrderItemsForSave: mixed lines fail', () => {
    expect(
      validateCustomerOrderItemsForSave(SO_TYPE_CUSTOMER, [
        { quoteId: 'q-1' },
        { quoteId: undefined }
      ])
    ).toBe('customerOrderLineQuoteRequired')
  })

  it('validateCustomerOrderItemsForSave: stocking passes without quote', () => {
    expect(validateCustomerOrderItemsForSave(SO_TYPE_STOCKING, [{ quoteId: undefined }])).toBeNull()
  })

  it('validateCustomerOrderItemsForSave: customer all linked passes', () => {
    expect(
      validateCustomerOrderItemsForSave(SO_TYPE_CUSTOMER, [{ quoteId: 'q-1' }, { quoteId: 'q-2' }])
    ).toBeNull()
  })
})
