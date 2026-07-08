import apiClient from './client'

export interface PurchaseCostParamDto {
  id: string
  ratio: number
  startTimeUtc: string
  remark?: string | null
  createTimeUtc?: string
  createByUserId?: string | null
  isEffectiveNow?: boolean
}

export interface PurchaseCostParamChangeLogDto {
  id: string
  purchaseCostParamId?: string | null
  ratio: number
  startTimeUtc: string
  changeTimeUtc: string
  changeUserId?: string | null
  changeUserName?: string | null
  changeSummary?: string | null
}

export interface PurchaseCostParamPage {
  items: PurchaseCostParamDto[]
  totalCount: number
  page: number
  pageSize: number
}

export interface PurchaseCostParamChangeLogPage {
  items: PurchaseCostParamChangeLogDto[]
  totalCount: number
  page: number
  pageSize: number
}

export const purchaseCostParamApi = {
  async getEffective(): Promise<PurchaseCostParamDto> {
    return apiClient.get<PurchaseCostParamDto>('/api/v1/purchase-cost-params/effective')
  },

  async list(page = 1, pageSize = 20): Promise<PurchaseCostParamPage> {
    return apiClient.get<PurchaseCostParamPage>('/api/v1/purchase-cost-params', {
      params: { page, pageSize }
    })
  },

  async create(body: {
    ratio: number
    startTimeUtc: string
    remark?: string | null
  }): Promise<PurchaseCostParamDto> {
    return apiClient.post<PurchaseCostParamDto>('/api/v1/purchase-cost-params', body)
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/purchase-cost-params/${encodeURIComponent(id)}`)
  },

  async getChangeLog(page = 1, pageSize = 20): Promise<PurchaseCostParamChangeLogPage> {
    return apiClient.get<PurchaseCostParamChangeLogPage>('/api/v1/purchase-cost-params/change-log', {
      params: { page, pageSize }
    })
  }
}

export async function fetchEffectivePurchaseCostParam(): Promise<PurchaseCostParamDto> {
  return purchaseCostParamApi.getEffective()
}
