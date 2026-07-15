import type { UploadFile } from 'element-plus'
import type { QcImageReadonlyRow } from '@/api/document'
import type { ImageBrowserItem } from '@/types/imageBrowser'

export function qcReadonlyRowsToBrowserItems(rows: QcImageReadonlyRow[]): ImageBrowserItem[] {
  const items: ImageBrowserItem[] = []
  for (const row of rows ?? []) {
    const documentId = String(row.documentId || '').trim()
    if (!documentId) continue
    const name = String(row.originalFileName || '').trim() || documentId
    items.push({ id: documentId, name, documentId })
  }
  return items
}

export function resolveBrowserItemDisplayName(name: string, fallbackId: string): string {
  const n = String(name || '').trim()
  return n || fallbackId || '—'
}

export function qcUploadFilesToBrowserItems(
  files: Array<UploadFile & { documentId?: string }>
): ImageBrowserItem[] {
  return (files ?? []).map((f, idx) => {
    const documentId = String(f.documentId || '').trim()
    const uid = String(f.uid || idx)
    const name = String(f.name || '').trim() || documentId || uid
    const previewUrl = String(f.url || '').trim() || undefined
    return {
      id: documentId || uid,
      name,
      documentId: documentId || undefined,
      previewUrl: documentId ? undefined : previewUrl
    } satisfies ImageBrowserItem
  })
}
