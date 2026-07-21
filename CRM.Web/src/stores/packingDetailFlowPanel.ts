import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import salesOrderApi, { type SalesOrderDetailTabAggregates } from '@/api/salesOrder'
import {
  packingApi,
  type PackingDetail,
  type PackingDetailLine,
  type PackingStockOutNotifyRow
} from '@/api/packing'
import {
  inventoryCenterApi,
  type PickPageByPacking,
  type PickPagePackingLine,
  type PickingTask
} from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import type { PackingFlowExtras } from '@/utils/packingItemFlowPanel'

type RowRecord = Record<string, unknown>

export type PackingFlowItemChip = {
  id: string
  itemCode: string
}

/** 装箱「流程」：详情选明细 / 列表选箱+明细单号切换 */
export const usePackingDetailFlowPanelStore = defineStore('packingDetailFlowPanel', () => {
  const packing = ref<PackingDetail | null>(null)
  const packingItem = ref<PackingDetailLine | null>(null)
  const pickPage = ref<PickPageByPacking | null>(null)
  /** 供流程适配器使用的行形态 */
  const flowRow = ref<RowRecord | null>(null)
  const flowExtras = ref<PackingFlowExtras | null>(null)
  const aggregates = ref<SalesOrderDetailTabAggregates | null>(null)
  const loading = ref(false)
  /** 列表单击装箱单后拉取详情/拣货 */
  const bindingLoading = ref(false)
  const bindError = ref('')
  const loadError = ref('')
  /** 明细未关联销售行：仍展示本箱流程站，不阻断面板 */
  const missingSellLink = ref(false)
  const aggregatesKey = ref('')

  let loadSeq = 0
  let bindSeq = 0

  const selectedPackingId = computed(() => String(packing.value?.id ?? '').trim())
  const selectedPackingItemId = computed(() => String(packingItem.value?.id ?? '').trim())

  const itemChips = computed<PackingFlowItemChip[]>(() => {
    const items = packing.value?.items ?? []
    return items.map((line, idx) => {
      const id = String(line.id || '').trim()
      const code = String(line.itemCode || '').trim()
      return {
        id,
        itemCode: code || (id ? `LINE-${idx + 1}` : `—-${idx + 1}`)
      }
    }).filter((x) => x.id)
  })

  function clear() {
    packing.value = null
    packingItem.value = null
    pickPage.value = null
    flowRow.value = null
    flowExtras.value = null
    aggregates.value = null
    loadError.value = ''
    bindError.value = ''
    missingSellLink.value = false
    loading.value = false
    bindingLoading.value = false
    aggregatesKey.value = ''
    loadSeq += 1
    bindSeq += 1
  }

  function resolveSelectedNotifies(
    header: PackingDetail,
    line: PackingDetailLine
  ): PackingStockOutNotifyRow[] {
    const all = header.stockOutNotifies ?? []
    const notifyId = line.stockOutNotifyId?.trim()
    if (notifyId) {
      const byId = all.find((n) => n.id === notifyId)
      return byId ? [byId] : []
    }
    const sellItemId = line.sellOrderItemId?.trim()
    if (sellItemId) {
      return all.filter((n) => (n.salesOrderItemId?.trim() || '') === sellItemId)
    }
    return []
  }

  function buildFlowExtras(
    header: PackingDetail,
    line: PackingDetailLine,
    page?: PickPageByPacking | null
  ): PackingFlowExtras {
    const itemId = String(line.id || '').trim()
    const pickLine: PickPagePackingLine | null =
      page?.lines?.find((l) => String(l.packingItemId || '').trim() === itemId) ?? null
    const pickingTask: PickingTask | null = page?.pickingTask ?? null
    return {
      stockOutNotifies: resolveSelectedNotifies(header, line),
      pickingTask,
      pickLine
    }
  }

  function buildFlowRow(header: PackingDetail, line: PackingDetailLine): RowRecord {
    const ext = (header.itemExtends ?? []).find(
      (e) => String(e.packingItemId || '').trim() === String(line.id || '').trim()
    )
    return {
      id: line.sellOrderItemId,
      sellOrderItemId: line.sellOrderItemId,
      sellOrderId: line.sellOrderId,
      sellOrderItemCode: line.sellOrderItemCode || line.itemCode,
      sellOrderCode: line.sellOrderCode || ext?.sellOrderCode,
      customerName: header.customerName ?? ext?.customerName,
      customerCode: null,
      salesUserName: header.salesUserName ?? ext?.salesUserName,
      qty: line.qty,
      price: line.price ?? ext?.price,
      currency: line.priceCurrency ?? ext?.priceCurrency,
      createTime: header.createTime,
      orderCreateTime: header.createTime,
      packingId: header.id,
      packingCode: header.code,
      packingStatus: header.status,
      packingItemId: line.id,
      packingItemQty: line.qty,
      stockOutNotifyId: line.stockOutNotifyId,
      createUserName: header.createUserName
    }
  }

  function setSelection(
    header: PackingDetail | null,
    line: PackingDetailLine | null,
    page?: PickPageByPacking | null
  ) {
    packing.value = header
    packingItem.value = line
    if (page !== undefined) pickPage.value = page
    if (!header || !line) {
      flowRow.value = null
      flowExtras.value = null
      aggregates.value = null
      aggregatesKey.value = ''
      loadError.value = ''
      missingSellLink.value = false
      loading.value = false
      return
    }
    flowRow.value = buildFlowRow(header, line)
    flowExtras.value = buildFlowExtras(header, line, pickPage.value)
    const key = `${header.id}|${line.id}|${line.sellOrderItemId || ''}`
    if (aggregatesKey.value !== key) {
      aggregates.value = null
      loadError.value = ''
      missingSellLink.value = false
    }
  }

  async function loadAggregates(loadFailedText = '加载流程失败') {
    const header = packing.value
    const line = packingItem.value
    if (!header || !line) return

    const sellOrderId = String(line.sellOrderId ?? '').trim()
    const sellOrderItemId = String(line.sellOrderItemId ?? '').trim()
    const key = `${header.id}|${line.id}|${sellOrderItemId}`

    if (!sellOrderId || !sellOrderItemId) {
      aggregates.value = null
      aggregatesKey.value = key
      loadError.value = ''
      missingSellLink.value = true
      loading.value = false
      return
    }

    if (aggregatesKey.value === key && aggregates.value !== null && !loadError.value && !missingSellLink.value)
      return

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''
    missingSellLink.value = false

    try {
      const data = await salesOrderApi.getSellOrderItemDetailTabAggregates(sellOrderId, sellOrderItemId)
      if (seq !== loadSeq) return
      aggregates.value = data
      aggregatesKey.value = key
    } catch (e: unknown) {
      if (seq !== loadSeq) return
      loadError.value = getApiErrorMessage(e, loadFailedText)
      aggregates.value = null
      aggregatesKey.value = ''
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectPackingItem(
    header: PackingDetail,
    line: PackingDetailLine,
    page?: PickPageByPacking | null
  ) {
    setSelection(header, line, page)
    await loadAggregates()
  }

  /** 详情页：拣货晚到时刷新 extras */
  function patchPickPage(page: PickPageByPacking | null) {
    pickPage.value = page
    const header = packing.value
    const line = packingItem.value
    if (!header || !line) {
      flowExtras.value = null
      return
    }
    flowExtras.value = buildFlowExtras(header, line, page)
  }

  /** 列表：单击装箱单 → 拉详情+拣货，默认首条明细 */
  async function bindPackingFromList(packingId: string, loadFailedText = '加载装箱单失败') {
    const id = String(packingId || '').trim()
    if (!id) return

    if (selectedPackingId.value === id && packing.value && !bindError.value) {
      // 同箱再次单击：保持当前明细，仅确保已加载
      if (packingItem.value) await loadAggregates()
      return
    }

    const seq = ++bindSeq
    bindingLoading.value = true
    bindError.value = ''
    loadError.value = ''
    missingSellLink.value = false
    aggregates.value = null
    aggregatesKey.value = ''
    flowRow.value = null
    flowExtras.value = null
    packing.value = null
    packingItem.value = null
    pickPage.value = null

    try {
      const [detail, page] = await Promise.all([
        packingApi.getById(id),
        inventoryCenterApi.getPickPageByPacking(id).catch(() => null)
      ])
      if (seq !== bindSeq) return

      packing.value = detail
      pickPage.value = page
      const first = detail.items?.[0] ?? null
      if (!first) {
        packingItem.value = null
        flowRow.value = null
        flowExtras.value = null
        return
      }
      await selectPackingItem(detail, first, page)
    } catch (e: unknown) {
      if (seq !== bindSeq) return
      packing.value = null
      packingItem.value = null
      pickPage.value = null
      flowRow.value = null
      flowExtras.value = null
      bindError.value = getApiErrorMessage(e, loadFailedText)
    } finally {
      if (seq === bindSeq) bindingLoading.value = false
    }
  }

  /** 列表/详情：切换本箱明细 */
  async function selectItemById(packingItemId: string) {
    const header = packing.value
    const itemId = String(packingItemId || '').trim()
    if (!header || !itemId) return
    if (selectedPackingItemId.value === itemId && flowRow.value) {
      await loadAggregates()
      return
    }
    const line = header.items.find((x) => String(x.id || '').trim() === itemId)
    if (!line) return
    await selectPackingItem(header, line, pickPage.value)
  }

  return {
    packing,
    packingItem,
    pickPage,
    flowRow,
    flowExtras,
    aggregates,
    loading,
    bindingLoading,
    bindError,
    loadError,
    missingSellLink,
    selectedPackingId,
    selectedPackingItemId,
    itemChips,
    clear,
    setSelection,
    loadAggregates,
    selectPackingItem,
    patchPickPage,
    bindPackingFromList,
    selectItemById
  }
})
