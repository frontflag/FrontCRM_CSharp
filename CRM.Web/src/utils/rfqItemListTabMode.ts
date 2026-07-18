/** 需求明细列表：筛选页签模式偏好（localStorage，单维度）。 */

const TAB_MODE_KEY = 'crm.rfq-item-list.tab-mode'

export type RfqItemTabModeDimension = 'off' | 'itemStatus'

/** 可在「页签模式」子菜单中选择的维度（不含 off） */
export const RFQ_ITEM_TAB_MODE_OPTIONS: Exclude<RfqItemTabModeDimension, 'off'>[] = ['itemStatus']

const TAB_MODE_SET = new Set<string>(['off', ...RFQ_ITEM_TAB_MODE_OPTIONS])

function isTabModeDimension(value: string): value is RfqItemTabModeDimension {
  return TAB_MODE_SET.has(value)
}

export function readRfqItemTabMode(): RfqItemTabModeDimension {
  try {
    const raw = localStorage.getItem(TAB_MODE_KEY)
    if (raw && isTabModeDimension(raw)) return raw
    return 'off'
  } catch {
    return 'off'
  }
}

export function writeRfqItemTabMode(dimension: RfqItemTabModeDimension): void {
  try {
    localStorage.setItem(TAB_MODE_KEY, dimension)
  } catch {
    /* ignore quota / private mode */
  }
}

/** 与明细状态下拉一致：0～5；「全部」对应清空 */
export type RfqItemStatusTabId = 'all' | '0' | '1' | '2' | '3' | '4' | '5'

/** 展示顺序：待报价 → 已报价 → 查无报价 → 已接受 → 已拒绝 → 已关闭（与搜索栏下拉一致） */
export const RFQ_ITEM_STATUS_TAB_VALUES = [0, 1, 5, 2, 3, 4] as const

export function itemStatusFilterToTab(value: number | undefined | null): RfqItemStatusTabId {
  if (value === 0 || value === 1 || value === 2 || value === 3 || value === 4 || value === 5) {
    return String(value) as RfqItemStatusTabId
  }
  return 'all'
}

export function itemStatusTabToFilter(tab: RfqItemStatusTabId): number | undefined {
  if (tab === 'all') return undefined
  const n = Number(tab)
  if (n === 0 || n === 1 || n === 2 || n === 3 || n === 4 || n === 5) return n
  return undefined
}
