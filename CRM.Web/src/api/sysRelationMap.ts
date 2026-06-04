import apiClient from './client'

export interface SaveSysRelationMapRequest {
  type: number
  objSrc: string
  addDestIds?: string[]
  removeDestIds?: string[]
}

export const sysRelationMapApi = {
  async getDestinations(type: number, objSrc: string): Promise<string[]> {
    const data = await apiClient.get<string[]>('/api/v1/sys-relation-maps/destinations', {
      params: { type, objSrc }
    })
    return Array.isArray(data) ? data : []
  },

  async saveBatch(body: SaveSysRelationMapRequest): Promise<void> {
    await apiClient.put('/api/v1/sys-relation-maps/batch', body)
  }
}
