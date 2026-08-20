import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  approvalsApi,
  type ApprovalSummary,
  type BizType,
  type PendingApprovalItem
} from '@/api/approvals'

/** 与 ApprovalAuditWorkspace 发出的 context 对齐（避免 store 依赖 .vue） */
export type ApprovalDesktopPartyContext = {
  bizType: BizType
  businessId: string
  customerId?: string | null
  customerName?: string | null
  vendorId?: string | null
  vendorName?: string | null
  orderItems?: any[]
}

const BIZ_TYPES: BizType[] = [
  'CUSTOMER',
  'VENDOR',
  'SALES_ORDER',
  'PURCHASE_ORDER',
  'FINANCE_PAYMENT'
]

function normalizePendingItem(raw: PendingApprovalItem): PendingApprovalItem {
  const legacy = raw as unknown as Record<string, unknown>
  const canDecideRaw = raw.canDecide ?? legacy.CanDecide
  const canDecide = typeof canDecideRaw === 'boolean' ? canDecideRaw : true
  const approver = (raw.approver ?? legacy.Approver) as string | null | undefined
  const approvedAt = (raw.approvedAt ?? legacy.ApprovedAt) as string | null | undefined
  return { ...raw, canDecide, approver: approver ?? null, approvedAt: approvedAt ?? null }
}

function extractPageItems(res: unknown): { items: PendingApprovalItem[]; total: number } {
  const r = res as {
    items?: PendingApprovalItem[]
    Items?: PendingApprovalItem[]
    total?: number
    Total?: number
  }
  const items = (r.items ?? r.Items ?? []).map(normalizePendingItem)
  const total = Number(r.total ?? r.Total ?? items.length)
  return { items, total }
}

export function approvalItemKey(item: PendingApprovalItem) {
  return `${item.bizType}:${item.businessId}`
}

/**
 * 审核桌面待审队列（左栏扩展面板页签与主区共享）。
 */
