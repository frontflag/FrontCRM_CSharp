import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'
import { buildQueryString } from '@/utils/progressStatusQuery'

export type InventoryOnHandListAnalyticsQuery = {
  materialModel?: string
  purchaseBrand?: string
  stockType?: number
  warehouseId?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface InventoryOnHandListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
}

export interface InventoryOnHandListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: {
    onHandQty: number
    currencyLines: InventoryOnHandListAnalyticsCurrencyLine[]
    weightedAvgAgeDays?: number | null
    stagnantLayerCount: number
  }
}

export interface InventoryOnHandListAnalyticsTrendCurrencyAmount {
  currencyKey: string
  currencyLabel: string
  amount?: number | null
}

export interface InventoryOnHandListAnalyticsTrendPoint {
  period: string
  onHandQty: number
  amountsByCurrency: InventoryOnHandListAnalyticsTrendCurrencyAmount[]
}

export interface InventoryOnHandListAnalyticsBreakdownGroup extends SalesAnalyticsBreakdownGroup {
  currencyKey?: string | null
  currencyLabel?: string | null
}

export interface InventoryOnHandListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface InventoryOnHandListAnalyticsRankingFacet {
  currencyKey: string
  currencyLabel: string
  rows: InventoryOnHandListAnalyticsRankingRow[]
}

export interface InventoryOnHandListAnalyticsRankings {
  customerByQty: InventoryOnHandListAnalyticsRankingRow[]
  salesUserByQty: InventoryOnHandListAnalyticsRankingRow[]
  materialByQty: InventoryOnHandListAnalyticsRankingRow[]
  brandByQty: InventoryOnHandListAnalyticsRankingRow[]
  customerByAmount: InventoryOnHandListAnalyticsRankingFacet[]
  salesUserByAmount: InventoryOnHandListAnalyticsRankingFacet[]
  materialByAmount: InventoryOnHandListAnalyticsRankingFacet[]
  brandByAmount: InventoryOnHandListAnalyticsRankingFacet[]
}

function buildParams(q: InventoryOnHandListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.materialModel) p.materialModel = q.materialModel
  if (q.purchaseBrand) p.purchaseBrand = q.purchaseBrand
  if (q.stockType !== undefined && q.stockType !== null) p.stockType = q.stockType
  if (q.warehouseId) p.warehouseId = q.warehouseId
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: InventoryOnHandListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/inventory-center/on-hand/analytics'

export const inventoryOnHandListAnalyticsApi = {
  getDashboard(query: InventoryOnHandListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsDashboard> {
    return apiClient.get<InventoryOnHandListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: InventoryOnHandListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsTrendPoint[]> {
    return apiClient.get<InventoryOnHandListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(query: InventoryOnHandListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsBreakdownGroup[]> {
    return apiClient.get<InventoryOnHandListAnalyticsBreakdownGroup[]>(analyticsUrl(`${BASE}/breakdowns`, query))
  },
  getRankings(query: InventoryOnHandListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsRankings> {
    return apiClient.get<InventoryOnHandListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}
