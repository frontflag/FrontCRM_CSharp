import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'
import { buildQueryString } from '@/utils/progressStatusQuery'
import type { StockOutItemListQuery } from './stockOut'

export type StockOutItemListAnalyticsQuery = StockOutItemListQuery & {
  groupBy?: 'day' | 'week' | 'month'
}

export interface StockOutItemListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
  usdAmount?: number | null
}

export interface StockOutItemListAnalyticsDashboard {
  context: { maskAmounts: boolean; exchangeRateHint?: string | null }
  snapshot: {
    customerCount: number
    lineCount: number
    amountUsd?: number | null
    currencyLines: StockOutItemListAnalyticsCurrencyLine[]
  }
}

export interface StockOutItemListAnalyticsTrendPoint {
  period: string
  lineCount: number
  amountUsd?: number | null
}

export interface StockOutItemListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface StockOutItemListAnalyticsRankings {
  customerByAmount: StockOutItemListAnalyticsRankingRow[]
  salesUserByAmount: StockOutItemListAnalyticsRankingRow[]
}

function buildParams(q: StockOutItemListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.stockOutType !== undefined && q.stockOutType !== null) p.stockOutType = q.stockOutType
  if (q.stockOutCode) p.stockOutCode = q.stockOutCode
  if (q.stockOutItemCode) p.stockOutItemCode = q.stockOutItemCode
  if (q.stockInCode) p.stockInCode = q.stockInCode
  if (q.packingCode) p.packingCode = q.packingCode
  if (q.freightForwarderOrderNo) p.freightForwarderOrderNo = q.freightForwarderOrderNo
  if (q.stockOutDateFrom) p.stockOutDateFrom = q.stockOutDateFrom
  if (q.stockOutDateTo) p.stockOutDateTo = q.stockOutDateTo
  if (q.customerName) p.customerName = q.customerName
  if (q.salesUserName) p.salesUserName = q.salesUserName
  if (q.purchasePn) p.purchasePn = q.purchasePn
  if (q.sellOrderItemCode) p.sellOrderItemCode = q.sellOrderItemCode
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: StockOutItemListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/stock-out/items/analytics'

export const stockOutItemListAnalyticsApi = {
  getDashboard(query: StockOutItemListAnalyticsQuery): Promise<StockOutItemListAnalyticsDashboard> {
    return apiClient.get<StockOutItemListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: StockOutItemListAnalyticsQuery): Promise<StockOutItemListAnalyticsTrendPoint[]> {
    return apiClient.get<StockOutItemListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(query: StockOutItemListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>(analyticsUrl(`${BASE}/breakdowns`, query))
  },
  getRankings(query: StockOutItemListAnalyticsQuery): Promise<StockOutItemListAnalyticsRankings> {
    return apiClient.get<StockOutItemListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}
