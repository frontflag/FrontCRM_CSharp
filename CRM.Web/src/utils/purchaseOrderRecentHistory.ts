export const PURCHASE_ORDER_RECENT_HISTORY_CHANGED_EVENT = 'purchase-order-recent-history-changed'

const STORAGE_KEY = 'frontcrm_purchase_order_recent_v1'
const MAX_ENTRIES = 40

export interface PurchaseOrderRecentEntry {
  id: string
  purchaseOrderCode: string
  vendorName: string
  at: string
}

function parseList(raw: string | null): PurchaseOrderRecentEntry[] {
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
          purchaseOrderCode: r.purchaseOrderCode != null ? String(r.purchaseOrderCode) : '',
          vendorName: r.vendorName != null ? String(r.vendorName) : '',
          at: r.at != null ? String(r.at) : new Date().toISOString()
        } as PurchaseOrderRecentEntry
      })
      .filter((x): x is PurchaseOrderRecentEntry => x != null)
  } catch {
    return []
  }
}

export function recordPurchaseOrderRecentView(p: {
  id: string
  purchaseOrderCode?: string
  vendorName?: string
}) {
  if (!p.id) return
  try {
    let list = parseList(localStorage.getItem(STORAGE_KEY))
    list = list.filter((e) => e.id !== p.id)
    list.unshift({
      id: p.id,
      purchaseOrderCode: p.purchaseOrderCode ?? '',
      vendorName: p.vendorName ?? '',
      at: new Date().toISOString()
    })
    list = list.slice(0, MAX_ENTRIES)
    localStorage.setItem(STORAGE_KEY, JSON.stringify(list))
    window.dispatchEvent(new Event(PURCHASE_ORDER_RECENT_HISTORY_CHANGED_EVENT))
  } catch {
    /* ignore */
  }
}

export function readPurchaseOrderRecentEntries(take: number): PurchaseOrderRecentEntry[] {
  const list = parseList(localStorage.getItem(STORAGE_KEY))
  return list.slice(0, Math.max(0, take))
}

export function clearPurchaseOrderRecentHistory() {
  try {
    localStorage.removeItem(STORAGE_KEY)
    window.dispatchEvent(new Event(PURCHASE_ORDER_RECENT_HISTORY_CHANGED_EVENT))
  } catch {
    /* ignore */
  }
}
