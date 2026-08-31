import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import { resolveArrivalNoticeCustomsBrokerName } from '@/utils/arrivalNoticeOpsOverview'

export type StockInPurchaseOrderTypeKey = 'customer' | 'stocking' | 'sample' | 'unknown'

export function resolveStockInPurchaseOrderTypeKey(
  type: number | string | null | undefined
): StockInPurchaseOrderTypeKey {
  const n = Number(type)
  if (n === 1) return 'customer'
  if (n === 2) return 'stocking'
  if (n === 3) return 'sample'
  return 'unknown'
}

/** 概况单价：优先采购行 cost；否则列表单值汇总。脱敏或无效为 —。 */
export function resolveStockInOverviewUnitPrice(opts: {
  maskSensitive?: boolean
  aggregateUnitPrice?: number | null
  aggregateCurrency?: number | null
  listSummary?: string | null
  listCurrency?: number | null
}): string {
  if (opts.maskSensitive) return '—'
  const agg = opts.aggregateUnitPrice
  if (agg != null && Number.isFinite(Number(agg)) && Number(agg) !== 0) {
    return formatUnitPriceWithCurrencyCodeSuffix(agg, opts.aggregateCurrency ?? undefined)
  }
  const summary = String(opts.listSummary ?? '').trim()
  if (!summary) return '—'
  if (summary.includes(',')) return summary
  const n = Number(summary.replace(/,/g, ''))
  if (!Number.isFinite(n) || n === 0) return summary
  return formatUnitPriceWithCurrencyCodeSuffix(n, opts.listCurrency ?? undefined)
}

export function resolveStockInCustomsBrokerName(
  row: Record<string, unknown> | null | undefined,
  stockInType?: number | string | null
): string {
  return resolveArrivalNoticeCustomsBrokerName(row, stockInType)
}
