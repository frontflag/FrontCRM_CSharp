/** 库存中心列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.inventory-list.tab-mode'

/** 仓库选项超过此数量时，仓库维度强制回退下拉（不展示页签条） */
export const INVENTORY_WAREHOUSE_TAB_MAX = 10

export type InventoryListTabModeDimension = 'off' | 'stockType' | 'warehouse'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const INVENTORY_LIST_TAB_MODE_OPTIONS: Exclude<InventoryListTabModeDimension, 'off'>[] = [
  'stockType',
  'warehouse'
]

const TAB_MODE_SET = new Set<string>(['off', ...INVENTORY_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is InventoryListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readInventoryListTabMode(): InventoryListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeInventoryListTabMode(dimension: InventoryListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与库存类型下拉一致：客单 / 备货 / 样品 */
export const INV_STOCK_TYPE_TAB_VALUES = [1, 2, 3] as const

export type InvStockTypeTabId = 'all' | '1' | '2' | '3'

export function invStockTypeFilterToTab(value: number | undefined | null): InvStockTypeTabId {
  if (value === 1 || value === 2 || value === 3) return String(value) as InvStockTypeTabId
  return 'all'
}

export function invStockTypeTabToFilter(tab: InvStockTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 1 || n === 2 || n === 3) return n
  return undefined
}

export type InvWarehouseTabId = 'all' | string

export function invWarehouseFilterToTab(value: string | undefined | null): InvWarehouseTabId {
  const v = String(value ?? '').trim()
  return v ? v : 'all'
}

export function invWarehouseTabToFilter(tab: InvWarehouseTabId): string | undefined {
  if (tab === 'all') return undefined
  const v = String(tab).trim()
  return v || undefined
}

export function isWarehouseTabModeAllowed(warehouseOptionCount: number): boolean {
  return warehouseOptionCount > 0 && warehouseOptionCount <= INVENTORY_WAREHOUSE_TAB_MAX
}
