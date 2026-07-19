/** 客户列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.customer-list.tab-mode'

export type CustomerListTabModeDimension = 'off' | 'settlementCurrency'

/** 可在「页签模式」子菜单中选择的维度（不含 off）。 */
export const CUSTOMER_LIST_TAB_MODE_OPTIONS: Exclude<CustomerListTabModeDimension, 'off'>[] = [
  'settlementCurrency'
]

const TAB_MODE_SET = new Set<string>(['off', ...CUSTOMER_LIST_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is CustomerListTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readCustomerListTabMode(): CustomerListTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeCustomerListTabMode(dimension: CustomerListTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

export type CustomerSettlementCurrencyTabId = 'all' | '1' | '2' | '3' | '4'

export function settlementCurrencyFilterToTab(
  value: number | undefined | null
): CustomerSettlementCurrencyTabId {
  if (value === 1 || value === 2 || value === 3 || value === 4) {
    return String(value) as CustomerSettlementCurrencyTabId
  }
  return 'all'
}

export function settlementCurrencyTabToFilter(
  tab: CustomerSettlementCurrencyTabId
): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 1 || n === 2 || n === 3 || n === 4) return n
  return undefined
}
