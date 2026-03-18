import apiClient from './client'

export interface StockOutDto {
  id: string
  stockOutCode: string
  stockOutType: number
  sourceCode?: string
  warehouseId: string
  stockOutDate: string
  totalQuantity: number
  totalAmount: number
  status: number
  remark?: string
  createTime?: string
}

export interface StockOutRequestDto {
  id: string
  requestCode: string
  salesOrderId: string
  customerId: string
  requestUserId: string
  requestDate: string
  status: number
  remark?: string
}

export const stockOutApi = {
  async getAll(): Promise<StockOutDto[]> {
    const res = await apiClient.get<any>('/api/v1/stock-out')
    if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
      return res.data as StockOutDto[]
    return Array.isArray(res) ? res : []
  },

  async getById(id: string): Promise<StockOutDto | null> {
    const res = await apiClient.get<any>(`/api/v1/stock-out/${id}`)
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as StockOutDto
    return res as StockOutDto ?? null
  },

  async execute(data: {
    stockOutRequestId: string
    stockOutCode: string
    warehouseId: string
    operatorId?: string
    stockOutDate: string
    remark?: string
    items: { lineNo: number; materialCode: string; materialName: string; quantity: number; batchNo?: string; warehouseLocation?: string }[]
  }): Promise<StockOutDto> {
    const res = await apiClient.post<any>('/api/v1/stock-out/execute', data)
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as StockOutDto
    return res as StockOutDto
  },

  async updateStatus(id: string, status: number): Promise<void> {
    await apiClient.patch(`/api/v1/stock-out/${id}/status?status=${status}`)
  }
}
