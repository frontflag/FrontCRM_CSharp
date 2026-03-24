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

  delete(id: string) {
    return apiClient.delete(`/api/v1/purchase-requisitions/${id}`)
  },

  autoGenerate(sellOrderId: string) {
    return apiClient.post(`/api/v1/purchase-requisitions/auto-generate/${sellOrderId}`, {})
  },

  recalculate(id: string) {
    return apiClient.post(`/api/v1/purchase-requisitions/${id}/recalculate`, {})
  }
}

export default purchaseRequisitionApi
