import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  financeReceivableApi,
  type FinanceReceivableWriteOffCandidateRow,
  type FinanceWriteOffCustomerSummary
} from '@/api/financeReceivable'
import {
  readReceiptWriteOffQueueSort,
  receiptDateSortValue,
  writeReceiptWriteOffQueueSort,
  type ReceiptWriteOffQueueSort
} from '@/utils/receiptWriteOffQueueSort'

export function receiptWriteOffCustomerKey(row: {
  customerId: string
  currency?: number | null
}) {
  return `${String(row.customerId || '').trim()}::${row.currency ?? 0}`
}

function matchesKeyword(row: FinanceWriteOffCustomerSummary, keyword: string) {
  const k = keyword.trim().toLowerCase()
  if (!k) return true
  const parts = [
    row.customerName,
    row.customerEnglishName,
    row.customerCode,
    row.salesUserName,
    row.customerId
  ]
  return parts.some((p) => (p || '').toLowerCase().includes(k))
}

/** 左栏待核销客户列表默认每页行数 */
export const RECEIPT_WRITE_OFF_QUEUE_PAGE_SIZE = 20

/**
 * 收款核销桌面：左栏「待核销客户」队列（客户+币别；仅有待核销应收）。
 */
export const useReceiptWriteOffDesktopQueueStore = defineStore(
  'receiptWriteOffDesktopQueue',
  () => {
    const loading = ref(false)
    /** 有应收的客户+币别（全量，不受左栏搜索影响；顶栏 N/M 用） */
    const allRows = ref<FinanceWriteOffCustomerSummary[]>([])
    const keyword = ref('')
    const sortBy = ref<ReceiptWriteOffQueueSort>(readReceiptWriteOffQueueSort())
    const page = ref(1)
    const pageSize = ref(RECEIPT_WRITE_OFF_QUEUE_PAGE_SIZE)
    const selected = ref<FinanceWriteOffCustomerSummary | null>(null)
    /** 中区当前聚焦的待核销应收行（驱动右栏「出库单」） */
    const focusedReceivable = ref<FinanceReceivableWriteOffCandidateRow | null>(null)
    const scrollToSelectedNonce = ref(0)

    const filteredList = computed(() => {
      const list = allRows.value.filter((r) => matchesKeyword(r, keyword.value))
      const mode = sortBy.value
      return [...list].sort((a, b) => {
        if (mode === 'latest') {
          const vb = receiptDateSortValue(b.latestReceiptDate)
          const va = receiptDateSortValue(a.latestReceiptDate)
          // 无日期沉底；有日期则最近在前
          if (!Number.isFinite(vb) && !Number.isFinite(va)) return 0
          if (!Number.isFinite(va)) return 1
          if (!Number.isFinite(vb)) return -1
          if (vb !== va) return vb - va
        } else {
          const va = receiptDateSortValue(a.earliestReceiptDate)
          const vb = receiptDateSortValue(b.earliestReceiptDate)
          if (va !== vb) return va - vb
        }
        return receiptWriteOffCustomerKey(a).localeCompare(receiptWriteOffCustomerKey(b))
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
      selected.value ? receiptWriteOffCustomerKey(selected.value) : ''
    )

    const selectedIndex = computed(() => {
      if (!selected.value) return -1
      const key = receiptWriteOffCustomerKey(selected.value)
      return filteredList.value.findIndex((x) => receiptWriteOffCustomerKey(x) === key)
    })

    function clampPage() {
      const max = pageCount.value
      if (page.value > max) page.value = max
      if (page.value < 1) page.value = 1
    }

    /** 将当前选中项所在页设为当前页 */
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

    /** 顶栏 N：有应收的客户+币别数（全局，不随搜索变） */
    const customerBucketCount = computed(() => allRows.value.length)

    /** 顶栏 M：上述桶下待核销收款条数合计 */
    const pendingReceiptItemCount = computed(() =>
      allRows.value.reduce((sum, r) => sum + (Number(r.pendingReceiptItemCount) || 0), 0)
    )

    function selectItem(item: FinanceWriteOffCustomerSummary | null) {
      selected.value = item
      focusedReceivable.value = null
    }

    function setFocusedReceivable(row: FinanceReceivableWriteOffCandidateRow | null) {
      focusedReceivable.value = row
    }

    function requestScrollToSelected() {
      scrollToSelectedNonce.value += 1
    }

    function pickSelectionAfterRefresh(preferKey?: string | null) {
      const prevKey = selected.value ? receiptWriteOffCustomerKey(selected.value) : ''
      const list = filteredList.value
      if (!list.length) {
        selected.value = null
        focusedReceivable.value = null
        page.value = 1
        return
      }
      if (preferKey) {
        const hit = list.find((x) => receiptWriteOffCustomerKey(x) === preferKey)
        if (hit) {
          selected.value = hit
          if (receiptWriteOffCustomerKey(hit) !== prevKey) focusedReceivable.value = null
          ensurePageForSelected()
          return
        }
      }
      if (selected.value) {
        const key = receiptWriteOffCustomerKey(selected.value)
        const still = list.find((x) => receiptWriteOffCustomerKey(x) === key)
        if (still) {
          selected.value = still
          ensurePageForSelected()
          return
        }
      }
      selected.value = list[0]
      focusedReceivable.value = null
      page.value = 1
    }

    async function refreshAll(opts?: { preferKey?: string | null; keepSelection?: boolean }) {
      loading.value = true
      const prefer =
        opts?.preferKey ??
        (opts?.keepSelection !== false && selected.value
          ? receiptWriteOffCustomerKey(selected.value)
          : null)
      try {
        const rows = await financeReceivableApi.getWriteOffCustomerSummaries()
        allRows.value = (rows ?? []).filter((r) => r.hasOpenReceivable)
        pickSelectionAfterRefresh(prefer)
        requestScrollToSelected()
      } finally {
        loading.value = false
      }
    }

    /** 核销成功：刷新队列，尽量留在当前客户+币别 */
    async function refreshAfterApply() {
      const key = selected.value ? receiptWriteOffCustomerKey(selected.value) : null
      await refreshAll({ preferKey: key, keepSelection: true })
    }

    function goPrev() {
      if (!canPrev.value) return
      selected.value = filteredList.value[selectedIndex.value - 1] ?? null
      focusedReceivable.value = null
      ensurePageForSelected()
      requestScrollToSelected()
    }

    function goNext() {
      if (!canNext.value) return
      selected.value = filteredList.value[selectedIndex.value + 1] ?? null
      focusedReceivable.value = null
      ensurePageForSelected()
      requestScrollToSelected()
    }

    function setKeyword(v: string) {
      keyword.value = v
      if (selected.value) {
        const key = receiptWriteOffCustomerKey(selected.value)
        const still = filteredList.value.find((x) => receiptWriteOffCustomerKey(x) === key)
        if (!still) {
          selected.value = filteredList.value[0] ?? null
          focusedReceivable.value = null
          page.value = 1
          requestScrollToSelected()
          return
        }
        ensurePageForSelected()
      } else if (filteredList.value.length) {
        selected.value = filteredList.value[0]
        focusedReceivable.value = null
        page.value = 1
        requestScrollToSelected()
      } else {
        page.value = 1
      }
    }

    function setSortBy(v: ReceiptWriteOffQueueSort) {
      if (sortBy.value === v) return
      sortBy.value = v
      writeReceiptWriteOffQueueSort(v)
      ensurePageForSelected()
      requestScrollToSelected()
    }

    function setPage(p: number) {
      const next = Math.max(1, Math.min(pageCount.value, Math.floor(p) || 1))
      if (page.value === next) return
      page.value = next
    }

    function focusItem(customerId: string, currency: number): boolean {
      const key = receiptWriteOffCustomerKey({ customerId, currency })
      const hit = allRows.value.find((x) => receiptWriteOffCustomerKey(x) === key)
      if (!hit) return false
      keyword.value = ''
      selected.value = hit
      focusedReceivable.value = null
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
      focusedReceivable.value = null
      scrollToSelectedNonce.value = 0
      // 保留 sortBy（用户偏好）
    }

    return {
      loading,
      allRows,
      keyword,
      sortBy,
      page,
      pageSize,
      selected,
      focusedReceivable,
      scrollToSelectedNonce,
      filteredList,
      filteredTotal,
      pagedList,
      selectedKey,
      selectedIndex,
      canPrev,
      canNext,
      customerBucketCount,
      pendingReceiptItemCount,
      selectItem,
      setFocusedReceivable,
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
