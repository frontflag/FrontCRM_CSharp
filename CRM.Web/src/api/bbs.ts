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
  likeCount: number
  dislikeCount: number
  myReaction: number
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

export interface BbsReactionResult {
  likeCount: number
  dislikeCount: number
  myReaction: number
}

export const BbsReactionValue = {
  Like: 1,
  Dislike: -1,
  None: 0
} as const

export interface BbsReplyPaged {
  total: number
  page: number
  pageSize: number
  items: BbsReplyItem[]
}

export interface BbsBoardStatItem {
  key: string
  type?: number | null
  subjectCount: number
  viewCount: number
}

export interface BbsBoardStats {
  all: BbsBoardStatItem
  top: BbsBoardStatItem
  byType: BbsBoardStatItem[]
}

export interface BbsBoardModerator {
  type: number
  userId?: string | null
  userName?: string | null
  realName?: string | null
  displayName?: string | null
  defaultName?: string | null
  isDeleted?: boolean
  subjectCount?: number
  sortOrder?: number
}

/** 与后端 BbsSubjectTypes 一致 */
export const BbsSubjectType = {
  CompanyNotice: 1,
  IndustryNews: 2,
  Share: 3,
  OpsGuide: 4,
  Suggestion: 5,
  SystemUpdate: 6,
  CustomMin: 100
} as const

/** 固定在置顶下方、不可拖放调整顺序的板块 */
export const BbsFixedBoardTypes = [
  BbsSubjectType.SystemUpdate,
  BbsSubjectType.OpsGuide
] as const

/** 内置可拖放板块（不含固定板块） */
export const BbsBuiltinMovableTypes = [
  BbsSubjectType.CompanyNotice,
  BbsSubjectType.IndustryNews,
  BbsSubjectType.Share,
  BbsSubjectType.Suggestion
] as const

export const BbsSubjectTypeOptions = [
  BbsSubjectType.SystemUpdate,
  BbsSubjectType.OpsGuide,
  BbsSubjectType.CompanyNotice,
  BbsSubjectType.IndustryNews,
  BbsSubjectType.Share,
  BbsSubjectType.Suggestion
] as const

export const BbsSubjectTypeI18nKey: Record<number, string> = {
  [BbsSubjectType.CompanyNotice]: 'bbs.types.companyNotice',
  [BbsSubjectType.IndustryNews]: 'bbs.types.industryNews',
  [BbsSubjectType.Share]: 'bbs.types.share',
  [BbsSubjectType.OpsGuide]: 'bbs.types.opsGuide',
  [BbsSubjectType.Suggestion]: 'bbs.types.suggestion',
  [BbsSubjectType.SystemUpdate]: 'bbs.types.systemUpdate'
}

/** 系统更新 / 操作说明：仅 SYS_ADMIN 可发帖，不设版主 */
export function bbsIsAdminOnlyPostType(type: number) {
  return type === BbsSubjectType.OpsGuide || type === BbsSubjectType.SystemUpdate
}

export function bbsIsCustomBoardType(type: number) {
  return Number(type) >= BbsSubjectType.CustomMin
}

export function bbsSupportsBoardModerator(type: number) {
  return !bbsIsAdminOnlyPostType(type)
}

export function bbsIsFixedBoardType(type: number) {
  return bbsIsAdminOnlyPostType(type)
}

function mapBoardModerator(row: unknown, fallbackType?: number): BbsBoardModerator {
  const r = (row ?? {}) as Record<string, unknown>
  return {
    type: Number(r.type ?? r.Type ?? fallbackType ?? 0),
    userId: (r.userId ?? r.UserId) as string | null | undefined,
    userName: (r.userName ?? r.UserName) as string | null | undefined,
    realName: (r.realName ?? r.RealName) as string | null | undefined,
    displayName: (r.displayName ?? r.DisplayName) as string | null | undefined,
    defaultName: (r.defaultName ?? r.DefaultName) as string | null | undefined,
    isDeleted: !!(r.isDeleted ?? r.IsDeleted),
    subjectCount: Number(r.subjectCount ?? r.SubjectCount ?? 0),
    sortOrder: Number(r.sortOrder ?? r.SortOrder ?? 0)
  }
}

export const BbsSubjectStatus = {
  Open: 1,
  Close: 2
} as const

