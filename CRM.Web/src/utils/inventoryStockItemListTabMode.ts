/** 库存明细列表：筛选页签模式偏好（localStorage，单维度）。 */

import { STOCK_IN_TYPE_FILTER_VALUES, parseStockInTypeFilterValue } from '@/constants/stockInType'
import {
  INVENTORY_WAREHOUSE_TAB_MAX,
  INV_STOCK_TYPE_TAB_VALUES,
  invStockTypeFilterToTab,
  invStockTypeTabToFilter,
  isWarehouseTabModeAllowed,
  type InvStockTypeTabId
} from '@/utils/inventoryListTabMode'

export { INVENTORY_WAREHOUSE_TAB_MAX, isWarehouseTabModeAllowed }

export const ISI_STOCK_TYPE_TAB_VALUES = INV_STOCK_TYPE_TAB_VALUES
export const isiStockTypeFilterToTab = invStockTypeFilterToTab
export const isiStockTypeTabToFilter = invStockTypeTabToFilter
export type IsiStockTypeTabId = InvStockTypeTabId

const TAB_MODE_KEY = 'crm.inventory-stock-item-list.tab-mode'

export type InventoryStockItemListTabModeDimension =
  | 'off'
  | 'outboundStatus'
  | 'stockPresence'
  | 'stockType'
  | 'warehouse'
  | 'stockInType'

/** 可在「页签模式」子菜单中选择的维度（不含 off；不含业务员/采购员） */
export const INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS: Exclude<
  InventoryStockItemListTabModeDimension,
  'off'
>[] = ['outboundStatus', 'stockPresence', 'stockType', 'warehouse', 'stockInType']

const TAB_MODE_SET = new Set<string>(['off', ...INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is InventoryStockItemListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readInventoryStockItemListTabMode(): InventoryStockItemListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeInventoryStockItemListTabMode(
  dimension: InventoryStockItemListTabModeDimension
): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

export const ISI_OUTBOUND_STATUS_TAB_VALUES = [1, 2, 3] as const

export type IsiOutboundStatusTabId = 'all' | '1' | '2' | '3'

export function isiOutboundStatusFilterToTab(
  value: number | undefined | null
): IsiOutboundStatusTabId {
  if (value === 1 || value === 2 || value === 3) return String(value) as IsiOutboundStatusTabId
  return 'all'
}

export function isiOutboundStatusTabToFilter(tab: IsiOutboundStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 1 || n === 2 || n === 3) return n
  return undefined
}

/** 页签首项「全部」对应下拉空串（不限制）；有/无库存为 has/none */
export type IsiStockPresenceTabId = 'all' | 'has' | 'none'

export function isiStockPresenceFilterToTab(value: string | undefined | null): IsiStockPresenceTabId {
  if (value === 'has' || value === 'none') return value
  return 'all'
}

export function isiStockPresenceTabToFilter(tab: IsiStockPresenceTabId): '' | 'has' | 'none' {
  if (tab === 'has' || tab === 'none') return tab
  return ''
}

export type IsiWarehouseTabId = 'all' | string

export function isiWarehouseFilterToTab(value: string | undefined | null): IsiWarehouseTabId {
  const v = String(value ?? '').trim()
  return v ? v : 'all'
}

export function isiWarehouseTabToFilter(tab: IsiWarehouseTabId): string {
  if (tab === 'all') return ''
  return String(tab).trim()
}

export type IsiStockInTypeTabId = 'all' | '10' | '20' | '30' | '40'

export const ISI_STOCK_IN_TYPE_TAB_VALUES = STOCK_IN_TYPE_FILTER_VALUES

export function isiStockInTypeFilterToTab(value: number | undefined | null): IsiStockInTypeTabId {
  const parsed = parseStockInTypeFilterValue(value)
  if (parsed != null) return String(parsed) as IsiStockInTypeTabId
  return 'all'
}

export function isiStockInTypeTabToFilter(tab: IsiStockInTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  return parseStockInTypeFilterValue(tab)
}
