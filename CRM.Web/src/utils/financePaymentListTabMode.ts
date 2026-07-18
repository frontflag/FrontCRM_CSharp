/** 付款记录列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.finance-payment-list.tab-mode'

export type FinancePaymentListTabModeDimension = 'off' | 'status' | 'paymentMode'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const FINANCE_PAYMENT_LIST_TAB_MODE_OPTIONS: Exclude<
  FinancePaymentListTabModeDimension,
  'off'
>[] = ['status', 'paymentMode']

const TAB_MODE_SET = new Set<string>(['off', ...FINANCE_PAYMENT_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is FinancePaymentListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readFinancePaymentListTabMode(): FinancePaymentListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeFinancePaymentListTabMode(dimension: FinancePaymentListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与状态下拉一致：新建～付款完成 + 审核失败 + 取消 */
export const FP_STATUS_TAB_VALUES = [1, 2, 10, 100, -1, -2] as const

export type FpStatusTabId = 'all' | '1' | '2' | '10' | '100' | '-1' | '-2'

export function fpStatusFilterToTab(value: number | undefined | null): FpStatusTabId {
  if (
    value === 1 ||
    value === 2 ||
    value === 10 ||
    value === 100 ||
    value === -1 ||
    value === -2
  ) {
    return String(value) as FpStatusTabId
  }
  return 'all'
}

export function fpStatusTabToFilter(tab: FpStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  if (tab === '-1') return -1
  if (tab === '-2') return -2
  const n = Number(tab)
  if (n === 1 || n === 2 || n === 10 || n === 100) return n
  return undefined
}

/** 与付款方式下拉一致 */
export const FP_PAYMENT_MODE_TAB_VALUES = [1, 2, 3, 4] as const

export type FpPaymentModeTabId = 'all' | '1' | '2' | '3' | '4'

export function fpPaymentModeFilterToTab(value: number | undefined | null): FpPaymentModeTabId {
  if (value === 1 || value === 2 || value === 3 || value === 4) {
    return String(value) as FpPaymentModeTabId
  }
  return 'all'
}

export function fpPaymentModeTabToFilter(tab: FpPaymentModeTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 1 || n === 2 || n === 3 || n === 4) return n
  return undefined
}
