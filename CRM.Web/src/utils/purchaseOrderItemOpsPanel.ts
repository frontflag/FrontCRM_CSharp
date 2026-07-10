import type { PurchaseOrderDetailTabAggregates } from '@/api/purchaseOrder'
import { calcProgressPercent } from '@/utils/sellOrderItemOpsPanel'

export { calcProgressPercent }

export function getPoLineTotal(row: Record<string, unknown>): number {
  const direct = Number(row.lineTotal ?? row.LineTotal ?? NaN)
  if (Number.isFinite(direct) && direct > 0) {
    return Math.round((direct + Number.EPSILON) * 100) / 100
  }
  const qty = Number(row.qty ?? row.Qty ?? 0)
  const cost = Number(row.cost ?? row.Cost ?? 0)
  if (Number.isFinite(qty) && Number.isFinite(cost) && qty > 0 && cost > 0) {
    return Math.round((qty * cost + Number.EPSILON) * 100) / 100
  }
  return 0
}

export function getArrivalMetrics(
  row: Record<string, unknown>,
  aggregates?: PurchaseOrderDetailTabAggregates | null
) {
  const ov = aggregates?.lineOverview?.arrivalNotice
  if (ov) {
    return {
      orderQty: Math.max(0, Math.trunc(Number(ov.total) || 0)),
      notifiedQty: Math.max(0, Math.trunc(Number(ov.done) || 0)),
      applicableQty: Math.max(0, Math.trunc(Number(ov.pending) || 0))
    }
  }
  const orderQty = Math.max(0, Math.round(Number(row.qty ?? row.Qty ?? 0)))
  const notifiedQty = Math.max(
    0,
    Math.round(Number(row.qtyStockInNotifyExpectSum ?? row.QtyStockInNotifyExpectSum ?? 0))
  )
  const applicableRaw = row.qtyStockInNotifyNot ?? row.QtyStockInNotifyNot
  const applicableQty = Math.max(
    0,
    Math.round(
      applicableRaw != null && applicableRaw !== ''
        ? Number(applicableRaw)
        : orderQty - notifiedQty
    )
  )
  return { orderQty, notifiedQty, applicableQty }
}

export function getPaymentMetrics(
  row: Record<string, unknown>,
  aggregates?: PurchaseOrderDetailTabAggregates | null
) {
  // 申请付款卡片口径：已申请/可申请 = 请款维度（PaymentAmountRequested），
  // 不用 lineOverview.payment（那是概况矩阵的已付/待付 PaymentAmountFinish / PaymentAmountNot）。
  void aggregates
  const lineTotal = getPoLineTotal(row)
  const requestedAmount = Math.max(0, Number(row.paymentRequestedAmount ?? row.PaymentRequestedAmount ?? 0))
  const availableAmount = Math.max(
    0,
    Math.round((lineTotal - requestedAmount + Number.EPSILON) * 100) / 100
  )
  const currency = Number(row.currency ?? row.Currency ?? 1)
  return { lineTotal, requestedAmount, availableAmount, currency }
}
