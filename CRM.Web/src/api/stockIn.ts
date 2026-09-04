import apiClient from './client'
import type { StockItemFlowDoc } from './inventoryCenter'

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
  /** 报关入库时关联报关公司名称 */
  customsBrokerName?: string | null
  /** 明细单价汇总（多行逗号分隔） */
  unitPriceSummary?: string | null
  /** 单价币别（明细 currency 去重） */
  unitPriceCurrencyCode?: number | null
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
      status?: number
      itemCurrency?: number
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

  /** 按当前筛选导出入库单列表 CSV（服务端写审计日志） */
  async exportList(
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
      status?: number
      itemCurrency?: number
    }
  ): Promise<Blob> {
    const qs = new URLSearchParams()
    Object.entries(params ?? {}).forEach(([k, v]) => {
      if (v === undefined || v === null || v === '') return
      qs.set(k, String(v))
    })
    const q = qs.toString()
    return apiClient.getBlob(q ? `/api/v1/stock-in/export?${q}` : '/api/v1/stock-in/export')
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
  },

  async getOpsAggregates(id: string): Promise<StockInOpsAggregatesDto> {
    const res = await apiClient.get<unknown>(`/api/v1/stock-in/${encodeURIComponent(id)}/ops-aggregates`)
    return normalizeStockInOpsAggregates(res)
  },

  async getFlowAggregates(id: string): Promise<StockInFlowAggregatesDto> {
    const res = await apiClient.get<unknown>(`/api/v1/stock-in/${encodeURIComponent(id)}/flow-aggregates`)
    return normalizeStockInFlowAggregates(res)
  },

  async runOpsCheck(): Promise<StockInOpsCheckResult> {
    const res = await apiClient.post<unknown>('/api/v1/stock-in/ops-check')
    const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
    const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
    const raw = Array.isArray(d?.findings)
      ? d!.findings
      : Array.isArray(d?.Findings)
        ? d!.Findings
        : []
    return {
      ranAtUtc: String(d?.ranAtUtc ?? d?.RanAtUtc ?? ''),
      errorCount: Number(d?.errorCount ?? d?.ErrorCount ?? 0),
      warningCount: Number(d?.warningCount ?? d?.WarningCount ?? 0),
      findingCount: Number(d?.findingCount ?? d?.FindingCount ?? 0),
      truncated: Boolean(d?.truncated ?? d?.Truncated),
      findings: (raw as unknown[]).map(mapOpsFinding)
    }
  }
}

export interface StockInOpsCheckFinding {
  severity: string
  category: string
  docType: string
  docId?: string | null
  docCode?: string | null
  routeName?: string | null
  routeParams?: Record<string, string> | null
  routeQuery?: Record<string, string> | null
  relatedDocType?: string | null
  relatedDocId?: string | null
  relatedDocCode?: string | null
  relatedRouteName?: string | null
  relatedRouteParams?: Record<string, string> | null
  relatedRouteQuery?: Record<string, string> | null
  reason: string
  suggestion: string
}

export interface StockInOpsCheckResult {
  ranAtUtc: string
  errorCount: number
  warningCount: number
  findingCount: number
  truncated: boolean
  findings: StockInOpsCheckFinding[]
}

export interface StockInOpsPurchaseLineDto {
  purchaseOrderItemId: string
  purchaseOrderItemCode: string
  purchaseOrderId: string
  purchaseUserName?: string | null
  purchaseOrderCreateTime?: string | null
  qty: number
  purchaseOrderType: number
  unitPrice: number
  currency: number
}

export interface StockInOpsArrivalNoticeDto {
  id: string
  noticeCode: string
  stockInType: number
  actualArrivalDate?: string | null
  receiveQty: number
  passQty?: number | null
}

export interface StockInOpsAggregatesDto {
  purchase?: StockInOpsPurchaseLineDto | null
  arrivalNotice?: StockInOpsArrivalNoticeDto | null
}

export interface StockInFlowAggregatesDto {
  stockInId: string
  stockIn: StockItemFlowDoc
  purchaseOrderItems?: StockItemFlowDoc[]
  qcs?: StockItemFlowDoc[]
  stockItems?: StockItemFlowDoc[]
  stockOutNotifies?: StockItemFlowDoc[]
  packings?: StockItemFlowDoc[]
  stockOuts?: StockItemFlowDoc[]
}

