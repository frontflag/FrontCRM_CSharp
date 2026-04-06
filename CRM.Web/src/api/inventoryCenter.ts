import apiClient from './client'

const unwrap = <T>(res: any): T => (res?.data ?? res) as T

export interface InventoryOverview {
  materialId: string
  /** 规格型号 */
  materialModel?: string | null
  /** 库存总览中与品牌同源展示（主数据名称或产品 Brand 回填） */
  materialName?: string | null
  warehouseId: string
  /** 仓库编码（接口解析；无档案时前端可回退 warehouseId，如 WH-DEFAULT） */
  warehouseCode?: string | null
  /** 库存类型 1=客单 2=备货 3=样品 */
  stockType?: number
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
  status: number
}

/** 拣货任务明细行（备货补充：isStockingSupplement=true） */
export interface PickingTaskLine {
  id: string
  materialId: string
  stockId?: string | null
  /** 1=客单 2=备货 3=样品 */
  stockType?: number | null
  planQty: number
  pickedQty: number
  isStockingSupplement?: boolean
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
  async generatePickingTask(payload: {
    stockOutRequestId: string
    warehouseId: string
    operatorId?: string
    items: { materialId: string; quantity: number }[]
  }): Promise<PickingTask> {
    return unwrap<PickingTask>(await apiClient.post('/api/v1/inventory-center/picking-tasks/generate', payload))
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

