import apiClient from './client'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import type { PurchaseOrderItemListLineRow } from '@/api/purchaseOrder'
import { normalizeStockOutRequestRow, normalizeStockOutListRow, type StockOutDto, type StockOutRequestDto } from '@/api/stockOut'
import { normalizePackingListItem, type PackingListItem } from '@/api/packing'
import { normalizeStockInNotifyRow, type StockInNotifyDto } from '@/api/logistics'

/** 报关公司服务方向：10 深圳、20 香港（与库列 Type 一致）。 */
export const CustomsBrokerRegionType = {
  Shenzhen: 10,
  HongKong: 20
} as const

export interface CustomsBrokerDto {
  id: string
  brokerCode: string
  cname: string
  ename?: string | null
  type: number
  status: number
  agencyRate?: number
  remark?: string | null
  createTime?: string
}

export const CUSTOMS_PENDLIST_STATUS = {
  Open: 1,
  CustomsOutNotifyCreated: 2,
  InCustomsProcess: 3,
  Closed: 10,
  Cancelled: -1
} as const

export interface CustomsPendlistListItemDto {
  id: string
  salesStockOutNotifyId: string
  salesStockOutNotifyCode?: string | null
  sellOrderItemId: string
  sellOrderItemCode?: string | null
  qty: number
  status: number
  customsStockOutNotifyId?: string | null
  customsStockOutNotifyCode?: string | null
  overseasWarehouseId?: string | null
  overseasWarehouseName?: string | null
  salesOrderId?: string | null
  salesOrderCode?: string | null
  materialCode?: string | null
  materialName?: string | null
  customerName?: string | null
  createTime: string
  createByUserId?: string | null
  createUserDisplay?: string | null
}

export interface CreateCustomsOutNotifyResultDto {
  pendlistId: string
  customsStockOutNotifyId: string
  customsStockOutNotifyCode: string
  pendlistStatus: number
}

export interface CustomsDeclarationListItemDto {
  id: string
  declarationCode: string
  stockOutRequestId?: string | null
  stockOutRequestCode?: string | null
  customsBrokerId: string
  customsBrokerName?: string | null
  declarationType: number
  internalStatus: number
  customsClearanceStatus: number
  declareDate: string
  totalTaxAmount: number
  remark?: string | null
  createTime: string
  createByUserId?: string | null
  createUserDisplay?: string | null
}

export interface CustomsDeclarationItemListItemDto {
  id: string
  declarationId: string
  declarationCode: string
  declareDate: string
  lineNo: number
  stockOutRequestId: string
  customerId?: string | null
  customerName?: string | null
  salesUserId?: string | null
  salesUserName?: string | null
  sellOrderItemCode?: string | null
  purchasePn?: string | null
  purchaseBrand?: string | null
  declareQty: number
  declareUnitPrice: number
  dutyAmount: number
  vatAmount: number
  customsPaymentGoods: number
  customsAgencyFee: number
  otherFee: number
  inspectionFee: number
  totalValueTax: number
  taxIncludedUnitPrice: number
  createTime: string
  createUserDisplay?: string | null
}

export interface StockTransferListItemDto {
  id: string
  transferCode: string
  bizScene: string
  customsDeclarationId: string
  declarationCode?: string | null
  status: number
  confirmedTime?: string | null
  confirmedByUserId?: string | null
  fromWarehouseId: string
  toWarehouseId: string
  fromWarehouseName?: string | null
  toWarehouseName?: string | null
  createTime: string
  createUserDisplay?: string | null
  isConfirmed: boolean
}

