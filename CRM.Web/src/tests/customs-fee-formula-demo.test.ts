import { describe, expect, it } from 'vitest'
import { CurrencyCode } from '@/constants/currency'
import {
  buildCustomsFeeLineDemo,
  roundAwayFromZero,
  type CustomsFeeFormulaDemoInput,
  type CustomsFeeFormulaExpr
} from '@/utils/customsFeeFormulaDemo'

function exprOf(input: CustomsFeeFormulaDemoInput, id: string): CustomsFeeFormulaExpr {
  const step = buildCustomsFeeLineDemo(input).find((s) => s.id === id)
  expect(step?.expr).toBeTruthy()
  return step!.expr!
}

function resultOf(expr: CustomsFeeFormulaExpr): number {
  return expr.result
}

const fx = { usdToCny: 7.2, usdToHkd: 7.8, usdToEur: 0.92 }

function baseInput(overrides: Partial<CustomsFeeFormulaDemoInput> = {}): CustomsFeeFormulaDemoInput {
  return {
    manualCostUsd: false,
    displayedCostUsd: 11,
    originalPurchasePrice: 10,
    purchaseCurrency: CurrencyCode.USD,
    purchaseRatio: 1.1,
    exchangeRate: 7.2,
    declareQty: 2,
    dutyRate: 0.1,
    vatRate: 0.13,
    agencyRate: 1.03,
    otherFee: 5,
    financeFx: fx,
    table: {
      customsPaymentGoods: 158.4,
      dutyAmount: 15.84,
      vatAmount: 22.65,
      customsAgencyFee: 5.91,
      totalValueTax: 207.8,
      taxIncludedUnitPrice: 103.9
    },
    ...overrides
  }
}

describe('roundAwayFromZero', () => {
  it('rounds half away from zero at 2 decimals', () => {
    expect(roundAwayFromZero(1.005, 2)).toBe(1.01)
    expect(roundAwayFromZero(1.004, 2)).toBe(1)
    expect(roundAwayFromZero(158.4, 2)).toBe(158.4)
  })
})

describe('buildCustomsFeeLineDemo', () => {
  it('substitutes a USD system line and matches the saved table', () => {
    const steps = buildCustomsFeeLineDemo(baseInput())
    expect(steps.map((s) => s.mismatch)).toEqual([false, false, false, false, false, false, false])
    expect(resultOf(exprOf(baseInput(), 'costUsd'))).toBe(11)
    expect(resultOf(exprOf(baseInput(), 'goods'))).toBe(158.4)
    expect(resultOf(exprOf(baseInput(), 'duty'))).toBe(15.84)
    expect(resultOf(exprOf(baseInput(), 'vat'))).toBe(22.65)
    expect(resultOf(exprOf(baseInput(), 'agency'))).toBe(5.91)
    expect(resultOf(exprOf(baseInput(), 'total'))).toBe(207.8)
    expect(resultOf(exprOf(baseInput(), 'unit'))).toBe(103.9)
    expect(exprOf(baseInput(), 'costUsd').kind).toBe('usd')
  })

  it('converts RMB P0 with the current finance rate before the ratio', () => {
    const expr = exprOf(
      baseInput({
        originalPurchasePrice: 72,
        purchaseCurrency: CurrencyCode.RMB,
        purchaseRatio: 1,
        displayedCostUsd: 10
      }),
      'costUsd'
    )
    expect(expr.kind).toBe('fx')
    if (expr.kind !== 'fx') return
    expect(expr.p0Usd).toBe(10)
    expect(expr.fxRate).toBe(7.2)
    expect(expr.result).toBe(10)
  })

  it('uses the manual cost and skips P0', () => {
    const expr = exprOf(
      baseInput({
        manualCostUsd: true,
        displayedCostUsd: 8.5,
        originalPurchasePrice: 0
      }),
      'costUsd'
    )
    expect(expr.kind).toBe('manual')
    expect(expr.result).toBe(8.5)
  })

  it('marks a step when the recomputed amount differs from the table', () => {
    const steps = buildCustomsFeeLineDemo(
      baseInput({
        table: { ...baseInput().table, dutyAmount: 0 }
      })
    )
    expect(steps.find((s) => s.id === 'duty')?.mismatch).toBe(true)
    expect(steps.find((s) => s.id === 'duty')?.tableValue).toBe(0)
    expect(steps.find((s) => s.id === 'goods')?.mismatch).toBe(false)
    expect(steps.find((s) => s.id === 'vat')?.mismatch).toBe(false)
  })

  it('does not include inspection fee in the total', () => {
    const matched = resultOf(exprOf(baseInput({ otherFee: 5 }), 'total'))
    const zeroOther = resultOf(exprOf(baseInput({ otherFee: 0 }), 'total'))
    expect(roundAwayFromZero(matched - zeroOther, 2)).toBe(5)
  })

  it('feeds the rounded goods amount into duty', () => {
    const goods = resultOf(exprOf(baseInput(), 'goods'))
    const duty = exprOf(baseInput(), 'duty')
    expect(duty.kind).toBe('duty')
    if (duty.kind !== 'duty') return
    expect(duty.goods).toBe(goods)
    expect(duty.result).toBe(roundAwayFromZero(goods * 0.1, 2))
  })

  it('reports missing cost when P0 and the table cost are both empty', () => {
    const steps = buildCustomsFeeLineDemo(
      baseInput({
        originalPurchasePrice: 0,
        displayedCostUsd: 0,
        financeFx: null
      })
    )
    expect(steps.every((s) => s.reason === 'missing-cost')).toBe(true)
    expect(steps.every((s) => s.expr == null)).toBe(true)
  })

  it('continues from the table cost when P0 cannot be converted', () => {
    const expr = exprOf(
      baseInput({
        originalPurchasePrice: 0,
        displayedCostUsd: 11,
        financeFx: null
      }),
      'costUsd'
    )
    expect(expr.kind).toBe('table-cost')
    expect(expr.result).toBe(11)
    const steps = buildCustomsFeeLineDemo(
      baseInput({
        originalPurchasePrice: 0,
        displayedCostUsd: 11,
        financeFx: null
      })
    )
    expect(steps.find((s) => s.id === 'costUsd')?.mismatch).toBe(false)
    expect(steps.find((s) => s.id === 'goods')?.expr).toBeTruthy()
  })

  it('stops later steps when the exchange rate is missing', () => {
    const steps = buildCustomsFeeLineDemo(baseInput({ exchangeRate: 0 }))
    expect(steps.find((s) => s.id === 'costUsd')?.expr).toBeTruthy()
    expect(steps.find((s) => s.id === 'goods')?.reason).toBe('bad-rate')
    expect(steps.find((s) => s.id === 'unit')?.expr).toBeNull()
  })
})
