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
  /** 关联装箱单编号，多个以逗号拼接 */
  packingCodes?: string | null
  totalQuantity: number
  totalAmount: number
  status: number
  remark?: string
  createTime?: string
  createUserName?: string
  customerName?: string
  customerEnglishName?: string
  customerCode?: string
  salesUserName?: string
  sellOrderItemCode?: string
  /** 出货方式（字典 LogisticsArrivalMethod ItemCode） */
  shipmentMethod?: string | null
  /** 快递公司（字典 LogisticsExpressMethod ItemCode） */
  expressCompany?: string | null
  courierTrackingNo?: string | null
  /** 货代单号（关联采购订单，多单逗号拼接） */
  freightForwarderOrderNo?: string | null
  /** 报关出库单关联的原销售出库通知 Id */
  salesStockOutNotifyId?: string | null
  /** 报关出库单关联的原销售出库通知单号 */
  salesStockOutNotifyCode?: string | null
  /** 关联报关单主键（报关出库 Type=20） */
  customsDeclarationId?: string | null
  /** 关联报关单号 */
  customsDeclarationCode?: string | null
}

export interface StockOutCustomsSummaryDto {
  declarationId: string
  declarationCode: string
  customsBrokerId?: string | null
  customsBrokerName?: string | null
  customsClearanceStatus?: number | null
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
  /** 仓库名称（服务端由 WarehouseId 解析） */
  warehouseName?: string | null
  sellOrderItemId?: string
  /** 关联销售订单主键 */
  sellOrderId?: string
  /** 关联销售订单号 */
  sellOrderCode?: string
  customsSummary?: StockOutCustomsSummaryDto | null
}

export interface StockOutApplyRegionInventoryDto {
  /** 10=大陆仓 20=海外仓 */
  regionType: number
  hasInventory: boolean
  availableQty: number
}

export interface StockOutApplyStockingRegionAvailabilityDto {
  regionType: number
  isAvailable: boolean
}

export interface StockOutApplyCustomsOptionDto {
  visible: boolean
  defaultChecked: boolean
  locked: boolean
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
  customerOrderInventoryByRegion?: StockOutApplyRegionInventoryDto[]
  stockingAvailabilityByRegion?: StockOutApplyStockingRegionAvailabilityDto[]
  evaluatedRequestedQty?: number
  customsOption?: StockOutApplyCustomsOptionDto
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
  const parseRegionInventory = (raw: unknown): StockOutApplyRegionInventoryDto[] => {
    if (!Array.isArray(raw)) return []
    return raw.map((row) => {
      const r = row as Record<string, unknown>
      return {
        regionType: truncInt(r.regionType ?? r.RegionType),
        hasInventory: Boolean(r.hasInventory ?? r.HasInventory),
        availableQty: truncInt(r.availableQty ?? r.AvailableQty)
      }
    })
  }
  const parseStockingAvailability = (raw: unknown): StockOutApplyStockingRegionAvailabilityDto[] => {
    if (!Array.isArray(raw)) return []
    return raw.map((row) => {
      const r = row as Record<string, unknown>
      return {
        regionType: truncInt(r.regionType ?? r.RegionType),
        isAvailable: Boolean(r.isAvailable ?? r.IsAvailable)
      }
    })
  }
  const rawCustoms = (o.customsOption ?? o.CustomsOption) as Record<string, unknown> | undefined
  const customsOption: StockOutApplyCustomsOptionDto = rawCustoms
    ? {
        visible: Boolean(rawCustoms.visible ?? rawCustoms.Visible),
        defaultChecked: Boolean(rawCustoms.defaultChecked ?? rawCustoms.DefaultChecked),
        locked: Boolean(rawCustoms.locked ?? rawCustoms.Locked)
      }
    : { visible: false, defaultChecked: false, locked: false }

