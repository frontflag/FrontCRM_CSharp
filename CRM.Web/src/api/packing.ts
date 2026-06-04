import apiClient from './client'
import { parsePackingBundlePayload, parseInvoiceBundlePayload, type StockOutPackingReportBundle, type PackingReportLine, type StockOutInvoiceReportBundle } from './stockOut'

/** 装箱单详情明细 → Packing 报表明细行（与后端 MapPackingLines 字段一致） */
export function packingDetailItemsToReportLines(items: PackingDetailLine[] | undefined | null): PackingReportLine[] {
  if (!items?.length) return []
  return items.map((item) => ({
    pn: item.pn ?? null,
    customerPn: item.customerPn ?? null,
    brand: item.brand ?? null,
    customerBrand: item.customerBrand ?? null,
    qty: Number(item.qty) || 0,
    carton: null,
    remark: item.comment ?? null
  }))
}

export interface PackingListItem {
  id: string
  code: string
  status: number
  stockOutType: number
  materialType: number
  customerId?: string | null
  customerName?: string | null
  salesId?: string | null
  salesUserName?: string | null
  storageId?: string | null
  warehouseName?: string | null
  itemRows: number
  comment?: string | null
  scheduleShipDate?: string | null
  /** 关联出库通知的计划出货日期 */
  requestDate?: string | null
  /** 关联出库通知的出货方式（字典 ItemCode） */
  shipmentMethod?: string | null
  /** 快递公司（字典 ItemCode） */
  expressCompany?: string | null
  createTime: string
  createByUserId?: string | null
  createUserName?: string | null
  /** packing_extend_ship.ship_company */
  shipCompany?: string | null
  /** packing_extend_ship.ship_address */
  shipAddress?: string | null
}

export interface PackingItemListRow {
  id: string
  packingId: string
  packingCode: string
  packingStatus: number
  pn?: string | null
  brand?: string | null
  qty: number
  unit?: string | null
  sellOrderId?: string | null
  sellOrderItemId?: string | null
  sellOrderCode?: string | null
  sellOrderItemCode?: string | null
  itemCode?: string | null
  customerName?: string | null
  createTime: string
}

export interface PackingCreateResult {
  packingId: string
  packingCode: string
  itemCount: number
}

export interface PackingExtendShipInput {
  shipCompany?: string | null
  shipAddress?: string | null
  shipAttn?: string | null
  shipTel?: string | null
  billCompany?: string | null
  billAddress?: string | null
  billAttn?: string | null
  billTel?: string | null
  deliveryReq?: string | null
  shipmentMethod?: string | null
  expressCompany?: string | null
  /** @deprecated 请使用 shipmentMethod */
  deliveryMethod?: number | null
}

export interface PackingExtendBoxInput {
  nw?: number | null
  gw?: number | null
  dim?: string | null
  ctns?: number | null
}

export interface PackingCreateExtras {
  ship?: PackingExtendShipInput
  box?: PackingExtendBoxInput
  comment?: string | null
  scheduleShipDate?: string | null
  /** 报关装箱（StockOutType=20）必填 */
  customsBrokerId?: string | null
}

export interface PackingDraftLine {
  stockOutRequestId: string
  requestCode?: string | null
  pn?: string | null
  brand?: string | null
  qty: number
  unit?: string | null
  sellOrderId?: string | null
  sellOrderItemId?: string | null
  sellOrderCode?: string | null
  sellOrderItemCode?: string | null
  remark?: string | null
}

export interface PackingDraftFromStockOutRequests {
  customerId: string
  customerName?: string | null
  salesId?: string | null
  salesUserName?: string | null
  /** 拟生成装箱单出库类型（与入参顺序第一条出库通知一致） */
  stockOutType?: number
  /** 由送达地域解析的出库仓库（写入 packing.storage_id） */
  warehouseId?: string | null
  warehouseName?: string | null
  shipmentMethod?: string | null
  expressCompany?: string | null
  lines: PackingDraftLine[]
}

