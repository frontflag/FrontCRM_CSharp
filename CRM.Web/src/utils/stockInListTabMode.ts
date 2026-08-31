/** 入库单列表：筛选页签模式偏好（localStorage，单维度）。 */

import { STOCK_IN_TYPE_FILTER_VALUES, parseStockInTypeFilterValue } from '@/constants/stockInType'
import {
  INVENTORY_WAREHOUSE_TAB_MAX,
  isWarehouseTabModeAllowed
} from '@/utils/inventoryListTabMode'

export { INVENTORY_WAREHOUSE_TAB_MAX, isWarehouseTabModeAllowed }

const TAB_MODE_KEY = 'crm.stock-in-list.tab-mode'

export type StockInListTabModeDimension = 'off' | 'warehouse' | 'stockInType'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const STOCK_IN_LIST_TAB_MODE_OPTIONS: Exclude<StockInListTabModeDimension, 'off'>[] = [
  'warehouse',
  'stockInType'
]

const TAB_MODE_SET = new Set<string>(['off', ...STOCK_IN_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is StockInListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readStockInListTabMode(): StockInListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeStockInListTabMode(dimension: StockInListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

export type StockInTypeTabId = 'all' | '10' | '20' | '30' | '40'

export const STOCK_IN_TYPE_TAB_VALUES = STOCK_IN_TYPE_FILTER_VALUES

export function stockInTypeFilterToTab(value: number | undefined | null): StockInTypeTabId {
  const parsed = parseStockInTypeFilterValue(value)
  if (parsed != null) return String(parsed) as StockInTypeTabId
  return 'all'
}

export function stockInTypeTabToFilter(tab: StockInTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  return parseStockInTypeFilterValue(tab)
}

export type StockInWarehouseTabId = 'all' | string

export function stockInWarehouseFilterToTab(value: string | undefined | null): StockInWarehouseTabId {
  const v = String(value ?? '').trim()
  return v ? v : 'all'
}

export function stockInWarehouseTabToFilter(tab: StockInWarehouseTabId): string {
  if (tab === 'all') return ''
  return String(tab).trim()
}
