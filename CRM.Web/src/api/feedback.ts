import apiClient from './client'

export type UserFeedbackListItem = {
  id: string
  category: string
  title: string
  summary: string
  bizRef?: string | null
  submitUserId: string
  submitUserName?: string | null
  needsHandling: boolean
  isHandled: boolean
  completedDate?: string | null
  createTime: string
  pageUrl?: string | null
  routeName?: string | null
}

export type UserFeedbackDetail = UserFeedbackListItem & {
  sessionId: string
  reproSteps?: string | null
  handleRemark?: string | null
  routeParamsJson?: string | null
  routeQueryJson?: string | null
  messages?: Array<{
    id: string
    role: string
    content?: string | null
    attachmentDocumentId?: string | null
    createTime: string
  }> | null
  attachmentDocumentIds?: string[] | null
}

export type UserFeedbackPaged = {
  items: UserFeedbackListItem[]
  total: number
  page: number
  pageSize: number
}

export type UserFeedbackQuery = {
  category?: string
  needsHandling?: boolean
  isHandled?: boolean
  keyword?: string
  page?: number
  pageSize?: number
}

export type PatchUserFeedbackPayload = {
  needsHandling?: boolean
  isHandled?: boolean
  completedDate?: string | null
  handleRemark?: string | null
  category?: string
}

export const feedbackApi = {
  adminList(params: UserFeedbackQuery) {
    return apiClient.get<UserFeedbackPaged>('/api/v1/feedback/admin', { params })
  },
  adminDetail(id: string, includeMessages = true) {
    return apiClient.get<UserFeedbackDetail>(`/api/v1/feedback/admin/${encodeURIComponent(id)}`, {
      params: { includeMessages }
    })
  },
  adminPatch(id: string, payload: PatchUserFeedbackPayload) {
    return apiClient.patch<UserFeedbackDetail>(
      `/api/v1/feedback/admin/${encodeURIComponent(id)}`,
      payload
    )
  }
}
