import type { RouteLocationRaw } from 'vue-router'
import type { PurchaseAnalyticsScopeContext } from '@/api/analytics/purchase'

export interface PurchaseAnalyticsDrillScope {
  dateFrom?: string
  dateTo?: string
  purchaseUserId?: string
  scopeContext?: PurchaseAnalyticsScopeContext | null
}

function scopePurchaseUserId(scope: PurchaseAnalyticsDrillScope): string | undefined {
  return scope.purchaseUserId?.trim() || scope.scopeContext?.resolvedPurchaseUserId?.trim() || undefined
}

function dateRangeQuery(scope: PurchaseAnalyticsDrillScope): Record<string, string> {
  const q: Record<string, string> = {}
  if (scope.dateFrom) q.startDate = scope.dateFrom
  if (scope.dateTo) q.endDate = scope.dateTo
  return q
}

function itemListDateQuery(scope: PurchaseAnalyticsDrillScope): Record<string, string> {
  const q: Record<string, string> = {}
  if (scope.dateFrom) q.orderCreateStart = scope.dateFrom
  if (scope.dateTo) q.orderCreateEnd = scope.dateTo
  return q
}

function scopeUserQuery(scope: PurchaseAnalyticsDrillScope): Record<string, string> {
  const uid = scopePurchaseUserId(scope)
  return uid ? { purchaseUserId: uid } : {}
}

export type PurchaseAnalyticsTodoDrillKey = 'payable' | 'pendingStockIn'

export function buildTodoDrillRoute(
  key: PurchaseAnalyticsTodoDrillKey,
  scope: PurchaseAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = scopeUserQuery(scope)
  switch (key) {
    case 'payable':
      return { name: 'FinancePaymentList', query: { onlyOpen: '1', ...userQ } }
    case 'pendingStockIn':
      return { name: 'PurchaseOrderItemList', query: { ...itemListDateQuery(scope), ...userQ } }
    default:
      return null
  }
}

export type PurchaseAnalyticsSnapshotDrillKey =
  | 'quoteItems'
  | 'quoteVendors'
  | 'poItems'
  | 'poVendors'
  | 'amount'
  | 'stockIn'
  | 'paid'

export function buildSnapshotDrillRoute(
  key: PurchaseAnalyticsSnapshotDrillKey,
  scope: PurchaseAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = scopeUserQuery(scope)
  switch (key) {
    case 'quoteItems':
    case 'quoteVendors':
      return { name: 'QuoteList', query: { ...dateRangeQuery(scope), ...userQ } }
    case 'poItems':
    case 'stockIn':
    case 'paid':
      return { name: 'PurchaseOrderItemList', query: { ...itemListDateQuery(scope), ...userQ } }
    case 'poVendors':
    case 'amount':
      return { name: 'PurchaseOrderList', query: { ...dateRangeQuery(scope), ...userQ } }
    default:
      return null
  }
}

export function buildVendorRankingDrillRoute(
  vendorId: string,
  vendorName: string,
  scope: PurchaseAnalyticsDrillScope
): RouteLocationRaw {
  const q: Record<string, string> = {
    ...dateRangeQuery(scope),
    ...scopeUserQuery(scope),
    vendorId
  }
  const name = vendorName?.trim()
  if (name) q.vendorName = name
  return { name: 'PurchaseOrderList', query: q }
}

export function buildPurchaseUserRankingDrillRoute(
  purchaseUserId: string,
  purchaseUserName: string,
  scope: PurchaseAnalyticsDrillScope
): RouteLocationRaw {
  const q: Record<string, string> = {
    ...dateRangeQuery(scope),
    purchaseUserId
  }
  const name = purchaseUserName?.trim()
  if (name) q.purchaseUserName = name
  return { name: 'PurchaseOrderList', query: q }
}

export function isTodoDrillable(key: PurchaseAnalyticsTodoDrillKey, maskAmounts: boolean): boolean {
  if (key === 'pendingStockIn') return true
  return !maskAmounts
}

export function isSnapshotDrillable(key: PurchaseAnalyticsSnapshotDrillKey, maskAmounts: boolean): boolean {
  if (key === 'quoteItems' || key === 'poItems') return true
  if (key === 'quoteVendors' || key === 'poVendors') return true
  return !maskAmounts
}
