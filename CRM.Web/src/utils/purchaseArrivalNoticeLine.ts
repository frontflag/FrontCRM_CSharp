export type PurchaseArrivalNoticeLineRow = {
  pn: string
  brand: string
  orderQty: number
  alreadyNotified: number
  applicableQty: number
  qty: number
  spec: string
  packaging: string
}

export function formatArrivalNoticeQty(v: unknown): string {
  const n = Math.max(0, Math.round(Number(v) || 0))
  return n.toLocaleString('en-US')
}

/** 从采购明细行（列表/详情）构建「来货明细」单行，口径与扩展表 QtyStockInNotify* 一致。 */
export function buildPurchaseArrivalNoticeLineRow(row: Record<string, unknown>): PurchaseArrivalNoticeLineRow {
  const orderQty = Math.max(0, Math.round(Number(row.qty ?? row.Qty ?? 0)))
  const alreadyNotified = Math.max(
    0,
    Math.round(Number(row.qtyStockInNotifyExpectSum ?? row.QtyStockInNotifyExpectSum ?? 0))
  )
  const applicableRaw = row.qtyStockInNotifyNot ?? row.QtyStockInNotifyNot
  const applicableQty = Math.max(
    0,
    Math.round(
      applicableRaw != null && applicableRaw !== ''
        ? Number(applicableRaw)
        : orderQty - alreadyNotified
    )
  )
  return {
    pn: String(row.pn ?? row.PN ?? '').trim(),
    brand: String(row.brand ?? row.Brand ?? '').trim(),
    orderQty,
    alreadyNotified,
    applicableQty,
    qty: applicableQty > 0 ? applicableQty : 0,
    spec: '',
    packaging: ''
  }
}
