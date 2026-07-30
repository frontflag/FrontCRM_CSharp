import apiClient from '../client'
import type {
  SalesOrderItemListAnalyticsDashboard,
  SalesOrderItemListAnalyticsRankings,
  SalesOrderItemListAnalyticsTrendPoint
} from '../salesOrderItemAnalytics'
import type { RfqListAnalyticsDashboard, RfqListAnalyticsTrendPoint } from '../rfqAnalytics'
import type { RfqItemListAnalyticsRankings } from '../rfqItemAnalytics'

export type SalesAnalyticsViewLevel = 'company' | 'department' | 'personal'

export interface SalesAnalyticsQuery {
  viewLevel?: SalesAnalyticsViewLevel
  departmentId?: string
  salesUserId?: string
  dateFrom?: string
  dateTo?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface SalesAnalyticsScopeContext {
  saleDataScope: number
  viewLevel: SalesAnalyticsViewLevel
  scopeLabel: string
  primaryDepartmentId?: string | null
  primaryDepartmentName?: string | null
  allowedViewLevels: SalesAnalyticsViewLevel[]
  allowedDepartments: { id: string; name: string }[]
  allowedSalesUsers?: { id: string; name: string }[]
  dataFiltered: boolean
  maskAmounts: boolean
  resolvedSalesUserId?: string | null
  resolvedDepartmentId?: string | null
}

export interface SalesAnalyticsSnapshot {
  rfqItemCount: number
  rfqCustomerCount: number
  rfqToSalesConversionRate?: number | null
  salesOrderItemCount: number
  salesOrderCustomerCount: number
  salesAmountApproved?: number | null
  salesAmountStockOut?: number | null
  salesAmountReceived?: number | null
}

export interface SalesAnalyticsTodo {
  receivableAmount?: number | null
  pendingStockOutItemCount: number
  pendingInvoiceAmount?: number | null
}

export interface SalesAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface SalesAnalyticsDashboard {
  scopeContext: SalesAnalyticsScopeContext
  snapshot: SalesAnalyticsSnapshot
  todo: SalesAnalyticsTodo
  rankings: {
    primary: SalesAnalyticsRankingRow[]
    secondary: SalesAnalyticsRankingRow[]
  }
}

export interface SalesAnalyticsTrendPoint {
  period: string
  rfqItemCount: number
  rfqCustomerCount: number
  salesOrderItemCount: number
  salesOrderCustomerCount: number
  salesAmountApproved?: number | null
  salesAmountStockOut?: number | null
  salesAmountReceived?: number | null
  receivableAmount?: number | null
  rfqToSalesConversionRate?: number | null
}

export interface SalesAnalyticsBreakdownItem {
  key: string
  label: string
  value: number
  ratio: number
}

export interface SalesAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  items: SalesAnalyticsBreakdownItem[]
}

export interface SalesAnalyticsCustomerSnapshot {
  approvedCustomerCount: number
  repeatCustomerCount: number
}

export interface SalesAnalyticsCustomerRankings {
  customerByAmount: SalesAnalyticsRankingRow[]
  customerByOrderCount: SalesAnalyticsRankingRow[]
  customerByRepeatOrderCount: SalesAnalyticsRankingRow[]
}

export interface SalesAnalyticsCustomer {
  scopeContext: SalesAnalyticsScopeContext
  snapshot: SalesAnalyticsCustomerSnapshot
  breakdowns: SalesAnalyticsBreakdownGroup[]
  rankings: SalesAnalyticsCustomerRankings
}

function buildParams(q: SalesAnalyticsQuery): Record<string, string> {
  const p: Record<string, string> = {}
  if (q.viewLevel) p.viewLevel = q.viewLevel
  if (q.departmentId) p.departmentId = q.departmentId
  if (q.salesUserId) p.salesUserId = q.salesUserId
  if (q.dateFrom) p.dateFrom = q.dateFrom
  if (q.dateTo) p.dateTo = q.dateTo
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

export const salesAnalyticsApi = {
  getDashboard(query: SalesAnalyticsQuery): Promise<SalesAnalyticsDashboard> {
    return apiClient.get<SalesAnalyticsDashboard>('/api/v1/analytics/sales/dashboard', {
      params: buildParams(query)
    })
  },

  getTrends(query: SalesAnalyticsQuery): Promise<SalesAnalyticsTrendPoint[]> {
    return apiClient.get<SalesAnalyticsTrendPoint[]>('/api/v1/analytics/sales/trends', {
      params: buildParams(query)
    })
  },

  getBreakdowns(query: SalesAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/analytics/sales/breakdowns', {
      params: buildParams(query)
    })
  },

  getCustomer(query: SalesAnalyticsQuery): Promise<SalesAnalyticsCustomer> {
    return apiClient.get<SalesAnalyticsCustomer>('/api/v1/analytics/sales/customer', {
      params: buildParams(query)
    })
  },

  /** 订单明细维（成单口径；与 /sales-orders/items/analytics 同实现） */
  getOrderItemsDashboard(query: SalesAnalyticsQuery): Promise<SalesOrderItemListAnalyticsDashboard> {
    return apiClient.get<SalesOrderItemListAnalyticsDashboard>(
      '/api/v1/analytics/sales/order-items/dashboard',
      { params: buildParams(query) }
    )
  },

  getOrderItemsTrends(query: SalesAnalyticsQuery): Promise<SalesOrderItemListAnalyticsTrendPoint[]> {
    return apiClient.get<SalesOrderItemListAnalyticsTrendPoint[]>(
      '/api/v1/analytics/sales/order-items/trends',
      { params: buildParams(query) }
    )
  },

  getOrderItemsBreakdowns(query: SalesAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>(
      '/api/v1/analytics/sales/order-items/breakdowns',
      { params: buildParams(query) }
    )
  },

  getOrderItemsRankings(query: SalesAnalyticsQuery): Promise<SalesOrderItemListAnalyticsRankings> {
    return apiClient.get<SalesOrderItemListAnalyticsRankings>(
      '/api/v1/analytics/sales/order-items/rankings',
      { params: buildParams(query) }
    )
  },

  /** 需求明细维（reportScope；与 /rfqs/items/analytics 同实现） */
  getRfqItemsDashboard(query: SalesAnalyticsQuery): Promise<RfqListAnalyticsDashboard> {
    return apiClient.get<RfqListAnalyticsDashboard>('/api/v1/analytics/sales/rfq-items/dashboard', {
      params: buildParams(query)
    })
  },

  getRfqItemsTrends(query: SalesAnalyticsQuery): Promise<RfqListAnalyticsTrendPoint[]> {
    return apiClient.get<RfqListAnalyticsTrendPoint[]>('/api/v1/analytics/sales/rfq-items/trends', {
      params: buildParams(query)
    })
  },

  getRfqItemsBreakdowns(query: SalesAnalyticsQuery): Promise<SalesAnalyticsBreakdownGroup[]> {
    return apiClient.get<SalesAnalyticsBreakdownGroup[]>('/api/v1/analytics/sales/rfq-items/breakdowns', {
      params: buildParams(query)
    })
  },

  getRfqItemsRankings(query: SalesAnalyticsQuery): Promise<RfqItemListAnalyticsRankings> {
    return apiClient.get<RfqItemListAnalyticsRankings>('/api/v1/analytics/sales/rfq-items/rankings', {
      params: buildParams(query)
    })
  }
}