export interface CustomsDeclarationDetailItemViewDto {
  id: string
  lineNo: number
  hsCode?: string | null
  purchasePn?: string | null
  purchaseBrand?: string | null
  declareQty: number
  declareUnitPrice: number
  originalPurchasePrice: number
  purchaseCurrency?: number | null
  purchaseRatio?: number
  purchaseCostParamId?: string | null
  costUsd?: number
  dutyRate?: number
  vatRate?: number
  dutyAmount: number
  vatAmount: number
  customsPaymentGoods: number
  customsAgencyFee: number
  otherFee: number
  inspectionFee: number
  totalValueTax: number
  taxIncludedUnitPrice: number
  sellOrderItemCode?: string | null
  customerId?: string | null
  customerName?: string | null
  vendorId?: string | null
  vendorName?: string | null
  stockOutRequestId: string
  arrivalNotifyCode?: string | null
}

export interface CreateCustomsArrivalNotifiesResultDto {
  declarationId: string
  createdCount: number
  created: Array<{
    noticeId: string
    noticeCode: string
    lineNo: number
    customsDeclarationItemId: string
  }>
}

export interface CustomsDeclarationDetailDto {
  id: string
  declarationCode: string
  packingId?: string | null
  packingCode?: string | null
  stockOutRequestId?: string | null
  stockOutRequestCode?: string | null
  customsBrokerId: string
  customsBrokerName?: string | null
  customsBrokerCode?: string | null
  declarationType: number
  internalStatus: number
  customsClearanceStatus: number
  declareDate: string
  exchangeRate: number
  brokerAgencyRate?: number
  feesCalculatedAt?: string | null
  feesLocked?: boolean
  totalTaxAmount: number
  fromWarehouseId: string
  toWarehouseId: string
  fromWarehouseCode?: string | null
  toWarehouseCode?: string | null
  fromWarehouseName?: string | null
  toWarehouseName?: string | null
  remark?: string | null
  createTime: string
  createByUserId?: string | null
  createUserDisplay?: string | null
  canCreateArrivalNotifies?: boolean
  pendingArrivalNotifyCount?: number
  existingArrivalNotifyCount?: number
  existingArrivalNotifyCodes?: string[]
  arrivalNotifyBlockReason?: string | null
  items?: CustomsDeclarationDetailItemViewDto[]
}

export async function fetchCustomsBrokersAdmin(): Promise<CustomsBrokerDto[]> {
  return apiClient.get<CustomsBrokerDto[]>('/api/v1/customs-brokers', { params: { all: true } })
}

export async function fetchCustomsPendlists(params: {
  status?: number
  keyword?: string
  take?: number
}): Promise<CustomsPendlistListItemDto[]> {
  return apiClient.get<CustomsPendlistListItemDto[]>('/api/v1/customs-pendlists', { params })
}

export async function createCustomsOutNotifyFromPendlist(
  pendlistId: string
): Promise<CreateCustomsOutNotifyResultDto> {
  return apiClient.post<CreateCustomsOutNotifyResultDto>(
    `/api/v1/customs-pendlists/${encodeURIComponent(pendlistId)}/customs-out-notify`,
    {}
  )
}

export async function createCustomsBroker(body: {
  cname: string
  ename?: string | null
  type: number
  agencyRate?: number
  remark?: string | null
}): Promise<CustomsBrokerDto> {
  return apiClient.post<CustomsBrokerDto>('/api/v1/customs-brokers', body)
}

export async function updateCustomsBroker(
  id: string,
  body: {
    cname: string
    ename?: string | null
    type: number
    agencyRate?: number
    remark?: string | null
  }
): Promise<CustomsBrokerDto> {
  return apiClient.put<CustomsBrokerDto>(`/api/v1/customs-brokers/${encodeURIComponent(id)}`, body)
}

/** 1=启用，0=停用 */
export async function patchCustomsBrokerStatus(id: string, status: 0 | 1): Promise<CustomsBrokerDto> {
  return apiClient.patch<CustomsBrokerDto>(`/api/v1/customs-brokers/${encodeURIComponent(id)}/status`, {
    status
  })
}

export async function deleteCustomsBroker(id: string): Promise<void> {
  await apiClient.delete(`/api/v1/customs-brokers/${encodeURIComponent(id)}`)
}

