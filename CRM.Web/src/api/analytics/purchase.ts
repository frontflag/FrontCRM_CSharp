import apiClient from '../client'

export type PurchaseAnalyticsViewLevel = 'company' | 'department' | 'personal'

export interface PurchaseAnalyticsQuery {
  viewLevel?: PurchaseAnalyticsViewLevel
  departmentId?: string
  purchaseUserId?: string
  dateFrom?: string
  dateTo?: string
  groupBy?: 'day' | 'week' | 'month'
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

function buildParams(q: PurchaseAnalyticsQuery): Record<string, string> {
  const p: Record<string, string> = {}
  if (q.viewLevel) p.viewLevel = q.viewLevel
  if (q.departmentId) p.departmentId = q.departmentId
  if (q.purchaseUserId) p.purchaseUserId = q.purchaseUserId
  if (q.dateFrom) p.dateFrom = q.dateFrom
  if (q.dateTo) p.dateTo = q.dateTo
  if (q.groupBy) p.groupBy = q.groupBy
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
  }
}
