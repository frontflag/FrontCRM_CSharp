import apiClient from './client'

/** 与后端 SysErrorLogResolveRemarks.Ignore 一致 */
export const ERROR_LOG_IGNORE_REMARK = '忽略'

export type ErrorLogStatus = 'open' | 'resolved' | 'ignored'

export type ErrorLogListItem = {
  id: number
  errorId: string
  occurredAt: string
  moduleName: string
  operationType?: string | null
  errorMessage: string
  requestPath?: string | null
  userName?: string | null
  isResolved: boolean
  status: ErrorLogStatus
  resolveRemark?: string | null
}

export type ErrorLogDetail = ErrorLogListItem & {
  errorDetail?: string | null
  documentNo?: string | null
  dataId?: string | null
  userId?: string | null
  requestBody?: string | null
}

export type ErrorLogPaged = {
  items: ErrorLogListItem[]
  total: number
  page: number
  pageSize: number
}

export type ErrorLogQuery = {
  moduleName?: string
  keyword?: string
  startDate?: string
  endDate?: string
  status?: ErrorLogStatus
  page?: number
  pageSize?: number
}

export function resolveErrorLogStatus(row: {
  isResolved: boolean
  status?: ErrorLogStatus
  resolveRemark?: string | null
}): ErrorLogStatus {
  if (row.status) return row.status
  if (!row.isResolved) return 'open'
  if (row.resolveRemark === ERROR_LOG_IGNORE_REMARK) return 'ignored'
  return 'resolved'
}

export const errorLogsApi = {
  list(params: ErrorLogQuery) {
    return apiClient.get<ErrorLogPaged>('/api/v1/error-logs', { params })
  },
  detail(id: number) {
    return apiClient.get<ErrorLogDetail>(`/api/v1/error-logs/${id}`)
  },
  resolve(id: number, remark?: string) {
    return apiClient.post(`/api/v1/error-logs/${id}/resolve`, { remark })
  }
}
