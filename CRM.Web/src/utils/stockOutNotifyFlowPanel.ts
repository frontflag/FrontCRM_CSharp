import type { StockItemFlowDoc } from '@/api/inventoryCenter'
import type { StockOutNotifyFlowAggregates, StockOutRequestDto } from '@/api/stockOut'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { resolveStockOutTypeLabelKey } from '@/constants/stockOutType'
import { translateSalesOrderStatus } from '@/constants/salesOrderStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import { formatFlowCardDate, resolveFlowPartyId } from '@/utils/sellOrderItemFlowPanel'
import type { FlowStationStatus, StockItemFlowCard } from '@/utils/stockItemFlowPanel'

export type StockOutNotifyFlowStationKey =
  | 'sellOrderItem'
  | 'stockOutNotify'
  | 'stockItem'
  | 'stockingStockItem'
  | 'packing'
  | 'stockOut'

export interface StockOutNotifyFlowStation {
  key: StockOutNotifyFlowStationKey
  titleKey: string
  stationStatus: FlowStationStatus
  cards: StockItemFlowCard[]
}

type TFunc = (key: string, ...args: unknown[]) => string
type RowRecord = Record<string, unknown>

function dash(v?: string | null) {
  const s = String(v ?? '').trim()
  return s || '—'
}

function maskDash(mask: boolean, v?: string | null) {
  if (mask) return '—'
  return dash(v)
}

function sortByCreatedAsc<T>(items: T[], getTime: (x: T) => string | null | undefined): T[] {
  return [...items].sort((a, b) => {
    const ta = Date.parse(String(getTime(a) ?? '')) || 0
    const tb = Date.parse(String(getTime(b) ?? '')) || 0
    return ta - tb
  })
}

function stationStatusFromCards(cards: StockItemFlowCard[]): FlowStationStatus {
  if (cards.length === 0) return 'empty'
  if (cards.every((c) => c.isFinal)) return 'done'
  return 'active'
}

function buildStation(
  key: StockOutNotifyFlowStationKey,
  titleKey: string,
  cards: StockItemFlowCard[]
): StockOutNotifyFlowStation {
  return {
    key,
    titleKey,
    stationStatus: stationStatusFromCards(cards),
    cards
  }
}

function formatQtyPcs(qty: unknown): string {
  const n = Number(qty)
  if (!Number.isFinite(n)) return '—'
  return `${Math.trunc(n)} pcs`
}

function asBizType(v: unknown): number | null {
  if (v == null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

function linkedCustomsId(v?: string | null): string | null {
  const s = String(v ?? '').trim()
  return s || null
}

function showInboundCustomsIcon(type: unknown, declarationId?: string | null): boolean {
  return resolveStockInTypeLabelKey(asBizType(type)) === 'customs' && !!linkedCustomsId(declarationId)
}

function showOutboundCustomsIcon(type: unknown, declarationId?: string | null): boolean {
  return resolveStockOutTypeLabelKey(asBizType(type)) === 'customs' && !!linkedCustomsId(declarationId)
}

function stockInTypeText(v: unknown, t: TFunc): string {
  const key = resolveStockInTypeLabelKey(asBizType(v))
  return t(`stockInList.stockInTypeLabels.${key}`)
}

function stockOutTypeText(v: unknown, t: TFunc): string {
  const key = resolveStockOutTypeLabelKey(asBizType(v))
  return t(`stockOutList.stockOutTypeLabels.${key}`)
}

function outboundStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === 1) return t('inventoryStockItemList.filters.outboundNone')
  if (s === 2) return t('inventoryStockItemList.filters.outboundPartial')
  if (s === 3) return t('inventoryStockItemList.filters.outboundDone')
  return '—'
}

function isStockItemFinal(v: unknown) {
  return Number(v) === 3
}

function stockOutNotifyStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function isStockOutNotifyFinal(v: unknown) {
  const s = Number(v)
  return s === STOCK_OUT_REQUEST_STATUS.StockedOut || s === STOCK_OUT_REQUEST_STATUS.Cancelled
}

function isPackingFinal(v: unknown) {
  return Number(v) === 100
}

function stockOutStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === 0) return t('stockOutList.status.draft')
  if (s === 1) return t('stockOutList.status.pending')
  if (s === 2) return t('stockOutList.status.done')
  if (s === 3) return t('stockOutList.status.cancelled')
  if (s === 4) return t('stockOutList.status.finished')
  return Number.isFinite(s) ? String(s) : '—'
}

function isStockOutFinal(v: unknown) {
  const s = Number(v)
  return s === 2 || s === 3 || s === 4
}

function isSalesOrderFinal(v: unknown) {
  const s = Number(v)
  return s < 0 || s === 100 || s === 110 || s === 120
}

function rowStr(row: RowRecord | null | undefined, ...keys: string[]): string {
  if (!row) return ''
  for (const k of keys) {
    const s = String(row[k] ?? '').trim()
    if (s) return s
  }
  return ''
}

function mapStockItemCard(
  src: StockItemFlowDoc,
  t: TFunc,
  options: {
    mask511: boolean
    mask521: boolean
    outbound?: StockItemFlowDoc | null
    vendorId?: string | null
    customerId?: string | null
  }
): StockItemFlowCard {
  const F = 'inventoryStockItemList.flowPanel'
  const itemId = String(src.id ?? '').trim() || 'stock-item'
  const aggregateId = String(src.stockAggregateId ?? '').trim()
  const customerName = maskDash(options.mask521, src.customerName)
  const hasSalesLink = String(src.customerName ?? '').trim().length > 0 || src.salesUnitPrice != null
  const stockInType = src.stockInType
  const inboundCustomsId = linkedCustomsId(src.customsDeclarationId)
  const inboundCustomsCode = linkedCustomsId(src.customsDeclarationCode)
  const out = options.outbound
  const stockOutType = asBizType(out?.stockOutType ?? src.stockOutType)
  const outboundTypeKnown = stockOutType != null && resolveStockOutTypeLabelKey(stockOutType) !== 'unknown'
  return {
    id: itemId,
    docNo: dash(src.docCode),
    docRoute: aggregateId ? { name: 'InventoryStockDetail', params: { stockId: aggregateId } } : undefined,
    statusText: outboundStatusLabel(src.status, t),
    isFinal: isStockItemFinal(src.status),
    createdAt: src.bizDate ?? src.createTime,
    createdAtLabelKey: `${F}.fields.stockInDate`,
    showVendor: true,
    vendorId: options.vendorId ?? null,
    vendorName: maskDash(options.mask511, src.vendorName),
    showCustomer: hasSalesLink,
    customerId: options.customerId ?? null,
    customerName,
    showPerson: false,
    personRoleKey: `${F}.role.salesUser`,
    personName: maskDash(options.mask521, src.personName),
    unitPriceText: options.mask511
      ? '—'
      : formatUnitPriceWithCurrencyCodeSuffix(src.unitPrice, Number(src.currency)),
    salesPriceText: hasSalesLink
      ? options.mask521
        ? '—'
        : formatUnitPriceWithCurrencyCodeSuffix(src.salesUnitPrice, Number(src.salesCurrency))
      : null,
    qtyText: formatQtyPcs(src.qty),
    qtyLabelKey: `${F}.fields.inboundQty`,
    qty2Text: formatQtyPcs(src.qty2),
    qty2LabelKey: `${F}.fields.outboundQty`,
    stockInType: asBizType(stockInType),
    stockOutType: outboundTypeKnown ? stockOutType : null,
    bizTypeText: stockInTypeText(stockInType, t),
    bizTypeLabelKey: `${F}.fields.stockInType`,
    customsDeclarationId: inboundCustomsId,
    customsDeclarationCode: inboundCustomsCode,
    stockOutCustomsDeclarationId: outboundTypeKnown ? linkedCustomsId(out?.customsDeclarationId) : null,
    stockOutCustomsDeclarationCode: outboundTypeKnown ? linkedCustomsId(out?.customsDeclarationCode) : null,
    showCustomsIcon: showInboundCustomsIcon(stockInType, inboundCustomsId),
    description: null
  }
}

