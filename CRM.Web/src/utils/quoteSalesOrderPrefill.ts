import { quoteApi } from '@/api/quote'
import { rfqApi } from '@/api/rfq'

/** 报价详情上的客户 ID；无则通过报价关联的 rfqId 拉需求主表 */
export async function resolveCustomerIdFromQuoteDetail(
  q: Record<string, unknown>
): Promise<string> {
  let cid = String(q.customerId ?? q.CustomerId ?? '').trim()
  if (cid) return cid
  const rfqId = String(q.rfqId ?? q.RFQId ?? '').trim()
  if (!rfqId) return ''
  try {
    const rfq = await rfqApi.getRFQById(rfqId)
    return String(rfq.customerId ?? '').trim()
  } catch {
    return ''
  }
}

/** 跳转前校验：所选报价解析出的客户 ID 须一致（空 ID 不参与冲突判断） */
export async function assertQuotesSameCustomer(
  quoteIds: string[]
): Promise<{ ok: true } | { ok: false; message: string }> {
  const ids = [...new Set(quoteIds.map((s) => s.trim()).filter(Boolean))]
  if (!ids.length) return { ok: false, message: '未选择报价单' }

  const customerIds: string[] = []
  for (const id of ids) {
    try {
      const res = await quoteApi.getById(id)
      const q = res?.data as Record<string, unknown> | undefined
      if (!q) return { ok: false, message: '报价单不存在或无权访问' }
      customerIds.push(await resolveCustomerIdFromQuoteDetail(q))
    } catch {
      return { ok: false, message: '加载报价失败，请重试' }
    }
  }

  const nonempty = customerIds.filter(Boolean)
  if (nonempty.length >= 2 && new Set(nonempty).size > 1) {
    return { ok: false, message: '请选择同一家客户生成销售订单' }
  }
  return { ok: true }
}