export const bbsApi = {
  getTop(): Promise<BbsSubjectListItem[]> {
    return apiClient.get('/api/v1/bbs/subjects/top') as Promise<BbsSubjectListItem[]>
  },
  boardStats(): Promise<BbsBoardStats> {
    return apiClient.get('/api/v1/bbs/board-stats').then((raw: unknown) => {
      const r = (raw ?? {}) as Record<string, unknown>
      const mapItem = (x: unknown, fallbackKey: string): BbsBoardStatItem => {
        const o = (x ?? {}) as Record<string, unknown>
        return {
          key: String(o.key ?? o.Key ?? fallbackKey),
          type: o.type != null || o.Type != null ? Number(o.type ?? o.Type) : null,
          subjectCount: Number(o.subjectCount ?? o.SubjectCount ?? 0),
          viewCount: Number(o.viewCount ?? o.ViewCount ?? 0)
        }
      }
      const byTypeRaw = (r.byType ?? r.ByType ?? []) as unknown[]
      return {
        all: mapItem(r.all ?? r.All, 'all'),
        top: mapItem(r.top ?? r.Top, 'top'),
        byType: byTypeRaw.map((x, i) => mapItem(x, String(i + 1)))
      }
    })
  },
  boardModerators(): Promise<BbsBoardModerator[]> {
    return apiClient.get('/api/v1/bbs/board-moderators').then((raw: unknown) => {
      const list = Array.isArray(raw) ? raw : []
      return list.map((row) => mapBoardModerator(row))
    })
  },
  createBoard(body: { displayName: string }): Promise<BbsBoardModerator> {
    return apiClient
      .post('/api/v1/bbs/board-moderators', body)
      .then((raw: unknown) => mapBoardModerator(raw))
  },
  reorderBoards(orderedTypes: number[]): Promise<void> {
    return apiClient.put('/api/v1/bbs/board-moderators/sort', { orderedTypes }) as Promise<void>
  },
  setBoardModerator(
    type: number,
    body: { userId: string | null; displayName: string | null; sortOrder?: number | null }
  ): Promise<BbsBoardModerator> {
    return apiClient
      .put(`/api/v1/bbs/board-moderators/${type}`, body)
      .then((raw: unknown) => mapBoardModerator(raw, type))
  },
  deleteBoard(type: number): Promise<void> {
    return apiClient.delete(`/api/v1/bbs/board-moderators/${type}`) as Promise<void>
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
  },
  listMedia(subjectId: string): Promise<BbsMediaItem[]> {
    return apiClient
      .get(`/api/v1/bbs/subjects/${encodeURIComponent(subjectId)}/media`)
      .then((raw: unknown) => {
        const list = Array.isArray(raw) ? raw : []
        return list.map(normalizeBbsMedia)
      })
  },
  uploadMedia(subjectId: string, files: File[]): Promise<BbsMediaItem[]> {
    const form = new FormData()
    files.forEach((f) => form.append('files', f))
    return apiClient
      .post(`/api/v1/bbs/subjects/${encodeURIComponent(subjectId)}/media`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 180_000
      })
      .then((raw: unknown) => {
        const list = Array.isArray(raw) ? raw : []
        return list.map(normalizeBbsMedia)
      })
  },
  deleteMedia(documentId: string): Promise<void> {
    return apiClient.delete(`/api/v1/bbs/media/${encodeURIComponent(documentId)}`) as Promise<void>
  },
  reactSubject(id: string, value: number): Promise<BbsReactionResult> {
    return apiClient.post(`/api/v1/bbs/subjects/${encodeURIComponent(id)}/reaction`, {
      value
    }) as Promise<BbsReactionResult>
  }
}

export interface BbsMediaItem {
  id: string
  originalFileName: string
  mimeType?: string | null
  fileExtension?: string | null
  fileSize: number
  kind: 'image' | 'video' | string
  previewPath: string
}

function normalizeBbsMedia(row: unknown): BbsMediaItem {
  const r = (row ?? {}) as Record<string, unknown>
  return {
    id: String(r.id ?? r.Id ?? ''),
    originalFileName: String(r.originalFileName ?? r.OriginalFileName ?? ''),
    mimeType: (r.mimeType ?? r.MimeType) as string | null | undefined,
    fileExtension: (r.fileExtension ?? r.FileExtension) as string | null | undefined,
    fileSize: Number(r.fileSize ?? r.FileSize ?? 0),
    kind: String(r.kind ?? r.Kind ?? 'image'),
    previewPath: String(r.previewPath ?? r.PreviewPath ?? '')
  }
}
