import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'

export interface SalesOrderListAnalyticsQuery {
  code?: string
  customer?: string
  salesUserName?: string
  comment?: string
  status?: number
  startDate?: string
  endDate?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface SalesOrderListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
  usdAmount?: number | null
}

export interface SalesOrderListAnalyticsSnapshot {
  approvedCustomerCount: number
  repeatCustomerCount: number
  approvedOrderCount: number
  repeatOrderCount: number
  approvedAmountUsd?: number | null
  currencyLines: SalesOrderListAnalyticsCurrencyLine[]
}

export interface SalesOrderListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: SalesOrderListAnalyticsSnapshot
}

export interface SalesOrderListAnalyticsTrendPoint {
  period: string
  approvedOrderCount: number
  approvedAmountUsd?: number | null
}

export interface SalesOrderListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface SalesOrderListAnalyticsRankings {
  customerByAmount: SalesOrderListAnalyticsRankingRow[]
  customerByOrderCount: SalesOrderListAnalyticsRankingRow[]
  customerByRepeatOrderCount: SalesOrderListAnalyticsRankingRow[]
  salesUserByAmount: SalesOrderListAnalyticsRankingRow[]
}

function buildParams(q: SalesOrderListAnalyticsQuery): Record<string, string | number> {
  const p: Record<string, string | number> = {}
  if (q.code) p.code = q.code
  if (q.customer) p.customer = q.customer
  if (q.salesUserName) p.salesUserName = q.salesUserName
  if (q.comment) p.comment = q.comment
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

export const salesOrderListAnalyticsApi = {
  getDashboard(query: SalesOrderListAnalyticsQuery): Promise<SalesOrderListAnalyticsDashboard> {
    return apiClient.get<SalesOrderListAnalyticsDashboard>('/api/v1/sales-orders/analytics/dashboard', {
      params: buildParams(query)
    })
  },

  getTrends(query: SalesOrderListAnalyticsQuery): Promise<SalesOrderListAnalyticsTrendPoint[]> {
    return apiClient.get<SalesOrderListAnalyticsTrendPoint[]>('/api/v1/sales-orders/analytics/trends', {
      params: buildParams(query)
    })
  },

  getBreakdowns(query: SalesOrderListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/sales-orders/analytics/breakdowns', {
      params: buildParams(query)
    })
  },

  getRankings(query: SalesOrderListAnalyticsQuery): Promise<SalesOrderListAnalyticsRankings> {
    return apiClient.get<SalesOrderListAnalyticsRankings>('/api/v1/sales-orders/analytics/rankings', {
      params: buildParams(query)
    })
  }
}
