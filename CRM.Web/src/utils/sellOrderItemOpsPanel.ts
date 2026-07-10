import { REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import type { SellOrderItemStockTabRow } from '@/api/salesOrder'

export function isStockingInventoryRow(row: SellOrderItemStockTabRow): boolean {
  if (row.isStockingPoolMatch) return true
  return Number(row.stockType) === 2
}

export function summarizeStockingByRegion(items: SellOrderItemStockTabRow[]) {
  let domestic = 0
  let overseas = 0
  for (const item of items) {
    if (!isStockingInventoryRow(item)) continue
    const qty = Math.max(0, Math.trunc(Number(item.qtyRepertoryAvailable) || 0))
    if (normalizeRegionType(item.regionType) === REGION_TYPE_OVERSEAS) overseas += qty
    else domestic += qty
  }
  return { domestic, overseas, total: domestic + overseas }
}

export function calcProgressPercent(done: number, total: number): number {
  if (!Number.isFinite(total) || total <= 0) return 0
  const pct = Math.round((Math.max(0, done) / total) * 100)
  return Math.min(100, Math.max(0, pct))
}
