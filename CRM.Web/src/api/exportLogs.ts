import apiClient from './client'

export interface ExportKindOption {
  kind: string
  name: string
}

export interface ExportLogRow {
  id: string
  operationTime: string
  operatorUserName?: string | null
  exportKind: string
  exportKindName: string
  pageTitle: string
  pageUrl: string
  filterSummary?: string | null
  exportedCount?: number | null
  sysRemark?: string | null
}

export interface ExportLogPaged {
  total: number
  page: number
  pageSize: number
  items: ExportLogRow[]
}

export interface ExportLogQueryParams {
  exportKind?: string
  operatorUserName?: string
  operationTimeFrom?: string
  operationTimeTo?: string
  page?: number
  pageSize?: number
}

export const exportLogsApi = {
  async kinds(): Promise<ExportKindOption[]> {
    const data = await apiClient.get<ExportKindOption[]>('/api/v1/export-logs/kinds')
    return Array.isArray(data) ? data : []
  },

  async list(params: ExportLogQueryParams): Promise<ExportLogPaged> {
    const data = await apiClient.get<ExportLogPaged>('/api/v1/export-logs', { params })
    return {
      total: data?.total ?? 0,
      page: data?.page ?? 1,
      pageSize: data?.pageSize ?? 20,
      items: data?.items ?? []
    }
  }
}
