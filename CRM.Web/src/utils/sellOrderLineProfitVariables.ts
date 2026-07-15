import type { SellOrderLineProfit } from '@/api/salesOrder'
import { listAmountCurrencyIso } from '@/utils/moneyFormat'

export type SellOrderLineProfitVariableGroup = {
  key: string
  title: string
  items: SellOrderLineProfitVariableItem[]
}

export type SellOrderLineProfitVariableItem = {
  key: string
  label: string
  value: string
}

type VariableTranslator = (key: string, params?: Record<string, unknown>) => string

function fmtQty(value: number): string {
  if (!Number.isFinite(value)) return '—'
  if (Number.isInteger(value)) return String(value)
  return value.toFixed(4).replace(/\.?0+$/, '')
}

function fmtUnit6(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return value.toFixed(6)
}

function fmtUsd2(value: number): string {
  if (!Number.isFinite(value)) return '—'
  return value.toFixed(2)
}

function fmtFxRate(value: number): string {
  if (!Number.isFinite(value) || value <= 0) return '—'
  return value.toFixed(6)
}

function formatFxSnapshotLabel(currency: number, p: SellOrderLineProfit, t: VariableTranslator): string {
  if (currency === 2) return t('salesOrderDetailView.performance.variables.fxUsdNative')
  if (currency === 1) {
    return t('salesOrderDetailView.performance.variables.fxUsdToLocal', {
      local: 'RMB',
      rate: fmtFxRate(p.fxUsdToCnySnapshot)
    })
  }
  if (currency === 3) {
    return t('salesOrderDetailView.performance.variables.fxUsdToLocal', {
      local: 'EUR',
      rate: fmtFxRate(p.fxUsdToEurSnapshot)
    })
  }
  if (currency === 4) {
    return t('salesOrderDetailView.performance.variables.fxUsdToLocal', {
      local: 'HKD',
      rate: fmtFxRate(p.fxUsdToHkdSnapshot)
    })
  }
  return '—'
}

function formatUnitWithCurrency(unit: number, currency: number): string {
  const iso = listAmountCurrencyIso(currency)
  const unitText = fmtUnit6(unit)
  if (unitText === '—') return '—'
  return `${unitText} ${iso}`
}

