import type { RouteLocationRaw } from 'vue-router'
import type { FinanceAnalyticsScopeContext } from '@/api/analytics/finance'

export interface FinanceAnalyticsDrillScope {
  dateFrom?: string
  dateTo?: string
  ownerUserId?: string
  scopeContext?: FinanceAnalyticsScopeContext | null
}

export type FinanceAnalyticsTodoDrillKey =
  | 'payable'
  | 'receivable'
  | 'pendingPurchaseInvoice'
  | 'pendingSellInvoice'

export type FinanceAnalyticsCompletedDrillKey =
  | 'paid'
  | 'received'
  | 'issuedPurchaseInvoice'
  | 'issuedSellInvoice'

function scopeOwnerUserId(scope: FinanceAnalyticsDrillScope): string | undefined {
  return scope.ownerUserId?.trim() || scope.scopeContext?.resolvedOwnerUserId?.trim() || undefined
}

function dateRangeQuery(scope: FinanceAnalyticsDrillScope): Record<string, string> {
  const q: Record<string, string> = {}
  if (scope.dateFrom) q.startDate = scope.dateFrom
  if (scope.dateTo) q.endDate = scope.dateTo
  return q
}

function ownerQuery(scope: FinanceAnalyticsDrillScope): Record<string, string> {
  const uid = scopeOwnerUserId(scope)
  return uid ? { ownerUserId: uid } : {}
}

export function buildTodoDrillRoute(
  key: FinanceAnalyticsTodoDrillKey,
  scope: FinanceAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = ownerQuery(scope)
  switch (key) {
    case 'payable':
      return { name: 'FinancePaymentList', query: { onlyOpen: '1', ...userQ } }
    case 'receivable':
      return { name: 'FinanceReceivableList', query: { onlyOpen: '1', ...userQ } }
    case 'pendingPurchaseInvoice':
      return { name: 'FinancePurchaseInvoiceList', query: { onlyOpen: '1', ...userQ } }
    case 'pendingSellInvoice':
      return { name: 'FinanceSellInvoiceList', query: { onlyOpen: '1', ...userQ } }
    default:
      return null
  }
}

export function buildCompletedDrillRoute(
  key: FinanceAnalyticsCompletedDrillKey,
  scope: FinanceAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = ownerQuery(scope)
  const dateQ = dateRangeQuery(scope)
  switch (key) {
    case 'paid':
      return { name: 'FinancePaymentList', query: { ...dateQ, ...userQ } }
    case 'received':
      return { name: 'FinanceReceiptList', query: { ...dateQ, ...userQ } }
    case 'issuedPurchaseInvoice':
      return { name: 'FinancePurchaseInvoiceList', query: { ...dateQ, ...userQ } }
    case 'issuedSellInvoice':
      return { name: 'FinanceSellInvoiceList', query: { ...dateQ, ...userQ } }
    default:
      return null
  }
}

export function isTodoDrillable(key: FinanceAnalyticsTodoDrillKey, maskAmounts: boolean): boolean {
  switch (key) {
    case 'payable':
    case 'receivable':
    case 'pendingPurchaseInvoice':
    case 'pendingSellInvoice':
      return !maskAmounts
  }
}

export function isCompletedDrillable(key: FinanceAnalyticsCompletedDrillKey, maskAmounts: boolean): boolean {
  switch (key) {
    case 'paid':
    case 'received':
    case 'issuedPurchaseInvoice':
    case 'issuedSellInvoice':
      return !maskAmounts
  }
}
