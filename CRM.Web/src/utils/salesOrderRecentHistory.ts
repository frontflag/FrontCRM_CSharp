export const SALES_ORDER_RECENT_HISTORY_CHANGED_EVENT = 'sales-order-recent-history-changed'

const STORAGE_KEY = 'frontcrm_sales_order_recent_v1'
const MAX_ENTRIES = 40

export interface SalesOrderRecentEntry {
  id: string
  sellOrderCode: string
  customerName: string
  at: string
}

function parseList(raw: string | null): SalesOrderRecentEntry[] {
  if (!raw) return []
  try {
    const o = JSON.parse(raw) as unknown
    if (!Array.isArray(o)) return []
    return o
      .map((x) => {
        if (!x || typeof x !== 'object') return null
        const r = x as Record<string, unknown>
        const id = r.id != null ? String(r.id) : ''
        if (!id) return null
        return {
          id,
          sellOrderCode: r.sellOrderCode != null ? String(r.sellOrderCode) : '',
          customerName: r.customerName != null ? String(r.customerName) : '',
          at: r.at != null ? String(r.at) : new Date().toISOString()
        } as SalesOrderRecentEntry
      })
      .filter((x): x is SalesOrderRecentEntry => x != null)
  } catch {
    return []
  }
}

export function recordSalesOrderRecentView(p: { id: string; sellOrderCode?: string; customerName?: string }) {
  if (!p.id) return
  try {
    let list = parseList(localStorage.getItem(STORAGE_KEY))
    list = list.filter((e) => e.id !== p.id)
    list.unshift({
      id: p.id,
      sellOrderCode: p.sellOrderCode ?? '',
      customerName: p.customerName ?? '',
      at: new Date().toISOString()
    })
    list = list.slice(0, MAX_ENTRIES)
    localStorage.setItem(STORAGE_KEY, JSON.stringify(list))
    window.dispatchEvent(new Event(SALES_ORDER_RECENT_HISTORY_CHANGED_EVENT))
  } catch {
    /* ignore */
  }
}

export function readSalesOrderRecentEntries(take: number): SalesOrderRecentEntry[] {
  const list = parseList(localStorage.getItem(STORAGE_KEY))
  return list.slice(0, Math.max(0, take))
}

export function clearSalesOrderRecentHistory() {
  try {
    localStorage.removeItem(STORAGE_KEY)
    window.dispatchEvent(new Event(SALES_ORDER_RECENT_HISTORY_CHANGED_EVENT))
  } catch {
    /* ignore */
  }
}
