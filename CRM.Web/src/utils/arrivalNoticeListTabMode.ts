/** 到货通知列表：筛选页签模式偏好（localStorage，单维度）。 */

import { STOCK_IN_TYPE_FILTER_VALUES } from '@/constants/stockInType'

const TAB_MODE_KEY = 'crm.arrival-notice-list.tab-mode'

export type ArrivalNoticeListTabModeDimension = 'off' | 'status' | 'stockInType'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const ARRIVAL_NOTICE_LIST_TAB_MODE_OPTIONS: Exclude<ArrivalNoticeListTabModeDimension, 'off'>[] = [
  'status',
  'stockInType'
]

const TAB_MODE_SET = new Set<string>(['off', ...ARRIVAL_NOTICE_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is ArrivalNoticeListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readArrivalNoticeListTabMode(): ArrivalNoticeListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeArrivalNoticeListTabMode(dimension: ArrivalNoticeListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与状态下拉一致：新建～已入库 */
export const ARRIVAL_NOTICE_STATUS_TAB_VALUES = [1, 10, 20, 30, 100] as const

export type ArrivalNoticeStatusTabId = 'all' | '1' | '10' | '20' | '30' | '100'

export function arrivalNoticeStatusFilterToTab(value: number | undefined | null): ArrivalNoticeStatusTabId {
  if (value === 1 || value === 10 || value === 20 || value === 30 || value === 100) {
    return String(value) as ArrivalNoticeStatusTabId
  }
  return 'all'
}

export function arrivalNoticeStatusTabToFilter(tab: ArrivalNoticeStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 1 || n === 10 || n === 20 || n === 30 || n === 100) return n
  return undefined
}

export type ArrivalNoticeStockInTypeTabId = 'all' | '10' | '20' | '30' | '40'

export const ARRIVAL_NOTICE_STOCK_IN_TYPE_TAB_VALUES = STOCK_IN_TYPE_FILTER_VALUES

export function arrivalNoticeStockInTypeFilterToTab(
  value: number | undefined | null
): ArrivalNoticeStockInTypeTabId {
  if (value === 10 || value === 20 || value === 30 || value === 40) {
    return String(value) as ArrivalNoticeStockInTypeTabId
  }
  return 'all'
}

export function arrivalNoticeStockInTypeTabToFilter(
  tab: ArrivalNoticeStockInTypeTabId
): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20 || n === 30 || n === 40) return n
  return undefined
}
