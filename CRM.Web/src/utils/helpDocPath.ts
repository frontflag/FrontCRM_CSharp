import type { RouteLocationNormalizedLoaded } from 'vue-router'

/**
 * 按当前路径前缀映射 help 子目录（与仓库 help/ 下文件夹一致）。
 * 较长前缀在前，避免 /purchase-orders 被 /purchase 误匹配。
 */
const MODULE_RULES: { prefix: string; folder: string }[] = [
  { prefix: '/customerlist', folder: 'customer' },
  { prefix: '/custome', folder: 'customer' },
  { prefix: '/customers', folder: 'customer' },
  { prefix: '/vendorlist', folder: 'vendor' },
  { prefix: '/vendor', folder: 'vendor' },
  { prefix: '/vendors', folder: 'vendor' },
  { prefix: '/rfq-items', folder: 'rfq' },
  { prefix: '/rfqlist', folder: 'rfq' },
  { prefix: '/rfqs', folder: 'rfq' },
  { prefix: '/rfq', folder: 'rfq' },
  { prefix: '/pn', folder: 'rfq' },
  { prefix: '/boms', folder: 'bom' },
  { prefix: '/inventory', folder: 'inventory' },
  { prefix: '/quotes', folder: 'quote' },
  { prefix: '/purchase-requisitions', folder: 'purchase' },
  { prefix: '/purchase-order-items', folder: 'purchase' },
  { prefix: '/purchase-orders', folder: 'purchase' },
  { prefix: '/sales-order-items', folder: 'sales' },
  { prefix: '/sales-orders', folder: 'sales' },
  { prefix: '/stock-out-notifies', folder: 'inventory' },
  { prefix: '/logistics', folder: 'logistics' },
  { prefix: '/finance', folder: 'finance' },
  { prefix: '/system', folder: 'system' },
  { prefix: '/dashboard', folder: 'dashboard' },
  { prefix: '/profile', folder: 'profile' },
  { prefix: '/drafts', folder: 'draft' },
  { prefix: '/pending-approvals', folder: 'approval' },
  { prefix: '/documents', folder: 'document' },
  { prefix: '/debug', folder: 'debug' }
]

export function helpModuleFolder(path: string): string {
  const p = path.startsWith('/') ? path : `/${path}`
  for (const { prefix, folder } of MODULE_RULES) {
    if (p === prefix || p.startsWith(`${prefix}/`)) return folder
  }
  return 'common'
}

/** 页面文档文件名 = 路由 name（与 help/{module}/{name}.md 对应） */
export function helpPageKey(route: RouteLocationNormalizedLoaded): string | null {
  const n = route.name
  if (typeof n !== 'string' || !n) return null
  return n
}

/** 静态资源根路径下的 help 文档 URL（供 fetch） */
export function helpMarkdownFetchUrl(route: RouteLocationNormalizedLoaded): string | null {
  const pageKey = helpPageKey(route)
  if (!pageKey) return null
  const module = helpModuleFolder(route.path)
  const base = import.meta.env.BASE_URL || '/'
  const root = base.endsWith('/') ? base.slice(0, -1) : base
  const rel = `help/${module}/${pageKey}.md`
  if (!root) return `/${rel}`
  return `${root}/${rel}`
}
