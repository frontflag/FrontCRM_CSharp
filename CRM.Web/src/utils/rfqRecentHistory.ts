export const RFQ_RECENT_HISTORY_CHANGED_EVENT = 'rfq-recent-history-changed'

const STORAGE_KEY = 'frontcrm_rfq_recent_v1'
const MAX_ENTRIES = 40

export interface RfqRecentEntry {
  id: string
  rfqCode: string
  customerName: string
  at: string
}

function parseList(raw: string | null): RfqRecentEntry[] {
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
          rfqCode: r.rfqCode != null ? String(r.rfqCode) : '',
          customerName: r.customerName != null ? String(r.customerName) : '',
          at: r.at != null ? String(r.at) : new Date().toISOString()
        } as RfqRecentEntry
      })
      .filter((x): x is RfqRecentEntry => x != null)
  } catch {
    return []
  }
}

export function recordRfqRecentView(p: { id: string; rfqCode?: string; customerName?: string }) {
  if (!p.id) return
  try {
    let list = parseList(localStorage.getItem(STORAGE_KEY))
    list = list.filter((e) => e.id !== p.id)
    list.unshift({
      id: p.id,
      rfqCode: p.rfqCode ?? '',
      customerName: p.customerName ?? '',
      at: new Date().toISOString()
    })
    list = list.slice(0, MAX_ENTRIES)
    localStorage.setItem(STORAGE_KEY, JSON.stringify(list))
    window.dispatchEvent(new Event(RFQ_RECENT_HISTORY_CHANGED_EVENT))
  } catch {
    /* ignore */
  }
}

export function readRfqRecentEntries(take: number): RfqRecentEntry[] {
  const list = parseList(localStorage.getItem(STORAGE_KEY))
  return list.slice(0, Math.max(0, take))
}

export function clearRfqRecentHistory() {
  try {
    localStorage.removeItem(STORAGE_KEY)
    window.dispatchEvent(new Event(RFQ_RECENT_HISTORY_CHANGED_EVENT))
  } catch {
    /* ignore */
  }
}
