import apiClient from './client'

export interface PurchaseOrderItemExtendFieldChangeDto {
  field: string
  label: string
  before: string
  after: string
}

export interface PurchaseOrderItemExtendChangeDto {
  purchaseOrderItemId: string
  purchaseOrderItemCode?: string
  fields: PurchaseOrderItemExtendFieldChangeDto[]
}

export interface PurchaseOrderItemExtendRefreshResult {
  purchaseOrderId: string
  totalItems: number
  changedItems: number
  changedFieldsCount: number
  syncedPurchaseRequisitionStatusCount?: number
  syncedArrivalNoticeStatusCount?: number
  refreshedAt: string
  changes: PurchaseOrderItemExtendChangeDto[]
}

export interface PurchaseOrderDetailTabAggregates {
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
  payments: Array<{
    id: string
    financePaymentCode: string
    vendorName?: string | null
    status: number
    paymentAmountToBe: number
    paymentAmount: number
    paymentCurrency: number
    paymentDate?: string | null
    createTime: string
  }>
  arrivalNotices: Array<{
    id: string
    noticeCode: string
    pn?: string | null
    brand?: string | null
    expectQty: number
    receiveQty: number
    status: number
    expectedArrivalDate?: string | null
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
    purchaseOrderItemId?: string | null
    purchaseOrderItemCode?: string | null
  }>
  purchaseInvoices: Array<{
    id: string
    vendorName?: string | null
    invoiceNo?: string | null
    invoiceAmount: number
    invoiceDate?: string | null
    confirmStatus: number
    redInvoiceStatus: number
    createTime: string
  }>
}

// 采购订单API
export const purchaseOrderApi = {
  // 获取采购订单列表（分页，与后端 PurchaseOrdersController 一致）
  async getList(params?: {
    /** 采购单号/供应商名称模糊 */
    keyword?: string
    status?: number
    startDate?: string
    endDate?: string
    page?: number
    pageSize?: number
  }) {
    return await apiClient.get('/api/v1/purchase-orders', { params })
  },

  // 获取采购订单详情
  async getById(id: string) {
    return await apiClient.get(`/api/v1/purchase-orders/${id}`)
  },

  async getDetailTabAggregates(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<PurchaseOrderDetailTabAggregates>(`/api/v1/purchase-orders/${enc}/detail-tab-aggregates`)
  },

  /** 报表页：订单 + 公司参数（单请求，不依赖 company-profile/report-bundle） */
  async getReportData(id: string) {
    return await apiClient.get(`/api/v1/purchase-orders/${id}/report-data`)
  },

  // 根据销售订单号获取采购订单
  async getBySellOrder(sellOrderCode: string) {
    return await apiClient.get(`/api/v1/purchase-orders/by-sell-order/${sellOrderCode}`)
  },

  // 创建采购订单
  async create(data: any) {
    return await apiClient.post('/api/v1/purchase-orders', data)
  },

  // 更新采购订单
  async update(id: string, data: any) {
    return await apiClient.put(`/api/v1/purchase-orders/${id}`, data)
  },

  // 删除采购订单
  async delete(id: string) {
    return await apiClient.delete(`/api/v1/purchase-orders/${id}`)
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    return await apiClient.patch(`/api/v1/purchase-orders/${id}/status`, { status })
  },

  // 自动生成采购订单(以销定采)
  async autoGenerate(sellOrderId: string) {
    return await apiClient.post(`/api/v1/purchase-orders/auto-generate/${sellOrderId}`, {})
  },

  // 刷新采购明细扩展字段（读取下游数据重算）
  async refreshItemExtends(id: string) {
    return await apiClient.post<PurchaseOrderItemExtendRefreshResult>(`/api/v1/purchase-orders/${id}/refresh-item-extends`, {})
  }
}

export default purchaseOrderApi
