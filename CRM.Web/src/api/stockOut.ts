import apiClient from './client'

/** 兼容 axios 拦截器已解包 / 未解包、以及 data / Data */
function unwrapArray<T>(res: unknown): T[] {
  if (Array.isArray(res)) return res as T[]
  if (res && typeof res === 'object') {
    const o = res as Record<string, unknown>
    const inner = o.data ?? o.Data
    if (Array.isArray(inner)) return inner as T[]
  }
  return []
}

export interface StockOutDto {
  id: string
  stockOutCode: string
  stockOutType: number
  sourceCode?: string
  /** 列表接口不再返回；详情等仍可能返回 */
  warehouseId?: string
  stockOutDate: string
  totalQuantity: number
  totalAmount: number
  status: number
  remark?: string
  createTime?: string
  createUserName?: string
  customerName?: string
  salesUserName?: string
  sellOrderItemCode?: string
  /** 出货方式（字典 LogisticsArrivalMethod ItemCode） */
  shipmentMethod?: string | null
  courierTrackingNo?: string | null
}

/** GET /api/v1/stock-out/:id（详情视图，含仓库与明细主键） */
export interface StockOutDetailDto extends StockOutDto {
  warehouseId?: string
  /** 仓库编号（服务端由 WarehouseId 解析） */
  warehouseCode?: string | null
  sellOrderItemId?: string
}

/** GET /api/v1/stock-out/request/apply-context */
export interface StockOutApplyContextDto {
  salesOrderItemId: string
  salesOrderQty: number
  alreadyNotifiedQty: number
  remainingNotifyQty: number
  availableStockQty: number
  suggestedMaxQty: number
}

export interface StockOutRequestDto {
  id: string
  requestCode: string
  salesOrderId: string
  /** 销售订单明细主键 */
  salesOrderItemId?: string
  salesOrderCode?: string
  materialModel?: string
  brand?: string
  outQuantity: number
  expectedStockOutDate?: string
  salesUserName?: string
  customerId: string
  customerName?: string
  requestUserId: string
  requestUserName?: string
  requestDate: string
  status: number
  remark?: string
  /** 出货方式（字典 LogisticsArrivalMethod ItemCode） */
  shipmentMethod?: string | null
  /** RegionType：10=境内 20=境外（与仓库、到货通知共用） */
  regionType?: number
  createTime?: string
}

export const stockOutApi = {
  async getAll(): Promise<StockOutDto[]> {
    const res = await apiClient.get<unknown>('/api/v1/stock-out')
    return unwrapArray<StockOutDto>(res)
  },

  async getById(id: string): Promise<StockOutDetailDto | null> {
    const res = await apiClient.get<unknown>(`/api/v1/stock-out/${id}`)
    if (res && typeof res === 'object') {
      const o = res as Record<string, unknown>
      const inner = o.data ?? o.Data
      if (inner && typeof inner === 'object') return inner as StockOutDetailDto
    }
    return (res as StockOutDetailDto) ?? null
  },

  async updateHeader(
    id: string,
    body: { stockOutDate: string; shipmentMethod?: string | null; courierTrackingNo?: string | null }
  ): Promise<void> {
    await apiClient.patch(`/api/v1/stock-out/${id}/header`, body)
  },

  async getRequestList(): Promise<StockOutRequestDto[]> {
    const res = await apiClient.get<unknown>('/api/v1/stock-out/request')
    return unwrapArray<StockOutRequestDto>(res)
  },

  async getApplyContext(salesOrderId: string, salesOrderItemId: string): Promise<StockOutApplyContextDto> {
    return apiClient.get<StockOutApplyContextDto>('/api/v1/stock-out/request/apply-context', {
      params: { salesOrderId, salesOrderItemId }
    })
  },

  async createRequest(data: {
    requestCode?: string
    salesOrderId: string
    salesOrderItemId: string
    materialCode: string
    materialName: string
    quantity: number
    customerId: string
    requestUserId: string
    requestDate: string
    remark?: string
    shipmentMethod?: string | null
    regionType?: number
  }): Promise<StockOutRequestDto> {
    // 去掉 Vue Proxy / 非枚举属性，保证 quantity 与网络载荷一致
    const body = JSON.parse(JSON.stringify(data)) as typeof data
    const res = await apiClient.post<any>('/api/v1/stock-out/request', body)
    if (res && typeof res === 'object' && 'data' in res && res.data)
      return res.data as StockOutRequestDto
    return res as StockOutRequestDto
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
