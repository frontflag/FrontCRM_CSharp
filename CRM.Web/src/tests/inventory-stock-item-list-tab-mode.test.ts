import { describe, expect, it } from 'vitest'
import {
  INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS,
  isiStockInTypeFilterToTab,
  isiStockInTypeTabToFilter
} from '@/utils/inventoryStockItemListTabMode'

describe('inventoryStockItemListTabMode', () => {
  it('includes stock-in type as a tab-mode option', () => {
    expect(INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS).toEqual([
      'outboundStatus',
      'stockPresence',
      'warehouse',
      'stockInType'
    ])
  })

  it('maps stock-in type filter and tabs, including legacy purchase 1', () => {
    expect(isiStockInTypeFilterToTab(20)).toBe('20')
    expect(isiStockInTypeFilterToTab(1)).toBe('10')
    expect(isiStockInTypeFilterToTab(undefined)).toBe('all')
    expect(isiStockInTypeTabToFilter('all')).toBeUndefined()
    expect(isiStockInTypeTabToFilter('20')).toBe(20)
    expect(isiStockInTypeTabToFilter('10')).toBe(10)
  })
})
