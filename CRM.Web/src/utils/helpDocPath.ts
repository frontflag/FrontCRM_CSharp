import type { RouteLocationNormalizedLoaded } from 'vue-router'
import registry from '../../../help/menu-registry.json'

export type HelpMenuEntry = {
  id: string
  label: string
  routeNames?: string[]
  pathPrefixes?: string[]
}

type RegistryFile = {
  version: number
  pagesDir?: string
  catalogFile?: string
  entries: HelpMenuEntry[]
}

const reg = registry as RegistryFile

function staticBase(): string {
  const base = import.meta.env.BASE_URL || '/'
  return base.endsWith('/') ? base.slice(0, -1) : base
}

export function helpCatalogFileName(): string {
  return reg.catalogFile || '帮助文档目录.md'
}

/** 帮助文档目录相对 /help/ 根的路径 */
export function helpCatalogRelativePath(): string {
  return helpCatalogFileName()
}

export function helpPagesDir(): string {
  return reg.pagesDir || 'pages'
}

export function helpMenuEntryDocRelativePath(entry: HelpMenuEntry): string {
  const dir = helpPagesDir()
  return `${dir}/${entry.label}_${entry.id}.md`
}

export function resolveHelpMenuEntry(route: RouteLocationNormalizedLoaded): HelpMenuEntry | null {
  const entries = reg.entries || []
  const name = route.name
  if (typeof name === 'string' && name) {
    const byName = entries.find((e) => e.routeNames?.includes(name))
    if (byName) return byName
  }
  const path = route.path
  let best: HelpMenuEntry | null = null
  let bestLen = -1
  for (const e of entries) {
    for (const p of e.pathPrefixes || []) {
      if (!p) continue
      if (path === p || path.startsWith(`${p}/`)) {
        if (p.length > bestLen) {
          best = e
          bestLen = p.length
        }
      }
    }
  }
  return best
}

/** 当前路由对应菜单项的帮助 MD（相对 /help/）；无匹配返回 null */
export function helpDocRelativePathForRoute(route: RouteLocationNormalizedLoaded): string | null {
  const entry = resolveHelpMenuEntry(route)
  if (entry) return helpMenuEntryDocRelativePath(entry)
  return null
}

/** 拼接为可 fetch 的绝对路径（含 BASE_URL） */
export function helpAssetUrl(relativeToHelpRoot: string): string {
  const root = staticBase()
  const rel = relativeToHelpRoot.replace(/^\/+/, '')
  if (!root) return `/help/${rel}`
  return `${root}/help/${rel}`
}

/**
 * 仍在使用旧 API 处：传入 route 得默认帮助 URL；无映射时返回目录 URL
 * @deprecated 请优先使用 helpDocRelativePathForRoute + helpAssetUrl
 */
export function helpMarkdownFetchUrl(route: RouteLocationNormalizedLoaded): string | null {
  const rel = helpDocRelativePathForRoute(route) ?? helpCatalogRelativePath()
  return helpAssetUrl(rel)
}

/** 将点击的 a[href] 解析为相对 help 根的路径；非站内 .md 返回 null */
export function resolveHelpLinkHref(
  rawHref: string,
  currentDocRelativePath: string
): string | null {
  const t = rawHref.trim()
  if (!t || t.startsWith('mailto:')) return null

  if (t.startsWith('#')) return null

  if (t.endsWith('.md')) {
    if (t.startsWith('http://') || t.startsWith('https://')) {
      try {
        const u = new URL(t)
        const pathname = decodeURIComponent(u.pathname)
        const marker = '/help/'
        const i = pathname.indexOf(marker)
        if (i !== -1) return pathname.slice(i + marker.length).replace(/^\//, '')
      } catch {
        return null
      }
      return null
    }

    if (t.startsWith('/')) {
      const pathname = decodeURIComponent(t)
      const marker = '/help/'
      const i = pathname.indexOf(marker)
      if (i !== -1) return pathname.slice(i + marker.length).replace(/^\//, '')
      if (pathname.endsWith('.md')) return pathname.replace(/^\//, '')
      return null
    }

    const baseDir =
      currentDocRelativePath.lastIndexOf('/') >= 0
        ? currentDocRelativePath.slice(0, currentDocRelativePath.lastIndexOf('/') + 1)
        : ''
    const parts = (baseDir + t).split('/').filter(Boolean)
    const stack: string[] = []
    for (const seg of parts) {
      if (seg === '..') stack.pop()
      else if (seg !== '.') stack.push(seg)
    }
    const out = stack.join('/')
    return out.endsWith('.md') ? out : null
  }

  return null
}
