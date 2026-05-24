export const PACKING_STOCK_OUT_QUEUE_KEY = 'packingStockOutRequestQueue'

export interface PackingStockOutQueueEntry {
  requestId: string
  packingId: string
}

export function parsePackingStockOutQueue(raw: string): PackingStockOutQueueEntry[] {
  const parsed = JSON.parse(raw) as unknown
  if (!Array.isArray(parsed)) return []
  if (parsed.length > 0 && typeof parsed[0] === 'string') {
    return (parsed as string[])
      .map((requestId) => ({ requestId: String(requestId || '').trim(), packingId: '' }))
      .filter((x) => x.requestId)
  }
  return parsed
    .map((x) => {
      const o = x && typeof x === 'object' ? (x as Record<string, unknown>) : {}
      return {
        requestId: String(o.requestId ?? o.RequestId ?? o.stockOutRequestId ?? o.StockOutRequestId ?? '').trim(),
        packingId: String(o.packingId ?? o.PackingId ?? '').trim()
      }
    })
    .filter((x) => x.requestId)
}

export function popNextPackingStockOutQueueEntry(): PackingStockOutQueueEntry | null {
  const raw = sessionStorage.getItem(PACKING_STOCK_OUT_QUEUE_KEY)
  if (!raw) return null
  try {
    const queue = parsePackingStockOutQueue(raw)
    if (!queue.length) {
      sessionStorage.removeItem(PACKING_STOCK_OUT_QUEUE_KEY)
      return null
    }
    const [next, ...rest] = queue
    if (rest.length) sessionStorage.setItem(PACKING_STOCK_OUT_QUEUE_KEY, JSON.stringify(rest))
    else sessionStorage.removeItem(PACKING_STOCK_OUT_QUEUE_KEY)
    return next
  } catch {
    sessionStorage.removeItem(PACKING_STOCK_OUT_QUEUE_KEY)
    return null
  }
}
