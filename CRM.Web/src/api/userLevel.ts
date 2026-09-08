import apiClient from './client'
import type { AdminUserDto } from './rbacAdmin'

export type { AdminUserDto }

export interface UserLevelHistoryItem {
  id: string
  userId: string
  userName: string
  oldLevel: number
  newLevel: number
  remark?: string
  changeTime: string
  operatorUserId?: string
  operatorUserName?: string
}

export interface UserLevelChangeResult {
  userId: string
  level: number
  levelChangedAt?: string | null
  levelRemark?: string | null
  levelChanged: boolean
}

export interface UserLevelDefinition {
  id: string
  userLevel: number
  description?: string | null
}

export const userLevelApi = {
  listDefinitions() {
    return apiClient.get<UserLevelDefinition[]>('/api/v1/user-levels/definitions')
  },
  updateDefinition(level: number, description?: string | null) {
    return apiClient.put<UserLevelDefinition>(
      `/api/v1/user-levels/definitions/${level}`,
      { description }
    )
  },
  change(userId: string, payload: { level: number; remark?: string | null }) {
    return apiClient.put<UserLevelChangeResult>(
      `/api/v1/user-levels/${encodeURIComponent(userId)}`,
      payload
    )
  },
  getHistory(userId: string) {
    return apiClient.get<UserLevelHistoryItem[]>(
      `/api/v1/user-levels/${encodeURIComponent(userId)}/history`
    )
  }
}

export const USER_LEVEL_OPTIONS = Array.from({ length: 20 }, (_, i) => i + 1)
