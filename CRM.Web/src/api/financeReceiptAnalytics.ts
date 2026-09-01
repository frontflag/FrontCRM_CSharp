import apiClient from './client'
import { buildQueryString } from '@/utils/progressStatusQuery'

export type FinanceReceiptListAnalyticsQuery = {
  keyword?: string
  status?: number
  receiptPurpose?: number
  verificationStatus?: number
  receiptCurrency?: number
  receiptDateFrom?: string
  receiptDateTo?: string
  startDate?: string
  endDate?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface FinanceReceiptListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
}

export interface FinanceReceiptListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: {
    customerCount: number
    headerCount: number
    currencyLines: FinanceReceiptListAnalyticsCurrencyLine[]
  }
}

export interface FinanceReceiptListAnalyticsTrendCurrencyAmount {
  currencyKey: string
  currencyLabel: string
  amount?: number | null
}

export interface FinanceReceiptListAnalyticsTrendPoint {
  period: string
  headerCount: number
  amountsByCurrency: FinanceReceiptListAnalyticsTrendCurrencyAmount[]
}

export interface FinanceReceiptListAnalyticsBreakdownItem {
  key: string
  label: string
  value: number
  ratio: number
}

export interface FinanceReceiptListAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  currencyKey?: string | null
  currencyLabel?: string | null
  items: FinanceReceiptListAnalyticsBreakdownItem[]
}

export interface FinanceReceiptListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface FinanceReceiptListAnalyticsRankingFacet {
  currencyKey: string
  currencyLabel: string
  rows: FinanceReceiptListAnalyticsRankingRow[]
}

export interface FinanceReceiptListAnalyticsRankings {
  customerByAmount: FinanceReceiptListAnalyticsRankingFacet[]
  salesUserByAmount: FinanceReceiptListAnalyticsRankingFacet[]
}

function buildParams(q: FinanceReceiptListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.keyword) p.keyword = q.keyword
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.receiptPurpose !== undefined && q.receiptPurpose !== null) p.receiptPurpose = q.receiptPurpose
  if (q.verificationStatus !== undefined && q.verificationStatus !== null)
    p.verificationStatus = q.verificationStatus
  if (q.receiptCurrency !== undefined && q.receiptCurrency !== null) p.receiptCurrency = q.receiptCurrency
  if (q.receiptDateFrom) p.receiptDateFrom = q.receiptDateFrom
  if (q.receiptDateTo) p.receiptDateTo = q.receiptDateTo
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: FinanceReceiptListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/finance/receipts/analytics'

export const financeReceiptListAnalyticsApi = {
  getDashboard(query: FinanceReceiptListAnalyticsQuery): Promise<FinanceReceiptListAnalyticsDashboard> {
    return apiClient.get<FinanceReceiptListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: FinanceReceiptListAnalyticsQuery): Promise<FinanceReceiptListAnalyticsTrendPoint[]> {
    return apiClient.get<FinanceReceiptListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(query: FinanceReceiptListAnalyticsQuery): Promise<FinanceReceiptListAnalyticsBreakdownGroup[]> {
    return apiClient.get<FinanceReceiptListAnalyticsBreakdownGroup[]>(analyticsUrl(`${BASE}/breakdowns`, query))
  },
  getRankings(query: FinanceReceiptListAnalyticsQuery): Promise<FinanceReceiptListAnalyticsRankings> {
    return apiClient.get<FinanceReceiptListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}
