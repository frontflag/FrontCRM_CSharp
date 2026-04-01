/**
 * 全站金额精度：单价 6 位小数，总额/金额 2 位小数（与后端 numeric 一致）
 */
export const UNIT_PRICE_DECIMALS = 6
export const TOTAL_AMOUNT_DECIMALS = 2

function num(v: unknown): number {
  const n = Number(v)
  return Number.isFinite(n) ? n : NaN
}

/** 单价展示（最多 6 位小数，至少 2 位） */
export function formatUnitPriceNumber(value: unknown): string {
  const n = num(value)
  if (Number.isNaN(n)) return '—'
  return n.toLocaleString('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: UNIT_PRICE_DECIMALS
  })
}

/** 总额/金额展示（固定 2 位小数） */
export function formatTotalAmountNumber(value: unknown): string {
  const n = num(value)
  if (Number.isNaN(n)) return '—'
  return n.toLocaleString('zh-CN', {
    minimumFractionDigits: TOTAL_AMOUNT_DECIMALS,
    maximumFractionDigits: TOTAL_AMOUNT_DECIMALS
  })
}

function currencySymbol(currency?: number): string {
  if (currency === 2) return '$'
  if (currency === 3) return '€'
  if (currency === 4) return 'HK$'
  return '¥'
}

/** 单价 + 币别符号（1=RMB 2=USD 3=EUR 4=HKD，与订单页一致） */
export function formatCurrencyUnitPrice(value: unknown, currency?: number): string {
  const sym = currencySymbol(currency)
  const n = num(value)
  if (Number.isNaN(n)) return '—'
  return `${sym}${n.toLocaleString('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: UNIT_PRICE_DECIMALS
  })}`
}

/** 总额 + 币别符号 */
export function formatCurrencyTotal(value: unknown, currency?: number): string {
  const sym = currencySymbol(currency)
  const n = num(value)
  if (Number.isNaN(n)) return '—'
  return `${sym}${n.toLocaleString('zh-CN', {
    minimumFractionDigits: TOTAL_AMOUNT_DECIMALS,
    maximumFractionDigits: TOTAL_AMOUNT_DECIMALS
  })}`
}
