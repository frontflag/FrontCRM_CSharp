import apiClient from './client'

const unwrap = <T>(res: unknown): T => (res as { data?: T })?.data ?? (res as T)

export interface ManualTransferPreview {
  sourceStockItemId: string
  stockItemCode?: string | null
  /** 物料型号（采购 PN 或主数据规格型号） */
  materialModel?: string | null
  /** 品牌 */
  materialBrand?: string | null
  /** 地域 10=境内 20=境外 */
  regionType: number
  fromWarehouseId: string
  fromWarehouseName?: string | null
  sourceLocationId?: string | null
  qtyRepertory: number
  qtyRepertoryAvailable: number
  plannedMoveQty: number
  canExecute: boolean
  blockReasons: string[]
}

export interface ManualTransferExecuteBody {
  sourceStockItemId: string
  toWarehouseId: string
  toLocationId?: string | null
  remark?: string | null
}

export interface ManualTransferExecuteResult {
  stockTransferManualId: string
  transferCode: string
  moveQty: number
  targetStockItemId: string
  targetStockAggregateId: string
}

export const manualStockTransferApi = {
  async preview(sourceStockItemId: string): Promise<ManualTransferPreview> {
    return unwrap<ManualTransferPreview>(
      await apiClient.get(
        `/api/v1/inventory/manual-transfers/preview?sourceStockItemId=${encodeURIComponent(sourceStockItemId)}`
      )
    )
  },

  async execute(body: ManualTransferExecuteBody): Promise<ManualTransferExecuteResult> {
    return unwrap<ManualTransferExecuteResult>(await apiClient.post('/api/v1/inventory/manual-transfers', body))
  }
}
