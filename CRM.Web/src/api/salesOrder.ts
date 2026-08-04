import apiClient, { type ApiRejectedError } from './client'
import { fetchCompanyProfileForReport, type CompanyProfileBundle } from './companyProfile'
import type { QcImageReadonlyRow } from './document'
import { buildQueryString } from '@/utils/progressStatusQuery'

function httpStatusFromApiError(e: unknown): number | undefined {
  if (typeof e !== 'object' || e === null) return undefined
  return (e as ApiRejectedError).httpStatus
}

export interface SalesOrderItemExtendFieldChangeDto {
  field: string
  label: string
  before: string
  after: string
}

export interface SalesOrderItemExtendChangeDto {
  sellOrderItemId: string
  sellOrderItemCode?: string
  fields: SalesOrderItemExtendFieldChangeDto[]
}

export interface SalesOrderItemExtendRefreshResult {
  salesOrderId: string
  totalItems: number
  changedItems: number
  changedFieldsCount: number
  syncedStockOutNotifyStatusCount?: number
  refreshedAt: string
  changes: SalesOrderItemExtendChangeDto[]
}

export interface SalesOrderCustomerDownstreamSyncPreviewItem {
  category: 'sellOrder' | 'stockOutNotify' | 'packing' | 'packingItemExtend' | 'stockOut' | string
  documentCode: string
  customerId?: string | null
  customerName?: string | null
  isMismatch: boolean
}

export interface SalesOrderCustomerDownstreamSyncPreview {
  salesOrderId: string
  sellOrderCode?: string | null
  customerId?: string | null
  customerName?: string | null
  oldCustomerId?: string | null
  oldCustomerName?: string | null
  canSync: boolean
  noOp: boolean
  blockReason?: string | null
  blockingDocuments: string[]
  /** 销售订单头客户名称快照是否需按主数据刷新（0/1） */
  sellOrderCustomerNameToSync: number
  stockOutNotifiesToSync: number
  packingsToSync: number
  packingItemExtendsToSync: number
  stockOutsToSync: number
  syncItems: SalesOrderCustomerDownstreamSyncPreviewItem[]
}

export interface SalesOrderCustomerDownstreamSyncApplyResult {
  preview: SalesOrderCustomerDownstreamSyncPreview
  applied: boolean
}

export interface SalesOrderFieldChangeLogRow {
  id: string
  sellOrderId: string
  sellOrderCode?: string | null
  objectLabel?: string | null
  fieldName: string
  fieldLabel?: string | null
  oldValue?: string | null
  newValue?: string | null
  changedByUserId?: string | null
  changedByUserName?: string | null
  changedAt: string
}

export interface SalesOrderDeletedItemRow {
  sellOrderItemId: string
  sellOrderItemCode?: string | null
  pn?: string | null
  brand?: string | null
  qty: number
  price: number
  currency: number
  comment?: string | null
  createTime?: string | null
  deletedAt?: string | null
  deletedByUserId?: string | null
  deletedByUserName?: string | null
}

import type { QcInfoDto } from './logistics'
import type { PackingListItem } from './packing'
import type { StockInListItemDto } from './stockIn'
import type { StockOutDto, StockOutRequestDto } from './stockOut'

