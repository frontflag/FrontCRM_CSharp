import apiClient from '../client'

export type LogisticsAnalyticsViewLevel = 'company' | 'department' | 'personal'
export type LogisticsInventoryType = 'all' | 'customerOrder' | 'purchaseStock'
export type LogisticsMatrixSubject = 'salesperson' | 'vendor' | 'purchaser' | 'brand'

export interface LogisticsAnalyticsQuery {
  viewLevel?: LogisticsAnalyticsViewLevel
  departmentId?: string
  ownerUserId?: string
  inventoryType?: LogisticsInventoryType
  matrixSubject?: LogisticsMatrixSubject
  dateFrom?: string
  dateTo?: string
  trendDateTo?: string
  groupBy?: 'day' | 'week' | 'month'
  warehouseId?: string
}

export interface LogisticsSubjectCounts {
  customer: number
  salesperson: number
  vendor: number
  purchaser: number
  brand: number
}

export interface LogisticsAnalyticsScopeContext {
  logisticsDataScope: number
  saleDataScope: number
  purchaseDataScope: number
  accessMode: 'logistics' | 'salesPurchaseOnly'
  viewLevel: LogisticsAnalyticsViewLevel
  scopeLabel: string
  inventoryType: LogisticsInventoryType
  primaryDepartmentId?: string | null
  primaryDepartmentName?: string | null
  allowedViewLevels: LogisticsAnalyticsViewLevel[]
  allowedDepartments: { id: string; name: string }[]
  dataFiltered: boolean
  maskAmounts: boolean
  maskSalesAmounts: boolean
  resolvedOwnerUserId?: string | null
  resolvedDepartmentId?: string | null
}

export interface LogisticsAnalyticsSnapshot {
  inventoryType: LogisticsInventoryType
  onHandQty: number
  onHandAmountUsd?: number | null
  weightedAvgAgeDays?: number | null
  subjectCounts: LogisticsSubjectCounts
}

export interface LogisticsAnalyticsTodo {
  pendingStockInQty: number
}

export interface LogisticsAnalyticsMoney {
  totalUsd?: number | null
  byCurrency: { currency: number; currencyLabel: string; amount: number }[]
}

export interface LogisticsAnalyticsFlow {
  stockInAmount: LogisticsAnalyticsMoney
  stockOutAmount: LogisticsAnalyticsMoney
}

export interface LogisticsAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface LogisticsAnalyticsDashboard {
  scopeContext: LogisticsAnalyticsScopeContext
  snapshot: LogisticsAnalyticsSnapshot
  todo: LogisticsAnalyticsTodo
  flow?: LogisticsAnalyticsFlow
  rankings: {
    primary: LogisticsAnalyticsRankingRow[]
    secondary: LogisticsAnalyticsRankingRow[]
  }
}

export interface LogisticsAnalyticsTrendPoint {
  period: string
  stockInQty: number
  pendingStockInQty: number
}

export interface LogisticsAnalyticsBreakdownItem {
  key: string
  label: string
  value: number
  ratio: number
}

export interface LogisticsAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  items: LogisticsAnalyticsBreakdownItem[]
}

export interface LogisticsAnalyticsMatrixChild {
  subjectKey: string
  subjectLabel: string
  onHandQty: number
  onHandAmountUsd?: number | null
  weightedAvgAgeDays?: number | null
}

export interface LogisticsAnalyticsMatrixRow {
  anchorCustomerId?: string | null
  anchorCustomerName: string
  onHandQty: number
  onHandAmountUsd?: number | null
  weightedAvgAgeDays?: number | null
  children: LogisticsAnalyticsMatrixChild[]
}

export interface LogisticsAnalyticsCustomerMatrix {
  inventoryType: LogisticsInventoryType
  matrixSubject: LogisticsMatrixSubject
  rows: LogisticsAnalyticsMatrixRow[]
}

function buildParams(q: LogisticsAnalyticsQuery): Record<string, string> {
  const p: Record<string, string> = {}
  if (q.viewLevel) p.viewLevel = q.viewLevel
  if (q.departmentId) p.departmentId = q.departmentId
  if (q.ownerUserId) p.ownerUserId = q.ownerUserId
  if (q.inventoryType) p.inventoryType = q.inventoryType
  if (q.matrixSubject) p.matrixSubject = q.matrixSubject
  if (q.dateFrom) p.dateFrom = q.dateFrom
  if (q.dateTo) p.dateTo = q.dateTo
  if (q.trendDateTo) p.trendDateTo = q.trendDateTo
  if (q.groupBy) p.groupBy = q.groupBy
  if (q.warehouseId) p.warehouseId = q.warehouseId
  return p
}

export const logisticsAnalyticsApi = {
  getDashboard(q: LogisticsAnalyticsQuery) {
    return apiClient.get<LogisticsAnalyticsDashboard>('/api/v1/analytics/logistics/dashboard', { params: buildParams(q) })
  },
  getTrends(q: LogisticsAnalyticsQuery) {
    return apiClient.get<LogisticsAnalyticsTrendPoint[]>('/api/v1/analytics/logistics/trends', { params: buildParams(q) })
  },
  getBreakdowns(q: LogisticsAnalyticsQuery) {
    return apiClient.get<LogisticsAnalyticsBreakdownGroup[]>('/api/v1/analytics/logistics/breakdowns', { params: buildParams(q) })
  },
  getCustomerMatrix(q: LogisticsAnalyticsQuery & { matrixSubject: LogisticsMatrixSubject }) {
    return apiClient.get<LogisticsAnalyticsCustomerMatrix>('/api/v1/analytics/logistics/customer-matrix', { params: buildParams(q) })
  }
}
