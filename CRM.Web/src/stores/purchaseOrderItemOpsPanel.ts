import { ref } from 'vue'
import { defineStore } from 'pinia'
import purchaseOrderApi, { type PurchaseOrderDetailTabAggregates } from '@/api/purchaseOrder'
import { getApiErrorMessage } from '@/utils/apiError'

type RowRecord = Record<string, unknown>
type RowHandler = (row: RowRecord) => void

export const usePurchaseOrderItemOpsPanelStore = defineStore('purchaseOrderItemOpsPanel', () => {
  const row = ref<RowRecord | null>(null)
  const aggregates = ref<PurchaseOrderDetailTabAggregates | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const aggregatesRowKey = ref('')

  let applyArrivalHandler: RowHandler | null = null
  let applyPaymentHandler: RowHandler | null = null
  let loadSeq = 0

  function rowKey(target: RowRecord) {
    return String(target.purchaseOrderItemId ?? target.id ?? target.Id ?? '').trim()
  }

  function registerHandlers(handlers: { applyArrival?: RowHandler; applyPayment?: RowHandler }) {
    if (handlers.applyArrival) applyArrivalHandler = handlers.applyArrival
    if (handlers.applyPayment) applyPaymentHandler = handlers.applyPayment
  }

  function unregisterHandlers() {
    applyArrivalHandler = null
    applyPaymentHandler = null
  }

  function clear() {
    row.value = null
    aggregates.value = null
    loadError.value = ''
    loading.value = false
    aggregatesRowKey.value = ''
    loadSeq += 1
  }

  /** 仅记录列表选中行，不请求聚合接口（右侧面板收起时使用） */
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
    const purchaseOrderId = String(row.value.purchaseOrderId ?? '').trim()
    const purchaseOrderItemId = rowKey(row.value)
    if (!purchaseOrderId || !purchaseOrderItemId) return
    if (aggregatesRowKey.value === purchaseOrderItemId && aggregates.value !== null && !loadError.value) return

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''

    try {
      const data = await purchaseOrderApi.getPurchaseOrderItemDetailTabAggregates(
        purchaseOrderId,
        purchaseOrderItemId
      )
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== purchaseOrderItemId) return
      aggregates.value = data
      aggregatesRowKey.value = purchaseOrderItemId
    } catch (e: unknown) {
      if (seq !== loadSeq || !row.value || rowKey(row.value) !== purchaseOrderItemId) return
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

  /** 外部已拉取 aggregates 时写入 store，避免重复请求（如采购订单详情底部面板） */
  function syncRowAndAggregates(
    target: RowRecord,
    data: PurchaseOrderDetailTabAggregates | null,
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

  async function refreshFromListRows(
    rows: RowRecord[],
    loadFailedText = '加载明细失败',
    fetchAggregates = true
  ) {
    if (!row.value) return
    const selectedId = rowKey(row.value)
    const updated = rows.find((r) => rowKey(r) === selectedId)
    if (!updated) return
    setRowOnly(updated)
    if (fetchAggregates) await loadAggregates(loadFailedText)
  }

  function runApplyArrival() {
    if (row.value && applyArrivalHandler) applyArrivalHandler(row.value)
  }

  function runApplyPayment() {
    if (row.value && applyPaymentHandler) applyPaymentHandler(row.value)
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
    runApplyArrival,
    runApplyPayment,
    rowKey
  }
})
