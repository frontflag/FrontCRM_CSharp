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
