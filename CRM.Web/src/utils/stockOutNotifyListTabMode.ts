/** 出库通知列表：筛选页签模式偏好（localStorage，单维度）。 */

import {
  STOCK_OUT_REQUEST_STATUS,
  type StockOutRequestStatusValue
} from '@/constants/stockOutRequestStatus'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { STOCK_OUT_TYPE_FILTER_VALUES } from '@/constants/stockOutType'

const TAB_MODE_KEY = 'crm.stock-out-notify-list.tab-mode'

export type SonListTabModeDimension = 'off' | 'status' | 'regionType' | 'stockOutType'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const SON_LIST_TAB_MODE_OPTIONS: Exclude<SonListTabModeDimension, 'off'>[] = [
  'status',
  'regionType',
  'stockOutType'
]

const TAB_MODE_SET = new Set<string>(['off', ...SON_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is SonListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readSonListTabMode(): SonListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeSonListTabMode(dimension: SonListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与状态下拉一致（含已取消） */
export const SON_STATUS_TAB_VALUES: readonly StockOutRequestStatusValue[] = [
  STOCK_OUT_REQUEST_STATUS.PendingCustoms,
  STOCK_OUT_REQUEST_STATUS.PendingPacking,
  STOCK_OUT_REQUEST_STATUS.Packed,
  STOCK_OUT_REQUEST_STATUS.StockedOut,
  STOCK_OUT_REQUEST_STATUS.Cancelled
]

export type SonStatusTabId = 'all' | '5' | '10' | '20' | '100' | '-1'

export function statusFilterToTab(value: number | undefined | null): SonStatusTabId {
  if (
    value === STOCK_OUT_REQUEST_STATUS.PendingCustoms ||
    value === STOCK_OUT_REQUEST_STATUS.PendingPacking ||
    value === STOCK_OUT_REQUEST_STATUS.Packed ||
    value === STOCK_OUT_REQUEST_STATUS.StockedOut ||
    value === STOCK_OUT_REQUEST_STATUS.Cancelled
  ) {
    return String(value) as SonStatusTabId
  }
  return 'all'
}

export function statusTabToFilter(tab: SonStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (
    n === STOCK_OUT_REQUEST_STATUS.PendingCustoms ||
    n === STOCK_OUT_REQUEST_STATUS.PendingPacking ||
    n === STOCK_OUT_REQUEST_STATUS.Packed ||
    n === STOCK_OUT_REQUEST_STATUS.StockedOut ||
    n === STOCK_OUT_REQUEST_STATUS.Cancelled
  ) {
    return n
  }
  return undefined
}

export type SonRegionTabId = 'all' | '10' | '20'

export function regionFilterToTab(value: number | undefined | null): SonRegionTabId {
  if (value === REGION_TYPE_DOMESTIC || value === REGION_TYPE_OVERSEAS) {
    return String(value) as SonRegionTabId
  }
  return 'all'
}

export function regionTabToFilter(tab: SonRegionTabId): number | undefined {
  if (tab === '10') return REGION_TYPE_DOMESTIC
  if (tab === '20') return REGION_TYPE_OVERSEAS
  return undefined
}

export type SonStockOutTypeTabId = 'all' | '10' | '20' | '30' | '40'

export const SON_STOCK_OUT_TYPE_TAB_VALUES = STOCK_OUT_TYPE_FILTER_VALUES

export function stockOutTypeFilterToTab(value: number | undefined | null): SonStockOutTypeTabId {
  if (
    value === 10 ||
    value === 20 ||
    value === 30 ||
    value === 40
  ) {
    return String(value) as SonStockOutTypeTabId
  }
  return 'all'
}

export function stockOutTypeTabToFilter(tab: SonStockOutTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20 || n === 30 || n === 40) return n
  return undefined
}
