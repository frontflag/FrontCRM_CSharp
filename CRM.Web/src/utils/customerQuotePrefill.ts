/** 草稿生成客户报价单：空 ID 不参与冲突，非空客户 / RFQ 业务员须各自唯一。 */

export type CustomerQuoteDraftCompatRow = {
  customerId?: string | null
  salesUserId?: string | null
}

export type DraftCompatResult =
  | { ok: true }
  | { ok: false; reason: 'customer' | 'sales' }

function nonemptyIds(values: Array<string | null | undefined>): Set<string> {
  return new Set(values.map((x) => (x || '').trim()).filter(Boolean))
}

export function assertDraftsSameCustomerAndSales(
  rows: CustomerQuoteDraftCompatRow[]
): DraftCompatResult {
  if (nonemptyIds(rows.map((r) => r.customerId)).size > 1) {
    return { ok: false, reason: 'customer' }
  }
  if (nonemptyIds(rows.map((r) => r.salesUserId)).size > 1) {
    return { ok: false, reason: 'sales' }
  }
  return { ok: true }
}
