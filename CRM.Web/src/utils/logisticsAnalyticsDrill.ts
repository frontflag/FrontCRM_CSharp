import type { RouteLocationRaw } from 'vue-router'
import type { LogisticsAnalyticsScopeContext, LogisticsInventoryType } from '@/api/analytics/logistics'

export interface LogisticsDrillScope {
  inventoryType?: LogisticsInventoryType
  customerName?: string
  vendorName?: string
  purchaseBrand?: string
  salespersonUserId?: string
  purchaserUserId?: string
  scopeContext?: LogisticsAnalyticsScopeContext
}

/** 入库单列表 URL：物流分析入库金额原币下钻 */
export const LOGISTICS_ANALYTICS_STOCK_IN_DRILL = 'logistics-analytics-stock-in'

/** 已过账 / 已入库 */
export const STOCK_IN_POSTED_STATUS = 2

/** 采购入库（列表将历史类型 1 一并视为采购） */
export const STOCK_IN_PURCHASE_TYPE = 10

function firstRouteQueryValue(v: unknown): string | undefined {
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

/** 物流公司视角 + 库存类型全部 + 未脱敏 + inventory.read */
export function canShowStockInCurrencyView(opts: {
  viewLevel?: string | null
  accessMode?: string | null
  inventoryType?: LogisticsInventoryType | string | null
  maskAmounts: boolean
  hasInventoryRead: boolean
}): boolean {
  return (
    opts.viewLevel === 'company' &&
    opts.accessMode === 'logistics' &&
    (opts.inventoryType ?? 'all') === 'all' &&
    !opts.maskAmounts &&
    opts.hasInventoryRead
  )
}

/** 入库金额原币行 → 入库单列表（已过账采购入库 + 明细原币 + 入库日期 = 趋势区间） */
export function buildStockInAmountCurrencyDrillRoute(
  scope: { dateFrom?: string; dateTo?: string },
  currency: number
): RouteLocationRaw | null {
  if (!Number.isFinite(currency)) return null
  const query: Record<string, string> = {
    drill: LOGISTICS_ANALYTICS_STOCK_IN_DRILL,
    stockInType: String(STOCK_IN_PURCHASE_TYPE),
    status: String(STOCK_IN_POSTED_STATUS),
    itemCurrency: String(currency)
  }
  if (scope.dateFrom) query.stockInDateStart = scope.dateFrom
  if (scope.dateTo) query.stockInDateEnd = scope.dateTo
  return { path: '/inventory/stock-in', query }
}

export type StockInAmountCurrencyDrillQuery = {
  isDrill: boolean
  itemCurrency?: number
  status?: number
  stockInDateStart?: string
  stockInDateEnd?: string
}

export function parseStockInAmountCurrencyDrillQuery(
  query: Record<string, unknown>
): StockInAmountCurrencyDrillQuery {
  const drill = firstRouteQueryValue(query.drill)
  return {
    isDrill: drill === LOGISTICS_ANALYTICS_STOCK_IN_DRILL,
    itemCurrency: parseOptionalInt(firstRouteQueryValue(query.itemCurrency)),
    status: parseOptionalInt(firstRouteQueryValue(query.status)),
    stockInDateStart: firstRouteQueryValue(query.stockInDateStart),
    stockInDateEnd: firstRouteQueryValue(query.stockInDateEnd)
  }
}

export function buildStockItemListDrillRoute(scope: LogisticsDrillScope) {
  const query: Record<string, string> = {
    repertoryHasStock: 'true'
  }
  if (scope.customerName) query.customerName = scope.customerName
  if (scope.vendorName) query.vendorName = scope.vendorName
  if (scope.purchaseBrand) query.purchaseBrand = scope.purchaseBrand
  if (scope.salespersonUserId) query.salespersonUserId = scope.salespersonUserId
  if (scope.purchaserUserId) query.purchaserUserId = scope.purchaserUserId
  return { path: '/inventory/stock-items', query }
}

export function buildPendingStockInDrillRoute(scope: LogisticsDrillScope) {
  const query: Record<string, string> = {}
  if (scope.inventoryType === 'customerOrder') query.type = '1'
  if (scope.inventoryType === 'purchaseStock') query.type = '2'
  if (scope.scopeContext?.resolvedOwnerUserId) query.purchaseUserId = scope.scopeContext.resolvedOwnerUserId
  return { path: '/purchase-order-items', query }
}

export function buildStockInFlowDrillRoute(scope: { dateFrom?: string; dateTo?: string }) {
  const query: Record<string, string> = {
    stockInType: '10'
  }
  if (scope.dateFrom) query.stockInDateStart = scope.dateFrom
  if (scope.dateTo) query.stockInDateEnd = scope.dateTo
  return { path: '/inventory/stock-in', query }
}

export function buildStockOutFlowDrillRoute(scope: { dateFrom?: string; dateTo?: string }) {
  const query: Record<string, string> = {
    status: '4',
    stockOutType: '10'
  }
  if (scope.dateFrom) query.stockOutDateFrom = scope.dateFrom
  if (scope.dateTo) query.stockOutDateTo = scope.dateTo
  return { path: '/inventory/stock-out/items', query }
}

/** 出库明细列表 URL：物流分析出库金额原币下钻 */
export const LOGISTICS_ANALYTICS_STOCK_OUT_DRILL = 'logistics-analytics-stock-out'

/** 出库完成 */
export const STOCK_OUT_FINISHED_STATUS = 4

/** 销售出库（列表将历史类型 1 一并视为销售） */
export const STOCK_OUT_SALES_TYPE = 10

/** 物流公司视角 + 库存类型全部 + 未销售脱敏 + inventory.read */
export function canShowStockOutCurrencyView(opts: {
  viewLevel?: string | null
  accessMode?: string | null
  inventoryType?: LogisticsInventoryType | string | null
  maskSalesAmounts: boolean
  hasInventoryRead: boolean
}): boolean {
  return (
    opts.viewLevel === 'company' &&
    opts.accessMode === 'logistics' &&
    (opts.inventoryType ?? 'all') === 'all' &&
    !opts.maskSalesAmounts &&
    opts.hasInventoryRead
  )
}

/** 出库金额原币行 → 出库明细（出库完成 + 销售出库 + 销售原币 + 出库日期 = 趋势区间） */
export function buildStockOutAmountCurrencyDrillRoute(
  scope: { dateFrom?: string; dateTo?: string },
  currency: number
): RouteLocationRaw | null {
  if (!Number.isFinite(currency)) return null
  const query: Record<string, string> = {
    drill: LOGISTICS_ANALYTICS_STOCK_OUT_DRILL,
    stockOutType: String(STOCK_OUT_SALES_TYPE),
    status: String(STOCK_OUT_FINISHED_STATUS),
    salesCurrency: String(currency)
  }
  if (scope.dateFrom) query.stockOutDateFrom = scope.dateFrom
  if (scope.dateTo) query.stockOutDateTo = scope.dateTo
  return { path: '/inventory/stock-out/items', query }
}

export type StockOutAmountCurrencyDrillQuery = {
  isDrill: boolean
  salesCurrency?: number
  status?: number
  stockOutDateFrom?: string
  stockOutDateTo?: string
}

export function parseStockOutAmountCurrencyDrillQuery(
  query: Record<string, unknown>
): StockOutAmountCurrencyDrillQuery {
  const drill = firstRouteQueryValue(query.drill)
  return {
    isDrill: drill === LOGISTICS_ANALYTICS_STOCK_OUT_DRILL,
    salesCurrency: parseOptionalInt(firstRouteQueryValue(query.salesCurrency)),
    status: parseOptionalInt(firstRouteQueryValue(query.status)),
    stockOutDateFrom: firstRouteQueryValue(query.stockOutDateFrom),
    stockOutDateTo: firstRouteQueryValue(query.stockOutDateTo)
  }
}
