import { describe, expect, it } from 'vitest'
import {
  INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS,
  isiStockInTypeFilterToTab,
  isiStockInTypeTabToFilter,
  isiStockTypeFilterToTab,
  isiStockTypeTabToFilter
} from '@/utils/inventoryStockItemListTabMode'

describe('inventoryStockItemListTabMode', () => {
  it('includes stock type and stock-in type as tab-mode options', () => {
    expect(INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS).toEqual([
      'outboundStatus',
      'stockPresence',
      'stockType',
      'warehouse',
      'stockInType'
    ])
  })

  it('maps stock type filter and tabs', () => {
    expect(isiStockTypeFilterToTab(2)).toBe('2')
    expect(isiStockTypeFilterToTab(1)).toBe('1')
    expect(isiStockTypeFilterToTab(undefined)).toBe('all')
    expect(isiStockTypeTabToFilter('all')).toBeUndefined()
    expect(isiStockTypeTabToFilter('2')).toBe(2)
    expect(isiStockTypeTabToFilter('3')).toBe(3)
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
