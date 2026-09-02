import type { CustomsPendlistFlowAggregatesDto, CustomsPendlistFlowDocDto } from '@/api/customs'
import { CUSTOMS_PENDLIST_STATUS } from '@/api/customs'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import type { FlowCard, FlowStation, FlowStationKey, FlowStationStatus } from '@/utils/sellOrderItemFlowPanel'

type TFunc = (key: string, ...args: unknown[]) => string

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

function isPendlistFinal(status?: number | null) {
  const n = Number(status)
  return n === CUSTOMS_PENDLIST_STATUS.Closed || n === CUSTOMS_PENDLIST_STATUS.Cancelled
}

function priceText(doc?: CustomsPendlistFlowDocDto | null) {
  if (doc?.unitPrice == null) return null
  return formatUnitPriceWithCurrencyCodeSuffix(Number(doc.unitPrice), Number(doc.currency ?? 1))
}

function toCard(
  doc: CustomsPendlistFlowDocDto,
  opts: {
    statusText: string
    isFinal: boolean
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
    isFinal: doc.isDeleted || opts.isFinal,
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
              isFinal: Number(sell.status) === 1,
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
              isFinal:
                !!salesSor.isDeleted ||
                Number(salesSor.status) === STOCK_OUT_REQUEST_STATUS.StockedOut ||
                Number(salesSor.status) === STOCK_OUT_REQUEST_STATUS.Cancelled,
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
                isFinal: isPendlistFinal(pendlist.status),
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
          isFinal:
            !!d.isDeleted ||
            Number(d.status) === STOCK_OUT_REQUEST_STATUS.StockedOut ||
            Number(d.status) === STOCK_OUT_REQUEST_STATUS.Cancelled,
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
          isFinal: Number(d.status) >= 100 || Number(d.status) === -1,
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
          statusText: d.status == null ? '—' : String(d.status),
          isFinal: Number(d.status) === 100 || Number(d.status) === -1,
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator'
        })
      )
    ),
    buildStation(
      'stockOut',
      'customsPages.pendlists.flowStations.stockOut',
      (aggregates.stockOuts ?? []).map((d) =>
        toCard(d, {
          statusText: d.status == null ? '—' : String(d.status),
          isFinal: Number(d.status) === 100 || Number(d.status) === -1,
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
          statusText: d.status == null ? '—' : String(d.status),
          isFinal: Number(d.status) === 30 || Number(d.status) === -1,
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
          statusText: d.status == null ? '—' : String(d.status),
          isFinal: Number(d.status) >= 100,
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
          statusText: d.status == null ? '—' : String(d.status),
          isFinal: Number(d.status) === 100 || Number(d.status) === -1,
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
          statusText: d.status == null ? '—' : String(d.status),
          isFinal: Number(d.status) === 100,
          personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
          docRoute: d.id ? { name: 'StockInDetail', params: { id: d.id } } : undefined
        })
      )
    )
  ]

  return stations
}
