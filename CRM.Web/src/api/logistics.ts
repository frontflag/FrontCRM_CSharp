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
  /** 实际到货日 */
  actualArrivalDate?: string | null
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
  /** 关联报关单主键（报关到货 Type=20） */
  customsDeclarationId?: string | null
  /** 关联报关单号（报关到货 Type=20） */
  customsDeclarationCode?: string | null
  /** 报关入库时关联报关公司名称（列表/操作面板展示） */
  customsBrokerName?: string | null
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
    actualArrivalDate: (r.actualArrivalDate ?? r.ActualArrivalDate) as string | null | undefined,
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
    customsDeclarationId: (r.customsDeclarationId ?? r.CustomsDeclarationId) as string | null | undefined,
    customsDeclarationCode: (r.customsDeclarationCode ?? r.CustomsDeclarationCode) as string | null | undefined,
    customsBrokerName: (r.customsBrokerName ?? r.CustomsBrokerName) as string | null | undefined,
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
  /** 本单质检图片附件数量（列表服务端聚合） */
  qcImageCount?: number
  QcImageCount?: number
  stockInId?: string
  /** 入库类型：10采购 20报关 30退货 40报废 */
  stockInType?: number
  /** 关联报关单主键（报关质检 Type=20） */
  customsDeclarationId?: string | null
  /** 关联报关单号（报关质检 Type=20） */
  customsDeclarationCode?: string | null
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

export interface ArrivalNoticeOpsPurchaseLineDto {
  purchaseOrderItemId: string
  purchaseOrderItemCode: string
  purchaseOrderId: string
  purchaseUserName?: string | null
  purchaseOrderCreateTime?: string | null
  qty: number
}

export interface ArrivalNoticeOpsQcDto {
  id: string
  qcCode: string
  createTime: string
  createUserName?: string | null
  passQty: number
  rejectQty: number
}

export interface ArrivalNoticeOpsStockInDto {
  id: string
  stockInCode: string
  stockInDate?: string | null
  createUserName?: string | null
  status: number
  stockInType: number
  warehouseName?: string | null
  totalQuantity: number
}

export interface ArrivalNoticeOpsAggregatesDto {
  purchase?: ArrivalNoticeOpsPurchaseLineDto | null
  qc?: ArrivalNoticeOpsQcDto | null
  stockIn?: ArrivalNoticeOpsStockInDto | null
}

export interface QcOpsArrivalNoticeDto {
  id: string
  noticeCode: string
  stockInType: number
  actualArrivalDate?: string | null
  expectedArrivalDate?: string | null
  expectQty: number
}

export interface QcOpsAggregatesDto {
  purchase?: ArrivalNoticeOpsPurchaseLineDto | null
  arrivalNotice?: QcOpsArrivalNoticeDto | null
  stockIn?: ArrivalNoticeOpsStockInDto | null
}

function normalizeQcOpsAggregates(row: unknown): QcOpsAggregatesDto {
  const r = (row ?? {}) as Record<string, unknown>
  const purchaseRaw = (r.purchase ?? r.Purchase) as Record<string, unknown> | null | undefined
  const arrivalRaw = (r.arrivalNotice ?? r.ArrivalNotice) as Record<string, unknown> | null | undefined
  const stockInRaw = (r.stockIn ?? r.StockIn) as Record<string, unknown> | null | undefined
  return {
    purchase: purchaseRaw
      ? {
          purchaseOrderItemId: String(purchaseRaw.purchaseOrderItemId ?? purchaseRaw.PurchaseOrderItemId ?? ''),
          purchaseOrderItemCode: String(
            purchaseRaw.purchaseOrderItemCode ?? purchaseRaw.PurchaseOrderItemCode ?? ''
          ),
          purchaseOrderId: String(purchaseRaw.purchaseOrderId ?? purchaseRaw.PurchaseOrderId ?? ''),
          purchaseUserName: (purchaseRaw.purchaseUserName ?? purchaseRaw.PurchaseUserName) as
            | string
            | null
            | undefined,
          purchaseOrderCreateTime: (purchaseRaw.purchaseOrderCreateTime ??
            purchaseRaw.PurchaseOrderCreateTime) as string | null | undefined,
          qty: Number(purchaseRaw.qty ?? purchaseRaw.Qty ?? 0)
        }
      : null,
    arrivalNotice: arrivalRaw
      ? {
          id: String(arrivalRaw.id ?? arrivalRaw.Id ?? ''),
          noticeCode: String(arrivalRaw.noticeCode ?? arrivalRaw.NoticeCode ?? ''),
          stockInType: Number(arrivalRaw.stockInType ?? arrivalRaw.StockInType ?? 10),
          actualArrivalDate: (arrivalRaw.actualArrivalDate ?? arrivalRaw.ActualArrivalDate) as
            | string
            | null
            | undefined,
          expectedArrivalDate: (arrivalRaw.expectedArrivalDate ?? arrivalRaw.ExpectedArrivalDate) as
            | string
            | null
            | undefined,
          expectQty: Number(arrivalRaw.expectQty ?? arrivalRaw.ExpectQty ?? 0)
        }
      : null,
    stockIn: stockInRaw
      ? {
          id: String(stockInRaw.id ?? stockInRaw.Id ?? ''),
          stockInCode: String(stockInRaw.stockInCode ?? stockInRaw.StockInCode ?? ''),
          stockInDate: (stockInRaw.stockInDate ?? stockInRaw.StockInDate) as string | null | undefined,
          createUserName: (stockInRaw.createUserName ?? stockInRaw.CreateUserName) as string | null | undefined,
          status: Number(stockInRaw.status ?? stockInRaw.Status ?? 0),
          stockInType: Number(stockInRaw.stockInType ?? stockInRaw.StockInType ?? 0),
          warehouseName: (stockInRaw.warehouseName ?? stockInRaw.WarehouseName) as string | null | undefined,
          totalQuantity: Number(stockInRaw.totalQuantity ?? stockInRaw.TotalQuantity ?? 0)
        }
      : null
  }
}