export async function fetchCustomsDeclarations(params: Record<string, unknown>): Promise<CustomsDeclarationListItemDto[]> {
  return apiClient.get<CustomsDeclarationListItemDto[]>('/api/v1/customs-declarations', { params })
}

export async function fetchCustomsDeclarationById(id: string): Promise<CustomsDeclarationDetailDto> {
  return apiClient.get<CustomsDeclarationDetailDto>(`/api/v1/customs-declarations/${encodeURIComponent(id)}`)
}

export interface CustomsDeclarationBusinessRecordRowDto {
  id: string
  code: string
  status?: number | null
  occurredAt?: string | null
  parentId?: string | null
}

export interface CustomsDeclarationBusinessRecordsDto {
  salesOrders: CustomsDeclarationBusinessRecordRowDto[]
  salesOrderItems: SalesOrderItemLineRow[]
  stockOutNotifyItems: StockOutRequestDto[]
  purchaseOrders: CustomsDeclarationBusinessRecordRowDto[]
  purchaseOrderItems: PurchaseOrderItemListLineRow[]
  stockOutNotifies: CustomsDeclarationBusinessRecordRowDto[]
  customsStockOutNotifyItems: StockOutRequestDto[]
  customsStockOutNotifies: CustomsDeclarationBusinessRecordRowDto[]
  customsPackings: CustomsDeclarationBusinessRecordRowDto[]
  customsPackingItems: PackingListItem[]
  customsStockOuts: CustomsDeclarationBusinessRecordRowDto[]
  customsStockOutItems: StockOutDto[]
  customsArrivalNotifies: CustomsDeclarationBusinessRecordRowDto[]
  customsArrivalNotifyItems: StockInNotifyDto[]
  customsStockIns: CustomsDeclarationBusinessRecordRowDto[]
  packings: CustomsDeclarationBusinessRecordRowDto[]
  packingItems: PackingListItem[]
  stockOuts: CustomsDeclarationBusinessRecordRowDto[]
  stockOutItems: StockOutDto[]
}

export async function fetchCustomsDeclarationBusinessRecords(
  id: string
): Promise<CustomsDeclarationBusinessRecordsDto> {
  const raw = await apiClient.get<Record<string, unknown>>(
    `/api/v1/customs-declarations/${encodeURIComponent(id)}/business-records`
  )
  return normalizeCustomsDeclarationBusinessRecords(raw)
}

function normalizeRecordRow(raw: unknown): CustomsDeclarationBusinessRecordRowDto {
  const r = (raw ?? {}) as Record<string, unknown>
  return {
    id: String(r.id ?? r.Id ?? ''),
    code: String(r.code ?? r.Code ?? ''),
    status: (r.status ?? r.Status) as number | null | undefined,
    occurredAt: (r.occurredAt ?? r.OccurredAt) as string | null | undefined,
    parentId: (r.parentId ?? r.ParentId) as string | null | undefined
  }
}

function pickRecordRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizeRecordRow)
}

