import apiClient from './client'

export type SysDictItemAdminRow = {
  id: string
  category: string
  itemCode: string
  nameZh: string
  nameEn?: string | null
  sortOrder: number
  isActive: boolean
  createTime: string
}

export type SysDictItemAdminPaged = {
  items: SysDictItemAdminRow[]
  total: number
}

/** 创建时 itemCode 由服务端生成，无需传 */
export type CreateSysDictItemPayload = {
  category: string
  nameZh: string
  nameEn?: string | null
  sortOrder?: number | null
  isActive: boolean
}

export type UpdateSysDictItemPayload = {
  nameZh: string
  nameEn?: string | null
  sortOrder: number
  isActive: boolean
}

/** 与后端 DictionariesAdminController 一致；优先 mgmt，避免部分网关对路径段 "admin" 误拦导致 404 */
const DICT_ADMIN_BASE = '/api/v1/dictionaries/mgmt'

export const dictAdminApi = {
  async fetchCategories(): Promise<string[]> {
    return apiClient.get<string[]>(`${DICT_ADMIN_BASE}/categories`)
  },

  async getNextItemCode(category: string): Promise<string> {
    return apiClient.get<string>(`${DICT_ADMIN_BASE}/next-item-code`, {
      params: { category }
    })
  },

  async fetchItems(params: {
    bizSegment?: 'customer' | 'vendor' | 'material' | 'logistics'
    category?: string
    keyword?: string
    isActive?: boolean | null
    page?: number
    pageSize?: number
  }): Promise<SysDictItemAdminPaged> {
    return apiClient.get<SysDictItemAdminPaged>(`${DICT_ADMIN_BASE}/items`, {
      params: {
        bizSegment: params.bizSegment,
        category: params.category || undefined,
        keyword: params.keyword || undefined,
        isActive:
          params.isActive === null || params.isActive === undefined ? undefined : params.isActive,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20
      }
    })
  },

  async createItem(body: CreateSysDictItemPayload): Promise<void> {
    await apiClient.post(`${DICT_ADMIN_BASE}/items`, body)
  },

  async updateItem(id: string, body: UpdateSysDictItemPayload): Promise<void> {
    await apiClient.put(`${DICT_ADMIN_BASE}/items/${encodeURIComponent(id)}`, body)
  },

  async reorderItems(body: { category: string; orderedIds: string[] }): Promise<void> {
    await apiClient.put(`${DICT_ADMIN_BASE}/items/reorder`, body)
  }
}
