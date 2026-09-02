import type { CustomsDeclarationFlowAggregatesDto, CustomsDeclarationFlowDocDto } from '@/api/customs'
import { CUSTOMS_PENDLIST_STATUS } from '@/api/customs'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import type { FlowCard, FlowStation, FlowStationKey, FlowStationStatus } from '@/utils/sellOrderItemFlowPanel'

type TFunc = (key: string, ...args: unknown[]) => string

const F = 'customsPages.declarations.flowPanel'
const ST = 'customsPages.declarations.flowStations'

function dash(v?: string | null) {
  const s = String(v ?? '').trim()
  return s || '—'
}

function stationStatusFromCards(cards: FlowCard[]): FlowStationStatus {
  if (cards.length === 0) return 'empty'
  if (cards.every((c) => c.isFinal)) return 'done'
  return 'active'
}

function buildStation(key: FlowStationKey, titleKey: string, cards: FlowCard[]): FlowStation {
  return { key, titleKey, stationStatus: stationStatusFromCards(cards), cards }
}

function salesSorStatusText(status?: number | null, t?: TFunc): string {
  const n = Number(status)
  if (n === STOCK_OUT_REQUEST_STATUS.PendingCustoms)
    return t?.('customsPages.pendlists.flowStatus.pendingCustoms') ?? '待报关'
  if (n === STOCK_OUT_REQUEST_STATUS.PendingPacking)
    return t?.('customsPages.pendlists.flowStatus.pendingPacking') ?? '待装箱'
  if (n === STOCK_OUT_REQUEST_STATUS.Packed) return t?.('customsPages.pendlists.flowStatus.packed') ?? '已装箱'
  if (n === STOCK_OUT_REQUEST_STATUS.StockedOut)
    return t?.('customsPages.pendlists.flowStatus.stockedOut') ?? '已出库'
  if (n === STOCK_OUT_REQUEST_STATUS.Cancelled) return t?.('customsPages.pendlists.flowStatus.cancelled') ?? '已取消'
  return status == null ? '—' : String(status)
}

function pendlistStatusText(status?: number | null, t?: TFunc): string {
  const n = Number(status)
  if (n === CUSTOMS_PENDLIST_STATUS.Open) return t?.('customsPages.pendlists.statusOpen') ?? '待处理'
  if (n === CUSTOMS_PENDLIST_STATUS.CustomsOutNotifyCreated)
    return t?.('customsPages.pendlists.statusCustomsOutCreated') ?? '已生成报关出库通知'
  if (n === CUSTOMS_PENDLIST_STATUS.InCustomsProcess)
    return t?.('customsPages.pendlists.statusInProcess') ?? '报关流程中'
  if (n === CUSTOMS_PENDLIST_STATUS.Closed) return t?.('customsPages.pendlists.statusClosed') ?? '已关闭'
  if (n === CUSTOMS_PENDLIST_STATUS.Cancelled) return t?.('customsPages.pendlists.statusCancelled') ?? '已取消'
  return status == null ? '—' : String(status)
}

function declarationStatusText(status?: number | null, t?: TFunc): string {
  const n = Number(status)
  if (n === -1) return t?.('customsPages.declarations.internalVoid') ?? '作废'
  if (n === 1) return t?.('customsPages.declarations.internalPending') ?? '待处理'
  if (n === 2) return t?.('customsPages.declarations.internalProcessing') ?? '报关中'
  if (n === 3) return t?.('customsPages.declarations.internalDone') ?? '已完成'
  return status == null ? '—' : String(status)
}

function isPendlistFinal(status?: number | null) {
  const n = Number(status)
  return n === CUSTOMS_PENDLIST_STATUS.Closed || n === CUSTOMS_PENDLIST_STATUS.Cancelled
}

function isSalesNotifyFinal(doc: CustomsDeclarationFlowDocDto) {
  return (
    !!doc.isDeleted ||
    Number(doc.status) === STOCK_OUT_REQUEST_STATUS.StockedOut ||
    Number(doc.status) === STOCK_OUT_REQUEST_STATUS.Cancelled
  )
}

