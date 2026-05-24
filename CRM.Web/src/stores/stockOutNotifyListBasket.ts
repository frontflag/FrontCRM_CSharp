import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { StockOutRequestDto } from '@/api/stockOut'

function rowId(row: StockOutRequestDto): string {
  return String(row?.id || '').trim()
}

/** 出库通知列表「复选篮子」：跨分页保留勾选，供生成装箱单等批量操作。 */
export const useStockOutNotifyListBasketStore = defineStore('stockOutNotifyListBasket', () => {
  const itemsById = ref<Record<string, StockOutRequestDto>>({})
  const idOrder = ref<string[]>([])

  const count = computed(() => idOrder.value.length)
  const items = computed(
    () => idOrder.value.map((id) => itemsById.value[id]).filter(Boolean) as StockOutRequestDto[]
  )

  function upsert(row: StockOutRequestDto) {
    const id = rowId(row)
    if (!id) return
    if (!itemsById.value[id]) {
      idOrder.value = [...idOrder.value, id]
    }
    itemsById.value = { ...itemsById.value, [id]: { ...row } }
  }

  function remove(id: string) {
    if (!id || !itemsById.value[id]) return
    const next = { ...itemsById.value }
    delete next[id]
    itemsById.value = next
    idOrder.value = idOrder.value.filter((x) => x !== id)
  }

  function clear() {
    itemsById.value = {}
    idOrder.value = []
  }

  function has(id: string) {
    return !!(id && itemsById.value[id])
  }

  function mergePageSelection(allPageRows: StockOutRequestDto[], selectedOnPage: StockOutRequestDto[]) {
    const pageIds = new Set(allPageRows.map((r) => rowId(r)).filter(Boolean))
    const selectedIds = new Set(selectedOnPage.map((r) => rowId(r)).filter(Boolean))
    for (const id of pageIds) {
      if (selectedIds.has(id)) {
        const row = allPageRows.find((r) => rowId(r) === id)
        if (row) upsert(row)
      } else {
        remove(id)
      }
    }
  }

  return {
    itemsById,
    idOrder,
    count,
    items,
    upsert,
    remove,
    clear,
    has,
    mergePageSelection
  }
})
