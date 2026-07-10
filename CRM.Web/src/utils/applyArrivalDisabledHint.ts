import { PO_STATUS_VENDOR_CONFIRMED } from '@/constants/purchaseOrderStatus'
import { getArrivalMetrics } from '@/utils/purchaseOrderItemOpsPanel'

export interface ApplyArrivalDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

export function applyArrivalButtonDisabled(row: Record<string, unknown>): boolean {
  const itemStatus = Number(row.itemStatus ?? row.ItemStatus)
  if (itemStatus !== PO_STATUS_VENDOR_CONFIRMED) return true
  return getArrivalMetrics(row).applicableQty <= 0
}

/** 构建到货通知禁用提示（与列表操作列口径一致）。 */
export function buildApplyArrivalDisabledHintContent(
  row: Record<string, unknown>,
  t: TranslateFn
): ApplyArrivalDisabledHintContent | null {
  if (!applyArrivalButtonDisabled(row)) return null

  const itemStatus = Number(row.itemStatus ?? row.ItemStatus)
  const { applicableQty } = getArrivalMetrics(row)

  if (itemStatus !== PO_STATUS_VENDOR_CONFIRMED) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.arrivalNeedConfirmed'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.arrivalNextConfirmed')
    }
  }

  if (applicableQty <= 0) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.arrivalNoRemaining'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.arrivalNextNoRemaining')
    }
  }

  return null
}
