import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  fetchCustomsDeclarationFlowAggregates,
  fetchCustomsDeclarationItemFlowAggregates,
  type CustomsDeclarationFlowAggregatesDto
} from '@/api/customs'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>

export const useCustomsDeclarationFlowPanelStore = defineStore('customsDeclarationFlowPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<CustomsDeclarationFlowAggregatesDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const source = ref<'declaration' | 'item'>('declaration')
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
    source.value = 'declaration'
  }

  function setRowOnly(r: RowRecord, nextSource: 'declaration' | 'item' = 'declaration') {
    const nextKey = rowKey(r)
    const prevKey = rowKey(row.value)
    row.value = r
    if (prevKey !== nextKey || source.value !== nextSource) {
      aggregates.value = null
      loadError.value = ''
    }
    source.value = nextSource
  }

  async function loadSelected(failMessage: string) {
    const id = rowKey(row.value)
    if (!id) return
    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''
    try {
      const data =
        source.value === 'item'
          ? await fetchCustomsDeclarationItemFlowAggregates(id)
          : await fetchCustomsDeclarationFlowAggregates(id)
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

  async function selectRow(
    r: RowRecord,
    failMessage: string,
    nextSource: 'declaration' | 'item' = 'declaration'
  ) {
    setRowOnly(r, nextSource)
    await loadSelected(failMessage)
  }

  return {
    row,
    source,
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
