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

  async softDelete(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-in/batches/${encodeURIComponent(id)}`)
  }
}
