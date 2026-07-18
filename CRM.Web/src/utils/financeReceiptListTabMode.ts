/** 收款记录列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.finance-receipt-list.tab-mode'

export type FinanceReceiptListTabModeDimension = 'off' | 'status' | 'receiptPurpose'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const FINANCE_RECEIPT_LIST_TAB_MODE_OPTIONS: Exclude<
  FinanceReceiptListTabModeDimension,
  'off'
>[] = ['status', 'receiptPurpose']

const TAB_MODE_SET = new Set<string>(['off', ...FINANCE_RECEIPT_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is FinanceReceiptListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readFinanceReceiptListTabMode(): FinanceReceiptListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeFinanceReceiptListTabMode(dimension: FinanceReceiptListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与状态下拉一致：草稿～已取消（含已取消） */
export const FR_STATUS_TAB_VALUES = [0, 1, 2, 3, 4] as const

export type FrStatusTabId = 'all' | '0' | '1' | '2' | '3' | '4'

export function frStatusFilterToTab(value: number | undefined | null): FrStatusTabId {
  if (value === 0 || value === 1 || value === 2 || value === 3 || value === 4) {
    return String(value) as FrStatusTabId
  }
  return 'all'
}

export function frStatusTabToFilter(tab: FrStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 0 || n === 1 || n === 2 || n === 3 || n === 4) return n
  return undefined
}

/** 10 普通 / 20 预收 */
export const FR_PURPOSE_TAB_VALUES = [10, 20] as const

export type FrPurposeTabId = 'all' | '10' | '20'

export function frPurposeFilterToTab(value: number | undefined | null): FrPurposeTabId {
  if (value === 10 || value === 20) return String(value) as FrPurposeTabId
  return 'all'
}

export function frPurposeTabToFilter(tab: FrPurposeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 10 || n === 20) return n
  return undefined
}
