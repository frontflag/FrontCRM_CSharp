import apiClient from '@/api/client'

export type AnnouncementType = 'platform_notice' | 'version_update'
export type AnnouncementStatus = 'draft' | 'published'

export interface AnnouncementDetail {
  id: string
  title: string
  type: AnnouncementType | string
  bodyMd: string
  status: AnnouncementStatus | string
  createTime: string
  publishedAt?: string | null
  publishedBy?: string | null
  modifyTime?: string | null
}

export interface AnnouncementAdminListItem {
  id: string
  title: string
  type: string
  status: string
  createTime: string
  publishedAt?: string | null
  modifyTime?: string | null
}

export interface AnnouncementHistoryItem {
  id: string
  title: string
  type: string
  publishedAt?: string | null
  isRead: boolean
}

export interface AnnouncementUnreadPreview {
  totalUnread: number
  items: AnnouncementDetail[]
}

export interface AnnouncementUpsertPayload {
  title: string
  type: AnnouncementType | string
  bodyMd: string
}

export const sysAnnouncementsApi = {
  adminList(params?: { status?: string; type?: string }): Promise<AnnouncementAdminListItem[]> {
    const q = new URLSearchParams()
    if (params?.status) q.set('status', params.status)
    if (params?.type) q.set('type', params.type)
    const qs = q.toString()
    return apiClient.get(`/api/v1/ops/announcements${qs ? `?${qs}` : ''}`) as Promise<AnnouncementAdminListItem[]>
  },
  adminGet(id: string): Promise<AnnouncementDetail> {
    return apiClient.get(`/api/v1/ops/announcements/${encodeURIComponent(id)}`) as Promise<AnnouncementDetail>
  },
  adminCreate(body: AnnouncementUpsertPayload): Promise<AnnouncementDetail> {
    return apiClient.post('/api/v1/ops/announcements', body) as Promise<AnnouncementDetail>
  },
  adminUpdate(id: string, body: AnnouncementUpsertPayload): Promise<AnnouncementDetail> {
    return apiClient.put(`/api/v1/ops/announcements/${encodeURIComponent(id)}`, body) as Promise<AnnouncementDetail>
  },
  adminDelete(id: string): Promise<void> {
    return apiClient.delete(`/api/v1/ops/announcements/${encodeURIComponent(id)}`) as Promise<void>
  },
  adminPublish(id: string): Promise<AnnouncementDetail> {
    return apiClient.post(`/api/v1/ops/announcements/${encodeURIComponent(id)}/publish`) as Promise<AnnouncementDetail>
  },

  unreadSummary(): Promise<{ totalUnread: number }> {
    return apiClient.get('/api/v1/me/announcements/unread-summary') as Promise<{ totalUnread: number }>
  },
  unreadPreview(limit = 5): Promise<AnnouncementUnreadPreview> {
    return apiClient.get(`/api/v1/me/announcements/unread-preview?limit=${limit}`) as Promise<AnnouncementUnreadPreview>
  },
  history(): Promise<AnnouncementHistoryItem[]> {
    return apiClient.get('/api/v1/me/announcements') as Promise<AnnouncementHistoryItem[]>
  },
  getPublished(id: string): Promise<AnnouncementDetail> {
    return apiClient.get(`/api/v1/me/announcements/${encodeURIComponent(id)}`) as Promise<AnnouncementDetail>
  },
  markRead(id: string): Promise<void> {
    return apiClient.post(`/api/v1/me/announcements/${encodeURIComponent(id)}/read`) as Promise<void>
  },
  markAllRead(): Promise<void> {
    return apiClient.post('/api/v1/me/announcements/read-all') as Promise<void>
  }
}
