import { ref } from 'vue'
import { defineStore } from 'pinia'
import {
  logisticsApi,
  normalizeStockInNotifyRow,
  type ArrivalNoticeOpsAggregatesDto,
  type StockInNotifyDto
} from '@/api/logistics'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>
type RowHandler = (row: RowRecord) => void | Promise<void>

export const useArrivalNoticeOpsPanelStore = defineStore('arrivalNoticeOpsPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<ArrivalNoticeOpsAggregatesDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const aggregatesRowKey = ref('')
  const actionLoading = ref(false)

  let confirmArrivedHandler: RowHandler | null = null
  let loadSeq = 0

  function rowKey(target: RowRecord) {
    return String(target.id ?? target.Id ?? '').trim()
  }

  function registerHandlers(handlers: { confirmArrived?: RowHandler }) {
    if (handlers.confirmArrived) confirmArrivedHandler = handlers.confirmArrived
  }

  function unregisterHandlers() {
    confirmArrivedHandler = null
  }

  function clear() {
    row.value = null
    aggregates.value = null
    loadError.value = ''
    loading.value = false
    actionLoading.value = false
    aggregatesRowKey.value = ''
    loadSeq += 1
  }

  function setRowOnly(target: RowRecord) {
    const key = rowKey(target)
    if (!key) return
    row.value = target
    if (aggregatesRowKey.value !== key) {
      aggregates.value = null
      loadError.value = ''
      loading.value = false
    }
  }

  async function loadAggregates(loadFailedText = '加载操作面板失败') {
    if (!row.value) return
    const id = rowKey(row.value)
    if (!id) return
    if (aggregatesRowKey.value === id && aggregates.value !== null && !loadError.value) return

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''

    try {
      const data = await logisticsApi.getArrivalNoticeOpsAggregates(id)
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== id) return
      aggregates.value = data
      aggregatesRowKey.value = id
    } catch (e: unknown) {
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== id) return
      loadError.value = getApiErrorMessage(e, loadFailedText)
      aggregates.value = null
      aggregatesRowKey.value = ''
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: RowRecord, loadFailedText = '加载操作面板失败') {
    setRowOnly(target)
    await loadAggregates(loadFailedText)
  }

  async function refreshFromListRows(rows: StockInNotifyDto[], loadFailedText = '加载操作面板失败') {
    if (!row.value) return
    const selectedId = rowKey(row.value)
    const updated = rows.find((r) => rowKey(r as unknown as RowRecord) === selectedId)
    if (!updated) return
    setRowOnly(updated as unknown as RowRecord)
    await loadAggregates(loadFailedText)
  }

  function syncNoticeRow(notice: StockInNotifyDto) {
    row.value = normalizeStockInNotifyRow(notice) as unknown as RowRecord
  }

  async function runConfirmArrived() {
    if (!row.value || !confirmArrivedHandler) return
    actionLoading.value = true
    try {
      await confirmArrivedHandler(row.value)
    } finally {
      actionLoading.value = false
    }
  }

  return {
    row,
    aggregates,
    loading,
    loadError,
    actionLoading,
    registerHandlers,
    unregisterHandlers,
    clear,
    setRowOnly,
    loadAggregates,
    selectRow,
    refreshFromListRows,
    syncNoticeRow,
    runConfirmArrived,
    rowKey
  }
})
