import type { StockItemFlowAggregates, StockItemFlowDoc } from '@/api/inventoryCenter'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { resolveStockOutTypeLabelKey } from '@/constants/stockOutType'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import { formatFlowCardDate, resolveFlowPartyId } from '@/utils/sellOrderItemFlowPanel'

export type FlowStationStatus = 'empty' | 'active' | 'done'

export type StockItemFlowStationKey =
  | 'purchaseOrderItem'
  | 'qc'
  | 'stockIn'
  | 'stockItem'
  | 'stockOutNotify'
  | 'packing'
  | 'stockOut'

export interface FlowDocRoute {
  name: string
  params?: Record<string, string>
  query?: Record<string, string>
}

export interface StockItemFlowCard {
  id: string
  docNo: string
  docRoute?: FlowDocRoute
  statusText: string
  isFinal: boolean
  createdAt?: string | null
  createdAtLabelKey: string
  showVendor: boolean
  vendorId?: string | null
  vendorName?: string | null
  showCustomer: boolean
  customerId?: string | null
  customerName?: string | null
  showPerson: boolean
  personRoleKey: string
  personName?: string | null
  unitPriceText?: string | null
  salesPriceText?: string | null
  qtyText?: string | null
  qtyLabelKey: string
  qty2Text?: string | null
  qty2LabelKey?: string
  description?: string | null
  bizTypeText?: string | null
  bizTypeLabelKey?: string
  stockInType?: number | null
  stockOutType?: number | null
  /** 入库类型对应的报关单（报关入库且有关联时显示图标） */
  customsDeclarationId?: string | null
  customsDeclarationCode?: string | null
  /** 库存明细站：出库类型对应的报关单 */
  stockOutCustomsDeclarationId?: string | null
  stockOutCustomsDeclarationCode?: string | null
  /** 报关入库 / 报关出库且已关联报关单时为 true */
  showCustomsIcon?: boolean
  lineDocNo?: string | null
  lineDocLabelKey?: string
  lineDocRoute?: FlowDocRoute
}

export interface StockItemFlowStation {
  key: StockItemFlowStationKey
  titleKey: string
  stationStatus: FlowStationStatus
  cards: StockItemFlowCard[]
}

type TFunc = (key: string, ...args: unknown[]) => string
type RowRecord = Record<string, unknown>

const PO_ITEM_STATUS_TEXT: Record<number, string> = {
  1: '新建',
  2: '待审核',
  10: '审核通过',
  20: '待确认',
  30: '已确认',
  40: '已付款',
  50: '已发货',
  60: '已入库',
  100: '采购完成',
  [-1]: '审核失败',
  [-2]: '取消'
}

function dash(v?: string | null) {
  const s = String(v ?? '').trim()
  return s || '—'
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
  key: StockItemFlowStationKey,
  titleKey: string,
  cards: StockItemFlowCard[]
): StockItemFlowStation {
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

function poItemStatusLabel(v: unknown): string {
  const s = Number(v)
  return Number.isFinite(s) ? (PO_ITEM_STATUS_TEXT[s] ?? String(s)) : '—'
}

function isPoItemFinal(v: unknown) {
  const s = Number(v)
  return s === 100 || s === -1 || s === -2
}

function qcStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === -1) return t('qcList.qcStatus.failed')
  if (s === 10) return t('qcList.qcStatus.partial')
  if (s === 100) return t('qcList.qcStatus.passed')
  return t('qcList.qcStatus.unknown')
}

function isQcFinal(v: unknown) {
  const s = Number(v)
  return s === 100 || s === -1
}

function stockInStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === 0) return t('stockInList.status.draft')
  if (s === 1) return t('stockInList.status.pending')
  if (s === 2) return t('stockInList.status.done')
  if (s === 3) return t('stockInList.status.cancelled')
  return Number.isFinite(s) ? String(s) : '—'
}

