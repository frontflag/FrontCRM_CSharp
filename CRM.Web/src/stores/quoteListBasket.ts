import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export type QuoteBasketRow = Record<string, unknown>

function rowId(row: QuoteBasketRow): string {
  const id = row.id ?? row.Id
  return id != null ? String(id).trim() : ''
}

/**
 * 报价列表「复选篮子」：跨分页保留勾选，供生成销售订单等批量操作。
 */
export const useQuoteListBasketStore = defineStore('quoteListBasket', () => {
  const itemsById = ref<Record<string, QuoteBasketRow>>({})
  const idOrder = ref<string[]>([])

  const count = computed(() => idOrder.value.length)
  const items = computed(() => idOrder.value.map((id) => itemsById.value[id]).filter(Boolean) as QuoteBasketRow[])

  function upsert(row: QuoteBasketRow) {
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

  /** 根据当前页全量行 + 本页勾选结果，更新篮子中属于本页的条目 */
  function mergePageSelection(allPageRows: QuoteBasketRow[], selectedOnPage: QuoteBasketRow[]) {
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
