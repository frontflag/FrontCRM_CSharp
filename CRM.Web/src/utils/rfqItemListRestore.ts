/** 从需求明细列表跳转报价等页面前写入；返回列表时一次性消费，恢复分页与选中行。 */
const STORAGE_KEY = 'crm:rfq-item-list:restore-v1'

export interface RfqItemListRestoreState {
  page: number
  pageSize: number
  selectedItemId?: string
}

export function saveRfqItemListRestoreState(state: RfqItemListRestoreState): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state))
  } catch {
    /* ignore quota / private mode */
  }
}

export function consumeRfqItemListRestoreState(): RfqItemListRestoreState | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    sessionStorage.removeItem(STORAGE_KEY)
    const parsed = JSON.parse(raw) as Partial<RfqItemListRestoreState>
    const page = Number(parsed.page)
    const pageSize = Number(parsed.pageSize)
    if (!Number.isFinite(page) || page < 1) return null
    if (!Number.isFinite(pageSize) || pageSize < 1) return null
    const selectedItemId =
      typeof parsed.selectedItemId === 'string' && parsed.selectedItemId.trim()
        ? parsed.selectedItemId.trim()
        : undefined
    return { page, pageSize, selectedItemId }
  } catch {
    try {
      sessionStorage.removeItem(STORAGE_KEY)
    } catch {
      /* ignore */
    }
    return null
  }
}
