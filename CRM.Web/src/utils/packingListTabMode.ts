/** 装箱单列表：筛选页签模式偏好（localStorage，单维度）。 */

import {
  PACKING_STATUS_FILTER_VALUES,
  PACKING_MATERIAL_TYPE_FILTER_VALUES,
  PACKING_STOCK_OUT_TYPE_FILTER_VALUES
} from '@/api/packing'

const TAB_MODE_KEY = 'crm.packing-list.tab-mode'

export type PackingListTabModeDimension = 'off' | 'status' | 'stockOutType' | 'materialType'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const PACKING_LIST_TAB_MODE_OPTIONS: Exclude<PackingListTabModeDimension, 'off'>[] = [
  'status',
  'stockOutType',
  'materialType'
]

const TAB_MODE_SET = new Set<string>(['off', ...PACKING_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is PackingListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readPackingListTabMode(): PackingListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writePackingListTabMode(dimension: PackingListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与状态下拉一致：新建～出库完成（无已取消） */
export const PACKING_STATUS_TAB_VALUES = PACKING_STATUS_FILTER_VALUES

export type PackingStatusTabId = 'all' | '10' | '20' | '30' | '40' | '50' | '100'

export function packingStatusFilterToTab(value: number | undefined | null): PackingStatusTabId {
  if (
    value === 10 ||
    value === 20 ||
    value === 30 ||
    value === 40 ||
    value === 50 ||
    value === 100
  ) {
    return String(value) as PackingStatusTabId
  }
  return 'all'
}

export function packingStatusTabToFilter(tab: PackingStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20 || n === 30 || n === 40 || n === 50 || n === 100) return n
  return undefined
}

export type PackingStockOutTypeTabId = 'all' | '10' | '20' | '30' | '40'

export const PACKING_STOCK_OUT_TYPE_TAB_VALUES = PACKING_STOCK_OUT_TYPE_FILTER_VALUES

export function packingStockOutTypeFilterToTab(value: number | undefined | null): PackingStockOutTypeTabId {
  if (value === 10 || value === 20 || value === 30 || value === 40) {
    return String(value) as PackingStockOutTypeTabId
  }
  return 'all'
}

export function packingStockOutTypeTabToFilter(tab: PackingStockOutTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20 || n === 30 || n === 40) return n
  return undefined
}

export type PackingMaterialTypeTabId = 'all' | '10' | '20' | '30'

export const PACKING_MATERIAL_TYPE_TAB_VALUES = PACKING_MATERIAL_TYPE_FILTER_VALUES

export function packingMaterialTypeFilterToTab(value: number | undefined | null): PackingMaterialTypeTabId {
  if (value === 10 || value === 20 || value === 30) {
    return String(value) as PackingMaterialTypeTabId
  }
  return 'all'
}

export function packingMaterialTypeTabToFilter(tab: PackingMaterialTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20 || n === 30) return n
  return undefined
}
