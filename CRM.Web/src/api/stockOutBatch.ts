import apiClient from './client'

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

export const stockOutBatchApi = {
  async importRows(body: StockOutBatchImportRequest): Promise<StockOutBatchImportResultDto> {
    return await apiClient.post<StockOutBatchImportResultDto>('/api/v1/stock-out/batches/import', body)
  },

  async softDelete(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/stock-out/batches/${encodeURIComponent(id)}`)
  }
}
