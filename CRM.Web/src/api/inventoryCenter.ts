import apiClient from './client'

const unwrap = <T>(res: any): T => (res?.data ?? res) as T

export interface InventoryOverview {
  /** stock.StockId */
  stockId: string
  /** 库存业务编号（历史行可能为空） */
  stockCode?: string | null
  materialId: string
  /** 规格型号（后端优先 stock.purchase_pn，缺省再主数据/订单行） */
  materialModel?: string | null
  /** 品牌（后端优先 stock.purchase_brand，缺省再主数据名称/订单行 Brand） */
  materialName?: string | null
  warehouseId: string
  /** 仓库编码（接口解析；无档案时前端可回退 warehouseId，如 WH-DEFAULT） */
  warehouseCode?: string | null
  /** 库存类型 1=客单 2=备货 3=样品 */
  stockType?: number
  /** 地域 10=境内 20=境外（stock.RegionType） */
  regionType?: number
  onHandQty: number
  availableQty: number
  lockedQty: number
  inventoryAmount: number
  /** 库存金额币别（与后端采购单 currency 编码一致：1=RMB 2=USD …） */
  currency?: number
  lastMoveTime?: string
}

export interface MaterialTrace {
  stockInTime?: string
  stockInCode?: string
  batchNo?: string
  quantity: number
  unitPrice: number
  purchaseOrderCode?: string
  purchaseUserName?: string
  qcStatus?: number
  qcCode?: string
  warehouseId?: string
  warehouseName?: string
  locationId?: string
}

/** 单条 stock 下的 stockitem 行（与后端 InventoryStockItemRowDto 一致，JSON camelCase） */
export interface StockItemRow {
  stockItemId: string
  /** 在库明细业务编号 stock_item.stock_item_code */
  stockItemCode?: string | null
  stockInItemId: string
  /** 入库明细业务编号（冗余 stock_item.stock_in_item_code，与 stock_in_item 一致） */
  stockInItemCode?: string | null
  stockInId: string
  stockInCode?: string | null
  materialId: string
  locationId?: string | null
  batchNo?: string | null
  productionDate?: string | null
  purchasePn?: string | null
  purchaseBrand?: string | null
  sellOrderItemCode?: string | null
  qtyInbound: number
  qtyStockOut: number
  qtyRepertory: number
  qtyRepertoryAvailable: number
  qtyOccupy: number
  qtySales: number
  purchasePrice: number
  /** 1=RMB 2=USD … */
  purchaseCurrency?: number
  purchasePriceUsd?: number
  salesPrice?: number | null
  salesCurrency?: number | null
  salesPriceUsd?: number | null
  vendorName?: string | null
  customerName?: string | null
  createTime?: string
}

/** 全库 stockitem 列表查询（query 参数与后端 InventoryStockItemListQuery 一致） */
export interface StockItemListQuery {
  stockInCode?: string
  stockInDateFrom?: string
  stockInDateTo?: string
  purchasePn?: string
  purchaseBrand?: string
  /** 1=未出库 2=部分 3=完成 */
  outboundStatus?: number
  customerName?: string
  vendorName?: string
  /** 模糊匹配冗余姓名（未传 userId 时生效） */
  salespersonName?: string
  purchaserName?: string
  /** 与 stockitem.SalespersonId 精确匹配 */
  salespersonUserId?: string
  /** 与 stockitem.PurchaserId 精确匹配 */
  purchaserUserId?: string
}

export interface StockItemListRow extends StockItemRow {
  stockInDate?: string | null
  purchaserName?: string | null
  salespersonName?: string | null
  stockAggregateId: string
  warehouseId: string
  warehouseCode?: string | null
  /** 由后端按 WarehouseId 解析；列表展示勿回退为 warehouseId（避免 Guid） */
  warehouseName?: string | null
  /** 1=未出库 2=部分 3=完成；入库量为 0 时可能为 0 */
  outboundStatus: number
  /** 出库业务 USD 利润（层快照价 × 累计出库数量） */
  profitOutBizUsd?: number
}

/** 销售订单明细维度：全仓可用库存合计（与拣货出库物料键解析一致） */
export interface SellOrderLineAvailableQty {
  availableQty: number
}

export interface FinanceSummary {
  inventoryCapital: number
  monthlyOutCost: number
  averageInventoryCapital: number
  turnoverRate: number
  turnoverDays: number
  stagnantMaterialCount: number
}

