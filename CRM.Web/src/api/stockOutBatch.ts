import apiClient from './client'

export interface StockOutBatchRow {
  id: string
  packingId: string
  globalBatchNo: string
  outQty: number
  createTime?: string
  modifyTime?: string | null
}

export interface StockOutBatchImportRow {
  globalBatchNo?: string | null
  outQty: number
}

export interface StockOutBatchImportRequest {
  packingId: string
  rows: StockOutBatchImportRow[]
}

export interface StockOutBatchImportResultDto {
  importedCount: number
}

export interface StockOutBatchUpdatePayload {
  outQty: number
}

export interface StockOutBatchBulkDeleteResult {
  deletedCount: number
  skippedCount: number
  deletedGlobalBatchNos: string[]
  skipped: { globalBatchNo: string; reason: string }[]
}

export interface StockOutBatchOperationLogRow {
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
  packingCode?: string | null
  affectedCount?: number | null
  batchNosSummary?: string | null
  skippedCount?: number | null
  skippedBatchNosSummary?: string | null
}

export type StockOutBatchOperationLogPaged = {
  items: StockOutBatchOperationLogRow[]
  total: number
  page: number
  pageSize: number
}

function parseBatchLogExtra(raw: string | null | undefined): Partial<StockOutBatchOperationLogRow> {
  if (!raw?.trim()) return {}
  try {
    const o = JSON.parse(raw) as Record<string, unknown>
    return {
      packingCode: (o.packingCode as string) ?? null,
      affectedCount: o.affectedCount != null ? Number(o.affectedCount) : null,
      batchNosSummary: (o.batchNosSummary as string) ?? null,
      skippedCount: o.skippedCount != null ? Number(o.skippedCount) : null,
      skippedBatchNosSummary: (o.skippedBatchNosSummary as string) ?? null
    }
  } catch {
    return {}
  }
}

export const stockOutBatchApi = {
  async getById(id: string): Promise<StockOutBatchRow | null> {
    const row = await apiClient.get<StockOutBatchRow | null>(`/api/v1/stock-out/batches/${encodeURIComponent(id)}`)
    return row && typeof row === 'object' ? row : null
  },

  async update(id: string, body: StockOutBatchUpdatePayload): Promise<StockOutBatchRow> {
    return await apiClient.put<StockOutBatchRow>(`/api/v1/stock-out/batches/${encodeURIComponent(id)}`, body)
  },

  async importRows(body: StockOutBatchImportRequest): Promise<StockOutBatchImportResultDto> {
    return await apiClient.post<StockOutBatchImportResultDto>('/api/v1/stock-out/batches/import', body)
  },

  async softDelete(id: string, reason: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-out/batches/${encodeURIComponent(id)}`, { data: { reason } })
  },

  async bulkDeleteByPacking(packingId: string, reason: string): Promise<StockOutBatchBulkDeleteResult> {
    return await apiClient.post<StockOutBatchBulkDeleteResult>('/api/v1/stock-out/batches/bulk-delete-by-packing', {
      packingId,
      reason
    })
  },

  async logExport(packingId: string, exportedCount: number): Promise<void> {
    await apiClient.post('/api/v1/stock-out/batches/log-export', { packingId, exportedCount })
  },

  async getBatchOperationLogs(
    packingId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<StockOutBatchOperationLogPaged> {
    const res = await apiClient.get<any>(
      `/api/v1/packing/${encodeURIComponent(packingId)}/batch-operation-logs`,
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
