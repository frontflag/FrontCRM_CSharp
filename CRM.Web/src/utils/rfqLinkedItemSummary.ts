/**
 * 与「新建报价」页顶部提示栏一致：从需求明细原始记录解析展示字段，并拉取明细（API + 主表 items 兜底）。
 */
import { rfqApi } from '@/api/rfq'
import type { RFQ, RFQItem } from '@/types/rfq'

export function extractMpn(raw: Record<string, unknown>): string {
  const v =
    raw.mpn ??
    raw.materialModel ??
    raw.MaterialModel ??
    raw.Mpn ??
    raw.customerMaterialModel ??
    raw.CustomerMaterialModel
  return v != null ? String(v).trim() : ''
}

export function extractBrand(raw: Record<string, unknown>): string {
  const v = raw.brand ?? raw.Brand ?? raw.customerBrand ?? raw.CustomerBrand
  return v != null ? String(v).trim() : ''
}

export function mapCurrencyLabelFromRaw(raw: Record<string, unknown>): string {
  const c = raw.priceCurrency ?? raw.currency ?? raw.PriceCurrency
  if (typeof c === 'string') {
    if (c.toUpperCase().includes('USD')) return 'USD'
    if (c.toUpperCase().includes('EUR')) return 'EUR'
    if (c.toUpperCase().includes('HK')) return 'HKD'
    return c.includes('RMB') || c.includes('CNY') ? 'RMB' : c
  }
  if (c === 2) return 'USD'
  if (c === 3) return 'EUR'
  if (c === 4) return 'HKD'
  return 'RMB'
}

export function formatLinkAlertQuantity(item: Record<string, unknown>): string {
  const qty = Number(item.quantity ?? item.Quantity ?? 1) || 1
  if (Number.isNaN(qty)) return '—'
  return qty.toLocaleString('zh-CN')
}

export function formatLinkTargetPriceText(item: Record<string, unknown>): string {
  const tp = item.targetPrice ?? item.TargetPrice
  const targetPrice = tp != null && tp !== '' ? Number(tp) : undefined
  const currencyLabel = mapCurrencyLabelFromRaw(item)
  if (targetPrice == null || Number.isNaN(Number(targetPrice))) return '—'
  const n = Number(targetPrice)
  return `${n.toLocaleString('zh-CN', { minimumFractionDigits: 4, maximumFractionDigits: 4 })} ${currencyLabel || 'RMB'}`
}

/** 与 QuoteCreate linkAlertRfqDisplay 一致：优先需求编号，否则 id */
export function linkAlertRfqDisplayFromRaw(
  item: Record<string, unknown>,
  opts: { rfqCode?: string; rfqId?: string; rfqHeader?: RFQ | null }
): string {
  const fromHeader = opts.rfqHeader?.rfqCode
  const code = (item.rfqCode as string) || fromHeader || opts.rfqCode || ''
  const id = String(item.rfqId ?? item.RfqId ?? opts.rfqId ?? '')
  return (code || id || '').trim() || '—'
}

export async function fetchLinkedRfqItemRecord(
  rfqId: string,
  itemId: string
): Promise<{ item: Record<string, unknown>; rfqHeader: RFQ | null } | null> {
  let item: Record<string, unknown> | null = null
  let rfqHeader: RFQ | null = null

  try {
    item = (await rfqApi.getRFQItemById(itemId)) as unknown as Record<string, unknown>
  } catch {
    item = null
  }

  if ((!item || (!extractMpn(item) && !extractBrand(item))) && rfqId) {
    try {
      rfqHeader = await rfqApi.getRFQById(rfqId)
      const list = (rfqHeader.items || []) as RFQItem[]
      const found = list.find((x) => x.id === itemId) as unknown as Record<string, unknown> | undefined
      if (found) item = found
    } catch {
      /* ignore */
    }
  }

  if (!item) return null
  return { item, rfqHeader }
}

export function buildLinkAlertFieldsFromItem(
  item: Record<string, unknown>,
  opts: { rfqCode?: string; rfqId?: string; rfqHeader?: RFQ | null }
) {
  return {
    linkAlertRfqDisplay: linkAlertRfqDisplayFromRaw(item, opts),
    mpn: extractMpn(item) || '—',
    brand: extractBrand(item) || '—',
    quantityDisplay: formatLinkAlertQuantity(item),
    targetPriceText: formatLinkTargetPriceText(item)
  }
}
