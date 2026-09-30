import apiClient from '@/api/client'

export interface BbsSubjectListItem {
  id: string
  title: string
  type: number
  typeLabel: string
  status: number
  statusLabel: string
  isTop: boolean
  isHot: boolean
  anonymous: boolean
  viewCount: number
  replyCount: number
  lastReplyTime: string | null
  createTime: string
  createBy: string | null
  authorDisplay: string
  canDelete: boolean
  canEdit: boolean
  canSetTop: boolean
  canModerate: boolean
}

export interface BbsSubjectDetail extends BbsSubjectListItem {
  content: string
}

export interface BbsSubjectPaged {
  total: number
  page: number
  pageSize: number
  items: BbsSubjectListItem[]
}

export interface BbsReplyItem {
  id: string
  subjectId: string
  content: string
  anonymous: boolean
  createTime: string
  createBy: string | null
  authorDisplay: string
  canDelete: boolean
}

export interface BbsReplyPaged {
  total: number
  page: number
  pageSize: number
  items: BbsReplyItem[]
}

/** 与后端 BbsSubjectTypes 一致 */
export const BbsSubjectType = {
  CompanyNotice: 1,
  IndustryNews: 2,
  Share: 3,
  OpsGuide: 4,
  Suggestion: 5,
  SystemUpdate: 6
} as const

export const BbsSubjectTypeOptions = [
  BbsSubjectType.CompanyNotice,
  BbsSubjectType.IndustryNews,
  BbsSubjectType.Share,
  BbsSubjectType.OpsGuide,
  BbsSubjectType.Suggestion,
  BbsSubjectType.SystemUpdate
] as const

export const BbsSubjectTypeI18nKey: Record<number, string> = {
  [BbsSubjectType.CompanyNotice]: 'bbs.types.companyNotice',
  [BbsSubjectType.IndustryNews]: 'bbs.types.industryNews',
  [BbsSubjectType.Share]: 'bbs.types.share',
  [BbsSubjectType.OpsGuide]: 'bbs.types.opsGuide',
  [BbsSubjectType.Suggestion]: 'bbs.types.suggestion',
  [BbsSubjectType.SystemUpdate]: 'bbs.types.systemUpdate'
}

export const BbsSubjectStatus = {
  Open: 1,
  Close: 2
} as const

export const bbsApi = {
  getTop(): Promise<BbsSubjectListItem[]> {
    return apiClient.get('/api/v1/bbs/subjects/top') as Promise<BbsSubjectListItem[]>
  },
  list(params: {
    type?: number | null
    status?: number | null
    keyword?: string
    page?: number
    pageSize?: number
  }): Promise<BbsSubjectPaged> {
    return apiClient.get('/api/v1/bbs/subjects', { params }) as Promise<BbsSubjectPaged>
  },
  detail(id: string): Promise<BbsSubjectDetail> {
    return apiClient.get(`/api/v1/bbs/subjects/${encodeURIComponent(id)}`) as Promise<BbsSubjectDetail>
  },
  create(body: { title: string; content: string; type: number; anonymous: boolean }): Promise<BbsSubjectDetail> {
    return apiClient.post('/api/v1/bbs/subjects', body) as Promise<BbsSubjectDetail>
  },
  update(
    id: string,
    body: { title: string; content: string; type: number; anonymous: boolean }
  ): Promise<BbsSubjectDetail> {
    return apiClient.put(`/api/v1/bbs/subjects/${encodeURIComponent(id)}`, body) as Promise<BbsSubjectDetail>
  },
  close(id: string): Promise<void> {
    return apiClient.post(`/api/v1/bbs/subjects/${encodeURIComponent(id)}/close`) as Promise<void>
  },
  open(id: string): Promise<void> {
    return apiClient.post(`/api/v1/bbs/subjects/${encodeURIComponent(id)}/open`) as Promise<void>
  },
  setTop(id: string): Promise<void> {
    return apiClient.post(`/api/v1/bbs/subjects/${encodeURIComponent(id)}/top`) as Promise<void>
  },
  untop(id: string): Promise<void> {
    return apiClient.post(`/api/v1/bbs/subjects/${encodeURIComponent(id)}/untop`) as Promise<void>
  },
  deleteSubject(id: string): Promise<void> {
    return apiClient.delete(`/api/v1/bbs/subjects/${encodeURIComponent(id)}`) as Promise<void>
  },
  replies(id: string, page = 1, pageSize = 50): Promise<BbsReplyPaged> {
    return apiClient.get(`/api/v1/bbs/subjects/${encodeURIComponent(id)}/replies`, {
      params: { page, pageSize }
    }) as Promise<BbsReplyPaged>
  },
  addReply(id: string, body: { content: string; anonymous: boolean }): Promise<BbsReplyItem> {
    return apiClient.post(
      `/api/v1/bbs/subjects/${encodeURIComponent(id)}/replies`,
      body
    ) as Promise<BbsReplyItem>
  },
  deleteReply(id: string): Promise<void> {
    return apiClient.delete(`/api/v1/bbs/replies/${encodeURIComponent(id)}`) as Promise<void>
  }
}
