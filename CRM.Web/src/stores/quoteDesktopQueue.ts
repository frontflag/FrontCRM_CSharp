import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { rfqApi } from '@/api/rfq'
import type { RFQItem } from '@/types/rfq'

/** 全部用 all（勿用空串：Element Plus Select 会显示「请选择」） */
export type QuoteDesktopDateFilter = 'all' | 'today' | 'yesterday' | 'dayBefore' | 'before3'

export type QuoteDesktopQueueItem = {
  id: string
  rfqId: string
  rfqCode: string
  createTime: string
  mpn: string
  brand: string
  salesUserName: string
  purchaserNames: string
  assignedPurchaserUserId1?: string | null
  assignedPurchaserUserId2?: string | null
}

const PAGE_SIZE = 20

function localDayStart(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0)
}

function addLocalDays(d: Date, days: number): Date {
  const n = new Date(d)
  n.setDate(n.getDate() + days)
  return n
}

/** 左栏日期筛 → itemCreate 时间窗（本地日） */
export function resolveQuoteDesktopDateWindow(
  filter: QuoteDesktopDateFilter
): { itemCreateStart?: string; itemCreateEndExclusive?: string } {
  if (!filter || filter === 'all') return {}
  const todayStart = localDayStart(new Date())
  const tomorrow = addLocalDays(todayStart, 1)
  if (filter === 'today') {
    return { itemCreateStart: todayStart.toISOString(), itemCreateEndExclusive: tomorrow.toISOString() }
  }
  if (filter === 'yesterday') {
    const y = addLocalDays(todayStart, -1)
    return { itemCreateStart: y.toISOString(), itemCreateEndExclusive: todayStart.toISOString() }
  }
  if (filter === 'dayBefore') {
    const d2 = addLocalDays(todayStart, -2)
    const d1 = addLocalDays(todayStart, -1)
    return { itemCreateStart: d2.toISOString(), itemCreateEndExclusive: d1.toISOString() }
  }
  // 3日前：创建日 ≤ 今天−3 → CreateTime < 前天 0:00
  const dayBeforeStart = addLocalDays(todayStart, -2)
  return { itemCreateEndExclusive: dayBeforeStart.toISOString() }
}

function mapRow(raw: RFQItem | Record<string, unknown>): QuoteDesktopQueueItem {
  const r = raw as Record<string, unknown>
  const id = String(r.id ?? r.Id ?? '').trim()
  const rfqId = String(r.rfqId ?? r.RfqId ?? '').trim()
  const rfqCode = String(r.rfqCode ?? r.RfqCode ?? '').trim()
  const createTime = String(
    r.itemCreateTime ?? r.ItemCreateTime ?? r.createTime ?? r.rfqCreateTime ?? r.RfqCreateTime ?? ''
  )
  const mpn = String(r.mpn ?? r.Mpn ?? r.materialModel ?? '').trim()
  const brand = String(r.brand ?? r.Brand ?? '').trim()
  const salesUserName = String(r.salesUserName ?? r.SalesUserName ?? '').trim()
  const p1 = String(r.assignedPurchaserName1 ?? r.AssignedPurchaserName1 ?? '').trim()
  const p2 = String(r.assignedPurchaserName2 ?? r.AssignedPurchaserName2 ?? '').trim()
  const purchaserNames = [p1, p2].filter(Boolean).join(' / ')
  return {
    id,
    rfqId,
    rfqCode,
    createTime,
    mpn,
    brand,
    salesUserName,
    purchaserNames,
    assignedPurchaserUserId1: (r.assignedPurchaserUserId1 ?? r.AssignedPurchaserUserId1) as
      | string
      | null
      | undefined,
    assignedPurchaserUserId2: (r.assignedPurchaserUserId2 ?? r.AssignedPurchaserUserId2) as
      | string
      | null
      | undefined
  }
}

