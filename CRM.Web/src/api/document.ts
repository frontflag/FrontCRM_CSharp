import apiClient from './client'

export interface UploadDocumentDto {
  id: string
  bizType: string
  bizId: string
  originalFileName: string
  storedFileName: string
  relativePath: string
  fileSize: number
  fileExtension?: string
  mimeType?: string
  thumbnailRelativePath?: string
  remark?: string
  uploadUserId?: string
  createTime?: string
}

const BASE = '/api/v1/documents'

export const documentApi = {
  /** 上传文档（multipart/form-data） */
  async uploadDocuments(
    bizType: string,
    bizId: string,
    files: File[],
    remark?: string,
    uploadUserId?: string
  ): Promise<UploadDocumentDto[]> {
    const form = new FormData()
    form.append('bizType', bizType)
    form.append('bizId', bizId)
    if (remark) form.append('remark', remark)
    if (uploadUserId) form.append('uploadUserId', uploadUserId)
    files.forEach((f) => form.append('files', f))

    const res = await apiClient.post<any>(BASE + '/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    if (Array.isArray(res)) return res as UploadDocumentDto[]
    if (res && Array.isArray(res.data)) return res.data as UploadDocumentDto[]
    return (res && res.data) ?? []
  },

  /** 按业务查询文档列表 */
  async getDocuments(bizType: string, bizId: string): Promise<UploadDocumentDto[]> {
    const res = await apiClient.get<any>(`${BASE}?bizType=${encodeURIComponent(bizType)}&bizId=${encodeURIComponent(bizId)}`)
    if (Array.isArray(res)) return res as UploadDocumentDto[]
    if (res && Array.isArray(res.data)) return res.data as UploadDocumentDto[]
    return []
  },

  /** 管理端分页查询 */
  async searchDocumentsAdmin(params: {
    bizType?: string
    bizId?: string
    uploadUserId?: string
    remarkKeyword?: string
    startDate?: string
    endDate?: string
    pageNumber?: number
    pageSize?: number
  }): Promise<{ items: UploadDocumentDto[]; totalCount: number }> {
    const q = new URLSearchParams()
    if (params.bizType) q.set('bizType', params.bizType)
    if (params.bizId) q.set('bizId', params.bizId)
    if (params.uploadUserId) q.set('uploadUserId', params.uploadUserId)
    if (params.remarkKeyword) q.set('remarkKeyword', params.remarkKeyword)
    if (params.startDate) q.set('startDate', params.startDate)
    if (params.endDate) q.set('endDate', params.endDate)
    q.set('pageNumber', String(params.pageNumber ?? 1))
    q.set('pageSize', String(params.pageSize ?? 20))
    const res = await apiClient.get<any>(`${BASE}/admin?${q.toString()}`)
    if (res && res.items) return { items: res.items, totalCount: res.totalCount ?? 0 }
    return { items: [], totalCount: 0 }
  },

  /** 软删除 */
  async deleteDocument(id: string, userId?: string): Promise<void> {
    const url = userId ? `${BASE}/${id}?userId=${encodeURIComponent(userId)}` : `${BASE}/${id}`
    await apiClient.delete(url)
  },

  /** 相对路径预览（用于 iframe/img，需同源或代理） */
  getPreviewPath(id: string): string {
    return `${BASE}/${id}/preview`
  },

  /** 相对路径下载（仅同源且带 Cookie 时可用） */
  getDownloadPath(id: string): string {
    return `${BASE}/${id}/download`
  },

  /** 通过认证请求下载并触发保存 */
  async downloadDocument(id: string, filename?: string): Promise<void> {
    const res = await apiClient.get<Blob>(`${BASE}/${id}/download`, {
      responseType: 'blob'
    } as any)
    const blob = res instanceof Blob ? res : (res as any)?.data
    if (!blob || !(blob instanceof Blob)) return
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename || 'download'
    a.click()
    URL.revokeObjectURL(url)
  }
}
