import apiClient from './client'

export type LogRecentOpenKind = 'detail' | 'edit'

export interface LogRecentItem {
  recordId: string
  recordCode?: string | null
  accessedAt: string
  openKind: string
}

export interface RecordLogRecentPayload {
  bizType: string
  recordId: string
  recordCode?: string
  openKind: LogRecentOpenKind
}

export const logRecentApi = {
  async record(payload: RecordLogRecentPayload): Promise<void> {
    await apiClient.post('/api/v1/log-recent', payload)
  },

  async list(bizType: string, take = 20): Promise<LogRecentItem[]> {
    const data = await apiClient.get<{ items?: LogRecentItem[] }>('/api/v1/log-recent', {
      params: { bizType, take }
    })
    return Array.isArray(data?.items) ? data.items : []
  }
}
