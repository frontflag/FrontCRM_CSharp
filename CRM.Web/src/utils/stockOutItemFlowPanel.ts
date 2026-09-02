import type { StockItemFlowDoc } from '@/api/inventoryCenter'
import type { StockOutItemFlowAggregates, StockOutItemListRow } from '@/api/stockOut'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { resolveStockOutTypeLabelKey } from '@/constants/stockOutType'
import { translateSalesOrderStatus } from '@/constants/salesOrderStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import { formatFlowCardDate, resolveFlowPartyId } from '@/utils/sellOrderItemFlowPanel'
import type { FlowStationStatus, StockItemFlowCard } from '@/utils/stockItemFlowPanel'

export type StockOutItemFlowStationKey =
  | 'sellOrderItem'
  | 'stockOutNotify'
  | 'stockItem'
  | 'packing'
  | 'stockOut'

export interface StockOutItemFlowStation {
  key: StockOutItemFlowStationKey
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
  key: StockOutItemFlowStationKey,
  titleKey: string,
  cards: StockItemFlowCard[]
): StockOutItemFlowStation {
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
  if (!Number.isFinite(n) || n === 0) return null
  return n
}

function linkedCustomsId(v?: string | null): string | null {
  const s = String(v ?? '').trim()
  return s || null
}

function firstStockOutType(...vals: unknown[]): number | null {
  for (const v of vals) {
    const n = asBizType(v)
    if (n != null) return n
  }
  return null
}

function firstCustomsId(...vals: Array<string | null | undefined>): string | null {
  for (const v of vals) {
    const s = linkedCustomsId(v)
    if (s) return s
  }
  return null
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
    outbound?: StockItemFlowDoc | StockOutItemListRow | null
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
  const stockOutType = firstStockOutType(out && 'stockOutType' in out ? out.stockOutType : null, src.stockOutType)
  const outboundTypeKnown = stockOutType != null && resolveStockOutTypeLabelKey(stockOutType) !== 'unknown'
  const outCustomsId =
    out && 'customsDeclarationId' in out ? (out.customsDeclarationId as string | null | undefined) : null
  const outCustomsCode =
    out && 'customsDeclarationCode' in out ? (out.customsDeclarationCode as string | null | undefined) : null
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
    stockOutCustomsDeclarationId: outboundTypeKnown ? linkedCustomsId(outCustomsId) : null,
    stockOutCustomsDeclarationCode: outboundTypeKnown ? linkedCustomsId(outCustomsCode) : null,
    showCustomsIcon: showInboundCustomsIcon(stockInType, inboundCustomsId),
    description: null
  }
}

