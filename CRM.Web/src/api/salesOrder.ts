import apiClient, { type ApiRejectedError } from './client'
import { fetchCompanyProfileForReport, type CompanyProfileBundle } from './companyProfile'

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

/** GET /api/v1/sales-orders/{id}/detail-tab-aggregates */
export interface SalesOrderDetailTabAggregates {
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
    purchasePn?: string | null
    purchaseBrand?: string | null
    qtyRepertory: number
    qtyRepertoryAvailable: number
    sellOrderItemId?: string | null
    sellOrderItemCode?: string | null
    warehouseId: string
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
}

// 销售订单API
export const salesOrderApi = {
  // 获取销售订单列表
  async getList(params?: Record<string, unknown>) {
    return await apiClient.get('/api/v1/sales-orders', { params })
  },

  /** 销售订单明细分页 */
  async getItemLines(params?: {
    orderCreateStart?: string
    orderCreateEnd?: string
    customerName?: string
    salesUserName?: string
    /** 销售订单号（模糊） */
    sellOrderCode?: string
    pn?: string
    page?: number
    pageSize?: number
  }) {
    return await apiClient.get('/api/v1/sales-orders/lines', { params })
  },

  // 获取销售订单详情
  async getById(id: string) {
    return await apiClient.get(`/api/v1/sales-orders/${id}`)
  },

  /** 详情页：采购申请/入库/库存/出库通知/出库/收款/销项发票等列表 */
  async getDetailTabAggregates(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<SalesOrderDetailTabAggregates>(`/api/v1/sales-orders/${enc}/detail-tab-aggregates`)
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
