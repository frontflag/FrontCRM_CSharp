import { ref } from 'vue'
import { defineStore } from 'pinia'

type RowRecord = Record<string, unknown>

export const usePurchaseOrderOpsPanelStore = defineStore('purchaseOrderOpsPanel', () => {
  const row = ref<RowRecord | null>(null)

  function rowKey(target: RowRecord) {
    return String(target.id ?? target.Id ?? '').trim()
  }

  function clear() {
    row.value = null
  }

  function setRowOnly(target: RowRecord) {
    if (!rowKey(target)) return
    row.value = target
  }

  async function selectRow(target: RowRecord) {
    setRowOnly(target)
  }

  return {
    row,
    rowKey,
    clear,
    setRowOnly,
    selectRow
  }
})