function isStockOutFinal(status?: number | null) {
  const s = Number(status)
  return s === 2 || s === 3 || s === 4 || s === 100 || s === -1
}

function priceText(doc?: CustomsDeclarationFlowDocDto | null) {
  if (doc?.unitPrice == null) return null
  return formatUnitPriceWithCurrencyCodeSuffix(Number(doc.unitPrice), Number(doc.currency ?? 1))
}

function toCard(
  doc: CustomsDeclarationFlowDocDto,
  opts: {
    statusText: string
    isFinal: boolean
    personRoleKey: string
    docRoute?: FlowCard['docRoute']
    showCustomer?: boolean
    qtyLabelKey?: string
    includeBroker?: boolean
    includePrice?: boolean
  }
): FlowCard {
  return {
    id: doc.id,
    docNo: doc.isDeleted
      ? dash(doc.docCode) === '—'
        ? '已删除'
        : `${dash(doc.docCode)}（已删除）`
      : dash(doc.docCode),
    docRoute: doc.isDeleted ? undefined : opts.docRoute,
    statusText: doc.isDeleted ? '已删除' : opts.statusText,
    isFinal: doc.isDeleted || opts.isFinal,
    createdAt: doc.createTime ?? null,
    showCustomer: opts.showCustomer === true,
    customerName: doc.customerName,
    customerCode: doc.customerCode,
    customerId: doc.customerId ?? null,
    personRoleKey: opts.personRoleKey,
    personName: doc.personName,
    unitPriceText: opts.includePrice ? priceText(doc) : null,
    qtyText: opts.qtyLabelKey && doc.qty != null ? String(doc.qty) : null,
    qtyLabelKey: opts.qtyLabelKey,
    description: null,
    stockOutType: doc.stockOutType ?? null,
    stockInType: doc.stockInType ?? null,
    customsDeclarationId: doc.customsDeclarationId ?? null,
    customsDeclarationCode: doc.customsDeclarationCode ?? null,
    brokerName: opts.includeBroker ? doc.brokerName ?? null : null
  }
}

