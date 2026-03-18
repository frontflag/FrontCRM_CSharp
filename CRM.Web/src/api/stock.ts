import apiClient from './client'

export interface StockInfo {
  id: string
  materialId: string
  warehouseId: string
  locationId?: string
  quantity: number
  availableQuantity: number
  lockedQuantity: number
  unit?: string
  batchNo?: string
  productionDate?: string
  expiryDate?: string
  status: number
  remark?: string
}

export const stockApi = {
  async getList(warehouseId?: string): Promise<StockInfo[]> {
    const url = warehouseId ? `/api/v1/stock?warehouseId=${encodeURIComponent(warehouseId)}` : '/api/v1/stock'
    const res = await apiClient.get<any>(url)
    if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
      return res.data as StockInfo[]
    return Array.isArray(res) ? res : []
  }
}
