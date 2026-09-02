import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  fetchCustomsDeclarationFlowAggregates,
  type CustomsDeclarationFlowAggregatesDto
} from '@/api/customs'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>

export const useCustomsDeclarationFlowPanelStore = defineStore('customsDeclarationFlowPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<CustomsDeclarationFlowAggregatesDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  let loadSeq = 0

  function rowKey(r: RowRecord | null | undefined) {
    return String(r?.id ?? r?.Id ?? '').trim()
  }

  function clear() {
    loadSeq += 1
    row.value = null
    aggregates.value = null
    loading.value = false
    loadError.value = ''
  }

  function setRowOnly(r: RowRecord) {
    const nextKey = rowKey(r)
    const prevKey = rowKey(row.value)
    row.value = r
    if (prevKey !== nextKey) {
      aggregates.value = null
      loadError.value = ''
    }
  }

  async function loadSelected(failMessage: string) {
    const id = rowKey(row.value)
    if (!id) return
    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''
    try {
      const data = await fetchCustomsDeclarationFlowAggregates(id)
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

  async function selectRow(r: RowRecord, failMessage: string) {
    setRowOnly(r)
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
    loadSelected
  }
})
