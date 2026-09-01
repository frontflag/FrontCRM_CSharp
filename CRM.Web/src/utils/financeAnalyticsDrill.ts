import type { RouteLocationRaw } from 'vue-router'
import type { FinanceAnalyticsScopeContext } from '@/api/analytics/finance'

export interface FinanceAnalyticsDrillScope {
  dateFrom?: string
  dateTo?: string
  ownerUserId?: string
  scopeContext?: FinanceAnalyticsScopeContext | null
}

export type FinanceAnalyticsTodoDrillKey =
  | 'payable'
  | 'receivable'
  | 'pendingPurchaseInvoice'
  | 'pendingSellInvoice'

export type FinanceAnalyticsCompletedDrillKey =
  | 'paid'
  | 'received'
  | 'issuedPurchaseInvoice'
  | 'issuedSellInvoice'

/** 付款列表 URL：财务分析已付款原币下钻 */
export const FINANCE_ANALYTICS_PAID_DRILL = 'finance-analytics-paid'

/** 收款列表 URL：财务分析已收款原币下钻 */
export const FINANCE_ANALYTICS_RECEIVED_DRILL = 'finance-analytics-received'

/** 付款完成 */
export const FINANCE_PAYMENT_COMPLETE_STATUS = 100

/** 收款确认 */
export const FINANCE_RECEIPT_CONFIRMED_STATUS = 3

function scopeOwnerUserId(scope: FinanceAnalyticsDrillScope): string | undefined {
  return scope.ownerUserId?.trim() || scope.scopeContext?.resolvedOwnerUserId?.trim() || undefined
}

function dateRangeQuery(scope: FinanceAnalyticsDrillScope): Record<string, string> {
  const q: Record<string, string> = {}
  if (scope.dateFrom) q.startDate = scope.dateFrom
  if (scope.dateTo) q.endDate = scope.dateTo
  return q
}

function ownerQuery(scope: FinanceAnalyticsDrillScope): Record<string, string> {
  const uid = scopeOwnerUserId(scope)
  return uid ? { ownerUserId: uid } : {}
}

export function firstRouteQueryValue(v: unknown): string | undefined {
  if (Array.isArray(v)) {
    const x = v[0]
    return typeof x === 'string' && x.trim() ? x.trim() : undefined
  }
  if (typeof v === 'string' && v.trim()) return v.trim()
  return undefined
}

function parseOptionalInt(raw: string | undefined): number | undefined {
  if (raw == null || !/^-?\d+$/.test(raw)) return undefined
  const n = Number(raw)
  return Number.isFinite(n) ? n : undefined
}

export function canShowPaidCurrencyView(opts: {
  viewLevel?: string | null
  accessMode?: string | null
  maskAmounts: boolean
  hasPaymentRead: boolean
}): boolean {
  return canShowFinanceCompanyCurrencyView({
    viewLevel: opts.viewLevel,
    accessMode: opts.accessMode,
    maskAmounts: opts.maskAmounts,
    hasListRead: opts.hasPaymentRead
  })
}

export function canShowReceivedCurrencyView(opts: {
  viewLevel?: string | null
  accessMode?: string | null
  maskAmounts: boolean
  hasReceiptRead: boolean
}): boolean {
  return canShowFinanceCompanyCurrencyView({
    viewLevel: opts.viewLevel,
    accessMode: opts.accessMode,
    maskAmounts: opts.maskAmounts,
    hasListRead: opts.hasReceiptRead
  })
}

function canShowFinanceCompanyCurrencyView(opts: {
  viewLevel?: string | null
  accessMode?: string | null
  maskAmounts: boolean
  hasListRead: boolean
}): boolean {
  return (
    opts.viewLevel === 'company' &&
    opts.accessMode === 'finance' &&
    !opts.maskAmounts &&
    opts.hasListRead
  )
}

/** 已付款原币行 → 付款记录（完成 + 付款币别 + KPI 区间起～截至） */
export function buildPaidCurrencyDrillRoute(
  scope: Pick<FinanceAnalyticsDrillScope, 'dateFrom' | 'dateTo'>,
  currency: number
): RouteLocationRaw | null {
  if (!Number.isFinite(currency)) return null
  const query: Record<string, string> = {
    drill: FINANCE_ANALYTICS_PAID_DRILL,
    status: String(FINANCE_PAYMENT_COMPLETE_STATUS),
    paymentCurrency: String(currency),
    ...dateRangeQuery(scope)
  }
  return { name: 'FinancePaymentList', query }
}