/** 绩效面板顶部：公式所用变量对照表 */
export function buildSellOrderLineProfitVariableGroups(
  lineProfit: SellOrderLineProfit,
  t: VariableTranslator
): SellOrderLineProfitVariableGroup[] {
  const sellLineAmountLocal = Math.round(lineProfit.qty * lineProfit.sellPrice * 100) / 100

  const salesItems: SellOrderLineProfitVariableItem[] = [
    {
      key: 'sellQty',
      label: t('salesOrderDetailView.performance.variables.sellQty'),
      value: fmtQty(lineProfit.qty)
    },
    {
      key: 'sellPriceLocal',
      label: t('salesOrderDetailView.performance.variables.sellPriceLocal'),
      value: formatUnitWithCurrency(lineProfit.sellPrice, lineProfit.sellCurrency)
    },
    {
      key: 'sellFxSnapshot',
      label: t('salesOrderDetailView.performance.variables.sellFxSnapshot'),
      value: formatFxSnapshotLabel(lineProfit.sellCurrency, lineProfit, t)
    },
    {
      key: 'sellPriceUsd',
      label: t('salesOrderDetailView.performance.variables.sellPriceUsd'),
      value: `${fmtUnit6(lineProfit.convertPrice)} USD`
    },
    {
      key: 'sellLineAmountLocal',
      label: t('salesOrderDetailView.performance.variables.sellLineAmountLocal'),
      value: `${fmtUsd2(sellLineAmountLocal)} ${listAmountCurrencyIso(lineProfit.sellCurrency)}`
    },
    {
      key: 'revenueUsd',
      label: t('salesOrderDetailView.performance.variables.revenueUsd'),
      value: `${fmtUsd2(lineProfit.revenueUsd)} USD`
    }
  ]

  const quoteItems: SellOrderLineProfitVariableItem[] =
    lineProfit.quoteConvertCost > 0 || lineProfit.quoteCost > 0
      ? [
          {
            key: 'quotePriceLocal',
            label: t('salesOrderDetailView.performance.variables.quotePriceLocal'),
            value: formatUnitWithCurrency(lineProfit.quoteCost, lineProfit.quoteCurrency)
          },
          {
            key: 'quoteFxSnapshot',
            label: t('salesOrderDetailView.performance.variables.quoteFxSnapshot'),
            value: formatFxSnapshotLabel(lineProfit.quoteCurrency, lineProfit, t)
          },
          {
            key: 'quotePriceUsd',
            label: t('salesOrderDetailView.performance.variables.quotePriceUsd'),
            value: `${fmtUnit6(lineProfit.quoteConvertCost)} USD`
          },
          {
            key: 'quoteCostUsd',
            label: t('salesOrderDetailView.performance.variables.quoteCostUsd'),
            value: `${fmtUsd2(lineProfit.quoteCostUsd)} USD`
          }
        ]
      : [
          {
            key: 'quoteMissing',
            label: t('salesOrderDetailView.performance.variables.quotePriceLocal'),
            value: t('salesOrderDetailView.performance.variables.quoteMissing')
          }
        ]

  const purchaseItems: SellOrderLineProfitVariableItem[] = [
    {
      key: 'poQtyTotal',
      label: t('salesOrderDetailView.performance.variables.poQtyTotal'),
      value: fmtQty(lineProfit.poQtyTotal)
    },
    {
      key: 'poCostUsdTotal',
      label: t('salesOrderDetailView.performance.variables.poCostUsdTotal'),
      value: `${fmtUsd2(lineProfit.poCostUsdTotal)} USD`
    },
    {
      key: 'poCostUsdConfirmed',
      label: t('salesOrderDetailView.performance.variables.poCostUsdConfirmed'),
      value: `${fmtUsd2(lineProfit.poCostUsdConfirmed)} USD`
    }
  ]

  if ((lineProfit.poCostLines?.length ?? 0) > 0) {
    for (const [idx, line] of lineProfit.poCostLines!.entries()) {
      const poLabel =
        line.purchaseOrderItemCode?.trim() || line.purchaseOrderItemId?.trim() || `PO ${idx + 1}`
      purchaseItems.push({
        key: `poBatch-${idx}`,
        label: t('salesOrderDetailView.performance.variables.poBatchLine', { poItem: poLabel }),
        value: `${fmtUnit6(line.convertPriceUsd)} USD × ${fmtQty(line.qty)} = ${fmtUsd2(line.costUsd)} USD`
      })
    }
  } else if (lineProfit.poQtyTotal > 0) {
    purchaseItems.push({
      key: 'avgPoCostUsd',
      label: t('salesOrderDetailView.performance.variables.avgPoCostUsd'),
      value: `${fmtUnit6(lineProfit.avgPoCostUsd)} USD`
    })
  }

  const outboundItems: SellOrderLineProfitVariableItem[] = [
    {
      key: 'qtyStockOutActual',
      label: t('salesOrderDetailView.performance.variables.qtyStockOutActual'),
      value: fmtQty(lineProfit.qtyStockOutActual)
    },
    {
      key: 'outboundRevenueUsd',
      label: t('salesOrderDetailView.performance.variables.outboundRevenueUsd'),
      value: `${fmtUsd2(lineProfit.outboundRevenueUsd)} USD`
    }
  ]

  if (lineProfit.useActualOutboundCost && (lineProfit.outboundCostLines?.length ?? 0) > 0) {
    for (const [idx, line] of lineProfit.outboundCostLines!.entries()) {
      const poLabel =
        line.purchaseOrderItemCode?.trim() || line.purchaseOrderItemId?.trim() || `批次 ${idx + 1}`
      outboundItems.push({
        key: `outboundBatch-${idx}`,
        label: t('salesOrderDetailView.performance.variables.outboundBatchLine', { poItem: poLabel }),
        value: `${fmtUnit6(line.purchasePriceUsd)} USD × ${fmtQty(line.qty)} = ${fmtUsd2(line.costUsd)} USD`
      })
    }
  }

  outboundItems.push(
    {
      key: 'effectiveOutboundAvgCostUsd',
      label: lineProfit.useActualOutboundCost
        ? t('salesOrderDetailView.performance.variables.effectiveOutboundAvgCostUsd')
        : t('salesOrderDetailView.performance.variables.avgPoCostUsd'),
      value:
        lineProfit.qtyStockOutActual > 0 || (lineProfit.effectiveOutboundAvgCostUsd ?? lineProfit.avgPoCostUsd) > 0
          ? `${fmtUnit6(lineProfit.effectiveOutboundAvgCostUsd ?? lineProfit.avgPoCostUsd)} USD`
          : '—'
    },
    {
      key: 'outboundCostUsd',
      label: t('salesOrderDetailView.performance.variables.outboundCostUsd'),
      value: `${fmtUsd2(lineProfit.outboundCostUsd)} USD`
    }
  )

  return [
    {
      key: 'sales',
      title: t('salesOrderDetailView.performance.variables.groupSales'),
      items: salesItems
    },
    {
      key: 'quote',
      title: t('salesOrderDetailView.performance.variables.groupQuote'),
      items: quoteItems
    },
    {
      key: 'purchase',
      title: t('salesOrderDetailView.performance.variables.groupPurchase'),
      items: purchaseItems
    },
    {
      key: 'outbound',
      title: t('salesOrderDetailView.performance.variables.groupOutbound'),
      items: outboundItems
    }
  ]
}
