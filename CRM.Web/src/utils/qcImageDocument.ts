import type { UploadDocumentDto } from '@/api/document'

const QC_IMAGE_EXTENSIONS = new Set(['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp'])

function normalizeExt(ext: string): string {
  const e = ext.trim().toLowerCase()
  if (!e) return ''
  return e.startsWith('.') ? e : `.${e}`
}

function extFromFileName(name?: string | null): string {
  if (!name) return ''
  const dot = name.lastIndexOf('.')
  if (dot < 0) return ''
  return normalizeExt(name.slice(dot))
}

/** 兼容 camelCase / PascalCase 的文档主键 */
export function resolveUploadDocumentId(d: UploadDocumentDto & { Id?: string }): string {
  return String(d.id ?? d.Id ?? '').trim()
}

/** 判定是否为质检图片附件（与采购/销售明细「质检图片」页签口径一致） */
export function isQcImageDocument(d: UploadDocumentDto): boolean {
  const mime = String(
    d.mimeType ?? (d as { MimeType?: string }).MimeType ?? ''
  ).toLowerCase()
  if (mime.startsWith('image/')) return true

  const ext = normalizeExt(
    String(d.fileExtension ?? (d as { FileExtension?: string }).FileExtension ?? '')
  )
  if (ext && QC_IMAGE_EXTENSIONS.has(ext)) return true

  return QC_IMAGE_EXTENSIONS.has(extFromFileName(d.originalFileName))
}

export function filterQcImageDocuments(docs: UploadDocumentDto[]): UploadDocumentDto[] {
  return (docs ?? []).filter(isQcImageDocument)
}

export function countQcImageDocuments(docs: UploadDocumentDto[]): number {
  return filterQcImageDocuments(docs).length
}

/** 缩略图加载失败时 el-upload 占位图（data URL，无需 revoke） */
export const QC_IMAGE_UPLOAD_PLACEHOLDER_URL =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="148" height="148">' +
      '<rect width="148" height="148" fill="#f5f7fa"/>' +
      '<text x="74" y="78" text-anchor="middle" fill="#909399" font-size="12">图片</text>' +
      '</svg>'
  )

export async function fetchQcDocumentPreviewBlob(
  documentId: string,
  getBlob: (url: string) => Promise<Blob>
): Promise<Blob | null> {
  const id = documentId.trim()
  if (!id) return null

  const paths = [
    `/api/v1/documents/${encodeURIComponent(id)}/preview?thumbnail=true`,
    `/api/v1/documents/${encodeURIComponent(id)}/preview`
  ]

  for (const path of paths) {
    try {
      const blob = await getBlob(path)
      if (blob instanceof Blob && blob.size > 0) return blob
    } catch {
      /* try next */
    }
  }
  return null
}
