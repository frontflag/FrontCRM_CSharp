import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

/** 销售订单明细行（列表 API 行），主键为 sellOrderItemId */
export type SalesOrderItemLineRow = Record<string, unknown> & { sellOrderItemId?: string }

function rowId(row: SalesOrderItemLineRow): string {
  return String(row?.sellOrderItemId ?? '').trim()
}

/**
 * 销售订单明细列表「复选篮子」：跨分页保留勾选，供批量申请采购等。
 * 合并规则：仅根据「当前页」勾选变化增删篮子中的 id，其它页已选记录不受影响。
 */
export const useSalesOrderItemListBasketStore = defineStore('salesOrderItemListBasket', () => {
  const itemsById = ref<Record<string, SalesOrderItemLineRow>>({})
  const idOrder = ref<string[]>([])

  const count = computed(() => idOrder.value.length)
  const items = computed(() => idOrder.value.map((id) => itemsById.value[id]).filter(Boolean) as SalesOrderItemLineRow[])

  function upsert(row: SalesOrderItemLineRow) {
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
  function mergePageSelection(allPageRows: SalesOrderItemLineRow[], selectedOnPage: SalesOrderItemLineRow[]) {
    const pageIds = new Set(
      allPageRows.map((r) => rowId(r)).filter((x) => x !== '')
    )
    const selectedIds = new Set(
      selectedOnPage.map((r) => rowId(r)).filter((x) => x !== '')
    )
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