export interface PackingDetailItemExtend {
  id: string
  packingItemId: string
  customerId?: string | null
  customerName?: string | null
  salesId?: string | null
  salesUserName?: string | null
  sellOrderId?: string | null
  sellOrderCode?: string | null
  sellOrderItemId?: string | null
  sellOrderItemCode?: string | null
  price?: number | null
  priceCurrency?: number | null
  priceConvertPrice?: number | null
  customerSo?: string | null
  customerPn?: string | null
  customerBrand?: string | null
}

/** 装箱单详情内嵌出库通知行 */
export interface PackingStockOutNotifyRow {
  id: string
  requestCode: string
  status: number
  salesOrderId?: string | null
  salesOrderCode?: string | null
  salesOrderItemId?: string | null
  materialModel?: string | null
  brand?: string | null
  outQuantity: number
  regionType?: number
  customerName?: string | null
  salesUserName?: string | null
  requestDate: string
  createTime: string
  remark?: string | null
}

export interface PackingDetailLine {
  id: string
  pn?: string | null
  brand?: string | null
  qty: number
  unit?: string | null
  sellOrderId?: string | null
  sellOrderItemId?: string | null
  stockOutNotifyId?: string | null
  sellOrderCode?: string | null
  sellOrderItemCode?: string | null
  itemCode?: string | null
  customerSo?: string | null
  customerPn?: string | null
  customerBrand?: string | null
  price?: number | null
  priceCurrency?: number | null
  comment?: string | null
}

export interface PackingDetail {
  id: string
  code: string
  status: number
  stockOutType: number
  materialType: number
  customerId?: string | null
  customerName?: string | null
  salesId?: string | null
  salesUserName?: string | null
  itemRows: number
  scheduleShipDate?: string | null
  comment?: string | null
  createTime: string
  boxNw?: number | null
  boxGw?: number | null
  boxDim?: string | null
  boxCtns?: number | null
  shipCompany?: string | null
  shipAddress?: string | null
  shipAttn?: string | null
  shipTel?: string | null
  billCompany?: string | null
  billAddress?: string | null
  billAttn?: string | null
  billTel?: string | null
  deliveryReq?: string | null
  shipmentMethod?: string | null
  expressCompany?: string | null
  /** @deprecated 请使用 shipmentMethod */
  deliveryMethod?: number | null
  items: PackingDetailLine[]
  itemExtends: PackingDetailItemExtend[]
  stockOutNotifies: PackingStockOutNotifyRow[]
}

export type PackingListPaged = { items: PackingListItem[]; total: number; page: number; pageSize: number }

export interface PackingListQuery {
  packingCode?: string
  status?: number
  stockOutType?: number
  materialType?: number
  customerName?: string
  salesUserName?: string
  createTimeFrom?: string
  createTimeTo?: string
  page?: number
  pageSize?: number
}

export const PACKING_STATUS_FILTER_VALUES = [10, 20, 30, 40, 50, 100] as const

/** 与后端 <see cref="PackingStatusCode"/> 一致 */
export const PackingStatusCode = {
  New: 10,
  Confirmed: 20,
  Picked: 30,
  Ready: 40,
  PendingStockOut: 50,
  StockOutFinished: 100
} as const
export {
  StockOutTypeCode,
  STOCK_OUT_TYPE_FILTER_VALUES as PACKING_STOCK_OUT_TYPE_FILTER_VALUES,
  stockOutTypeLabel,
  stockOutTypeLabel as packingStockOutTypeLabel
} from '@/constants/stockOutType'
export const PACKING_MATERIAL_TYPE_FILTER_VALUES = [10, 20, 30] as const
export type PackingItemListPaged = { items: PackingItemListRow[]; total: number; page: number; pageSize: number }

