import apiClient from './client'

export interface StockInItemDto {
  lineNo: number
  materialCode: string
  materialName: string
  specification?: string
  quantity: number
  unit: string
  unitPrice?: number
  batchNo?: string
  warehouseLocation?: string
}

export interface CreateStockInRequest {
  stockInCode: string
  purchaseOrderId?: string
  vendorId?: string
  warehouseId: string
  operatorId?: string
  stockInDate: string
  totalQuantity: number
  remark?: string
  items?: StockInItemDto[]
}

export interface StockInDto {
  id: string
  stockInCode: string
  stockInType: number
  sourceCode?: string
  warehouseId: string
  vendorId?: string
  stockInDate: string
  totalQuantity: number
  totalAmount: number
  status: number
  remark?: string
  createTime?: string
}

/** 列表接口返回：含来源单号、供应商名称等展示字段 */
export interface StockInListItemDto {
  id: string
  stockInCode: string
  stockInType: number
  sourceDisplayNo?: string
  warehouseId: string
  vendorId?: string
  vendorName?: string
  stockInDate: string
  totalQuantity: number
  totalAmount: number
  status: number
  remark?: string
  createTime?: string
}

export const stockInApi = {
  async getAll(): Promise<StockInListItemDto[]> {
    const res = await apiClient.get<any>('/api/v1/stock-in')
    if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
      return res.data as StockInListItemDto[]
    return Array.isArray(res) ? res : []
  },

  async getById(id: string): Promise<StockInDto | null> {
    const res = await apiClient.get<any>(`/api/v1/stock-in/${id}`)
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as StockInDto
    return res as StockInDto ?? null
  },

  async create(data: CreateStockInRequest): Promise<StockInDto> {
    const payload = {
      stockInCode: data.stockInCode,
      purchaseOrderId: data.purchaseOrderId,
      vendorId: data.vendorId,
      warehouseId: data.warehouseId,
      operatorId: data.operatorId ?? '',
      stockInDate: data.stockInDate,
      totalQuantity: data.totalQuantity,
      remark: data.remark,
      items: data.items ?? []
    }
    const res = await apiClient.post<any>('/api/v1/stock-in', payload)
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as StockInDto
    return res as StockInDto
  },

  async update(id: string, data: { remark?: string }): Promise<StockInDto> {
    const res = await apiClient.put<any>(`/api/v1/stock-in/${id}`, data)
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as StockInDto
    return res as StockInDto
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-in/${id}`)
  },

  async updateStatus(id: string, status: number): Promise<void> {
    await apiClient.patch(`/api/v1/stock-in/${id}/status?status=${status}`)
  }
}