/** GET /api/v1/sales-orders/{id}/detail-tab-aggregates */
export interface SalesOrderDetailTabAggregates {
  rfqItems: Array<{
    id: string
    rfqId: string
    rfqCode: string
    lineNo: number
    mpn: string
    customerMpn?: string | null
    customerBrand?: string | null
    brand: string
    quantity: number
    status: number
    productionDate?: string | null
    customerName?: string | null
    salesUserName?: string | null
    sellOrderItemId?: string | null
    sellOrderItemCode?: string | null
    quoteCode?: string | null
    assignedPurchaserName1?: string | null
    assignedPurchaserName2?: string | null
    rfqCreateTime: string
    createTime: string
  }>
  quotes: Array<{
    id: string
    quoteCode: string
    mpn?: string | null
    brand?: string | null
    status: number
    rfqCode?: string | null
    salesUserName?: string | null
    purchaseUserName?: string | null
    quoteDate: string
    sellOrderItemId?: string | null
    sellOrderItemCode?: string | null
    items: Array<{
      quantity: number
      unitPrice: number
      currency: number
      vendorName?: string | null
    }>
    createTime: string
  }>
  purchaseRequisitions: Array<{
    id: string
    billCode: string
    status: number
    sellOrderItemId: string
    pn?: string | null
    brand?: string | null
    qty: number
    expectedPurchaseTime: string
    createTime: string
  }>
  purchaseOrderItems: Array<{
    id: string
    purchaseOrderId: string
    purchaseOrderCode: string
    purchaseOrderItemCode: string
    poStatus: number
    sellOrderItemId?: string | null
    pn?: string | null
    brand?: string | null
    qty: number
    cost: number
    currency: number
    itemStatus: number
    vendorName?: string | null
    purchaseUserName?: string | null
    createTime: string
  }>
  qcs: QcInfoDto[]
  stockIns: StockInListItemDto[]
  stockItems: Array<{
    id: string
    stockItemCode?: string | null
    stockAggregateId: string
    stockInCode?: string | null
    stockInDate?: string | null
    warehouseName?: string | null
    regionType?: number
    /** 1=客单 2=备货 3=样品 */
    stockType?: number
    /** 同 PN+品牌备货池匹配（非本销售行强绑定） */
    isStockingPoolMatch?: boolean
    purchasePn?: string | null
    purchaseBrand?: string | null
    stockOutStatus?: number
    qtyInbound?: number
    qtyStockOut?: number
    qtyRepertory: number
    qtyRepertoryAvailable: number
    sellOrderItemId?: string | null
    sellOrderItemCode?: string | null
    warehouseId: string
    purchaseOrderItemCode?: string | null
    batchNo?: string | null
    locationId?: string | null
  }>
  /** 销售明细关联装箱单（字段与 /inventory/packing 列表一致） */
  packings: PackingListItem[]
  stockOutRequests: StockOutRequestDto[]
  stockOuts: StockOutDto[]
  receiptWriteOffs: Array<{
    id: string
    amount: number
    writeOffSource: number
    createTime?: string | null
    financeReceiptId?: string | null
    financeReceiptCode?: string | null
    financeReceivableId: string
    receivableCode?: string | null
    stockOutId?: string | null
    stockOutCode?: string | null
    sellOrderId?: string | null
    sellOrderCode?: string | null
    customerName?: string | null
    customerEnglishName?: string | null
    pn?: string | null
    brand?: string | null
    currency: number
    operatorUserName?: string | null
    remark?: string | null
  }>
  sellInvoices: Array<{
    id: string
    invoiceCode?: string | null
    invoiceNo?: string | null
    customerName?: string | null
    invoiceTotal: number
    makeInvoiceDate?: string | null
    invoiceStatus: number
    receiveDone: number
    receiveToBe: number
    currency: number
    createTime: string
  }>
  /** 销售明细关联质检单上的图片文档（只读展示） */
  qcImages: QcImageReadonlyRow[]
  /** 销售订单明细详情「概况」页签（仅单条明细 aggregates 接口返回） */
  lineOverview?: SellOrderLineOverview | null
  /** 使用备货（仅单条明细 aggregates 接口返回；按采购主单汇总备货补充拣货量） */
  stockingUsage?: SellOrderStockingUsage | null
}

export interface SellOrderLineProfitLayer {
  /** 无可用成本来源时为 null，界面显示「-」 */
  profitUsd?: number | null
  profitRate?: number | null
}

export interface SellOrderLinePoCostLine {
  purchaseOrderItemId?: string | null
  purchaseOrderItemCode?: string | null
  convertPriceUsd: number
  qty: number
  costUsd: number
}

export interface SellOrderLineOutboundCostLine {
  purchaseOrderItemId?: string | null
  purchaseOrderItemCode?: string | null
  purchasePriceUsd: number
  qty: number
  costUsd: number
  profitOutBizUsd: number
}

