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

  async getQuoterPool(filter: 'all' | 'selected' = 'all'): Promise<PurchaseQuoterPoolListResponse> {
    return apiClient.get<PurchaseQuoterPoolListResponse>('/api/v1/purchase-params/quoter-pool', {
      params: { filter }
    })
  },

  async saveQuoterPool(userIds: string[]): Promise<PurchaseQuoterPoolListResponse> {
    return apiClient.put<PurchaseQuoterPoolListResponse>('/api/v1/purchase-params/quoter-pool', { userIds })
  }
}
