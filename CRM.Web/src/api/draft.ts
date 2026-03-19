import apiClient from './client'

export interface SaveDraftRequest {
  draftId?: string
  entityType: 'CUSTOMER' | 'VENDOR' | 'RFQ' | string
  draftName?: string
  payloadJson: string
  remark?: string
}

export interface DraftDto {
  draftId: string
  userId: number
  entityType: string
  draftName?: string
  payloadJson: string
  status: number
  remark?: string
  convertedEntityId?: string
  convertedAt?: string
  createTime: string
  modifyTime?: string
}

export interface DraftConvertResultDto {
  draftId: string
  entityType: string
  entityId: string
}

export const draftApi = {
  async saveDraft(payload: SaveDraftRequest): Promise<DraftDto> {
    return apiClient.post<DraftDto>('/api/v1/drafts', payload)
  },

  async getDrafts(params?: {
    entityType?: string
    status?: number
    keyword?: string
  }): Promise<DraftDto[]> {
    return apiClient.get<DraftDto[]>('/api/v1/drafts', { params })
  },

  async getDraftById(draftId: string): Promise<DraftDto> {
    return apiClient.get<DraftDto>(`/api/v1/drafts/${draftId}`)
  },

  async deleteDraft(draftId: string): Promise<void> {
    await apiClient.delete(`/api/v1/drafts/${draftId}`)
  },

  async convertDraft(draftId: string): Promise<DraftConvertResultDto> {
    return apiClient.post<DraftConvertResultDto>(`/api/v1/drafts/${draftId}/convert`)
  }
}

export default draftApi
