import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'

export interface PurchaseOrderListAnalyticsQuery {
  code?: string
  vendor?: string
  freightForwarderOrderNo?: string
  purchaseUserName?: string
  comment?: string
  orderType?: number
  status?: number
  startDate?: string
  endDate?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface PurchaseOrderListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
  usdAmount?: number | null
}

export interface PurchaseOrderListAnalyticsSnapshot {
  approvedVendorCount: number
  repeatVendorCount: number
  approvedOrderCount: number
  repeatOrderCount: number
  approvedAmountUsd?: number | null
  currencyLines: PurchaseOrderListAnalyticsCurrencyLine[]
}

export interface PurchaseOrderListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: PurchaseOrderListAnalyticsSnapshot
}

export interface PurchaseOrderListAnalyticsTrendPoint {
  period: string
  approvedOrderCount: number
  approvedAmountUsd?: number | null
}

export interface PurchaseOrderListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface PurchaseOrderListAnalyticsRankings {
  vendorByAmount: PurchaseOrderListAnalyticsRankingRow[]
  vendorByOrderCount: PurchaseOrderListAnalyticsRankingRow[]
  vendorByRepeatOrderCount: PurchaseOrderListAnalyticsRankingRow[]
  purchaseUserByAmount: PurchaseOrderListAnalyticsRankingRow[]
}

function buildParams(q: PurchaseOrderListAnalyticsQuery): Record<string, string | number> {
  const p: Record<string, string | number> = {}
  if (q.code) p.code = q.code
  if (q.vendor) p.vendor = q.vendor
  if (q.freightForwarderOrderNo) p.freightForwarderOrderNo = q.freightForwarderOrderNo
  if (q.purchaseUserName) p.purchaseUserName = q.purchaseUserName
  if (q.comment) p.comment = q.comment
  if (q.orderType !== undefined && q.orderType !== null) p.orderType = q.orderType
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

export const purchaseOrderListAnalyticsApi = {
  getDashboard(query: PurchaseOrderListAnalyticsQuery): Promise<PurchaseOrderListAnalyticsDashboard> {
    return apiClient.get<PurchaseOrderListAnalyticsDashboard>('/api/v1/purchase-orders/analytics/dashboard', {
      params: buildParams(query)
    })
  },

  getTrends(query: PurchaseOrderListAnalyticsQuery): Promise<PurchaseOrderListAnalyticsTrendPoint[]> {
    return apiClient.get<PurchaseOrderListAnalyticsTrendPoint[]>('/api/v1/purchase-orders/analytics/trends', {
      params: buildParams(query)
    })
  },

  getBreakdowns(query: PurchaseOrderListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/purchase-orders/analytics/breakdowns', {
      params: buildParams(query)
    })
  },

  getRankings(query: PurchaseOrderListAnalyticsQuery): Promise<PurchaseOrderListAnalyticsRankings> {
    return apiClient.get<PurchaseOrderListAnalyticsRankings>('/api/v1/purchase-orders/analytics/rankings', {
      params: buildParams(query)
    })
  }
}
