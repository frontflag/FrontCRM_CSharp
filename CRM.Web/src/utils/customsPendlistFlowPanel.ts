import type { CustomsPendlistFlowAggregatesDto, CustomsPendlistFlowDocDto } from '@/api/customs'
import { CUSTOMS_PENDLIST_STATUS } from '@/api/customs'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import type { FlowCard, FlowStation, FlowStationKey, FlowStationStatus } from '@/utils/sellOrderItemFlowPanel'
import {
  arrivalOutcome,
  declarationOutcome,
  foldFlowCards,
  packingOutcome,
  pendlistOutcome,
  pickingOutcome,
  qcOutcome,
  sellLineOutcome,
  stockInOutcome,
  stockOutNotifyOutcome,
  stockOutOutcome,
  type FlowDocOutcome
} from '@/utils/flowStationBadge'

type TFunc = (key: string, ...args: unknown[]) => string

function dash(v?: string | null) {
  const s = String(v ?? '').trim()
  return s || '—'
}

function stationStatusFromCards(cards: FlowCard[]): FlowStationStatus {
  return foldFlowCards(cards)
}

function buildStation(key: FlowStationKey, titleKey: string, cards: FlowCard[]): FlowStation {
  return { key, titleKey, stationStatus: stationStatusFromCards(cards), cards }
}