export const useApprovalDesktopQueueStore = defineStore('approvalDesktopQueue', () => {
  const loading = ref(false)
  const pendingList = ref<PendingApprovalItem[]>([])
  const bizTypeFilter = ref<'' | BizType>('')
  const selected = ref<PendingApprovalItem | null>(null)
  /** 当前审批工作区加载出的关联方 / 明细（右扩展调查与行卡片用） */
  const partyContext = ref<ApprovalDesktopPartyContext | null>(null)
  const stats = ref({
    total: 0,
    CUSTOMER: 0,
    VENDOR: 0,
    SALES_ORDER: 0,
    PURCHASE_ORDER: 0,
    FINANCE_PAYMENT: 0,
    FINANCE_RECEIPT: 0
  })

  const filteredList = computed(() => {
    const filter = bizTypeFilter.value
    if (!filter) return pendingList.value
    return pendingList.value.filter((x) => x.bizType === filter)
  })

  const selectedKey = computed(() => (selected.value ? approvalItemKey(selected.value) : ''))

  const selectedIndex = computed(() => {
    if (!selected.value) return -1
    const key = approvalItemKey(selected.value)
    return filteredList.value.findIndex((x) => approvalItemKey(x) === key)
  })

  const canPrev = computed(() => selectedIndex.value > 0)
  const canNext = computed(
    () => selectedIndex.value >= 0 && selectedIndex.value < filteredList.value.length - 1
  )

  /** 左栏队列滚到选中项（变更时递增，供面板 watch） */
  const scrollToSelectedNonce = ref(0)

  function selectItem(item: PendingApprovalItem | null) {
    const prevKey = selected.value ? approvalItemKey(selected.value) : ''
    const nextKey = item ? approvalItemKey(item) : ''
    selected.value = item
    if (prevKey !== nextKey) {
      partyContext.value = null
    }
  }

  function requestScrollToSelected() {
    scrollToSelectedNonce.value += 1
  }

  /** 按业务键定位待审项；会清空类型筛选以确保左栏可见，并触发滚动 */
  function focusItem(bizType: string, businessId: string): boolean {
    const bt = String(bizType || '').trim()
    const id = String(businessId || '').trim()
    if (!bt || !id) return false
    const key = `${bt}:${id}`
    const hit = pendingList.value.find((x) => approvalItemKey(x) === key)
    if (!hit) return false
    if (bizTypeFilter.value && bizTypeFilter.value !== hit.bizType) {
      bizTypeFilter.value = ''
    }
    selectItem(hit)
    requestScrollToSelected()
    return true
  }

  function setPartyContext(ctx: ApprovalDesktopPartyContext | null) {
    partyContext.value = ctx
  }

  function goPrev() {
    if (!canPrev.value) return
    selectItem(filteredList.value[selectedIndex.value - 1] ?? null)
  }

  function goNext() {
    if (!canNext.value) return
    selectItem(filteredList.value[selectedIndex.value + 1] ?? null)
  }

  function pickSelectionAfterFilter() {
    const list = filteredList.value
    if (list.length === 0) {
      selectItem(null)
      return
    }
    if (selected.value) {
      const key = approvalItemKey(selected.value)
      const still = list.find((x) => approvalItemKey(x) === key)
      if (still) {
        selected.value = still
        return
      }
    }
    selectItem(list[0] ?? null)
  }

  async function loadPendingQueue() {
    const pageSize = 500
    let page = 1
    let total = Infinity
    const all: PendingApprovalItem[] = []

    while (all.length < total) {
      const res = await approvalsApi.getApprovalItems({
        state: 'pending',
        page,
        pageSize,
        sortBy: 'submittedAt',
        sortDir: 'desc',
        sortAsc: false
      })
      const { items, total: pageTotal } = extractPageItems(res)
      total = Number.isFinite(pageTotal) ? pageTotal : items.length
      all.push(...items)
      if (items.length < pageSize) break
      page += 1
      if (page > 40) break
    }

    pendingList.value = all
  }

  /** summary 失败时，用已加载队列回填导航统计，避免「有列表却显示 0 条」 */
  function syncStatsFromPendingList() {
    const next = {
      total: pendingList.value.length,
      CUSTOMER: 0,
      VENDOR: 0,
      SALES_ORDER: 0,
      PURCHASE_ORDER: 0,
      FINANCE_PAYMENT: 0,
      FINANCE_RECEIPT: 0
    }
    for (const item of pendingList.value) {
      const bt = item.bizType as BizType
      if (BIZ_TYPES.includes(bt)) next[bt] += 1
    }
    stats.value = next
  }

  /**
   * 可选：一次 COUNT summary（pendingOnly）校正顶栏；失败则保持队列回填。
   * 审核桌面默认不再依赖 7 次 summary。
   */
  async function loadGlobalStatsFromSummary() {
    const res = (await approvalsApi.getApprovalSummary({ pendingOnly: true })) as ApprovalSummary &
      Record<string, unknown>
    const next = {
      total: Number(res?.pendingCount ?? res?.PendingCount ?? 0),
      CUSTOMER: 0,
      VENDOR: 0,
      SALES_ORDER: 0,
      PURCHASE_ORDER: 0,
      FINANCE_PAYMENT: 0,
      FINANCE_RECEIPT: 0
    }
    const byRaw = (res?.byBizType ?? res?.ByBizType ?? {}) as Record<string, Record<string, unknown>>
    for (const bt of BIZ_TYPES) {
      const row = byRaw[bt] ?? byRaw[bt.toLowerCase()] ?? {}
      next[bt] = Number(row.pendingCount ?? row.PendingCount ?? 0)
    }
    if (next.total <= 0 && BIZ_TYPES.some((bt) => next[bt] > 0)) {
      next.total = BIZ_TYPES.reduce((s, bt) => s + next[bt], 0)
    }
    stats.value = next
  }

  async function refreshAll() {
    loading.value = true
    try {
      // A：队列加载完即结束转圈并用队列回填统计（不再打 7×summary）
      await loadPendingQueue()
      syncStatsFromPendingList()
    } finally {
      loading.value = false
    }
    // B1：后台一次 pendingOnly COUNT 校正顶栏（失败则保持队列回填，不挡左栏）
    try {
      await loadGlobalStatsFromSummary()
    } catch {
      /* ignore */
    }
  }

  /** 审批完成后：刷新队列并优先选中原「下一条」 */
  async function refreshAfterDecide() {
    const listBefore = filteredList.value
    const idx = selectedIndex.value
    const nextBefore =
      idx >= 0 && idx < listBefore.length - 1 ? listBefore[idx + 1] : null
    const prevBefore = idx > 0 ? listBefore[idx - 1] : null

    await refreshAll()

    const list = filteredList.value
    if (list.length === 0) {
      selectItem(null)
      return
    }

    if (nextBefore) {
      const stillNext = list.find((x) => approvalItemKey(x) === approvalItemKey(nextBefore))
      if (stillNext) {
        selectItem(stillNext)
        return
      }
    }
    if (prevBefore) {
      const stillPrev = list.find((x) => approvalItemKey(x) === approvalItemKey(prevBefore))
      if (stillPrev) {
        selectItem(stillPrev)
        return
      }
    }

    const fallbackIdx = Math.min(Math.max(idx, 0), list.length - 1)
    selectItem(list[fallbackIdx] ?? null)
  }

  function setBizTypeFilter(v: '' | BizType) {
    bizTypeFilter.value = v
    pickSelectionAfterFilter()
  }

  function clear() {
    pendingList.value = []
    selected.value = null
    partyContext.value = null
    bizTypeFilter.value = ''
    stats.value = {
      total: 0,
      CUSTOMER: 0,
      VENDOR: 0,
      SALES_ORDER: 0,
      PURCHASE_ORDER: 0,
      FINANCE_PAYMENT: 0,
      FINANCE_RECEIPT: 0
    }
  }

  return {
    loading,
    pendingList,
    bizTypeFilter,
    selected,
    partyContext,
    stats,
    filteredList,
    selectedKey,
    selectedIndex,
    scrollToSelectedNonce,
    canPrev,
    canNext,
    selectItem,
    focusItem,
    requestScrollToSelected,
    setPartyContext,
    goPrev,
    goNext,
    pickSelectionAfterFilter,
    setBizTypeFilter,
    refreshAll,
    refreshAfterDecide,
    clear
  }
})
