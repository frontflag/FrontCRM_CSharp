import type { RouteLocationRaw } from 'vue-router'
import type { SalesAnalyticsScopeContext } from '@/api/analytics/sales'

/** 看板当前透镜，用于生成业务列表 query（与后端 DataScope 一致，列表页自行再过滤）。 */
export interface SalesAnalyticsDrillScope {
  dateFrom?: string
  dateTo?: string
  salesUserId?: string
  scopeContext?: SalesAnalyticsScopeContext | null
}

function scopeSalesUserId(scope: SalesAnalyticsDrillScope): string | undefined {
  return scope.salesUserId?.trim() || scope.scopeContext?.resolvedSalesUserId?.trim() || undefined
}

function dateRangeQuery(scope: SalesAnalyticsDrillScope): Record<string, string> {
  const q: Record<string, string> = {}
  if (scope.dateFrom) q.startDate = scope.dateFrom
  if (scope.dateTo) q.endDate = scope.dateTo
  return q
}

function itemListDateQuery(scope: SalesAnalyticsDrillScope): Record<string, string> {
  const q: Record<string, string> = {}
  if (scope.dateFrom) q.orderCreateStart = scope.dateFrom
  if (scope.dateTo) q.orderCreateEnd = scope.dateTo
  return q
}

function scopeUserQuery(scope: SalesAnalyticsDrillScope): Record<string, string> {
  const uid = scopeSalesUserId(scope)
  return uid ? { salesUserId: uid } : {}
}

export type SalesAnalyticsTodoDrillKey = 'receivable' | 'pendingStockOut' | 'pendingInvoice'

export function buildTodoDrillRoute(
  key: SalesAnalyticsTodoDrillKey,
  scope: SalesAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = scopeUserQuery(scope)
  switch (key) {
    case 'receivable':
      return {
        name: 'FinanceReceivableList',
        query: { onlyOpen: '1' }
      }
    case 'pendingStockOut':
      return {
        name: 'SalesOrderItemList',
        query: { stockOutPending: '1', ...userQ }
      }
    case 'pendingInvoice':
      return {
        name: 'SalesOrderItemList',
        query: { invoicePending: '1', ...userQ }
      }
    default:
      return null
  }
}

export type SalesAnalyticsSnapshotDrillKey =
  | 'rfqItems'
  | 'rfqCustomers'
  | 'soItems'
  | 'soCustomers'
  | 'amount'
  | 'stockOut'
  | 'received'

export function buildSnapshotDrillRoute(
  key: SalesAnalyticsSnapshotDrillKey,
  scope: SalesAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = scopeUserQuery(scope)
  switch (key) {
    case 'rfqItems':
    case 'rfqCustomers':
      return {
        name: 'RFQItemList',
        query: { ...dateRangeQuery(scope), ...userQ }
      }
    case 'soItems':
    case 'stockOut':
    case 'received':
      return {
        name: 'SalesOrderItemList',
        query: { ...itemListDateQuery(scope), ...userQ }
      }
    case 'soCustomers':
    case 'amount':
      return {
        name: 'SalesOrderList',
        query: { ...dateRangeQuery(scope), ...userQ }
      }
    default:
      return null
  }
}

export function buildCustomerRankingDrillRoute(
  customerId: string,
  customerName: string,
  scope: SalesAnalyticsDrillScope
): RouteLocationRaw {
  const q: Record<string, string> = {
    ...dateRangeQuery(scope),
    ...scopeUserQuery(scope),
    customerId
  }
  const name = customerName?.trim()
  if (name) q.customer = name
  return { name: 'SalesOrderList', query: q }
}

export function buildSalesUserRankingDrillRoute(
  salesUserId: string,
  salesUserName: string,
  scope: SalesAnalyticsDrillScope
): RouteLocationRaw {
  const q: Record<string, string> = {
    ...dateRangeQuery(scope),
    salesUserId
  }
  const name = salesUserName?.trim()
  if (name) q.salesUserName = name
  return { name: 'SalesOrderList', query: q }
}

export function isTodoDrillable(key: SalesAnalyticsTodoDrillKey, maskAmounts: boolean): boolean {
  if (key === 'pendingStockOut') return true
  return !maskAmounts
}

export function isSnapshotDrillable(key: SalesAnalyticsSnapshotDrillKey, maskAmounts: boolean): boolean {
  if (key === 'rfqItems' || key === 'soItems') return true
  if (key === 'rfqCustomers' || key === 'soCustomers') return true
  return !maskAmounts
}
