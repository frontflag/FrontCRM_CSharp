import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { PackingListItem } from '@/api/packing'
import { PackingStatusCode } from '@/api/packing'

function rowId(row: PackingListItem): string {
  return String(row?.id || '').trim()
}

/** 装箱单列表复选篮子：跨分页保留，供批量「出库」等操作。 */
export const usePackingListBasketStore = defineStore('packingListBasket', () => {
  const itemsById = ref<Record<string, PackingListItem>>({})
  const idOrder = ref<string[]>([])

  const count = computed(() => idOrder.value.length)
  const items = computed(
    () => idOrder.value.map((id) => itemsById.value[id]).filter(Boolean) as PackingListItem[]
  )

  function upsert(row: PackingListItem) {
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

  function mergePageSelection(allPageRows: PackingListItem[], selectedOnPage: PackingListItem[]) {
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

/** 列表行是否允许勾选批量「出库」（仅「已备货」）。 */
export function isPackingEligibleForStockOut(row: PackingListItem): boolean {
  return Number(row?.status) === PackingStatusCode.Ready
}
