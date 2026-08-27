import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'
import { buildQueryString } from '@/utils/progressStatusQuery'

export type StockInListAnalyticsQuery = {
  model?: string
  vendorName?: string
  purchaseOrderCode?: string
  freightForwarderOrderNo?: string
  salesOrderCode?: string
  stockInCode?: string
  sourceDisplayNo?: string
  warehouseId?: string
  stockInDateStart?: string
  stockInDateEnd?: string
  remark?: string
  stockInType?: number
  groupBy?: 'day' | 'week' | 'month'
}

export interface StockInListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
  usdAmount?: number | null
}

export interface StockInListAnalyticsDashboard {
  context: { maskAmounts: boolean; exchangeRateHint?: string | null }
  snapshot: {
    vendorCount: number
    headerCount: number
    amountUsd?: number | null
    currencyLines: StockInListAnalyticsCurrencyLine[]
  }
}

export interface StockInListAnalyticsTrendPoint {
  period: string
  headerCount: number
  amountUsd?: number | null
}

export interface StockInListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface StockInListAnalyticsRankings {
  vendorByAmount: StockInListAnalyticsRankingRow[]
  purchaseUserByAmount: StockInListAnalyticsRankingRow[]
}

function buildParams(q: StockInListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.model) p.model = q.model
  if (q.vendorName) p.vendorName = q.vendorName
  if (q.purchaseOrderCode) p.purchaseOrderCode = q.purchaseOrderCode
  if (q.freightForwarderOrderNo) p.freightForwarderOrderNo = q.freightForwarderOrderNo
  if (q.salesOrderCode) p.salesOrderCode = q.salesOrderCode
  if (q.stockInCode) p.stockInCode = q.stockInCode
  if (q.sourceDisplayNo) p.sourceDisplayNo = q.sourceDisplayNo
  if (q.warehouseId) p.warehouseId = q.warehouseId
  if (q.stockInDateStart) p.stockInDateStart = q.stockInDateStart
  if (q.stockInDateEnd) p.stockInDateEnd = q.stockInDateEnd
  if (q.remark) p.remark = q.remark
  if (q.stockInType !== undefined && q.stockInType !== null) p.stockInType = q.stockInType
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: StockInListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/stock-in/analytics'

export const stockInListAnalyticsApi = {
  getDashboard(query: StockInListAnalyticsQuery): Promise<StockInListAnalyticsDashboard> {
    return apiClient.get<StockInListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: StockInListAnalyticsQuery): Promise<StockInListAnalyticsTrendPoint[]> {
    return apiClient.get<StockInListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(query: StockInListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>(analyticsUrl(`${BASE}/breakdowns`, query))
  },
  getRankings(query: StockInListAnalyticsQuery): Promise<StockInListAnalyticsRankings> {
    return apiClient.get<StockInListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}
