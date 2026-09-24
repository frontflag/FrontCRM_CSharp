import apiClient from './client'

export type KbDocumentVersion = {
  documentId: string
  documentCode: string
  title: string
  versionId: string
  versionNo: number
  status: number
  isActive: boolean
  chunkCount: number
  errorMessage?: string | null
  sourceFileName: string
}

export type KbChunkListItem = {
  id: string
  chunkIndex: number
  heading: string
  chapterNo?: string | null
  sectionNo?: string | null
  contentChars: number
  excerpt: string
}

export type KbCitation = {
  heading: string
  excerpt: string
  distance: number
}

export type KbAskResult = {
  covered: boolean
  answer: string
  documentTitle?: string | null
  versionNo?: number | null
  fromCache: boolean
  citations: KbCitation[]
}

export type KbImportResult = {
  documentId: string
  versionId: string
  versionNo: number
  status: number
}

const base = '/api/v1/kb'

export const knowledgeBaseApi = {
  listVersions(code = 'handbook.distributor.newcomer') {
    return apiClient.get<KbDocumentVersion[]>(`${base}/documents/${code}/versions`)
  },
  listChunks(versionId: string, page = 1, pageSize = 20) {
    return apiClient.get<KbChunkListItem[]>(`${base}/versions/${versionId}/chunks`, { params: { page, pageSize } })
  },
  upload(file: File, title?: string, code = 'handbook.distributor.newcomer') {
    const form = new FormData()
    form.append('file', file)
    if (title) form.append('title', title)
    return apiClient.post<KbImportResult>(`${base}/documents/${code}/versions`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 120000
    })
  },
  activate(versionId: string) {
    return apiClient.post<boolean>(`${base}/versions/${versionId}/activate`)
  },
  ask(question: string) {
    return apiClient.post<KbAskResult>(`${base}/ask`, { question }, { timeout: 180000 })
  }
}
