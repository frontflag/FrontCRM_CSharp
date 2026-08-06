import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import {
  customerIntelApi,
  type CustomerIntelReportDetail,
  type CustomerIntelReportSummary
} from '@/api/customerIntel'
import { getApiErrorMessage } from '@/utils/apiError'

const SESSION_SELECTED_KEY = 'customer-intel-selected-id'

export type CustomerIntelCrmContext = {
  customerId?: string | null
  companyName: string
  creditCode?: string | null
  region?: string | null
  salesPersonName?: string | null
  blackList?: boolean
  disenableStatus?: boolean
}

type CustomerIntelSlot = {
  currentReport: CustomerIntelReportDetail | null
  historyReports: CustomerIntelReportSummary[]
  loadError: string
}

function emptySlot(): CustomerIntelSlot {
  return { currentReport: null, historyReports: [], loadError: '' }
}

function customerKey(customerId?: string | null): string | null {
  const id = customerId?.trim()
  return id || null
}

/**
 * 客户情报右栏状态（列表/详情「调查」页签）。
 * 按 customerId 分槽；进行中的调查不因切换选中行而取消（对齐 materialIntelLookup）。
 */
export const useCustomerIntelLookupStore = defineStore('customerIntelLookup', () => {
  const boundContext = ref<CustomerIntelCrmContext | null>(null)
  const slotByCustomerId = ref<Record<string, CustomerIntelSlot>>({})

  const investigatingCustomerIds = ref<string[]>([])
  const loadingLatestCustomerIds = ref<string[]>([])
  const investigateStartedAt = ref<Record<string, number>>({})

  const inFlightInvestigate = new Map<string, Promise<void>>()
  const inFlightLoadLatest = new Map<string, Promise<void>>()

  const boundCustomerId = computed(() => customerKey(boundContext.value?.customerId))

  function getSlot(customerId: string): CustomerIntelSlot {
    return slotByCustomerId.value[customerId] ?? emptySlot()
  }

  function patchSlot(customerId: string, patch: Partial<CustomerIntelSlot>) {
    slotByCustomerId.value = {
      ...slotByCustomerId.value,
      [customerId]: { ...getSlot(customerId), ...patch }
    }
  }

  function isCustomerInvestigating(customerId: string): boolean {
    return investigatingCustomerIds.value.includes(customerId)
  }

  function isCustomerLoadingLatest(customerId: string): boolean {
    return loadingLatestCustomerIds.value.includes(customerId)
  }

  function getInvestigateElapsedSeconds(customerId: string): number {
    const started = investigateStartedAt.value[customerId]
    if (!started) return 0
    return Math.max(0, Math.floor((Date.now() - started) / 1000))
  }

  function addInvestigating(customerId: string) {
    if (!investigatingCustomerIds.value.includes(customerId)) {
      investigatingCustomerIds.value = [...investigatingCustomerIds.value, customerId]
    }
    investigateStartedAt.value = { ...investigateStartedAt.value, [customerId]: Date.now() }
  }

  function removeInvestigating(customerId: string) {
    investigatingCustomerIds.value = investigatingCustomerIds.value.filter((id) => id !== customerId)
    const next = { ...investigateStartedAt.value }
    delete next[customerId]
    investigateStartedAt.value = next
  }

  function addLoadingLatest(customerId: string) {
    if (!loadingLatestCustomerIds.value.includes(customerId)) {
      loadingLatestCustomerIds.value = [...loadingLatestCustomerIds.value, customerId]
    }
  }

  function removeLoadingLatest(customerId: string) {
    loadingLatestCustomerIds.value = loadingLatestCustomerIds.value.filter((id) => id !== customerId)
  }

  const boundSlot = computed(() => {
    const id = boundCustomerId.value
    if (!id) return emptySlot()
    return getSlot(id)
  })

  const boundCurrentReport = computed(() => boundSlot.value.currentReport)
  const boundHistoryReports = computed(() => boundSlot.value.historyReports)
  const boundLoadError = computed(() => boundSlot.value.loadError)
  const boundInvestigating = computed(() =>
    boundCustomerId.value ? isCustomerInvestigating(boundCustomerId.value) : false
  )
  const boundLoadingLatest = computed(() =>
    boundCustomerId.value ? isCustomerLoadingLatest(boundCustomerId.value) : false
  )

  function bindContext(ctx: CustomerIntelCrmContext | null) {
    boundContext.value = ctx
    if (ctx?.customerId) {
      try {
        sessionStorage.setItem(SESSION_SELECTED_KEY, ctx.customerId)
      } catch {
        /* ignore */
      }
    }
  }

  function readSessionSelectedId(): string | null {
    try {
      return sessionStorage.getItem(SESSION_SELECTED_KEY)
    } catch {
      return null
    }
  }

  function clearBound() {
    boundContext.value = null
  }

  async function loadHistoryFor(customerId: string, take = 20): Promise<void> {
    try {
      const list = await customerIntelApi.listByCustomerId(customerId, take)
      patchSlot(customerId, { historyReports: list })
    } catch {
      patchSlot(customerId, { historyReports: [] })
    }
  }

  async function loadLatest(customerId?: string): Promise<void> {
    const id = customerKey(customerId ?? boundContext.value?.customerId)
    if (!id) return

    const existing = inFlightLoadLatest.get(id)
    if (existing) return existing

    const task = (async () => {
      addLoadingLatest(id)
      patchSlot(id, { loadError: '' })
      try {
        const report = await customerIntelApi.getLatestByCustomerId(id)
        patchSlot(id, { currentReport: report })
      } catch (e: unknown) {
        patchSlot(id, {
          currentReport: null,
          loadError: getApiErrorMessage(e, '加载调查报告失败')
        })
      } finally {
        removeLoadingLatest(id)
        inFlightLoadLatest.delete(id)
      }
    })()

    inFlightLoadLatest.set(id, task)
    return task
  }

  async function loadHistory(take = 20): Promise<void> {
    const id = boundCustomerId.value
    if (!id) return
    await loadHistoryFor(id, take)
  }

  async function selectReportById(reportId: string): Promise<void> {
    const id = boundCustomerId.value
    if (!id || !reportId.trim()) return

    addLoadingLatest(id)
    patchSlot(id, { loadError: '' })
    try {
      const report = await customerIntelApi.getById(reportId)
      patchSlot(id, { currentReport: report })
    } catch (e: unknown) {
      patchSlot(id, { loadError: getApiErrorMessage(e, '加载报告失败') })
    } finally {
      removeLoadingLatest(id)
    }
  }

  async function investigate(options?: { force?: boolean }): Promise<void> {
    const ctx = boundContext.value
    const id = customerKey(ctx?.customerId)
    const companyName = ctx?.companyName?.trim()
    if (!id || !companyName) return

    const existing = inFlightInvestigate.get(id)
    if (existing && !options?.force) return existing

    const snapshot: CustomerIntelCrmContext = { ...ctx, companyName }

    const task = (async () => {
      addInvestigating(id)
      patchSlot(id, { loadError: '' })
      try {
        const result = await customerIntelApi.investigate({
          customerId: snapshot.customerId ?? null,
          companyName: snapshot.companyName,
          creditCode: snapshot.creditCode ?? null,
          region: snapshot.region ?? null,
          forceRefresh: !!options?.force
        })
        patchSlot(id, { currentReport: result.report })
        await loadHistoryFor(id)
      } catch (e: unknown) {
        patchSlot(id, { loadError: getApiErrorMessage(e, '客户情报调查失败') })
      } finally {
        removeInvestigating(id)
        inFlightInvestigate.delete(id)
      }
    })()

    inFlightInvestigate.set(id, task)
    return task
  }

  function hasUsableReport(report: CustomerIntelReportDetail | null | undefined): boolean {
    const data = report?.report
    if (data == null || typeof data !== 'object') return false
    return Object.keys(data as object).length > 0
  }

  /** 无最新调查报告时自动发起调查（不强制刷新；失败静默） */
  async function ensureLookup(): Promise<void> {
    const id = boundCustomerId.value
    const companyName = boundContext.value?.companyName?.trim()
    if (!id || !companyName) return
    if (isCustomerInvestigating(id)) return
    try {
      await loadLatest(id)
      if (hasUsableReport(getSlot(id).currentReport)) return
      await investigate({ force: false })
    } catch {
      /* 自动调查不阻断审批桌面 */
    }
  }

  return {
    boundContext,
    boundCustomerId,
    boundCurrentReport,
    boundHistoryReports,
    boundLoadError,
    boundInvestigating,
    boundLoadingLatest,
    slotByCustomerId,
    getSlot,
    isCustomerInvestigating,
    isCustomerLoadingLatest,
    getInvestigateElapsedSeconds,
    bindContext,
    readSessionSelectedId,
    clearBound,
    loadLatest,
    loadHistory,
    selectReportById,
    investigate,
    ensureLookup
  }
})
