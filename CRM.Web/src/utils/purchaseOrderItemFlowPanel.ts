import type { PurchaseOrderDetailTabAggregates } from '@/api/purchaseOrder'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { formatFlowCardDate, resolveFlowPartyId } from '@/utils/sellOrderItemFlowPanel'
import {
  formatTotalAmountNumber,
  formatUnitPriceWithCurrencyCodeSuffix,
  listAmountCurrencyIso
} from '@/utils/moneyFormat'

export type FlowStationStatus = 'empty' | 'active' | 'done'

export type PoFlowStationKey =
  | 'purchaseRequisition'
  | 'purchaseOrderItem'
  | 'paymentRequest'
  | 'payment'
  | 'arrivalNotice'
  | 'qc'
  | 'stockIn'
  | 'purchaseInvoice'

export interface FlowDocRoute {
  name: string
  params?: Record<string, string>
  query?: Record<string, string>
}

export interface PoFlowCard {
  id: string
  docNo: string
  docRoute?: FlowDocRoute
  statusText: string
  isFinal: boolean
  createdAt?: string | null
  showVendor: boolean
  vendorId?: string | null
  vendorName?: string | null
  vendorCode?: string | null
  personRoleKey: string
  personName?: string | null
  unitPriceText?: string | null
  qtyText?: string | null
  description?: string | null
  /** 入库站：入库类型文案，与状态同一列展示 */
  bizTypeText?: string | null
}

