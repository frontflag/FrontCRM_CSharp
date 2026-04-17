import apiClient from './client'

export interface StockInItemDto {
  lineNo: number
  /** 详情页：入库明细业务编号（服务端生成） */
  stockInItemCode?: string
  /** 创建时作为 MaterialId 提交；详情页仅保留数据不展示列 */
  materialCode: string
  /** 展示用：物料型号（与入库列表「物料型号」一致） */
  materialName: string
  /** 展示用：品牌（详情由接口填充；创建可填但不提交后端） */
  materialBrand?: string
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
  /** 到货通知主键，写入 stockin.SourceId/SourceCode */
  stockInNotifyId?: string
  /** 质检主键，写入 stockin.QCID/QcCode；可顺带从质检解析到货通知 */
  qcId?: string
  vendorId?: string
  warehouseId: string
  operatorId?: string
  stockInDate: string
  totalQuantity: number
  remark?: string
  items?: StockInItemDto[]
}

/** GET 详情接口中单条入库明细（与后端 StockInItem 序列化字段一致，camelCase） */
export interface StockInDetailItemDto {
  id: string
  stockInId: string
  /** 入库明细业务编号（{stockInCode}-{行序号}） */
  stockInItemCode?: string
  materialId: string
  quantity: number
  price: number
  amount: number
  locationId?: string | null
  batchNo?: string | null
  remark?: string | null
  /** 后端填充：物料编码 */
  detailMaterialCode?: string | null
  /** 后端填充：物料名称 */
  detailMaterialName?: string | null
  /** 后端填充：物料型号 */
  detailMaterialModel?: string | null
  /** 后端填充：品牌 */
  detailMaterialBrand?: string | null
  /** 后端填充：单位 */
  detailUnit?: string | null
  /** 表字段快照：采购 PN */
  purchasePn?: string | null
  /** 表字段快照：采购品牌 */
  purchaseBrand?: string | null
  currency?: number | null
}

export interface StockInDto {
  id: string
  stockInCode: string
  stockInType: number
  purchaseOrderItemCode?: string | null
  purchaseOrderItemId?: string | null
  sellOrderItemCode?: string | null
  sellOrderItemId?: string | null
  /** 到货通知编码 */
  sourceCode?: string | null
  /** 到货通知 ID */
  sourceId?: string | null
  qcCode?: string | null
  qcId?: string | null
  warehouseId: string
  vendorId?: string
  stockInDate: string
  totalQuantity: number
  totalAmount: number
  status: number
  remark?: string
  createTime?: string
  items?: StockInDetailItemDto[]
  /** 详情填充：仓库编号 */
  detailWarehouseCode?: string | null
  /** 详情填充：供应商展示名 */
  detailVendorName?: string | null
}

/** 列表接口返回：含到货通知号、供应商名称等展示字段 */
export interface StockInListItemDto {
  id: string
  stockInCode: string
  stockInType: number
  sourceDisplayNo?: string
  warehouseId: string
  vendorId?: string
  vendorName?: string
  /** 采购订单号（列表接口由采购头解析） */
  purchaseOrderCode?: string | null
  salesOrderCode?: string
  /** 列表汇总：物料型号（多行逗号分隔） */
  materialModelSummary?: string | null
  /** 列表汇总：品牌 */
  materialBrandSummary?: string | null
  stockInDate: string
  totalQuantity: number
  totalAmount: number
  /** 币别编码（与采购明细一致）；后端无法解析时为 null */
  currencyCode?: number | null
  status: number
  remark?: string
  createTime?: string
  /** 创建人展示名（后端由 CreatedBy 解析） */
  createUserName?: string | null
}

export const stockInApi = {
  async getAll(params?: {
    model?: string
    vendorName?: string
    purchaseOrderCode?: string
    salesOrderCode?: string
  }): Promise<StockInListItemDto[]> {
    const res = await apiClient.get<any>('/api/v1/stock-in', { params })
    // 与 inventoryCenter 等一致：兼容拦截器已解包为数组，或仍带一层 data
    const payload = res?.data ?? res
    return Array.isArray(payload) ? (payload as StockInListItemDto[]) : []
  },

  async getById(id: string): Promise<StockInDto | null> {
    const res = await apiClient.get<any>(`/api/v1/stock-in/${encodeURIComponent(id)}`)
    if (res == null || typeof res !== 'object') return null
    return res as StockInDto
  },

  async create(data: CreateStockInRequest): Promise<StockInDto> {
    const payload = {
      stockInCode: data.stockInCode,
      purchaseOrderId: data.purchaseOrderId,
      stockInNotifyId: data.stockInNotifyId,
      qcId: data.qcId,
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