export interface WarehouseInfo {
  id?: string
  warehouseCode: string
  warehouseName: string
  address?: string
  /** RegionType：10=境内 20=境外（与 @/constants/regionType 及到货通知共用） */
  regionType?: number
  status: number
}

/** 拣货任务明细行（备货补充：isStockingSupplement=true） */
export interface PickingTaskLine {
  id: string
  materialId: string
  stockId?: string | null
  /** 在库明细业务编号 stock_item.stock_item_code */
  stockItemCode?: string | null
  /** 入库明细业务编号 stock_item.stock_in_item_code */
  stockInItemCode?: string | null
  /** 在库明细 stockitem.Id（新流程） */
  stockItemId?: string | null
  /** 1=客单 2=备货 3=样品 */
  stockType?: number | null
  planQty: number
  pickedQty: number
  isStockingSupplement?: boolean
}

/** 拣货候选在库明细（服务端 FIFO 排序） */
export interface PickingStockItemCandidate {
  stockItemId: string
  /** 入库日期（stock_in.StockInDate） */
  stockInDate?: string | null
  /** 在库明细业务编号 stock_item.stock_item_code */
  stockItemCode?: string | null
  /** 入库明细业务编号 stock_item.stock_in_item_code */
  stockInItemCode?: string | null
  stockAggregateId: string
  materialId: string
  availableQty: number
  stockType: number
  purchasePn?: string | null
  purchaseBrand?: string | null
  locationId?: string | null
  batchNo?: string | null
  warehouseId: string
  productionDate?: string | null
  createTime?: string
  /** 备货池命中（非本销售行强绑定） */
  isStockingCandidate?: boolean
}

export interface SavePickingTaskItemLine {
  stockItemId: string
  stockId: string
  qty: number
}

export interface PickingTask {
  id: string
  taskCode: string
  stockOutRequestId: string
  warehouseId: string
  operatorId: string
  status: number
  remark?: string
  createTime?: string
  /** 明细计划拣货量合计 */
  planQtyTotal?: number
  /** 明细已拣货量合计 */
  pickedQtyTotal?: number
  /** 本任务涉及的库存类型（去重） */
  distinctStockTypes?: number[]
  /** 拣货明细（含备货补充标记） */
  items?: PickingTaskLine[]
}

/** 拣货单列表行（与后端 PickingTaskListItemDto 一致，JSON camelCase） */
export interface PickingTaskListRow {
  id: string
  status: number
  warehouseId: string
  warehouseDisplay?: string | null
  materialModel?: string | null
  brand?: string | null
  customerName?: string | null
  salesUserName?: string | null
  planQtyTotal: number
  lineCount: number
  stockOutRequestCode?: string | null
  taskCode: string
  createTime?: string
  createUserDisplay?: string | null
}

export interface PickingTaskDetailView extends PickingTaskListRow {
  remark?: string | null
  distinctStockTypes?: number[]
  items?: PickingTaskLine[]
}

export interface CountPlan {
  id: string
  planMonth: string
  warehouseId: string
  creatorId: string
  submitterId?: string
  status: number
  remark?: string
  createTime?: string
}

