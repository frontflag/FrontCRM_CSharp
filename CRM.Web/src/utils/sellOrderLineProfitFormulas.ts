import type { SellOrderLineProfit } from '@/api/salesOrder'
import {
  computeGrossMarginPercent,
  formatProfitRateMultiplierDisplay,
  formatUsdProfitAmount
} from '@/utils/sellOrderLineProfitDisplay'

export type SellOrderLineProfitFormulaLine = {
  key: string
  /** 带真实数值代入后的完整公式行 */
  text: string
}

type FormulaTranslator = (key: string, params?: Record<string, unknown>) => string

function fmtQty(value: number): string {
  if (!Number.isFinite(value)) return '—'
  if (Number.isInteger(value)) return String(value)
  return value.toFixed(4).replace(/\.?0+$/, '')
}

function fmtUsd2(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return value.toFixed(2)
}

function fmtUsd6(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return value.toFixed(6)
}

function fmtRate(value?: number | null): string {
  return formatProfitRateMultiplierDisplay(0, value)
}

function fmtGrossMargin(profitUsd: number, revenueUsd: number): string {
  const pct = computeGrossMarginPercent(profitUsd, revenueUsd)
  if (pct == null) return '—'
  return `${pct.toFixed(2)}%`
}

export type SellOrderLineProfitLayerFormulas = {
  key: 'quote' | 'salesExpected' | 'outbound'
  lines: SellOrderLineProfitFormulaLine[]
}

