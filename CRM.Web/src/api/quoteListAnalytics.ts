import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'

export interface QuoteListAnalyticsQuery {
  keyword?: string
  status?: number
  rfqItemId?: string
  startDate?: string
  endDate?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface QuoteListAnalyticsContext {
  maskCustomerNames: boolean
  maskVendorNames: boolean
}

export interface QuoteListAnalyticsSnapshot {
  quoteVendorCount: number
  validQuoteCount: number
  noQuoteFoundItemCount: number
  rfqQuoteRate?: number | null
  avgResponseMinutes?: number | null
  avgQuotesPerRfqItem?: number | null
  convertedLineCount: number
  quoteConversionRate?: number | null
}

export interface QuoteListAnalyticsDashboard {
  context: QuoteListAnalyticsContext
  snapshot: QuoteListAnalyticsSnapshot
}

export interface QuoteListAnalyticsTrendPoint {
  period: string
  quoteVendorCount: number
  rfqItemCount: number
  totalDemandCount: number
  validQuoteCount: number
}

export interface QuoteListAnalyticsRankingRow {
  id: string
  name: string
  orderCount: number
  amount?: number | null
}

export interface QuoteListAnalyticsRankings {
  vendorByRfqItemCount: QuoteListAnalyticsRankingRow[]
  purchaserByQuoteCount: QuoteListAnalyticsRankingRow[]
  purchaserByQuoteRate: QuoteListAnalyticsRankingRow[]
  mpnByQuoteCount: QuoteListAnalyticsRankingRow[]
  mpnByQty: QuoteListAnalyticsRankingRow[]
  brandByQuoteCount: QuoteListAnalyticsRankingRow[]
  brandByQty: QuoteListAnalyticsRankingRow[]
}

function buildParams(q: QuoteListAnalyticsQuery): Record<string, string | number> {
  const p: Record<string, string | number> = {}
  if (q.keyword) p.keyword = q.keyword
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.rfqItemId) p.rfqItemId = q.rfqItemId
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

export const quoteListAnalyticsApi = {
  getDashboard(query: QuoteListAnalyticsQuery): Promise<QuoteListAnalyticsDashboard> {
    return apiClient.get<QuoteListAnalyticsDashboard>('/api/v1/quotes/analytics/dashboard', {
      params: buildParams(query)
    })
  },

  getTrends(query: QuoteListAnalyticsQuery): Promise<QuoteListAnalyticsTrendPoint[]> {
    return apiClient.get<QuoteListAnalyticsTrendPoint[]>('/api/v1/quotes/analytics/trends', {
      params: buildParams(query)
    })
  },

  getBreakdowns(query: QuoteListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/quotes/analytics/breakdowns', {
      params: buildParams(query)
    })
  },

  getRankings(query: QuoteListAnalyticsQuery): Promise<QuoteListAnalyticsRankings> {
    return apiClient.get<QuoteListAnalyticsRankings>('/api/v1/quotes/analytics/rankings', {
      params: buildParams(query)
    })
  }
}
