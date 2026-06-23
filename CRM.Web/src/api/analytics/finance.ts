import apiClient from '../client'

export type FinanceAnalyticsViewLevel = 'company' | 'department' | 'personal'

export interface FinanceAnalyticsCurrencyAmount {
  currency: number
  currencyLabel: string
  amount: number
}

export interface FinanceAnalyticsMoney {
  totalUsd: number | null
  byCurrency: FinanceAnalyticsCurrencyAmount[]
}

export interface FinanceAnalyticsQuery {
  viewLevel?: FinanceAnalyticsViewLevel
  departmentId?: string
  ownerUserId?: string
  dateFrom?: string
  dateTo?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface FinanceAnalyticsScopeContext {
  financeDataScope: number
  saleDataScope: number
  purchaseDataScope: number
  accessMode: 'finance' | 'salesPurchaseOnly'
  viewLevel: FinanceAnalyticsViewLevel
  scopeLabel: string
  primaryDepartmentId?: string | null
  primaryDepartmentName?: string | null
  allowedViewLevels: FinanceAnalyticsViewLevel[]
  allowedDepartments: { id: string; name: string }[]
  dataFiltered: boolean
  maskAmounts: boolean
  exchangeRateHint?: string
  resolvedOwnerUserId?: string | null
  resolvedDepartmentId?: string | null
}

export interface FinanceAnalyticsTodo {
  payableAmount: FinanceAnalyticsMoney
  receivableAmount: FinanceAnalyticsMoney
  pendingPurchaseInvoiceAmount: FinanceAnalyticsMoney
  pendingSellInvoiceAmount: FinanceAnalyticsMoney
}

export interface FinanceAnalyticsCompleted {
  paidAmount: FinanceAnalyticsMoney
  receivedAmount: FinanceAnalyticsMoney
  issuedPurchaseInvoiceAmount: FinanceAnalyticsMoney
  issuedSellInvoiceAmount: FinanceAnalyticsMoney
}

export interface FinanceAnalyticsDashboard {
  scopeContext: FinanceAnalyticsScopeContext
  todo: FinanceAnalyticsTodo
  completed: FinanceAnalyticsCompleted
}

export interface FinanceAnalyticsTrendPoint {
  period: string
  paidAmount?: FinanceAnalyticsMoney | null
  receivedAmount?: FinanceAnalyticsMoney | null
  payableAmount?: FinanceAnalyticsMoney | null
  receivableAmount?: FinanceAnalyticsMoney | null
}

export interface FinanceAnalyticsBreakdownItem {
  key: string
  label: string
  value: number
  ratio: number
}

export interface FinanceAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  items: FinanceAnalyticsBreakdownItem[]
}

function buildParams(q: FinanceAnalyticsQuery): Record<string, string> {
  const p: Record<string, string> = {}
  if (q.viewLevel) p.viewLevel = q.viewLevel
  if (q.departmentId) p.departmentId = q.departmentId
  if (q.ownerUserId) p.ownerUserId = q.ownerUserId
  if (q.dateFrom) p.dateFrom = q.dateFrom
  if (q.dateTo) p.dateTo = q.dateTo
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

export const financeAnalyticsApi = {
  getDashboard(q: FinanceAnalyticsQuery) {
    return apiClient.get<FinanceAnalyticsDashboard>('/api/v1/analytics/finance/dashboard', { params: buildParams(q) })
  },
  getTrends(q: FinanceAnalyticsQuery) {
    return apiClient.get<FinanceAnalyticsTrendPoint[]>('/api/v1/analytics/finance/trends', { params: buildParams(q) })
  },
  getBreakdowns(q: FinanceAnalyticsQuery) {
    return apiClient.get<FinanceAnalyticsBreakdownGroup[]>('/api/v1/analytics/finance/breakdowns', { params: buildParams(q) })
  }
}
