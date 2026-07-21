import apiClient from './client'

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
  }
}
