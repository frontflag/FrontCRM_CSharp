import { CurrencyCode } from '@/constants/currency'
import { unitLocalToUsd, type ExchangeRatesUsdBase } from '@/utils/exchangeRateToUsd'

/**
 * 报关费用公式演示。逐步舍入与 CustomsFeeCalculator 一致：
 * 金额 2 位、单价 6 位，四舍五入远离 0；后一步只用前一步舍入后的结果。
 * 采购美金价折合与费用面板展示同一条 unitLocalToUsd 链。
 */

/** 报关公司账单核对用综合费率（固定 0.3%），仅演示区展示，不参与落库。 */
export const BROKER_COMPREHENSIVE_FEE_RATE = 0.003

export type CustomsFeeFormulaStepId =
  | 'costUsd'
  | 'goods'
  | 'duty'
  | 'vat'
  | 'agency'
  | 'total'
  | 'unit'

export type CustomsFeeFormulaUnavailableReason =
  | 'missing-cost'
  | 'bad-rate'
  | 'bad-qty'
  | 'bad-agency'

export type CustomsFeeFormulaExpr =
  | { kind: 'manual'; result: number }
  | { kind: 'usd'; p0Usd: number; ratio: number; result: number }
  | {
      kind: 'fx'
      p0: number
      currency: number
      fxRate: number
      p0Usd: number
      ratio: number
      result: number
    }
  | { kind: 'table-cost'; result: number }
  | { kind: 'goods'; costUsd: number; rate: number; qty: number; result: number }
  | { kind: 'duty'; goods: number; dutyRate: number; result: number }
  | { kind: 'vat'; goods: number; duty: number; vatRate: number; result: number }
  | {
      kind: 'agency'
      goods: number
      duty: number
      vat: number
      agencyRate: number
      result: number
    }
  | {
      kind: 'total'
      goods: number
      duty: number
      vat: number
      agency: number
      other: number
      result: number
    }
  | { kind: 'unit'; total: number; qty: number; result: number }

/** 报关公司公式核对（挂在⑤代理费步骤下，不单独编号）。 */
export type BrokerAgencyFormulaCheck = {
  costUsd: number
  qty: number
  exchangeRate: number
  dutyRate: number
  vatRate: number
  comprehensiveRate: number
  result: number
  systemAgency: number
  mismatch: boolean
}

export type CustomsFeeFormulaStep = {
  id: CustomsFeeFormulaStepId
  expr: CustomsFeeFormulaExpr | null
  mismatch: boolean
  /** 对照用的表格列数值；不一致时展示。 */
  tableValue: number | null
  reason: CustomsFeeFormulaUnavailableReason | null
  /** 仅 agency 步骤：报关公司公式再算一遍代理费。 */
  brokerAgency?: BrokerAgencyFormulaCheck | null
}

export type CustomsFeeFormulaTable = {
  customsPaymentGoods: number
  dutyAmount: number
  vatAmount: number
  customsAgencyFee: number
  totalValueTax: number
  taxIncludedUnitPrice: number
}

export type CustomsFeeFormulaDemoInput = {
  /** 头上的采购美金价模式为手工，且本行使用手工价。 */
  manualCostUsd: boolean
  /** 表格「采购美金价」列当前展示值（含未保存草稿）。 */
  displayedCostUsd: number
  originalPurchasePrice: number
  purchaseCurrency: number | null
  purchaseRatio: number | null
  exchangeRate: number
  declareQty: number
  dutyRate: number
  vatRate: number
  agencyRate: number
  otherFee: number
  financeFx: ExchangeRatesUsdBase | null
  table: CustomsFeeFormulaTable
}

const STEP_IDS: CustomsFeeFormulaStepId[] = [
  'costUsd',
  'goods',
  'duty',
  'vat',
  'agency',
  'total',
  'unit'
]

