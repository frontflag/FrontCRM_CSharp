import apiClient from '@/api/client'

export interface UserNoticeRecipient {
  id: string
  userName: string
  realName?: string | null
}

export interface UserNoticeAdminListItem {
  id: string
  isUrgent: boolean
  isRead: boolean
  recipientUserId: string
  recipientLabel: string
  title: string
  bodyPreview: string
  imageCount: number
  createTime: string
}

export interface UserNoticeAdminPaged {
  items: UserNoticeAdminListItem[]
  total: number
  page: number
  pageSize: number
}

export interface UserNoticeImage {
  documentId: string
  originalFileName: string
}

export interface UserNoticeDetail {
  id: string
  isUrgent: boolean
  isRead: boolean
  recipientUserId: string
  recipientLabel: string
  title: string
  body: string
  images?: UserNoticeImage[]
  createTime: string
  readAt?: string | null
}

export interface UserNoticeMeListItem {
  id: string
  isUrgent: boolean
  isRead: boolean
  title: string
  bodyPreview: string
  imageCount: number
  createTime: string
}

export interface UserNoticeUnreadSummary {
  unreadCount: number
  hasUnreadUrgent: boolean
}

export interface UserNoticeAdminQuery {
  isUrgent?: boolean
  isRead?: boolean
  recipientUserId?: string
  keyword?: string
  sendFrom?: string
  sendTo?: string
  page?: number
  pageSize?: number
}

export interface UserNoticeSendPayload {
  recipientUserId: string
  isUrgent: boolean
  title: string
  body: string
}

export const sysUserNoticesApi = {
  recipients() {
    return apiClient.get<UserNoticeRecipient[]>('/api/v1/ops/user-notices/recipients')
  },
  adminList(params: UserNoticeAdminQuery) {
    return apiClient.get<UserNoticeAdminPaged>('/api/v1/ops/user-notices', { params })
  },
  adminGet(id: string) {
    return apiClient.get<UserNoticeDetail>(`/api/v1/ops/user-notices/${encodeURIComponent(id)}`)
  },
  send(payload: UserNoticeSendPayload, files?: File[]) {
    const form = new FormData()
    form.append('recipientUserId', payload.recipientUserId)
    form.append('isUrgent', payload.isUrgent ? 'true' : 'false')
    form.append('title', payload.title)
    form.append('body', payload.body ?? '')
    for (const f of files ?? []) {
      form.append('files', f)
    }
    return apiClient.post<UserNoticeDetail>('/api/v1/ops/user-notices', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 120_000
    })
  },
  unreadSummary() {
    return apiClient.get<UserNoticeUnreadSummary>('/api/v1/me/user-notices/unread-summary')
  },
  mine() {
    return apiClient.get<UserNoticeMeListItem[]>('/api/v1/me/user-notices')
  },
  getMine(id: string) {
    return apiClient.get<UserNoticeDetail>(`/api/v1/me/user-notices/${encodeURIComponent(id)}`)
  },
  markRead(id: string) {
    return apiClient.post<void>(`/api/v1/me/user-notices/${encodeURIComponent(id)}/read`)
  },
  markAllRead() {
    return apiClient.post<void>('/api/v1/me/user-notices/read-all')
  }
}
