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

type DocOutcome = 'active' | 'done' | 'cancelled' | 'failed'

/**
 * 已删除不参与完成判断；全部删除为已取消。
 * 还有进行中则进行中；全部已取消为已取消；完成与已取消混在一起为已完成。
 * 质检：未通过优先于已通过。
 */
function stationStatusFromDocs(
  docs: CustomsDeclarationFlowDocDto[] | null | undefined,
  classify: (doc: CustomsDeclarationFlowDocDto) => DocOutcome,
  failedBeatsDone = false
): FlowStationStatus {
  const rows = docs ?? []
  if (rows.length === 0) return 'empty'
  const live = rows.filter((d) => !d.isDeleted)
  if (live.length === 0) return 'cancelled'
  const outcomes = live.map(classify)
  if (outcomes.some((o) => o === 'active')) return 'active'
  if (failedBeatsDone && outcomes.some((o) => o === 'failed')) return 'failed'
  if (outcomes.every((o) => o === 'cancelled')) return 'cancelled'
  if (outcomes.some((o) => o === 'failed')) return 'failed'
  return 'done'
}

function sellLineOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  if (Number(doc.status) === 1) return 'cancelled'
  if (Number(doc.receiptProgressStatus) === 2 && Number(doc.invoiceProgressStatus) === 2) return 'done'
  return 'active'
}

function notifyOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return 'done'
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return 'cancelled'
  return 'active'
}

function pendlistOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s === CUSTOMS_PENDLIST_STATUS.Closed) return 'done'
  if (s === CUSTOMS_PENDLIST_STATUS.Cancelled) return 'cancelled'
  return 'active'
}

function packingOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s >= 100) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

function declarationOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s === 3) return 'done'
  if (s === -1) return 'cancelled'
  return 'active'
}

function stockOutOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s === 4) return 'done'
  if (s === 3) return 'cancelled'
  return 'active'
}

function arrivalOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  return Number(doc.status) >= 100 ? 'done' : 'active'
}

function qcOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s === 100) return 'done'
  if (s === -1) return 'failed'
  return 'active'
}

function stockInOutcome(doc: CustomsDeclarationFlowDocDto): DocOutcome {
  const s = Number(doc.status)
  if (s === 2) return 'done'
  if (s === 3) return 'cancelled'
  return 'active'
}

function stockOutStatusLabel(status: number | null | undefined, t: TFunc): string {
  if (status == null) return '—'
  const s = Number(status)
  if (s === 0) return t('stockOutList.status.draft')
  if (s === 1) return t('stockOutList.status.pending')
  if (s === 2) return t('stockOutList.status.done')
  if (s === 3) return t('stockOutList.status.cancelled')
  if (s === 4) return t('stockOutList.status.finished')
  return Number.isFinite(s) ? String(s) : '—'
}

function arrivalStatusLabel(status: number | null | undefined, t: TFunc): string {
  if (status == null) return '—'
  const keyMap: Record<number, string> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[Number(status)]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
}

function qcStatusLabel(status: number | null | undefined, t: TFunc): string {
  if (status == null) return '—'
  const s = Number(status)
  if (s === -1) return t('qcList.qcStatus.failed')
  if (s === 10) return t('qcList.qcStatus.partial')
  if (s === 100) return t('qcList.qcStatus.passed')
  return t('qcList.qcStatus.unknown')
}

function stockInStatusLabel(status: number | null | undefined, t: TFunc): string {
  if (status == null) return '—'
  const s = Number(status)
  if (s === 0) return t('stockInList.status.draft')
  if (s === 1) return t('stockInList.status.pending')
  if (s === 2) return t('stockInList.status.done')
  if (s === 3) return t('stockInList.status.cancelled')
  return Number.isFinite(s) ? String(s) : '—'
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
      statusText: stockOutStatusLabel(d.status, t),
      isFinal: Number(d.status) === 4,
      personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.customsOutQty`,
      docRoute: d.id ? { name: 'StockOutDetail', params: { id: d.id } } : undefined
    })
  )

  const arrivalCards = mapDocs(aggregates.arrivals, (d) =>
    toCard(d, {
      statusText: arrivalStatusLabel(d.status, t),
      isFinal: Number(d.status) >= 100,
      personRoleKey: 'salesOrderItemList.flowPanel.role.purchaser',
      showCustomer: true,
      qtyLabelKey: `${F}.fields.arrivalQty`,
      docRoute: d.id ? { name: 'ArrivalNoticeList', query: { highlightId: d.id } } : undefined
    })
  )

  const qcCards = mapDocs(aggregates.qcs, (d) =>
    toCard(d, {
      statusText: qcStatusLabel(d.status, t),
      isFinal: Number(d.status) === 100 || Number(d.status) === -1,
      personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
      qtyLabelKey: `${F}.fields.qcQty`
    })
  )

  const stockInCards = mapDocs(aggregates.stockIns, (d) =>
    toCard(d, {
      statusText: stockInStatusLabel(d.status, t),
      isFinal: Number(d.status) === 100 || Number(d.status) === 2,
      personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
      qtyLabelKey: `${F}.fields.customsInQty`,
      docRoute: d.id ? { name: 'StockInDetail', params: { id: d.id } } : undefined
    })
  )

  const place = (
    key: FlowStationKey,
    titleKey: string,
    cards: FlowCard[],
    docs: CustomsDeclarationFlowDocDto[] | null | undefined,
    classify: (doc: CustomsDeclarationFlowDocDto) => DocOutcome,
    failedBeatsDone = false
  ): FlowStation => ({
    key,
    titleKey,
    stationStatus: stationStatusFromDocs(docs, classify, failedBeatsDone),
    cards
  })

  return [
    place('sellOrderItem', `${ST}.sellOrderItem`, sellCards, aggregates.sellOrderItems, sellLineOutcome),
    place('stockOutNotify', `${ST}.salesStockOutNotify`, salesNotifyCards, aggregates.salesStockOutNotifies, notifyOutcome),
    place('pendlist', `${ST}.pendlist`, pendlistCards, aggregates.pendlists, pendlistOutcome),
    place(
      'customsStockOutNotify',
      `${ST}.customsStockOutNotify`,
      customsNotifyCards,
      aggregates.customsStockOutNotifies,
      notifyOutcome
    ),
    place('packing', `${ST}.packing`, packingCards, aggregates.packing ? [aggregates.packing] : [], packingOutcome),
    place(
      'customsDeclaration',
      `${ST}.declaration`,
      declarationCards,
      aggregates.declaration ? [aggregates.declaration] : [],
      declarationOutcome
    ),
    place('stockOut', `${ST}.stockOut`, stockOutCards, aggregates.stockOuts, stockOutOutcome),
    place('arrivalNotify', `${ST}.arrival`, arrivalCards, aggregates.arrivals, arrivalOutcome),
    place('qc', `${ST}.qc`, qcCards, aggregates.qcs, qcOutcome, true),
    place('customsStockIn', `${ST}.stockIn`, stockInCards, aggregates.stockIns, stockInOutcome)
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