function normalizePackingListItem(row: unknown): PackingListItem {
  const r = row as Record<string, unknown>
  return {
    id: String(r.id ?? r.Id ?? ''),
    code: String(r.code ?? r.Code ?? ''),
    status: Number(r.status ?? r.Status ?? 0),
    stockOutType: Number(r.stockOutType ?? r.StockOutType ?? 0),
    materialType: Number(r.materialType ?? r.MaterialType ?? 0),
    customerId: (r.customerId ?? r.CustomerId) as string | null | undefined,
    customerName: (r.customerName ?? r.CustomerName) as string | null | undefined,
    salesId: (r.salesId ?? r.SalesId) as string | null | undefined,
    salesUserName: (r.salesUserName ?? r.SalesUserName) as string | null | undefined,
    storageId: (r.storageId ?? r.StorageId) as string | null | undefined,
    warehouseName: (r.warehouseName ?? r.WarehouseName) as string | null | undefined,
    itemRows: Number(r.itemRows ?? r.ItemRows ?? 0),
    comment: (r.comment ?? r.Comment) as string | null | undefined,
    scheduleShipDate: (r.scheduleShipDate ?? r.ScheduleShipDate) as string | null | undefined,
    requestDate: (r.requestDate ?? r.RequestDate) as string | null | undefined,
    shipmentMethod: (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined,
    expressCompany: (r.expressCompany ?? r.ExpressCompany) as string | null | undefined,
    createTime: String(r.createTime ?? r.CreateTime ?? ''),
    createByUserId: (r.createByUserId ?? r.CreateByUserId) as string | null | undefined,
    createUserName: (r.createUserName ?? r.CreateUserName) as string | null | undefined,
    shipCompany: (r.shipCompany ?? r.ShipCompany) as string | null | undefined,
    shipAddress: (r.shipAddress ?? r.ShipAddress) as string | null | undefined
  }
}

function unwrapPaged<T>(res: unknown, pick: (d: Record<string, unknown>) => T[]): PackingListPaged {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
  if (d && Array.isArray(d.items)) {
    return {
      items: pick(d) as T[] & PackingListItem[],
      total: Number(d.total ?? 0),
      page: Number(d.page ?? 1),
      pageSize: Number(d.pageSize ?? 20)
    } as PackingListPaged
  }
  return { items: [], total: 0, page: 1, pageSize: 20 }
}

function unwrapDetail(res: unknown): PackingDetail | null {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
  if (!d) return null
  const id = String(d.id ?? d.Id ?? '').trim()
  if (!id) return null
  const rawItems = (d.items ?? d.Items) as unknown[] | undefined
  const items: PackingDetailLine[] = Array.isArray(rawItems)
    ? rawItems.map((row) => {
        const r = row as Record<string, unknown>
        return {
          id: String(r.id ?? r.Id ?? ''),
          pn: (r.pn ?? r.Pn) as string | null | undefined,
          brand: (r.brand ?? r.Brand) as string | null | undefined,
          qty: Number(r.qty ?? r.Qty ?? 0),
          unit: (r.unit ?? r.Unit) as string | null | undefined,
          sellOrderId: (r.sellOrderId ?? r.SellOrderId) as string | null | undefined,
          sellOrderItemId: (r.sellOrderItemId ?? r.SellOrderItemId) as string | null | undefined,
          stockOutNotifyId: (r.stockOutNotifyId ?? r.StockOutNotifyId) as string | null | undefined,
          sellOrderCode: (r.sellOrderCode ?? r.SellOrderCode) as string | null | undefined,
          sellOrderItemCode: (r.sellOrderItemCode ?? r.SellOrderItemCode) as string | null | undefined,
          itemCode: (r.itemCode ?? r.ItemCode) as string | null | undefined,
          customerSo: (r.customerSo ?? r.CustomerSo) as string | null | undefined,
          customerPn: (r.customerPn ?? r.CustomerPn) as string | null | undefined,
          customerBrand: (r.customerBrand ?? r.CustomerBrand) as string | null | undefined,
          price: r.price != null || r.Price != null ? Number(r.price ?? r.Price) : null,
          priceCurrency:
            r.priceCurrency != null || r.PriceCurrency != null
              ? Number(r.priceCurrency ?? r.PriceCurrency)
              : null,
          comment: (r.comment ?? r.Comment) as string | null | undefined
        }
      })
    : []
  const rawExtends = (d.itemExtends ?? d.ItemExtends) as unknown[] | undefined
  const itemExtends: PackingDetailItemExtend[] = Array.isArray(rawExtends)
    ? rawExtends.map((row) => {
        const r = row as Record<string, unknown>
        return {
          id: String(r.id ?? r.Id ?? ''),
          packingItemId: String(r.packingItemId ?? r.PackingItemId ?? ''),
          customerId: (r.customerId ?? r.CustomerId) as string | null | undefined,
          customerName: (r.customerName ?? r.CustomerName) as string | null | undefined,
          salesId: (r.salesId ?? r.SalesId) as string | null | undefined,
          salesUserName: (r.salesUserName ?? r.SalesUserName) as string | null | undefined,
          sellOrderId: (r.sellOrderId ?? r.SellOrderId) as string | null | undefined,
          sellOrderCode: (r.sellOrderCode ?? r.SellOrderCode) as string | null | undefined,
          sellOrderItemId: (r.sellOrderItemId ?? r.SellOrderItemId) as string | null | undefined,
          sellOrderItemCode: (r.sellOrderItemCode ?? r.SellOrderItemCode) as string | null | undefined,
          price: r.price != null || r.Price != null ? Number(r.price ?? r.Price) : null,
          priceCurrency:
            r.priceCurrency != null || r.PriceCurrency != null
              ? Number(r.priceCurrency ?? r.PriceCurrency)
              : null,
          priceConvertPrice:
            r.priceConvertPrice != null || r.PriceConvertPrice != null
              ? Number(r.priceConvertPrice ?? r.PriceConvertPrice)
              : null,
          customerSo: (r.customerSo ?? r.CustomerSo) as string | null | undefined,
          customerPn: (r.customerPn ?? r.CustomerPn) as string | null | undefined,
          customerBrand: (r.customerBrand ?? r.CustomerBrand) as string | null | undefined
        }
      })
    : []
  return {
    id,
    code: String(d.code ?? d.Code ?? ''),
    status: Number(d.status ?? d.Status ?? 0),
    stockOutType: Number(d.stockOutType ?? d.StockOutType ?? 0),
    materialType: Number(d.materialType ?? d.MaterialType ?? 0),
    customerId: (d.customerId ?? d.CustomerId) as string | null | undefined,
    customerName: (d.customerName ?? d.CustomerName) as string | null | undefined,
    salesId: (d.salesId ?? d.SalesId) as string | null | undefined,
    salesUserName: (d.salesUserName ?? d.SalesUserName) as string | null | undefined,
    itemRows: Number(d.itemRows ?? d.ItemRows ?? 0),
    scheduleShipDate: (d.scheduleShipDate ?? d.ScheduleShipDate) as string | null | undefined,
    comment: (d.comment ?? d.Comment) as string | null | undefined,
    createTime: String(d.createTime ?? d.CreateTime ?? ''),
    boxNw: d.boxNw != null || d.BoxNw != null ? Number(d.boxNw ?? d.BoxNw) : null,
    boxGw: d.boxGw != null || d.BoxGw != null ? Number(d.boxGw ?? d.BoxGw) : null,
    boxDim: (d.boxDim ?? d.BoxDim) as string | null | undefined,
    boxCtns: d.boxCtns != null || d.BoxCtns != null ? Number(d.boxCtns ?? d.BoxCtns) : null,
    shipCompany: (d.shipCompany ?? d.ShipCompany) as string | null | undefined,
    shipAddress: (d.shipAddress ?? d.ShipAddress) as string | null | undefined,
    shipAttn: (d.shipAttn ?? d.ShipAttn) as string | null | undefined,
    shipTel: (d.shipTel ?? d.ShipTel) as string | null | undefined,
    billCompany: (d.billCompany ?? d.BillCompany) as string | null | undefined,
    billAddress: (d.billAddress ?? d.BillAddress) as string | null | undefined,
    billAttn: (d.billAttn ?? d.BillAttn) as string | null | undefined,
    billTel: (d.billTel ?? d.BillTel) as string | null | undefined,
    deliveryReq: (d.deliveryReq ?? d.DeliveryReq) as string | null | undefined,
    shipmentMethod: (d.shipmentMethod ?? d.ShipmentMethod) as string | null | undefined,
    expressCompany: (d.expressCompany ?? d.ExpressCompany) as string | null | undefined,
    deliveryMethod:
      d.deliveryMethod != null || d.DeliveryMethod != null
        ? Number(d.deliveryMethod ?? d.DeliveryMethod)
        : null,
    items,
    itemExtends,
    stockOutNotifies: unwrapPackingStockOutNotifies(d)
  }
}

function unwrapPackingStockOutNotifies(d: Record<string, unknown>): PackingStockOutNotifyRow[] {
  const raw = (d.stockOutNotifies ?? d.StockOutNotifies) as unknown[] | undefined
  if (!Array.isArray(raw)) return []
  return raw.map((row) => {
    const r = row as Record<string, unknown>
    return {
      id: String(r.id ?? r.Id ?? ''),
      requestCode: String(r.requestCode ?? r.RequestCode ?? ''),
      status: Number(r.status ?? r.Status ?? 0),
      salesOrderId: (r.salesOrderId ?? r.SalesOrderId) as string | null | undefined,
      salesOrderCode: (r.salesOrderCode ?? r.SalesOrderCode) as string | null | undefined,
      salesOrderItemId: (r.salesOrderItemId ?? r.SalesOrderItemId) as string | null | undefined,
      materialModel: (r.materialModel ?? r.MaterialModel) as string | null | undefined,
      brand: (r.brand ?? r.Brand) as string | null | undefined,
      outQuantity: Number(r.outQuantity ?? r.OutQuantity ?? 0),
      regionType: r.regionType != null || r.RegionType != null ? Number(r.regionType ?? r.RegionType) : undefined,
      customerName: (r.customerName ?? r.CustomerName) as string | null | undefined,
      salesUserName: (r.salesUserName ?? r.SalesUserName) as string | null | undefined,
      requestDate: String(r.requestDate ?? r.RequestDate ?? ''),
      createTime: String(r.createTime ?? r.CreateTime ?? ''),
      remark: (r.remark ?? r.Remark) as string | null | undefined
    }
  })
}

function unwrapCreate(res: unknown): PackingCreateResult | null {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
  if (!d) return null
  const id = String(d.packingId ?? d.PackingId ?? '')
  if (!id) return null
  return {
    packingId: id,
    packingCode: String(d.packingCode ?? d.PackingCode ?? ''),
    itemCount: Number(d.itemCount ?? d.ItemCount ?? 0)
  }
}

function parseApiError(e: unknown, fallback: string): string {
  if (e && typeof e === 'object') {
    const o = e as Record<string, unknown>
    const msg = o.message ?? o.Message
    if (typeof msg === 'string' && msg.trim()) return msg.trim()
    const resp = o.response as Record<string, unknown> | undefined
    const data = resp?.data as Record<string, unknown> | undefined
    const dm = data?.message ?? data?.Message
    if (typeof dm === 'string' && dm.trim()) return dm.trim()
  }
  return e instanceof Error ? e.message : fallback
}

function unwrapLinkedStockOutId(res: unknown): string | null {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
  if (!d) return null
  const id = String(d.stockOutId ?? d.StockOutId ?? '').trim()
  return id || null
}

export interface PackingStockOutRequestLink {
  stockOutRequestId: string
  packingId: string
}

export interface PackingStockOutRequestsResolve {
  stockOutRequestIds: string[]
  links: PackingStockOutRequestLink[]
  customerId?: string | null
  packingCount: number
}

export interface PackingBatchStockOutLine {
  packingId: string
  packingCode?: string | null
  stockOutId: string
  stockOutCode: string
}

export interface PackingBatchStockOutResult {
  lines: PackingBatchStockOutLine[]
}

function unwrapStockOutRequestIdsResolve(res: unknown): PackingStockOutRequestsResolve | null {
  const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
  const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
  if (!d) return null
  const raw = (d.stockOutRequestIds ?? d.StockOutRequestIds) as unknown
  const stockOutRequestIds = Array.isArray(raw)
    ? raw.map((x) => String(x || '').trim()).filter(Boolean)
    : []
  const linksRaw = (d.links ?? d.Links) as unknown
  const links = Array.isArray(linksRaw)
    ? linksRaw
        .map((x) => {
          const o = x && typeof x === 'object' ? (x as Record<string, unknown>) : {}
          return {
            stockOutRequestId: String(
              o.stockOutRequestId ?? o.StockOutRequestId ?? ''
            ).trim(),
            packingId: String(o.packingId ?? o.PackingId ?? '').trim()
          }
        })
        .filter((x) => x.stockOutRequestId && x.packingId)
    : []
  return {
    stockOutRequestIds,
    links,
    customerId: (d.customerId ?? d.CustomerId) as string | null | undefined,
    packingCount: Number(d.packingCount ?? d.PackingCount ?? 0)
  }
}

async function resolveStockOutRequestIdsInternal(
  packingIds: string[],
  forPicking: boolean
): Promise<PackingStockOutRequestsResolve> {
  const ids = packingIds.map((x) => String(x || '').trim()).filter(Boolean)
  if (!ids.length) throw new Error('请至少选择一张装箱单')
  try {
    const res = await apiClient.get<unknown>('/api/v1/packing/stock-out-request-ids', {
      params: { ids: ids.join(','), forPicking }
    })
    const parsed = unwrapStockOutRequestIdsResolve(res)
    if (!parsed) throw new Error('解析出库通知失败')
    return parsed
  } catch (e) {
    throw new Error(parseApiError(e, forPicking ? '解析拣货出库通知失败' : '解析出库通知失败'))
  }
}

export const packingApi = {
  async getListPaged(params?: PackingListQuery): Promise<PackingListPaged> {
    const res = await apiClient.get<unknown>('/api/v1/packing', { params })
    return unwrapPaged(res, (d) =>
      Array.isArray(d.items) ? (d.items as unknown[]).map(normalizePackingListItem) : []
    )
  },

  /** 所选装箱单 → 可执行出库的出库通知 Id（通知须为已装箱） */
  async resolveStockOutRequestIds(packingIds: string[]): Promise<PackingStockOutRequestsResolve> {
    return resolveStockOutRequestIdsInternal(packingIds, false)
  },

  /** 批量出库：服务端校验并直接生成出库单（不打开执行出库页） */
  async batchStockOut(packingIds: string[], expectedStockOutDate: string): Promise<PackingBatchStockOutResult> {
    const ids = packingIds.map((x) => String(x || '').trim()).filter(Boolean)
    if (!ids.length) throw new Error('请至少选择一张装箱单')
    const expected = String(expectedStockOutDate || '').trim()
    if (!expected) throw new Error('请填写预计出库日期')
    try {
      const res = await apiClient.post<unknown>('/api/v1/packing/batch-stock-out', {
        packingIds: ids,
        expectedStockOutDate: expected
      })
      const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
      const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
      const raw = (d?.lines ?? d?.Lines) as unknown
      const lines = Array.isArray(raw)
        ? raw.map((x) => {
            const o = x && typeof x === 'object' ? (x as Record<string, unknown>) : {}
            return {
              packingId: String(o.packingId ?? o.PackingId ?? ''),
              packingCode: (o.packingCode ?? o.PackingCode) as string | null | undefined,
              stockOutId: String(o.stockOutId ?? o.StockOutId ?? ''),
              stockOutCode: String(o.stockOutCode ?? o.StockOutCode ?? '')
            }
          })
        : []
      return { lines }
    } catch (e) {
      throw new Error(parseApiError(e, '批量出库失败'))
    }
  },

  /** 拣货：按装箱单明细关联的出库通知 Id（装箱单须已确认，不校验通知为已装箱） */
  async resolveStockOutRequestIdsForPicking(packingIds: string[]): Promise<PackingStockOutRequestsResolve> {
    return resolveStockOutRequestIdsInternal(packingIds, true)
  },

  /** 装箱单 Invoice 打印页 bundle（无关联出库单时由装箱单合成） */
  async getInvoiceReportBundle(packingId: string): Promise<StockOutInvoiceReportBundle | null> {
    const id = packingId.trim()
    if (!id) return null
    const res = await apiClient.get<unknown>(
      `/api/v1/packing/${encodeURIComponent(id)}/invoice-report-bundle`
    )
    return parseInvoiceBundlePayload(res)
  },

  /** 装箱单 Packing 打印页 bundle */
  async getPackingReportBundle(
    packingId: string,
    withInspection: boolean
  ): Promise<StockOutPackingReportBundle | null> {
    const id = packingId.trim()
    if (!id) return null
    const res = await apiClient.get<unknown>(
      `/api/v1/packing/${encodeURIComponent(id)}/packing-report-bundle`,
      { params: { withInspection } }
    )
    return parsePackingBundlePayload(res, withInspection)
  },

  /** 装箱单打印：关联出库单 Id（无则 null） */
  async getLinkedStockOutId(packingId: string): Promise<string | null> {
    const id = packingId.trim()
    if (!id) return null
    try {
      const res = await apiClient.get<unknown>(`/api/v1/packing/${encodeURIComponent(id)}/linked-stock-out-id`)
      return unwrapLinkedStockOutId(res)
    } catch {
      return null
    }
  },

  /** 确认装箱单（status 10 → 20） */
  async confirm(id: string): Promise<void> {
    const rid = String(id || '').trim()
    if (!rid) throw new Error('缺少装箱单 ID')
    try {
      await apiClient.post<unknown>(`/api/v1/packing/${encodeURIComponent(rid)}/confirm`)
    } catch (e) {
      throw new Error(parseApiError(e, '确认装箱单失败'))
    }
  },

  /** 备货完成（status 30 → 40） */
  async markReady(id: string): Promise<void> {
    const rid = String(id || '').trim()
    if (!rid) throw new Error('缺少装箱单 ID')
    try {
      await apiClient.post<unknown>(`/api/v1/packing/${encodeURIComponent(rid)}/ready`)
    } catch (e) {
      throw new Error(parseApiError(e, '备货失败'))
    }
  },

  /** 删除装箱单（仅 status=10）；回滚关联出库通知 */
  async delete(id: string): Promise<void> {
    const rid = String(id || '').trim()
    if (!rid) throw new Error('缺少装箱单 ID')
    try {
      await apiClient.delete<unknown>(`/api/v1/packing/${encodeURIComponent(rid)}`)
    } catch (e) {
      throw new Error(parseApiError(e, '删除装箱单失败'))
    }
  },

  async getById(id: string): Promise<PackingDetail> {
    const rid = String(id || '').trim()
    if (!rid) throw new Error('缺少装箱单 ID')
    try {
      const res = await apiClient.get<unknown>(`/api/v1/packing/${encodeURIComponent(rid)}`)
      const parsed = unwrapDetail(res)
      if (!parsed) throw new Error('装箱单不存在或响应无效')
      return parsed
    } catch (e) {
      throw new Error(parseApiError(e, '加载装箱单详情失败'))
    }
  },

  /** 按出库通知 Id 加载关联装箱单详情（全部装箱明细行） */
  async getByStockOutRequestId(requestId: string): Promise<PackingDetail | null> {
    const rid = String(requestId || '').trim()
    if (!rid) return null
    try {
      const res = await apiClient.get<unknown>(
        `/api/v1/packing/by-stock-out-request/${encodeURIComponent(rid)}`
      )
      return unwrapDetail(res)
    } catch (e: unknown) {
      const status = (e as { response?: { status?: number } })?.response?.status
      if (status === 404) return null
      throw new Error(parseApiError(e, '加载装箱明细失败'))
    }
  },

  async getItemListPaged(params: {
    keyword?: string
    packingCode?: string
    page?: number
    pageSize?: number
  }): Promise<PackingItemListPaged> {
    const res = await apiClient.get<unknown>('/api/v1/packing/items', { params })
    const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
    const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
    if (d && Array.isArray(d.items)) {
      return {
        items: d.items as PackingItemListRow[],
        total: Number(d.total ?? 0),
        page: Number(d.page ?? 1),
        pageSize: Number(d.pageSize ?? 20)
      }
    }
    return { items: [], total: 0, page: 1, pageSize: 20 }
  },

  async previewFromStockOutRequests(stockOutRequestIds: string[]): Promise<PackingDraftFromStockOutRequests> {
    const ids = stockOutRequestIds.map((x) => String(x || '').trim()).filter(Boolean)
    if (!ids.length) throw new Error('请至少选择一条出库通知')
    try {
      const res = await apiClient.get<unknown>('/api/v1/packing/from-stock-out-requests/preview', {
        params: { ids: ids.join(',') }
      })
      const root = res && typeof res === 'object' ? (res as Record<string, unknown>) : null
      const d = (root?.data ?? root?.Data ?? root) as Record<string, unknown> | null
      if (!d) throw new Error('预览数据无效')
      const rawLines = (d.lines ?? d.Lines) as unknown[] | undefined
      const lines: PackingDraftLine[] = Array.isArray(rawLines)
        ? rawLines.map((row) => {
            const r = row as Record<string, unknown>
            return {
              stockOutRequestId: String(r.stockOutRequestId ?? r.StockOutRequestId ?? ''),
              requestCode: (r.requestCode ?? r.RequestCode) as string | null | undefined,
              pn: (r.pn ?? r.Pn) as string | null | undefined,
              brand: (r.brand ?? r.Brand) as string | null | undefined,
              qty: Number(r.qty ?? r.Qty ?? 0),
              unit: (r.unit ?? r.Unit) as string | null | undefined,
              sellOrderId: (r.sellOrderId ?? r.SellOrderId) as string | null | undefined,
              sellOrderItemId: (r.sellOrderItemId ?? r.SellOrderItemId) as string | null | undefined,
              sellOrderCode: (r.sellOrderCode ?? r.SellOrderCode) as string | null | undefined,
              sellOrderItemCode: (r.sellOrderItemCode ?? r.SellOrderItemCode) as string | null | undefined,
              remark: (r.remark ?? r.Remark) as string | null | undefined
            }
          })
        : []
      return {
        customerId: String(d.customerId ?? d.CustomerId ?? ''),
        customerName: (d.customerName ?? d.CustomerName) as string | null | undefined,
        salesId: (d.salesId ?? d.SalesId) as string | null | undefined,
        salesUserName: (d.salesUserName ?? d.SalesUserName) as string | null | undefined,
        stockOutType:
          d.stockOutType != null || d.StockOutType != null
            ? Number(d.stockOutType ?? d.StockOutType)
            : undefined,
        warehouseId: (d.warehouseId ?? d.WarehouseId) as string | null | undefined,
        warehouseName: (d.warehouseName ?? d.WarehouseName) as string | null | undefined,
        shipmentMethod: (d.shipmentMethod ?? d.ShipmentMethod) as string | null | undefined,
        expressCompany: (d.expressCompany ?? d.ExpressCompany) as string | null | undefined,
        lines
      }
    } catch (e) {
      throw new Error(parseApiError(e, '加载装箱预览失败'))
    }
  },

  async createFromStockOutRequests(
    stockOutRequestIds: string[],
    extras?: PackingCreateExtras
  ): Promise<PackingCreateResult> {
    try {
      const res = await apiClient.post<unknown>('/api/v1/packing/from-stock-out-requests', {
        stockOutRequestIds,
        extras: extras ?? undefined
      })
      const parsed = unwrapCreate(res)
      if (!parsed) throw new Error('生成装箱单失败：响应无效')
      return parsed
    } catch (e) {
      throw new Error(parseApiError(e, '生成装箱单失败'))
    }
  }
}

export function packingStatusLabel(status: number): string {
  const map: Record<number, string> = {
    10: '新建',
    20: '已确认',
    30: '已拣货',
    40: '已备货',
    50: '待出库',
    100: '出库完成'
  }
  return map[status] ?? String(status)
}

export function packingMaterialTypeLabel(type: number): string {
  const map: Record<number, string> = {
    10: '正常',
    20: '测试',
    30: '样品'
  }
  return map[type] ?? String(type)
}

export function packingDeliveryMethodLabel(method?: number | null): string {
  if (method === 10) return '送货'
  if (method === 20) return '自提'
  return method != null ? String(method) : '—'
}

/** Packing List 报表英文出货方式 */
export function packingDeliveryMethodLabelEn(method?: number | null): string {
  if (method === 10) return 'Delivery'
  if (method === 20) return 'Self Pick-up'
  return method != null ? String(method) : '—'
}

export function currencyLabel(code: number): string {
  const map: Record<number, string> = {
    1: 'RMB',
    2: 'USD',
    3: 'EUR',
    4: 'HKD',
    5: 'JPY',
    6: 'GBP'
  }
  return map[code] ?? String(code)
}