/** 已收款原币行 → 收款记录（确认 + 收款币别 + 收款日期 = KPI 区间起～截至） */
export function buildReceivedCurrencyDrillRoute(
  scope: Pick<FinanceAnalyticsDrillScope, 'dateFrom' | 'dateTo'>,
  currency: number
): RouteLocationRaw | null {
  if (!Number.isFinite(currency)) return null
  const query: Record<string, string> = {
    drill: FINANCE_ANALYTICS_RECEIVED_DRILL,
    status: String(FINANCE_RECEIPT_CONFIRMED_STATUS),
    receiptCurrency: String(currency)
  }
  if (scope.dateFrom) query.receiptDateFrom = scope.dateFrom
  if (scope.dateTo) query.receiptDateTo = scope.dateTo
  return { name: 'FinanceReceiptList', query }
}

export type PaidCurrencyDrillQuery = {
  isDrill: boolean
  paymentCurrency?: number
  status?: number
  startDate?: string
  endDate?: string
}

export function parsePaidCurrencyDrillQuery(
  query: Record<string, unknown>
): PaidCurrencyDrillQuery {
  const drill = firstRouteQueryValue(query.drill)
  return {
    isDrill: drill === FINANCE_ANALYTICS_PAID_DRILL,
    paymentCurrency: parseOptionalInt(firstRouteQueryValue(query.paymentCurrency)),
    status: parseOptionalInt(firstRouteQueryValue(query.status)),
    startDate: firstRouteQueryValue(query.startDate),
    endDate: firstRouteQueryValue(query.endDate)
  }
}

export type ReceivedCurrencyDrillQuery = {
  isDrill: boolean
  receiptCurrency?: number
  status?: number
  receiptDateFrom?: string
  receiptDateTo?: string
}

export function parseReceivedCurrencyDrillQuery(
  query: Record<string, unknown>
): ReceivedCurrencyDrillQuery {
  const drill = firstRouteQueryValue(query.drill)
  return {
    isDrill: drill === FINANCE_ANALYTICS_RECEIVED_DRILL,
    receiptCurrency: parseOptionalInt(firstRouteQueryValue(query.receiptCurrency)),
    status: parseOptionalInt(firstRouteQueryValue(query.status)),
    receiptDateFrom: firstRouteQueryValue(query.receiptDateFrom),
    receiptDateTo: firstRouteQueryValue(query.receiptDateTo)
  }
}

export function buildTodoDrillRoute(
  key: FinanceAnalyticsTodoDrillKey,
  scope: FinanceAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = ownerQuery(scope)
  switch (key) {
    case 'payable':
      return { name: 'FinancePaymentList', query: { onlyOpen: '1', ...userQ } }
    case 'receivable':
      return { name: 'FinanceReceivableList', query: { onlyOpen: '1', ...userQ } }
    case 'pendingPurchaseInvoice':
      return { name: 'FinancePurchaseInvoiceList', query: { onlyOpen: '1', ...userQ } }
    case 'pendingSellInvoice':
      return { name: 'FinanceSellInvoiceList', query: { onlyOpen: '1', ...userQ } }
    default:
      return null
  }
}

export function buildCompletedDrillRoute(
  key: FinanceAnalyticsCompletedDrillKey,
  scope: FinanceAnalyticsDrillScope
): RouteLocationRaw | null {
  const userQ = ownerQuery(scope)
  const dateQ = dateRangeQuery(scope)
  switch (key) {
    case 'paid':
      return null
    case 'received':
      return null
    case 'issuedPurchaseInvoice':
      return { name: 'FinancePurchaseInvoiceList', query: { ...dateQ, ...userQ } }
    case 'issuedSellInvoice':
      return { name: 'FinanceSellInvoiceList', query: { ...dateQ, ...userQ } }
    default:
      return null
  }
}

export function isTodoDrillable(key: FinanceAnalyticsTodoDrillKey, maskAmounts: boolean): boolean {
  switch (key) {
    case 'payable':
    case 'receivable':
    case 'pendingPurchaseInvoice':
    case 'pendingSellInvoice':
      return !maskAmounts
  }
}

export function isCompletedDrillable(key: FinanceAnalyticsCompletedDrillKey, maskAmounts: boolean): boolean {
  switch (key) {
    case 'paid':
    case 'received':
      return false
    case 'issuedPurchaseInvoice':
    case 'issuedSellInvoice':
      return !maskAmounts
  }
}
