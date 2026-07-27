import apiClient from './client'
import type { SalesAnalyticsBreakdownGroup } from './analytics/sales'
import { buildQueryString } from '@/utils/progressStatusQuery'

export interface SalesOrderItemListAnalyticsQuery {
  orderCreateStart?: string
  orderCreateEnd?: string
  customerName?: string
  salesUserName?: string
  salesUserId?: string
  purchaseUserAccount?: string
  customerId?: string
  sellOrderCode?: string
  pn?: string
  customerSo?: string
  customerPn?: string
  transactionCurrency?: string
  stockOutPending?: boolean
  invoicePending?: boolean
  purchaseProgressStatus?: number | number[]
  stockInProgressStatus?: number | number[]
  stockOutNotifyProgressStatus?: number | number[]
  stockOutProgressStatus?: number | number[]
  receiptProgressStatus?: number | number[]
  invoiceProgressStatus?: number | number[]
  /** 左栏快捷检索（与六 progress 互斥） */
  quickFilter?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface SalesOrderItemListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
  usdAmount?: number | null
}

export interface SalesOrderItemListAnalyticsSnapshot {
  approvedCustomerCount: number
  approvedOrderCount: number
  approvedLineCount: number
  approvedAmountUsd?: number | null
  currencyLines: SalesOrderItemListAnalyticsCurrencyLine[]
  purchaseProfitUsd?: number | null
  outboundProfitUsd?: number | null
  inStockCustomerCount: number
  inStockLineCount: number
  inStockAmountUsd?: number | null
  maxStockAgeDays?: number | null
  receivableCustomerCount: number
  receivableLineCount: number
  receivableAmountUsd?: number | null
  receivableCurrencyLines: SalesOrderItemListAnalyticsCurrencyLine[]
  maxReceivableAgeDays?: number | null
}

export interface SalesOrderItemListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: SalesOrderItemListAnalyticsSnapshot
}

export interface SalesOrderItemListAnalyticsTrendPoint {
  period: string
  approvedOrderCount: number
  approvedLineCount: number
  approvedLineAmountUsd?: number | null
}

export interface SalesOrderItemListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface SalesOrderItemListAnalyticsRankings {
  customerByAmount: SalesOrderItemListAnalyticsRankingRow[]
  pnByAmount: SalesOrderItemListAnalyticsRankingRow[]
  pnByQty: SalesOrderItemListAnalyticsRankingRow[]
  brandByAmount: SalesOrderItemListAnalyticsRankingRow[]
  brandByQty: SalesOrderItemListAnalyticsRankingRow[]
  salesUserByAmount: SalesOrderItemListAnalyticsRankingRow[]
}

function buildParams(q: SalesOrderItemListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.orderCreateStart) p.orderCreateStart = q.orderCreateStart
  if (q.orderCreateEnd) p.orderCreateEnd = q.orderCreateEnd
  if (q.customerName) p.customerName = q.customerName
  if (q.salesUserName) p.salesUserName = q.salesUserName
  if (q.salesUserId) p.salesUserId = q.salesUserId
  if (q.purchaseUserAccount) p.purchaseUserAccount = q.purchaseUserAccount
  if (q.customerId) p.customerId = q.customerId
  if (q.sellOrderCode) p.sellOrderCode = q.sellOrderCode
  if (q.pn) p.pn = q.pn
  if (q.customerSo) p.customerSo = q.customerSo
  if (q.customerPn) p.customerPn = q.customerPn
  if (q.transactionCurrency) p.transactionCurrency = q.transactionCurrency
  if (q.stockOutPending) p.stockOutPending = true
  if (q.invoicePending) p.invoicePending = true
  if (q.purchaseProgressStatus !== undefined && q.purchaseProgressStatus !== null) {
    p.purchaseProgressStatus = q.purchaseProgressStatus
  }
  if (q.stockInProgressStatus !== undefined && q.stockInProgressStatus !== null) {
    p.stockInProgressStatus = q.stockInProgressStatus
  }
  if (q.stockOutNotifyProgressStatus !== undefined && q.stockOutNotifyProgressStatus !== null) {
    p.stockOutNotifyProgressStatus = q.stockOutNotifyProgressStatus
  }
  if (q.stockOutProgressStatus !== undefined && q.stockOutProgressStatus !== null) {
    p.stockOutProgressStatus = q.stockOutProgressStatus
  }
  if (q.receiptProgressStatus !== undefined && q.receiptProgressStatus !== null) {
    p.receiptProgressStatus = q.receiptProgressStatus
  }
  if (q.invoiceProgressStatus !== undefined && q.invoiceProgressStatus !== null) {
    p.invoiceProgressStatus = q.invoiceProgressStatus
  }
  if (q.quickFilter) p.quickFilter = q.quickFilter
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: SalesOrderItemListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

export const salesOrderItemListAnalyticsApi = {
  getDashboard(query: SalesOrderItemListAnalyticsQuery): Promise<SalesOrderItemListAnalyticsDashboard> {
    return apiClient.get<SalesOrderItemListAnalyticsDashboard>(
      analyticsUrl('/api/v1/sales-orders/items/analytics/dashboard', query)
    )
  },

  getTrends(query: SalesOrderItemListAnalyticsQuery): Promise<SalesOrderItemListAnalyticsTrendPoint[]> {
    return apiClient.get<SalesOrderItemListAnalyticsTrendPoint[]>(
      analyticsUrl('/api/v1/sales-orders/items/analytics/trends', query)
    )
  },

  getBreakdowns(query: SalesOrderItemListAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>(
      analyticsUrl('/api/v1/sales-orders/items/analytics/breakdowns', query)
    )
  },

  getRankings(query: SalesOrderItemListAnalyticsQuery): Promise<SalesOrderItemListAnalyticsRankings> {
    return apiClient.get<SalesOrderItemListAnalyticsRankings>(
      analyticsUrl('/api/v1/sales-orders/items/analytics/rankings', query)
    )
  }
}
