/** 操作+进度卡片「已完成」时展示的生成对象单号（可跳转）。 */

export type OpsGeneratedDoc = {
  id: string
  code: string
}

type OpsGeneratedDocSource = {
  id?: string | null
  code?: string | null
  status?: number
  isDeleted?: boolean
}

export function collectOpsGeneratedDocs(
  rows: OpsGeneratedDocSource[],
  isSkipped?: (status: number) => boolean
): OpsGeneratedDoc[] {
  const seen = new Set<string>()
  const docs: OpsGeneratedDoc[] = []
  for (const row of rows) {
    if (row.isDeleted === true) continue
    if (isSkipped?.(Number(row.status))) continue
    const id = String(row.id ?? '').trim()
    const code = String(row.code ?? '').trim()
    if (!id || !code || seen.has(id)) continue
    seen.add(id)
    docs.push({ id, code })
  }
  docs.sort((a, b) => a.code.localeCompare(b.code, 'en'))
  return docs
}

/** 采购明细关联到货通知（跳过异常/取消态 status < 0）。 */
export function listLinkedArrivalNoticeDocs(
  aggregates?: { arrivalNotices?: Array<{ id?: string; noticeCode?: string; status?: number }> } | null
): OpsGeneratedDoc[] {
  if (aggregates == null) return []
  return collectOpsGeneratedDocs(
    (aggregates.arrivalNotices ?? []).map((x) => ({
      id: x.id,
      code: x.noticeCode,
      status: x.status
    })),
    (status) => status < 0
  )
}

const PR_STATUS_CANCELLED = 3

/** 销售明细关联采购申请（跳过已取消）。 */
export function listLinkedPurchaseRequisitionDocs(
  aggregates?: { purchaseRequisitions?: Array<{ id?: string; billCode?: string; status?: number }> } | null
): OpsGeneratedDoc[] {
  if (aggregates == null) return []
  return collectOpsGeneratedDocs(
    (aggregates.purchaseRequisitions ?? []).map((x) => ({
      id: x.id,
      code: x.billCode,
      status: x.status
    })),
    (status) => status === PR_STATUS_CANCELLED
  )
}

const STOCK_OUT_REQUEST_STATUS_CANCELLED = -1

/** 销售明细关联出库通知（跳过已取消）。 */
export function listLinkedStockOutRequestDocs(
  aggregates?: { stockOutRequests?: Array<{ id?: string; requestCode?: string; status?: number }> } | null
): OpsGeneratedDoc[] {
  if (aggregates == null) return []
  return collectOpsGeneratedDocs(
    (aggregates.stockOutRequests ?? []).map((x) => ({
      id: x.id,
      code: x.requestCode,
      status: x.status
    })),
    (status) => status === STOCK_OUT_REQUEST_STATUS_CANCELLED
  )
}
