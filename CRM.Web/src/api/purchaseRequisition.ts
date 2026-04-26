import apiClient from './client'

export const purchaseRequisitionApi = {
  getList(params?: {
    keyword?: string
    sellOrderId?: string
    status?: number
    page?: number
    pageSize?: number
  }) {
    return apiClient.get('/api/v1/purchase-requisitions', { params })
  },

  getById(id: string) {
    return apiClient.get(`/api/v1/purchase-requisitions/${id}`)
  },

  /** 与申请同一销行（SellOrderItemId）的采购订单明细 */
  getPurchaseOrderItemsByRequisitionId(requisitionId: string) {
    return apiClient.get(`/api/v1/purchase-requisitions/${requisitionId}/purchase-order-items`)
  },

  getLineOptions(sellOrderId: string) {
    return apiClient.get(`/api/v1/purchase-requisitions/sell-order/${sellOrderId}/line-options`)
  },

  create(data: {
    sellOrderItemId: string
    qty?: number
    expectedPurchaseTime: string
    type: number
    purchaseUserId?: string
    remark?: string
    busComplianceStatus?: string
    countryOfOrigin?: string
    quoteBaseOrigin?: string
    quotePackageOrigin?: string
    quoteTotal?: number
    quoteVendorId?: string
  }) {
    return apiClient.post('/api/v1/purchase-requisitions', data)
  },

  update(id: string, data: Record<string, unknown>) {
    return apiClient.put(`/api/v1/purchase-requisitions/${id}`, data)
  },

  /** 普通删除（软删），与后端 POST .../soft-delete 一致 */
  softDelete(id: string) {
    return apiClient.post(`/api/v1/purchase-requisitions/${id}/soft-delete`, {})
  },

  /**
   * 强制删除（软删，仅 SYS_ADMIN）。须提交与单号完全一致的确认。
   */
  forceDelete(id: string, confirmBillCode: string) {
    return apiClient.post(`/api/v1/purchase-requisitions/${id}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  },

  autoGenerate(sellOrderId: string) {
    return apiClient.post(`/api/v1/purchase-requisitions/auto-generate/${sellOrderId}`, {})
  },

  recalculate(id: string) {
    return apiClient.post(`/api/v1/purchase-requisitions/${id}/recalculate`, {})
  }
}

export default purchaseRequisitionApi
