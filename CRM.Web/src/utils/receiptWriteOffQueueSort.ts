/** 收款核销桌面：待核销客户队列排序偏好（localStorage）。 */

const STORAGE_KEY = 'crm.receipt-write-off.queue-sort'

/** earliest：按最早收款日期升序；latest：按最近收款日期降序 */
export type ReceiptWriteOffQueueSort = 'earliest' | 'latest'

const SORT_SET = new Set<string>(['earliest', 'latest'])

function isSort(value: string): value is ReceiptWriteOffQueueSort {
  return SORT_SET.has(value)
}

/** 默认按最早收款日期（先核销更早收款） */
export function readReceiptWriteOffQueueSort(): ReceiptWriteOffQueueSort {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw && isSort(raw)) return raw
    return 'earliest'
  } catch {
    return 'earliest'
  }
}

export function writeReceiptWriteOffQueueSort(sort: ReceiptWriteOffQueueSort): void {
  try {
    localStorage.setItem(STORAGE_KEY, sort)
  } catch {
    /* ignore */
  }
}

export function receiptDateSortValue(v?: string | null): number {
  if (!v) return Number.POSITIVE_INFINITY
  const t = Date.parse(String(v))
  return Number.isFinite(t) ? t : Number.POSITIVE_INFINITY
}
