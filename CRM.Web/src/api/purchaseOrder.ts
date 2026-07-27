import apiClient from './client'
import type { QcImageReadonlyRow } from './document'
import type { StockInListItemDto } from './stockIn'
import type { StockInNotifyDto } from './logistics'
import type { StockItemListRow } from './inventoryCenter'
import { buildQueryString } from '@/utils/progressStatusQuery'

export interface PurchaseOrderItemExtendFieldChangeDto {
  field: string
  label: string
  before: string
  after: string
}

export interface PurchaseOrderItemExtendChangeDto {
  purchaseOrderItemId: string
  purchaseOrderItemCode?: string
  fields: PurchaseOrderItemExtendFieldChangeDto[]
}

export interface PurchaseOrderItemExtendRefreshResult {
  purchaseOrderId: string
  totalItems: number
  changedItems: number
  changedFieldsCount: number
  syncedPurchaseRequisitionStatusCount?: number
  syncedArrivalNoticeStatusCount?: number
  refreshedAt: string
  changes: PurchaseOrderItemExtendChangeDto[]
}

export interface PurchaseOrderVendorNameRefreshResult {
  purchaseOrderId: string
  vendorId: string
  oldVendorName?: string | null
  newVendorName?: string | null
  changed: boolean
}

export interface PurchaseOrderVendorChangePreviewResult {
  purchaseOrderId: string
  purchaseOrderCode?: string | null
  oldVendorId?: string | null
  oldVendorName?: string | null
  newVendorId: string
  newVendorName?: string | null
  canChange: boolean
  noOp?: boolean
  sameVendorId?: boolean
  blockReason?: string | null
  blockingDocuments?: string[]
  /** 采购订单头供应商名称快照是否需刷新（0/1） */
  poVendorNameToSync?: number
  poItemsToSync: number
  arrivalNoticesToSync: number
  stockInsToSync: number
  paymentsToSync: number
  purchaseInvoicesToSync: number
}

export interface PurchaseOrderFieldChangeLogRow {
  id: string
  purchaseOrderId: string
  purchaseOrderCode?: string | null
  objectLabel?: string | null
  fieldName: string
  fieldLabel?: string | null
  oldValue?: string | null
  newValue?: string | null
  changedByUserId?: string | null
  changedByUserName?: string | null
  changedAt: string
}

export interface PurchaseOrderDeletedItemRow {
  purchaseOrderItemId: string
  purchaseOrderItemCode?: string | null
  pn?: string | null
  brand?: string | null
  qty: number
  cost: number
  currency: number
  comment?: string | null
  createTime?: string | null
  deletedAt?: string | null
  deletedByUserId?: string | null
  deletedByUserName?: string | null
}

export interface PurchaseOrderBatchExportLogRow {
  id: string
  operationTime: string
  operatorUserName?: string | null
  exportedCount?: number | null
  operationDesc?: string | null
  filterSummary?: string | null
}

export type PurchaseOrderBatchExportLogPaged = {
  items: PurchaseOrderBatchExportLogRow[]
  total: number
  page: number
  pageSize: number
}

function parsePoBatchExportLogExtra(raw: string | null | undefined): Partial<PurchaseOrderBatchExportLogRow> {
  if (!raw?.trim()) return {}
  try {
    const o = JSON.parse(raw) as Record<string, unknown>
    return {
      exportedCount: o.exportedCount != null ? Number(o.exportedCount) : o.affectedCount != null ? Number(o.affectedCount) : null,
      filterSummary: (o.filterSummary as string) ?? null
    }
  } catch {
    return {}
  }
}

export interface PurchaseOrderDetailTabAggregates {
  purchaseRequisitions: Array<{
    id: string
    billCode: string
    status: number
    sellOrderItemId: string
    sellOrderId?: string | null
    sellOrderCode?: string | null
    salesUserName?: string | null
    pn?: string | null
    brand?: string | null
    qty: number
    expectedPurchaseTime: string
    createTime: string
  }>
  payments: Array<{
    id: string
    financePaymentCode: string
    vendorName?: string | null
    status: number
    paymentAmountToBe: number
    paymentAmount: number
    paymentCurrency: number
    paymentDate?: string | null
    createByUserId?: string | null
    createUserName?: string | null
    createTime: string
  }>
  arrivalNotices: StockInNotifyDto[]
  stockIns: StockInListItemDto[]
  stockItems: StockItemListRow[]
  purchaseInvoices: Array<{
    id: string
    vendorName?: string | null
    invoiceNo?: string | null
    invoiceAmount: number
    invoiceDate?: string | null
    confirmStatus: number
    redInvoiceStatus: number
    createTime: string
  }>
  /** 采购明细关联质检单（流程页签 / 下游列表） */
  qcs?: Array<{
    id: string
    qcCode: string
    stockInNotifyId: string
    stockInNotifyCode?: string | null
    status: number
    stockInStatus?: number
    passQty: number
    rejectQty: number
    stockInId?: string | null
    createByUserId?: string | null
    createUserName?: string | null
    createTime: string
  }>
  /** 采购明细关联质检单上的图片文档（只读展示） */
  qcImages: QcImageReadonlyRow[]
  /** 采购订单明细详情「概况」页签（仅单条明细 aggregates 接口返回） */
  lineOverview?: PurchaseOrderLineOverview | null
}