export function buildCustomsDeclarationFlowStations(
  aggregates: CustomsDeclarationFlowAggregatesDto | null | undefined,
  t: TFunc,
  options?: { maskSensitive?: boolean }
): FlowStation[] {
  if (!aggregates) return []
  const mask = !!options?.maskSensitive

  const mapDocs = (
    docs: CustomsDeclarationFlowDocDto[] | null | undefined,
    map: (d: CustomsDeclarationFlowDocDto) => FlowCard
  ) => (docs ?? []).map((d) => redact(map(d), mask))

  const sellCards = mapDocs(aggregates.sellOrderItems, (d) =>
    toCard(d, {
      statusText: Number(d.status) === 1 ? '已取消' : '正常',
      isFinal: Number(d.status) === 1,
      personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.salesOutQty`,
      includePrice: true,
      docRoute:
        d.salesOrderId && d.id
          ? {
              name: 'SalesOrderDetail',
              params: { id: String(d.salesOrderId) },
              query: { highlightItemId: String(d.id) }
            }
          : undefined
    })
  )

  const salesNotifyCards = mapDocs(aggregates.salesStockOutNotifies, (d) =>
    toCard(d, {
      statusText: salesSorStatusText(d.status, t),
      isFinal: isSalesNotifyFinal(d),
      personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.salesOutQty`,
      docRoute: !d.isDeleted && d.id ? { name: 'StockOutNotifyList', query: { highlightId: d.id } } : undefined
    })
  )

  const pendlistCards = mapDocs(aggregates.pendlists, (d) =>
    toCard(
      { ...d, docCode: d.docCode || '待报关记录' },
      {
        statusText: pendlistStatusText(d.status, t),
        isFinal: isPendlistFinal(d.status),
        personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
        showCustomer: true,
        qtyLabelKey: `${F}.fields.salesOutQty`
      }
    )
  )

  const customsNotifyCards = mapDocs(aggregates.customsStockOutNotifies, (d) =>
    toCard(d, {
      statusText: salesSorStatusText(d.status, t),
      isFinal: isSalesNotifyFinal(d),
      personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.customsOutQty`,
      docRoute: !d.isDeleted && d.id ? { name: 'StockOutNotifyList', query: { highlightId: d.id } } : undefined
    })
  )

  const packingCards = aggregates.packing
    ? [
        redact(
          toCard(aggregates.packing, {
            statusText: packingStatusLabel(Number(aggregates.packing.status ?? 0)),
            isFinal: Number(aggregates.packing.status) >= 100 || Number(aggregates.packing.status) === -1,
            personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
            showCustomer: true,
            qtyLabelKey: `${F}.fields.customsOutQty`,
            docRoute: aggregates.packing.id ? { name: 'PackingDetail', params: { id: aggregates.packing.id } } : undefined
          }),
          mask
        )
      ]
    : []

  const declaration = aggregates.declaration
  const declarationCards = declaration
    ? [
        redact(
          toCard(declaration, {
            statusText: declarationStatusText(declaration.status, t),
            isFinal: Number(declaration.status) === 3 || Number(declaration.status) === -1,
            personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
            includeBroker: true,
            docRoute: declaration.id ? { name: 'CustomsDeclarationDetail', params: { id: declaration.id } } : undefined
          }),
          mask
        )
      ]
    : []

  const stockOutCards = mapDocs(aggregates.stockOuts, (d) =>
    toCard(d, {
      statusText: d.status == null ? '—' : String(d.status),
      isFinal: isStockOutFinal(d.status),
      personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.customsOutQty`,
      docRoute: d.id ? { name: 'StockOutDetail', params: { id: d.id } } : undefined
    })
  )

  const arrivalCards = mapDocs(aggregates.arrivals, (d) =>
    toCard(d, {
      statusText: d.status == null ? '—' : String(d.status),
      isFinal: Number(d.status) >= 100,
      personRoleKey: 'salesOrderItemList.flowPanel.role.purchaser',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.arrivalQty`,
      docRoute: d.id ? { name: 'ArrivalNoticeList', query: { highlightId: d.id } } : undefined
    })
  )

  const qcCards = mapDocs(aggregates.qcs, (d) =>
    toCard(d, {
      statusText: d.status == null ? '—' : String(d.status),
      isFinal: Number(d.status) === 100 || Number(d.status) === -1,
      personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
      qtyLabelKey: `${F}.fields.qcQty`
    })
  )

  const stockInCards = mapDocs(aggregates.stockIns, (d) =>
    toCard(d, {
      statusText: d.status == null ? '—' : String(d.status),
      isFinal: Number(d.status) === 100 || Number(d.status) === 2,
      personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
      qtyLabelKey: `${F}.fields.customsInQty`,
      docRoute: d.id ? { name: 'StockInDetail', params: { id: d.id } } : undefined
    })
  )

  return [
    buildStation('sellOrderItem', `${ST}.sellOrderItem`, sellCards),
    buildStation('stockOutNotify', `${ST}.salesStockOutNotify`, salesNotifyCards),
    buildStation('pendlist', `${ST}.pendlist`, pendlistCards),
    buildStation('customsStockOutNotify', `${ST}.customsStockOutNotify`, customsNotifyCards),
    buildStation('packing', `${ST}.packing`, packingCards),
    buildStation('customsDeclaration', `${ST}.declaration`, declarationCards),
    buildStation('stockOut', `${ST}.stockOut`, stockOutCards),
    buildStation('arrivalNotify', `${ST}.arrival`, arrivalCards),
    buildStation('qc', `${ST}.qc`, qcCards),
    buildStation('customsStockIn', `${ST}.stockIn`, stockInCards)
  ]
}

function redact(card: FlowCard, mask: boolean): FlowCard {
  if (!mask) return card
  return {
    ...card,
    customerId: null,
    customerName: '—',
    customerCode: '—',
    unitPriceText: card.unitPriceText ? '—' : card.unitPriceText
  }
}