/** line-profit：出库成本明细表（每条 stock_out_item_extend，含出库单维度） */
export interface SellOrderLineOutboundCostDetail {
  stockOutId?: string | null
  stockOutCode?: string | null
  stockOutItemId?: string | null
  purchaseOrderItemId?: string | null
  purchaseOrderItemCode?: string | null
  purchasePriceUsd: number
  qty: number
  costUsd: number
}

export type SellOrderSalesExpectedCostSource = 'po' | 'stocking' | 'quote' | 'none'

/** GET .../sell-order-items/{itemId}/line-profit */
export interface SellOrderLineProfit {
  qty: number
  sellPrice: number
  sellCurrency: number
  convertPrice: number
  quoteCost: number
  quoteCurrency: number
  quoteConvertCost: number
  fxUsdToCnySnapshot: number
  fxUsdToHkdSnapshot: number
  fxUsdToEurSnapshot: number
  useReQuote: boolean
  revenueUsd: number
  quoteCostUsd: number
  poCostUsdTotal: number
  poCostUsdConfirmed: number
  purchaseProfitExpected: number
  qtyStockOutActual: number
  poQtyTotal: number
  avgPoCostUsd: number
  poCostLines?: SellOrderLinePoCostLine[]
  useActualOutboundCost?: boolean
  effectiveOutboundAvgCostUsd?: number
  outboundCostLines?: SellOrderLineOutboundCostLine[]
  outboundCostDetails?: SellOrderLineOutboundCostDetail[]
  outboundRevenueUsd: number
  outboundCostUsd: number
  purchaseProgressStatus?: number
  stockOutProgressStatus?: number
  /** 预计销售成本来源：po | stocking | quote | none */
  salesExpectedCostSource?: SellOrderSalesExpectedCostSource
  /** 预计销售所用成本基数 USD */
  salesExpectedCostUsd?: number
  quote: SellOrderLineProfitLayer
  salesExpected: SellOrderLineProfitLayer
  outbound: SellOrderLineProfitLayer
}

export interface SellOrderStockingUsageItem {
  purchaseOrderId: string
  purchaseOrderCode: string
  purchaseOrderCreateTime?: string | null
  purchaseUserName?: string | null
  usedQty: number
}

export interface SellOrderStockingUsage {
  totalUsedQty: number
  items: SellOrderStockingUsageItem[]
}

export interface SellOrderLineOverviewQtyMetric {
  total: number
  done?: number
  pending?: number
}

export interface SellOrderLineOverviewAmountMetric {
  total: number
  done?: number
  pending?: number
  currency?: number
}

export interface SellOrderLineOverview {
  lineAmount: { total: number; currency?: number }
  lineQty: { total: number }
  purchaseRequisition: SellOrderLineOverviewQtyMetric
  purchaseOrder: SellOrderLineOverviewQtyMetric
  stockIn: SellOrderLineOverviewQtyMetric
  stockOutNotify: SellOrderLineOverviewQtyMetric
  stockOut: SellOrderLineOverviewQtyMetric
  receiptWriteOff: SellOrderLineOverviewAmountMetric
  invoice: SellOrderLineOverviewAmountMetric
}

/** 销售订单明细详情 / 参考面板「库存」Tab 行 */
export type SellOrderItemStockTabRow = SalesOrderDetailTabAggregates['stockItems'][number]
export type SellOrderItemStockInTabRow = SalesOrderDetailTabAggregates['stockIns'][number]
export type SellOrderItemQcTabRow = SalesOrderDetailTabAggregates['qcs'][number]
export type SellOrderItemPackingTabRow = SalesOrderDetailTabAggregates['packings'][number]
export type SellOrderItemStockOutNotifyTabRow = SalesOrderDetailTabAggregates['stockOutRequests'][number]
export type SellOrderItemStockOutTabRow = SalesOrderDetailTabAggregates['stockOuts'][number]

