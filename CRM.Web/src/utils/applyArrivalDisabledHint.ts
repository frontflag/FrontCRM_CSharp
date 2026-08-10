import { purchaseOrderAllowsArrivalNotice } from '@/constants/purchaseOrderStatus'
import { getArrivalMetrics } from '@/utils/purchaseOrderItemOpsPanel'

export interface ApplyArrivalDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

export function applyArrivalButtonDisabled(row: Record<string, unknown>): boolean {
  if (!purchaseOrderAllowsArrivalNotice(row)) return true
  return getArrivalMetrics(row).applicableQty <= 0
}

/** 构建到货通知禁用提示（与列表操作列口径一致）。 */
export function buildApplyArrivalDisabledHintContent(
  row: Record<string, unknown>,
  t: TranslateFn
): ApplyArrivalDisabledHintContent | null {
  if (!applyArrivalButtonDisabled(row)) return null

  if (!purchaseOrderAllowsArrivalNotice(row)) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.arrivalNeedConfirmed'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.arrivalNextConfirmed')
    }
  }

  const { applicableQty } = getArrivalMetrics(row)
  if (applicableQty <= 0) {
    return {
      summary: t('purchaseOrderItemList.opsPanel.arrivalNoRemaining'),
      details: [],
      nextStep: t('purchaseOrderItemList.opsPanel.arrivalNextNoRemaining')
    }
  }

  return null
}
