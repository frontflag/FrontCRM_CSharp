import type { RouteLocationRaw, Router } from 'vue-router'
import type { InventoryOnHandListAnalyticsQuery } from '@/api/inventoryOnHandAnalytics'

export type RankingDrillDimension = 'customer' | 'salesUser' | 'material' | 'brand'

function appendBoardFilters(
  query: Record<string, string>,
  filters: InventoryOnHandListAnalyticsQuery
) {
  const pn = filters.materialModel?.trim()
  const brand = filters.purchaseBrand?.trim()
  const wh = filters.warehouseId?.trim()
  if (pn) query.purchasePn = pn
  if (brand) query.purchaseBrand = brand
  if (wh) query.warehouseId = wh
  if (filters.stockType != null && filters.stockType >= 1 && filters.stockType <= 3) {
    query.stockType = String(filters.stockType)
  }
}

/** 库存中心看板 → 库存明细列表下钻（呆滞 KPI） */
export function buildStagnantStockItemDrillRoute(filters: InventoryOnHandListAnalyticsQuery) {
  const query: Record<string, string> = {
    drill: 'stagnant',
    stagnantOnly: 'true',
    repertoryHasStock: 'true'
  }
  appendBoardFilters(query, filters)
  return { path: '/inventory/stock-items', query }
}

/** 库存中心看板 Top 排行 → 库存明细列表下钻 */
export function buildRankingStockItemDrillRoute(
  boardFilters: InventoryOnHandListAnalyticsQuery,
  dimension: RankingDrillDimension,
  row: { id: string; name: string },
  metricMode: 'qty' | 'amount',
  currencyKey?: string,
  panelTitle?: string
) {
  const query: Record<string, string> = {
    drill: 'ranking',
    repertoryHasStock: 'true',
    rankDimension: dimension,
    rankKey: row.id,
    rankLabel: row.name
  }
  if (panelTitle?.trim()) query.rankPanel = panelTitle.trim()
  if (metricMode === 'amount' && currencyKey) query.rankCurrency = currencyKey
  appendBoardFilters(query, boardFilters)
  return { path: '/inventory/stock-items', query }
}

/** 看板下钻：在新标签页打开目标路由（保留当前看板页）。 */
export function openInventoryBoardDrillInNewTab(router: Router, to: RouteLocationRaw) {
  const { href } = router.resolve(to)
  window.open(href, '_blank', 'noopener,noreferrer')
}

export type StockItemListDrillMode = '' | 'stagnant' | 'ranking'

/** 从路由 query 解析库存明细列表筛选（看板下钻 / 物流分析等） */
export function applyStockItemListRouteQuery(
  routeQuery: Record<string, unknown>,
  targets: {
    filters: {
      stockInCode: string
      stockItemCode: string
      freightForwarderOrderNo: string
      purchasePn: string
      purchaseBrand: string
      warehouseId: string
      outboundStatus: number | undefined
      stockPresence: '' | 'has' | 'none'
      customerName: string
      vendorName: string
      salespersonUserId: string | undefined
      purchaserUserId: string | undefined
      stockType: number | undefined
      stagnantOnly: boolean
      rankDimension: string
      rankKey: string
      rankCurrency: number | undefined
    }
    dateFrom: { value: string | null }
    dateTo: { value: string | null }
    drillMode: { value: StockItemListDrillMode }
    drillRankLabel: { value: string }
    drillRankPanel: { value: string }
    drillRankCurrencyKey: { value: string }
  }
) {
  const q = routeQuery
  const str = (key: string) => {
    const v = q[key]
    if (v == null) return ''
    const s = Array.isArray(v) ? v[0] : v
    return String(s ?? '').trim()
  }
  const bool = (key: string) => {
    const s = str(key).toLowerCase()
    return s === 'true' || s === '1'
  }
  const num = (key: string) => {
    const s = str(key)
    if (!s) return undefined
    const n = Number(s)
    return Number.isFinite(n) ? n : undefined
  }

  targets.filters.stockInCode = str('stockInCode')
  targets.filters.stockItemCode = str('stockItemCode')
  targets.filters.freightForwarderOrderNo = str('freightForwarderOrderNo')
  targets.filters.purchasePn = str('purchasePn')
  targets.filters.purchaseBrand = str('purchaseBrand')
  targets.filters.warehouseId = str('warehouseId')
  targets.filters.customerName = str('customerName')
  targets.filters.vendorName = str('vendorName')
  targets.filters.salespersonUserId = str('salespersonUserId') || undefined
  targets.filters.purchaserUserId = str('purchaserUserId') || undefined

  const outbound = num('outboundStatus')
  targets.filters.outboundStatus =
    outbound != null && outbound >= 1 && outbound <= 3 ? outbound : undefined

  const stockType = num('stockType')
  targets.filters.stockType =
    stockType != null && stockType >= 1 && stockType <= 3 ? stockType : undefined

  targets.filters.stagnantOnly = bool('stagnantOnly') || str('drill') === 'stagnant'
  targets.filters.rankDimension = str('rankDimension')
  targets.filters.rankKey = str('rankKey')
  const rankCurrency = num('rankCurrency')
  targets.filters.rankCurrency =
    rankCurrency != null && rankCurrency >= 1 ? rankCurrency : undefined

  if (bool('repertoryHasStock')) targets.filters.stockPresence = 'has'
  else if (str('repertoryHasStock').toLowerCase() === 'false') targets.filters.stockPresence = 'none'
  else if (targets.filters.stagnantOnly || targets.filters.rankDimension) {
    targets.filters.stockPresence = 'has'
  }

  targets.dateFrom.value = str('stockInDateFrom') || null
  targets.dateTo.value = str('stockInDateTo') || null

  const drill = str('drill')
  targets.drillMode.value =
    drill === 'stagnant' ? 'stagnant' : drill === 'ranking' ? 'ranking' : ''
  targets.drillRankLabel.value = str('rankLabel')
  targets.drillRankPanel.value = str('rankPanel')
  targets.drillRankCurrencyKey.value = str('rankCurrency')
}