function unwrapApiData(res: unknown): Record<string, unknown> {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : {}
  const data = (root.data ?? root.Data ?? root) as Record<string, unknown>
  return data && typeof data === 'object' ? data : {}
}

function normalizeStockInOpsAggregates(res: unknown): StockInOpsAggregatesDto {
  const r = unwrapApiData(res)
  const purchaseRaw = (r.purchase ?? r.Purchase) as Record<string, unknown> | null | undefined
  const arrivalRaw = (r.arrivalNotice ?? r.ArrivalNotice) as Record<string, unknown> | null | undefined
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
          qty: Number(purchaseRaw.qty ?? purchaseRaw.Qty ?? 0),
          purchaseOrderType: Number(purchaseRaw.purchaseOrderType ?? purchaseRaw.PurchaseOrderType ?? 0),
          unitPrice: Number(purchaseRaw.unitPrice ?? purchaseRaw.UnitPrice ?? 0),
          currency: Number(purchaseRaw.currency ?? purchaseRaw.Currency ?? 0)
        }
      : null,
    arrivalNotice: arrivalRaw
      ? {
          id: String(arrivalRaw.id ?? arrivalRaw.Id ?? ''),
          noticeCode: String(arrivalRaw.noticeCode ?? arrivalRaw.NoticeCode ?? ''),
          stockInType: Number(arrivalRaw.stockInType ?? arrivalRaw.StockInType ?? 0),
          actualArrivalDate: (arrivalRaw.actualArrivalDate ?? arrivalRaw.ActualArrivalDate) as
            | string
            | null
            | undefined,
          receiveQty: Number(arrivalRaw.receiveQty ?? arrivalRaw.ReceiveQty ?? 0),
          passQty:
            arrivalRaw.passQty != null || arrivalRaw.PassQty != null
              ? Number(arrivalRaw.passQty ?? arrivalRaw.PassQty)
              : null
        }
      : null
  }
}

function mapFlowDoc(raw: Record<string, unknown>): StockItemFlowDoc {
  return {
    id: String(raw.id ?? raw.Id ?? ''),
    docCode: (raw.docCode ?? raw.DocCode) as string | null | undefined,
    status: raw.status != null || raw.Status != null ? Number(raw.status ?? raw.Status) : null,
    createTime: (raw.createTime ?? raw.CreateTime) as string | null | undefined,
    bizDate: (raw.bizDate ?? raw.BizDate) as string | null | undefined,
    vendorName: (raw.vendorName ?? raw.VendorName) as string | null | undefined,
    vendorCode: (raw.vendorCode ?? raw.VendorCode) as string | null | undefined,
    customerName: (raw.customerName ?? raw.CustomerName) as string | null | undefined,
    customerCode: (raw.customerCode ?? raw.CustomerCode) as string | null | undefined,
    personName: (raw.personName ?? raw.PersonName) as string | null | undefined,
    unitPrice: raw.unitPrice != null || raw.UnitPrice != null ? Number(raw.unitPrice ?? raw.UnitPrice) : null,
    currency: raw.currency != null || raw.Currency != null ? Number(raw.currency ?? raw.Currency) : null,
    salesUnitPrice:
      raw.salesUnitPrice != null || raw.SalesUnitPrice != null
        ? Number(raw.salesUnitPrice ?? raw.SalesUnitPrice)
        : null,
    salesCurrency:
      raw.salesCurrency != null || raw.SalesCurrency != null
        ? Number(raw.salesCurrency ?? raw.SalesCurrency)
        : null,
    qty: raw.qty != null || raw.Qty != null ? Number(raw.qty ?? raw.Qty) : null,
    qty2: raw.qty2 != null || raw.Qty2 != null ? Number(raw.qty2 ?? raw.Qty2) : null,
    passQty: raw.passQty != null || raw.PassQty != null ? Number(raw.passQty ?? raw.PassQty) : null,
    rejectQty: raw.rejectQty != null || raw.RejectQty != null ? Number(raw.rejectQty ?? raw.RejectQty) : null,
    stockInType:
      raw.stockInType != null || raw.StockInType != null ? Number(raw.stockInType ?? raw.StockInType) : null,
    stockOutType:
      raw.stockOutType != null || raw.StockOutType != null ? Number(raw.stockOutType ?? raw.StockOutType) : null,
    customsDeclarationId: (raw.customsDeclarationId ?? raw.CustomsDeclarationId) as string | null | undefined,
    customsDeclarationCode: (raw.customsDeclarationCode ?? raw.CustomsDeclarationCode) as string | null | undefined,
    stockInNotifyId: (raw.stockInNotifyId ?? raw.StockInNotifyId) as string | null | undefined,
    purchaseOrderId: (raw.purchaseOrderId ?? raw.PurchaseOrderId) as string | null | undefined,
    purchaseOrderItemId: (raw.purchaseOrderItemId ?? raw.PurchaseOrderItemId) as string | null | undefined,
    stockAggregateId: (raw.stockAggregateId ?? raw.StockAggregateId) as string | null | undefined,
    sellOrderId: (raw.sellOrderId ?? raw.SellOrderId) as string | null | undefined,
    lineDocCode: (raw.lineDocCode ?? raw.LineDocCode) as string | null | undefined,
    isDeleted: Boolean(raw.isDeleted ?? raw.IsDeleted)
  }
}