function normalizeSalesOrderItemLine(raw: unknown): SalesOrderItemLineRow {
  const r = (raw ?? {}) as Record<string, unknown>
  const sellOrderItemId = String(r.sellOrderItemId ?? r.SellOrderItemId ?? '').trim()
  const qty = Number(r.qty ?? r.Qty)
  const priceRaw = r.price ?? r.Price
  const lineTotalRaw = r.lineTotal ?? r.LineTotal
  let lineTotal: number | undefined
  if (lineTotalRaw != null && lineTotalRaw !== '') lineTotal = Number(lineTotalRaw)
  else if (priceRaw != null && priceRaw !== '' && Number.isFinite(Number(priceRaw))) {
    lineTotal = (Number.isFinite(qty) ? qty : 0) * Number(priceRaw)
  }

  return {
    sellOrderItemId,
    sellOrderId: String(r.sellOrderId ?? r.SellOrderId ?? ''),
    sellOrderCode: String(r.sellOrderCode ?? r.SellOrderCode ?? ''),
    sellOrderItemCode: (r.sellOrderItemCode ?? r.SellOrderItemCode) as string | undefined,
    orderStatus: r.orderStatus ?? r.OrderStatus,
    orderCreateTime: r.orderCreateTime ?? r.OrderCreateTime,
    customerId: r.customerId ?? r.CustomerId,
    customerName: r.customerName ?? r.CustomerName,
    salesUserName: r.salesUserName ?? r.SalesUserName,
    pn: r.pn ?? r.PN,
    brand: r.brand ?? r.Brand,
    customerSo: r.customerSo ?? r.CustomerSo,
    customerPn: r.customerPn ?? r.CustomerPn,
    qty: Number.isFinite(qty) ? qty : 0,
    price: r.price ?? r.Price,
    lineTotal,
    currency: r.currency ?? r.Currency,
    usdUnitPrice: r.usdUnitPrice ?? r.UsdUnitPrice,
    usdLineTotal: r.usdLineTotal ?? r.UsdLineTotal,
    itemStatus: r.itemStatus ?? r.ItemStatus,
    purchaseProgressStatus: r.purchaseProgressStatus ?? r.PurchaseProgressStatus,
    stockInProgressStatus: r.stockInProgressStatus ?? r.StockInProgressStatus,
    stockOutProgressStatus: r.stockOutProgressStatus ?? r.StockOutProgressStatus,
    stockOutNotifyProgressStatus: r.stockOutNotifyProgressStatus ?? r.StockOutNotifyProgressStatus,
    receiptProgressStatus: r.receiptProgressStatus ?? r.ReceiptProgressStatus,
    invoiceProgressStatus: r.invoiceProgressStatus ?? r.InvoiceProgressStatus,
    salesProfitExpected: r.salesProfitExpected ?? r.SalesProfitExpected,
    profitOutBizUsd: r.profitOutBizUsd ?? r.ProfitOutBizUsd,
    profitOutRateBiz: r.profitOutRateBiz ?? r.ProfitOutRateBiz
  } as SalesOrderItemLineRow
}

function pickSalesOrderItemRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizeSalesOrderItemLine)
}

function pickStockOutNotifyItemRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizeStockOutRequestRow)
}

function pickPackingItemRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizePackingListItem)
}

function pickStockOutItemRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizeStockOutListRow)
}

function pickArrivalNotifyItemRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizeStockInNotifyRow)
}

