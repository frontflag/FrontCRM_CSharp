/** 报关单列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.customs-declaration-list.tab-mode'

export type CustomsDeclarationListTabModeDimension =
  | 'off'
  | 'internalStatus'
  | 'declarationType'
  | 'customsClearanceStatus'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const CUSTOMS_DECLARATION_LIST_TAB_MODE_OPTIONS: Exclude<
  CustomsDeclarationListTabModeDimension,
  'off'
>[] = ['internalStatus', 'declarationType', 'customsClearanceStatus']

const TAB_MODE_SET = new Set<string>(['off', ...CUSTOMS_DECLARATION_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is CustomsDeclarationListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readCustomsDeclarationListTabMode(): CustomsDeclarationListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeCustomsDeclarationListTabMode(
  dimension: CustomsDeclarationListTabModeDimension
): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与内部状态下拉一致：待处理～已完成 + 作废 */
export const CDL_INTERNAL_STATUS_TAB_VALUES = [1, 2, 3, -1] as const

export type CdlInternalStatusTabId = 'all' | '1' | '2' | '3' | '-1'

export function cdlInternalStatusFilterToTab(
  value: number | undefined | null
): CdlInternalStatusTabId {
  if (value === 1 || value === 2 || value === 3 || value === -1) {
    return String(value) as CdlInternalStatusTabId
  }
  return 'all'
}

export function cdlInternalStatusTabToFilter(tab: CdlInternalStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  if (tab === '-1') return -1
  const n = Number(tab)
  if (n === 1 || n === 2 || n === 3) return n
  return undefined
}

export const CDL_DECLARATION_TYPE_TAB_VALUES = [1, 2] as const

export type CdlDeclarationTypeTabId = 'all' | '1' | '2'

export function cdlDeclarationTypeFilterToTab(
  value: number | undefined | null
): CdlDeclarationTypeTabId {
  if (value === 1 || value === 2) return String(value) as CdlDeclarationTypeTabId
  return 'all'
}

export function cdlDeclarationTypeTabToFilter(tab: CdlDeclarationTypeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 1 || n === 2) return n
  return undefined
}

/** 与结关状态下拉一致：未维护 / 放行 / 已结关 */
export const CDL_CLEARANCE_STATUS_TAB_VALUES = [0, 10, 100] as const

export type CdlClearanceStatusTabId = 'all' | '0' | '10' | '100'

export function cdlClearanceStatusFilterToTab(
  value: number | undefined | null
): CdlClearanceStatusTabId {
  if (value === 0 || value === 10 || value === 100) {
    return String(value) as CdlClearanceStatusTabId
  }
  return 'all'
}

export function cdlClearanceStatusTabToFilter(tab: CdlClearanceStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 0 || n === 10 || n === 100) return n
  return undefined
}
