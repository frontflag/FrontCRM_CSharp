import { PO_STATUS_VENDOR_CONFIRMED } from '@/constants/purchaseOrderStatus'
import { getPaymentMetrics } from '@/utils/purchaseOrderItemOpsPanel'

export interface ApplyPaymentDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

function canApplyPayment(row: Record<string, unknown>): boolean {
  return row.canApplyPayment === true || row.CanApplyPayment === true
}

export function applyPaymentButtonDisabled(row: Record<string, unknown>): boolean {
  if (!canApplyPayment(row)) return true
  return getPaymentMetrics(row).availableAmount <= 0
}

/** 构建申请付款禁用提示（与列表操作列口径一致）。 */
export function buildApplyPaymentDisabledHintContent(
  row: Record<string, unknown>,
  t: TranslateFn
): ApplyPaymentDisabledHintContent | null {
  if (!applyPaymentButtonDisabled(row)) return null

  const itemStatus = Number(row.itemStatus ?? row.ItemStatus)

  if (!canApplyPayment(row)) {
    if (itemStatus !== PO_STATUS_VENDOR_CONFIRMED) {
      return {
        summary: t('purchaseOrderItemList.opsPanel.paymentNeedConfirmed'),
        details: [],
        nextStep: t('purchaseOrderItemList.opsPanel.paymentNextConfirmed')
      }
    }
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentNotEligible'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextNotEligible')
    }
  }

  const { availableAmount } = getPaymentMetrics(row)
  if (availableAmount <= 0) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.paymentNoRemaining'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.paymentNextNoRemaining')
    }
  }

  return null
}
