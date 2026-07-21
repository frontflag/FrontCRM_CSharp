import type { SalesOrderDetailTabAggregates } from '@/api/salesOrder'
import type { PackingStockOutNotifyRow } from '@/api/packing'
import { packingStatusLabel } from '@/api/packing'
import type { PickPagePackingLine, PickingTask } from '@/api/inventoryCenter'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { translateSalesOrderStatus } from '@/constants/salesOrderStatus'
import {
  formatUnitPriceWithCurrencyCodeSuffix
} from '@/utils/moneyFormat'
import {
  formatFlowCardDate,
  type FlowCard,
  type FlowStation,
  type FlowStationKey,
  type FlowStationStatus
} from '@/utils/sellOrderItemFlowPanel'

export type PackingFlowStationKey =
  | 'sellOrderItem'
  | 'stockOutNotify'
  | 'packing'
  | 'picking'
  | 'stockOut'

export type PackingFlowExtras = {
  stockOutNotifies?: PackingStockOutNotifyRow[] | null
  pickingTask?: PickingTask | null
  pickLine?: PickPagePackingLine | null
}

type TFunc = (key: string, ...args: unknown[]) => string
type RowRecord = Record<string, unknown>

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

function stationStatusFromCards(cards: FlowCard[]): FlowStationStatus {
  if (cards.length === 0) return 'empty'
  if (cards.every((c) => c.isFinal)) return 'done'
  return 'active'
}

