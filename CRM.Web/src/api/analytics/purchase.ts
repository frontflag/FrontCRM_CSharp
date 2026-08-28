import apiClient from '../client'
import type {
  PurchaseOrderItemListAnalyticsDashboard,
  PurchaseOrderItemListAnalyticsRankings,
  PurchaseOrderItemListAnalyticsTrendPoint
} from '../purchaseOrderItemAnalytics'
import type {
  QuoteListAnalyticsDashboard,
  QuoteListAnalyticsRankings,
  QuoteListAnalyticsTrendPoint
} from '../quoteListAnalytics'

export type PurchaseAnalyticsViewLevel = 'company' | 'department' | 'personal'

export interface PurchaseAnalyticsQuery {
  viewLevel?: PurchaseAnalyticsViewLevel
  departmentId?: string
  purchaseUserId?: string
  dateFrom?: string
  dateTo?: string
  groupBy?: 'day' | 'week' | 'month'
  rankingSort?: 'amount' | 'count'
  rankingLineMetric?: 'lines' | 'transactions'
}

export interface PurchaseAnalyticsScopeContext {
  purchaseDataScope: number
  viewLevel: PurchaseAnalyticsViewLevel
  scopeLabel: string
  primaryDepartmentId?: string | null
  primaryDepartmentName?: string | null
  allowedViewLevels: PurchaseAnalyticsViewLevel[]
  allowedDepartments: { id: string; name: string }[]
  allowedPurchaseUsers?: { id: string; name: string }[]
  canSelectPurchaseUser?: boolean
  dataFiltered: boolean
  maskAmounts: boolean
  resolvedPurchaseUserId?: string | null
  resolvedDepartmentId?: string | null
}

export interface PurchaseAnalyticsSnapshot {
  quoteItemCount: number
  quoteVendorCount: number
  quoteToPurchaseConversionRate?: number | null
  purchaseOrderItemCount: number
  purchaseOrderVendorCount: number
  purchaseAmountApproved?: number | null
  purchaseAmountStockIn?: number | null
  purchaseAmountPaid?: number | null
}

export interface PurchaseAnalyticsTodo {
  payableAmount?: number | null
  pendingStockInItemCount: number
}

export interface PurchaseAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface PurchaseAnalyticsDashboard {
  scopeContext: PurchaseAnalyticsScopeContext
  snapshot: PurchaseAnalyticsSnapshot
  todo: PurchaseAnalyticsTodo
  rankings: {
    primary: PurchaseAnalyticsRankingRow[]
    secondary: PurchaseAnalyticsRankingRow[]
  }
}

export interface PurchaseAnalyticsTrendPoint {
  period: string
  quoteItemCount: number
  quoteVendorCount: number
  purchaseOrderItemCount: number
  purchaseOrderVendorCount: number
  purchaseAmountApproved?: number | null
  purchaseAmountStockIn?: number | null
  purchaseAmountPaid?: number | null
  payableAmount?: number | null
  quoteToPurchaseConversionRate?: number | null
}

export interface PurchaseAnalyticsBreakdownItem {
  key: string
  label: string
  value: number
  ratio: number
}

export interface PurchaseAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  items: PurchaseAnalyticsBreakdownItem[]
}

export interface PurchaseAnalyticsVendorSnapshot {
  approvedVendorCount: number
  repeatVendorCount: number
}

export interface PurchaseAnalyticsVendorRankings {
  vendorByAmount: PurchaseAnalyticsRankingRow[]
  vendorByOrderCount: PurchaseAnalyticsRankingRow[]
  vendorByRepeatOrderCount: PurchaseAnalyticsRankingRow[]
}

export interface PurchaseAnalyticsVendor {
  scopeContext: PurchaseAnalyticsScopeContext
  snapshot: PurchaseAnalyticsVendorSnapshot
  breakdowns: PurchaseAnalyticsBreakdownGroup[]
  rankings: PurchaseAnalyticsVendorRankings
}

function buildParams(q: PurchaseAnalyticsQuery): Record<string, string> {
  const p: Record<string, string> = {}
  if (q.viewLevel) p.viewLevel = q.viewLevel
  if (q.departmentId) p.departmentId = q.departmentId
  if (q.purchaseUserId) p.purchaseUserId = q.purchaseUserId
  if (q.dateFrom) p.dateFrom = q.dateFrom
  if (q.dateTo) p.dateTo = q.dateTo
  if (q.groupBy) p.groupBy = q.groupBy
  if (q.rankingSort) p.rankingSort = q.rankingSort
  if (q.rankingLineMetric) p.rankingLineMetric = q.rankingLineMetric
  return p
}

