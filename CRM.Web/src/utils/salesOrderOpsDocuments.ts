const PREVIEW_EXTS = new Set(['.pdf', '.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp'])

function extFromName(name?: string | null): string {
  const s = String(name ?? '').trim().toLowerCase()
  const i = s.lastIndexOf('.')
  if (i < 0) return ''
  return s.slice(i)
}

function normalizeExt(raw?: string | null, fileName?: string | null): string {
  const e = String(raw ?? '').trim().toLowerCase()
  if (e) return e.startsWith('.') ? e : `.${e}`
  return extFromName(fileName)
}

/** 图片与 PDF 可在预览对话框内查看；其它格式需确认后下载。 */
export function isInlinePreviewableUpload(doc: {
  mimeType?: string | null
  fileExtension?: string | null
  originalFileName?: string | null
}): boolean {
  const mime = String(doc.mimeType ?? '').trim().toLowerCase()
  if (mime.startsWith('image/') || mime.includes('pdf')) return true
  return PREVIEW_EXTS.has(normalizeExt(doc.fileExtension, doc.originalFileName))
}

export function previewMimeForUpload(doc: {
  mimeType?: string | null
  fileExtension?: string | null
  originalFileName?: string | null
}): string {
  const mime = String(doc.mimeType ?? '').trim()
  if (mime) return mime
  const ext = normalizeExt(doc.fileExtension, doc.originalFileName)
  if (ext === '.pdf') return 'application/pdf'
  if (PREVIEW_EXTS.has(ext) && ext !== '.pdf') return 'image/' + ext.slice(1)
  return ''
}

export const SO_OPS_DOCS_EXPANDED_STORAGE_KEY = 'frontcrm.so-item-ops.docs-expanded'
export const PO_OPS_DOCS_EXPANDED_STORAGE_KEY = 'frontcrm.po-item-ops.docs-expanded'
export const STOCK_OUT_OPS_DOCS_EXPANDED_STORAGE_KEY = 'frontcrm.stock-out-ops.docs-expanded'

export function readOpsDocsExpanded(key: string): boolean {
  try {
    return localStorage.getItem(key) === '1'
  } catch {
    return false
  }
}

export function writeOpsDocsExpanded(key: string, expanded: boolean): void {
  try {
    localStorage.setItem(key, expanded ? '1' : '0')
  } catch {
    /* ignore quota / private mode */
  }
}

/** 从未写过偏好时视为收起。 */
export function readSoOpsDocsExpanded(): boolean {
  return readOpsDocsExpanded(SO_OPS_DOCS_EXPANDED_STORAGE_KEY)
}

export function writeSoOpsDocsExpanded(expanded: boolean): void {
  writeOpsDocsExpanded(SO_OPS_DOCS_EXPANDED_STORAGE_KEY, expanded)
}