function mapFlowDocList(raw: unknown): StockItemFlowDoc[] {
  if (!Array.isArray(raw)) return []
  return raw
    .filter((x): x is Record<string, unknown> => !!x && typeof x === 'object')
    .map(mapFlowDoc)
}

function normalizeStockInFlowAggregates(res: unknown): StockInFlowAggregatesDto {
  const r = unwrapApiData(res)
  const stockInRaw = (r.stockIn ?? r.StockIn) as Record<string, unknown> | undefined
  return {
    stockInId: String(r.stockInId ?? r.StockInId ?? stockInRaw?.id ?? stockInRaw?.Id ?? ''),
    stockIn: mapFlowDoc(stockInRaw ?? {}),
    purchaseOrderItems: mapFlowDocList(r.purchaseOrderItems ?? r.PurchaseOrderItems),
    qcs: mapFlowDocList(r.qcs ?? r.Qcs),
    stockItems: mapFlowDocList(r.stockItems ?? r.StockItems),
    stockOutNotifies: mapFlowDocList(r.stockOutNotifies ?? r.StockOutNotifies),
    packings: mapFlowDocList(r.packings ?? r.Packings),
    stockOuts: mapFlowDocList(r.stockOuts ?? r.StockOuts)
  }
}

function mapOpsFinding(row: unknown): StockInOpsCheckFinding {
  const r = row && typeof row === 'object' ? (row as Record<string, unknown>) : {}
  const dict = (v: unknown): Record<string, string> | null => {
    if (!v || typeof v !== 'object') return null
    const out: Record<string, string> = {}
    for (const [k, val] of Object.entries(v as Record<string, unknown>)) {
      if (val != null) out[k] = String(val)
    }
    return out
  }
  return {
    severity: String(r.severity ?? r.Severity ?? 'error'),
    category: String(r.category ?? r.Category ?? ''),
    docType: String(r.docType ?? r.DocType ?? ''),
    docId: (r.docId ?? r.DocId) as string | null | undefined,
    docCode: (r.docCode ?? r.DocCode) as string | null | undefined,
    routeName: (r.routeName ?? r.RouteName) as string | null | undefined,
    routeParams: dict(r.routeParams ?? r.RouteParams),
    routeQuery: dict(r.routeQuery ?? r.RouteQuery),
    relatedDocType: (r.relatedDocType ?? r.RelatedDocType) as string | null | undefined,
    relatedDocId: (r.relatedDocId ?? r.RelatedDocId) as string | null | undefined,
    relatedDocCode: (r.relatedDocCode ?? r.RelatedDocCode) as string | null | undefined,
    relatedRouteName: (r.relatedRouteName ?? r.RelatedRouteName) as string | null | undefined,
    relatedRouteParams: dict(r.relatedRouteParams ?? r.RelatedRouteParams),
    relatedRouteQuery: dict(r.relatedRouteQuery ?? r.RelatedRouteQuery),
    reason: String(r.reason ?? r.Reason ?? ''),
    suggestion: String(r.suggestion ?? r.Suggestion ?? '')
  }
}
