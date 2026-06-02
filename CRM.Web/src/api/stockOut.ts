import apiClient, { type ApiRejectedError } from './client'
import { fetchCompanyProfileForReport, type CompanyProfileBundle, type CompanyReportInfo } from '@/api/companyProfile'

export interface StockOutDto {
  id: string
  stockOutCode: string
  stockOutType: number
  sourceCode?: string
  /** 列表接口不再返回；详情等仍可能返回 */
  warehouseId?: string
  stockOutDate: string
  /** 预计出库日期 */
  expectedStockOutDate?: string | null
  /** 关联装箱单数量 */
  packingCount?: number
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

export interface StockOutMarkFinishPacking {
  id: string
  code?: string | null
}

export interface StockOutMarkFinishContext {
  stockOutId: string
  stockOutCode?: string
  customerName?: string
  shipAddress?: string
  packings: StockOutMarkFinishPacking[]
  stockOutDate?: string
  courierTrackingNo?: string
  remark?: string
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
  /** 客单绑定在库可用（本销售行） */
  availableStockQty: number
  /** 同 PN+品牌备货在库可用 */
  purchasedStockAvailableQty?: number
  suggestedMaxQty: number
}

/** 解包 GET apply-context：兼容 PascalCase、双重 data 包层，避免备货字段解析为 0 */
function normalizeApplyContextPayload(res: unknown): StockOutApplyContextDto {
  let o: Record<string, unknown> | null = null
  if (res && typeof res === 'object') {
    o = res as Record<string, unknown>
    const inner = o.data ?? o.Data
    if (inner && typeof inner === 'object') o = inner as Record<string, unknown>
  }
  if (!o) {
    return {
      salesOrderItemId: '',
      salesOrderQty: 0,
      alreadyNotifiedQty: 0,
      remainingNotifyQty: 0,
      availableStockQty: 0,
      purchasedStockAvailableQty: 0,
      suggestedMaxQty: 0
    }
  }
  const num = (v: unknown) => Number(v ?? 0)
  const truncInt = (v: unknown) => Math.trunc(num(v))
  return {
    salesOrderItemId: String(o.salesOrderItemId ?? o.SalesOrderItemId ?? ''),
    salesOrderQty: num(o.salesOrderQty ?? o.SalesOrderQty),
    alreadyNotifiedQty: num(o.alreadyNotifiedQty ?? o.AlreadyNotifiedQty),
    remainingNotifyQty: num(o.remainingNotifyQty ?? o.RemainingNotifyQty),
    availableStockQty: num(o.availableStockQty ?? o.AvailableStockQty),
    purchasedStockAvailableQty: truncInt(o.purchasedStockAvailableQty ?? o.PurchasedStockAvailableQty),
    suggestedMaxQty: num(o.suggestedMaxQty ?? o.SuggestedMaxQty)
  }
}

/** GET /api/v1/stock-out/:id/invoice-report-bundle（打印页：出库详情 + 公司参数） */
export interface StockOutInvoiceReportBundle {
  stockOut: StockOutDetailDto
  companyProfile: CompanyProfileBundle
  packingCode?: string | null
  packingAddresses?: PackingReportAddressPanel | null
  warehouseAddress?: string | null
  /** packing.storage_id / 出库单仓库 → warehouseinfo.RegionType：10=大陆 20=海外 */
  warehouseRegionType?: number | null
  packingLines?: PackingReportLine[]
}

/** Packing 报表 Bill To / Ship To：客户名称、地址、联系人、电话 */
export interface PackingReportAddressPanel {
  billToLines: string[]
  shipToLines: string[]
}

/** GET /api/v1/stock-out/:id/packing-report-bundle?withInspection=… */
export interface StockOutPackingReportBundle extends StockOutInvoiceReportBundle {
  withShipmentInspection: boolean
  /** 关联装箱单编号 packing.code（PAK…） */
  packingCode?: string | null
  packingAddresses?: PackingReportAddressPanel | null
  /** 出库仓库地址 warehouseinfo.Address */
  warehouseAddress?: string | null
  /** 装箱单送货方式 packing_extend_ship.DeliveryMethod（10=送货 20=自提） */
  deliveryMethod?: number | null
  /** 装箱单明细行 */
  packingLines?: PackingReportLine[]
}

export interface PackingReportLine {
  pn?: string | null
  customerPn?: string | null
  brand?: string | null
  customerBrand?: string | null
  qty: number
  carton?: string | null
  remark?: string | null
}

export function parseInvoiceBundlePayload(res: unknown): StockOutInvoiceReportBundle | null {
  if (!res || typeof res !== 'object') return null
  const o = res as Record<string, unknown>
  const stockOut = (o.stockOut ?? o.StockOut) as StockOutDetailDto | undefined
  const rawCp = (o.companyProfile ?? o.CompanyProfile) as Record<string, unknown> | undefined
  if (!stockOut || !rawCp) return null
  const companyProfile: CompanyProfileBundle = {
    basicInfos: (rawCp.basicInfos ?? rawCp.BasicInfos ?? []) as CompanyProfileBundle['basicInfos'],
    bankInfos: (rawCp.bankInfos ?? rawCp.BankInfos ?? []) as CompanyProfileBundle['bankInfos'],
    logos: (rawCp.logos ?? rawCp.Logos ?? []) as NonNullable<CompanyProfileBundle['logos']>,
    seals: (rawCp.seals ?? rawCp.Seals ?? []) as CompanyProfileBundle['seals'],
    warehouses: (rawCp.warehouses ?? rawCp.Warehouses ?? []) as CompanyProfileBundle['warehouses'],
    reportInfo: normalizeReportInfo(rawCp.reportInfo ?? rawCp.ReportInfo)
  }
  const packingAddresses = parsePackingAddressPanel(o.packingAddresses ?? o.PackingAddresses)
  const rawCode = o.packingCode ?? o.PackingCode
  const packingCode =
    typeof rawCode === 'string' && rawCode.trim().length > 0 ? rawCode.trim() : null
  const rawWarehouseAddress = o.warehouseAddress ?? o.WarehouseAddress
  const warehouseAddress =
    typeof rawWarehouseAddress === 'string' && rawWarehouseAddress.trim().length > 0
      ? rawWarehouseAddress.trim()
      : null
  const rawRegionType = o.warehouseRegionType ?? o.WarehouseRegionType
  const warehouseRegionType =
    rawRegionType != null && rawRegionType !== '' && !Number.isNaN(Number(rawRegionType))
      ? Number(rawRegionType)
      : null
  return {
    stockOut,
    companyProfile,
    packingCode,
    packingAddresses,
    warehouseAddress,
    warehouseRegionType,
    packingLines: parsePackingLines(o)
  }
}

function normalizeReportRemarks(raw: unknown): CompanyReportInfo['invoice'] {
  const o = (raw && typeof raw === 'object' ? raw : {}) as Record<string, unknown>
  return {
    remarkCn: String(o.remarkCn ?? o.RemarkCn ?? ''),
    remarkEn: String(o.remarkEn ?? o.RemarkEn ?? '')
  }
}

function normalizeReportInfo(raw: unknown): CompanyReportInfo {
  const o = (raw && typeof raw === 'object' ? raw : {}) as Record<string, unknown>
  return {
    invoice: normalizeReportRemarks(o.invoice ?? o.Invoice),
    packingList: normalizeReportRemarks(o.packingList ?? o.PackingList)
  }
}

function parsePackingAddressPanel(raw: unknown): PackingReportAddressPanel | null {
  if (!raw || typeof raw !== 'object') return null
  const a = raw as Record<string, unknown>
  const bill = a.billToLines ?? a.BillToLines
  const ship = a.shipToLines ?? a.ShipToLines
  if (!Array.isArray(bill) && !Array.isArray(ship)) return null
  const dash = '—'
  const toLines = (arr: unknown): string[] => {
    if (!Array.isArray(arr) || arr.length === 0) return []
    return arr.map((x) => String(x ?? dash))
  }
  // 保持 API 原始行数（3 行旧格式或 4 行新格式），避免 pad 后把联系人误当地址
  return {
    billToLines: toLines(bill),
    shipToLines: toLines(ship)
  }
}

export function parsePackingBundlePayload(res: unknown, requestFlag: boolean): StockOutPackingReportBundle | null {
  const base = parseInvoiceBundlePayload(res)
  if (!base) return null
  const o = res as Record<string, unknown>
  const w = o.withShipmentInspection ?? o.WithShipmentInspection
  const withShipmentInspection = typeof w === 'boolean' ? w : requestFlag
  const packingAddresses = parsePackingAddressPanel(o.packingAddresses ?? o.PackingAddresses)
  const rawCode = o.packingCode ?? o.PackingCode
  const packingCode =
    typeof rawCode === 'string' && rawCode.trim().length > 0 ? rawCode.trim() : null
  const rawWarehouseAddress = o.warehouseAddress ?? o.WarehouseAddress
  const warehouseAddress =
    typeof rawWarehouseAddress === 'string' && rawWarehouseAddress.trim().length > 0
      ? rawWarehouseAddress.trim()
      : null
  const rawDeliveryMethod = o.deliveryMethod ?? o.DeliveryMethod
  const deliveryMethod =
    rawDeliveryMethod != null && rawDeliveryMethod !== '' && !Number.isNaN(Number(rawDeliveryMethod))
      ? Number(rawDeliveryMethod)
      : null
  return { ...base, withShipmentInspection, packingCode, packingAddresses, warehouseAddress, deliveryMethod, packingLines: parsePackingLines(o) }
}

function parsePackingLines(o: Record<string, unknown>): PackingReportLine[] {
  const raw = o.packingLines ?? o.PackingLines
  if (!Array.isArray(raw)) return []
  return raw.map((row) => {
    const r = (row && typeof row === 'object' ? row : {}) as Record<string, unknown>
    return {
      pn: r.pn != null ? String(r.pn) : r.Pn != null ? String(r.Pn) : null,
      customerPn:
        r.customerPn != null ? String(r.customerPn) : r.CustomerPn != null ? String(r.CustomerPn) : null,
      brand: r.brand != null ? String(r.brand) : r.Brand != null ? String(r.Brand) : null,
      customerBrand:
        r.customerBrand != null
          ? String(r.customerBrand)
          : r.CustomerBrand != null
            ? String(r.CustomerBrand)
            : null,
      qty: Number(r.qty ?? r.Qty ?? 0) || 0,
      carton: r.carton != null ? String(r.carton) : r.Carton != null ? String(r.Carton) : null,
      remark: r.remark != null ? String(r.remark) : r.Remark != null ? String(r.Remark) : null
    }
  })
}

async function loadStockOutCompanyBundleFallback(id: string): Promise<StockOutInvoiceReportBundle | null> {
  const stockOut = await getStockOutDetailInternal(id)
  if (!stockOut) return null
  const cp = await fetchCompanyProfileForReport()
  return {
    stockOut,
    companyProfile: {
      basicInfos: cp.basicInfos ?? [],
      bankInfos: cp.bankInfos ?? [],
      logos: cp.logos ?? [],
      seals: cp.seals ?? [],
      warehouses: cp.warehouses ?? [],
      reportInfo: cp.reportInfo ?? normalizeReportInfo(null)
    }
  }
}

async function getStockOutDetailInternal(id: string): Promise<StockOutDetailDto | null> {
  const enc = encodeURIComponent(id)
  const res = await apiClient.get<unknown>(`/api/v1/stock-out/${enc}`)
  if (res && typeof res === 'object') {
    const o = res as Record<string, unknown>
    const inner = o.data ?? o.Data
    if (inner && typeof inner === 'object') return inner as StockOutDetailDto
  }
  return (res as StockOutDetailDto) ?? null
}

/** GET /api/v1/stock-out/items 查询参数（与后端 StockOutItemListQuery 一致） */
export interface StockOutItemListQuery {
  status?: number
  stockOutCode?: string
  stockOutDateFrom?: string
  stockOutDateTo?: string
  customerName?: string
  salesUserName?: string
  purchasePn?: string
  sellOrderItemCode?: string
  /** 入库单号（子串匹配） */
  stockInCode?: string
}

export interface StockOutItemListRow {
  stockOutItemId: string
  stockOutId: string
  status: number
  stockOutCode: string
  stockOutDate: string
  customerName?: string | null
  salesUserName?: string | null
  purchasePn?: string | null
  purchaseBrand?: string | null
  outQuantity: number
  shipmentMethod?: string | null
  courierTrackingNo?: string | null
  sellOrderItemCode?: string | null
  /** 来源入库单号 */
  stockInCode?: string | null
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
  /** 出库类型：10销售 20报关 30退货 40报废 */
  stockOutType?: number
  /** 销售明细币别（1=RMB 2=USD …） */
  currency?: number
  createTime?: string
}

/** GET 出库单列表：<code>data</code> 与《翻页查询规范》一致 */
export type StockOutListPaged = { items: StockOutDto[]; total: number; page: number; pageSize: number }

/** GET 出库通知列表 */
export type StockOutRequestListPaged = { items: StockOutRequestDto[]; total: number; page: number; pageSize: number }

/** GET 出库明细列表 */
export type StockOutItemListPaged = { items: StockOutItemListRow[]; total: number; page: number; pageSize: number }

function unwrapPagedStockOuts(res: unknown): StockOutListPaged {
  const d = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  if (d && Array.isArray(d.items)) {
    return {
      items: d.items as StockOutDto[],
      total: Number(d.total ?? 0),
      page: Number(d.page ?? 1),
      pageSize: Number(d.pageSize ?? 20)
    }
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

function normalizeStockOutRequestRow(row: unknown): StockOutRequestDto {
  const r = row as Record<string, unknown>
  return {
    id: String(r.id ?? r.Id ?? ''),
    requestCode: String(r.requestCode ?? r.RequestCode ?? ''),
    salesOrderId: String(r.salesOrderId ?? r.SalesOrderId ?? ''),
    salesOrderItemId: (r.salesOrderItemId ?? r.SalesOrderItemId) as string | undefined,
    salesOrderCode: (r.salesOrderCode ?? r.SalesOrderCode) as string | undefined,
    materialModel: (r.materialModel ?? r.MaterialModel) as string | undefined,
    brand: (r.brand ?? r.Brand) as string | undefined,
    outQuantity: Number(r.outQuantity ?? r.OutQuantity ?? 0),
    expectedStockOutDate: (r.expectedStockOutDate ?? r.ExpectedStockOutDate) as string | undefined,
    salesUserName: (r.salesUserName ?? r.SalesUserName) as string | undefined,
    customerId: String(r.customerId ?? r.CustomerId ?? '').trim(),
    customerName: (r.customerName ?? r.CustomerName) as string | undefined,
    requestUserId: String(r.requestUserId ?? r.RequestUserId ?? ''),
    requestUserName: (r.requestUserName ?? r.RequestUserName) as string | undefined,
    requestDate: String(r.requestDate ?? r.RequestDate ?? ''),
    status: Number(r.status ?? r.Status ?? 0),
    remark: (r.remark ?? r.Remark) as string | undefined,
    shipmentMethod: (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined,
    regionType: r.regionType != null || r.RegionType != null ? Number(r.regionType ?? r.RegionType) : undefined,
    stockOutType:
      r.stockOutType != null || r.StockOutType != null ? Number(r.stockOutType ?? r.StockOutType) : undefined,
    currency: r.currency != null || r.Currency != null ? Number(r.currency ?? r.Currency) : undefined,
    createTime: (r.createTime ?? r.CreateTime) as string | undefined
  }
}

function unwrapPagedRequests(res: unknown): StockOutRequestListPaged {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
  if (d && Array.isArray(d.items)) {
    return {
      items: (d.items as unknown[]).map(normalizeStockOutRequestRow),
      total: Number(d.total ?? d.Total ?? 0),
      page: Number(d.page ?? d.Page ?? 1),
      pageSize: Number(d.pageSize ?? d.PageSize ?? 20)
    }
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

function unwrapPagedStockOutItems(res: unknown): StockOutItemListPaged {
  const d = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  if (d && Array.isArray(d.items)) {
    return {
      items: d.items as StockOutItemListRow[],
      total: Number(d.total ?? 0),
      page: Number(d.page ?? 1),
      pageSize: Number(d.pageSize ?? 20)
    }
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

export const stockOutApi = {
  /** 出库单列表分页（主列表页） */
  async getListPaged(params?: {
    keyword?: string
    /** 与出库来源单号精确匹配（忽略 keyword） */
    sourceCode?: string
    page?: number
    pageSize?: number
  }): Promise<StockOutListPaged> {
    const res = await apiClient.get<unknown>('/api/v1/stock-out', { params: params ?? {} })
    return unwrapPagedStockOuts(res)
  },

  /** @deprecated 请使用 {@link stockOutApi.getListPaged}；保留兼容时拉一页大页 */
  async getAll(): Promise<StockOutDto[]> {
    const p = await stockOutApi.getListPaged({ page: 1, pageSize: 2000 })
    return p.items
  },

  async searchItemsPaged(
    query?: StockOutItemListQuery & { page?: number; pageSize?: number }
  ): Promise<StockOutItemListPaged> {
    const res = await apiClient.get<unknown>('/api/v1/stock-out/items', { params: query ?? {} })
    return unwrapPagedStockOutItems(res)
  },

  /** @deprecated 请使用 {@link stockOutApi.searchItemsPaged} */
  async searchItems(query?: StockOutItemListQuery): Promise<StockOutItemListRow[]> {
    const p = await stockOutApi.searchItemsPaged({ ...query, page: 1, pageSize: 2000 })
    return p.items
  },

  async getById(id: string): Promise<StockOutDetailDto | null> {
    return getStockOutDetailInternal(id)
  },

  /**
   * 优先请求专用 bundle；若后端未部署该路由（404），则降级为「出库详情 + 公司报表参数」两请求拼装（需 purchase-order.read 以拉取公司 report-bundle）。
   */
  async getInvoiceReportBundle(id: string): Promise<StockOutInvoiceReportBundle | null> {
    const enc = encodeURIComponent(id)
    try {
      const res = await apiClient.get<unknown>(`/api/v1/stock-out/${enc}/invoice-report-bundle`)
      return parseInvoiceBundlePayload(res)
    } catch (e: unknown) {
      const status = typeof e === 'object' && e !== null ? (e as ApiRejectedError).httpStatus : undefined
      if (status !== 404) throw e
      return loadStockOutCompanyBundleFallback(id)
    }
  },

  /**
   * Packing 报表；withInspection=true 为「含出货检验」版式。
   * 若专用接口 404（旧后端），降级逻辑与 Invoice 相同，并由前端固定版式标志。
   */
  async getPackingReportBundle(
    id: string,
    withInspection: boolean,
    packingId?: string | null
  ): Promise<StockOutPackingReportBundle | null> {
    const enc = encodeURIComponent(id)
    const pid = packingId?.trim()
    try {
      const res = await apiClient.get<unknown>(`/api/v1/stock-out/${enc}/packing-report-bundle`, {
        params: {
          withInspection,
          ...(pid ? { packingId: pid } : {})
        }
      })
      return parsePackingBundlePayload(res, withInspection)
    } catch (e: unknown) {
      const status = typeof e === 'object' && e !== null ? (e as ApiRejectedError).httpStatus : undefined
      if (status !== 404) throw e
      const fb = await loadStockOutCompanyBundleFallback(id)
      return fb ? { ...fb, withShipmentInspection: withInspection } : null
    }
  },

  async updateHeader(
    id: string,
    body: { stockOutDate: string; shipmentMethod?: string | null; courierTrackingNo?: string | null }
  ): Promise<void> {
    await apiClient.patch(`/api/v1/stock-out/${id}/header`, body)
  },

  async deleteStockOut(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-out/${encodeURIComponent(id)}`)
  },

  async forceDeleteStockOut(id: string, confirmBillCode: string): Promise<void> {
    await apiClient.post(`/api/v1/stock-out/${encodeURIComponent(id)}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  },

  async getRequestListPaged(params?: {
    keyword?: string
    workflow?: string
    page?: number
    pageSize?: number
  }): Promise<StockOutRequestListPaged> {
    const res = await apiClient.get<unknown>('/api/v1/stock-out/request', { params: params ?? {} })
    return unwrapPagedRequests(res)
  },

  /** @deprecated 请使用 {@link stockOutApi.getRequestListPaged} */
  async getRequestList(): Promise<StockOutRequestDto[]> {
    const p = await stockOutApi.getRequestListPaged({ page: 1, pageSize: 2000 })
    return p.items
  },
  async deleteStockOutRequest(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-out/request/${encodeURIComponent(id)}`)
  },
  async forceDeleteStockOutRequest(id: string, confirmBillCode: string): Promise<void> {
    await apiClient.post(`/api/v1/stock-out/request/${encodeURIComponent(id)}/force-delete`, {
      confirmBillCode: confirmBillCode.trim()
    })
  },

  async getApplyContext(salesOrderId: string, salesOrderItemId: string): Promise<StockOutApplyContextDto> {
    const res = await apiClient.get<unknown>('/api/v1/stock-out/request/apply-context', {
      params: { salesOrderId, salesOrderItemId }
    })
    return normalizeApplyContextPayload(res)
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
    packingId?: string
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
  },

  async getMarkFinishContext(id: string): Promise<StockOutMarkFinishContext> {
    const enc = encodeURIComponent(id)
    const res = await apiClient.get<unknown>(`/api/v1/stock-out/${enc}/mark-finish-context`)
    const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
    const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
    const packRaw = (d?.packings ?? d?.Packings) as unknown
    const packings = Array.isArray(packRaw)
      ? packRaw.map((x) => {
          const o = x && typeof x === 'object' ? (x as Record<string, unknown>) : {}
          return {
            id: String(o.id ?? o.Id ?? ''),
            code: (o.code ?? o.Code) as string | null | undefined
          }
        })
      : []
    return {
      stockOutId: String(d?.stockOutId ?? d?.StockOutId ?? id),
      stockOutCode: (d?.stockOutCode ?? d?.StockOutCode) as string | undefined,
      customerName: (d?.customerName ?? d?.CustomerName) as string | undefined,
      shipAddress: (d?.shipAddress ?? d?.ShipAddress) as string | undefined,
      packings,
      stockOutDate: (d?.stockOutDate ?? d?.StockOutDate) as string | undefined,
      courierTrackingNo: (d?.courierTrackingNo ?? d?.CourierTrackingNo) as string | undefined,
      remark: (d?.remark ?? d?.Remark) as string | undefined
    }
  },

  async markFinished(
    id: string,
    body: { stockOutDate: string; courierTrackingNo: string; remark?: string }
  ): Promise<void> {
    await apiClient.post(`/api/v1/stock-out/${encodeURIComponent(id)}/mark-finished`, body)
  }
}
