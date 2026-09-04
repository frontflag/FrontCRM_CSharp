import type { StockItemFlowDoc } from '@/api/inventoryCenter'
import {
  buildStockItemFlowStations,
  formatStockItemFlowCardDate,
  mapStockItemFlowDocToLayerRow,
  type FlowDocRoute,
  type FlowStationStatus,
  type StockItemFlowCard,
  type StockItemFlowStation,
  type StockItemFlowStationKey
} from '@/utils/stockItemFlowPanel'

export type { FlowDocRoute, FlowStationStatus, StockItemFlowCard, StockItemFlowStation, StockItemFlowStationKey }

export interface StockInFlowAggregates {
  stockInId: string
  stockIn: StockItemFlowDoc
  purchaseOrderItems?: StockItemFlowDoc[]
  qcs?: StockItemFlowDoc[]
  stockItems?: StockItemFlowDoc[]
  stockOutNotifies?: StockItemFlowDoc[]
  packings?: StockItemFlowDoc[]
  stockOuts?: StockItemFlowDoc[]
}

type TFunc = (key: string, ...args: unknown[]) => string
type RowRecord = Record<string, unknown>

export { formatStockItemFlowCardDate as formatStockInFlowCardDate }

export function buildStockInFlowStations(
  row: RowRecord | null | undefined,
  aggregates: StockInFlowAggregates | null | undefined,
  t: TFunc,
  options?: { maskPurchase?: boolean; maskSale?: boolean }
): StockItemFlowStation[] {
  if (!aggregates) {
    return buildStockItemFlowStations(row, null, t, options).map((s) => ({
      ...s,
      titleKey: `stockInList.flowPanel.stations.${s.key}`,
      stationStatus: 'empty' as FlowStationStatus,
      cards: []
    }))
  }

  const poCards: StockItemFlowCard[] = []
  for (const po of aggregates.purchaseOrderItems ?? []) {
    const cards = buildStockItemFlowStations(row, { stockItem: { id: '' }, purchaseOrderItem: po } as never, t, options)
    const c = cards.find((s) => s.key === 'purchaseOrderItem')?.cards[0]
    if (c) poCards.push(c)
  }

  const qcCards: StockItemFlowCard[] = []
  for (const qc of aggregates.qcs ?? []) {
    const cards = buildStockItemFlowStations(row, { stockItem: { id: '' }, qc } as never, t, options)
    const c = cards.find((s) => s.key === 'qc')?.cards[0]
    if (c) qcCards.push(c)
  }

  const stockInCards = buildStockItemFlowStations(
    row,
    { stockItem: { id: '' }, stockIn: aggregates.stockIn } as never,
    t,
    options
  )
    .find((s) => s.key === 'stockIn')
    ?.cards.map((c) => ({
      ...c,
      showVendor: true,
      vendorId: row?.vendorId as string | null | undefined,
      vendorName: options?.maskPurchase ? '—' : aggregates.stockIn.vendorName ?? (row?.vendorName as string | null)
    })) ?? []

  const stockItemCards: StockItemFlowCard[] = []
  for (const layer of aggregates.stockItems ?? []) {
    const layerRow = mapStockItemFlowDocToLayerRow(layer)
    const cards = buildStockItemFlowStations(layerRow, {
      stockItemId: layer.id,
      stockItem: layer,
      stockIn: aggregates.stockIn
    } as never, t, options)
    const c = cards.find((s) => s.key === 'stockItem')?.cards[0]
    if (c) {
      stockItemCards.push({
        ...c,
        docRoute: layer.stockAggregateId
          ? { name: 'InventoryStockDetail', params: { stockId: String(layer.stockAggregateId) } }
          : c.docRoute
      })
    }
  }

  const downstream = buildStockItemFlowStations(
    row,
    {
      stockItemId: aggregates.stockInId,
      stockItem: aggregates.stockItems?.[0] ?? aggregates.stockIn,
      stockIn: aggregates.stockIn,
      stockOutNotifies: aggregates.stockOutNotifies ?? [],
      packings: aggregates.packings ?? [],
      stockOuts: aggregates.stockOuts ?? []
    } as never,
    t,
    options
  )

  const pick = (key: StockItemFlowStationKey, cards: StockItemFlowCard[]) => {
    return {
      key,
      titleKey: `stockInList.flowPanel.stations.${key}`,
      stationStatus: cards.length === 0 ? 'empty' : cards.every((c) => c.isFinal) ? 'done' : 'active',
      cards
    } as StockItemFlowStation
  }

  return [
    pick('purchaseOrderItem', poCards),
    pick('qc', qcCards),
    pick('stockIn', stockInCards),
    pick('stockItem', stockItemCards),
    pick('stockOutNotify', downstream.find((s) => s.key === 'stockOutNotify')?.cards ?? []),
    pick('packing', downstream.find((s) => s.key === 'packing')?.cards ?? []),
    pick('stockOut', downstream.find((s) => s.key === 'stockOut')?.cards ?? [])
  ]
}
