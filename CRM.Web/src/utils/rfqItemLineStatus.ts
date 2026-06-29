import { RFQItemStatus } from '@/types/rfq'

/** 与需求明细列表一致：查无报价(5) 不被报价条数覆盖；待报价(0)+有报价 → 已报价(1) */
export function effectiveRfqItemLineStatus(
  rawStatus: number | string | undefined | null,
  quoteCount = 0
): number | undefined {
  const n = rawStatus === undefined || rawStatus === null || rawStatus === '' ? NaN : Number(rawStatus)
  if (Number.isFinite(n) && n === RFQItemStatus.NoQuoteFound) return n
  if (Number.isFinite(n) && n === RFQItemStatus.Pending && quoteCount > 0) return RFQItemStatus.Quoted
  if (Number.isFinite(n)) return n
  return undefined
}

export type RfqItemStatusTagType = 'info' | 'warning' | undefined

export function rfqItemStatusTagType(status?: number | string): RfqItemStatusTagType {
  const n = status === undefined || status === null || status === '' ? NaN : Number(status)
  if (!Number.isFinite(n)) return undefined
  if (n === RFQItemStatus.Pending) return 'info'
  if (n === RFQItemStatus.NoQuoteFound) return 'warning'
  return undefined
}

export const RFQ_ITEM_STATUS_I18N_KEYS: Record<number, string> = {
  [RFQItemStatus.Pending]: 'rfqItemList.status.pending',
  [RFQItemStatus.Quoted]: 'rfqItemList.status.quoted',
  [RFQItemStatus.Accepted]: 'rfqItemList.status.accepted',
  [RFQItemStatus.Rejected]: 'rfqItemList.status.rejected',
  [RFQItemStatus.Closed]: 'rfqItemList.status.closed',
  [RFQItemStatus.NoQuoteFound]: 'rfqItemList.status.noQuote',
}
