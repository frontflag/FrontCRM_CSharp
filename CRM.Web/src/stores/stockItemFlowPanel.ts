import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  inventoryCenterApi,
  type StockItemFlowAggregates,
  type StockItemListRow
} from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'

export const useStockItemFlowPanelStore = defineStore('stockItemFlowPanel', () => {
  const row = ref<StockItemListRow | null>(null)
  const aggregates = ref<StockItemFlowAggregates | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  let loadSeq = 0

  function rowKey(r: StockItemListRow | Record<string, unknown> | null | undefined) {
    return String((r as StockItemListRow | null | undefined)?.stockItemId ?? '').trim()
  }

  function clear() {
    loadSeq += 1
    row.value = null
    aggregates.value = null
    loading.value = false
    loadError.value = ''
  }

  function setRowOnly(r: StockItemListRow | Record<string, unknown>) {
    const next = r as StockItemListRow
    const prevKey = rowKey(row.value)
    const nextKey = rowKey(next)
    row.value = next
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
      const data = await inventoryCenterApi.getStockItemFlowAggregates(id)
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

  async function selectRow(r: StockItemListRow | Record<string, unknown>, failMessage: string) {
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
