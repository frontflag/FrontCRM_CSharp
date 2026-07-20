import type { SalesOrderDetailTabAggregates } from '@/api/salesOrder'
import { packingStatusLabel } from '@/api/packing'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { translateSalesOrderStatus } from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  formatTotalAmountNumber,
  formatUnitPriceWithCurrencyCodeSuffix,
  listAmountCurrencyIso
} from '@/utils/moneyFormat'

export type FlowStationStatus = 'empty' | 'active' | 'done'

export type FlowStationKey =
  | 'sellOrderItem'
  | 'purchaseRequisition'
  | 'purchaseOrderItem'
  | 'qc'
  | 'stockIn'
  | 'stockOutNotify'
  | 'stockingUsage'
  | 'packing'
  | 'stockOut'
  | 'receiptWriteOff'
  | 'invoice'

export interface FlowDocRoute {
  name: string
  params?: Record<string, string>
  query?: Record<string, string>
}

export interface FlowCard {
  id: string
  docNo: string
  docRoute?: FlowDocRoute
  statusText: string
  isFinal: boolean
  createdAt?: string | null
  showCustomer: boolean
  customerName?: string | null
  customerCode?: string | null
  personRoleKey: string
  personName?: string | null
  unitPriceText?: string | null
  qtyText?: string | null
  description?: string | null
}

export interface FlowStation {
  key: FlowStationKey
  titleKey: string
  stationStatus: FlowStationStatus
  cards: FlowCard[]
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

function stationStatusFromCards(cards: FlowCard[]): FlowStationStatus {
  if (cards.length === 0) return 'empty'
  if (cards.every((c) => c.isFinal)) return 'done'
  return 'active'
}

function formatAmountWithCurrency(amount: unknown, currency?: number): string {
  const a = formatTotalAmountNumber(amount)
  if (a === '—') return a
  return `${a} ${listAmountCurrencyIso(currency)}`
}

function formatQtyPcs(qty: unknown): string {
  const n = Number(qty)
  if (!Number.isFinite(n)) return '—'
  return `${Math.trunc(n)} pcs`
}

function prStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === 0) return t('salesOrderDetailView.prStatus0')
  if (s === 1) return t('salesOrderDetailView.prStatus1')
  if (s === 2) return t('salesOrderDetailView.prStatus2')
  if (s === 3) return t('salesOrderDetailView.prStatus3')
  return Number.isFinite(s) ? String(s) : '—'
}