export function buildStockOutNotifyFlowStations(
  row: RowRecord | StockOutRequestDto | null | undefined,
  aggregates: StockOutNotifyFlowAggregates | null | undefined,
  t: TFunc,
  options?: { maskPurchase?: boolean; maskSale?: boolean }
): StockOutNotifyFlowStation[] {
  const mask511 = !!options?.maskPurchase
  const mask521 = !!options?.maskSale
  const F = 'inventoryStockItemList.flowPanel'
  const N = 'stockOutNotifyList.flowPanel'
  const rec = (row ?? null) as RowRecord | null
  const stations: StockOutNotifyFlowStation[] = []
  const lineVendorId = resolveFlowPartyId(mask511, rec?.vendorId)
  const lineCustomerId = resolveFlowPartyId(mask521, rec?.customerId)

  const notifyDoc = aggregates?.stockOutNotify ?? null
  const sell = aggregates?.sellOrderItem ?? null
  const outboundForItem = notifyDoc ?? (aggregates?.packings?.[0] ?? aggregates?.stockOuts?.[0] ?? null)

  {
    const cards: StockItemFlowCard[] = []
    if (sell) {
      const orderId = String(sell.sellOrderId ?? rowStr(rec, 'salesOrderId')).trim()
      const itemId = String(sell.id ?? rowStr(rec, 'salesOrderItemId')).trim()
      const status = Number(sell.status)
      cards.push({
        id: itemId || 'soi',
        docNo: dash(sell.docCode),
        docRoute:
          orderId && !mask521
            ? {
                name: 'SalesOrderDetail',
                params: { id: orderId },
                query: itemId ? { sellOrderItemId: itemId } : undefined
              }
            : undefined,
        statusText: Number.isFinite(status) ? translateSalesOrderStatus(status, t) : '—',
        isFinal: isSalesOrderFinal(status),
        createdAt: sell.createTime,
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: false,
        showCustomer: true,
        customerId: lineCustomerId,
        customerName: maskDash(mask521, sell.customerName),
        showPerson: true,
        personRoleKey: `${F}.role.salesUser`,
        personName: maskDash(mask521, sell.personName),
        salesPriceText: mask521
          ? '—'
          : formatUnitPriceWithCurrencyCodeSuffix(sell.salesUnitPrice, Number(sell.salesCurrency)),
        qtyText: formatQtyPcs(sell.qty),
        qtyLabelKey: `${F}.fields.qty`,
        description: null
      })
    }
    stations.push(buildStation('sellOrderItem', `${N}.stations.sellOrderItem`, cards))
  }

  {
    const src = notifyDoc
    const cards: StockItemFlowCard[] = []
    const id = String(src?.id ?? rowStr(rec, 'id')).trim()
    if (id || src || rec) {
      const stockOutType = asBizType(src?.stockOutType ?? rec?.stockOutType)
      cards.push({
        id: id || 'notify',
        docNo: dash(src?.docCode ?? rowStr(rec, 'requestCode')),
        docRoute: id && !mask521 ? { name: 'StockOutNotifyDetail', params: { id } } : undefined,
        statusText: stockOutNotifyStatusLabel(src?.status ?? rec?.status, t),
        isFinal: isStockOutNotifyFinal(src?.status ?? rec?.status),
        createdAt: src?.createTime ?? (rec?.createTime as string | null),
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: false,
        showCustomer: true,
        customerId: lineCustomerId,
        customerName: maskDash(mask521, src?.customerName ?? (rec?.customerName as string | null)),
        showPerson: false,
        personRoleKey: `${F}.role.salesUser`,
        personName: maskDash(mask521, src?.personName ?? (rec?.salesUserName as string | null)),
        qtyText: formatQtyPcs(src?.qty ?? rec?.outQuantity),
        qtyLabelKey: `${F}.fields.qty`,
        stockOutType,
        bizTypeText: stockOutTypeText(stockOutType, t),
        bizTypeLabelKey: `${F}.fields.stockOutType`,
        customsDeclarationId: linkedCustomsId(
          src?.customsDeclarationId ?? (rec?.customsDeclarationId as string | null)
        ),
        customsDeclarationCode: linkedCustomsId(
          src?.customsDeclarationCode ?? (rec?.customsDeclarationCode as string | null)
        ),
        showCustomsIcon: showOutboundCustomsIcon(
          stockOutType,
          src?.customsDeclarationId ?? (rec?.customsDeclarationId as string | null)
        ),
        description: null
      })
    }
    stations.push(buildStation('stockOutNotify', `${N}.stations.stockOutNotify`, cards))
  }

  {
    const list = sortByCreatedAsc(aggregates?.stockItems ?? [], (x) => x.bizDate ?? x.createTime)
    const cards = list.map((x) =>
      mapStockItemCard(x, t, {
        mask511,
        mask521,
        outbound: outboundForItem,
        vendorId: lineVendorId,
        customerId: lineCustomerId
      })
    )
    stations.push(buildStation('stockItem', `${N}.stations.stockItem`, cards))
  }

  {
    const list = sortByCreatedAsc(aggregates?.stockingStockItems ?? [], (x) => x.bizDate ?? x.createTime)
    if (list.length > 0) {
      const cards = list.map((x) =>
        mapStockItemCard(x, t, { mask511, mask521, outbound: null, vendorId: lineVendorId, customerId: lineCustomerId })
      )
      stations.push(buildStation('stockingStockItem', `${N}.stations.stockingStockItem`, cards))
    }
  }

  {
    const list = sortByCreatedAsc(aggregates?.packings ?? [], (x) => x.createTime)
    const cards: StockItemFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.docCode),
      docRoute: !mask521 ? { name: 'PackingDetail', params: { id: x.id } } : undefined,
      statusText: packingStatusLabel(Number(x.status)),
      isFinal: isPackingFinal(x.status),
      createdAt: x.createTime,
      createdAtLabelKey: `${F}.fields.createdAt`,
      showVendor: false,
      showCustomer: true,
      customerId: lineCustomerId,
      customerName: maskDash(mask521, x.customerName),
      showPerson: false,
      personRoleKey: `${F}.role.salesUser`,
      personName: maskDash(mask521, x.personName),
      qtyText: formatQtyPcs(x.qty),
      qtyLabelKey: `${F}.fields.packingQty`,
      stockOutType: asBizType(x.stockOutType),
      bizTypeText: stockOutTypeText(x.stockOutType, t),
      bizTypeLabelKey: `${F}.fields.stockOutType`,
      customsDeclarationId: linkedCustomsId(x.customsDeclarationId),
      customsDeclarationCode: linkedCustomsId(x.customsDeclarationCode),
      showCustomsIcon: showOutboundCustomsIcon(x.stockOutType, x.customsDeclarationId),
      description: null
    }))
    stations.push(buildStation('packing', `${N}.stations.packing`, cards))
  }

  {
    const list = sortByCreatedAsc(aggregates?.stockOuts ?? [], (x) => x.createTime)
    const cards: StockItemFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.docCode),
      docRoute: !mask521 ? { name: 'StockOutDetail', params: { id: x.id } } : undefined,
      statusText: stockOutStatusLabel(x.status, t),
      isFinal: isStockOutFinal(x.status),
      createdAt: x.createTime,
      createdAtLabelKey: `${F}.fields.createdAt`,
      showVendor: false,
      showCustomer: true,
      customerId: lineCustomerId,
      customerName: maskDash(mask521, x.customerName),
      showPerson: false,
      personRoleKey: `${F}.role.salesUser`,
      personName: maskDash(mask521, x.personName),
      qtyText: formatQtyPcs(x.qty),
      qtyLabelKey: `${F}.fields.outboundQty`,
      stockOutType: asBizType(x.stockOutType),
      bizTypeText: stockOutTypeText(x.stockOutType, t),
      bizTypeLabelKey: `${F}.fields.stockOutType`,
      customsDeclarationId: linkedCustomsId(x.customsDeclarationId),
      customsDeclarationCode: linkedCustomsId(x.customsDeclarationCode),
      showCustomsIcon: showOutboundCustomsIcon(x.stockOutType, x.customsDeclarationId),
      description: null
    }))
    stations.push(buildStation('stockOut', `${N}.stations.stockOut`, cards))
  }

  return stations
}

export { formatFlowCardDate as formatStockOutNotifyFlowCardDate }
