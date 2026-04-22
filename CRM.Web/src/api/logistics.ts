import apiClient from './client'

export interface StockInNotifyItemDto {
  id: string
  stockInNotifyId: string
  purchaseOrderItemId: string
  pn?: string
  brand?: string
  qty: number
  arrivedQty: number
  passedQty: number
}

export interface StockInNotifyDto {
  id: string
  noticeCode: string
  purchaseOrderId: string
  purchaseOrderCode: string
  purchaseOrderItemId?: string
  sellOrderItemId?: string | null
  vendorId?: string
  vendorName?: string
  /** 供应商编号（接口从采购单关联填充） */
  vendorCode?: string | null
  purchaseUserName?: string
  status: number
  /** 预计到货日期 */
  expectedArrivalDate?: string | null
  /** RegionType：10=境内 20=境外（与仓库档案共用） */
  regionType?: number
  pn?: string | null
  brand?: string | null
  expectQty?: number
  receiveQty?: number
  passedQty?: number
  createTime: string
  modifyTime?: string
  items: StockInNotifyItemDto[]
}

export interface QcInfoDto {
  id: string
  qcCode: string
  stockInNotifyId: string
  stockInNotifyCode: string
  vendorName?: string
  purchaseOrderCode?: string
  salesOrderCode?: string
  model?: string
  brand?: string
  status: number
  stockInStatus: number
  passQty: number
  rejectQty: number
  stockInId?: string
  /** 质检保存的计划入库日（ISO）；生成入库单时优先使用 */
  stockInPlanDate?: string | null
  /** 部分接口仍返回 PascalCase */
  StockInPlanDate?: string | null
  createTime: string
  modifyTime?: string
  createByUserId?: string | null
  /** 列表由后端根据 createByUserId 解析 */
  createUserName?: string | null
  /** 个别序列化配置可能保留 PascalCase */
  CreateUserName?: string | null
}

const unwrap = <T>(res: any): T => (res?.data ?? res) as T

export const logisticsApi = {
  async getArrivalNotices(params?: {
    status?: number
    purchaseOrderCode?: string
    expectedArrivalDate?: string
  }): Promise<StockInNotifyDto[]> {
    return unwrap<StockInNotifyDto[]>(await apiClient.get('/api/v1/logistics/arrival-notices', { params }))
  },
  async createArrivalNotice(payload: {
    purchaseOrderItemId: string
    expectQty: number
    purchaseOrderId?: string
    expectedArrivalDate?: string | null
    regionType?: number
  }): Promise<StockInNotifyDto> {
    return unwrap<StockInNotifyDto>(await apiClient.post('/api/v1/logistics/arrival-notices', payload))
  },
  async updateArrivalStatus(id: string, status: number): Promise<void> {
    await apiClient.patch(`/api/v1/logistics/arrival-notices/${id}/status?status=${status}`)
  },
  async getQcs(params?: {
    model?: string
    vendorName?: string
    purchaseOrderCode?: string
    salesOrderCode?: string
  }): Promise<QcInfoDto[]> {
    const res = await apiClient.get<any>('/api/v1/logistics/qcs', { params })
    const payload = res?.data ?? res
    return Array.isArray(payload) ? (payload as QcInfoDto[]) : []
  },
  async createQc(stockInNotifyId: string): Promise<QcInfoDto> {
    return unwrap<QcInfoDto>(await apiClient.post('/api/v1/logistics/qcs', { stockInNotifyId }))
  },
  async updateQcResult(
    id: string,
    payload: {
      result: 'pass' | 'partial' | 'reject'
      passQty: number
      rejectQty: number
      /** 为 true 时写入 stockInPlanDate（含 null 清空）；旧客户端不传则不修改 */
      hasStockInPlanDate?: boolean
      stockInPlanDate?: string | null
    }
  ): Promise<QcInfoDto> {
    return unwrap<QcInfoDto>(await apiClient.patch(`/api/v1/logistics/qcs/${id}/result`, payload))
  },
  async bindQcStockIn(id: string, stockInId: string): Promise<void> {
    await apiClient.patch(`/api/v1/logistics/qcs/${id}/bind-stock-in?stockInId=${encodeURIComponent(stockInId)}`)
  }
}
