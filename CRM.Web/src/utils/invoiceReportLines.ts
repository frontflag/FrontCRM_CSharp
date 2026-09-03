/** Invoice 行金额：优先装箱明细销售单价，缺价时才按出库头金额摊数量。 */

import { CurrencyCode } from '@/constants/currency'
import {
  formatTotalAmountNumber,
  formatTotalAmountWithCurrencyCodeSuffix,
  formatUnitPriceNumber,
  formatUnitPriceWithCurrencyCodeSuffix
} from '@/utils/moneyFormat'

/** 报关出库 / 报关装箱（与后端 StockOutTypeCode.Customs 一致） */
export const STOCK_OUT_TYPE_CUSTOMS = 20

export type InvoiceReportAmountSource = {
  qty?: number | null
  price?: number | null
  priceCurrency?: number | null
}

export type InvoiceReportLineAmount = {
  qty: number
  unit: number
  amount: number
}

export function resolveInvoiceLineAmounts(
  rows: InvoiceReportAmountSource[],
  headerTotalAmount: number
): InvoiceReportLineAmount[] {
  const list = rows ?? []
  const hasLinePrice = list.some((r) => r.price != null && Number.isFinite(Number(r.price)))
  const totalQty = list.reduce((acc, row) => acc + (Number(row.qty) || 0), 0)
  const header = Number(headerTotalAmount) || 0
  return list.map((row) => {
    const qty = Number(row.qty) || 0
    if (hasLinePrice) {
      const unit = row.price != null && Number.isFinite(Number(row.price)) ? Number(row.price) : 0
      return { qty, unit, amount: unit * qty }
    }
    const amount = totalQty > 0 ? (header * qty) / totalQty : 0
    const unit = qty > 0 ? amount / qty : 0
    return { qty, unit, amount }
  })
}

export function sumInvoiceLineAmounts(lines: InvoiceReportLineAmount[]): number {
  return lines.reduce((acc, line) => acc + (Number(line.amount) || 0), 0)
}

/** 行上有币别码则金额后跟币别；无码只打数字。 */
export function formatInvoiceMoney(
  value: number,
  currency: number | null | undefined,
  kind: 'unit' | 'total'
): string {
  const hasCcy = currency != null && Number.isFinite(Number(currency))
  if (kind === 'unit') {
    return hasCcy
      ? formatUnitPriceWithCurrencyCodeSuffix(value, Number(currency))
      : formatUnitPriceNumber(value)
  }
  return hasCcy
    ? formatTotalAmountWithCurrencyCodeSuffix(value, Number(currency))
    : formatTotalAmountNumber(value)
}

/** 报关装箱 Invoice：判定应走美金段（装箱类型优先，勿用关联销售出库单类型）。 */
export function resolveCustomsInvoiceStockOutType(args: {
  packingStockOutType?: number | null
  stockOutType?: number | null
  customsBrokerConsignee?: boolean
}): number | null {
  const packingType = args.packingStockOutType
  if (packingType != null && Number.isFinite(Number(packingType))) return Number(packingType)
  if (args.customsBrokerConsignee) return STOCK_OUT_TYPE_CUSTOMS
  const stockOutType = args.stockOutType
  if (stockOutType != null && Number.isFinite(Number(stockOutType))) return Number(stockOutType)
  return null
}

/** 报关装箱 Invoice：美金段，有折算美金价则用之，币别固定 USD（对齐后端 ResolveLine）。 */
export function applyCustomsInvoiceUsdPrices(
  stockOutType: number | null | undefined,
  lines: Array<{ packingItemId?: string | null; price?: number | null; priceCurrency?: number | null }>,
  extendsList?: Array<{ packingItemId?: string | null; priceConvertPrice?: number | null }> | null
): void {
  if (Number(stockOutType) !== STOCK_OUT_TYPE_CUSTOMS || !lines.length) return
  const convertById = new Map<string, number>()
  for (const row of extendsList ?? []) {
    const id = String(row.packingItemId ?? '').trim().toLowerCase()
    const conv = Number(row.priceConvertPrice)
    if (id && Number.isFinite(conv) && conv > 0) convertById.set(id, conv)
  }
  for (const line of lines) {
    const id = String(line.packingItemId ?? '').trim().toLowerCase()
    const conv = id ? convertById.get(id) : undefined
    const base = line.price != null && Number.isFinite(Number(line.price)) ? Number(line.price) : null
    line.price = conv != null && conv > 0 ? conv : base
    line.priceCurrency = CurrencyCode.USD
  }
}

/** 全部行同一币别则返回该码，否则合计不带币别。 */
export function resolveInvoiceTotalCurrency(
  rows: Array<{ priceCurrency?: number | null }>
): number | null {
  const codes = [
    ...new Set(
      (rows ?? [])
        .map((r) => r.priceCurrency)
        .filter((c) => c != null && Number.isFinite(Number(c)))
        .map((c) => Number(c))
    )
  ]
  return codes.length === 1 ? codes[0] : null
}
