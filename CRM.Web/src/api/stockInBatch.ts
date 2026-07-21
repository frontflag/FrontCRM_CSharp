import apiClient from './client'

export interface StockInBatchRow {
  id: string
  stockInItemId: string
  globalBatchNo: string
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
  remark?: string | null
  createTime?: string
  modifyTime?: string | null
}

export interface StockInBatchUpdatePayload {
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
  remark?: string | null
}

/** 与 Excel 模板列对应，提交至 POST import（不含全局编号） */
export interface StockInBatchImportRow {
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
  remark?: string | null
}

export interface StockInBatchImportRequest {
  stockInId: string
  stockInItemId: string
  rows: StockInBatchImportRow[]
}

export interface StockInBatchImportResultDto {
  importedCount: number
  globalBatchNos: string[]
}

export const STOCK_IN_BATCH_SN_DUPLICATE_ERROR_CODE = 40001

export interface StockInBatchBulkDeleteResult {
  deletedCount: number
  skippedCount: number
  deletedGlobalBatchNos: string[]
  skipped: { globalBatchNo: string; reason: string }[]
}

export interface StockInBatchOperationLogRow {
  id: string
  bizType: string
  recordId: string
  recordCode?: string | null
  actionType: string
  operationTime: string
  operatorUserId?: string | null
  operatorUserName?: string | null
  reason?: string | null
  operationDesc?: string | null
  extraInfo?: string | null
  stockInItemCode?: string | null
  affectedCount?: number | null
  batchNosSummary?: string | null
  skippedCount?: number | null
  skippedBatchNosSummary?: string | null
  filterSummary?: string | null
}

export type StockInBatchOperationLogPaged = {
  items: StockInBatchOperationLogRow[]
  total: number
  page: number
  pageSize: number
}

function parseBatchLogExtra(raw: string | null | undefined): Partial<StockInBatchOperationLogRow> {
  if (!raw?.trim()) return {}
  try {
    const o = JSON.parse(raw) as Record<string, unknown>
    return {
      stockInItemCode: (o.stockInItemCode as string) ?? null,
      affectedCount: o.affectedCount != null ? Number(o.affectedCount) : o.exportedCount != null ? Number(o.exportedCount) : null,
      batchNosSummary: (o.batchNosSummary as string) ?? null,
      skippedCount: o.skippedCount != null ? Number(o.skippedCount) : null,
      skippedBatchNosSummary: (o.skippedBatchNosSummary as string) ?? null,
      filterSummary: (o.filterSummary as string) ?? null
    }
  } catch {
    return {}
  }
}

export type StockInBatchListPaged = { items: StockInBatchRow[]; total: number; page: number; pageSize: number }

export const stockInBatchApi = {
  async listPaged(params?: {
    globalBatchNo?: string
    lot?: string
    serialNumber?: string
    page?: number
    pageSize?: number
  }): Promise<StockInBatchListPaged> {
    const res = await apiClient.get<any>('/api/v1/stock-in/batches', { params })
    const d = res?.data ?? res
    if (d && typeof d === 'object' && Array.isArray(d.items)) {
      return {
        items: d.items as StockInBatchRow[],
        total: Number(d.total ?? 0),
        page: Number(d.page ?? 1),
        pageSize: Number(d.pageSize ?? 20)
      }
    }
    return { items: [], total: 0, page: 1, pageSize: 20 }
  },

  async getById(id: string): Promise<StockInBatchRow | null> {
    const row = await apiClient.get<StockInBatchRow | null>(`/api/v1/stock-in/batches/${encodeURIComponent(id)}`)
    return row && typeof row === 'object' ? row : null
  },

  async update(id: string, body: StockInBatchUpdatePayload): Promise<StockInBatchRow> {
    return await apiClient.put<StockInBatchRow>(`/api/v1/stock-in/batches/${encodeURIComponent(id)}`, body)
  },

  async importRows(body: StockInBatchImportRequest): Promise<StockInBatchImportResultDto> {
    return await apiClient.post<StockInBatchImportResultDto>('/api/v1/stock-in/batches/import', body)
  },

  async softDelete(id: string, reason: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-in/batches/${encodeURIComponent(id)}`, { data: { reason } })
  },

  async bulkDeleteByItem(stockInItemId: string, reason: string): Promise<StockInBatchBulkDeleteResult> {
    return await apiClient.post<StockInBatchBulkDeleteResult>('/api/v1/stock-in/batches/bulk-delete-by-item', {
      stockInItemId,
      reason
    })
  },

  async logExport(stockInId: string, exportedCount: number): Promise<void> {
    await apiClient.post('/api/v1/stock-in/batches/log-export', { stockInId, exportedCount })
  },

  async getBatchOperationLogs(
    stockInId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<StockInBatchOperationLogPaged> {
    const res = await apiClient.get<any>(
      `/api/v1/stock-in/${encodeURIComponent(stockInId)}/batch-operation-logs`,
      { params }
    )
    const d = res?.data ?? res
    const items = Array.isArray(d?.items) ? d.items : []
    return {
      items: items.map((row: Record<string, unknown>) => {
        const extra = parseBatchLogExtra(row.extraInfo as string | null | undefined)
        return {
          id: String(row.id ?? ''),
          bizType: String(row.bizType ?? ''),
          recordId: String(row.recordId ?? ''),
          recordCode: (row.recordCode as string) ?? null,
          actionType: String(row.actionType ?? ''),
          operationTime: String(row.operationTime ?? ''),
          operatorUserId: (row.operatorUserId as string) ?? null,
          operatorUserName: (row.operatorUserName as string) ?? null,
          reason: (row.reason as string) ?? null,
          operationDesc: (row.operationDesc as string) ?? null,
          extraInfo: (row.extraInfo as string) ?? null,
          ...extra
        }
      }),
      total: Number(d?.total ?? 0),
      page: Number(d?.page ?? 1),
      pageSize: Number(d?.pageSize ?? 20)
    }
  }
}
