import apiClient from './client'

// 采购订单API
export const purchaseOrderApi = {
  // 获取采购订单列表
  async getList() {
    return await apiClient.get('/api/v1/purchase-orders')
  },

  // 获取采购订单详情
  async getById(id: string) {
    return await apiClient.get(`/api/v1/purchase-orders/${id}`)
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
  }
}

export default purchaseOrderApi
