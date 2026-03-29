import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { RFQItem } from '@/types/rfq'

/**
 * 需求明细列表「复选篮子」：跨分页保留勾选，供批量操作。
 * 合并规则：仅根据「当前页」勾选变化增删篮子中的 id，其它页已选记录不受影响。
 */
export const useRfqItemListBasketStore = defineStore('rfqItemListBasket', () => {
  const itemsById = ref<Record<string, RFQItem>>({})
  const idOrder = ref<string[]>([])

  const count = computed(() => idOrder.value.length)
  const items = computed(() => idOrder.value.map((id) => itemsById.value[id]).filter(Boolean) as RFQItem[])

  function upsert(row: RFQItem) {
    if (!row?.id) return
    if (!itemsById.value[row.id]) {
      idOrder.value = [...idOrder.value, row.id]
    }
    itemsById.value = { ...itemsById.value, [row.id]: { ...row } }
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

  /** 根据当前页全量行 + 本页勾选结果，更新篮子中属于本页的条目 */
  function mergePageSelection(allPageRows: RFQItem[], selectedOnPage: RFQItem[]) {
    const pageIds = new Set(allPageRows.map((r) => r.id).filter(Boolean) as string[])
    const selectedIds = new Set(selectedOnPage.map((r) => r.id).filter(Boolean) as string[])
    for (const id of pageIds) {
      if (selectedIds.has(id)) {
        const row = allPageRows.find((r) => r.id === id)
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
