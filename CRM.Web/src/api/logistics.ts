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

/** 与《翻页查询规范》一致：<code>data.items</code> / <code>data.total</code> / <code>data.page</code> / <code>data.pageSize</code> */
export type ListPaged<T> = { items: T[]; total: number; page: number; pageSize: number }

function unwrapListPaged<T>(res: any): ListPaged<T> {
  const d = res?.data ?? res
  if (d && typeof d === 'object' && Array.isArray(d.items)) {
    return {
      items: d.items as T[],
      total: Number(d.total ?? 0),
      page: Number(d.page ?? 1),
      pageSize: Number(d.pageSize ?? 20)
    }
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

export const logisticsApi = {
  async getArrivalNotices(params?: {
    status?: number
    purchaseOrderCode?: string
    expectedArrivalDate?: string
    /** 按到货通知主键精确查（编辑/联动场景） */
    id?: string
    page?: number
    pageSize?: number
  }): Promise<ListPaged<StockInNotifyDto>> {
    const res = await apiClient.get<any>('/api/v1/logistics/arrival-notices', { params })
    return unwrapListPaged<StockInNotifyDto>(res)
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
  async deleteArrivalNotice(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/logistics/arrival-notices/${encodeURIComponent(id)}`)
  },
  async forceDeleteArrivalNotice(id: string, confirmBillCode: string): Promise<void> {
    await apiClient.post(`/api/v1/logistics/arrival-notices/${encodeURIComponent(id)}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  },
  async getQcs(params?: {
    qcId?: string
    model?: string
    vendorName?: string
    purchaseOrderCode?: string
    salesOrderCode?: string
    page?: number
    pageSize?: number
  }): Promise<ListPaged<QcInfoDto>> {
    const res = await apiClient.get<any>('/api/v1/logistics/qcs', { params })
    return unwrapListPaged<QcInfoDto>(res)
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
  },
  async deleteQc(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/logistics/qcs/${encodeURIComponent(id)}`)
  },
  async forceDeleteQc(id: string, confirmBillCode: string): Promise<void> {
    await apiClient.post(`/api/v1/logistics/qcs/${encodeURIComponent(id)}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  }
}
