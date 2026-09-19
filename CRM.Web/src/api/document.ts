import apiClient from './client'
import { normalizeUploadDocCategory, type UploadDocCategory } from '@/constants/uploadDocumentCategory'

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
  docCategory?: UploadDocCategory
  uploadUserId?: string
  createTime?: string
}

function pickStr(raw: Record<string, unknown>, ...keys: string[]): string {
  for (const k of keys) {
    const v = raw[k]
    if (v != null && String(v).trim()) return String(v).trim()
  }
  return ''
}

export function normalizeUploadDocument(row: unknown): UploadDocumentDto {
  const r = (row ?? {}) as Record<string, unknown>
  return {
    id: pickStr(r, 'id', 'Id'),
    bizType: pickStr(r, 'bizType', 'BizType'),
    bizId: pickStr(r, 'bizId', 'BizId'),
    originalFileName: pickStr(r, 'originalFileName', 'OriginalFileName'),
    storedFileName: pickStr(r, 'storedFileName', 'StoredFileName'),
    relativePath: pickStr(r, 'relativePath', 'RelativePath'),
    fileSize: Number(r.fileSize ?? r.FileSize ?? 0),
    fileExtension: pickStr(r, 'fileExtension', 'FileExtension') || undefined,
    mimeType: pickStr(r, 'mimeType', 'MimeType') || undefined,
    thumbnailRelativePath: pickStr(r, 'thumbnailRelativePath', 'ThumbnailRelativePath') || undefined,
    remark: pickStr(r, 'remark', 'Remark') || undefined,
    docCategory: normalizeUploadDocCategory(pickStr(r, 'docCategory', 'DocCategory')),
    uploadUserId: pickStr(r, 'uploadUserId', 'UploadUserId') || undefined,
    createTime: pickStr(r, 'createTime', 'CreateTime') || undefined
  }
}

function unwrapDocumentList(res: unknown): UploadDocumentDto[] {
  const raw = Array.isArray(res)
    ? res
    : Array.isArray((res as { data?: unknown })?.data)
      ? ((res as { data: unknown[] }).data)
      : []
  return raw.map(normalizeUploadDocument)
}

const BASE = '/api/v1/documents'

/** 质检单附件，bizId 为质检单主键（QCInfo.Id） */
export const DOCUMENT_BIZ_TYPE_QC = 'QC'

/** 订单明细「质检图片」页签只读展示行 */
export interface QcImageReadonlyRow {
  documentId: string
  qcId: string
  qcCode?: string | null
  stockInNotifyCode?: string | null
  originalFileName?: string | null
  mimeType?: string | null
  fileExtension?: string | null
  createTime: string
}

export const documentApi = {
  /** 上传文档（multipart/form-data） */
  async uploadDocuments(
    bizType: string,
    bizId: string,
    files: File[],
    remark?: string,
    uploadUserId?: string,
    docCategory?: string
  ): Promise<UploadDocumentDto[]> {
    const form = new FormData()
    form.append('bizType', bizType)
    form.append('bizId', bizId)
    if (remark) form.append('remark', remark)
    if (docCategory) form.append('docCategory', docCategory)
    if (uploadUserId) form.append('uploadUserId', uploadUserId)
    files.forEach((f) => form.append('files', f))

    const res = await apiClient.post<any>(BASE + '/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 120_000,
    })
    return unwrapDocumentList(res)
  },

  /** 按业务查询文档列表 */
  async getDocuments(bizType: string, bizId: string): Promise<UploadDocumentDto[]> {
    const res = await apiClient.get<any>(`${BASE}?bizType=${encodeURIComponent(bizType)}&bizId=${encodeURIComponent(bizId)}`)
    return unwrapDocumentList(res)
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
    if (res && res.items) return { items: (res.items as unknown[]).map(normalizeUploadDocument), totalCount: res.totalCount ?? 0 }
    if (res?.data?.items) return { items: (res.data.items as unknown[]).map(normalizeUploadDocument), totalCount: res.data.totalCount ?? 0 }
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

  /**
   * 新窗口预览：通过 axios 携带 JWT 拉取文件流，避免直接打开 /preview 时无 Authorization 导致失败。
   */
  async openPreviewInNewTab(id: string): Promise<void> {
    const blob = await apiClient.getBlob(`${BASE}/${encodeURIComponent(id)}/preview`)
    if (!blob?.size) {
      throw new Error('文件为空或不存在')
    }
    const url = URL.createObjectURL(blob)
    const w = window.open(url, '_blank', 'noopener,noreferrer')
    if (!w) {
      URL.revokeObjectURL(url)
      throw new Error('无法打开新窗口，请检查浏览器是否拦截弹窗')
    }
    window.setTimeout(() => URL.revokeObjectURL(url), 300_000)
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
