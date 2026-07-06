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
  freightForwarderOrderNo?: string | null
  purchaseOrderItemId?: string
  sellOrderItemId?: string | null
  vendorId?: string
  vendorName?: string
  vendorEnglishName?: string
  /** 供应商编号（接口从采购单关联填充） */
  vendorCode?: string | null
  purchaseUserName?: string
  status: number
  /** 预计到货日期 */
  expectedArrivalDate?: string | null
  /** RegionType：10=境内 20=境外（与仓库档案共用） */
  regionType?: number
  /** 到货类型（StockInType：10 采购 / 20 报关 / 30 退货 / 40 报废） */
  stockInType?: number
  pn?: string | null
  brand?: string | null
  expectQty?: number
  receiveQty?: number
  passedQty?: number
  remark?: string | null
  /** 预计到货方式（字典 LogisticsArrivalMethod ItemCode） */
  shipmentMethod?: string | null
  /** 预计到货快递单号 */
  courierTrackingNo?: string | null
  /** 快递公司（字典 LogisticsExpressMethod ItemCode） */
  expressCompany?: string | null
  createTime: string
  modifyTime?: string
  createUserName?: string | null
  createdBy?: string | null
  items: StockInNotifyItemDto[]
}

function normalizeStockInNotifyItem(row: unknown): StockInNotifyItemDto {
  const r = (row ?? {}) as Record<string, unknown>
  return {
    id: String(r.id ?? r.Id ?? ''),
    stockInNotifyId: String(r.stockInNotifyId ?? r.StockInNotifyId ?? ''),
    purchaseOrderItemId: String(r.purchaseOrderItemId ?? r.PurchaseOrderItemId ?? ''),
    pn: (r.pn ?? r.Pn) as string | undefined,
    brand: (r.brand ?? r.Brand) as string | undefined,
    qty: Number(r.qty ?? r.Qty ?? 0),
    arrivedQty: Number(r.arrivedQty ?? r.ArrivedQty ?? 0),
    passedQty: Number(r.passedQty ?? r.PassedQty ?? 0)
  }
}

export function normalizeStockInNotifyRow(row: unknown): StockInNotifyDto {
  const r = (row ?? {}) as Record<string, unknown>
  const rawItems = (r.items ?? r.Items) as unknown[] | undefined
  const items: StockInNotifyItemDto[] = Array.isArray(rawItems) ? rawItems.map(normalizeStockInNotifyItem) : []
  return {
    id: String(r.id ?? r.Id ?? ''),
    noticeCode: String(r.noticeCode ?? r.NoticeCode ?? ''),
    purchaseOrderId: String(r.purchaseOrderId ?? r.PurchaseOrderId ?? ''),
    purchaseOrderCode: String(r.purchaseOrderCode ?? r.PurchaseOrderCode ?? ''),
    freightForwarderOrderNo: (r.freightForwarderOrderNo ?? r.FreightForwarderOrderNo) as string | null | undefined,
    purchaseOrderItemId: (r.purchaseOrderItemId ?? r.PurchaseOrderItemId) as string | undefined,
    sellOrderItemId: (r.sellOrderItemId ?? r.SellOrderItemId) as string | null | undefined,
    vendorId: (r.vendorId ?? r.VendorId) as string | undefined,
    vendorName: (r.vendorName ?? r.VendorName) as string | undefined,
    vendorEnglishName: (r.vendorEnglishName ?? r.VendorEnglishName) as string | undefined,
    vendorCode: (r.vendorCode ?? r.VendorCode) as string | null | undefined,
    purchaseUserName: (r.purchaseUserName ?? r.PurchaseUserName) as string | undefined,
    status: Number(r.status ?? r.Status ?? 0),
    expectedArrivalDate: (r.expectedArrivalDate ?? r.ExpectedArrivalDate) as string | null | undefined,
    regionType: r.regionType != null || r.RegionType != null ? Number(r.regionType ?? r.RegionType) : undefined,
    stockInType:
      r.stockInType != null || r.StockInType != null ? Number(r.stockInType ?? r.StockInType) : undefined,
    pn: (r.pn ?? r.Pn) as string | null | undefined,
    brand: (r.brand ?? r.Brand) as string | null | undefined,
    expectQty: r.expectQty != null || r.ExpectQty != null ? Number(r.expectQty ?? r.ExpectQty) : undefined,
    receiveQty: r.receiveQty != null || r.ReceiveQty != null ? Number(r.receiveQty ?? r.ReceiveQty) : undefined,
    passedQty: r.passedQty != null || r.PassedQty != null ? Number(r.passedQty ?? r.PassedQty) : undefined,
    remark: (r.remark ?? r.Remark) as string | null | undefined,
    shipmentMethod: (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined,
    courierTrackingNo: (r.courierTrackingNo ?? r.CourierTrackingNo) as string | null | undefined,
    expressCompany: (r.expressCompany ?? r.ExpressCompany) as string | null | undefined,
    createTime: String(r.createTime ?? r.CreateTime ?? ''),
    modifyTime: (r.modifyTime ?? r.ModifyTime) as string | undefined,
    createUserName: (r.createUserName ?? r.CreateUserName) as string | null | undefined,
    createdBy: (r.createdBy ?? r.CreatedBy) as string | null | undefined,
    items
  }
}

export interface QcInfoDto {
  id: string
  qcCode: string
  stockInNotifyId: string
  stockInNotifyCode: string
  vendorName?: string
  vendorEnglishName?: string
  purchaseOrderCode?: string
  freightForwarderOrderNo?: string | null
  salesOrderCode?: string
  model?: string
  brand?: string
  status: number
  stockInStatus: number
  passQty: number
  rejectQty: number
  stockInId?: string
  /** 入库类型：10采购 20报关 30退货 40报废 */
  stockInType?: number
  /** 质检保存的计划入库日（ISO）；生成入库单时优先使用 */
  stockInPlanDate?: string | null
  /** 部分接口仍返回 PascalCase */
  StockInPlanDate?: string | null
  remark?: string | null
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

function unwrapListPaged<T>(res: any, mapItem?: (row: unknown) => T): ListPaged<T> {
  const d = res?.data ?? res
  if (d && typeof d === 'object' && Array.isArray(d.items)) {
    const items = mapItem ? (d.items as unknown[]).map(mapItem) : (d.items as T[])
    return {
      items,
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
    freightForwarderOrderNo?: string
    expectedArrivalDate?: string
    /** 到货类型（StockInType：10 采购 / 20 报关 / 30 退货 / 40 报废） */
    stockInType?: number
    /** 按到货通知主键精确查（编辑/联动场景） */
    id?: string
    page?: number
    pageSize?: number
  }): Promise<ListPaged<StockInNotifyDto>> {
    const res = await apiClient.get<any>('/api/v1/logistics/arrival-notices', { params })
    return unwrapListPaged<StockInNotifyDto>(res, normalizeStockInNotifyRow)
  },
  async createArrivalNotice(payload: {
    purchaseOrderItemId: string
    expectQty: number
    purchaseOrderId?: string
    expectedArrivalDate?: string | null
    regionType?: number
    remark?: string | null
    shipmentMethod?: string | null
    courierTrackingNo?: string | null
    expressCompany?: string | null
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
    freightForwarderOrderNo?: string
    salesOrderCode?: string
    /** 到货类型（StockInType：10 采购 / 20 报关 / 30 退货 / 40 报废） */
    stockInType?: number
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
      /** 为 true 时写入 remark（含 null 清空） */
      hasRemark?: boolean
      remark?: string | null
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