export interface PoFlowStation {
  key: PoFlowStationKey
  titleKey: string
  stationStatus: FlowStationStatus
  cards: PoFlowCard[]
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

const FINANCE_PAYMENT_STATUS_CANCELLED = -2
const FINANCE_PAYMENT_STATUS_COMPLETED = 100

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

function stationStatusFromCards(cards: PoFlowCard[]): FlowStationStatus {
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

function paymentStatusLabel(v: unknown): string {
  const map: Record<number, string> = {
    1: '新建',
    2: '待审核',
    10: '审核通过',
    100: '付款完成',
    [-1]: '审核失败',
    [-2]: '已取消'
  }
  const s = Number(v)
  return Number.isFinite(s) ? (map[s] ?? String(s)) : '—'
}

function isPaymentRequestFinal(v: unknown) {
  const s = Number(v)
  return s === FINANCE_PAYMENT_STATUS_COMPLETED || s === -1
}

function arrivalStatusLabel(v: unknown, t: TFunc): string {
  const keyMap: Record<number, string> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[Number(v)]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
}

function isArrivalFinal(v: unknown) {
  return Number(v) === 100
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

function purchaseInvoiceStatusLabel(confirmStatus: unknown, redInvoiceStatus: unknown): string {
  const red = Number(redInvoiceStatus)
  if (Number.isFinite(red) && red > 0) return '红冲'
  return Number(confirmStatus) === 1 ? '已认证' : '未认证'
}

function isPurchaseInvoiceFinal(confirmStatus: unknown, redInvoiceStatus: unknown) {
  const red = Number(redInvoiceStatus)
  if (Number.isFinite(red) && red > 0) return true
  return Number(confirmStatus) === 1
}

function buildStation(
  key: PoFlowStationKey,
  titleKey: string,
  cards: PoFlowCard[]
): PoFlowStation {
  return {
    key,
    titleKey,
    stationStatus: stationStatusFromCards(cards),
    cards
  }
}

/** 列表行 vendorCode 常为空（主单未冗余），从下游单据取第一个可用编号。 */
function pickFirstVendorCode(aggregates: PurchaseOrderDetailTabAggregates | null | undefined): string | null {
  const buckets: Array<Array<{ vendorCode?: string | null } | undefined> | undefined> = [
    aggregates?.stockIns,
    aggregates?.arrivalNotices
  ]
  for (const list of buckets) {
    for (const row of list ?? []) {
      const c = String(row?.vendorCode ?? '').trim()
      if (c) return c
    }
  }
  return null
}

function pickFirstVendorId(aggregates: PurchaseOrderDetailTabAggregates | null | undefined): string | null {
  const buckets: Array<Array<{ vendorId?: string | null } | undefined> | undefined> = [
    aggregates?.stockIns,
    aggregates?.stockItems,
    aggregates?.arrivalNotices
  ]
  for (const list of buckets) {
    for (const row of list ?? []) {
      const id = String(row?.vendorId ?? '').trim()
      if (id) return id
    }
  }
  return null
}

export { formatFlowCardDate as formatPoFlowCardDate }

export function buildPurchaseOrderItemFlowStations(
  row: RowRecord | null | undefined,
  aggregates: PurchaseOrderDetailTabAggregates | null | undefined,
  t: TFunc,
  options?: { maskSensitive?: boolean }
): PoFlowStation[] {
  const mask = !!options?.maskSensitive
  const stations: PoFlowStation[] = []

  const lineVendorName = mask ? '—' : (String(row?.vendorName ?? '').trim() || null)
  const lineVendorCode = mask
    ? '—'
    : (String(row?.vendorCode ?? '').trim() ||
        pickFirstVendorCode(aggregates) ||
        null)
  const lineVendorId = resolveFlowPartyId(mask, row?.vendorId, pickFirstVendorId(aggregates))

  function resolveVendorName(docName?: string | null): string | null {
    if (mask) return '—'
    const n = String(docName ?? '').trim()
    return n || lineVendorName
  }

  function resolveVendorCode(docCode?: string | null): string | null {
    if (mask) return '—'
    const c = String(docCode ?? '').trim()
    return c || lineVendorCode
  }

  // 1. 采购申请
  {
    const list = sortByCreatedAsc(aggregates?.purchaseRequisitions ?? [], (x) => x.createTime)
    const cards: PoFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.billCode),
      docRoute: !mask ? { name: 'PurchaseRequisitionDetail', params: { id: x.id } } : undefined,
      statusText: prStatusLabel(x.status, t),
      isFinal: isPrFinal(x.status),
      createdAt: x.createTime,
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(null),
      vendorCode: resolveVendorCode(null),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.purchaser',
      personName: '—',
      qtyText: formatQtyPcs(x.qty),
      description: null
    }))
    stations.push(
      buildStation('purchaseRequisition', 'purchaseOrderItemList.flowPanel.stations.purchaseRequisition', cards)
    )
  }

  // 2. 采购订单明细
  {
    const cards: PoFlowCard[] = []
    if (row) {
      const itemId = String(row.purchaseOrderItemId ?? row.id ?? '').trim()
      const orderId = String(row.purchaseOrderId ?? '').trim()
      const code = String(row.purchaseOrderItemCode ?? '').trim() || '—'
      const status = Number(row.itemStatus)
      cards.push({
        id: itemId || 'poi',
        docNo: code,
        docRoute:
          orderId && !mask
            ? {
                name: 'PurchaseOrderDetail',
                params: { id: orderId },
                query: itemId ? { purchaseOrderItemId: itemId } : undefined
              }
            : undefined,
        statusText: poItemStatusLabel(status),
        isFinal: isPoItemFinal(status),
        createdAt: (row.orderCreateTime ?? row.createTime ?? null) as string | null,
        showVendor: true,
      vendorId: lineVendorId,
        vendorName: resolveVendorName(row.vendorName as string | null),
        vendorCode: resolveVendorCode(row.vendorCode as string | null),
        personRoleKey: 'purchaseOrderItemList.flowPanel.role.purchaser',
        personName: mask ? '—' : dash(row.purchaseUserName as string | null),
        unitPriceText: mask
          ? '—'
          : formatUnitPriceWithCurrencyCodeSuffix(row.cost, Number(row.currency)),
        qtyText: formatQtyPcs(row.qty),
        description: null
      })
    }
    stations.push(
      buildStation('purchaseOrderItem', 'purchaseOrderItemList.flowPanel.stations.purchaseOrderItem', cards)
    )
  }

  const payments = sortByCreatedAsc(
    (aggregates?.payments ?? []).filter((x) => !x.isDeleted),
    (x) => x.createTime
  )
  const requestPayments = payments.filter((x) => Number(x.status) !== FINANCE_PAYMENT_STATUS_CANCELLED)
  const paidPayments = payments.filter((x) => Number(x.status) === FINANCE_PAYMENT_STATUS_COMPLETED)

  // 3. 申请付款
  {
    const cards: PoFlowCard[] = requestPayments.map((x) => ({
      id: `req-${x.id}`,
      docNo: dash(x.financePaymentCode),
      docRoute: !mask ? { name: 'FinancePaymentDetail', params: { id: x.id } } : undefined,
      statusText: paymentStatusLabel(x.status),
      isFinal: isPaymentRequestFinal(x.status),
      createdAt: x.createTime,
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(x.vendorName),
      vendorCode: resolveVendorCode(null),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.requester',
      personName: dash(x.createUserName),
      unitPriceText: mask
        ? '—'
        : formatAmountWithCurrency(x.paymentAmountToBe, x.paymentCurrency),
      description: null
    }))
    stations.push(
      buildStation('paymentRequest', 'purchaseOrderItemList.flowPanel.stations.paymentRequest', cards)
    )
  }

  // 4. 付款
  {
    const cards: PoFlowCard[] = paidPayments.map((x) => ({
      id: `pay-${x.id}`,
      docNo: dash(x.financePaymentCode),
      docRoute: !mask ? { name: 'FinancePaymentDetail', params: { id: x.id } } : undefined,
      statusText: paymentStatusLabel(x.status),
      isFinal: true,
      createdAt: x.paymentDate ?? x.createTime,
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(x.vendorName),
      vendorCode: resolveVendorCode(null),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.requester',
      personName: dash(x.createUserName),
      unitPriceText: mask
        ? '—'
        : formatAmountWithCurrency(x.paymentAmount, x.paymentCurrency),
      description: null
    }))
    stations.push(buildStation('payment', 'purchaseOrderItemList.flowPanel.stations.payment', cards))
  }

  // 5. 到货通知
  {
    const list = sortByCreatedAsc(aggregates?.arrivalNotices ?? [], (x) => x.createTime)
    const cards: PoFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.noticeCode),
      docRoute: !mask ? { name: 'ArrivalNoticeList', query: { noticeId: x.id } } : undefined,
      statusText: arrivalStatusLabel(x.status, t),
      isFinal: isArrivalFinal(x.status),
      createdAt: x.createTime,
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(x.vendorName),
      vendorCode: resolveVendorCode(x.vendorCode),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.purchaser',
      personName: dash(x.purchaseUserName),
      qtyText: formatQtyPcs(x.expectQty ?? x.items?.[0]?.qty),
      description: null
    }))
    stations.push(
      buildStation('arrivalNotice', 'purchaseOrderItemList.flowPanel.stations.arrivalNotice', cards)
    )
  }

  // 6. 质检
  {
    const list = sortByCreatedAsc(aggregates?.qcs ?? [], (x) => x.createTime)
    const cards: PoFlowCard[] = list.map((x) => ({
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
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(null),
      vendorCode: resolveVendorCode(null),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName),
      qtyText: `${Number(x.passQty) || 0}/${Number(x.rejectQty) || 0} pcs`,
      description: null
    }))
    stations.push(buildStation('qc', 'purchaseOrderItemList.flowPanel.stations.qc', cards))
  }

  // 7. 入库
  {
    const list = sortByCreatedAsc(aggregates?.stockIns ?? [], (x) => x.createTime ?? x.stockInDate)
    const cards: PoFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.stockInCode),
      docRoute: !mask ? { name: 'StockInDetail', params: { id: x.id } } : undefined,
      statusText: stockInStatusLabel(x.status, t),
      isFinal: isStockInFinal(x.status),
      createdAt: x.createTime ?? x.stockInDate,
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(x.vendorName),
      vendorCode: resolveVendorCode(x.vendorCode),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.creator',
      personName: dash(x.createUserName),
      qtyText: formatQtyPcs(x.totalQuantity),
      description: null,
      bizTypeText: t(`stockInList.stockInTypeLabels.${resolveStockInTypeLabelKey(x.stockInType)}`)
    }))
    stations.push(buildStation('stockIn', 'purchaseOrderItemList.flowPanel.stations.stockIn', cards))
  }

  // 8. 进项发票
  {
    const list = sortByCreatedAsc(aggregates?.purchaseInvoices ?? [], (x) => x.createTime)
    const cards: PoFlowCard[] = list.map((x) => ({
      id: x.id,
      docNo: dash(x.invoiceNo),
      docRoute: !mask ? { name: 'FinancePurchaseInvoiceDetail', params: { id: x.id } } : undefined,
      statusText: purchaseInvoiceStatusLabel(x.confirmStatus, x.redInvoiceStatus),
      isFinal: isPurchaseInvoiceFinal(x.confirmStatus, x.redInvoiceStatus),
      createdAt: x.createTime,
      showVendor: true,
      vendorId: lineVendorId,
      vendorName: resolveVendorName(x.vendorName),
      vendorCode: resolveVendorCode(null),
      personRoleKey: 'purchaseOrderItemList.flowPanel.role.creator',
      personName: '—',
      unitPriceText: mask ? '—' : formatAmountWithCurrency(x.invoiceAmount),
      description: null
    }))
    stations.push(
      buildStation('purchaseInvoice', 'purchaseOrderItemList.flowPanel.stations.purchaseInvoice', cards)
    )
  }

  return stations
}
