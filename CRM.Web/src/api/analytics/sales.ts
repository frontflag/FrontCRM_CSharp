import apiClient from '../client'

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
  }
}
