import { ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchCustomsDeclarationById, type CustomsDeclarationDetailDto } from '@/api/customs'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>
type RowHandler = (row: RowRecord) => void

export const useCustomsDeclarationOpsPanelStore = defineStore('customsDeclarationOpsPanel', () => {
  const row = ref<RowRecord | null>(null)
  const detail = ref<CustomsDeclarationDetailDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const detailRowKey = ref('')
  const actionLoading = ref(false)

  let setClearanceHandler: RowHandler | null = null
  let createArrivalHandler: RowHandler | null = null
  let loadSeq = 0

  function rowKey(target: RowRecord) {
    return String(target.id ?? target.Id ?? '').trim()
  }

  function registerHandlers(handlers: { setClearance?: RowHandler; createArrival?: RowHandler }) {
    if (handlers.setClearance) setClearanceHandler = handlers.setClearance
    if (handlers.createArrival) createArrivalHandler = handlers.createArrival
  }

  function unregisterHandlers() {
    setClearanceHandler = null
    createArrivalHandler = null
  }

  function clear() {
    row.value = null
    detail.value = null
    loadError.value = ''
    loading.value = false
    actionLoading.value = false
    detailRowKey.value = ''
    loadSeq += 1
  }

  function setRowOnly(target: RowRecord) {
    const key = rowKey(target)
    if (!key) return
    row.value = target
    if (detailRowKey.value !== key) {
      detail.value = null
      loadError.value = ''
      loading.value = false
    }
  }

  async function loadDetail(loadFailedText = '加载报关单失败') {
    if (!row.value) return
    const id = rowKey(row.value)
    if (!id) return
    if (detailRowKey.value === id && detail.value !== null && !loadError.value) return

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''

    try {
      const data = await fetchCustomsDeclarationById(id)
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== id) return
      detail.value = data
      detailRowKey.value = id
    } catch (e: unknown) {
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== id) return
      loadError.value = getApiErrorMessage(e, loadFailedText)
      detail.value = null
      detailRowKey.value = ''
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: RowRecord, loadFailedText = '加载报关单失败') {
    setRowOnly(target)
    await loadDetail(loadFailedText)
  }

  async function refreshFromListRows(rows: RowRecord[], loadFailedText = '加载报关单失败', fetchDetail = true) {
    if (!row.value) return
    const selectedId = rowKey(row.value)
    const updated = rows.find((r) => rowKey(r) === selectedId)
    if (!updated) return
    setRowOnly(updated)
    if (fetchDetail) await loadDetail(loadFailedText)
  }

  function runSetClearance() {
    if (row.value && setClearanceHandler) setClearanceHandler(row.value)
  }

  function runCreateArrival() {
    if (row.value && createArrivalHandler) createArrivalHandler(row.value)
  }

  return {
    row,
    detail,
    loading,
    loadError,
    actionLoading,
    registerHandlers,
    unregisterHandlers,
    clear,
    setRowOnly,
    loadDetail,
    selectRow,
    refreshFromListRows,
    runSetClearance,
    runCreateArrival,
    rowKey
  }
})
