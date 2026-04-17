import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'

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

/** 单价 + 币别字母后缀（如 `3.45 RMB`），与 `CURRENCY_CODE_TO_TEXT` 一致 */
export function formatUnitPriceWithCurrencyCodeSuffix(value: unknown, currency?: number): string {
  const s = formatUnitPriceNumber(value)
  if (s === '—') return s
  const c = Number(currency)
  const code =
    (Number.isFinite(c) && CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT]) || 'RMB'
  return `${s} ${code}`
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

/** 列表金额是否有有效数值（与库存列表、报价阶梯一致） */
export function listTotalAmountHasValue(v: unknown): boolean {
  if (v == null || v === '') return false
  const n = Number(v)
  return Number.isFinite(n)
}

/**
 * 列表金额拆段（Intl formatToParts），与《业务列表规范》库存/RFQ 阶梯展示一致：
 * 整数+千分位与小数分段，便于与尾部 ISO 币别色标组合。
 */
export function splitListMoneyParts(n: number): { intPart: string; fracPart: string } {
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: TOTAL_AMOUNT_DECIMALS,
    maximumFractionDigits: TOTAL_AMOUNT_DECIMALS
  }).formatToParts(n)
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) {
    const fallback = n.toLocaleString('zh-CN', {
      minimumFractionDigits: TOTAL_AMOUNT_DECIMALS,
      maximumFractionDigits: TOTAL_AMOUNT_DECIMALS
    })
    return { intPart: fallback, fracPart: '' }
  }
  return { intPart, fracPart }
}

/** 币别枚举 → ISO 字母（与 CURRENCY_CODE_TO_TEXT 一致） */
export function listAmountCurrencyIso(currency?: number | null): string {
  const c = Number(currency)
  const code = Number.isFinite(c) ? c : 1
  return CURRENCY_CODE_TO_TEXT[code as keyof typeof CURRENCY_CODE_TO_TEXT] ?? 'RMB'
}

/**
 * 列表金额尾部币别色 class（与 crm-quote-tier-dock.scss 一致）。
 * 用于 `dock-tier-ccy` 与 `splitListMoneyParts` 组合。
 */
export function listAmountCurrencyDockClass(currency?: number | null): string {
  const n = Number(currency)
  if (n === 2) return 'dock-tier-ccy--usd'
  if (n === 3) return 'dock-tier-ccy--eur'
  if (n === 4) return 'dock-tier-ccy--hkd'
  if (n === 1 || !Number.isFinite(n) || n === 0) return 'dock-tier-ccy--rmb'
  return 'dock-tier-ccy--purple'
}
