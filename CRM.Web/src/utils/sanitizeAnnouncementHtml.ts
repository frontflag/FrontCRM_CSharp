import { marked } from 'marked'
import DOMPurify from 'dompurify'
import apiClient from '@/api/client'

marked.setOptions({ gfm: true, breaks: true })

/** 允许相对路径、http(s)、blob（本地预览 / 鉴权 blob） */
const ALLOWED_URI =
  /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp|blob):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i

const STYLE_ALLOWED = new Set([
  'color',
  'background-color',
  'font-size',
  'font-family',
  'text-align',
  'font-weight',
  'font-style',
  'text-decoration'
])

let styleHookInstalled = false
function ensureStyleHook() {
  if (styleHookInstalled) return
  styleHookInstalled = true
  DOMPurify.addHook('uponSanitizeAttribute', (_node, data) => {
    if (data.attrName !== 'style') return
    const kept: string[] = []
    for (const part of String(data.attrValue || '').split(';')) {
      const idx = part.indexOf(':')
      if (idx <= 0) continue
      const prop = part.slice(0, idx).trim().toLowerCase()
      const val = part.slice(idx + 1).trim()
      if (!prop || !val) continue
      if (!STYLE_ALLOWED.has(prop)) continue
      if (/expression|url\s*\(|@import|javascript:/i.test(val)) continue
      kept.push(`${prop}: ${val}`)
    }
    if (kept.length) data.attrValue = kept.join('; ')
    else data.keepAttr = false
  })
}

/**
 * 宽松 ATX 标题：`#标题` / `##22` → `# 标题` / `## 22`
 *（标准 CommonMark 要求 # 后必须有空格，国内输入习惯常省略）
 */
export function normalizeMarkdownAtxHeadings(md: string): string {
  return (md || '').replace(/^(#{1,6})([^\s#])/gm, '$1 $2')
}

/** 是否像 HTML 正文（富文本编辑器产出）。 */
export function isLikelyHtmlContent(content: string): boolean {
  const t = (content || '').trim()
  if (!t) return false
  return /^<[a-z!/?]/i.test(t)
}

function openLinksInNewTab(html: string): string {
  return html.replace(/<a\s/gi, '<a target="_blank" rel="noopener noreferrer" ')
}

function purifyHtml(html: string): string {
  ensureStyleHook()
  return DOMPurify.sanitize(html, {
    USE_PROFILES: { html: true },
    ADD_TAGS: ['video', 'source'],
    ADD_ATTR: ['style', 'class', 'target', 'rel', 'controls', 'src', 'type', 'preload', 'width', 'height'],
    ALLOWED_URI_REGEXP: ALLOWED_URI
  })
}

/** Markdown → 消毒后的 HTML（链接新窗口打开；允许 img/video）。 */
export function renderAnnouncementMarkdown(md: string): string {
  const normalized = normalizeMarkdownAtxHeadings(md)
  const raw = marked.parse(normalized || '', { async: false }) as string
  return openLinksInNewTab(purifyHtml(raw))
}

function expandStoredFontFamilies(html: string): string {
  const map: Record<string, string> = {
    'microsoft-yahei': 'Microsoft YaHei, 微软雅黑, sans-serif',
    simsun: 'SimSun, 宋体, serif',
    simhei: 'SimHei, 黑体, sans-serif',
    kaiti: 'KaiTi, 楷体, serif',
    fangsong: 'FangSong, 仿宋, serif'
  }
  let out = html
  for (const [key, css] of Object.entries(map)) {
    const re = new RegExp(`font-family:\\s*${key}\\b`, 'gi')
    out = out.replace(re, `font-family: ${css}`)
  }
  return out
}

/** 消毒富文本 HTML（保留字号/字体/颜色等安全 style）。 */
export function sanitizeBbsHtml(html: string): string {
  return openLinksInNewTab(purifyHtml(expandStoredFontFamilies(html || '')))
}

/** 论坛正文：HTML 走消毒，否则按 Markdown 渲染。 */
export function renderBbsContent(content: string): string {
  if (isLikelyHtmlContent(content)) return sanitizeBbsHtml(content)
  return renderAnnouncementMarkdown(content)
}

/**
 * 将正文中 `/api/v1/documents/{id}/preview` 的 img/video 换成带鉴权的 blob URL。
 * 返回需要 revoke 的 object URL 列表。
 */
export async function resolveAnnouncementDocumentImages(root: HTMLElement | null): Promise<string[]> {
  if (!root) return []
  const urls: string[] = []
  const nodes = [
    ...Array.from(root.querySelectorAll('img')),
    ...Array.from(root.querySelectorAll('video'))
  ]
  for (const el of nodes) {
    const src = el.getAttribute('src') || ''
    const m = src.match(/^\/api\/v1\/documents\/([^/?#]+)\/preview/i)
    if (!m) continue
    const id = decodeURIComponent(m[1])
    try {
      const blob = await apiClient.getBlob(`/api/v1/documents/${encodeURIComponent(id)}/preview`)
      if (!blob?.size) continue
      const obj = URL.createObjectURL(blob)
      urls.push(obj)
      el.setAttribute('src', obj)
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
