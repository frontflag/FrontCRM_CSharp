import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'

export interface RfqListAnalyticsQuery {
  keyword?: string
  status?: number
  startDate?: string
  endDate?: string
  salesUserName?: string
  createUserName?: string
  tagIds?: string[]
  groupBy?: 'day' | 'week' | 'month'
}

export interface RfqListAnalyticsSnapshot {
  publishedCustomerCount: number
  repeatInquiryCustomerCount: number
  repeatInquiryRfqCount: number
  rfqCount: number
  rfqItemCount: number
  convertedLineCount: number
  conversionRate?: number | null
}

export interface RfqListAnalyticsDashboard {
  context: { maskCustomerNames: boolean }
  snapshot: RfqListAnalyticsSnapshot
}

export interface RfqListAnalyticsTrendPoint {
  period: string
  customerCount: number
  rfqCount: number
  rfqItemCount: number
}

export interface RfqListAnalyticsRankingRow {
  id: string
  name: string
  orderCount: number
}

export interface RfqListAnalyticsRankings {
  customerByLineCount: RfqListAnalyticsRankingRow[]
  salesUserByLineCount: RfqListAnalyticsRankingRow[]
}

function buildParams(q: RfqListAnalyticsQuery): Record<string, string | number> {
  const p: Record<string, string | number> = {}
  if (q.keyword) p.keyword = q.keyword
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.salesUserName) p.salesUserName = q.salesUserName
  if (q.createUserName) p.createUserName = q.createUserName
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function buildParamsWithTags(q: RfqListAnalyticsQuery): Record<string, string | number | string[]> {
  const p = buildParams(q) as Record<string, string | number | string[]>
  if (q.tagIds?.length) p.tagIds = q.tagIds
  return p
}

export const rfqListAnalyticsApi = {
  getDashboard(query: RfqListAnalyticsQuery): Promise<RfqListAnalyticsDashboard> {
    return apiClient.get<RfqListAnalyticsDashboard>('/api/v1/rfqs/analytics/dashboard', {
      params: buildParamsWithTags(query)
    })
  },

  getTrends(query: RfqListAnalyticsQuery): Promise<RfqListAnalyticsTrendPoint[]> {
    return apiClient.get<RfqListAnalyticsTrendPoint[]>('/api/v1/rfqs/analytics/trends', {
      params: buildParamsWithTags(query)
    })
  },

  getBreakdowns(query: RfqListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/rfqs/analytics/breakdowns', {
      params: buildParamsWithTags(query)
    })
  },

  getRankings(query: RfqListAnalyticsQuery): Promise<RfqListAnalyticsRankings> {
    return apiClient.get<RfqListAnalyticsRankings>('/api/v1/rfqs/analytics/rankings', {
      params: buildParamsWithTags(query)
    })
  }
}
