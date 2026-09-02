import apiClient from './client'

export interface SalesRefreshCompletedFacets {
  customer: boolean
  pn: boolean
  brand: boolean
  qty: boolean
  price: boolean
}

function normalizeRefreshCompletedFacets(raw: Partial<SalesRefreshCompletedFacets> | null | undefined): SalesRefreshCompletedFacets {
  return {
    customer: !!raw?.customer,
    pn: raw?.pn !== false,
    brand: raw?.brand !== false,
    qty: raw?.qty !== false,
    price: raw?.price !== false
  }
}

export const salesParamsApi = {
  async getAllowRefreshCompletedBizNodes(): Promise<boolean> {
    const res = await apiClient.get<{ allow: boolean }>(
      '/api/v1/sales-params/allow-refresh-completed-biz-nodes'
    )
    return !!res.allow
  },

  async setAllowRefreshCompletedBizNodes(allow: boolean): Promise<boolean> {
    const res = await apiClient.put<{ allow: boolean }>(
      '/api/v1/sales-params/allow-refresh-completed-biz-nodes',
      { allow }
    )
    return !!res.allow
  },

  async getRefreshCompletedFacets(): Promise<SalesRefreshCompletedFacets> {
    const res = await apiClient.get<SalesRefreshCompletedFacets>(
      '/api/v1/sales-params/refresh-completed-facets'
    )
    return normalizeRefreshCompletedFacets(res)
  },

  async setRefreshCompletedFacets(
    facets: SalesRefreshCompletedFacets
  ): Promise<SalesRefreshCompletedFacets> {
    const res = await apiClient.put<SalesRefreshCompletedFacets>(
      '/api/v1/sales-params/refresh-completed-facets',
      facets
    )
    return normalizeRefreshCompletedFacets(res)
  }
}
