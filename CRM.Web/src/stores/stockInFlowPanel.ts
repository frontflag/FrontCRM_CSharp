import { defineStore } from 'pinia'
import { ref } from 'vue'
import { stockInApi, type StockInFlowAggregatesDto, type StockInListItemDto } from '@/api/stockIn'
import { getApiErrorMessage } from '@/utils/apiError'
type RowRecord = Record<string, unknown>

export const useStockInFlowPanelStore = defineStore('stockInFlowPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<StockInFlowAggregatesDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  let loadSeq = 0

  function rowKey(target: RowRecord) {
    return String(target.id ?? target.Id ?? '').trim()
  }

  function clear() {
    loadSeq += 1
    row.value = null
    aggregates.value = null
    loading.value = false
    loadError.value = ''
  }

  function setRowOnly(target: RowRecord) {
    const key = rowKey(target)
    if (!key) return
    const prev = row.value ? rowKey(row.value) : ''
    row.value = target
    if (prev !== key) {
      aggregates.value = null
      loadError.value = ''
    }
  }

  async function loadSelected(failMessage: string) {
    if (!row.value) return
    const id = rowKey(row.value)
    if (!id) return
    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''
    try {
      const data = await stockInApi.getFlowAggregates(id)
      if (seq !== loadSeq) return
      aggregates.value = data
    } catch (e: unknown) {
      if (seq !== loadSeq) return
      aggregates.value = null
      loadError.value = getApiErrorMessage(e, failMessage)
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: RowRecord, failMessage: string) {
    setRowOnly(target)
    await loadSelected(failMessage)
  }

  async function refreshFromListRows(rows: StockInListItemDto[], failMessage: string) {
    if (!row.value) return
    const selectedId = rowKey(row.value)
    const updated = rows.find(r => rowKey(r as unknown as RowRecord) === selectedId)
    if (!updated) return
    setRowOnly(updated as unknown as RowRecord)
    await loadSelected(failMessage)
  }

  return {
    row,
    aggregates,
    loading,
    loadError,
    rowKey,
    clear,
    setRowOnly,
    selectRow,
    loadSelected,
    refreshFromListRows
  }
})
