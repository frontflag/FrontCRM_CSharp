import apiClient from './client'
import type { StockItemListQuery } from './inventoryCenter'
import type {
  InventoryOnHandListAnalyticsBreakdownGroup,
  InventoryOnHandListAnalyticsCurrencyLine,
  InventoryOnHandListAnalyticsRankingRow,
  InventoryOnHandListAnalyticsRankings,
  InventoryOnHandListAnalyticsTrendPoint
} from './inventoryOnHandAnalytics'
import { buildQueryString } from '@/utils/progressStatusQuery'

export type InventoryStockItemListAnalyticsQuery = StockItemListQuery & {
  groupBy?: 'day' | 'week' | 'month'
}

export interface InventoryStockItemListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: {
    onHandQty: number
    currencyLines: InventoryOnHandListAnalyticsCurrencyLine[]
    turnoverDays?: number | null
    stagnantQty: number
  }
}

export type {
  InventoryOnHandListAnalyticsBreakdownGroup as InventoryStockItemListAnalyticsBreakdownGroup,
  InventoryOnHandListAnalyticsRankingRow as InventoryStockItemListAnalyticsRankingRow,
  InventoryOnHandListAnalyticsRankings as InventoryStockItemListAnalyticsRankings,
  InventoryOnHandListAnalyticsTrendPoint as InventoryStockItemListAnalyticsTrendPoint
}

function buildParams(q: InventoryStockItemListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.stockInCode) p.stockInCode = q.stockInCode
  if (q.stockItemCode) p.stockItemCode = q.stockItemCode
  if (q.stockInDateFrom) p.stockInDateFrom = q.stockInDateFrom
  if (q.stockInDateTo) p.stockInDateTo = q.stockInDateTo
  if (q.warehouseId) p.warehouseId = q.warehouseId
  if (q.purchasePn) p.purchasePn = q.purchasePn
  if (q.purchaseBrand) p.purchaseBrand = q.purchaseBrand
  if (q.freightForwarderOrderNo) p.freightForwarderOrderNo = q.freightForwarderOrderNo
  if (q.outboundStatus != null) p.outboundStatus = q.outboundStatus
  if (q.customerName) p.customerName = q.customerName
  if (q.vendorName) p.vendorName = q.vendorName
  if (q.salespersonName) p.salespersonName = q.salespersonName
  if (q.purchaserName) p.purchaserName = q.purchaserName
  if (q.salespersonUserId) p.salespersonUserId = q.salespersonUserId
  if (q.purchaserUserId) p.purchaserUserId = q.purchaserUserId
  if (q.repertoryHasStock !== undefined) p.repertoryHasStock = q.repertoryHasStock
  if (q.stockType != null) p.stockType = q.stockType
  if (q.stockInType != null) p.stockInType = q.stockInType
  if (q.stagnantOnly) p.stagnantOnly = q.stagnantOnly
  if (q.rankDimension) p.rankDimension = q.rankDimension
  if (q.rankKey) p.rankKey = q.rankKey
  if (q.rankCurrency != null) p.rankCurrency = q.rankCurrency
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: InventoryStockItemListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/inventory-center/stock-items/analytics'

export const inventoryStockItemListAnalyticsApi = {
  getDashboard(query: InventoryStockItemListAnalyticsQuery): Promise<InventoryStockItemListAnalyticsDashboard> {
    return apiClient.get<InventoryStockItemListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: InventoryStockItemListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsTrendPoint[]> {
    return apiClient.get<InventoryOnHandListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(query: InventoryStockItemListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsBreakdownGroup[]> {
    return apiClient.get<InventoryOnHandListAnalyticsBreakdownGroup[]>(analyticsUrl(`${BASE}/breakdowns`, query))
  },
  getRankings(query: InventoryStockItemListAnalyticsQuery): Promise<InventoryOnHandListAnalyticsRankings> {
    return apiClient.get<InventoryOnHandListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}

export type InventoryStockItemRankingDrillPayload = {
  dimension: 'customer' | 'salesUser' | 'material' | 'brand'
  row: InventoryOnHandListAnalyticsRankingRow
  metricMode: 'qty' | 'amount'
  currencyKey?: string
  panelTitle: string
}