function extractPage(res: unknown): {
  items: QuoteDesktopQueueItem[]
  total: number
  page: number
  pageSize: number
} {
  const r = res as Record<string, unknown>
  const rawItems = (r.items ?? r.Items ?? []) as unknown[]
  const items = rawItems.map((x) => mapRow(x as RFQItem))
  const total = Number(r.total ?? r.Total ?? r.totalCount ?? r.TotalCount ?? items.length)
  const page = Number(r.page ?? r.Page ?? r.pageNumber ?? r.PageNumber ?? 1)
  const pageSize = Number(r.pageSize ?? r.PageSize ?? PAGE_SIZE)
  return { items, total, page, pageSize }
}

/**
 * 报价桌面待报价队列（左栏与主区共享）。
 */
export const useQuoteDesktopQueueStore = defineStore('quoteDesktopQueue', () => {
  const loading = ref(false)
  const items = ref<QuoteDesktopQueueItem[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(PAGE_SIZE)
  const dateFilter = ref<QuoteDesktopDateFilter>('all')
  const selected = ref<QuoteDesktopQueueItem | null>(null)
  const scrollToSelectedNonce = ref(0)

  const selectedId = computed(() => selected.value?.id ?? '')
  const selectedIndex = computed(() => {
    if (!selected.value) return -1
    return items.value.findIndex((x) => x.id === selected.value!.id)
  })

  const canPrev = computed(() => {
    if (selectedIndex.value > 0) return true
    return page.value > 1 && total.value > 0
  })

  const canNext = computed(() => {
    if (selectedIndex.value >= 0 && selectedIndex.value < items.value.length - 1) return true
    const loaded = (page.value - 1) * pageSize.value + items.value.length
    return loaded < total.value
  })

  function selectItem(item: QuoteDesktopQueueItem | null) {
    selected.value = item
  }

  function requestScrollToSelected() {
    scrollToSelectedNonce.value += 1
  }

  async function loadPage(opts?: { page?: number; preferItemId?: string | null }) {
    loading.value = true
    try {
      const nextPage = opts?.page != null && opts.page >= 1 ? opts.page : page.value
      const window = resolveQuoteDesktopDateWindow(dateFilter.value)
      // 与 /rfq-items「待报价」一致：pending_quote + 数据权限；可报价校验在提交报价时做
      const res = await rfqApi.searchRFQItems({
        pageNumber: nextPage,
        pageSize: pageSize.value,
        quickFilter: 'pending_quote',
        preferItemId: opts?.preferItemId || undefined,
        itemCreateStart: window.itemCreateStart,
        itemCreateEndExclusive: window.itemCreateEndExclusive
      })
      const extracted = extractPage(res)
      items.value = extracted.items
      total.value = extracted.total
      page.value = extracted.page
      pageSize.value = extracted.pageSize
      return extracted
    } finally {
      loading.value = false
    }
  }

  async function refreshAll(opts?: { preferItemId?: string | null; keepSelection?: boolean }) {
    const keepId = opts?.keepSelection ? selected.value?.id : undefined
    const prefer = opts?.preferItemId || keepId || undefined
    const extracted = await loadPage({ page: prefer ? page.value : page.value, preferItemId: prefer })
    if (prefer) {
      const hit = extracted.items.find((x) => x.id === prefer)
      if (hit) {
        selectItem(hit)
        requestScrollToSelected()
        return true
      }
    }
    if (opts?.keepSelection && keepId) {
      const still = extracted.items.find((x) => x.id === keepId)
      if (still) {
        selectItem(still)
        return true
      }
    }
    if (!selected.value || !extracted.items.some((x) => x.id === selected.value?.id)) {
      selectItem(extracted.items[0] ?? null)
      if (selected.value) requestScrollToSelected()
    }
    return !!selected.value
  }

  /** 深链定位；找不到返回 false */
  async function focusItem(rfqItemId: string): Promise<boolean> {
    const id = String(rfqItemId || '').trim()
    if (!id) return false
    dateFilter.value = 'all'
    const extracted = await loadPage({ page: 1, preferItemId: id })
    const hit = extracted.items.find((x) => x.id === id)
    if (!hit) {
      selectItem(extracted.items[0] ?? null)
      if (selected.value) requestScrollToSelected()
      return false
    }
    selectItem(hit)
    requestScrollToSelected()
    return true
  }

  async function goPrev() {
    if (selectedIndex.value > 0) {
      selectItem(items.value[selectedIndex.value - 1] ?? null)
      requestScrollToSelected()
      return
    }
    if (page.value <= 1) return
    await loadPage({ page: page.value - 1 })
    selectItem(items.value[items.value.length - 1] ?? null)
    requestScrollToSelected()
  }

  async function goNext() {
    if (selectedIndex.value >= 0 && selectedIndex.value < items.value.length - 1) {
      selectItem(items.value[selectedIndex.value + 1] ?? null)
      requestScrollToSelected()
      return
    }
    const loaded = (page.value - 1) * pageSize.value + items.value.length
    if (loaded >= total.value) return
    await loadPage({ page: page.value + 1 })
    selectItem(items.value[0] ?? null)
    requestScrollToSelected()
  }

  /**
   * 报价/查无报价成功后：记住「原下一条」的全局位置，刷新后尽量选中它。
   */
  async function refreshAfterComplete() {
    const idx = selectedIndex.value
    const hadNextOnPage = idx >= 0 && idx < items.value.length - 1
    const nextIdOnPage = hadNextOnPage ? items.value[idx + 1]?.id : null
    const globalIndex = idx >= 0 ? (page.value - 1) * pageSize.value + idx : -1
    const nextGlobalIndex = globalIndex >= 0 ? globalIndex + 1 : -1

    await loadPage({ page: page.value })

    if (nextIdOnPage) {
      const hit = items.value.find((x) => x.id === nextIdOnPage)
      if (hit) {
        selectItem(hit)
        requestScrollToSelected()
        return
      }
    }

    // 原「下一条」可能因当前条消失而落到本页同一 index，或需翻页
    if (nextGlobalIndex >= 0 && nextGlobalIndex < total.value) {
      const targetPage = Math.floor(nextGlobalIndex / pageSize.value) + 1
      const indexInPage = nextGlobalIndex % pageSize.value
      if (targetPage !== page.value) {
        await loadPage({ page: targetPage })
      }
      selectItem(items.value[indexInPage] ?? items.value[items.value.length - 1] ?? null)
      requestScrollToSelected()
      return
    }

    if (items.value.length) {
      const fallbackIdx = Math.min(Math.max(idx, 0), items.value.length - 1)
      selectItem(items.value[fallbackIdx] ?? null)
      requestScrollToSelected()
      return
    }

    if (page.value > 1) {
      await loadPage({ page: page.value - 1 })
      selectItem(items.value[items.value.length - 1] ?? null)
      requestScrollToSelected()
      return
    }

    selectItem(null)
  }

  async function setDateFilter(filter: QuoteDesktopDateFilter) {
    dateFilter.value = filter || 'all'
    await loadPage({ page: 1 })
    selectItem(items.value[0] ?? null)
    if (selected.value) requestScrollToSelected()
  }

  async function setPage(p: number) {
    await loadPage({ page: p })
    selectItem(items.value[0] ?? null)
    if (selected.value) requestScrollToSelected()
  }

  function clear() {
    items.value = []
    total.value = 0
    page.value = 1
    selected.value = null
    dateFilter.value = 'all'
  }

  return {
    loading,
    items,
    total,
    page,
    pageSize,
    dateFilter,
    selected,
    selectedId,
    selectedIndex,
    scrollToSelectedNonce,
    canPrev,
    canNext,
    selectItem,
    requestScrollToSelected,
    loadPage,
    refreshAll,
    focusItem,
    goPrev,
    goNext,
    refreshAfterComplete,
    setDateFilter,
    setPage,
    clear
  }
})
