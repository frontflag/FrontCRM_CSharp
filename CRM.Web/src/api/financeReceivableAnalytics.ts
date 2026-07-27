import apiClient from './client'
import { buildQueryString } from '@/utils/progressStatusQuery'

export interface FinanceReceivableListAnalyticsQuery {
  keyword?: string
  customerId?: string
  verificationStatus?: number
  onlyOpen?: boolean
  stockOutDateFrom?: string
  stockOutDateTo?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface FinanceReceivableListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
  usdAmount?: number | null
}

export interface FinanceReceivableListAnalyticsSnapshot {
  customerCount: number
  lineCount: number
  pendingAmountUsd?: number | null
  pendingCurrencyLines: FinanceReceivableListAnalyticsCurrencyLine[]
  totalAmountUsd?: number | null
  totalCurrencyLines: FinanceReceivableListAnalyticsCurrencyLine[]
  maxReceivableAgeDays?: number | null
}

export interface FinanceReceivableListAnalyticsDashboard {
  context: { maskAmounts: boolean; exchangeRateHint?: string | null }
  snapshot: FinanceReceivableListAnalyticsSnapshot
}

export interface FinanceReceivableListAnalyticsTrendPoint {
  period: string
  customerCount: number
  lineCount: number
  pendingAmountUsd?: number | null
  totalAmountUsd?: number | null
}

export interface FinanceReceivableListAnalyticsBreakdownItem {
  key: string
  label: string
  pendingValue: number
  totalValue: number
  pendingRatio: number
  totalRatio: number
}

export interface FinanceReceivableListAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  agingPendingOnly?: boolean
  items: FinanceReceivableListAnalyticsBreakdownItem[]
}

export interface FinanceReceivableListAnalyticsRankingRow {
  id: string
  name: string
  pendingAmountUsd?: number | null
  totalAmountUsd?: number | null
  orderCount: number
  verificationStatus?: number | null
}

export interface FinanceReceivableListAnalyticsRankings {
  receivableByTotalAmount: FinanceReceivableListAnalyticsRankingRow[]
  customerByAmount: FinanceReceivableListAnalyticsRankingRow[]
  salesUserByAmount: FinanceReceivableListAnalyticsRankingRow[]
}

function buildParams(q: FinanceReceivableListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.keyword) p.keyword = q.keyword
  if (q.customerId) p.customerId = q.customerId
  if (q.verificationStatus !== undefined && q.verificationStatus !== null) {
    p.verificationStatus = q.verificationStatus
  }
  if (q.onlyOpen !== undefined && q.onlyOpen !== null) p.onlyOpen = q.onlyOpen
  if (q.stockOutDateFrom) p.stockOutDateFrom = q.stockOutDateFrom
  if (q.stockOutDateTo) p.stockOutDateTo = q.stockOutDateTo
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: FinanceReceivableListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/finance/receivables/analytics'

export const financeReceivableListAnalyticsApi = {
  getDashboard(query: FinanceReceivableListAnalyticsQuery): Promise<FinanceReceivableListAnalyticsDashboard> {
    return apiClient.get<FinanceReceivableListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: FinanceReceivableListAnalyticsQuery): Promise<FinanceReceivableListAnalyticsTrendPoint[]> {
    return apiClient.get<FinanceReceivableListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(
    query: FinanceReceivableListAnalyticsQuery
  ): Promise<FinanceReceivableListAnalyticsBreakdownGroup[]> {
    return apiClient.get<FinanceReceivableListAnalyticsBreakdownGroup[]>(
      analyticsUrl(`${BASE}/breakdowns`, query)
    )
  },
  getRankings(query: FinanceReceivableListAnalyticsQuery): Promise<FinanceReceivableListAnalyticsRankings> {
    return apiClient.get<FinanceReceivableListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}
