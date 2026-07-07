import apiClient from './client'

export interface StockInItemDto {
  lineNo: number
  /** 详情页：入库明细主键（ItemId），用于批次录入等 */
  itemId?: string
  /** 详情页：入库明细业务编号（服务端生成） */
  stockInItemCode?: string
  /** 详情页：入库日期（单头冗余） */
  stockInDate?: string
  /** 详情页：到货通知单号 */
  sourceCode?: string
  /** 详情页：采购订单明细编号 */
  purchaseOrderItemCode?: string
  /** 详情页：供应商名称 */
  vendorName?: string
  /** 详情页：供应商英文名（展示用） */
  vendorEnglishName?: string
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
  /** 详情页：采购总额 */
  amount?: number
  /** 详情页：采购币别 */
  currency?: number
  /** 详情页：地域类型 */
  regionType?: number
  /** 详情页：仓库编号 */
  warehouseCode?: string
  /** 详情页：入库类型 */
  stockInType?: number
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
  /** 详情填充：入库日期 */
  detailStockInDate?: string | null
  /** 详情填充：到货通知单号 */
  detailSourceCode?: string | null
  /** 详情填充：采购订单明细编号 */
  detailPurchaseOrderItemCode?: string | null
  /** 详情填充：供应商名称 */
  detailVendorName?: string | null
  /** 详情填充：仓库编号 */
  detailWarehouseCode?: string | null
  /** 详情填充：仓库名称 */
  detailWarehouseName?: string | null
  /** 详情填充：地域类型 */
  detailRegionType?: number | null
  /** 详情填充：入库类型 */
  detailStockInType?: number | null
  /** 详情填充：采购币别 */
  detailCurrency?: number | null
}

export interface StockInCustomsContextItemDto {
  arrivalNotifyId?: string | null
  arrivalNotifyCode?: string | null
  declarationItemId: string
  lineNo: number
  declarationId: string
  declarationCode: string
  customsBrokerId: string
  customsBrokerName?: string | null
  customsBrokerCode?: string | null
  packingId?: string | null
  packingCode?: string | null
  fromWarehouseId?: string | null
  fromWarehouseCode?: string | null
  toWarehouseId?: string | null
  toWarehouseCode?: string | null
  salesStockOutNotifyId?: string | null
  salesStockOutNotifyCode?: string | null
  customsStockOutNotifyId?: string | null
  customsStockOutNotifyCode?: string | null
  vendorId?: string | null
  vendorName?: string | null
  originalPurchasePrice?: number | null
  taxIncludedUnitPrice?: number | null
  sellOrderItemCode?: string | null
  customerId?: string | null
  customerName?: string | null
  purchasePn?: string | null
  purchaseBrand?: string | null
  declareQty?: number | null
  customsClearanceStatus?: number | null
  hsCode?: string | null
  declareUnitPrice?: number | null
  dutyAmount?: number | null
  vatAmount?: number | null
  customsPaymentGoods?: number | null
  customsAgencyFee?: number | null
  otherFee?: number | null
  inspectionFee?: number | null
  totalValueTax?: number | null
  declareDate?: string | null
  declarationTotalTaxAmount?: number | null
  exchangeRate?: number | null
  timeline?: StockInCustomsTimelineStepDto[]
}

export interface StockInCustomsTimelineStepDto {
  stepCode: string
  sortOrder: number
  docId?: string | null
  docCode?: string | null
  status?: number | null
  occurredAt?: string | null
  /** pending | done */
  state: string
}

export interface StockInCustomsContextDto {
  qcId?: string | null
  qcCode?: string | null
  items: StockInCustomsContextItemDto[]
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
  /** 地域类型：10=境内 20=境外 */
  regionType?: number
  status: number
  remark?: string
  createTime?: string
  /** 详情/列表：创建人登录账号（由后端解析） */
  createUserName?: string | null
  items?: StockInDetailItemDto[]
  /** 详情填充：仓库编号 */
  detailWarehouseCode?: string | null
  /** 详情填充：仓库名称 */
  detailWarehouseName?: string | null
  /** 详情填充：供应商展示名 */
  detailVendorName?: string | null
  /** 报关入库溯源（StockInType=20） */
  customsContext?: StockInCustomsContextDto | null
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
  vendorEnglishName?: string | null
  vendorCode?: string | null
  /** 采购订单号（列表接口由采购头解析） */
  purchaseOrderCode?: string | null
  freightForwarderOrderNo?: string | null
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
  /** 是否已录入入库批次 */
  hasBatchEntered?: boolean
  /** 关联报关单主键（报关入库 Type=20） */
  customsDeclarationId?: string | null
  /** 关联报关单号（报关入库 Type=20） */
  customsDeclarationCode?: string | null
}

/** GET 入库单列表：与《翻页查询规范》<code>data</code> 结构一致 */
export type StockInListPaged = { items: StockInListItemDto[]; total: number; page: number; pageSize: number }

export const stockInApi = {
  async getListPaged(
    params?: {
      model?: string
      vendorName?: string
      purchaseOrderCode?: string
      freightForwarderOrderNo?: string
      salesOrderCode?: string
      stockInCode?: string
      sourceDisplayNo?: string
      warehouseId?: string
      stockInDateStart?: string
      stockInDateEnd?: string
      remark?: string
      stockInType?: number
      page?: number
      pageSize?: number
    }
  ): Promise<StockInListPaged> {
    const res = await apiClient.get<any>('/api/v1/stock-in', { params })
    const d = res?.data ?? res
    if (d && typeof d === 'object' && Array.isArray(d.items)) {
      return {
        items: d.items as StockInListItemDto[],
        total: Number(d.total ?? 0),
        page: Number(d.page ?? 1),
        pageSize: Number(d.pageSize ?? 20)
      }
    }
    return { items: [], total: 0, page: 1, pageSize: 20 }
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

  async forceDelete(id: string, confirmBillCode: string): Promise<void> {
    await apiClient.post(`/api/v1/stock-in/${id}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  },

  async updateStatus(id: string, status: number): Promise<void> {
    await apiClient.patch(`/api/v1/stock-in/${id}/status?status=${status}`)
  }
}