/** 与 C# Math.Round(..., MidpointRounding.AwayFromZero) 对齐（金额为非负数）。 */
export function roundAwayFromZero(value: number, digits: number): number {
  if (!Number.isFinite(value)) return Number.NaN
  const sign = value < 0 ? -1 : 1
  const factor = 10 ** digits
  const scaled = Math.abs(value) * factor
  const nearest = Math.round(scaled)
  if (Math.abs(scaled - nearest) < 1e-6) {
    return (sign * nearest) / factor
  }
  const base = Math.trunc(scaled)
  const frac = scaled - base
  const bump = frac > 0.5 || Math.abs(frac - 0.5) <= 1e-8
  return (sign * (base + (bump ? 1 : 0))) / factor
}

function round2(value: number): number {
  return roundAwayFromZero(value, 2)
}

function round6(value: number): number {
  return roundAwayFromZero(value, 6)
}

function sameAt(a: number, b: number, digits: number): boolean {
  if (!Number.isFinite(a) || !Number.isFinite(b)) return false
  return roundAwayFromZero(a, digits) === roundAwayFromZero(b, digits)
}

function unavailable(
  reason: CustomsFeeFormulaUnavailableReason,
  from: CustomsFeeFormulaStepId
): CustomsFeeFormulaStep[] {
  const start = STEP_IDS.indexOf(from)
  return STEP_IDS.slice(start).map((id) => ({
    id,
    expr: null,
    mismatch: false,
    tableValue: null,
    reason
  }))
}

function fxRateFor(currency: number, fx: ExchangeRatesUsdBase): number | null {
  switch (currency) {
    case CurrencyCode.RMB:
      return fx.usdToCny
    case CurrencyCode.EUR:
      return fx.usdToEur
    case CurrencyCode.HKD:
      return fx.usdToHkd
    default:
      return null
  }
}

function resolveSystemCost(
  input: CustomsFeeFormulaDemoInput
): Extract<CustomsFeeFormulaExpr, { kind: 'usd' | 'fx' }> | null {
  const p0 = Number(input.originalPurchasePrice)
  const currency = input.purchaseCurrency
  const ratio = input.purchaseRatio
  const fx = input.financeFx
  if (!(p0 > 0) || currency == null || ratio == null || !(ratio > 0) || fx == null) return null
  const p0Usd = unitLocalToUsd(p0, currency, fx)
  if (p0Usd == null) return null
  const result = Math.round(p0Usd * ratio * 1e6) / 1e6
  if (currency === CurrencyCode.USD) {
    return { kind: 'usd', p0Usd, ratio, result }
  }
  const fxRate = fxRateFor(currency, fx)
  if (fxRate == null || !(fxRate > 0)) return null
  return { kind: 'fx', p0, currency, fxRate, p0Usd, ratio, result }
}

function costStep(
  expr: Extract<CustomsFeeFormulaExpr, { kind: 'manual' | 'usd' | 'fx' | 'table-cost' }>,
  displayedCostUsd: number
): CustomsFeeFormulaStep {
  const mismatch =
    expr.kind === 'table-cost' ? false : !sameAt(expr.result, displayedCostUsd, 6)
  return { id: 'costUsd', expr, mismatch, tableValue: displayedCostUsd, reason: null }
}

/** 报关公司公式：一次乘完再 Round2；综合费率固定 0.3%。 */
export function buildBrokerAgencyFormulaCheck(input: {
  costUsd: number
  qty: number
  exchangeRate: number
  dutyRate: number
  vatRate: number
  systemAgency: number
}): BrokerAgencyFormulaCheck {
  const costUsd = Number(input.costUsd)
  const qty = Number(input.qty)
  const exchangeRate = Number(input.exchangeRate)
  const dutyRate = Number(input.dutyRate) || 0
  const vatRate = Number(input.vatRate) || 0
  const comprehensiveRate = BROKER_COMPREHENSIVE_FEE_RATE
  const raw =
    costUsd * qty * exchangeRate * (1 + dutyRate) * (1 + vatRate) * comprehensiveRate
  const result = round2(raw)
  const systemAgency = Number(input.systemAgency)
  return {
    costUsd,
    qty,
    exchangeRate,
    dutyRate,
    vatRate,
    comprehensiveRate,
    result,
    systemAgency,
    mismatch: !sameAt(result, systemAgency, 2)
  }
}

