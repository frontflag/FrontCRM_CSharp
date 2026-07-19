import { ref } from 'vue'
import { defineStore } from 'pinia'
import { logisticsApi, type QcInfoDto, type QcOpsAggregatesDto } from '@/api/logistics'
import { documentApi, DOCUMENT_BIZ_TYPE_QC } from '@/api/document'
import { countQcImageDocuments } from '@/utils/qcImageDocument'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>
type RowHandler = (row: RowRecord) => void | Promise<void>

export const useQcOpsPanelStore = defineStore('qcOpsPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<QcOpsAggregatesDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const aggregatesRowKey = ref('')
  const actionLoading = ref(false)
  const qcImageCount = ref(0)

  let createStockInHandler: RowHandler | null = null
  let loadSeq = 0

  function rowKey(target: RowRecord) {
    return String(target.id ?? target.Id ?? '').trim()
  }

  function registerHandlers(handlers: { createStockIn?: RowHandler }) {
    if (handlers.createStockIn) createStockInHandler = handlers.createStockIn
  }

  function unregisterHandlers() {
    createStockInHandler = null
  }

  function clear() {
    row.value = null
    aggregates.value = null
    loadError.value = ''
    loading.value = false
    actionLoading.value = false
    aggregatesRowKey.value = ''
    qcImageCount.value = 0
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
      qcImageCount.value = 0
    }
  }

  async function loadAggregates(loadFailedText = '加载操作面板失败', force = false) {
    if (!row.value) return
    const id = rowKey(row.value)
    if (!id) return
    if (
      !force &&
      aggregatesRowKey.value === id &&
      aggregates.value !== null &&
      !loadError.value
    ) {
      return
    }

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''

    try {
      const [data, docs] = await Promise.all([
        logisticsApi.getQcOpsAggregates(id),
        documentApi.getDocuments(DOCUMENT_BIZ_TYPE_QC, id).catch(() => [])
      ])
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== id) return
      aggregates.value = data
      aggregatesRowKey.value = id
      qcImageCount.value = countQcImageDocuments(Array.isArray(docs) ? docs : [])
    } catch (e: unknown) {
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== id) return
      loadError.value = getApiErrorMessage(e, loadFailedText)
      aggregates.value = null
      aggregatesRowKey.value = ''
      qcImageCount.value = 0
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: RowRecord, loadFailedText = '加载操作面板失败') {
    setRowOnly(target)
    await loadAggregates(loadFailedText)
  }

  async function refreshFromListRows(rows: QcInfoDto[], loadFailedText = '加载操作面板失败') {
    if (!row.value) return
    const selectedId = rowKey(row.value)
    const updated = rows.find(r => rowKey(r as unknown as RowRecord) === selectedId)
    if (!updated) return
    setRowOnly(updated as unknown as RowRecord)
    await loadAggregates(loadFailedText, true)
  }

  async function runCreateStockIn() {
    if (!row.value || !createStockInHandler) return
    actionLoading.value = true
    try {
      await createStockInHandler(row.value)
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
    qcImageCount,
    registerHandlers,
    unregisterHandlers,
    clear,
    setRowOnly,
    loadAggregates,
    selectRow,
    refreshFromListRows,
    runCreateStockIn,
    rowKey
  }
})
