/** 进项发票核销桌面：待核销供应商队列排序偏好（localStorage）。 */

const STORAGE_KEY = 'crm.purchase-invoice-write-off.queue-sort'

/** earliest：按最早开票日期升序；latest：按最近开票日期降序 */
export type PurchaseInvoiceWriteOffQueueSort = 'earliest' | 'latest'

const SORT_SET = new Set<string>(['earliest', 'latest'])

function isSort(value: string): value is PurchaseInvoiceWriteOffQueueSort {
  return SORT_SET.has(value)
}

/** 默认按最早开票日期 */
export function readPurchaseInvoiceWriteOffQueueSort(): PurchaseInvoiceWriteOffQueueSort {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw && isSort(raw)) return raw
    return 'earliest'
  } catch {
    return 'earliest'
  }
}

export function writePurchaseInvoiceWriteOffQueueSort(sort: PurchaseInvoiceWriteOffQueueSort): void {
  try {
    localStorage.setItem(STORAGE_KEY, sort)
  } catch {
    /* ignore */
  }
}

export function invoiceDateSortValue(v?: string | null): number {
  if (!v) return Number.POSITIVE_INFINITY
  const t = Date.parse(String(v))
  return Number.isFinite(t) ? t : Number.POSITIVE_INFINITY
}