function normalizeArrivalNoticeOpsAggregates(row: unknown): ArrivalNoticeOpsAggregatesDto {
  const r = (row ?? {}) as Record<string, unknown>
  const purchaseRaw = (r.purchase ?? r.Purchase) as Record<string, unknown> | null | undefined
  const qcRaw = (r.qc ?? r.Qc) as Record<string, unknown> | null | undefined
  const stockInRaw = (r.stockIn ?? r.StockIn) as Record<string, unknown> | null | undefined
  return {
    purchase: purchaseRaw
      ? {
          purchaseOrderItemId: String(purchaseRaw.purchaseOrderItemId ?? purchaseRaw.PurchaseOrderItemId ?? ''),
          purchaseOrderItemCode: String(
            purchaseRaw.purchaseOrderItemCode ?? purchaseRaw.PurchaseOrderItemCode ?? ''
          ),
          purchaseOrderId: String(purchaseRaw.purchaseOrderId ?? purchaseRaw.PurchaseOrderId ?? ''),
          purchaseUserName: (purchaseRaw.purchaseUserName ?? purchaseRaw.PurchaseUserName) as
            | string
            | null
            | undefined,
          purchaseOrderCreateTime: (purchaseRaw.purchaseOrderCreateTime ??
            purchaseRaw.PurchaseOrderCreateTime) as string | null | undefined,
          qty: Number(purchaseRaw.qty ?? purchaseRaw.Qty ?? 0)
        }
      : null,
    qc: qcRaw
      ? {
          id: String(qcRaw.id ?? qcRaw.Id ?? ''),
          qcCode: String(qcRaw.qcCode ?? qcRaw.QcCode ?? ''),
          createTime: String(qcRaw.createTime ?? qcRaw.CreateTime ?? ''),
          createUserName: (qcRaw.createUserName ?? qcRaw.CreateUserName) as string | null | undefined,
          passQty: Number(qcRaw.passQty ?? qcRaw.PassQty ?? 0),
          rejectQty: Number(qcRaw.rejectQty ?? qcRaw.RejectQty ?? 0)
        }
      : null,
    stockIn: stockInRaw
      ? {
          id: String(stockInRaw.id ?? stockInRaw.Id ?? ''),
          stockInCode: String(stockInRaw.stockInCode ?? stockInRaw.StockInCode ?? ''),
          stockInDate: (stockInRaw.stockInDate ?? stockInRaw.StockInDate) as string | null | undefined,
          createUserName: (stockInRaw.createUserName ?? stockInRaw.CreateUserName) as string | null | undefined,
          status: Number(stockInRaw.status ?? stockInRaw.Status ?? 0),
          stockInType: Number(stockInRaw.stockInType ?? stockInRaw.StockInType ?? 0),
          warehouseName: (stockInRaw.warehouseName ?? stockInRaw.WarehouseName) as string | null | undefined,
          totalQuantity: Number(stockInRaw.totalQuantity ?? stockInRaw.TotalQuantity ?? 0)
        }
      : null
  }
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
    /** 左栏 preset（与 ArrivalNoticeListQuickFilterCodes 一致） */
    preset?: string
    /** 到货类型（StockInType：10 采购 / 20 报关 / 30 退货 / 40 报废） */
    stockInType?: number
    /** 物料型号（通知快照 / 采购明细 PN，Contains） */
    pn?: string
    /** 供应商名称（中文快照 / 档案中英文名，Contains） */
    vendorName?: string
    /** 采购币种（CurrencyCode 1–6，按采购明细币别精确匹配） */
    purchaseCurrency?: number
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
  async updateArrivalInfo(
    id: string,
    payload: {
      shipmentMethod?: string | null
      expressCompany?: string | null
      courierTrackingNo?: string | null
    }
  ): Promise<StockInNotifyDto> {
    return normalizeStockInNotifyRow(
      unwrap<unknown>(
        await apiClient.patch(`/api/v1/logistics/arrival-notices/${encodeURIComponent(id)}/arrival-info`, payload)
      )
    )
  },
  async deleteArrivalNotice(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/logistics/arrival-notices/${encodeURIComponent(id)}`)
  },
  async forceDeleteArrivalNotice(id: string, confirmBillCode: string): Promise<void> {
    await apiClient.post(`/api/v1/logistics/arrival-notices/${encodeURIComponent(id)}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  },
  async getArrivalNoticeOpsAggregates(id: string): Promise<ArrivalNoticeOpsAggregatesDto> {
    const res = await apiClient.get<any>(
      `/api/v1/logistics/arrival-notices/${encodeURIComponent(id)}/ops-aggregates`
    )
    return normalizeArrivalNoticeOpsAggregates(unwrap(res))
  },
  async getQcOpsAggregates(id: string): Promise<QcOpsAggregatesDto> {
    const res = await apiClient.get<any>(`/api/v1/logistics/qcs/${encodeURIComponent(id)}/ops-aggregates`)
    return normalizeQcOpsAggregates(unwrap(res))
  },
  async getQcs(params?: {
    qcId?: string
    qcCode?: string
    model?: string
    vendorName?: string
    purchaseOrderCode?: string
    freightForwarderOrderNo?: string
    salesOrderCode?: string
    /** 左栏 preset（与 QcListQuickFilterCodes 一致） */
    preset?: string
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
