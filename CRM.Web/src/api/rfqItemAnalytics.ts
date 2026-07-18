import apiClient from './client'
import type { RfqListAnalyticsDashboard, RfqListAnalyticsTrendPoint } from './rfqAnalytics'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'

export interface RfqItemListAnalyticsQuery {
  startDate?: string
  endDate?: string
  itemCreateStart?: string
  itemCreateEndExclusive?: string
  quoteCreateStart?: string
  quoteCreateEndExclusive?: string
  quickFilter?: string
  customerKeyword?: string
  materialModel?: string
  salesUserId?: string
  purchaserUserId?: string
  hasQuotesOnly?: boolean
  status?: number
  rfqCode?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface RfqItemListAnalyticsRankingRow {
  id: string
  name: string
  orderCount: number
}

export interface RfqItemListAnalyticsRankings {
  customerByLineCount: RfqItemListAnalyticsRankingRow[]
  salesUserByLineCount: RfqItemListAnalyticsRankingRow[]
  mpnByLineCount: RfqItemListAnalyticsRankingRow[]
  mpnByQty: RfqItemListAnalyticsRankingRow[]
  brandByLineCount: RfqItemListAnalyticsRankingRow[]
  brandByQty: RfqItemListAnalyticsRankingRow[]
}

function buildParams(q: RfqItemListAnalyticsQuery): Record<string, string | number | boolean> {
  const p: Record<string, string | number | boolean> = {}
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.itemCreateStart) p.itemCreateStart = q.itemCreateStart
  if (q.itemCreateEndExclusive) p.itemCreateEndExclusive = q.itemCreateEndExclusive
  if (q.quoteCreateStart) p.quoteCreateStart = q.quoteCreateStart
  if (q.quoteCreateEndExclusive) p.quoteCreateEndExclusive = q.quoteCreateEndExclusive
  if (q.quickFilter) p.quickFilter = q.quickFilter
  if (q.customerKeyword) p.customerKeyword = q.customerKeyword
  if (q.materialModel) p.materialModel = q.materialModel
  if (q.salesUserId) p.salesUserId = q.salesUserId
  if (q.purchaserUserId) p.purchaserUserId = q.purchaserUserId
  if (q.hasQuotesOnly) p.hasQuotesOnly = true
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.rfqCode) p.rfqCode = q.rfqCode
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

export const rfqItemListAnalyticsApi = {
  getDashboard(query: RfqItemListAnalyticsQuery): Promise<RfqListAnalyticsDashboard> {
    return apiClient.get<RfqListAnalyticsDashboard>('/api/v1/rfqs/items/analytics/dashboard', {
      params: buildParams(query)
    })
  },

  getTrends(query: RfqItemListAnalyticsQuery): Promise<RfqListAnalyticsTrendPoint[]> {
    return apiClient.get<RfqListAnalyticsTrendPoint[]>('/api/v1/rfqs/items/analytics/trends', {
      params: buildParams(query)
    })
  },

  getBreakdowns(query: RfqItemListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/rfqs/items/analytics/breakdowns', {
      params: buildParams(query)
    })
  },

  getRankings(query: RfqItemListAnalyticsQuery): Promise<RfqItemListAnalyticsRankings> {
    return apiClient.get<RfqItemListAnalyticsRankings>('/api/v1/rfqs/items/analytics/rankings', {
      params: buildParams(query)
    })
  }
}
