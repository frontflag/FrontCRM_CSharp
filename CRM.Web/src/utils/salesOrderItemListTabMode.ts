/** 销售订单明细列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.so-item-list.tab-mode'
/** @deprecated 迁移后由 TAB_MODE_KEY 取代 */
const LEGACY_CURRENCY_TAB_MODE_KEY = 'crm.so-item-list.tab-mode.currency'

export type SoItemTabModeDimension =
  | 'off'
  | 'currency'
  | 'purchase'
  | 'stockIn'
  | 'stockOutNotify'
  | 'stockOut'
  | 'receipt'
  | 'invoice'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const SO_ITEM_TAB_MODE_OPTIONS: Exclude<SoItemTabModeDimension, 'off'>[] = [
  'currency',
  'purchase',
  'stockIn',
  'stockOutNotify',
  'stockOut',
  'receipt',
  'invoice'
]

const TAB_MODE_SET = new Set<string>(['off', ...SO_ITEM_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is SoItemTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function isProgressTabDimension(
  dim: SoItemTabModeDimension
): dim is Exclude<SoItemTabModeDimension, 'off' | 'currency'> {
  return dim !== 'off' && dim !== 'currency'
}

export function readSoItemTabMode(): SoItemTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    if (localStorage.getItem(LEGACY_CURRENCY_TAB_MODE_KEY) === '1') return 'currency'
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeSoItemTabMode(dimension: SoItemTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
    if (dimension === 'currency') {
      localStorage.setItem(LEGACY_CURRENCY_TAB_MODE_KEY, '1')
    } else {
      localStorage.setItem(LEGACY_CURRENCY_TAB_MODE_KEY, '0')
    }
  } catch {
    /* ignore quota / private mode */
  }
}

export type SoItemCurrencyTabId = 'all' | 'rmb' | 'foreign'

export function currencyFilterToTab(value: '' | 'rmb' | 'foreign'): SoItemCurrencyTabId {
  if (value === 'rmb' || value === 'foreign') return value
  return 'all'
}

export function currencyTabToFilter(tab: SoItemCurrencyTabId): '' | 'rmb' | 'foreign' {
  if (tab === 'rmb' || tab === 'foreign') return tab
  return ''
}

export type SoItemProgressTabId = 'all' | '0' | '1' | '2'

export function progressFilterToTab(value: number | number[] | undefined | null): SoItemProgressTabId {
  const arr = Array.isArray(value)
    ? value
    : value === 0 || value === 1 || value === 2
      ? [value]
      : []
  if (arr.length === 1 && (arr[0] === 0 || arr[0] === 1 || arr[0] === 2)) {
    return String(arr[0]) as SoItemProgressTabId
  }
  return 'all'
}

export function progressTabToFilter(tab: SoItemProgressTabId): number[] {
  if (tab === '0' || tab === '1' || tab === '2') return [Number(tab)]
  return []
}

export type SoItemProgressFilterKey =
  | 'purchaseProgressStatus'
  | 'stockInProgressStatus'
  | 'stockOutNotifyProgressStatus'
  | 'stockOutProgressStatus'
  | 'receiptProgressStatus'
  | 'invoiceProgressStatus'

const PROGRESS_DIM_TO_FILTER: Record<
  Exclude<SoItemTabModeDimension, 'off' | 'currency'>,
  SoItemProgressFilterKey
> = {
  purchase: 'purchaseProgressStatus',
  stockIn: 'stockInProgressStatus',
  stockOutNotify: 'stockOutNotifyProgressStatus',
  stockOut: 'stockOutProgressStatus',
  receipt: 'receiptProgressStatus',
  invoice: 'invoiceProgressStatus'
}

export function progressDimensionToFilterKey(
  dim: Exclude<SoItemTabModeDimension, 'off' | 'currency'>
): SoItemProgressFilterKey {
  return PROGRESS_DIM_TO_FILTER[dim]
}
