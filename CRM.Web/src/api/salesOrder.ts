import apiClient, { type ApiRejectedError } from './client'
import { fetchCompanyProfileForReport, type CompanyProfileBundle } from './companyProfile'
import type { QcImageReadonlyRow } from './document'

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

export interface SalesOrderFieldChangeLogRow {
  id: string
  sellOrderId: string
  sellOrderCode?: string | null
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
  stockIns: Array<{
    id: string
    stockInCode: string
    stockInType: number
    status: number
    stockInDate: string
    createTime: string
  }>
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
  stockOutRequests: Array<{
    id: string
    requestCode: string
    materialCode: string
    quantity: number
    status: number
    requestDate: string
    createTime: string
  }>
  stockOuts: Array<{
    id: string
    stockOutCode: string
    status: number
    totalQuantity: number
    stockOutDate: string
    sellOrderItemId?: string | null
    createTime: string
  }>
  receipts: Array<{
    id: string
    financeReceiptCode: string
    status: number
    customerName?: string | null
    receiptAmount: number
    receiptCurrency: number
    receiptDate?: string | null
    createTime: string
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
}

/** 销售订单明细详情 / 参考面板「库存」Tab 行 */
export type SellOrderItemStockTabRow = SalesOrderDetailTabAggregates['stockItems'][number]

// 销售订单API
export const salesOrderApi = {
  // 获取销售订单列表
  async getList(params?: Record<string, unknown>) {
    return await apiClient.get('/api/v1/sales-orders', { params })
  },

  /** 销售订单明细分页（GET /api/v1/sales-orders/items） */
  async getItemLines(params?: {
    orderCreateStart?: string
    orderCreateEnd?: string
    customerName?: string
    salesUserName?: string
    salesUserId?: string
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
    purchaseProgressStatus?: number
    stockInProgressStatus?: number
    stockOutNotifyProgressStatus?: number
    stockOutProgressStatus?: number
    receiptProgressStatus?: number
    invoiceProgressStatus?: number
    page?: number
    pageSize?: number
  }) {
    return await apiClient.get('/api/v1/sales-orders/items', { params })
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
  }
}

export default salesOrderApi
