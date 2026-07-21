import apiClient from './client'

export interface OperationLogRow {
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
  /** 从 ExtraInfo.filterSummary 解析（导出审计等） */
  filterSummary?: string | null
}

export interface OperationLogPaged {
  total: number
  page: number
  pageSize: number
  items: OperationLogRow[]
}

export interface OperationLogQueryParams {
  bizType?: string
  actionType?: string
  recordCode?: string
  operatorUserName?: string
  operationTimeFrom?: string
  operationTimeTo?: string
  reason?: string
  page?: number
  pageSize?: number
}

function parseExtraInfo(raw: string | null | undefined): Pick<OperationLogRow, 'filterSummary'> {
  if (!raw?.trim()) return {}
  try {
    const o = JSON.parse(raw) as Record<string, unknown>
    const summary = o.filterSummary
    return {
      filterSummary: typeof summary === 'string' && summary.trim() ? summary.trim() : null
    }
  } catch {
    return {}
  }
}

function mapRow(row: OperationLogRow): OperationLogRow {
  const extra = (row as OperationLogRow & { ExtraInfo?: string }).extraInfo
    ?? (row as OperationLogRow & { ExtraInfo?: string }).ExtraInfo
    ?? null
  const parsed = parseExtraInfo(extra)
  return {
    ...row,
    extraInfo: extra,
    filterSummary: parsed.filterSummary ?? null
  }
}

export const operationLogsApi = {
  async list(params: OperationLogQueryParams): Promise<OperationLogPaged> {
    const data = await apiClient.get<OperationLogPaged>('/api/v1/operation-logs', { params })
    return {
      total: data?.total ?? 0,
      page: data?.page ?? 1,
      pageSize: data?.pageSize ?? 20,
      items: (data?.items ?? []).map(mapRow)
    }
  }
}
