import { StockOutTypeCode } from '@/constants/stockOutType'

export function isCustomsPackingReport(stockOutType?: number | string | null): boolean {
  return Number(stockOutType) === StockOutTypeCode.Customs
}

/** 报关装箱单收货人名称不脱敏；销售装箱单仍走 521。 */
export function resolvePackingReportConsigneeName(args: {
  stockOutType?: number | string | null
  customerName?: string | null
  shipToFirstLine?: string | null
  maskSaleSensitive?: boolean
  customsBrokerConsignee?: boolean
}): string {
  const dash = '—'
  if (args.customsBrokerConsignee || isCustomsPackingReport(args.stockOutType)) {
    const n = (args.shipToFirstLine ?? '').trim()
    return n && n !== dash ? n : dash
  }
  if (args.maskSaleSensitive) return dash
  return (args.customerName ?? '').trim() || dash
}