function isStockInFinal(v: unknown) {
  const s = Number(v)
  return s === 2 || s === 3
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

function asBizType(v: unknown): number | null {
  if (v == null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

function stockInTypeText(v: unknown, t: TFunc): string {
  const key = resolveStockInTypeLabelKey(asBizType(v))
  return t(`stockInList.stockInTypeLabels.${key}`)
}

function stockOutTypeText(v: unknown, t: TFunc): string {
  const key = resolveStockOutTypeLabelKey(asBizType(v))
  return t(`stockOutList.stockOutTypeLabels.${key}`)
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

function firstOutboundDoc(aggregates: StockItemFlowAggregates | null | undefined): StockItemFlowDoc | null {
  const notify = sortByCreatedAsc(aggregates?.stockOutNotifies ?? [], (x) => x.createTime)[0]
  if (notify) return notify
  const packing = sortByCreatedAsc(aggregates?.packings ?? [], (x) => x.createTime)[0]
  if (packing) return packing
  return sortByCreatedAsc(aggregates?.stockOuts ?? [], (x) => x.createTime)[0] ?? null
}

function maskDash(mask: boolean, v?: string | null) {
  if (mask) return '—'
  return dash(v)
}

export { formatFlowCardDate as formatStockItemFlowCardDate }

export function buildStockItemFlowStations(
  row: RowRecord | null | undefined,
  aggregates: StockItemFlowAggregates | null | undefined,
  t: TFunc,
  options?: { maskPurchase?: boolean; maskSale?: boolean }
): StockItemFlowStation[] {
  const mask511 = !!options?.maskPurchase
  const mask521 = !!options?.maskSale
  const F = 'inventoryStockItemList.flowPanel'
  const stations: StockItemFlowStation[] = []
  const lineVendorId = resolveFlowPartyId(mask511, row?.vendorId)
  const lineCustomerId = resolveFlowPartyId(mask521, row?.customerId)

  const po = aggregates?.purchaseOrderItem ?? null
  const qc = aggregates?.qc ?? null
  const stockIn = aggregates?.stockIn ?? null
  const stockItemDoc = aggregates?.stockItem ?? null

  {
    const cards: StockItemFlowCard[] = []
    if (po) {
      const orderId = String(po.purchaseOrderId ?? '').trim()
      const itemId = String(po.purchaseOrderItemId ?? po.id ?? '').trim()
      cards.push({
        id: po.id,
        docNo: dash(po.docCode),
        docRoute:
          orderId && !mask511
            ? {
                name: 'PurchaseOrderDetail',
                params: { id: orderId },
                query: itemId ? { purchaseOrderItemId: itemId } : undefined
              }
            : undefined,
        statusText: poItemStatusLabel(po.status),
        isFinal: isPoItemFinal(po.status),
        createdAt: po.createTime,
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: true,
        vendorId: lineVendorId,
        vendorName: maskDash(mask511, po.vendorName),
        showCustomer: false,
        showPerson: true,
        personRoleKey: `${F}.role.purchaser`,
        personName: dash(po.personName),
        unitPriceText: mask511 ? '—' : formatUnitPriceWithCurrencyCodeSuffix(po.unitPrice, Number(po.currency)),
        qtyText: formatQtyPcs(po.qty),
        qtyLabelKey: `${F}.fields.qty`,
        description: null
      })
    }
    stations.push(buildStation('purchaseOrderItem', `${F}.stations.purchaseOrderItem`, cards))
  }

  {
    const cards: StockItemFlowCard[] = []
    if (qc) {
      const noticeId = String(qc.stockInNotifyId ?? '').trim()
      cards.push({
        id: qc.id,
        docNo: dash(qc.docCode),
        docRoute:
          !mask511 && noticeId
            ? { name: 'QcCreate', query: { noticeId, qcId: qc.id } }
            : undefined,
        statusText: qcStatusLabel(qc.status, t),
        isFinal: isQcFinal(qc.status),
        createdAt: qc.createTime,
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: false,
        showCustomer: false,
        showPerson: true,
        personRoleKey: `${F}.role.creator`,
        personName: dash(qc.personName),
        qtyText: `${Number(qc.passQty) || 0} / ${Number(qc.rejectQty) || 0} pcs`,
        qtyLabelKey: `${F}.fields.qcQty`,
        description: null
      })
    }
    stations.push(buildStation('qc', `${F}.stations.qc`, cards))
  }

  {
    const cards: StockItemFlowCard[] = []
    if (stockIn) {
      cards.push({
        id: stockIn.id,
        docNo: dash(stockIn.docCode),
        docRoute: !mask511 ? { name: 'StockInDetail', params: { id: stockIn.id } } : undefined,
        statusText: stockInStatusLabel(stockIn.status, t),
        isFinal: isStockInFinal(stockIn.status),
        createdAt: stockIn.createTime ?? stockIn.bizDate,
        createdAtLabelKey: `${F}.fields.createdAt`,
        showVendor: false,
        showCustomer: false,
        showPerson: true,
        personRoleKey: `${F}.role.creator`,
        personName: dash(stockIn.personName),
        qtyText: formatQtyPcs(stockIn.qty),
        qtyLabelKey: `${F}.fields.inboundQty`,
        stockInType: asBizType(stockIn.stockInType),
        bizTypeText: stockInTypeText(stockIn.stockInType, t),
        customsDeclarationId: linkedCustomsId(stockIn.customsDeclarationId),
        customsDeclarationCode: linkedCustomsId(stockIn.customsDeclarationCode),
        showCustomsIcon: showInboundCustomsIcon(stockIn.stockInType, stockIn.customsDeclarationId),
        description: null
      })
    }
    stations.push(buildStation('stockIn', `${F}.stations.stockIn`, cards))
  }

  {
    const src = stockItemDoc ?? (row ? ({} as StockItemFlowDoc) : null)
    const cards: StockItemFlowCard[] = []
    if (row || src) {
      const itemId = String(src?.id ?? row?.stockItemId ?? '').trim() || 'stock-item'
      const aggregateId = String(src?.stockAggregateId ?? row?.stockAggregateId ?? '').trim()
      const customerId = String(row?.customerId ?? '').trim()
      const sellLine = String(row?.sellOrderItemCode ?? '').trim()
      const hasSalesLink = customerId.length > 0 || sellLine.length > 0
      const customerName = maskDash(mask521, src?.customerName ?? (row?.customerName as string | null))
      const salesperson = maskDash(mask521, src?.personName ?? (row?.salespersonName as string | null))
      const rawSalesPrice = src?.salesUnitPrice ?? row?.salesPrice
      const salesPrice = !hasSalesLink
        ? null
        : mask521
          ? '—'
          : rawSalesPrice != null
            ? formatUnitPriceWithCurrencyCodeSuffix(rawSalesPrice, Number(src?.salesCurrency ?? row?.salesCurrency))
            : null
      const showCustomer = hasSalesLink
      const showSales = hasSalesLink
      const stockInType = src?.stockInType ?? row?.stockInType ?? stockIn?.stockInType
      const inboundCustomsId = linkedCustomsId(src?.customsDeclarationId ?? stockIn?.customsDeclarationId)
      const inboundCustomsCode = linkedCustomsId(src?.customsDeclarationCode ?? stockIn?.customsDeclarationCode)
      const firstOut = firstOutboundDoc(aggregates)
      const stockOutType = asBizType(firstOut?.stockOutType)
      const outboundTypeKnown = stockOutType != null && resolveStockOutTypeLabelKey(stockOutType) !== 'unknown'
      cards.push({
        id: itemId,
        docNo: dash(src?.docCode ?? (row?.stockItemCode as string | null)),
        docRoute: aggregateId
          ? { name: 'InventoryStockDetail', params: { stockId: aggregateId } }
          : undefined,
        statusText: outboundStatusLabel(src?.status ?? row?.outboundStatus, t),
        isFinal: isStockItemFinal(src?.status ?? row?.outboundStatus),
        createdAt: src?.bizDate ?? (row?.stockInDate as string | null),
        createdAtLabelKey: `${F}.fields.stockInDate`,
        showVendor: true,
        vendorId: lineVendorId,
        vendorName: maskDash(mask511, src?.vendorName ?? (row?.vendorName as string | null)),
        showCustomer,
        customerId: lineCustomerId,
        customerName,
        showPerson: false,
        personRoleKey: `${F}.role.salesUser`,
        personName: salesperson,
        unitPriceText: mask511
          ? '—'
          : formatUnitPriceWithCurrencyCodeSuffix(
              src?.unitPrice ?? row?.purchasePrice,
              Number(src?.currency ?? row?.purchaseCurrency)
            ),
        salesPriceText: showSales ? salesPrice : null,
        qtyText: formatQtyPcs(src?.qty ?? row?.qtyInbound),
        qtyLabelKey: `${F}.fields.inboundQty`,
        qty2Text: formatQtyPcs(src?.qty2 ?? row?.qtyStockOut),
        qty2LabelKey: `${F}.fields.outboundQty`,
        stockInType: asBizType(stockInType),
        stockOutType: outboundTypeKnown ? stockOutType : null,
        bizTypeText: stockInTypeText(stockInType, t),
        bizTypeLabelKey: `${F}.fields.stockInType`,
        customsDeclarationId: inboundCustomsId,
        customsDeclarationCode: inboundCustomsCode,
        stockOutCustomsDeclarationId: outboundTypeKnown
          ? linkedCustomsId(firstOut?.customsDeclarationId)
          : null,
        stockOutCustomsDeclarationCode: outboundTypeKnown
          ? linkedCustomsId(firstOut?.customsDeclarationCode)
          : null,
        showCustomsIcon: showInboundCustomsIcon(stockInType, inboundCustomsId),
        description: null
      })
    }
    stations.push(buildStation('stockItem', `${F}.stations.stockItem`, cards))
  }

  {
    const list = sortByCreatedAsc(aggregates?.stockOutNotifies ?? [], (x) => x.createTime)
    const cards: StockItemFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.docCode),
      docRoute: !mask521 ? { name: 'StockOutNotifyDetail', params: { id: x.id } } : undefined,
      statusText: stockOutNotifyStatusLabel(x.status, t),
      isFinal: isStockOutNotifyFinal(x.status),
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
    stations.push(buildStation('stockOutNotify', `${F}.stations.stockOutNotify`, cards))
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
    stations.push(buildStation('packing', `${F}.stations.packing`, cards))
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
    stations.push(buildStation('stockOut', `${F}.stations.stockOut`, cards))
  }

  return stations
}
