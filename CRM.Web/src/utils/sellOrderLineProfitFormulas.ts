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

function fmtGrossMargin(profitUsd: number | null | undefined, revenueUsd: number): string {
  if (profitUsd == null) return '—'
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
    useActualOutboundCost = false,
    effectiveOutboundAvgCostUsd,
    outboundCostLines = [],
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
  const costSource = lineProfit.salesExpectedCostSource ?? 'none'
  const salesCostUsd = lineProfit.salesExpectedCostUsd ?? poCostUsdConfirmed
  const costLabelKey =
    costSource === 'po'
      ? 'salesOrderDetailView.performance.formulas.salesCostLabelPo'
      : costSource === 'stocking'
        ? 'salesOrderDetailView.performance.formulas.salesCostLabelStocking'
        : costSource === 'quote'
          ? 'salesOrderDetailView.performance.formulas.salesCostLabelQuote'
          : 'salesOrderDetailView.performance.formulas.salesCostLabelNone'
  const costLabel = t(costLabelKey)
  const salesLines: SellOrderLineProfitFormulaLine[] = [
    {
      key: 'revenue',
      text: t('salesOrderDetailView.performance.formulas.revenueUsd', {
        qty: qtyText,
        convertPrice: convertPriceText,
        result: revenueText
      })
    }
  ]

  if (costSource === 'none' || salesExpected.profitUsd == null) {
    salesLines.push(
      {
        key: 'profit',
        text: t('salesOrderDetailView.performance.formulas.salesProfitUnavailable')
      },
      {
        key: 'rate',
        text: t('salesOrderDetailView.performance.formulas.salesRateUnavailable', {
          revenue: salesRevenueBasis,
          cost: fmtUsd2(salesCostUsd),
          costLabel
        })
      },
      {
        key: 'grossMargin',
        text: t('salesOrderDetailView.performance.formulas.grossMargin', {
          profit: '—',
          revenue: salesRevenueBasis,
          result: '—'
        })
      }
    )
  } else {
    salesLines.push(
      {
        key: 'profit',
        text: t('salesOrderDetailView.performance.formulas.salesProfit', {
          revenue: salesRevenueBasis,
          cost: fmtUsd2(salesCostUsd),
          costLabel,
          result: formatUsdProfitAmount(salesExpected.profitUsd)
        })
      },
      {
        key: 'rate',
        text:
          salesExpected.profitRate == null
            ? t('salesOrderDetailView.performance.formulas.salesRateUnavailable', {
                revenue: salesRevenueBasis,
                cost: fmtUsd2(salesCostUsd),
                costLabel
              })
            : t('salesOrderDetailView.performance.formulas.salesRate', {
                revenue: salesRevenueBasis,
                cost: fmtUsd2(salesCostUsd),
                costLabel,
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
    )
  }

  const outQtyText = fmtQty(qtyStockOutActual)
  const outboundLines: SellOrderLineProfitFormulaLine[] = []
  const outboundAvgCost = effectiveOutboundAvgCostUsd ?? avgPoCostUsd
  const outboundAvgCostText = fmtUsd6(outboundAvgCost)

  if (useActualOutboundCost && (outboundCostLines?.length ?? 0) > 0) {
    for (const [idx, line] of outboundCostLines.entries()) {
      const poLabel = line.purchaseOrderItemCode?.trim() || line.purchaseOrderItemId?.trim() || '—'
      outboundLines.push({
        key: `actualCostLine-${idx}`,
        text: t('salesOrderDetailView.performance.formulas.outboundActualCostLine', {
          poItem: poLabel,
          purchasePrice: fmtUsd6(line.purchasePriceUsd),
          qty: fmtQty(line.qty),
          result: fmtUsd2(line.costUsd)
        })
      })
    }
    outboundLines.push({
      key: 'actualCostTotal',
      text: t('salesOrderDetailView.performance.formulas.outboundActualCostTotal', {
        result: fmtUsd2(outboundCostUsd)
      })
    })
  } else if (poQtyTotal > 0) {
    outboundLines.push({
      key: 'avgCost',
      text: t('salesOrderDetailView.performance.formulas.avgPoCostUsd', {
        poCostTotal: fmtUsd2(poCostUsdTotal),
        poQty: fmtQty(poQtyTotal),
        result: outboundAvgCostText
      })
    })
    outboundLines.push({
      key: 'avgCostFallbackNote',
      text: t('salesOrderDetailView.performance.formulas.outboundCostFallbackWeighted')
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
      text: useActualOutboundCost
        ? t('salesOrderDetailView.performance.formulas.outboundCostUsdActual', {
            result: fmtUsd2(outboundCostUsd)
          })
        : t('salesOrderDetailView.performance.formulas.outboundCostUsd', {
            qty: outQtyText,
            avgCost: outboundAvgCostText,
            result: fmtUsd2(outboundCostUsd)
          })
    },
    {
      key: 'profit',
      text: useActualOutboundCost
        ? t('salesOrderDetailView.performance.formulas.outboundProfitActual', {
            revenue: fmtUsd2(outboundRevenueUsd),
            cost: fmtUsd2(outboundCostUsd),
            result: formatUsdProfitAmount(outbound.profitUsd)
          })
        : t('salesOrderDetailView.performance.formulas.outboundProfit', {
            convertPrice: convertPriceText,
            avgCost: outboundAvgCostText,
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