  return {
    salesOrderItemId: String(o.salesOrderItemId ?? o.SalesOrderItemId ?? ''),
    salesOrderQty: num(o.salesOrderQty ?? o.SalesOrderQty),
    alreadyNotifiedQty: num(o.alreadyNotifiedQty ?? o.AlreadyNotifiedQty),
    remainingNotifyQty: num(o.remainingNotifyQty ?? o.RemainingNotifyQty),
    availableStockQty: num(o.availableStockQty ?? o.AvailableStockQty),
    purchasedStockAvailableQty: truncInt(o.purchasedStockAvailableQty ?? o.PurchasedStockAvailableQty),
    suggestedMaxQty: num(o.suggestedMaxQty ?? o.SuggestedMaxQty),
    customerOrderInventoryByRegion: parseRegionInventory(
      o.customerOrderInventoryByRegion ?? o.CustomerOrderInventoryByRegion
    ),
    stockingAvailabilityByRegion: parseStockingAvailability(
      o.stockingAvailabilityByRegion ?? o.StockingAvailabilityByRegion
    ),
    evaluatedRequestedQty: truncInt(o.evaluatedRequestedQty ?? o.EvaluatedRequestedQty),
    customsOption
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
  /** 装箱单出货方式 packing_extend_ship.shipment_method */
  shipmentMethod?: string | null
  /** @deprecated 请使用 shipmentMethod */
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
  const shipmentMethod = (o.shipmentMethod ?? o.ShipmentMethod) as string | null | undefined
  return { ...base, withShipmentInspection, packingCode, packingAddresses, warehouseAddress, shipmentMethod, deliveryMethod, packingLines: parsePackingLines(o) }
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

export function normalizeStockOutCustomsSummary(raw: unknown): StockOutCustomsSummaryDto | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as Record<string, unknown>
  const declarationId = String(r.declarationId ?? r.DeclarationId ?? '').trim()
  if (!declarationId) return null
  return {
    declarationId,
    declarationCode: String(r.declarationCode ?? r.DeclarationCode ?? '').trim(),
    customsBrokerId: (r.customsBrokerId ?? r.CustomsBrokerId) as string | null | undefined,
    customsBrokerName: (r.customsBrokerName ?? r.CustomsBrokerName) as string | null | undefined,
    customsClearanceStatus:
      r.customsClearanceStatus != null || r.CustomsClearanceStatus != null
        ? Number(r.customsClearanceStatus ?? r.CustomsClearanceStatus)
        : null
  }
}

function normalizeStockOutDetailRow(row: unknown): StockOutDetailDto {
  const base = normalizeStockOutListRow(row)
  const r = row as Record<string, unknown>
  return {
    ...base,
    warehouseId: (r.warehouseId ?? r.WarehouseId) as string | undefined,
    warehouseCode: (r.warehouseCode ?? r.WarehouseCode) as string | null | undefined,
    warehouseName: (r.warehouseName ?? r.WarehouseName) as string | null | undefined,
    sellOrderItemId: (r.sellOrderItemId ?? r.SellOrderItemId) as string | undefined,
    sellOrderId: (r.sellOrderId ?? r.SellOrderId) as string | undefined,
    sellOrderCode: (r.sellOrderCode ?? r.SellOrderCode) as string | undefined,
    customsSummary: normalizeStockOutCustomsSummary(r.customsSummary ?? r.CustomsSummary)
  }
}

async function getStockOutDetailInternal(id: string): Promise<StockOutDetailDto | null> {
  const enc = encodeURIComponent(id)
  const res = await apiClient.get<unknown>(`/api/v1/stock-out/${enc}`)
  if (res && typeof res === 'object') {
    const o = res as Record<string, unknown>
    const inner = o.data ?? o.Data
    if (inner && typeof inner === 'object') return normalizeStockOutDetailRow(inner)
  }
  return res ? normalizeStockOutDetailRow(res) : null
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
  /** 装箱单号（子串匹配） */
  packingCode?: string
  freightForwarderOrderNo?: string
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
  packingId?: string | null
  packingCode?: string | null
  freightForwarderOrderNo?: string | null
  /** 销售单价（出库扩展快照或销售订单明细单价） */
  salesPrice?: number | null
  /** 销售币别（1=RMB 2=USD …） */
  salesCurrency?: number | null
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
  /** 报关状态：0未知 10无需报关 20待报关 30报关中 100报关完成 */
  customsStatus?: number
  remark?: string
  /** 出货方式（字典 LogisticsArrivalMethod ItemCode） */
  shipmentMethod?: string | null
  /** 快递公司（字典 LogisticsExpressMethod ItemCode） */
  expressCompany?: string | null
  /** 关联装箱单 Id */
  packingId?: string | null
  /** 关联装箱单号 packing.code */
  packingCode?: string | null
  /** RegionType：10=境内 20=境外（与仓库、到货通知共用） */
  regionType?: number
  /** 出库类型：10销售 20报关 30退货 40报废 */
  stockOutType?: number
  /** 报关出库通知关联的原销售出库通知 Id */
  salesStockOutNotifyId?: string | null
  /** 报关出库通知关联的原销售出库通知单号 */
  salesStockOutNotifyCode?: string | null
  /** 销售明细币别（1=RMB 2=USD …） */
  currency?: number
  createTime?: string
  /** 关联报关单主键（报关出库通知 Type=20） */
  customsDeclarationId?: string | null
  /** 关联报关单号 */
  customsDeclarationCode?: string | null
  /** 报关公司名称（展示用） */
  customsBrokerName?: string | null
}

/** GET 出库单列表：<code>data</code> 与《翻页查询规范》一致 */
export type StockOutListPaged = { items: StockOutDto[]; total: number; page: number; pageSize: number }

/** GET /api/v1/stock-out 列表筛选 */
export type StockOutListQuery = {
  keyword?: string
  sourceCode?: string
  status?: number
  stockOutCode?: string
  packingCode?: string
  shipmentMethod?: string
  customerName?: string
  salesUserName?: string
  remark?: string
  freightForwarderOrderNo?: string
  stockOutType?: number
  stockOutDateFrom?: string
  stockOutDateTo?: string
  page?: number
  pageSize?: number
}

/** GET 出库通知列表 */
export type StockOutRequestListPaged = { items: StockOutRequestDto[]; total: number; page: number; pageSize: number }

/** GET 出库明细列表 */
export type StockOutItemListPaged = { items: StockOutItemListRow[]; total: number; page: number; pageSize: number }

export function normalizeStockOutListRow(row: unknown): StockOutDto {
  const r = row as Record<string, unknown>
  return {
    id: String(r.id ?? r.Id ?? ''),
    stockOutCode: String(r.stockOutCode ?? r.StockOutCode ?? ''),
    stockOutType: Number(r.stockOutType ?? r.StockOutType ?? 0),
    sourceCode: (r.sourceCode ?? r.SourceCode) as string | undefined,
    warehouseId: (r.warehouseId ?? r.WarehouseId) as string | undefined,
    stockOutDate: String(r.stockOutDate ?? r.StockOutDate ?? ''),
    expectedStockOutDate: (r.expectedStockOutDate ?? r.ExpectedStockOutDate) as string | null | undefined,
    packingCount: r.packingCount != null || r.PackingCount != null ? Number(r.packingCount ?? r.PackingCount) : undefined,
    packingCodes: (r.packingCodes ?? r.PackingCodes) as string | null | undefined,
    totalQuantity: Number(r.totalQuantity ?? r.TotalQuantity ?? 0),
    totalAmount: Number(r.totalAmount ?? r.TotalAmount ?? 0),
    status: Number(r.status ?? r.Status ?? 0),
    remark: (r.remark ?? r.Remark) as string | undefined,
    createTime: (r.createTime ?? r.CreateTime) as string | undefined,
    createUserName: (r.createUserName ?? r.CreateUserName) as string | undefined,
    customerName: (r.customerName ?? r.CustomerName) as string | undefined,
    customerEnglishName: (r.customerEnglishName ?? r.CustomerEnglishName) as string | undefined,
    customerCode: (r.customerCode ?? r.CustomerCode) as string | undefined,
    salesUserName: (r.salesUserName ?? r.SalesUserName) as string | undefined,
    sellOrderItemCode: (r.sellOrderItemCode ?? r.SellOrderItemCode) as string | undefined,
    shipmentMethod: (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined,
    expressCompany: (r.expressCompany ?? r.ExpressCompany) as string | null | undefined,
    courierTrackingNo: (r.courierTrackingNo ?? r.CourierTrackingNo) as string | null | undefined,
    freightForwarderOrderNo: (r.freightForwarderOrderNo ?? r.FreightForwarderOrderNo) as string | null | undefined,
    salesStockOutNotifyId: (r.salesStockOutNotifyId ?? r.SalesStockOutNotifyId) as string | null | undefined,
    salesStockOutNotifyCode: (r.salesStockOutNotifyCode ?? r.SalesStockOutNotifyCode) as string | null | undefined,
    customsDeclarationId: (r.customsDeclarationId ?? r.CustomsDeclarationId) as string | null | undefined,
    customsDeclarationCode: (r.customsDeclarationCode ?? r.CustomsDeclarationCode) as string | null | undefined
  }
}

function unwrapPagedStockOuts(res: unknown): StockOutListPaged {
  const d = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  if (d && Array.isArray(d.items)) {
    return {
      items: (d.items as unknown[]).map(normalizeStockOutListRow),
      total: Number(d.total ?? 0),
      page: Number(d.page ?? 1),
      pageSize: Number(d.pageSize ?? 20)
    }
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

export function normalizeStockOutRequestRow(row: unknown): StockOutRequestDto {
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
    customsStatus: Number(r.customsStatus ?? r.CustomsStatus ?? 0),
    remark: (r.remark ?? r.Remark) as string | undefined,
    shipmentMethod: (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined,
    expressCompany: (r.expressCompany ?? r.ExpressCompany) as string | null | undefined,
    packingId: (r.packingId ?? r.PackingId) as string | null | undefined,
    packingCode: (r.packingCode ?? r.PackingCode) as string | null | undefined,
    regionType: r.regionType != null || r.RegionType != null ? Number(r.regionType ?? r.RegionType) : undefined,
    stockOutType:
      r.stockOutType != null || r.StockOutType != null ? Number(r.stockOutType ?? r.StockOutType) : undefined,
    salesStockOutNotifyId: (r.salesStockOutNotifyId ?? r.SalesStockOutNotifyId) as string | null | undefined,
    salesStockOutNotifyCode: (r.salesStockOutNotifyCode ?? r.SalesStockOutNotifyCode) as string | null | undefined,
    currency: r.currency != null || r.Currency != null ? Number(r.currency ?? r.Currency) : undefined,
    createTime: (r.createTime ?? r.CreateTime) as string | undefined,
    customsDeclarationId: (r.customsDeclarationId ?? r.CustomsDeclarationId) as string | null | undefined,
    customsDeclarationCode: (r.customsDeclarationCode ?? r.CustomsDeclarationCode) as string | null | undefined,
    customsBrokerName: (r.customsBrokerName ?? r.CustomsBrokerName) as string | null | undefined
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

function normalizeStockOutItemListRow(row: unknown): StockOutItemListRow {
  const r = row as Record<string, unknown>
  return {
    stockOutItemId: String(r.stockOutItemId ?? r.StockOutItemId ?? ''),
    stockOutId: String(r.stockOutId ?? r.StockOutId ?? ''),
    status: Number(r.status ?? r.Status ?? 0),
    stockOutCode: String(r.stockOutCode ?? r.StockOutCode ?? ''),
    stockOutDate: String(r.stockOutDate ?? r.StockOutDate ?? ''),
    customerName: (r.customerName ?? r.CustomerName) as string | null | undefined,
    salesUserName: (r.salesUserName ?? r.SalesUserName) as string | null | undefined,
    purchasePn: (r.purchasePn ?? r.PurchasePn) as string | null | undefined,
    purchaseBrand: (r.purchaseBrand ?? r.PurchaseBrand) as string | null | undefined,
    outQuantity: Number(r.outQuantity ?? r.OutQuantity ?? 0),
    shipmentMethod: (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined,
    courierTrackingNo: (r.courierTrackingNo ?? r.CourierTrackingNo) as string | null | undefined,
    sellOrderItemCode: (r.sellOrderItemCode ?? r.SellOrderItemCode) as string | null | undefined,
    stockInCode: (r.stockInCode ?? r.StockInCode) as string | null | undefined,
    packingId: (r.packingId ?? r.PackingId) as string | null | undefined,
    packingCode: (r.packingCode ?? r.PackingCode) as string | null | undefined,
    freightForwarderOrderNo: (r.freightForwarderOrderNo ?? r.FreightForwarderOrderNo) as string | null | undefined,
    salesPrice:
      r.salesPrice != null || r.SalesPrice != null ? Number(r.salesPrice ?? r.SalesPrice) : null,
    salesCurrency:
      r.salesCurrency != null || r.SalesCurrency != null ? Number(r.salesCurrency ?? r.SalesCurrency) : null
  }
}

function unwrapPagedStockOutItems(res: unknown): StockOutItemListPaged {
  const d = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  if (d && Array.isArray(d.items)) {
    return {
      items: (d.items as unknown[]).map(normalizeStockOutItemListRow),
      total: Number(d.total ?? 0),
      page: Number(d.page ?? 1),
      pageSize: Number(d.pageSize ?? 20)
    }
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

export const stockOutApi = {
  /** 出库单列表分页（主列表页） */
  async getListPaged(params?: StockOutListQuery): Promise<StockOutListPaged> {
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
    status?: number
    regionType?: number
    customerName?: string
    salesUserName?: string
    materialModel?: string
    requestDateFrom?: string
    requestDateTo?: string
    stockOutType?: number
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

  async getApplyContext(
    salesOrderId: string,
    salesOrderItemId: string,
    requestedQty?: number
  ): Promise<StockOutApplyContextDto> {
    const params: Record<string, string | number> = { salesOrderId, salesOrderItemId }
    if (requestedQty != null && Number.isFinite(requestedQty) && requestedQty > 0) {
      params.requestedQty = Math.trunc(requestedQty)
    }
    const res = await apiClient.get<unknown>('/api/v1/stock-out/request/apply-context', { params })
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
    expressCompany?: string | null
    regionType?: number
    useOverseasWarehouseAndCustoms?: boolean
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