function normalizePurchaseOrderItemListLine(raw: unknown): PurchaseOrderItemListLineRow {
  const r = (raw ?? {}) as Record<string, unknown>
  const qty = Number(r.qty ?? r.Qty)
  const costRaw = r.cost ?? r.Cost
  const lineTotalRaw = r.lineTotal ?? r.LineTotal
  let lineTotal: number | undefined
  if (lineTotalRaw != null && lineTotalRaw !== '') lineTotal = Number(lineTotalRaw)
  else if (costRaw != null && costRaw !== '' && Number.isFinite(Number(costRaw))) {
    lineTotal = (Number.isFinite(qty) ? qty : 0) * Number(costRaw)
  }

  return {
    purchaseOrderItemId: String(r.purchaseOrderItemId ?? r.PurchaseOrderItemId ?? '').trim(),
    purchaseOrderId: String(r.purchaseOrderId ?? r.PurchaseOrderId ?? '').trim(),
    purchaseOrderItemCode: (r.purchaseOrderItemCode ?? r.PurchaseOrderItemCode) as string | undefined,
    purchaseOrderCode: (r.purchaseOrderCode ?? r.PurchaseOrderCode) as string | undefined,
    freightForwarderOrderNo: (r.freightForwarderOrderNo ?? r.FreightForwarderOrderNo) as string | null | undefined,
    purchaseOrderType: Number(r.purchaseOrderType ?? r.PurchaseOrderType ?? 0) || undefined,
    vendorId: (r.vendorId ?? r.VendorId) as string | null | undefined,
    vendorName: (r.vendorName ?? r.VendorName) as string | null | undefined,
    vendorEnglishName: (r.vendorEnglishName ?? r.VendorEnglishName) as string | null | undefined,
    itemStatus: (r.itemStatus ?? r.ItemStatus) as number | undefined,
    purchaseProgressStatus: (r.purchaseProgressStatus ?? r.PurchaseProgressStatus) as number | undefined,
    stockInProgressStatus: (r.stockInProgressStatus ?? r.StockInProgressStatus) as number | undefined,
    paymentRequestProgressStatus: (r.paymentRequestProgressStatus ?? r.PaymentRequestProgressStatus) as number | undefined,
    paymentProgressStatus: (r.paymentProgressStatus ?? r.PaymentProgressStatus) as number | undefined,
    invoiceProgressStatus: (r.invoiceProgressStatus ?? r.InvoiceProgressStatus) as number | undefined,
    orderCreateTime: (r.orderCreateTime ?? r.OrderCreateTime) as string | null | undefined,
    createTime: (r.createTime ?? r.CreateTime) as string | null | undefined,
    purchaseUserName: (r.purchaseUserName ?? r.PurchaseUserName) as string | null | undefined,
    createUserName: (r.createUserName ?? r.CreateUserName) as string | null | undefined,
    createdBy: (r.createdBy ?? r.CreatedBy) as string | null | undefined,
    pn: (r.pn ?? r.Pn ?? r.PN) as string | null | undefined,
    brand: (r.brand ?? r.Brand) as string | null | undefined,
    qty: Number.isFinite(qty) ? qty : undefined,
    cost: costRaw != null && costRaw !== '' ? Number(costRaw) : undefined,
    lineTotal,
    currency: (r.currency ?? r.Currency) as number | undefined
  }
}

function pickPurchaseOrderItemRows(raw: Record<string, unknown>, camel: string, pascal: string) {
  const rows = raw[camel] ?? raw[pascal]
  if (!Array.isArray(rows)) return []
  return rows.map(normalizePurchaseOrderItemListLine)
}

function normalizeCustomsDeclarationBusinessRecords(raw: unknown): CustomsDeclarationBusinessRecordsDto {
  const r = (raw ?? {}) as Record<string, unknown>
  return {
    salesOrders: pickRecordRows(r, 'salesOrders', 'SalesOrders'),
    salesOrderItems: pickSalesOrderItemRows(r, 'salesOrderItems', 'SalesOrderItems'),
    purchaseOrders: pickRecordRows(r, 'purchaseOrders', 'PurchaseOrders'),
    purchaseOrderItems: pickPurchaseOrderItemRows(r, 'purchaseOrderItems', 'PurchaseOrderItems'),
    stockOutNotifyItems: pickStockOutNotifyItemRows(r, 'stockOutNotifyItems', 'StockOutNotifyItems'),
    stockOutNotifies: pickRecordRows(r, 'stockOutNotifies', 'StockOutNotifies'),
    customsStockOutNotifyItems: pickStockOutNotifyItemRows(r, 'customsStockOutNotifyItems', 'CustomsStockOutNotifyItems'),
    customsStockOutNotifies: pickRecordRows(r, 'customsStockOutNotifies', 'CustomsStockOutNotifies'),
    customsPackings: pickRecordRows(r, 'customsPackings', 'CustomsPackings'),
    customsPackingItems: pickPackingItemRows(r, 'customsPackingItems', 'CustomsPackingItems'),
    customsStockOuts: pickRecordRows(r, 'customsStockOuts', 'CustomsStockOuts'),
    customsStockOutItems: pickStockOutItemRows(r, 'customsStockOutItems', 'CustomsStockOutItems'),
    customsArrivalNotifies: pickRecordRows(r, 'customsArrivalNotifies', 'CustomsArrivalNotifies'),
    customsArrivalNotifyItems: pickArrivalNotifyItemRows(r, 'customsArrivalNotifyItems', 'CustomsArrivalNotifyItems'),
    customsStockIns: pickRecordRows(r, 'customsStockIns', 'CustomsStockIns'),
    packings: pickRecordRows(r, 'packings', 'Packings'),
    packingItems: pickPackingItemRows(r, 'packingItems', 'PackingItems'),
    stockOuts: pickRecordRows(r, 'stockOuts', 'StockOuts'),
    stockOutItems: pickStockOutItemRows(r, 'stockOutItems', 'StockOutItems')
  }
}

