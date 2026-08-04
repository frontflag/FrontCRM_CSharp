import type { SellOrderLineProfit } from '@/api/salesOrder'
import { extendTriLabel, type SellOrderExtendProgressKind } from '@/utils/sellOrderItemOpsPanel'

export type SellOrderLineProfitHint = {
  level: 'quote' | 'salesExpected' | 'outbound' | 'general'
  type: 'info' | 'warning'
  message: string
}

type HintTranslator = (key: string, params?: Record<string, unknown>) => string

const EPS = 0.005
const RATE_ONE_EPS = 0.000001

function isNearZero(n: number | null | undefined): boolean {
  const v = Number(n ?? 0)
  if (!Number.isFinite(v)) return false
  return Math.abs(v) < EPS
}

function isNearRateOne(rate?: number | null): boolean {
  if (rate == null || !Number.isFinite(rate)) return false
  return Math.abs(rate - 1) <= RATE_ONE_EPS
}

function progressLabel(
  t: HintTranslator,
  kind: SellOrderExtendProgressKind,
  status?: number
): string {
  return extendTriLabel(t as (key: string, ...args: unknown[]) => string, kind, status ?? 0)
}

/** 根据 lineProfit 快照生成固定规则异常说明（不链帮助文档）。 */
export function buildSellOrderLineProfitHints(
  lineProfit: SellOrderLineProfit | null | undefined,
  t: HintTranslator
): SellOrderLineProfitHint[] {
  if (!lineProfit) return []

  const hints: SellOrderLineProfitHint[] = []
  const {
    revenueUsd,
    quoteCostUsd,
    poCostUsdTotal,
    poCostUsdConfirmed,
    qtyStockOutActual,
    purchaseProgressStatus,
    stockOutProgressStatus,
    useActualOutboundCost = false,
    salesExpectedCostSource,
    quote,
    salesExpected,
    outbound
  } = lineProfit

  const costSource = salesExpectedCostSource ?? 'none'
  const purchaseStatus = progressLabel(t, 'purchase', purchaseProgressStatus)
  const stockOutStatus = progressLabel(t, 'stockOut', stockOutProgressStatus)

  if (quoteCostUsd <= 0 && quote.profitRate == null) {
    hints.push({
      level: 'quote',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.noQuoteCost')
    })
  } else if (quoteCostUsd > 0 && isNearZero(quote.profitUsd) && isNearRateOne(quote.profitRate)) {
    hints.push({
      level: 'quote',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.quoteBreakEven')
    })
  }

  if (costSource === 'none' || salesExpected.profitUsd == null) {
    hints.push({
      level: 'salesExpected',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.noConfirmedPoCost', { purchaseStatus })
    })
  } else if (isNearZero(Number(salesExpected.profitUsd))) {
    hints.push({
      level: 'salesExpected',
      type: 'info',
      message: isNearRateOne(salesExpected.profitRate)
        ? t('salesOrderDetailView.performance.hints.salesBreakEvenParity')
        : t('salesOrderDetailView.performance.hints.salesBreakEven')
    })
  }

  if (costSource === 'stocking') {
    hints.push({
      level: 'salesExpected',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.salesUsingStockingCost')
    })
  } else if (costSource === 'quote') {
    hints.push({
      level: 'salesExpected',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.salesUsingQuoteCost')
    })
  } else if (costSource === 'po' && poCostUsdTotal > poCostUsdConfirmed + EPS) {
    hints.push({
      level: 'salesExpected',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.unconfirmedPoCost', { purchaseStatus })
    })
  }

  if (qtyStockOutActual <= 0 && isNearZero(outbound.profitUsd)) {
    hints.push({
      level: 'outbound',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.notShippedYet', { stockOutStatus })
    })
  } else if (qtyStockOutActual > 0 && isNearZero(outbound.profitUsd)) {
    if (isNearRateOne(outbound.profitRate)) {
      hints.push({
        level: 'outbound',
        type: 'info',
        message: t('salesOrderDetailView.performance.hints.outboundBreakEven', {
          qty: qtyStockOutActual
        })
      })
    } else if (outbound.profitRate != null) {
      hints.push({
        level: 'outbound',
        type: 'info',
        message: t('salesOrderDetailView.performance.hints.outboundZeroProfitShipped', {
          qty: qtyStockOutActual
        })
      })
    }
  }

  if (!useActualOutboundCost && qtyStockOutActual > 0) {
    hints.push({
      level: 'outbound',
      type: 'info',
      message: t('salesOrderDetailView.performance.hints.outboundCostFallbackWeighted')
    })
  }

  if (outbound.profitRate == null && (outbound.profitUsd ?? 0) > EPS) {
    hints.push({
      level: 'outbound',
      type: 'warning',
      message: t('salesOrderDetailView.performance.hints.outboundNoCostBaseline', { purchaseStatus })
    })
  }

  if (revenueUsd <= 0) {
    hints.push({
      level: 'general',
      type: 'warning',
      message: t('salesOrderDetailView.performance.hints.noRevenue')
    })
  }

  return hints
}
