import apiClient from './client'

export interface PurchaseQuoterPoolMemberDto {
  userId: string
  userName: string
  realName?: string | null
  departmentName?: string | null
  isActive: boolean
  isSelected: boolean
}

export interface PurchaseQuoterPoolListResponse {
  selectedCount: number
  items: PurchaseQuoterPoolMemberDto[]
}

export const purchaseParamsApi = {
  async getAssigneeCount(): Promise<number> {
    const res = await apiClient.get<{ count: number }>('/api/v1/purchase-params/assignee-count')
    return res.count
  },

  async setAssigneeCount(count: number): Promise<number> {
    const res = await apiClient.put<{ count: number }>('/api/v1/purchase-params/assignee-count', { count })
    return res.count
  },

  async getDemandProtectionMinutes(): Promise<number> {
    const res = await apiClient.get<{ minutes: number }>('/api/v1/purchase-params/demand-protection-minutes')
    return res.minutes
  },

  async setDemandProtectionMinutes(minutes: number): Promise<number> {
    const res = await apiClient.put<{ minutes: number }>('/api/v1/purchase-params/demand-protection-minutes', { minutes })
    return res.minutes
  },

  async getDefaultAssignMethod(): Promise<number> {
    const res = await apiClient.get<{ assignMethod: number }>('/api/v1/purchase-params/default-assign-method')
    return res.assignMethod
  },

  async setDefaultAssignMethod(assignMethod: number): Promise<number> {
    const res = await apiClient.put<{ assignMethod: number }>('/api/v1/purchase-params/default-assign-method', { assignMethod })
    return res.assignMethod
  },

  async getAllowRefreshCompletedBizNodes(): Promise<boolean> {
    const res = await apiClient.get<{ allow: boolean }>(
      '/api/v1/purchase-params/allow-refresh-completed-biz-nodes'
    )
    return !!res.allow
  },

  async setAllowRefreshCompletedBizNodes(allow: boolean): Promise<boolean> {
    const res = await apiClient.put<{ allow: boolean }>(
      '/api/v1/purchase-params/allow-refresh-completed-biz-nodes',
      { allow }
    )
    return !!res.allow
  },

  async getQuoterPool(filter: 'all' | 'selected' = 'all'): Promise<PurchaseQuoterPoolListResponse> {
    return apiClient.get<PurchaseQuoterPoolListResponse>('/api/v1/purchase-params/quoter-pool', {
      params: { filter }
    })
  },

  async saveQuoterPool(userIds: string[]): Promise<PurchaseQuoterPoolListResponse> {
    return apiClient.put<PurchaseQuoterPoolListResponse>('/api/v1/purchase-params/quoter-pool', { userIds })
  }
}
