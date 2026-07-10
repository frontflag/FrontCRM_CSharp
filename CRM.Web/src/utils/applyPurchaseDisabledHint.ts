import { salesOrderMainAllowsPurchaseAndStockOut } from '@/constants/salesOrderStatus'

export interface ApplyPurchaseDisabledHintContent {
  summary: string
  details: string[]
  nextStep: string
}

type TranslateFn = (key: string, params?: Record<string, unknown>) => string

function purchaseRemainingQty(row: Record<string, unknown>): number | null {
  const raw = row.purchaseRemainingQty ?? row.PurchaseRemainingQty
  if (raw === undefined || raw === null) return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
}

export function applyPurchaseButtonDisabled(row: Record<string, unknown>): boolean {
  const orderStatus = Number(row.orderStatus ?? row.OrderStatus)
  if (!salesOrderMainAllowsPurchaseAndStockOut(orderStatus)) return true
  const remaining = purchaseRemainingQty(row)
  if (remaining === null) return false
  return remaining <= 0
}

/** 构建申请采购禁用提示（与列表操作列口径一致）。 */
export function buildApplyPurchaseDisabledHintContent(
  row: Record<string, unknown>,
  t: TranslateFn
): ApplyPurchaseDisabledHintContent | null {
  if (!applyPurchaseButtonDisabled(row)) return null

  const orderStatus = Number(row.orderStatus ?? row.OrderStatus)
  const remaining = purchaseRemainingQty(row)

  if (!salesOrderMainAllowsPurchaseAndStockOut(orderStatus)) {
    return {
      summary: t('salesOrderItemList.messages.applyPurchaseNeedAudit'),
      details: [],
      nextStep: t('salesOrderItemList.opsPanel.purchaseNextAudit')
    }
  }

  if (remaining !== null && remaining <= 0) {
    return {
      summary: t('salesOrderItemList.messages.prLineNotAvailable'),
      details: [],
      nextStep: t('salesOrderItemList.opsPanel.purchaseNextNoRemaining')
    }
  }

  return null
}
