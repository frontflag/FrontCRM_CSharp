import apiClient, { type ApiRejectedError } from './client'
import { fetchCompanyProfileForReport, type CompanyProfileBundle } from '@/api/companyProfile'

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

/** GET /api/v1/stock-out/:id/invoice-report-bundle（打印页：出库详情 + 公司参数） */
export interface StockOutInvoiceReportBundle {
  stockOut: StockOutDetailDto
  companyProfile: CompanyProfileBundle
}

/** GET /api/v1/stock-out/:id/packing-report-bundle?withInspection=… */
export interface StockOutPackingReportBundle extends StockOutInvoiceReportBundle {
  withShipmentInspection: boolean
}

function parseInvoiceBundlePayload(res: unknown): StockOutInvoiceReportBundle | null {
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
    warehouses: (rawCp.warehouses ?? rawCp.Warehouses ?? []) as CompanyProfileBundle['warehouses']
  }
  return { stockOut, companyProfile }
}

function parsePackingBundlePayload(res: unknown, requestFlag: boolean): StockOutPackingReportBundle | null {
  const base = parseInvoiceBundlePayload(res)
  if (!base) return null
  const o = res as Record<string, unknown>
  const w = o.withShipmentInspection ?? o.WithShipmentInspection
  const withShipmentInspection = typeof w === 'boolean' ? w : requestFlag
  return { ...base, withShipmentInspection }
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
      warehouses: cp.warehouses ?? []
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
  async getPackingReportBundle(id: string, withInspection: boolean): Promise<StockOutPackingReportBundle | null> {
    const enc = encodeURIComponent(id)
    try {
      const res = await apiClient.get<unknown>(`/api/v1/stock-out/${enc}/packing-report-bundle`, {
        params: { withInspection }
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
