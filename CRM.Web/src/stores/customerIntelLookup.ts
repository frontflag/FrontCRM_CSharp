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

export const useCustomerIntelLookupStore = defineStore('customerIntelLookup', () => {
  const boundContext = ref<CustomerIntelCrmContext | null>(null)
  const currentReport = ref<CustomerIntelReportDetail | null>(null)
  const historyReports = ref<CustomerIntelReportSummary[]>([])
  const loadingLatest = ref(false)
  const investigating = ref(false)
  const loadError = ref('')
  const investigateStartedAt = ref(0)

  const boundCustomerId = computed(() => boundContext.value?.customerId?.trim() || null)

  function isInvestigating(): boolean {
    return investigating.value
  }

  function getInvestigateElapsedSeconds(): number {
    if (!investigating.value || !investigateStartedAt.value) return 0
    return Math.max(0, Math.floor((Date.now() - investigateStartedAt.value) / 1000))
  }

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
    currentReport.value = null
    historyReports.value = []
    loadError.value = ''
  }

  async function loadLatest(): Promise<void> {
    const ctx = boundContext.value
    if (!ctx?.customerId) {
      currentReport.value = null
      return
    }
    loadingLatest.value = true
    loadError.value = ''
    try {
      currentReport.value = await customerIntelApi.getLatestByCustomerId(ctx.customerId)
    } catch (e: unknown) {
      loadError.value = getApiErrorMessage(e, '加载调查报告失败')
      currentReport.value = null
    } finally {
      loadingLatest.value = false
    }
  }

  async function loadHistory(take = 20): Promise<void> {
    const ctx = boundContext.value
    if (!ctx?.customerId) {
      historyReports.value = []
      return
    }
    try {
      historyReports.value = await customerIntelApi.listByCustomerId(ctx.customerId, take)
    } catch {
      historyReports.value = []
    }
  }

  async function selectReportById(reportId: string): Promise<void> {
    if (!reportId.trim()) return
    loadingLatest.value = true
    loadError.value = ''
    try {
      currentReport.value = await customerIntelApi.getById(reportId)
    } catch (e: unknown) {
      loadError.value = getApiErrorMessage(e, '加载报告失败')
    } finally {
      loadingLatest.value = false
    }
  }

  async function investigate(options?: { force?: boolean }): Promise<void> {
    const ctx = boundContext.value
    const companyName = ctx?.companyName?.trim()
    if (!companyName) return

    investigating.value = true
    investigateStartedAt.value = Date.now()
    loadError.value = ''
    try {
      const result = await customerIntelApi.investigate({
        customerId: ctx?.customerId ?? null,
        companyName,
        creditCode: ctx?.creditCode ?? null,
        region: ctx?.region ?? null,
        forceRefresh: !!options?.force
      })
      currentReport.value = result.report
      await loadHistory()
    } catch (e: unknown) {
      loadError.value = getApiErrorMessage(e, '客户情报调查失败')
    } finally {
      investigating.value = false
      investigateStartedAt.value = 0
    }
  }

  return {
    boundContext,
    boundCustomerId,
    currentReport,
    historyReports,
    loadingLatest,
    investigating,
    loadError,
    isInvestigating,
    getInvestigateElapsedSeconds,
    bindContext,
    readSessionSelectedId,
    clearBound,
    loadLatest,
    loadHistory,
    selectReportById,
    investigate
  }
})
