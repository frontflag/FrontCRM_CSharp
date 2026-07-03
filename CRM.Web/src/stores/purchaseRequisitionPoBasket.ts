import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { PurchaseRequisitionBasketItem } from '@/types/purchaseRequisitionBasket'
import { isPrBasketEligibleStatus, normalizePrListRowToBasketItem } from '@/utils/purchaseRequisitionBatchPo'

/**
 * 采购申请「待生成采购单」篮子：跨分页、列表与详情共用。
 */
export const usePurchaseRequisitionPoBasketStore = defineStore('purchaseRequisitionPoBasket', () => {
  const itemsById = ref<Record<string, PurchaseRequisitionBasketItem>>({})
  const idOrder = ref<string[]>([])

  const count = computed(() => idOrder.value.length)
  const items = computed(() =>
    idOrder.value.map((id) => itemsById.value[id]).filter(Boolean) as PurchaseRequisitionBasketItem[]
  )

  function upsert(row: PurchaseRequisitionBasketItem) {
    if (!row?.id) return false
    if (!isPrBasketEligibleStatus(row.status)) return false
    if (!itemsById.value[row.id]) {
      idOrder.value = [...idOrder.value, row.id]
    }
    itemsById.value = { ...itemsById.value, [row.id]: { ...row } }
    return true
  }

  function upsertFromListRow(row: Record<string, unknown>): PurchaseRequisitionBasketItem | null {
    const item = normalizePrListRowToBasketItem(row)
    if (!item) return null
    if (!upsert(item)) return null
    return item
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

  function mergePageSelection(allPageRows: Record<string, unknown>[], selectedOnPage: Record<string, unknown>[]) {
    const pageIds = new Set(
      allPageRows.map((r) => String(r.id ?? r.Id ?? '').trim()).filter(Boolean)
    )
    const selectedIds = new Set(
      selectedOnPage.map((r) => String(r.id ?? r.Id ?? '').trim()).filter(Boolean)
    )
    for (const id of pageIds) {
      if (selectedIds.has(id)) {
        const row = allPageRows.find((r) => String(r.id ?? r.Id ?? '').trim() === id)
        if (row) upsertFromListRow(row)
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
    upsertFromListRow,
    remove,
    clear,
    has,
    mergePageSelection
  }
})
