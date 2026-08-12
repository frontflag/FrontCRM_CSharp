import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  financePurchaseInvoiceWriteOffApi,
  type FinancePurchaseInvoiceWriteOffStockInRow,
  type FinancePurchaseInvoiceWriteOffVendorSummary
} from '@/api/financePurchaseInvoiceWriteOff'
import {
  invoiceDateSortValue,
  readPurchaseInvoiceWriteOffQueueSort,
  writePurchaseInvoiceWriteOffQueueSort,
  type PurchaseInvoiceWriteOffQueueSort
} from '@/utils/purchaseInvoiceWriteOffQueueSort'

export function purchaseInvoiceWriteOffVendorKey(row: {
  vendorId: string
  currency?: number | null
}) {
  return `${String(row.vendorId || '').trim()}::${row.currency ?? 0}`
}

function matchesKeyword(row: FinancePurchaseInvoiceWriteOffVendorSummary, keyword: string) {
  const k = keyword.trim().toLowerCase()
  if (!k) return true
  const parts = [row.vendorName, row.vendorEnglishName, row.vendorId]
  return parts.some((p) => (p || '').toLowerCase().includes(k))
}

/** 左栏待核销供应商列表默认每页行数 */
export const PURCHASE_INVOICE_WRITE_OFF_QUEUE_PAGE_SIZE = 20

/**
 * 进项发票核销桌面：左栏「待核销供应商」队列（供应商+币别；双边待核销）。
 */
