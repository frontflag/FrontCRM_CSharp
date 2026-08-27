/** 出库明细列表：筛选页签模式偏好（localStorage，单维度）。 */

import type { StockOutListTabModeDimension } from '@/utils/stockOutListTabMode'

export type StockOutItemListTabModeDimension = StockOutListTabModeDimension

export const STOCK_OUT_ITEM_LIST_TAB_MODE_OPTIONS: Exclude<StockOutItemListTabModeDimension, 'off'>[] = [
  'status',
  'stockOutType'
]

/** 状态下拉/页签顺序：已取消置底 */
export const STOCK_OUT_ITEM_LIST_STATUS_TAB_VALUES = [0, 1, 2, 4, 3] as const

const TAB_MODE_KEY = 'crm.stock-out-item-list.tab-mode'
const TAB_MODE_SET = new Set<string>(['off', ...STOCK_OUT_ITEM_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is StockOutItemListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readStockOutItemListTabMode(): StockOutItemListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeStockOutItemListTabMode(dimension: StockOutItemListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}