function isPrFinal(v: unknown) {
  const s = Number(v)
  return s === 2 || s === 3
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

function invoiceStatusLabel(v: unknown, t: TFunc): string {
  const s = Number(v)
  if (s === 1) return t('salesOrderDetailView.invSt1')
  if (s === 2) return t('salesOrderDetailView.invSt2')
  if (s === 100) return t('salesOrderDetailView.invSt100')
  if (s === 101) return t('salesOrderDetailView.invSt101')
  if (s === -1) return t('salesOrderDetailView.invStNeg1')
  return Number.isFinite(s) ? String(s) : '—'
}

function isInvoiceFinal(v: unknown) {
  const s = Number(v)
  return s === 100 || s === 101 || s === -1
}

function isSalesOrderFinal(v: unknown) {
  const s = Number(v)
  // 常见：取消 / 关闭类；具体以 translate 文案为准，终态取负值或 100+ 关闭
  return s < 0 || s === 100 || s === 110 || s === 120
}

function buildStation(
  key: FlowStationKey,
  titleKey: string,
  cards: FlowCard[]
): FlowStation {
  return {
    key,
    titleKey,
    stationStatus: stationStatusFromCards(cards),
    cards
  }
}

export function buildSellOrderItemFlowStations(
  row: RowRecord | null | undefined,
  aggregates: SalesOrderDetailTabAggregates | null | undefined,
  t: TFunc,
  options?: { maskSensitive?: boolean }
): FlowStation[] {
  const mask = !!options?.maskSensitive
  const stations: FlowStation[] = []
  const lineCustomerCode = (() => {
    if (mask) return '—'
    const c = String(row?.customerCode ?? '').trim()
    return c || null
  })()

  function resolveCustomerCode(docCode?: string | null): string | null {
    if (mask) return '—'
    const c = String(docCode ?? '').trim()
    return c || lineCustomerCode
  }

  // 1. 销售订单明细
  {
    const cards: FlowCard[] = []
    if (row) {
      const itemId = String(row.sellOrderItemId ?? row.id ?? '').trim()
      const orderId = String(row.sellOrderId ?? '').trim()
      const code = String(row.sellOrderItemCode ?? '').trim() || '—'
      const status = Number(row.orderStatus)
      cards.push({
        id: itemId || 'soi',
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
        qtyText: formatQtyPcs(row.qty),
        description: null
      })
    }
    stations.push(buildStation('sellOrderItem', 'salesOrderItemList.flowPanel.stations.sellOrderItem', cards))
  }

  // 2. 采购申请
  {
    const list = sortByCreatedAsc(aggregates?.purchaseRequisitions ?? [], (x) => x.createTime)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.billCode),
      docRoute: !mask ? { name: 'PurchaseRequisitionDetail', params: { id: x.id } } : undefined,
      statusText: prStatusLabel(x.status, t),
      isFinal: isPrFinal(x.status),
      createdAt: x.createTime,
      showCustomer: false,
      personRoleKey: 'salesOrderItemList.flowPanel.role.purchaser',
      personName: '—',
      qtyText: formatQtyPcs(x.qty),
      description: null
    }))
    stations.push(buildStation('purchaseRequisition', 'salesOrderItemList.flowPanel.stations.purchaseRequisition', cards))
  }

  // 3. 采购明细
  {
    const list = sortByCreatedAsc(aggregates?.purchaseOrderItems ?? [], (x) => x.createTime)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.purchaseOrderItemCode),
      docRoute:
        x.purchaseOrderId && !mask
          ? {
              name: 'PurchaseOrderDetail',
              params: { id: x.purchaseOrderId },
              query: { purchaseOrderItemId: x.id }
            }
          : undefined,
      statusText: poItemStatusLabel(x.itemStatus),
      isFinal: isPoItemFinal(x.itemStatus),
      createdAt: x.createTime,
      showCustomer: false,
      personRoleKey: 'salesOrderItemList.flowPanel.role.purchaser',
      personName: dash(x.purchaseUserName),
      unitPriceText: formatUnitPriceWithCurrencyCodeSuffix(x.cost, x.currency),
      qtyText: formatQtyPcs(x.qty),
      description: null
    }))
    stations.push(buildStation('purchaseOrderItem', 'salesOrderItemList.flowPanel.stations.purchaseOrderItem', cards))
  }

  // 4. 质检
  {
    const list = sortByCreatedAsc(aggregates?.qcs ?? [], (x) => x.createTime)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.qcCode),
      docRoute:
        !mask && x.stockInNotifyId
          ? {
              name: 'QcCreate',
              query: { noticeId: x.stockInNotifyId, qcId: x.id }
            }
          : undefined,
      statusText: qcStatusLabel(x.status, t),
      isFinal: isQcFinal(x.status),
      createdAt: x.createTime,
      showCustomer: false,
      personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName ?? x.CreateUserName),
      qtyText: `${Number(x.passQty) || 0}/${Number(x.rejectQty) || 0} pcs`,
      description: null
    }))
    stations.push(buildStation('qc', 'salesOrderItemList.flowPanel.stations.qc', cards))
  }

  // 5. 入库
  {
    const list = sortByCreatedAsc(aggregates?.stockIns ?? [], (x) => x.createTime ?? x.stockInDate)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.stockInCode),
      docRoute: !mask ? { name: 'StockInDetail', params: { id: x.id } } : undefined,
      statusText: stockInStatusLabel(x.status, t),
      isFinal: isStockInFinal(x.status),
      createdAt: x.createTime ?? x.stockInDate,
      showCustomer: false,
      personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName),
      qtyText: formatQtyPcs(x.totalQuantity),
      description: null
    }))
    stations.push(buildStation('stockIn', 'salesOrderItemList.flowPanel.stations.stockIn', cards))
  }

  // 6. 出库通知
  {
    const list = sortByCreatedAsc(aggregates?.stockOutRequests ?? [], (x) => x.createTime ?? x.requestDate)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.requestCode),
      docRoute: !mask ? { name: 'StockOutNotifyDetail', params: { id: x.id } } : undefined,
      statusText: stockOutNotifyStatusLabel(x.status, t),
      isFinal: isStockOutNotifyFinal(x.status),
      createdAt: x.createTime ?? x.requestDate,
      showCustomer: true,
      customerName: mask ? '—' : x.customerName,
      customerCode: resolveCustomerCode(null),
      personRoleKey: 'salesOrderItemList.flowPanel.role.requester',
      personName: dash(x.requestUserName),
      qtyText: formatQtyPcs(x.outQuantity),
      description: null
    }))
    stations.push(buildStation('stockOutNotify', 'salesOrderItemList.flowPanel.stations.stockOutNotify', cards))
  }

  // 7. 使用备货（无记录则整站不显示）
  {
    const list = sortByCreatedAsc(aggregates?.stockingUsage?.items ?? [], (x) => x.purchaseOrderCreateTime)
    if (list.length > 0) {
      const cards: FlowCard[] = list.map((x) => ({
        id: x.purchaseOrderId,
        docNo: dash(x.purchaseOrderCode),
        docRoute:
          x.purchaseOrderId && !mask
            ? { name: 'PurchaseOrderDetail', params: { id: x.purchaseOrderId } }
            : undefined,
        statusText: t('salesOrderItemList.flowPanel.stockingUsageStatus'),
        isFinal: true,
        createdAt: x.purchaseOrderCreateTime,
        showCustomer: false,
        personRoleKey: 'salesOrderItemList.flowPanel.role.purchaser',
        personName: dash(x.purchaseUserName),
        qtyText: formatQtyPcs(x.usedQty),
        description: null
      }))
      stations.push(buildStation('stockingUsage', 'salesOrderItemList.flowPanel.stations.stockingUsage', cards))
    }
  }

  // 8. 装箱
  {
    const list = sortByCreatedAsc(aggregates?.packings ?? [], (x) => x.createTime)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.code),
      docRoute: !mask ? { name: 'PackingDetail', params: { id: x.id } } : undefined,
      statusText: packingStatusLabel(Number(x.status)),
      isFinal: isPackingFinal(x.status),
      createdAt: x.createTime,
      showCustomer: true,
      customerName: mask ? '—' : x.customerName,
      customerCode: resolveCustomerCode(null),
      personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName),
      qtyText: formatQtyPcs(x.itemRows),
      description: null
    }))
    stations.push(buildStation('packing', 'salesOrderItemList.flowPanel.stations.packing', cards))
  }

  // 9. 出库
  {
    const list = sortByCreatedAsc(aggregates?.stockOuts ?? [], (x) => x.createTime ?? x.stockOutDate)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.stockOutCode),
      docRoute: !mask ? { name: 'StockOutDetail', params: { id: x.id } } : undefined,
      statusText: stockOutStatusLabel(x.status, t),
      isFinal: isStockOutFinal(x.status),
      createdAt: x.createTime ?? x.stockOutDate,
      showCustomer: true,
      customerName: mask ? '—' : x.customerName,
      customerCode: resolveCustomerCode(x.customerCode),
      personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName),
      qtyText: formatQtyPcs(x.totalQuantity),
      description: null
    }))
    stations.push(buildStation('stockOut', 'salesOrderItemList.flowPanel.stations.stockOut', cards))
  }

  // 10. 收款核销
  {
    const list = sortByCreatedAsc(aggregates?.receiptWriteOffs ?? [], (x) => x.createTime)
    const cards: FlowCard[] = list.map((x) => {
      const docNo = dash(x.financeReceiptCode || x.receivableCode || x.id)
      return {
        id: x.id,
        docNo,
        docRoute:
          x.financeReceiptId && !mask
            ? { name: 'FinanceReceiptDetail', params: { id: x.financeReceiptId } }
            : undefined,
        statusText: t('salesOrderItemList.flowPanel.writeOffStatus'),
        isFinal: true,
        createdAt: x.createTime,
        showCustomer: true,
        customerName: mask ? '—' : x.customerName,
        customerCode: resolveCustomerCode(null),
        personRoleKey: 'salesOrderItemList.flowPanel.role.operator',
        personName: dash(x.operatorUserName),
        unitPriceText: mask ? '—' : formatAmountWithCurrency(x.amount, x.currency),
        qtyText: null,
        description: null
      }
    })
    stations.push(buildStation('receiptWriteOff', 'salesOrderItemList.flowPanel.stations.receiptWriteOff', cards))
  }

  // 11. 发票
  {
    const list = sortByCreatedAsc(aggregates?.sellInvoices ?? [], (x) => x.createTime)
    const cards: FlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.invoiceCode || x.invoiceNo),
      docRoute: !mask ? { name: 'FinanceSellInvoiceDetail', params: { id: x.id } } : undefined,
      statusText: invoiceStatusLabel(x.invoiceStatus, t),
      isFinal: isInvoiceFinal(x.invoiceStatus),
      createdAt: x.createTime ?? x.makeInvoiceDate,
      showCustomer: true,
      customerName: mask ? '—' : x.customerName,
      customerCode: resolveCustomerCode(null),
      personRoleKey: 'salesOrderItemList.flowPanel.role.creator',
      personName: '—',
      unitPriceText: mask ? '—' : formatAmountWithCurrency(x.invoiceTotal, x.currency),
      qtyText: null,
      description: null
    }))
    stations.push(buildStation('invoice', 'salesOrderItemList.flowPanel.stations.invoice', cards))
  }

  return stations
}

export function formatFlowCardDate(v?: string | null) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}