export type PurchaseOrderItemArrivalNoticeTabRow = PurchaseOrderDetailTabAggregates['arrivalNotices'][number]
export type PurchaseOrderItemStockInTabRow = PurchaseOrderDetailTabAggregates['stockIns'][number]
export type PurchaseOrderItemStockTabRow = PurchaseOrderDetailTabAggregates['stockItems'][number]

export interface PurchaseOrderLineOverviewQtyMetric {
  total: number
  done?: number
  pending?: number
}

export interface PurchaseOrderLineOverviewAmountMetric {
  total: number
  done?: number
  pending?: number
  currency?: number
}

export interface PurchaseOrderLineOverview {
  lineAmount: { total: number; currency?: number }
  lineQty: { total: number }
  payment: PurchaseOrderLineOverviewAmountMetric
  arrivalNotice: PurchaseOrderLineOverviewQtyMetric
  stockIn: PurchaseOrderLineOverviewQtyMetric
  purchaseInvoice: PurchaseOrderLineOverviewAmountMetric
}

export interface PurchaseOrderItemListLineRow {
  purchaseOrderItemId: string
  purchaseOrderId: string
  purchaseOrderItemCode?: string
  purchaseOrderCode?: string
  freightForwarderOrderNo?: string | null
  purchaseOrderType?: number
  vendorId?: string | null
  vendorName?: string | null
  vendorCode?: string | null
  vendorEnglishName?: string | null
  itemStatus?: number
  purchaseProgressStatus?: number
  stockInProgressStatus?: number
  paymentRequestProgressStatus?: number
  paymentProgressStatus?: number
  invoiceProgressStatus?: number
  orderCreateTime?: string | null
  createTime?: string | null
  purchaseUserName?: string | null
  createUserName?: string | null
  createdBy?: string | null
  pn?: string | null
  brand?: string | null
      qty?: number
  qtyStockInNotifyExpectSum?: number
  qtyStockInNotifyNot?: number
  cost?: number
  lineTotal?: number
  currency?: number
}