export const inventoryCenterApi = {
  async getAvailableQtyForSellOrderItem(sellOrderItemId: string): Promise<SellOrderLineAvailableQty> {
    return unwrap<SellOrderLineAvailableQty>(
      await apiClient.get(`/api/v1/inventory-center/sell-order-items/${encodeURIComponent(sellOrderItemId)}/available-qty`)
    )
  },
  async getOverview(warehouseId?: string): Promise<InventoryOverview[]> {
    const suffix = warehouseId ? `?warehouseId=${encodeURIComponent(warehouseId)}` : ''
    return unwrap<InventoryOverview[]>(await apiClient.get(`/api/v1/inventory-center/overview${suffix}`))
  },
  async getMaterialTrace(materialId: string): Promise<MaterialTrace[]> {
    return unwrap<MaterialTrace[]>(await apiClient.get(`/api/v1/inventory-center/materials/${encodeURIComponent(materialId)}/traces`))
  },
  async getStockItemsForStock(stockId: string): Promise<StockItemRow[]> {
    return unwrap<StockItemRow[]>(
      await apiClient.get(`/api/v1/inventory-center/stocks/${encodeURIComponent(stockId)}/stock-items`)
    )
  },
  async searchStockItems(query?: StockItemListQuery): Promise<StockItemListRow[]> {
    const params = new URLSearchParams()
    const q = query || {}
    const add = (key: string, v: string | number | undefined | null) => {
      if (v === undefined || v === null) return
      const s = typeof v === 'string' ? v.trim() : String(v)
      if (s === '') return
      params.set(key, s)
    }
    add('stockInCode', q.stockInCode)
    add('stockInDateFrom', q.stockInDateFrom)
    add('stockInDateTo', q.stockInDateTo)
    add('purchasePn', q.purchasePn)
    add('purchaseBrand', q.purchaseBrand)
    if (q.outboundStatus != null && q.outboundStatus >= 1 && q.outboundStatus <= 3) {
      params.set('outboundStatus', String(q.outboundStatus))
    }
    add('customerName', q.customerName)
    add('vendorName', q.vendorName)
    add('salespersonName', q.salespersonName)
    add('purchaserName', q.purchaserName)
    add('salespersonUserId', q.salespersonUserId)
    add('purchaserUserId', q.purchaserUserId)
    const qs = params.toString()
    return unwrap<StockItemListRow[]>(await apiClient.get(`/api/v1/inventory-center/stock-items${qs ? `?${qs}` : ''}`))
  },
  async getFinanceSummary(stagnantDays = 90): Promise<FinanceSummary> {
    return unwrap<FinanceSummary>(await apiClient.get(`/api/v1/inventory-center/finance/summary?stagnantDays=${stagnantDays}`))
  },
  async getWarehouses(): Promise<WarehouseInfo[]> {
    return unwrap<WarehouseInfo[]>(await apiClient.get('/api/v1/inventory-center/warehouses'))
  },
  async saveWarehouse(payload: WarehouseInfo): Promise<WarehouseInfo> {
    return unwrap<WarehouseInfo>(await apiClient.post('/api/v1/inventory-center/warehouses', payload))
  },
  async getPickingTasks(status?: number): Promise<PickingTask[]> {
    const suffix = status == null ? '' : `?status=${status}`
    return unwrap<PickingTask[]>(await apiClient.get(`/api/v1/inventory-center/picking-tasks${suffix}`))
  },
  async getPickingListRows(): Promise<PickingTaskListRow[]> {
    return unwrap<PickingTaskListRow[]>(await apiClient.get('/api/v1/inventory-center/picking-list'))
  },
  async getPickingListDetail(id: string): Promise<PickingTaskDetailView> {
    return unwrap<PickingTaskDetailView>(
      await apiClient.get(`/api/v1/inventory-center/picking-list/${encodeURIComponent(id)}`)
    )
  },
  async generatePickingTask(payload: {
    stockOutRequestId: string
    warehouseId: string
    operatorId?: string
    /** 可传空数组；拣货行由 savePickingTaskItems 维护 */
    items?: { materialId: string; quantity: number }[]
  }): Promise<PickingTask> {
    const body = { ...payload, items: payload.items ?? [] }
    return unwrap<PickingTask>(await apiClient.post('/api/v1/inventory-center/picking-tasks/generate', body))
  },
  async getPickingCandidates(stockOutRequestId: string, warehouseId: string): Promise<PickingStockItemCandidate[]> {
    const qs = new URLSearchParams({
      stockOutRequestId: stockOutRequestId.trim(),
      warehouseId: warehouseId.trim()
    })
    return unwrap<PickingStockItemCandidate[]>(
      await apiClient.get(`/api/v1/inventory-center/picking-candidates?${qs.toString()}`)
    )
  },
  async savePickingTaskItems(taskId: string, lines: SavePickingTaskItemLine[]): Promise<void> {
    await apiClient.post(`/api/v1/inventory-center/picking-tasks/${encodeURIComponent(taskId)}/items`, lines)
  },
  async completePickingTask(taskId: string): Promise<void> {
    await apiClient.post(`/api/v1/inventory-center/picking-tasks/${encodeURIComponent(taskId)}/complete`, {})
  },
  async getCountPlans(): Promise<CountPlan[]> {
    return unwrap<CountPlan[]>(await apiClient.get('/api/v1/inventory-center/count-plans'))
  },
  async createCountPlan(payload: { planMonth: string; warehouseId: string; creatorId: string; remark?: string }): Promise<CountPlan> {
    return unwrap<CountPlan>(await apiClient.post('/api/v1/inventory-center/count-plans', payload))
  },
  async submitCountPlan(payload: {
    planId: string
    submitterId: string
    items: { materialId: string; locationId?: string; countQty: number; countAmount: number }[]
  }): Promise<void> {
    await apiClient.post('/api/v1/inventory-center/count-plans/submit', payload)
  }
}

