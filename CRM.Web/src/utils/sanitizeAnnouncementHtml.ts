import { marked } from 'marked'
import DOMPurify from 'dompurify'
import apiClient from '@/api/client'

marked.setOptions({ gfm: true, breaks: true })

/** Markdown → 消毒后的 HTML（链接新窗口打开）。 */
export function renderAnnouncementMarkdown(md: string): string {
  const raw = marked.parse(md || '', { async: false }) as string
  const clean = DOMPurify.sanitize(raw, {
    USE_PROFILES: { html: true },
    ADD_ATTR: ['target', 'rel']
  })
  return clean.replace(/<a\s/gi, '<a target="_blank" rel="noopener noreferrer" ')
}

/**
 * 将正文中 `/api/v1/documents/{id}/preview` 图片换成带鉴权的 blob URL。
 * 返回需要 revoke 的 object URL 列表。
 */
export async function resolveAnnouncementDocumentImages(root: HTMLElement | null): Promise<string[]> {
  if (!root) return []
  const urls: string[] = []
  const imgs = Array.from(root.querySelectorAll('img'))
  for (const img of imgs) {
    const src = img.getAttribute('src') || ''
    const m = src.match(/^\/api\/v1\/documents\/([^/?#]+)\/preview/i)
    if (!m) continue
    const id = decodeURIComponent(m[1])
    try {
      const blob = await apiClient.getBlob(`/api/v1/documents/${encodeURIComponent(id)}/preview`)
      if (!blob?.size) continue
      const obj = URL.createObjectURL(blob)
      urls.push(obj)
      img.src = obj
    } catch {
      /* 保留原 src */
    }
  }
  return urls
}

export function revokeObjectUrls(urls: string[]) {
  for (const u of urls) {
    try {
      URL.revokeObjectURL(u)
    } catch {
      /* ignore */
    }
  }
}
