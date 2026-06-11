import apiClient from './client'

export interface BatchReconciliationQuery {
  globalBatchNo?: string
  purchaseOrderCode?: string
  stockInCode?: string
  packingCode?: string
  materialModel?: string
  lot?: string
  serialNumber?: string
  vendorName?: string
  customerName?: string
  remark?: string
}

export interface BatchReconciliationRow {
  stockInBatchId: string
  stockOutBatchId?: string | null
  globalBatchNo: string
  warehouseName?: string | null
  stockInDate: string
  stockInCode: string
  purchaseOrderCode?: string | null
  freightForwarderOrderNo?: string | null
  vendorId?: string | null
  vendorName?: string | null
  materialModel?: string | null
  materialBrand?: string | null
  stockInItemQuantity: number
  batchDimension?: string | null
  batchUnit?: string | null
  unitNo?: string | null
  batchQty: number
  dc?: string | null
  packageOrigin?: string | null
  waferOrigin?: string | null
  lot?: string | null
  serialNumber?: string | null
  firmwareVersion?: string | null
  partCode?: string | null
  batchRemark?: string | null
  packingCode?: string | null
  customerId?: string | null
  customerName?: string | null
  stockOutDate?: string | null
  outQty?: number | null
  totalOutQty: number
  remainingQty: number
}

export interface BatchReconciliationConsumptionRow {
  stockOutBatchId: string
  packingCode: string
  outQty: number
  stockOutDate?: string | null
  customerId?: string | null
  customerName?: string | null
}

export type BatchReconciliationListPaged = {
  items: BatchReconciliationRow[]
  total: number
  page: number
  pageSize: number
}

function buildQueryParams(params?: BatchReconciliationQuery & { page?: number; pageSize?: number }) {
  const q: Record<string, string | number> = {}
  if (!params) return q
  if (params.globalBatchNo?.trim()) q.globalBatchNo = params.globalBatchNo.trim()
  if (params.purchaseOrderCode?.trim()) q.purchaseOrderCode = params.purchaseOrderCode.trim()
  if (params.stockInCode?.trim()) q.stockInCode = params.stockInCode.trim()
  if (params.packingCode?.trim()) q.packingCode = params.packingCode.trim()
  if (params.materialModel?.trim()) q.materialModel = params.materialModel.trim()
  if (params.lot?.trim()) q.lot = params.lot.trim()
  if (params.serialNumber?.trim()) q.serialNumber = params.serialNumber.trim()
  if (params.vendorName?.trim()) q.vendorName = params.vendorName.trim()
  if (params.customerName?.trim()) q.customerName = params.customerName.trim()
  if (params.remark?.trim()) q.remark = params.remark.trim()
  if (params.page != null) q.page = params.page
  if (params.pageSize != null) q.pageSize = params.pageSize
  return q
}

export const batchReconciliationApi = {
  async listPaged(params?: BatchReconciliationQuery & { page?: number; pageSize?: number }): Promise<BatchReconciliationListPaged> {
    const res = await apiClient.get<any>('/api/v1/batch-reconciliation', { params: buildQueryParams(params) })
    const d = res?.data ?? res
    if (d && typeof d === 'object' && Array.isArray(d.items)) {
      return {
        items: d.items as BatchReconciliationRow[],
        total: Number(d.total ?? 0),
        page: Number(d.page ?? 1),
        pageSize: Number(d.pageSize ?? 20)
      }
    }
    return { items: [], total: 0, page: 1, pageSize: 20 }
  },

  async getConsumption(globalBatchNo: string): Promise<BatchReconciliationConsumptionRow[]> {
    const key = globalBatchNo.trim()
    const res = await apiClient.get<any>(`/api/v1/batch-reconciliation/consumption/${encodeURIComponent(key)}`)
    const d = res?.data ?? res
    return Array.isArray(d) ? (d as BatchReconciliationConsumptionRow[]) : []
  },

  async exportInBatches(params?: BatchReconciliationQuery): Promise<Blob> {
    const qs = new URLSearchParams(buildQueryParams(params) as Record<string, string>).toString()
    const url = qs ? `/api/v1/batch-reconciliation/export/in-batches?${qs}` : '/api/v1/batch-reconciliation/export/in-batches'
    return apiClient.getBlob(url)
  },

  async exportOutBatches(params?: BatchReconciliationQuery): Promise<Blob> {
    const qs = new URLSearchParams(buildQueryParams(params) as Record<string, string>).toString()
    const url = qs ? `/api/v1/batch-reconciliation/export/out-batches?${qs}` : '/api/v1/batch-reconciliation/export/out-batches'
    return apiClient.getBlob(url)
  }
}
