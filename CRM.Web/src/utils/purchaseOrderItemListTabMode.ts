/** 采购订单明细列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.po-item-list.tab-mode'

export type PoItemTabModeDimension =
  | 'off'
  | 'currency'
  | 'orderType'
  | 'payment'
  | 'purchase'
  | 'stockIn'
  | 'invoice'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const PO_ITEM_TAB_MODE_OPTIONS: Exclude<PoItemTabModeDimension, 'off'>[] = [
  'currency',
  'orderType',
  'payment',
  'purchase',
  'stockIn',
  'invoice'
]

const TAB_MODE_SET = new Set<string>(['off', ...PO_ITEM_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is PoItemTabModeDimension {
  return TAB_MODE_SET.has(value)
}

/** 与左栏 preset 互斥的进度类维度 */
export function isPoProgressTabDimension(
  dim: PoItemTabModeDimension
): dim is Exclude<PoItemTabModeDimension, 'off' | 'currency' | 'orderType'> {
  return dim === 'payment' || dim === 'purchase' || dim === 'stockIn' || dim === 'invoice'
}

export function readPoItemTabMode(): PoItemTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writePoItemTabMode(dimension: PoItemTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

export type PoItemCurrencyTabId = 'all' | 'rmb' | 'foreign'

export function currencyFilterToTab(value: '' | 'rmb' | 'foreign'): PoItemCurrencyTabId {
  if (value === 'rmb' || value === 'foreign') return value
  return 'all'
}

export function currencyTabToFilter(tab: PoItemCurrencyTabId): '' | 'rmb' | 'foreign' {
  if (tab === 'rmb' || tab === 'foreign') return tab
  return ''
}

export type PoItemOrderTypeTabId = 'all' | '1' | '2' | '3'

export function orderTypeFilterToTab(value: number | undefined | null): PoItemOrderTypeTabId {
  if (value === 1 || value === 2 || value === 3) return String(value) as PoItemOrderTypeTabId
  return 'all'
}

export function orderTypeTabToFilter(tab: PoItemOrderTypeTabId): number | undefined {
  if (tab === '1' || tab === '2' || tab === '3') return Number(tab)
  return undefined
}

export type PoItemProgressTabId = 'all' | '0' | '1' | '2'

export function progressFilterToTab(value: number | undefined | null): PoItemProgressTabId {
  if (value === 0 || value === 1 || value === 2) return String(value) as PoItemProgressTabId
  return 'all'
}

export function progressTabToFilter(tab: PoItemProgressTabId): number | undefined {
  if (tab === '0' || tab === '1' || tab === '2') return Number(tab)
  return undefined
}

export type PoItemProgressFilterKey =
  | 'paymentProgressStatus'
  | 'purchaseProgressStatus'
  | 'stockInProgressStatus'
  | 'invoiceProgressStatus'

const PROGRESS_DIM_TO_FILTER: Record<
  Exclude<PoItemTabModeDimension, 'off' | 'currency' | 'orderType'>,
  PoItemProgressFilterKey
> = {
  payment: 'paymentProgressStatus',
  purchase: 'purchaseProgressStatus',
  stockIn: 'stockInProgressStatus',
  invoice: 'invoiceProgressStatus'
}

export function progressDimensionToFilterKey(
  dim: Exclude<PoItemTabModeDimension, 'off' | 'currency' | 'orderType'>
): PoItemProgressFilterKey {
  return PROGRESS_DIM_TO_FILTER[dim]
}
