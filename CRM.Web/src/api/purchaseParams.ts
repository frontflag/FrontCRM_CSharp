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

export interface PurchaseRefreshCompletedFacets {
  vendor: boolean
  pn: boolean
  brand: boolean
  qty: boolean
  price: boolean
}

function normalizeRefreshCompletedFacets(raw: Partial<PurchaseRefreshCompletedFacets> | null | undefined): PurchaseRefreshCompletedFacets {
  return {
    vendor: !!raw?.vendor,
    pn: raw?.pn !== false,
    brand: raw?.brand !== false,
    qty: raw?.qty !== false,
    price: raw?.price !== false
  }
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

  async getDefaultAssignMethod(): Promise<{ assignMethod: number; allowDesignatedPurchaser: boolean }> {
    const res = await apiClient.get<{ assignMethod: number; allowDesignatedPurchaser?: boolean }>(
      '/api/v1/purchase-params/default-assign-method'
    )
    return {
      assignMethod: res.assignMethod,
      allowDesignatedPurchaser: !!res.allowDesignatedPurchaser
    }
  },

  async setDefaultAssignMethod(
    assignMethod: number,
    allowDesignatedPurchaser: boolean
  ): Promise<{ assignMethod: number; allowDesignatedPurchaser: boolean }> {
    const res = await apiClient.put<{ assignMethod: number; allowDesignatedPurchaser?: boolean }>(
      '/api/v1/purchase-params/default-assign-method',
      { assignMethod, allowDesignatedPurchaser }
    )
    return {
      assignMethod: res.assignMethod,
      allowDesignatedPurchaser: !!res.allowDesignatedPurchaser
    }
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

  async getRefreshCompletedFacets(): Promise<PurchaseRefreshCompletedFacets> {
    const res = await apiClient.get<PurchaseRefreshCompletedFacets>(
      '/api/v1/purchase-params/refresh-completed-facets'
    )
    return normalizeRefreshCompletedFacets(res)
  },

  async setRefreshCompletedFacets(
    facets: PurchaseRefreshCompletedFacets
  ): Promise<PurchaseRefreshCompletedFacets> {
    const res = await apiClient.put<PurchaseRefreshCompletedFacets>(
      '/api/v1/purchase-params/refresh-completed-facets',
      facets
    )
    return normalizeRefreshCompletedFacets(res)
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
