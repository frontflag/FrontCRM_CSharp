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

export const operationLogsApi = {
  list(params: OperationLogQueryParams) {
    return apiClient.get<OperationLogPaged>('/api/v1/operation-logs', { params })
  }
}