export const usePurchaseInvoiceWriteOffDesktopQueueStore = defineStore(
  'purchaseInvoiceWriteOffDesktopQueue',
  () => {
    const loading = ref(false)
    const allRows = ref<FinancePurchaseInvoiceWriteOffVendorSummary[]>([])
    const keyword = ref('')
    const sortBy = ref<PurchaseInvoiceWriteOffQueueSort>(readPurchaseInvoiceWriteOffQueueSort())
    const page = ref(1)
    const pageSize = ref(PURCHASE_INVOICE_WRITE_OFF_QUEUE_PAGE_SIZE)
    const selected = ref<FinancePurchaseInvoiceWriteOffVendorSummary | null>(null)
    const focusedStockIn = ref<FinancePurchaseInvoiceWriteOffStockInRow | null>(null)
    const scrollToSelectedNonce = ref(0)

    const filteredList = computed(() => {
      const list = allRows.value.filter((r) => matchesKeyword(r, keyword.value))
      const mode = sortBy.value
      return [...list].sort((a, b) => {
        if (mode === 'latest') {
          const vb = invoiceDateSortValue(b.latestInvoiceDate)
          const va = invoiceDateSortValue(a.latestInvoiceDate)
          if (!Number.isFinite(vb) && !Number.isFinite(va)) return 0
          if (!Number.isFinite(va)) return 1
          if (!Number.isFinite(vb)) return -1
          if (vb !== va) return vb - va
        } else {
          const va = invoiceDateSortValue(a.earliestInvoiceDate)
          const vb = invoiceDateSortValue(b.earliestInvoiceDate)
          if (va !== vb) return va - vb
        }
        return purchaseInvoiceWriteOffVendorKey(a).localeCompare(purchaseInvoiceWriteOffVendorKey(b))
      })
    })

    const filteredTotal = computed(() => filteredList.value.length)

    const pageCount = computed(() =>
      Math.max(1, Math.ceil(filteredTotal.value / pageSize.value) || 1)
    )

    const pagedList = computed(() => {
      const size = pageSize.value
      const start = (page.value - 1) * size
      return filteredList.value.slice(start, start + size)
    })

    const selectedKey = computed(() =>
      selected.value ? purchaseInvoiceWriteOffVendorKey(selected.value) : ''
    )

    const selectedIndex = computed(() => {
      if (!selected.value) return -1
      const key = purchaseInvoiceWriteOffVendorKey(selected.value)
      return filteredList.value.findIndex((x) => purchaseInvoiceWriteOffVendorKey(x) === key)
    })

    function clampPage() {
      const max = pageCount.value
      if (page.value > max) page.value = max
      if (page.value < 1) page.value = 1
    }

    function ensurePageForSelected() {
      const idx = selectedIndex.value
      if (idx < 0) {
        clampPage()
        return
      }
      page.value = Math.floor(idx / pageSize.value) + 1
    }

    const canPrev = computed(() => selectedIndex.value > 0)
    const canNext = computed(
      () =>
        selectedIndex.value >= 0 && selectedIndex.value < filteredList.value.length - 1
    )

    const vendorBucketCount = computed(() => allRows.value.length)

    const pendingInvoiceRecordCount = computed(() =>
      allRows.value.reduce((sum, r) => sum + (Number(r.pendingInvoiceCount) || 0), 0)
    )

    function selectItem(item: FinancePurchaseInvoiceWriteOffVendorSummary | null) {
      selected.value = item
      focusedStockIn.value = null
    }

    function setFocusedStockIn(row: FinancePurchaseInvoiceWriteOffStockInRow | null) {
      focusedStockIn.value = row
    }

    function requestScrollToSelected() {
      scrollToSelectedNonce.value += 1
    }

    function pickSelectionAfterRefresh(preferKey?: string | null) {
      const prevKey = selected.value ? purchaseInvoiceWriteOffVendorKey(selected.value) : ''
      const list = filteredList.value
      if (!list.length) {
        selected.value = null
        focusedStockIn.value = null
        page.value = 1
        return
      }
      if (preferKey) {
        const hit = list.find((x) => purchaseInvoiceWriteOffVendorKey(x) === preferKey)
        if (hit) {
          selected.value = hit
          if (purchaseInvoiceWriteOffVendorKey(hit) !== prevKey) focusedStockIn.value = null
          ensurePageForSelected()
          return
        }
      }
      if (selected.value) {
        const key = purchaseInvoiceWriteOffVendorKey(selected.value)
        const still = list.find((x) => purchaseInvoiceWriteOffVendorKey(x) === key)
        if (still) {
          selected.value = still
          ensurePageForSelected()
          return
        }
      }
      selected.value = list[0]
      focusedStockIn.value = null
      page.value = 1
    }

    async function refreshAll(opts?: { preferKey?: string | null; keepSelection?: boolean }) {
      loading.value = true
      const prefer =
        opts?.preferKey ??
        (opts?.keepSelection !== false && selected.value
          ? purchaseInvoiceWriteOffVendorKey(selected.value)
          : null)
      try {
        const rows = await financePurchaseInvoiceWriteOffApi.getVendorSummaries()
        allRows.value = (rows ?? []).filter((r) => r.hasOpenStockIn)
        pickSelectionAfterRefresh(prefer)
        requestScrollToSelected()
      } finally {
        loading.value = false
      }
    }

    async function refreshAfterApply() {
      const key = selected.value ? purchaseInvoiceWriteOffVendorKey(selected.value) : null
      await refreshAll({ preferKey: key, keepSelection: true })
    }

    function goPrev() {
      if (!canPrev.value) return
      selected.value = filteredList.value[selectedIndex.value - 1] ?? null
      focusedStockIn.value = null
      ensurePageForSelected()
      requestScrollToSelected()
    }

    function goNext() {
      if (!canNext.value) return
      selected.value = filteredList.value[selectedIndex.value + 1] ?? null
      focusedStockIn.value = null
      ensurePageForSelected()
      requestScrollToSelected()
    }

    function setKeyword(v: string) {
      keyword.value = v
      if (selected.value) {
        const key = purchaseInvoiceWriteOffVendorKey(selected.value)
        const still = filteredList.value.find((x) => purchaseInvoiceWriteOffVendorKey(x) === key)
        if (!still) {
          selected.value = filteredList.value[0] ?? null
          focusedStockIn.value = null
          page.value = 1
          requestScrollToSelected()
          return
        }
        ensurePageForSelected()
      } else if (filteredList.value.length) {
        selected.value = filteredList.value[0]
        focusedStockIn.value = null
        page.value = 1
        requestScrollToSelected()
      } else {
        page.value = 1
      }
    }

    function setSortBy(v: PurchaseInvoiceWriteOffQueueSort) {
      if (sortBy.value === v) return
      sortBy.value = v
      writePurchaseInvoiceWriteOffQueueSort(v)
      ensurePageForSelected()
      requestScrollToSelected()
    }

    function setPage(p: number) {
      const next = Math.max(1, Math.min(pageCount.value, Math.floor(p) || 1))
      if (page.value === next) return
      page.value = next
    }

    function focusItem(vendorId: string, currency: number): boolean {
      const key = purchaseInvoiceWriteOffVendorKey({ vendorId, currency })
      const hit = allRows.value.find((x) => purchaseInvoiceWriteOffVendorKey(x) === key)
      if (!hit) return false
      keyword.value = ''
      selected.value = hit
      focusedStockIn.value = null
      ensurePageForSelected()
      requestScrollToSelected()
      return true
    }

    function reset() {
      loading.value = false
      allRows.value = []
      keyword.value = ''
      page.value = 1
      selected.value = null
      focusedStockIn.value = null
      scrollToSelectedNonce.value = 0
    }

    return {
      loading,
      allRows,
      keyword,
      sortBy,
      page,
      pageSize,
      selected,
      focusedStockIn,
      scrollToSelectedNonce,
      filteredList,
      filteredTotal,
      pagedList,
      selectedKey,
      selectedIndex,
      canPrev,
      canNext,
      vendorBucketCount,
      pendingInvoiceRecordCount,
      selectItem,
      setFocusedStockIn,
      requestScrollToSelected,
      refreshAll,
      refreshAfterApply,
      goPrev,
      goNext,
      setKeyword,
      setSortBy,
      setPage,
      focusItem,
      reset
    }
  }
)
