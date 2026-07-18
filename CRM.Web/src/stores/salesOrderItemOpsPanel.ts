import { ref } from 'vue'
import { defineStore } from 'pinia'
import salesOrderApi, { type SalesOrderDetailTabAggregates } from '@/api/salesOrder'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>
type RowHandler = (row: RowRecord) => void

export const useSalesOrderItemOpsPanelStore = defineStore('salesOrderItemOpsPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<SalesOrderDetailTabAggregates | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const aggregatesRowKey = ref('')

  let applyPurchaseHandler: RowHandler | null = null
  let applyStockOutHandler: RowHandler | null = null
  let loadSeq = 0

  function rowKey(target: RowRecord) {
    return String(target.sellOrderItemId ?? target.id ?? target.Id ?? '').trim()
  }

  function registerHandlers(handlers: { applyPurchase?: RowHandler; applyStockOut?: RowHandler }) {
    if (handlers.applyPurchase) applyPurchaseHandler = handlers.applyPurchase
    if (handlers.applyStockOut) applyStockOutHandler = handlers.applyStockOut
  }

  function unregisterHandlers() {
    applyPurchaseHandler = null
    applyStockOutHandler = null
  }

  function clear() {
    row.value = null
    aggregates.value = null
    loadError.value = ''
    loading.value = false
    aggregatesRowKey.value = ''
    loadSeq += 1
  }

  /** 仅记录列表选中行，不请求聚合接口（右栏非「操作」页签单击行时使用；切到「操作」后再 loadAggregates） */
  function setRowOnly(target: RowRecord) {
    const key = rowKey(target)
    if (!key) return
    row.value = target
    if (aggregatesRowKey.value !== key) {
      aggregates.value = null
      loadError.value = ''
      loading.value = false
    }
  }

  async function loadAggregates(loadFailedText = '加载明细失败') {
    if (!row.value) return
    const sellOrderId = String(row.value.sellOrderId ?? '').trim()
    const sellOrderItemId = rowKey(row.value)
    if (!sellOrderId || !sellOrderItemId) return
    if (aggregatesRowKey.value === sellOrderItemId && aggregates.value !== null && !loadError.value) return

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''

    try {
      const data = await salesOrderApi.getSellOrderItemDetailTabAggregates(sellOrderId, sellOrderItemId)
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== sellOrderItemId) return
      aggregates.value = data
      aggregatesRowKey.value = sellOrderItemId
    } catch (e: unknown) {
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== sellOrderItemId) return
      loadError.value = getApiErrorMessage(e, loadFailedText)
      aggregates.value = null
      aggregatesRowKey.value = ''
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectRow(target: RowRecord, loadFailedText = '加载明细失败') {
    setRowOnly(target)
    await loadAggregates(loadFailedText)
  }

  /** 外部已拉取 aggregates 时写入 store，避免重复请求（如销售订单详情底部面板） */
  function syncRowAndAggregates(
    target: RowRecord,
    data: SalesOrderDetailTabAggregates | null,
    options?: { error?: string }
  ) {
    const key = rowKey(target)
    if (!key) return
    row.value = target
    aggregates.value = data
    aggregatesRowKey.value = data ? key : ''
    loadError.value = String(options?.error ?? '').trim()
    loading.value = false
  }

  async function refreshFromListRows(rows: RowRecord[], loadFailedText = '加载明细失败', fetchAggregates = true) {
    if (!row.value) return
    const selectedId = rowKey(row.value)
    const updated = rows.find((r) => rowKey(r) === selectedId)
    if (!updated) return
    setRowOnly(updated)
    if (fetchAggregates) await loadAggregates(loadFailedText)
  }

  function runApplyPurchase() {
    if (row.value && applyPurchaseHandler) applyPurchaseHandler(row.value)
  }

  function runApplyStockOut() {
    if (row.value && applyStockOutHandler) applyStockOutHandler(row.value)
  }

  return {
    row,
    aggregates,
    loading,
    loadError,
    registerHandlers,
    unregisterHandlers,
    clear,
    setRowOnly,
    loadAggregates,
    selectRow,
    syncRowAndAggregates,
    refreshFromListRows,
    runApplyPurchase,
    runApplyStockOut,
    rowKey
  }
})
