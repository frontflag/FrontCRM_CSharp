import apiClient from './client'
import type { OperationLogPaged, OperationLogRow } from './operationLogs'

function mapRow(row: OperationLogRow): OperationLogRow {
  return {
    ...row,
    extraInfo:
      row.extraInfo ??
      (row as OperationLogRow & { ExtraInfo?: string }).ExtraInfo ??
      null
  }
}

export const debugSuperApi = {
  async listOperationLogs(page = 1, pageSize = 20): Promise<OperationLogPaged> {
    const data = await apiClient.get<OperationLogPaged>('/api/v1/debug/super/operation-logs', {
      params: { page, pageSize }
    })
    return {
      total: data?.total ?? 0,
      page: data?.page ?? 1,
      pageSize: data?.pageSize ?? 20,
      items: (data?.items ?? []).map(mapRow)
    }
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await apiClient.post('/api/v1/debug/super/change-password', {
      currentPassword,
      newPassword
    })
  },

  async createSuperAdmin(payload: {
    userName: string
    password: string
    realName?: string
    email?: string
  }): Promise<{ id: string; userName: string; realName?: string | null; email?: string | null }> {
    return await apiClient.post('/api/v1/debug/super/create-super-admin', payload)
  }
}