function salesSorStatusText(status?: number | null, t?: TFunc): string {
  const n = Number(status)
  if (n === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t?.('customsPages.pendlists.flowStatus.pendingCustoms') ?? '待报关'
  if (n === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t?.('customsPages.pendlists.flowStatus.pendingPacking') ?? '待装箱'
  if (n === STOCK_OUT_REQUEST_STATUS.Packed) return t?.('customsPages.pendlists.flowStatus.packed') ?? '已装箱'
  if (n === STOCK_OUT_REQUEST_STATUS.StockedOut) return t?.('customsPages.pendlists.flowStatus.stockedOut') ?? '已出库'
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

function pickingStatusLabel(status: number | null | undefined, t: TFunc): string {
  if (status == null) return '—'
  const s = Number(status)
  if (s === 1) return t('pickingSlip.status.pending')
  if (s === 2) return t('pickingSlip.status.inProgress')
  if (s === 100) return t('pickingSlip.status.done')
  if (s === -1) return t('pickingSlip.status.cancelled')
  return t('pickingSlip.status.unknown')
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

function declarationStatusLabel(status: number | null | undefined, t: TFunc): string {
  if (status == null) return '—'
  const s = Number(status)
  if (s === -1) return t('customsPages.declarations.internalVoid')
  if (s === 1) return t('customsPages.declarations.internalPending')
  if (s === 2) return t('customsPages.declarations.internalProcessing')
  if (s === 3) return t('customsPages.declarations.internalDone')
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

function priceText(doc?: CustomsPendlistFlowDocDto | null) {
  if (doc?.unitPrice == null) return null
  return formatUnitPriceWithCurrencyCodeSuffix(Number(doc.unitPrice), Number(doc.currency ?? 1))
}

function toCard(
  doc: CustomsPendlistFlowDocDto,
  opts: {
    statusText: string
    outcome: FlowDocOutcome
    personRoleKey: string
    docRoute?: FlowCard['docRoute']
    showCustomer?: boolean
    includePendlistId?: boolean
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
    isFinal: !doc.isDeleted && opts.outcome === 'done',
    outcome: opts.outcome,
    isDeleted: !!doc.isDeleted,
    createdAt: doc.createTime ?? null,
    showCustomer: opts.showCustomer !== false,
    customerName: doc.customerName,
    customerCode: doc.customerCode,
    customerId: doc.customerId ?? null,
    personRoleKey: opts.personRoleKey,
    personName: doc.personName,
    unitPriceText: priceText(doc),
    qtyText: doc.qty == null ? null : String(doc.qty),
    description: null,
    pendlistId: opts.includePendlistId ? doc.pendlistId ?? null : null
  }
}

export function buildCustomsPendlistFlowStations(
  aggregates: CustomsPendlistFlowAggregatesDto | null | undefined,
  t: TFunc
): FlowStation[] {
  if (!aggregates) return []

  const sell = aggregates.sellOrderItem
  const salesSor = aggregates.salesStockOutNotify
  const pendlist = aggregates.pendlist

  const stations: FlowStation[] = [
    buildStation(
      'sellOrderItem',
      'customsPages.pendlists.flowStations.sellOrderItem',
      sell
        ? [
            toCard(sell, {
              statusText: Number(sell.status) === 1 ? '已取消' : '正常',
              outcome: sellLineOutcome(sell.status, sell.receiptProgressStatus, sell.invoiceProgressStatus),
              personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
              docRoute:
                sell.salesOrderId && sell.id
                  ? {
                      name: 'SalesOrderDetail',
                      params: { id: String(sell.salesOrderId) },
                      query: { highlightItemId: String(sell.id) }
                    }
                  : undefined
            })
          ]
        : []
    ),
    buildStation(
      'stockOutNotify',
      'customsPages.pendlists.flowStations.salesStockOutNotify',
      salesSor
        ? [
            toCard(salesSor, {
              statusText: salesSorStatusText(salesSor.status, t),
              outcome: stockOutNotifyOutcome(salesSor.status),
              personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
              docRoute:
                !salesSor.isDeleted && salesSor.id
                  ? { name: 'StockOutNotifyList', query: { highlightId: salesSor.id } }
                  : undefined
            })
          ]
        : []
    ),
    buildStation(
      'pendlist',
      'customsPages.pendlists.flowStations.pendlist',
      pendlist
        ? [
            toCard(
              { ...pendlist, docCode: pendlist.docCode || '待报关记录' },
              {
                statusText: pendlistStatusText(pendlist.status, t),
                outcome: pendlistOutcome(pendlist.status),
                personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
                showCustomer: true
              }
            )
          ]
        : []
    ),
    buildStation(
      'customsStockOutNotify',
      'customsPages.pendlists.flowStations.customsStockOutNotify',
      (aggregates.customsStockOutNotifies ?? []).map((d) =>
        toCard(d, {
          statusText: salesSorStatusText(d.status, t),
          outcome: stockOutNotifyOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
          includePendlistId: true,
          docRoute: !d.isDeleted && d.id ? { name: 'StockOutNotifyList', query: { highlightId: d.id } } : undefined
        })
      )
    ),
    buildStation(
      'packing',
      'customsPages.pendlists.flowStations.packing',
      (aggregates.packings ?? []).map((d) =>
        toCard(d, {
          statusText: packingStatusLabel(Number(d.status ?? 0)),
          outcome: packingOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
          docRoute: d.id ? { name: 'PackingDetail', params: { id: d.id } } : undefined
        })
      )
    ),
    buildStation(
      'picking',
      'customsPages.pendlists.flowStations.picking',
      (aggregates.pickings ?? []).map((d) =>
        toCard(d, {
          statusText: pickingStatusLabel(d.status, t),
          outcome: pickingOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator'
        })
      )
    ),
    buildStation(
      'stockOut',
      'customsPages.pendlists.flowStations.stockOut',
      (aggregates.stockOuts ?? []).map((d) =>
        toCard(d, {
          statusText: stockOutStatusLabel(d.status, t),
          outcome: stockOutOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
          docRoute: d.id ? { name: 'StockOutDetail', params: { id: d.id } } : undefined
        })
      )
    ),
    buildStation(
      'customsDeclaration',
      'customsPages.pendlists.flowStations.declaration',
      (aggregates.declarations ?? []).map((d) =>
        toCard(d, {
          statusText: declarationStatusLabel(d.status, t),
          outcome: declarationOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
          docRoute: d.id ? { name: 'CustomsDeclarationDetail', params: { id: d.id } } : undefined
        })
      )
    ),
    buildStation(
      'arrivalNotify',
      'customsPages.pendlists.flowStations.arrival',
      (aggregates.arrivals ?? []).map((d) =>
        toCard(d, {
          statusText: arrivalStatusLabel(d.status, t),
          outcome: arrivalOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.purchaser',
          docRoute: d.id ? { name: 'ArrivalNoticeList', query: { highlightId: d.id } } : undefined
        })
      )
    ),
    buildStation(
      'qc',
      'customsPages.pendlists.flowStations.qc',
      (aggregates.qcs ?? []).map((d) =>
        toCard(d, {
          statusText: qcStatusLabel(d.status, t),
          outcome: qcOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
          docRoute: d.id ? { name: 'QcList', query: { highlightId: d.id } } : undefined
        })
      )
    ),
    buildStation(
      'customsStockIn',
      'customsPages.pendlists.flowStations.stockIn',
      (aggregates.stockIns ?? []).map((d) =>
        toCard(d, {
          statusText: stockInStatusLabel(d.status, t),
          outcome: stockInOutcome(d.status),
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
          docRoute: d.id ? { name: 'StockInDetail', params: { id: d.id } } : undefined
        })
      )
    )
  ]

  return stations
}
