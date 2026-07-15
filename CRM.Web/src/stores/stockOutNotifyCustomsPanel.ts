import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchCustomsDeclarationById, type CustomsDeclarationDetailDto } from '@/api/customs'
import type { StockOutRequestDto } from '@/api/stockOut'
import { StockOutTypeCode } from '@/constants/stockOutType'
import { getApiErrorMessage } from '@/utils/apiError'

function resolveNotifyStockOutType(v: unknown): number {
  const n = Number(v)
  if (
    n === StockOutTypeCode.Sales ||
    n === StockOutTypeCode.Customs ||
    n === StockOutTypeCode.Return ||
    n === StockOutTypeCode.Scrap
  ) {
    return n
  }
  return StockOutTypeCode.Sales
}

export const useStockOutNotifyCustomsPanelStore = defineStore('stockOutNotifyCustomsPanel', () => {
  const notifyRow = ref<StockOutRequestDto | null>(null)
  const detail = ref<CustomsDeclarationDetailDto | null>(null)
  const loading = ref(false)
  const loadError = ref('')
  const noDeclaration = ref(false)
  const detailDeclarationId = ref('')

  let loadSeq = 0

  const isCustomsSelection = computed(() => {
    const row = notifyRow.value
    if (!row) return false
    return resolveNotifyStockOutType(row.stockOutType) === StockOutTypeCode.Customs
  })

  function notifyRowKey(row: StockOutRequestDto | null | undefined) {
    return String(row?.id ?? '').trim()
  }

  function declarationIdFromRow(row: StockOutRequestDto | null | undefined) {
    return String(row?.customsDeclarationId ?? '').trim()
  }

  function clear() {
    notifyRow.value = null
    detail.value = null
    loadError.value = ''
    loading.value = false
    noDeclaration.value = false
    detailDeclarationId.value = ''
    loadSeq += 1
  }

  async function loadDetail(declarationId: string, loadFailedText = '加载报关单失败') {
    const id = declarationId.trim()
    if (!id) return

    if (detailDeclarationId.value === id && detail.value !== null && !loadError.value) return

    const seq = ++loadSeq
    loading.value = true
    loadError.value = ''

    try {
      const data = await fetchCustomsDeclarationById(id)
      if (seq !== loadSeq || declarationIdFromRow(notifyRow.value) !== id) return
      detail.value = data
      detailDeclarationId.value = id
      noDeclaration.value = false
    } catch (e: unknown) {
      if (seq !== loadSeq || declarationIdFromRow(notifyRow.value) !== id) return
      loadError.value = getApiErrorMessage(e, loadFailedText)
      detail.value = null
      detailDeclarationId.value = ''
    } finally {
      if (seq === loadSeq) loading.value = false
    }
  }

  async function selectNotifyRow(target: StockOutRequestDto, loadFailedText = '加载报关单失败') {
    const key = notifyRowKey(target)
    if (!key) return

    notifyRow.value = target
    const declarationId = declarationIdFromRow(target)

    if (!declarationId) {
      detail.value = null
      loadError.value = ''
      loading.value = false
      noDeclaration.value = true
      detailDeclarationId.value = ''
      loadSeq += 1
      return
    }

    noDeclaration.value = false
    await loadDetail(declarationId, loadFailedText)
  }

  async function refreshFromListRows(rows: StockOutRequestDto[], loadFailedText = '加载报关单失败') {
    if (!notifyRow.value) return
    const selectedId = notifyRowKey(notifyRow.value)
    const updated = rows.find((r) => notifyRowKey(r) === selectedId)
    if (!updated) return
    await selectNotifyRow(updated, loadFailedText)
  }

  return {
    notifyRow,
    detail,
    loading,
    loadError,
    noDeclaration,
    isCustomsSelection,
    clear,
    selectNotifyRow,
    refreshFromListRows,
    notifyRowKey
  }
})
