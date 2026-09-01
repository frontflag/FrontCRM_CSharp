import apiClient from './client'
import { buildQueryString } from '@/utils/progressStatusQuery'

export type FinancePaymentListAnalyticsQuery = {
  keyword?: string
  financePaymentCode?: string
  freightForwarderOrderNo?: string
  bankSlipNo?: string
  paymentMode?: number
  vendorName?: string
  purchaseOrderCode?: string
  purchaseUserName?: string
  purchaseCurrency?: number
  paymentCurrency?: number
  remark?: string
  status?: number
  startDate?: string
  endDate?: string
  groupBy?: 'day' | 'week' | 'month'
}

export interface FinancePaymentListAnalyticsCurrencyLine {
  currencyKey: string
  currencyLabel: string
  originalAmount?: number | null
}

export interface FinancePaymentListAnalyticsDashboard {
  context: { maskAmounts: boolean }
  snapshot: {
    vendorCount: number
    headerCount: number
    currencyLines: FinancePaymentListAnalyticsCurrencyLine[]
  }
}

export interface FinancePaymentListAnalyticsTrendCurrencyAmount {
  currencyKey: string
  currencyLabel: string
  amount?: number | null
}

export interface FinancePaymentListAnalyticsTrendPoint {
  period: string
  headerCount: number
  amountsByCurrency: FinancePaymentListAnalyticsTrendCurrencyAmount[]
}

export interface FinancePaymentListAnalyticsBreakdownItem {
  key: string
  label: string
  value: number
  ratio: number
}

export interface FinancePaymentListAnalyticsBreakdownGroup {
  groupKey: string
  groupLabel: string
  currencyKey?: string | null
  currencyLabel?: string | null
  items: FinancePaymentListAnalyticsBreakdownItem[]
}

export interface FinancePaymentListAnalyticsRankingRow {
  id: string
  name: string
  amount?: number | null
  orderCount: number
}

export interface FinancePaymentListAnalyticsRankingFacet {
  currencyKey: string
  currencyLabel: string
  rows: FinancePaymentListAnalyticsRankingRow[]
}

export interface FinancePaymentListAnalyticsRankings {
  vendorByAmount: FinancePaymentListAnalyticsRankingFacet[]
  purchaseUserByAmount: FinancePaymentListAnalyticsRankingFacet[]
}

function buildParams(q: FinancePaymentListAnalyticsQuery): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  if (q.keyword) p.keyword = q.keyword
  if (q.financePaymentCode) p.financePaymentCode = q.financePaymentCode
  if (q.freightForwarderOrderNo) p.freightForwarderOrderNo = q.freightForwarderOrderNo
  if (q.bankSlipNo) p.bankSlipNo = q.bankSlipNo
  if (q.paymentMode !== undefined && q.paymentMode !== null) p.paymentMode = q.paymentMode
  if (q.vendorName) p.vendorName = q.vendorName
  if (q.purchaseOrderCode) p.purchaseOrderCode = q.purchaseOrderCode
  if (q.purchaseUserName) p.purchaseUserName = q.purchaseUserName
  if (q.purchaseCurrency !== undefined && q.purchaseCurrency !== null) p.purchaseCurrency = q.purchaseCurrency
  if (q.paymentCurrency !== undefined && q.paymentCurrency !== null) p.paymentCurrency = q.paymentCurrency
  if (q.remark) p.remark = q.remark
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.startDate) p.startDate = q.startDate
  if (q.endDate) p.endDate = q.endDate
  if (q.groupBy) p.groupBy = q.groupBy
  return p
}

function analyticsUrl(path: string, query: FinancePaymentListAnalyticsQuery): string {
  const qs = buildQueryString(buildParams(query))
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/finance/payments/analytics'

export const financePaymentListAnalyticsApi = {
  getDashboard(query: FinancePaymentListAnalyticsQuery): Promise<FinancePaymentListAnalyticsDashboard> {
    return apiClient.get<FinancePaymentListAnalyticsDashboard>(analyticsUrl(`${BASE}/dashboard`, query))
  },
  getTrends(query: FinancePaymentListAnalyticsQuery): Promise<FinancePaymentListAnalyticsTrendPoint[]> {
    return apiClient.get<FinancePaymentListAnalyticsTrendPoint[]>(analyticsUrl(`${BASE}/trends`, query))
  },
  getBreakdowns(query: FinancePaymentListAnalyticsQuery): Promise<FinancePaymentListAnalyticsBreakdownGroup[]> {
    return apiClient.get<FinancePaymentListAnalyticsBreakdownGroup[]>(analyticsUrl(`${BASE}/breakdowns`, query))
  },
  getRankings(query: FinancePaymentListAnalyticsQuery): Promise<FinancePaymentListAnalyticsRankings> {
    return apiClient.get<FinancePaymentListAnalyticsRankings>(analyticsUrl(`${BASE}/rankings`, query))
  }
}