export async function patchCustomsClearanceStatus(id: string, customsClearanceStatus: number): Promise<void> {
  await apiClient.patch(`/api/v1/customs-declarations/${encodeURIComponent(id)}/customs-clearance-status`, {
    customsClearanceStatus
  })
}

export async function patchCustomsDeclarationHeader(
  id: string,
  body: {
    toWarehouseId?: string | null
    remark?: string | null
    exchangeRate?: number | null
    customsBrokerId?: string | null
  }
): Promise<void> {
  await apiClient.patch(`/api/v1/customs-declarations/${encodeURIComponent(id)}`, body)
}

export interface PatchCustomsDeclarationItemBody {
  hsCode?: string | null
  declareQty?: number | null
  declareUnitPrice?: number | null
  dutyRate?: number | null
  vatRate?: number | null
  otherFee?: number | null
  inspectionFee?: number | null
}

export async function patchCustomsDeclarationItem(
  id: string,
  body: PatchCustomsDeclarationItemBody
): Promise<void> {
  await apiClient.patch(`/api/v1/customs-declaration-items/${encodeURIComponent(id)}`, body)
}

export interface RecalculateCustomsDeclarationFeesResultDto {
  declarationId: string
  feesCalculatedAtUtc: string
  totalTaxAmount: number
  lineCount: number
}

export async function recalculateCustomsDeclarationFees(
  id: string
): Promise<RecalculateCustomsDeclarationFeesResultDto> {
  return apiClient.post<RecalculateCustomsDeclarationFeesResultDto>(
    `/api/v1/customs-declarations/${encodeURIComponent(id)}/recalculate-fees`,
    {}
  )
}

export type { PurchaseCostParamDto } from './purchaseCostParam'
export { fetchEffectivePurchaseCostParam } from './purchaseCostParam'

export async function createCustomsArrivalNotifies(
  id: string
): Promise<CreateCustomsArrivalNotifiesResultDto> {
  return apiClient.post<CreateCustomsArrivalNotifiesResultDto>(
    `/api/v1/customs-declarations/${encodeURIComponent(id)}/create-arrival-notifies`,
    {}
  )
}

export async function completeCustomsDeclaration(id: string): Promise<void> {
  await apiClient.post(`/api/v1/customs-declarations/${encodeURIComponent(id)}/complete`, {})
}

export async function deleteCustomsDeclaration(id: string): Promise<void> {
  await apiClient.delete(`/api/v1/customs-declarations/${encodeURIComponent(id)}`)
}

export async function forceDeleteCustomsDeclaration(id: string, confirmBillCode: string): Promise<void> {
  await apiClient.post(`/api/v1/customs-declarations/${encodeURIComponent(id)}/force-delete`, {
    confirmBillCode: confirmBillCode.trim()
  })
}

export async function fetchCustomsDeclarationItems(
  params: Record<string, unknown>
): Promise<CustomsDeclarationItemListItemDto[]> {
  return apiClient.get<CustomsDeclarationItemListItemDto[]>('/api/v1/customs-declaration-items', { params })
}

export interface StockTransferPaged {
  items: StockTransferListItemDto[]
  total: number
  page: number
  pageSize: number
}

export async function fetchStockTransfers(params: Record<string, unknown>): Promise<StockTransferPaged> {
  return apiClient.get<StockTransferPaged>('/api/v1/inventory/transfers-customers', { params })
}

export async function confirmStockTransfer(id: string): Promise<void> {
  await apiClient.patch(`/api/v1/inventory/transfers-customers/${encodeURIComponent(id)}/confirm`, {})
}