export function buildStockOutItemFlowStations(
  row: RowRecord | StockOutItemListRow | null | undefined,
  aggregates: StockOutItemFlowAggregates | null | undefined,
  t: TFunc,
  options?: { maskPurchase?: boolean; maskSale?: boolean }
): StockOutItemFlowStation[] {
  const mask511 = !!options?.maskPurchase
  const mask521 = !!options?.maskSale
  const F = 'inventoryStockItemList.flowPanel'
  const N = 'stockOutItemList.flowPanel'
  const rec = (row ?? null) as RowRecord | null
  const stations: StockOutItemFlowStation[] = []
  const lineVendorId = resolveFlowPartyId(mask511, rec?.vendorId)
  const lineCustomerId = resolveFlowPartyId(mask521, rec?.customerId)

  const notifyDoc = aggregates?.stockOutNotify ?? null
  const sell = aggregates?.sellOrderItem ?? null
  const packingDoc = aggregates?.packings?.[0] ?? null
  const outDoc = aggregates?.stockOuts?.[0] ?? null
  const outboundForItem: StockItemFlowDoc | StockOutItemListRow | null =
    outDoc ?? packingDoc ?? notifyDoc ?? (rec as StockOutItemListRow | null)

  {
    const cards: StockItemFlowCard[] = []
    if (sell) {
      const orderId = String(sell.sellOrderId ?? '').trim()
      const itemId = String(sell.id ?? '').trim()
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
    const id = String(src?.id ?? '').trim()
    if (src && (id || src.docCode)) {
      const stockOutType = firstStockOutType(src.stockOutType, packingDoc?.stockOutType, rec?.stockOutType)
      const customsId = firstCustomsId(src.customsDeclarationId, packingDoc?.customsDeclarationId)
      const customsCode = firstCustomsId(src.customsDeclarationCode, packingDoc?.customsDeclarationCode)
      cards.push({
        id: id || 'notify',
        docNo: dash(src.docCode),
        docRoute: id && !mask521 ? { name: 'StockOutNotifyDetail', params: { id } } : undefined,
        statusText: stockOutNotifyStatusLabel(src.status, t),
        isFinal: isStockOutNotifyFinal(src.status),
        createdAt: src.createTime,
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: false,
        showCustomer: true,
        customerId: lineCustomerId,
        customerName: maskDash(mask521, src.customerName),
        showPerson: false,
        personRoleKey: `${F}.role.salesUser`,
        personName: maskDash(mask521, src.personName),
        qtyText: formatQtyPcs(src.qty),
        qtyLabelKey: `${F}.fields.qty`,
        stockOutType,
        bizTypeText: stockOutTypeText(stockOutType, t),
        bizTypeLabelKey: `${F}.fields.stockOutType`,
        customsDeclarationId: customsId,
        customsDeclarationCode: customsCode,
        showCustomsIcon: showOutboundCustomsIcon(stockOutType, customsId),
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
    const list = sortByCreatedAsc(aggregates?.packings ?? [], (x) => x.createTime)
    const cards: StockItemFlowCard[] = list.map((x) => {
      const stockOutType = firstStockOutType(x.stockOutType, rec?.stockOutType)
      const customsId = firstCustomsId(x.customsDeclarationId)
      const customsCode = firstCustomsId(x.customsDeclarationCode)
      return {
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
        stockOutType,
        bizTypeText: stockOutTypeText(stockOutType, t),
        bizTypeLabelKey: `${F}.fields.stockOutType`,
        customsDeclarationId: customsId,
        customsDeclarationCode: customsCode,
        showCustomsIcon: showOutboundCustomsIcon(stockOutType, customsId),
        description: null
      }
    })
    stations.push(buildStation('packing', `${N}.stations.packing`, cards))
  }

  {
    const list = sortByCreatedAsc(aggregates?.stockOuts ?? [], (x) => x.createTime)
    const fallback =
      list.length === 0 && rec && rowStr(rec, 'stockOutId')
        ? [
            {
              id: rowStr(rec, 'stockOutId'),
              docCode: rowStr(rec, 'stockOutCode'),
              lineDocCode: rowStr(rec, 'stockOutItemCode') || null,
              status: Number(rec.status),
              createTime: (rec.stockOutDate as string | null) ?? null,
              customerName: (rec.customerName as string | null) ?? null,
              personName: (rec.salesUserName as string | null) ?? null,
              qty: rec.outQuantity,
              stockOutType: rec.stockOutType as number | null | undefined
            } as StockItemFlowDoc
          ]
        : list
    const cards: StockItemFlowCard[] = fallback.map((x) => {
      const headerId = String(x.id ?? rowStr(rec, 'stockOutId')).trim()
      const lineNo = dash(x.lineDocCode ?? rowStr(rec, 'stockOutItemCode'))
      const stockOutType = firstStockOutType(
        x.stockOutType,
        packingDoc?.stockOutType,
        notifyDoc?.stockOutType,
        rec?.stockOutType
      )
      const customsId = firstCustomsId(
        x.customsDeclarationId,
        packingDoc?.customsDeclarationId,
        notifyDoc?.customsDeclarationId
      )
      const customsCode = firstCustomsId(
        x.customsDeclarationCode,
        packingDoc?.customsDeclarationCode,
        notifyDoc?.customsDeclarationCode
      )
      return {
        id: `${headerId}|${lineNo}`,
        docNo: dash(x.docCode ?? rowStr(rec, 'stockOutCode')),
        docRoute:
          headerId && !mask521 ? { name: 'StockOutDetail', params: { id: headerId } } : undefined,
        lineDocNo: lineNo,
        lineDocLabelKey: 'packingDetail.flowPanel.fields.stockOutItemCode',
        lineDocRoute:
          !mask521 && lineNo !== '—'
            ? { name: 'StockOutItemList', query: { highlight: lineNo } }
            : undefined,
        statusText: stockOutStatusLabel(x.status ?? rec?.status, t),
        isFinal: isStockOutFinal(x.status ?? rec?.status),
        createdAt: x.createTime ?? (rec?.stockOutDate as string | null),
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: false,
        showCustomer: true,
        customerId: lineCustomerId,
        customerName: maskDash(mask521, x.customerName ?? (rec?.customerName as string | null)),
        showPerson: true,
        personRoleKey: `${F}.role.creator`,
        personName: maskDash(mask521, x.personName),
        qtyText: formatQtyPcs(x.qty ?? rec?.outQuantity),
        qtyLabelKey: `${F}.fields.outboundQty`,
        stockOutType,
        bizTypeText: stockOutTypeText(stockOutType, t),
        bizTypeLabelKey: `${F}.fields.stockOutType`,
        customsDeclarationId: customsId,
        customsDeclarationCode: customsCode,
        showCustomsIcon: showOutboundCustomsIcon(stockOutType, customsId),
        description: null
      }
    })
    stations.push(buildStation('stockOut', `${N}.stations.stockOut`, cards))
  }

  return stations
}

export { formatFlowCardDate as formatStockOutItemFlowCardDate }
