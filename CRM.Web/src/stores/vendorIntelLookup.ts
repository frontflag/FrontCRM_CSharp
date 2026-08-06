import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import {
  vendorIntelApi,
  type VendorIntelReportDetail,
  type VendorIntelReportSummary
} from '@/api/vendorIntel'
import { getApiErrorMessage } from '@/utils/apiError'

const SESSION_SELECTED_KEY = 'vendor-intel-selected-id'

export type VendorIntelCrmContext = {
  vendorId?: string | null
  companyName: string
  creditCode?: string | null
  region?: string | null
  purchaserName?: string | null
  blackList?: boolean
  isDisenable?: boolean
}

type VendorIntelSlot = {
  currentReport: VendorIntelReportDetail | null
  historyReports: VendorIntelReportSummary[]
  loadError: string
}

function emptySlot(): VendorIntelSlot {
  return { currentReport: null, historyReports: [], loadError: '' }
}

function vendorKey(vendorId?: string | null): string | null {
  const id = vendorId?.trim()
  return id || null
}

/**
 * 供应商情报右栏状态（列表/详情「调查」页签）。
 * 按 vendorId 分槽；进行中的调查不因切换选中行而取消（对齐 materialIntelLookup）。
 */
export const useVendorIntelLookupStore = defineStore('vendorIntelLookup', () => {
  const boundContext = ref<VendorIntelCrmContext | null>(null)
  const slotByVendorId = ref<Record<string, VendorIntelSlot>>({})

  const investigatingVendorIds = ref<string[]>([])
  const loadingLatestVendorIds = ref<string[]>([])
  const investigateStartedAt = ref<Record<string, number>>({})

  const inFlightInvestigate = new Map<string, Promise<void>>()
  const inFlightLoadLatest = new Map<string, Promise<void>>()

  const boundVendorId = computed(() => vendorKey(boundContext.value?.vendorId))

  function getSlot(vendorId: string): VendorIntelSlot {
    return slotByVendorId.value[vendorId] ?? emptySlot()
  }

  function patchSlot(vendorId: string, patch: Partial<VendorIntelSlot>) {
    slotByVendorId.value = {
      ...slotByVendorId.value,
      [vendorId]: { ...getSlot(vendorId), ...patch }
    }
  }

  function isVendorInvestigating(vendorId: string): boolean {
    return investigatingVendorIds.value.includes(vendorId)
  }

  function isVendorLoadingLatest(vendorId: string): boolean {
    return loadingLatestVendorIds.value.includes(vendorId)
  }

  function getInvestigateElapsedSeconds(vendorId: string): number {
    const started = investigateStartedAt.value[vendorId]
    if (!started) return 0
    return Math.max(0, Math.floor((Date.now() - started) / 1000))
  }

  function addInvestigating(vendorId: string) {
    if (!investigatingVendorIds.value.includes(vendorId)) {
      investigatingVendorIds.value = [...investigatingVendorIds.value, vendorId]
    }
    investigateStartedAt.value = { ...investigateStartedAt.value, [vendorId]: Date.now() }
  }

  function removeInvestigating(vendorId: string) {
    investigatingVendorIds.value = investigatingVendorIds.value.filter((id) => id !== vendorId)
    const next = { ...investigateStartedAt.value }
    delete next[vendorId]
    investigateStartedAt.value = next
  }

  function addLoadingLatest(vendorId: string) {
    if (!loadingLatestVendorIds.value.includes(vendorId)) {
      loadingLatestVendorIds.value = [...loadingLatestVendorIds.value, vendorId]
    }
  }

  function removeLoadingLatest(vendorId: string) {
    loadingLatestVendorIds.value = loadingLatestVendorIds.value.filter((id) => id !== vendorId)
  }

  /** 当前绑定供应商的展示状态（右栏 UI 用） */
  const boundSlot = computed(() => {
    const id = boundVendorId.value
    if (!id) return emptySlot()
    return getSlot(id)
  })

  const boundCurrentReport = computed(() => boundSlot.value.currentReport)
  const boundHistoryReports = computed(() => boundSlot.value.historyReports)
  const boundLoadError = computed(() => boundSlot.value.loadError)
  const boundInvestigating = computed(() =>
    boundVendorId.value ? isVendorInvestigating(boundVendorId.value) : false
  )
  const boundLoadingLatest = computed(() =>
    boundVendorId.value ? isVendorLoadingLatest(boundVendorId.value) : false
  )

  function bindContext(ctx: VendorIntelCrmContext | null) {
    boundContext.value = ctx
    if (ctx?.vendorId) {
      try {
        sessionStorage.setItem(SESSION_SELECTED_KEY, ctx.vendorId)
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

  async function loadHistoryFor(vendorId: string, take = 20): Promise<void> {
    try {
      const list = await vendorIntelApi.listByVendorId(vendorId, take)
      patchSlot(vendorId, { historyReports: list })
    } catch {
      patchSlot(vendorId, { historyReports: [] })
    }
  }

  async function loadLatest(vendorId?: string): Promise<void> {
    const id = vendorKey(vendorId ?? boundContext.value?.vendorId)
    if (!id) return

    const existing = inFlightLoadLatest.get(id)
    if (existing) return existing

    const task = (async () => {
      addLoadingLatest(id)
      patchSlot(id, { loadError: '' })
      try {
        const report = await vendorIntelApi.getLatestByVendorId(id)
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
    const id = boundVendorId.value
    if (!id) return
    await loadHistoryFor(id, take)
  }

  async function selectReportById(reportId: string): Promise<void> {
    const id = boundVendorId.value
    if (!id || !reportId.trim()) return

    addLoadingLatest(id)
    patchSlot(id, { loadError: '' })
    try {
      const report = await vendorIntelApi.getById(reportId)
      patchSlot(id, { currentReport: report })
    } catch (e: unknown) {
      patchSlot(id, { loadError: getApiErrorMessage(e, '加载报告失败') })
    } finally {
      removeLoadingLatest(id)
    }
  }

  async function investigate(options?: { force?: boolean }): Promise<void> {
    const ctx = boundContext.value
    const id = vendorKey(ctx?.vendorId)
    const companyName = ctx?.companyName?.trim()
    if (!id || !companyName) return

    const existing = inFlightInvestigate.get(id)
    if (existing && !options?.force) return existing

    const snapshot: VendorIntelCrmContext = { ...ctx, companyName }

    const task = (async () => {
      addInvestigating(id)
      patchSlot(id, { loadError: '' })
      try {
        const result = await vendorIntelApi.investigate({
          vendorId: snapshot.vendorId ?? null,
          companyName: snapshot.companyName,
          creditCode: snapshot.creditCode ?? null,
          region: snapshot.region ?? null,
          forceRefresh: !!options?.force
        })
        patchSlot(id, { currentReport: result.report })
        await loadHistoryFor(id)
      } catch (e: unknown) {
        patchSlot(id, { loadError: getApiErrorMessage(e, '供应商情报调查失败') })
      } finally {
        removeInvestigating(id)
        inFlightInvestigate.delete(id)
      }
    })()

    inFlightInvestigate.set(id, task)
    return task
  }

  function hasUsableReport(report: VendorIntelReportDetail | null | undefined): boolean {
    const data = report?.report
    if (data == null || typeof data !== 'object') return false
    return Object.keys(data as object).length > 0
  }

  /** 无最新调查报告时自动发起调查（不强制刷新；失败静默） */
  async function ensureLookup(): Promise<void> {
    const id = boundVendorId.value
    const companyName = boundContext.value?.companyName?.trim()
    if (!id || !companyName) return
    if (isVendorInvestigating(id)) return
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
    boundVendorId,
    boundCurrentReport,
    boundHistoryReports,
    boundLoadError,
    boundInvestigating,
    boundLoadingLatest,
    slotByVendorId,
    getSlot,
    isVendorInvestigating,
    isVendorLoadingLatest,
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
