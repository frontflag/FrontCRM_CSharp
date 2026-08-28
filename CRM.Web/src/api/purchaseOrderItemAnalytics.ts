import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'
import type { PurchaseOrderListAnalyticsCurrencyLine } from './purchaseOrderAnalytics'
import { buildQueryString } from '@/utils/progressStatusQuery'

export interface PurchaseOrderItemListAnalyticsQuery {
  startDate?: string
  endDate?: string
  purchaseOrderCode?: string
  freightForwarderOrderNo?: string
  vendorName?: string
  purchaseUserName?: string
  pn?: string
  orderType?: number
  transactionCurrency?: string
  paymentProgressStatus?: number | number[]
  purchaseProgressStatus?: number | number[]
  stockInProgressStatus?: number | number[]
  invoiceProgressStatus?: number | number[]
  /** 左栏快捷检索业务项 */
  quickFilter?: string
  groupBy?: 'day' | 'week' | 'month'
  dataset?: 'listFilter' | 'reportApproved'
  rankingSort?: 'amount' | 'count'
  rankingLineMetric?: 'lines' | 'transactions'
}

export interface PurchaseOrderItemListAnalyticsSnapshot {
  approvedVendorCount: number
  approvedOrderCount: number
  approvedLineCount: number
  approvedAmountUsd?: number | null
  currencyLines: PurchaseOrderListAnalyticsCurrencyLine[]
  inStockVendorCount: number
  inStockLineCount: number
  inStockAmountUsd?: number | null
  maxStockAgeDays?: number | null
  payableVendorCount: number
  payableLineCount: number
  payableAmountUsd?: number | null
  payableCurrencyLines: PurchaseOrderListAnalyticsCurrencyLine[]
}

export interface PurchaseOrderItemListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: PurchaseOrderItemListAnalyticsSnapshot
}

export interface PurchaseOrderItemListAnalyticsTrendPoint {
  period: string
  approvedOrderCount: number
  approvedLineCount: number
  approvedLineAmountUsd?: number | null
}

export interface PurchaseOrderItemListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
  transactionCount?: number
}

export interface PurchaseOrderItemListAnalyticsRankings {
  vendorByAmount: PurchaseOrderItemListAnalyticsRankingRow[]
  pnByAmount: PurchaseOrderItemListAnalyticsRankingRow[]
  pnByQty: PurchaseOrderItemListAnalyticsRankingRow[]
  brandByAmount: PurchaseOrderItemListAnalyticsRankingRow[]
  brandByQty: PurchaseOrderItemListAnalyticsRankingRow[]
  purchaseUserByAmount: PurchaseOrderItemListAnalyticsRankingRow[]
}

function buildParams(q: PurchaseOrderItemListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.purchaseOrderCode) p.purchaseOrderCode = q.purchaseOrderCode
  if (q.freightForwarderOrderNo) p.freightForwarderOrderNo = q.freightForwarderOrderNo
  if (q.vendorName) p.vendorName = q.vendorName
  if (q.purchaseUserName) p.purchaseUserName = q.purchaseUserName
  if (q.pn) p.pn = q.pn
  if (q.orderType !== undefined && q.orderType !== null) p.orderType = q.orderType
  if (q.transactionCurrency) p.transactionCurrency = q.transactionCurrency
  if (q.paymentProgressStatus !== undefined && q.paymentProgressStatus !== null) {
    p.paymentProgressStatus = q.paymentProgressStatus
  }
  if (q.purchaseProgressStatus !== undefined && q.purchaseProgressStatus !== null) {
    p.purchaseProgressStatus = q.purchaseProgressStatus
  }
  if (q.stockInProgressStatus !== undefined && q.stockInProgressStatus !== null) {
    p.stockInProgressStatus = q.stockInProgressStatus
  }
  if (q.invoiceProgressStatus !== undefined && q.invoiceProgressStatus !== null) {
    p.invoiceProgressStatus = q.invoiceProgressStatus
  }
  if (q.quickFilter) p.quickFilter = q.quickFilter
  if (q.groupBy) p.groupBy = q.groupBy
  if (q.dataset) p.dataset = q.dataset
  if (q.rankingSort) p.rankingSort = q.rankingSort
  if (q.rankingLineMetric) p.rankingLineMetric = q.rankingLineMetric
  return p
}

function analyticsUrl(path: string, query: PurchaseOrderItemListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

export const purchaseOrderItemListAnalyticsApi = {
  getDashboard(query: PurchaseOrderItemListAnalyticsQuery): Promise<PurchaseOrderItemListAnalyticsDashboard> {
    return apiClient.get<PurchaseOrderItemListAnalyticsDashboard>(
      analyticsUrl('/api/v1/purchase-orders/items/analytics/dashboard', query)
    )
  },

  getTrends(query: PurchaseOrderItemListAnalyticsQuery): Promise<PurchaseOrderItemListAnalyticsTrendPoint[]> {
    return apiClient.get<PurchaseOrderItemListAnalyticsTrendPoint[]>(
      analyticsUrl('/api/v1/purchase-orders/items/analytics/trends', query)
    )
  },

  getBreakdowns(query: PurchaseOrderItemListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>(
      analyticsUrl('/api/v1/purchase-orders/items/analytics/breakdowns', query)
    )
  },

  getRankings(query: PurchaseOrderItemListAnalyticsQuery): Promise<PurchaseOrderItemListAnalyticsRankings> {
    return apiClient.get<PurchaseOrderItemListAnalyticsRankings>(
      analyticsUrl('/api/v1/purchase-orders/items/analytics/rankings', query)
    )
  }
}