export const purchaseAnalyticsApi = {
  getDashboard(query: PurchaseAnalyticsQuery): Promise<PurchaseAnalyticsDashboard> {
    return apiClient.get<PurchaseAnalyticsDashboard>('/api/v1/analytics/purchase/dashboard', {
      params: buildParams(query)
    })
  },

  getTrends(query: PurchaseAnalyticsQuery): Promise<PurchaseAnalyticsTrendPoint[]> {
    return apiClient.get<PurchaseAnalyticsTrendPoint[]>('/api/v1/analytics/purchase/trends', {
      params: buildParams(query)
    })
  },

  getBreakdowns(query: PurchaseAnalyticsQuery): Promise<PurchaseAnalyticsBreakdownGroup[]> {
    return apiClient.get<PurchaseAnalyticsBreakdownGroup[]>('/api/v1/analytics/purchase/breakdowns', {
      params: buildParams(query)
    })
  },

  getVendor(query: PurchaseAnalyticsQuery): Promise<PurchaseAnalyticsVendor> {
    return apiClient.get<PurchaseAnalyticsVendor>('/api/v1/analytics/purchase/vendor', {
      params: buildParams(query)
    })
  },

  /** 采购订单明细维（成单口径；与 /purchase-orders/items/analytics 同实现） */
  getOrderItemsDashboard(query: PurchaseAnalyticsQuery): Promise<PurchaseOrderItemListAnalyticsDashboard> {
    return apiClient.get<PurchaseOrderItemListAnalyticsDashboard>(
      '/api/v1/analytics/purchase/order-items/dashboard',
      { params: buildParams(query) }
    )
  },

  getOrderItemsTrends(query: PurchaseAnalyticsQuery): Promise<PurchaseOrderItemListAnalyticsTrendPoint[]> {
    return apiClient.get<PurchaseOrderItemListAnalyticsTrendPoint[]>(
      '/api/v1/analytics/purchase/order-items/trends',
      { params: buildParams(query) }
    )
  },

  getOrderItemsBreakdowns(query: PurchaseAnalyticsQuery): Promise<PurchaseAnalyticsBreakdownGroup[]> {
    return apiClient.get<PurchaseAnalyticsBreakdownGroup[]>(
      '/api/v1/analytics/purchase/order-items/breakdowns',
      { params: buildParams(query) }
    )
  },

  getOrderItemsRankings(query: PurchaseAnalyticsQuery): Promise<PurchaseOrderItemListAnalyticsRankings> {
    return apiClient.get<PurchaseOrderItemListAnalyticsRankings>(
      '/api/v1/analytics/purchase/order-items/rankings',
      { params: buildParams(query) }
    )
  },

  /** 报价列表维（reportScope；与 /quotes/analytics 同实现） */
  getQuotesDashboard(query: PurchaseAnalyticsQuery): Promise<QuoteListAnalyticsDashboard> {
    return apiClient.get<QuoteListAnalyticsDashboard>('/api/v1/analytics/purchase/quotes/dashboard', {
      params: buildParams(query)
    })
  },

  getQuotesTrends(query: PurchaseAnalyticsQuery): Promise<QuoteListAnalyticsTrendPoint[]> {
    return apiClient.get<QuoteListAnalyticsTrendPoint[]>('/api/v1/analytics/purchase/quotes/trends', {
      params: buildParams(query)
    })
  },

  getQuotesBreakdowns(query: PurchaseAnalyticsQuery): Promise<PurchaseAnalyticsBreakdownGroup[]> {
    return apiClient.get<PurchaseAnalyticsBreakdownGroup[]>('/api/v1/analytics/purchase/quotes/breakdowns', {
      params: buildParams(query)
    })
  },

  getQuotesRankings(query: PurchaseAnalyticsQuery): Promise<QuoteListAnalyticsRankings> {
    return apiClient.get<QuoteListAnalyticsRankings>('/api/v1/analytics/purchase/quotes/rankings', {
      params: buildParams(query)
    })
  }
}
