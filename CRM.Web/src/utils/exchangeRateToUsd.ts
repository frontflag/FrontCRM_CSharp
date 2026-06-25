import { CurrencyCode } from '@/constants/currency'

/** 与财务参数、后端 <see cref="ExchangeRateToUsdConverter"/> 一致：1 USD 可兑的外币数量。 */
export type ExchangeRatesUsdBase = {
  usdToCny: number
  usdToHkd: number
  usdToEur: number
}

function round6(v: number): number {
  return Math.round(v * 1e6) / 1e6
}

function div(price: number, rate: number): number {
  return rate > 0 ? round6(price / rate) : 0
}

/** 原币单价 → 美元单价（6 位小数；与订单明细 convert_price 口径一致）。 */
export function unitLocalToUsd(
  unitPrice: number,
  currency: number,
  rates: ExchangeRatesUsdBase
): number | undefined {
  const p = Number(unitPrice)
  if (!Number.isFinite(p) || p < 0) return undefined
  if (p === 0) return 0

  const { usdToCny, usdToHkd, usdToEur } = rates

  switch (currency) {
    case CurrencyCode.USD:
      return round6(p)
    case CurrencyCode.RMB:
      return usdToCny > 0 ? div(p, usdToCny) : undefined
    case CurrencyCode.EUR:
      return usdToEur > 0 ? div(p, usdToEur) : undefined
    case CurrencyCode.HKD:
      return usdToHkd > 0 ? div(p, usdToHkd) : undefined
    default:
      return undefined
  }
}
