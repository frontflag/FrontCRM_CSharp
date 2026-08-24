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
  createTime: string
}

export interface UserNoticeAdminPaged {
  items: UserNoticeAdminListItem[]
  total: number
  page: number
  pageSize: number
}

export interface UserNoticeDetail {
  id: string
  isUrgent: boolean
  isRead: boolean
  recipientUserId: string
  recipientLabel: string
  title: string
  body: string
  createTime: string
  readAt?: string | null
}

export interface UserNoticeMeListItem {
  id: string
  isUrgent: boolean
  isRead: boolean
  title: string
  bodyPreview: string
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
  send(payload: UserNoticeSendPayload) {
    return apiClient.post<UserNoticeDetail>('/api/v1/ops/user-notices', payload)
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