export function buildCustomsFeeLineDemo(input: CustomsFeeFormulaDemoInput): CustomsFeeFormulaStep[] {
  const displayed = Number(input.displayedCostUsd)
  let first: CustomsFeeFormulaStep
  let costUsd: number

  if (input.manualCostUsd) {
    if (!(displayed > 0)) return unavailable('missing-cost', 'costUsd')
    costUsd = round6(displayed)
    first = costStep({ kind: 'manual', result: costUsd }, displayed)
  } else {
    const system = resolveSystemCost(input)
    if (system) {
      costUsd = system.result
      first = costStep(system, displayed)
    } else if (displayed > 0) {
      costUsd = round6(displayed)
      first = costStep({ kind: 'table-cost', result: costUsd }, displayed)
    } else {
      return unavailable('missing-cost', 'costUsd')
    }
  }

  const qty = Number(input.declareQty)
  if (!(qty > 0)) return [first, ...unavailable('bad-qty', 'goods')]

  const exchangeRate = Number(input.exchangeRate)
  if (!(exchangeRate > 0)) return [first, ...unavailable('bad-rate', 'goods')]

  const goods = round2(costUsd * exchangeRate * qty)
  const dutyRate = Number(input.dutyRate) || 0
  const duty = round2(goods * dutyRate)
  const vatRate = Number(input.vatRate) || 0
  const vat = round2((goods + duty) * vatRate)

  const goodsStep: CustomsFeeFormulaStep = {
    id: 'goods',
    expr: { kind: 'goods', costUsd, rate: exchangeRate, qty, result: goods },
    mismatch: !sameAt(goods, input.table.customsPaymentGoods, 2),
    tableValue: Number(input.table.customsPaymentGoods),
    reason: null
  }
  const dutyStep: CustomsFeeFormulaStep = {
    id: 'duty',
    expr: { kind: 'duty', goods, dutyRate, result: duty },
    mismatch: !sameAt(duty, input.table.dutyAmount, 2),
    tableValue: Number(input.table.dutyAmount),
    reason: null
  }
  const vatStep: CustomsFeeFormulaStep = {
    id: 'vat',
    expr: { kind: 'vat', goods, duty, vatRate, result: vat },
    mismatch: !sameAt(vat, input.table.vatAmount, 2),
    tableValue: Number(input.table.vatAmount),
    reason: null
  }

  const agencyRate = Number(input.agencyRate)
  if (!(agencyRate >= 1)) {
    return [first, goodsStep, dutyStep, vatStep, ...unavailable('bad-agency', 'agency')]
  }

  const agency = round2((goods + duty + vat) * (agencyRate - 1))
  const other = Number(input.otherFee) || 0
  const total = round2(goods + duty + vat + agency + other)
  const unit = round6(total / qty)
  const brokerAgency = buildBrokerAgencyFormulaCheck({
    costUsd,
    qty,
    exchangeRate,
    dutyRate,
    vatRate,
    systemAgency: agency
  })

  return [
    first,
    goodsStep,
    dutyStep,
    vatStep,
    {
      id: 'agency',
      expr: { kind: 'agency', goods, duty, vat, agencyRate, result: agency },
      mismatch: !sameAt(agency, input.table.customsAgencyFee, 2),
      tableValue: Number(input.table.customsAgencyFee),
      reason: null,
      brokerAgency
    },
    {
      id: 'total',
      expr: { kind: 'total', goods, duty, vat, agency, other, result: total },
      mismatch: !sameAt(total, input.table.totalValueTax, 2),
      tableValue: Number(input.table.totalValueTax),
      reason: null
    },
    {
      id: 'unit',
      expr: { kind: 'unit', total, qty, result: unit },
      mismatch: !sameAt(unit, input.table.taxIncludedUnitPrice, 6),
      tableValue: Number(input.table.taxIncludedUnitPrice),
      reason: null
    }
  ]
}
