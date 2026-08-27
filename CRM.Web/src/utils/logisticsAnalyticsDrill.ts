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
