import { describe, expect, it } from 'vitest'
import {
  STOCK_IN_LIST_TAB_MODE_OPTIONS,
  stockInTypeFilterToTab,
  stockInTypeTabToFilter,
  stockInWarehouseFilterToTab,
  stockInWarehouseTabToFilter
} from '@/utils/stockInListTabMode'

describe('stockInListTabMode', () => {
  it('exposes warehouse then stock-in type as tab-mode options', () => {
    expect(STOCK_IN_LIST_TAB_MODE_OPTIONS).toEqual(['warehouse', 'stockInType'])
  })

  it('maps stock-in type filter and tabs, including legacy purchase 1', () => {
    expect(stockInTypeFilterToTab(20)).toBe('20')
    expect(stockInTypeFilterToTab(1)).toBe('10')
    expect(stockInTypeFilterToTab(undefined)).toBe('all')
    expect(stockInTypeTabToFilter('all')).toBeUndefined()
    expect(stockInTypeTabToFilter('20')).toBe(20)
    expect(stockInTypeTabToFilter('10')).toBe(10)
  })

  it('maps warehouse filter and tabs', () => {
    expect(stockInWarehouseFilterToTab('')).toBe('all')
    expect(stockInWarehouseFilterToTab('  wh-1  ')).toBe('wh-1')
    expect(stockInWarehouseTabToFilter('all')).toBe('')
    expect(stockInWarehouseTabToFilter('wh-1')).toBe('wh-1')
  })
})
