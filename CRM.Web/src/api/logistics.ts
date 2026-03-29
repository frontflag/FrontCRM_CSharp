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
  createTime: string
  modifyTime?: string
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
    return unwrap<QcInfoDto[]>(await apiClient.get('/api/v1/logistics/qcs', { params }))
  },
  async createQc(stockInNotifyId: string): Promise<QcInfoDto> {
    return unwrap<QcInfoDto>(await apiClient.post('/api/v1/logistics/qcs', { stockInNotifyId }))
  },
  async updateQcResult(id: string, payload: { result: 'pass' | 'partial' | 'reject'; passQty: number; rejectQty: number }): Promise<QcInfoDto> {
    return unwrap<QcInfoDto>(await apiClient.patch(`/api/v1/logistics/qcs/${id}/result`, payload))
  },
  async bindQcStockIn(id: string, stockInId: string): Promise<void> {
    await apiClient.patch(`/api/v1/logistics/qcs/${id}/bind-stock-in?stockInId=${encodeURIComponent(stockInId)}`)
  }
}