function buildStation(key: PackingFlowStationKey, titleKey: string, cards: FlowCard[]): FlowStation {
  return {
    key: key as FlowStationKey,
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

function isPackingFinal(v: unknown) {
  return Number(v) === 100
}

function isSalesOrderFinal(v: unknown) {
  const s = Number(v)
  return s < 0 || s === 100 || s === 110 || s === 120
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

function pickingStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === 1) return t('pickingSlip.status.pending')
  if (s === 2) return t('pickingSlip.status.inProgress')
  if (s === 100) return t('pickingSlip.status.done')
  if (s === -1) return t('pickingSlip.status.cancelled')
  return t('pickingSlip.status.unknown')
}

function isPickingFinal(v: unknown) {
  const s = Number(v)
  return s === 100 || s === -1
}

function packingCodeMatched(packingCodes: string | null | undefined, packingCode: string): boolean {
  const code = packingCode.trim()
  if (!code) return false
  const raw = String(packingCodes ?? '')
  if (!raw.trim()) return false
  return raw
    .split(/[,，;；]/)
    .map((x) => x.trim())
    .filter(Boolean)
    .some((x) => x.toLowerCase() === code.toLowerCase())
}

/**
 * 装箱明细流程（定稿节点）：
 * 销售订单明细 → 出库通知 → 装箱 → 拣货 → 出库
 */
export function buildPackingItemFlowStations(
  row: RowRecord | null | undefined,
  aggregates: SalesOrderDetailTabAggregates | null | undefined,
  t: TFunc,
  options?: { maskSensitive?: boolean; extras?: PackingFlowExtras | null }
): FlowStation[] {
  const mask = !!options?.maskSensitive
  const extras = options?.extras ?? null
  const stations: FlowStation[] = []
  const packingId = String(row?.packingId ?? '').trim()
  const packingCode = String(row?.packingCode ?? '').trim()
  const packingItemId = String(row?.packingItemId ?? '').trim()
  const notifyId = String(row?.stockOutNotifyId ?? '').trim()
  const sellItemId = String(row?.sellOrderItemId ?? row?.id ?? '').trim()

  // 1. 销售订单明细
  {
    const cards: FlowCard[] = []
    if (row && sellItemId) {
      const orderId = String(row.sellOrderId ?? '').trim()
      const code = String(row.sellOrderItemCode ?? '').trim() || '—'
      const status = Number(row.orderStatus)
      cards.push({
        id: sellItemId,
        docNo: code,
        docRoute:
          orderId && !mask
            ? { name: 'SalesOrderDetail', params: { id: orderId } }
            : undefined,
        statusText: Number.isFinite(status) ? translateSalesOrderStatus(status, t) : '—',
        isFinal: isSalesOrderFinal(status),
        createdAt: (row.orderCreateTime ?? row.createTime ?? null) as string | null,
        showCustomer: true,
        customerName: mask ? '—' : (row.customerName as string | null),
        customerCode: mask ? '—' : (row.customerCode as string | null),
        personRoleKey: 'salesOrderItemList.flowPanel.role.salesUser',
        personName: mask ? '—' : dash(row.salesUserName as string | null),
        unitPriceText: mask
          ? '—'
          : formatUnitPriceWithCurrencyCodeSuffix(row.price, Number(row.currency)),
        qtyText: formatQtyPcs(row.packingItemQty ?? row.qty),
        description: null
      })
    }
    stations.push(
      buildStation('sellOrderItem', 'packingDetail.flowPanel.stations.sellOrderItem', cards)
    )
  }

  // 2. 出库通知（优先本箱明细关联）
  {
    const fromDetail = (extras?.stockOutNotifies ?? []).filter((n) => {
      if (notifyId) return String(n.id || '').trim() === notifyId
      if (sellItemId) return String(n.salesOrderItemId || '').trim() === sellItemId
      return false
    })
    const fromAgg = sortByCreatedAsc(aggregates?.stockOutRequests ?? [], (x) => x.createTime ?? x.requestDate).filter(
      (x) => {
        if (notifyId) return String(x.id || '').trim() === notifyId
        if (sellItemId) return String(x.salesOrderItemId || '').trim() === sellItemId
        return true
      }
    )
    const cards: FlowCard[] =
      fromDetail.length > 0
        ? fromDetail.map((x) => ({
            id: x.id,
            docNo: dash(x.requestCode),
            docRoute: !mask ? { name: 'StockOutNotifyDetail', params: { id: x.id } } : undefined,
            statusText: stockOutNotifyStatusLabel(x.status, t),
            isFinal: isStockOutNotifyFinal(x.status),
            createdAt: x.createTime ?? x.requestDate,
            showCustomer: true,
            customerName: mask ? '—' : x.customerName,
            customerCode: mask ? '—' : null,
            personRoleKey: 'salesOrderItemList.flowPanel.role.requester',
            personName: mask ? '—' : dash(x.salesUserName),
            qtyText: formatQtyPcs(x.outQuantity),
            description: null
          }))
        : fromAgg.map((x) => ({
            id: x.id,
            docNo: dash(x.requestCode),
            docRoute: !mask ? { name: 'StockOutNotifyDetail', params: { id: x.id } } : undefined,
            statusText: stockOutNotifyStatusLabel(x.status, t),
            isFinal: isStockOutNotifyFinal(x.status),
            createdAt: x.createTime ?? x.requestDate,
            showCustomer: true,
            customerName: mask ? '—' : x.customerName,
            customerCode: mask ? '—' : null,
            personRoleKey: 'salesOrderItemList.flowPanel.role.requester',
            personName: mask ? '—' : dash(x.requestUserName),
            qtyText: formatQtyPcs(x.outQuantity),
            description: null
          }))
    stations.push(
      buildStation('stockOutNotify', 'packingDetail.flowPanel.stations.stockOutNotify', cards)
    )
  }

  // 3. 装箱（主节点：当前装箱）
  {
    const fromAgg = (aggregates?.packings ?? []).filter(
      (x) => packingId && String(x.id || '').trim().toLowerCase() === packingId.toLowerCase()
    )
    const cards: FlowCard[] =
      fromAgg.length > 0
        ? fromAgg.map((x) => ({
            id: x.id,
            docNo: dash(x.code),
            docRoute: !mask ? { name: 'PackingDetail', params: { id: x.id } } : undefined,
            statusText: packingStatusLabel(Number(x.status)),
            isFinal: isPackingFinal(x.status),
            createdAt: x.createTime,
            showCustomer: true,
            customerName: mask ? '—' : x.customerName,
            customerCode: mask ? '—' : null,
            personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
            personName: mask ? '—' : dash(x.createUserName),
            qtyText: formatQtyPcs(row?.packingItemQty ?? x.itemRows),
            description: null
          }))
        : packingId
          ? [
              {
                id: packingId,
                docNo: dash(packingCode),
                docRoute: !mask ? { name: 'PackingDetail', params: { id: packingId } } : undefined,
                statusText: packingStatusLabel(Number(row?.packingStatus)),
                isFinal: isPackingFinal(row?.packingStatus),
                createdAt: (row?.createTime ?? null) as string | null,
                showCustomer: true,
                customerName: mask ? '—' : (row?.customerName as string | null),
                customerCode: mask ? '—' : null,
                personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
                personName: mask ? '—' : dash(row?.createUserName as string | null),
                qtyText: formatQtyPcs(row?.packingItemQty ?? row?.qty),
                description: null
              }
            ]
          : []
    stations.push(buildStation('packing', 'packingDetail.flowPanel.stations.packing', cards))
  }

  // 4. 拣货
  {
    const task = extras?.pickingTask ?? null
    const line = extras?.pickLine ?? null
    const cards: FlowCard[] = []
    if (task && String(task.id || '').trim()) {
      const qty =
        packingItemId && line
          ? line.pickedQtyTotal ?? line.planQtyTotal
          : task.pickedQtyTotal ?? task.planQtyTotal
      cards.push({
        id: task.id,
        docNo: dash(task.taskCode),
        docRoute: !mask ? { name: 'PickingSlipDetail', params: { id: task.id } } : undefined,
        statusText: pickingStatusLabel(task.status, t),
        isFinal: isPickingFinal(task.status),
        createdAt: task.createTime ?? null,
        showCustomer: false,
        personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
        personName: '—',
        qtyText: formatQtyPcs(qty),
        description: null
      })
    }
    stations.push(buildStation('picking', 'packingDetail.flowPanel.stations.picking', cards))
  }

  // 5. 出库（优先匹配当前装箱单号）
  {
    const all = sortByCreatedAsc(aggregates?.stockOuts ?? [], (x) => x.createTime ?? x.stockOutDate)
    const matched = packingCode
      ? all.filter((x) => packingCodeMatched(x.packingCodes, packingCode))
      : []
    const list = matched.length > 0 ? matched : packingCode ? [] : all
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.stockOutCode),
      docRoute: !mask ? { name: 'StockOutDetail', params: { id: x.id } } : undefined,
      statusText: stockOutStatusLabel(x.status, t),
      isFinal: isStockOutFinal(x.status),
      createdAt: x.createTime ?? x.stockOutDate,
      showCustomer: true,
      customerName: mask ? '—' : x.customerName,
      customerCode: mask ? '—' : x.customerCode,
      personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName),
      qtyText: formatQtyPcs(x.totalQuantity),
      description: null
    }))
    stations.push(buildStation('stockOut', 'packingDetail.flowPanel.stations.stockOut', cards))
  }

  return stations
}

export { formatFlowCardDate }
