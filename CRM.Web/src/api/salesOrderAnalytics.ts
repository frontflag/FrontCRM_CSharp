import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'
import { buildQueryString } from '@/utils/progressStatusQuery'
import { assignSalesOrderStatusesParam } from '@/utils/salesOrderStatusQuery'

export interface SalesOrderListAnalyticsQuery {
  code?: string
  customer?: string
  salesUserName?: string
  comment?: string
  /** 主状态多选；空/未传表示不限 */
  status?: number[]
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

function buildQuery(q: SalesOrderListAnalyticsQuery): string {
  const p: Record<string, unknown> = {}
  if (q.code) p.code = q.code
  if (q.customer) p.customer = q.customer
  if (q.salesUserName) p.salesUserName = q.salesUserName
  if (q.comment) p.comment = q.comment
  assignSalesOrderStatusesParam(p, 'status', q.status)
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.groupBy) p.groupBy = q.groupBy
  return buildQueryString(p)
}

export const salesOrderListAnalyticsApi = {
  getDashboard(query: SalesOrderListAnalyticsQuery): Promise<SalesOrderListAnalyticsDashboard> {
    const qs = buildQuery(query)
    return apiClient.get<SalesOrderListAnalyticsDashboard>(
      `/api/v1/sales-orders/analytics/dashboard${qs ? `?${qs}` : ''}`
    )
  },

  getTrends(query: SalesOrderListAnalyticsQuery): Promise<SalesOrderListAnalyticsTrendPoint[]> {
    const qs = buildQuery(query)
    return apiClient.get<SalesOrderListAnalyticsTrendPoint[]>(
      `/api/v1/sales-orders/analytics/trends${qs ? `?${qs}` : ''}`
    )
  },

  getBreakdowns(query: SalesOrderListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    const qs = buildQuery(query)
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>(
      `/api/v1/sales-orders/analytics/breakdowns${qs ? `?${qs}` : ''}`
    )
  },

  getRankings(query: SalesOrderListAnalyticsQuery): Promise<SalesOrderListAnalyticsRankings> {
    const qs = buildQuery(query)
    return apiClient.get<SalesOrderListAnalyticsRankings>(
      `/api/v1/sales-orders/analytics/rankings${qs ? `?${qs}` : ''}`
    )
  }
}