// 销售订单API
export interface SalesOrderBatchExportLogRow {
  id: string
  operationTime: string
  operatorUserName?: string | null
  exportedCount?: number | null
  operationDesc?: string | null
  filterSummary?: string | null
}

export type SalesOrderBatchExportLogPaged = {
  items: SalesOrderBatchExportLogRow[]
  total: number
  page: number
  pageSize: number
}

function parseSoBatchExportLogExtra(raw: string | null | undefined): Partial<SalesOrderBatchExportLogRow> {
  if (!raw?.trim()) return {}
  try {
    const o = JSON.parse(raw) as Record<string, unknown>
    return {
      exportedCount: o.exportedCount != null ? Number(o.exportedCount) : o.affectedCount != null ? Number(o.affectedCount) : null,
      filterSummary: (o.filterSummary as string) ?? null
    }
  } catch {
    return {}
  }
}

export const salesOrderApi = {
  // 获取销售订单列表
  async getList(params?: Record<string, unknown>) {
    const q = buildQueryString((params ?? {}) as Record<string, unknown>)
    return await apiClient.get(`/api/v1/sales-orders${q ? `?${q}` : ''}`)
  },

  /** 销售订单明细分页（GET /api/v1/sales-orders/items） */
  async getItemLines(params?: {
    orderCreateStart?: string
    orderCreateEnd?: string
    customerName?: string
    salesUserName?: string
    salesUserId?: string
    purchaseUserAccount?: string
    customerId?: string
    stockOutPending?: boolean | string
    invoicePending?: boolean | string
    /** 销售订单号（模糊） */
    sellOrderCode?: string
    pn?: string
    customerSo?: string
    customerPn?: string
    /** 交易币别：rmb=人民币，foreign=外币 */
    transactionCurrency?: 'rmb' | 'foreign' | ''
    purchaseProgressStatus?: number | number[]
    stockInProgressStatus?: number | number[]
    stockOutNotifyProgressStatus?: number | number[]
    stockOutProgressStatus?: number | number[]
    receiptProgressStatus?: number | number[]
    invoiceProgressStatus?: number | number[]
    /** 左栏快捷检索（与 preset 对应；与六 progress 互斥） */
    quickFilter?: string
    page?: number
    pageSize?: number
  }) {
    const q = buildQueryString((params ?? {}) as Record<string, unknown>)
    return await apiClient.get(`/api/v1/sales-orders/items?${q}`)
  },

  // 获取销售订单详情
  async getById(id: string) {
    return await apiClient.get(`/api/v1/sales-orders/${id}`)
  },

  /** 销售订单主表字段变更日志 */
  async getChangeLogs(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<SalesOrderFieldChangeLogRow[]>(`/api/v1/sales-orders/${enc}/change-logs`)
  },

  /** 已软删除的销售订单明细 */
  async getDeletedItems(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<SalesOrderDeletedItemRow[]>(`/api/v1/sales-orders/${enc}/deleted-items`)
  },

  /** 详情页：采购申请/采购订单明细/入库/库存/出库通知/出库/收款/销项发票等列表 */
  async getDetailTabAggregates(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<SalesOrderDetailTabAggregates>(`/api/v1/sales-orders/${enc}/detail-tab-aggregates`)
  },

  /** 单条销售明细：同上结构，按销售明细主键过滤 */
  async getSellOrderItemDetailTabAggregates(orderId: string, sellOrderItemId: string) {
    const encO = encodeURIComponent(orderId)
    const encI = encodeURIComponent(sellOrderItemId)
    return await apiClient.get<SalesOrderDetailTabAggregates>(
      `/api/v1/sales-orders/${encO}/sell-order-items/${encI}/detail-tab-aggregates`
    )
  },

  /** 单条销售明细绩效（展开绩效面板时按需加载） */
  async getSellOrderItemLineProfit(orderId: string, sellOrderItemId: string) {
    const encO = encodeURIComponent(orderId)
    const encI = encodeURIComponent(sellOrderItemId)
    return await apiClient.get<SellOrderLineProfit | null>(
      `/api/v1/sales-orders/${encO}/sell-order-items/${encI}/line-profit`
    )
  },

  /**
   * 报表页：订单 + 公司参数。
   * 优先单请求 report-data；若后端尚未部署该路由（404），降级为详情 + company-profile/report-bundle。
   */
  async getReportData(id: string) {
    const enc = encodeURIComponent(id)
    try {
      return await apiClient.get(`/api/v1/sales-orders/${enc}/report-data`)
    } catch (e: unknown) {
      if (httpStatusFromApiError(e) !== 404) throw e
      const order = await apiClient.get(`/api/v1/sales-orders/${enc}`)
      let companyProfile: CompanyProfileBundle
      try {
        companyProfile = await fetchCompanyProfileForReport()
      } catch {
        companyProfile = {
          basicInfos: [],
          bankInfos: [],
          warehouses: [],
          seals: [],
          logos: []
        }
      }
      return { order, companyProfile }
    }
  },

  // 根据客户获取销售订单
  async getByCustomer(customerId: string) {
    return await apiClient.get(`/api/v1/sales-orders/by-customer/${customerId}`)
  },

  // 创建销售订单
  async create(data: any) {
    return await apiClient.post('/api/v1/sales-orders', data)
  },

  // 更新销售订单
  async update(id: string, data: any) {
    return await apiClient.put(`/api/v1/sales-orders/${id}`, data)
  },

  // 删除销售订单
  async delete(id: string) {
    return await apiClient.delete(`/api/v1/sales-orders/${id}`)
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    return await apiClient.patch(`/api/v1/sales-orders/${id}/status`, { status })
  },

  // 获取关联采购订单
  async getRelatedPurchaseOrders(id: string) {
    return await apiClient.get(`/api/v1/sales-orders/${id}/purchase-orders`)
  },

  // 刷新销售订单明细扩展字段（读取下游数据重算）
  async refreshItemExtends(id: string) {
    return await apiClient.post<SalesOrderItemExtendRefreshResult>(`/api/v1/sales-orders/${id}/refresh-item-extends`, {})
  },

  async previewSyncDownstreamCustomer(id: string, proposedCustomerId?: string) {
    const q =
      proposedCustomerId && proposedCustomerId.trim()
        ? `?proposedCustomerId=${encodeURIComponent(proposedCustomerId.trim())}`
        : ''
    return await apiClient.get<SalesOrderCustomerDownstreamSyncPreview>(
      `/api/v1/sales-orders/${id}/sync-downstream-customer/preview${q}`
    )
  },

  async syncDownstreamCustomer(id: string) {
    return await apiClient.post<SalesOrderCustomerDownstreamSyncApplyResult>(
      `/api/v1/sales-orders/${id}/sync-downstream-customer`,
      {}
    )
  },

  async logBatchExport(salesOrderId: string, exportedCount: number): Promise<void> {
    const enc = encodeURIComponent(salesOrderId)
    await apiClient.post(`/api/v1/sales-orders/${enc}/batch-log-export`, { exportedCount })
  },

  async getBatchExportLogs(
    salesOrderId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<SalesOrderBatchExportLogPaged> {
    const enc = encodeURIComponent(salesOrderId)
    const res = await apiClient.get<any>(`/api/v1/sales-orders/${enc}/batch-export-logs`, { params })
    const d = res?.data ?? res
    const items = Array.isArray(d?.items) ? d.items : []
    return {
      items: items.map((row: Record<string, unknown>) => {
        const extra = parseSoBatchExportLogExtra(row.extraInfo as string | null | undefined)
        return {
          id: String(row.id ?? ''),
          operationTime: String(row.operationTime ?? ''),
          operatorUserName: (row.operatorUserName as string) ?? null,
          operationDesc: (row.operationDesc as string) ?? null,
          ...extra
        }
      }),
      total: Number(d?.total ?? 0),
      page: Number(d?.page ?? 1),
      pageSize: Number(d?.pageSize ?? 20)
    }
  }
}

export default salesOrderApi