// 采购订单API
export const purchaseOrderApi = {
  // 获取采购订单列表（分页，与后端 PurchaseOrdersController 一致）
  /** 采购订单明细行列表（数据库分页），对应 GET /api/v1/purchase-orders/items */
  async getItemLinesPage(params: {
    page: number
    pageSize: number
    startDate?: string
    endDate?: string
    purchaseOrderCode?: string
    vendorName?: string
    purchaseUserName?: string
    pn?: string
    freightForwarderOrderNo?: string
    orderType?: number
    /** 交易币别：rmb=人民币，foreign=外币 */
    transactionCurrency?: 'rmb' | 'foreign' | ''
    paymentProgressStatus?: number | number[]
    purchaseProgressStatus?: number | number[]
    stockInProgressStatus?: number | number[]
    invoiceProgressStatus?: number | number[]
    /** 左栏快捷检索业务项 */
    quickFilter?: string
  }) {
    const q = buildQueryString(params as Record<string, unknown>)
    return await apiClient.get(`/api/v1/purchase-orders/items?${q}`)
  },

  async getList(params?: {
    /** 采购单号/供应商名称模糊（兼容旧版；与 code+vendor 拆分条件二选一语义见后端） */
    keyword?: string
    /** 采购单号包含 */
    code?: string
    /** 供应商名称包含 */
    vendor?: string
    /** 货代单号包含（独立筛选） */
    freightForwarderOrderNo?: string
    /** 采购员姓名包含 */
    purchaseUserName?: string
    /** 备注包含 */
    comment?: string
    /** 主表订单类型 1/2/3 */
    orderType?: number
    status?: number | number[]
    startDate?: string
    endDate?: string
    page?: number
    pageSize?: number
  }) {
    const q = buildQueryString((params ?? {}) as Record<string, unknown>)
    return await apiClient.get(`/api/v1/purchase-orders${q ? `?${q}` : ''}`)
  },

  // 获取采购订单详情
  async getById(id: string) {
    return await apiClient.get(`/api/v1/purchase-orders/${id}`)
  },

  /** 采购订单主表字段变更日志 */
  async getChangeLogs(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<PurchaseOrderFieldChangeLogRow[]>(`/api/v1/purchase-orders/${enc}/change-logs`)
  },

  /** 已软删除的采购订单明细 */
  async getDeletedItems(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<PurchaseOrderDeletedItemRow[]>(`/api/v1/purchase-orders/${enc}/deleted-items`)
  },

  async getDetailTabAggregates(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<PurchaseOrderDetailTabAggregates>(`/api/v1/purchase-orders/${enc}/detail-tab-aggregates`)
  },

  /** 单条采购明细：与 detail-tab-aggregates 结构一致，按采购明细主键过滤 */
  async getPurchaseOrderItemDetailTabAggregates(purchaseOrderId: string, purchaseOrderItemId: string) {
    const encO = encodeURIComponent(purchaseOrderId)
    const encI = encodeURIComponent(purchaseOrderItemId)
    return await apiClient.get<PurchaseOrderDetailTabAggregates>(
      `/api/v1/purchase-orders/${encO}/purchase-order-items/${encI}/detail-tab-aggregates`
    )
  },

  /** 报表页：订单 + 公司参数（单请求，不依赖 company-profile/report-bundle） */
  async getReportData(id: string) {
    return await apiClient.get(`/api/v1/purchase-orders/${id}/report-data`)
  },

  // 根据销售订单号获取采购订单
  async getBySellOrder(sellOrderCode: string) {
    return await apiClient.get(`/api/v1/purchase-orders/by-sell-order/${sellOrderCode}`)
  },

  // 创建采购订单
  async create(data: any) {
    return await apiClient.post('/api/v1/purchase-orders', data)
  },

  // 更新采购订单
  async update(id: string, data: any) {
    return await apiClient.put(`/api/v1/purchase-orders/${id}`, data)
  },

  // 删除采购订单
  async delete(id: string) {
    return await apiClient.delete(`/api/v1/purchase-orders/${id}`)
  },

  // 更新状态
  async updateStatus(id: string, status: number) {
    return await apiClient.patch(`/api/v1/purchase-orders/${id}/status`, { status })
  },

  // 自动生成采购订单(以销定采)
  async autoGenerate(sellOrderId: string) {
    return await apiClient.post(`/api/v1/purchase-orders/auto-generate/${sellOrderId}`, {})
  },

  // 刷新采购明细扩展字段（读取下游数据重算）
  async refreshItemExtends(id: string) {
    return await apiClient.post<PurchaseOrderItemExtendRefreshResult>(`/api/v1/purchase-orders/${id}/refresh-item-extends`, {})
  },

  /** 按 vendor_id 刷新头名称快照并同步未完结下游（换供应商权限） */
  async refreshVendorName(id: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.post<PurchaseOrderVendorNameRefreshResult>(`/api/v1/purchase-orders/${enc}/refresh-vendor-name`, {})
  },

  /** 更换供应商预检（管理员/采购总监） */
  async previewVendorChange(id: string, newVendorId: string) {
    const enc = encodeURIComponent(id)
    return await apiClient.get<PurchaseOrderVendorChangePreviewResult>(
      `/api/v1/purchase-orders/${enc}/change-vendor/preview`,
      { params: { newVendorId: newVendorId.trim() } }
    )
  },

  /** 录入/修改/清空货代单号（物流写权限） */
  async updateFreightForwarderOrderNo(id: string, freightForwarderOrderNo: string | null) {
    const enc = encodeURIComponent(id)
    return await apiClient.patch(`/api/v1/purchase-orders/${enc}/freight-forwarder-order-no`, {
      freightForwarderOrderNo
    })
  },

  async logBatchExport(purchaseOrderId: string, exportedCount: number): Promise<void> {
    const enc = encodeURIComponent(purchaseOrderId)
    await apiClient.post(`/api/v1/purchase-orders/${enc}/batch-log-export`, { exportedCount })
  },

  async getBatchExportLogs(
    purchaseOrderId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<PurchaseOrderBatchExportLogPaged> {
    const enc = encodeURIComponent(purchaseOrderId)
    const res = await apiClient.get<any>(`/api/v1/purchase-orders/${enc}/batch-export-logs`, { params })
    const d = res?.data ?? res
    const items = Array.isArray(d?.items) ? d.items : []
    return {
      items: items.map((row: Record<string, unknown>) => {
        const extra = parsePoBatchExportLogExtra(row.extraInfo as string | null | undefined)
        return {
          id: String(row.id ?? ''),
          operationTime: String(row.operationTime ?? ''),
          operatorUserName: (row.operatorUserName as string) ?? null,
          operationDesc: (row.operationDesc as string) ?? null,
          ...extra
        }
      }),
      total: Number(d?.total ?? 0),
      page: Number(d?.page ?? 1),
      pageSize: Number(d?.pageSize ?? 20)
    }
  }
}

export default purchaseOrderApi
