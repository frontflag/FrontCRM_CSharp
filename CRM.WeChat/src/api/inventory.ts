/**
 * 库存 API - 微信端（概览 + 搜索）
 */
import apiClient from './client'

export interface InventoryOverview {
  stockId: string
  stockCode?: string
  materialId: string
  materialModel?: string
  materialName?: string
  warehouseId: string
  warehouseCode?: string
  onHandQty: number
  availableQty: number
  lockedQty: number
  lastMoveTime?: string
}

export interface InventoryQuery {
  keyword?: string
  warehouseId?: string
  page?: number
  pageSize?: number
}

export interface PickingTaskListItem {
  id: string
  taskCode: string
  warehouseDisplay: string
  materialModel: string
  brand: string
  customerName: string
  status: number
  planQtyTotal: number
  lineCount: number
  createTime: string
}

export const inventoryApi = {
  /** 库存概览 */
  getOverview(query: InventoryQuery): Promise<{ items: InventoryOverview[]; total: number }> {
    return apiClient.get('/api/v1/inventory-center/overview/paged', query as Record<string, any>)
  },

  /** 拣货单列表 */
  getPickingList(query: { page?: number; pageSize?: number }): Promise<{ items: PickingTaskListItem[]; total: number }> {
    return apiClient.get('/api/v1/inventory-center/picking-list', query as Record<string, any>)
  },

  /** 拣货单详情 */
  getPickingDetail(id: string): Promise<any> {
    return apiClient.get(`/api/v1/inventory-center/picking-list/${id}`)
  },
}
