/** 出库单列表：筛选页签模式偏好（localStorage，单维度）。 */

import { STOCK_OUT_TYPE_FILTER_VALUES } from '@/constants/stockOutType'

const TAB_MODE_KEY = 'crm.stock-out-list.tab-mode'

export type StockOutListTabModeDimension = 'off' | 'status' | 'stockOutType'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const STOCK_OUT_LIST_TAB_MODE_OPTIONS: Exclude<StockOutListTabModeDimension, 'off'>[] = [
  'status',
  'stockOutType'
]

const TAB_MODE_SET = new Set<string>(['off', ...STOCK_OUT_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is StockOutListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readStockOutListTabMode(): StockOutListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeStockOutListTabMode(dimension: StockOutListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与状态下拉一致：草稿～出库完成（含已取消） */
export const STOCK_OUT_STATUS_TAB_VALUES = [0, 1, 2, 3, 4] as const

export type StockOutStatusTabId = 'all' | '0' | '1' | '2' | '3' | '4'

export function stockOutStatusFilterToTab(value: number | undefined | null): StockOutStatusTabId {
  if (value === 0 || value === 1 || value === 2 || value === 3 || value === 4) {
    return String(value) as StockOutStatusTabId
  }
  return 'all'
}

export function stockOutStatusTabToFilter(tab: StockOutStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 0 || n === 1 || n === 2 || n === 3 || n === 4) return n
  return undefined
}

export type StockOutTypeTabId = 'all' | '10' | '20' | '30' | '40'

export const STOCK_OUT_TYPE_TAB_VALUES = STOCK_OUT_TYPE_FILTER_VALUES

export function stockOutTypeFilterToTab(value: number | undefined | null): StockOutTypeTabId {
  if (value === 10 || value === 20 || value === 30 || value === 40) {
    return String(value) as StockOutTypeTabId
  }
  return 'all'
}

export function stockOutTypeTabToFilter(tab: StockOutTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20 || n === 30 || n === 40) return n
  return undefined
}
