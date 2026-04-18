import apiClient from './client'

export interface StockInBatchRow {
  id: string
  stockInId: string
  stockInItemId: string
  stockInItemCode?: string | null
  materialModel?: string | null
  dc?: string | null
  packageOrigin?: string | null
  waferOrigin?: string | null
  lot?: string | null
  lotQtyIn: number
  lotQtyOut: number
  origin?: string | null
  serialNumber?: string | null
  snQtyIn: number
  snQtyOut: number
  firmwareVersion?: string | null
  remark?: string | null
  createTime?: string
  modifyTime?: string | null
}

export interface StockInBatchUpdatePayload {
  materialModel?: string | null
  dc?: string | null
  packageOrigin?: string | null
  waferOrigin?: string | null
  lot?: string | null
  lotQtyIn: number
  lotQtyOut: number
  origin?: string | null
  serialNumber?: string | null
  snQtyIn: number
  snQtyOut: number
  firmwareVersion?: string | null
  remark?: string | null
}

/** 与 Excel 模板列对应，提交至 POST import */
export interface StockInBatchImportRow {
  materialModel?: string | null
  dc?: string | null
  packageOrigin?: string | null
  waferOrigin?: string | null
  lot?: string | null
  lotQtyIn: number
  origin?: string | null
  serialNumber?: string | null
  snQtyIn: number
  firmwareVersion?: string | null
  remark?: string | null
}

export interface StockInBatchImportRequest {
  stockInId: string
  stockInItemId: string
  stockInItemCode?: string
  rows: StockInBatchImportRow[]
}

export interface StockInBatchWriteOffRow {
  lot?: string | null
  lotWriteOffQty: number
  serialNumber?: string | null
  snWriteOffQty: number
}

export interface StockInBatchWriteOffRequest {
  rows: StockInBatchWriteOffRow[]
}

/** POST write-off 返回：校验失败时 <code>validationPassed</code> 为 false，不写库 */
export interface StockInBatchWriteOffResultDto {
  validationPassed: boolean
  updatedRowCount: number
  invalidLots: string[]
  invalidSerialNumbers: string[]
}

export const stockInBatchApi = {
  async list(params?: {
    stockInItemCode?: string
    lot?: string
    serialNumber?: string
  }): Promise<StockInBatchRow[]> {
    const rows = await apiClient.get<StockInBatchRow[] | null>('/api/v1/stock-in/batches', { params })
    return Array.isArray(rows) ? rows : []
  },

  async getById(id: string): Promise<StockInBatchRow | null> {
    const row = await apiClient.get<StockInBatchRow | null>(`/api/v1/stock-in/batches/${encodeURIComponent(id)}`)
    return row && typeof row === 'object' ? row : null
  },

  async update(id: string, body: StockInBatchUpdatePayload): Promise<StockInBatchRow> {
    return await apiClient.put<StockInBatchRow>(`/api/v1/stock-in/batches/${encodeURIComponent(id)}`, body)
  },

  /** Excel 解析后的行批量写入 stock_in_batch，返回新增条数 */
  async importRows(body: StockInBatchImportRequest): Promise<number> {
    return await apiClient.post<number>('/api/v1/stock-in/batches/import', body)
  },

  /** LOT/SN 核销：累加 lot_qty_out、sn_qty_out；校验失败不写库 */
  async writeOff(body: StockInBatchWriteOffRequest): Promise<StockInBatchWriteOffResultDto> {
    return await apiClient.post<StockInBatchWriteOffResultDto>('/api/v1/stock-in/batches/write-off', body)
  }
}
