import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { approvalsApi, type BizType, type PendingApprovalItem } from '@/api/approvals'

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
  'FINANCE_PAYMENT',
  'FINANCE_RECEIPT'
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

  function selectItem(item: PendingApprovalItem | null) {
    const prevKey = selected.value ? approvalItemKey(selected.value) : ''
    const nextKey = item ? approvalItemKey(item) : ''
    selected.value = item
    if (prevKey !== nextKey) {
      partyContext.value = null
    }
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

  async function loadGlobalStats() {
    const [totalRes, ...typeRes] = await Promise.all([
      approvalsApi.getApprovalSummary({}),
      ...BIZ_TYPES.map((bizType) => approvalsApi.getApprovalSummary({ bizType }))
    ])

    const next = {
      total: Number(totalRes?.pendingCount ?? 0),
      CUSTOMER: 0,
      VENDOR: 0,
      SALES_ORDER: 0,
      PURCHASE_ORDER: 0,
      FINANCE_PAYMENT: 0,
      FINANCE_RECEIPT: 0
    }
    BIZ_TYPES.forEach((bt, i) => {
      next[bt] = Number(typeRes[i]?.pendingCount ?? 0)
    })
    stats.value = next
  }

  async function refreshAll() {
    loading.value = true
    try {
      await Promise.all([loadPendingQueue(), loadGlobalStats()])
    } finally {
      loading.value = false
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
    canPrev,
    canNext,
    selectItem,
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