export function buildSellOrderLineProfitLayerFormulas(
  lineProfit: SellOrderLineProfit,
  t: FormulaTranslator
): SellOrderLineProfitLayerFormulas[] {
  const {
    qty,
    convertPrice,
    quoteConvertCost,
    revenueUsd,
    quoteCostUsd,
    poCostUsdConfirmed,
    qtyStockOutActual,
    poQtyTotal,
    poCostUsdTotal,
    avgPoCostUsd,
    outboundRevenueUsd,
    outboundCostUsd,
    quote,
    salesExpected,
    outbound
  } = lineProfit

  const qtyText = fmtQty(qty)
  const convertPriceText = fmtUsd6(convertPrice)
  const revenueText = fmtUsd2(revenueUsd)

  const quoteLines: SellOrderLineProfitFormulaLine[] = [
    {
      key: 'revenue',
      text: t('salesOrderDetailView.performance.formulas.revenueUsd', {
        qty: qtyText,
        convertPrice: convertPriceText,
        result: revenueText
      })
    }
  ]

  if (quoteConvertCost > 0) {
    quoteLines.push(
      {
        key: 'quoteCost',
        text: t('salesOrderDetailView.performance.formulas.quoteCostUsd', {
          qty: qtyText,
          quoteConvertCost: fmtUsd6(quoteConvertCost),
          result: fmtUsd2(quoteCostUsd)
        })
      },
      {
        key: 'profit',
        text: t('salesOrderDetailView.performance.formulas.quoteProfit', {
          revenue: revenueText,
          cost: fmtUsd2(quoteCostUsd),
          result: formatUsdProfitAmount(quote.profitUsd)
        })
      },
      {
        key: 'rate',
        text:
          quote.profitRate == null
            ? t('salesOrderDetailView.performance.formulas.quoteRateUnavailable', {
                revenue: revenueText,
                cost: fmtUsd2(quoteCostUsd)
              })
            : t('salesOrderDetailView.performance.formulas.quoteRate', {
                revenue: revenueText,
                cost: fmtUsd2(quoteCostUsd),
                result: fmtRate(quote.profitRate)
              })
      }
    )
  } else {
    quoteLines.push(
      {
        key: 'quoteCostMissing',
        text: t('salesOrderDetailView.performance.formulas.quoteCostMissing')
      },
      {
        key: 'profit',
        text: t('salesOrderDetailView.performance.formulas.quoteProfitFallback', {
          result: formatUsdProfitAmount(quote.profitUsd)
        })
      },
      {
        key: 'rate',
        text: t('salesOrderDetailView.performance.formulas.quoteRateUnavailable', {
          revenue: revenueText,
          cost: fmtUsd2(quoteCostUsd)
        })
      }
    )
  }

  const salesRevenueBasis = revenueText
  const salesLines: SellOrderLineProfitFormulaLine[] = [
    {
      key: 'revenue',
      text: t('salesOrderDetailView.performance.formulas.revenueUsd', {
        qty: qtyText,
        convertPrice: convertPriceText,
        result: revenueText
      })
    },
    {
      key: 'profit',
      text:
        poCostUsdConfirmed <= 0
          ? t('salesOrderDetailView.performance.formulas.salesProfitNoCost', {
              revenue: salesRevenueBasis,
              cost: fmtUsd2(poCostUsdConfirmed),
              result: formatUsdProfitAmount(salesExpected.profitUsd)
            })
          : t('salesOrderDetailView.performance.formulas.salesProfit', {
              revenue: salesRevenueBasis,
              cost: fmtUsd2(poCostUsdConfirmed),
              result: formatUsdProfitAmount(salesExpected.profitUsd)
            })
    },
    {
      key: 'rate',
      text:
        salesExpected.profitRate == null
          ? t('salesOrderDetailView.performance.formulas.salesRateUnavailable', {
              revenue: salesRevenueBasis,
              cost: fmtUsd2(poCostUsdConfirmed)
            })
          : t('salesOrderDetailView.performance.formulas.salesRate', {
              revenue: salesRevenueBasis,
              cost: fmtUsd2(poCostUsdConfirmed),
              result: fmtRate(salesExpected.profitRate)
            })
    },
    {
      key: 'grossMargin',
      text: t('salesOrderDetailView.performance.formulas.grossMargin', {
        profit: formatUsdProfitAmount(salesExpected.profitUsd),
        revenue: salesRevenueBasis,
        result: fmtGrossMargin(salesExpected.profitUsd, revenueUsd)
      })
    }
  ]

  const outQtyText = fmtQty(qtyStockOutActual)
  const outboundLines: SellOrderLineProfitFormulaLine[] = []
  if (poQtyTotal > 0) {
    outboundLines.push({
      key: 'avgCost',
      text: t('salesOrderDetailView.performance.formulas.avgPoCostUsd', {
        poCostTotal: fmtUsd2(poCostUsdTotal),
        poQty: fmtQty(poQtyTotal),
        result: fmtUsd6(avgPoCostUsd)
      })
    })
  } else {
    outboundLines.push({
      key: 'avgCostMissing',
      text: t('salesOrderDetailView.performance.formulas.avgPoCostMissing')
    })
  }
  outboundLines.push(
    {
      key: 'outRevenue',
      text: t('salesOrderDetailView.performance.formulas.outboundRevenueUsd', {
        qty: outQtyText,
        convertPrice: convertPriceText,
        result: fmtUsd2(outboundRevenueUsd)
      })
    },
    {
      key: 'outCost',
      text: t('salesOrderDetailView.performance.formulas.outboundCostUsd', {
        qty: outQtyText,
        avgCost: fmtUsd6(avgPoCostUsd),
        result: fmtUsd2(outboundCostUsd)
      })
    },
    {
      key: 'profit',
      text: t('salesOrderDetailView.performance.formulas.outboundProfit', {
        convertPrice: convertPriceText,
        avgCost: fmtUsd6(avgPoCostUsd),
        qty: outQtyText,
        result: formatUsdProfitAmount(outbound.profitUsd)
      })
    },
    {
      key: 'rate',
      text:
        outbound.profitRate == null
          ? t('salesOrderDetailView.performance.formulas.outboundRateUnavailable', {
              revenue: fmtUsd2(outboundRevenueUsd),
              cost: fmtUsd2(outboundCostUsd)
            })
          : t('salesOrderDetailView.performance.formulas.outboundRate', {
              revenue: fmtUsd2(outboundRevenueUsd),
              cost: fmtUsd2(outboundCostUsd),
              result: fmtRate(outbound.profitRate)
            })
    },
    {
      key: 'grossMargin',
      text: t('salesOrderDetailView.performance.formulas.grossMarginOutbound', {
        profit: formatUsdProfitAmount(outbound.profitUsd),
        revenue: fmtUsd2(outboundRevenueUsd),
        result: fmtGrossMargin(outbound.profitUsd, outboundRevenueUsd)
      })
    }
  )

  // 报价层毛利率基于整单销售收入
  quoteLines.push({
    key: 'grossMargin',
    text: t('salesOrderDetailView.performance.formulas.grossMargin', {
      profit: formatUsdProfitAmount(quote.profitUsd),
      revenue: revenueText,
      result: fmtGrossMargin(quote.profitUsd, revenueUsd)
    })
  })

  return [
    { key: 'quote', lines: quoteLines },
    { key: 'salesExpected', lines: salesLines },
    { key: 'outbound', lines: outboundLines }
  ]
}
