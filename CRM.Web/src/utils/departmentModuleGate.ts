import type { RouteLocationNormalized } from 'vue-router'

/** 客户管理模块路由（列表/详情/编辑/子页） */
export function isCustomerManagementRoute(to: RouteLocationNormalized): boolean {
  const p = to.path.replace(/\/+$/, '').toLowerCase()
  if (p === '/custome' || p === '/customerlist') return true
  if (p === '/customers') return true
  return p.startsWith('/customers/')
}

/** 供应商管理模块路由（列表/详情/编辑/子页） */
export function isVendorManagementRoute(to: RouteLocationNormalized): boolean {
  const p = to.path.replace(/\/+$/, '').toLowerCase()
  if (p === '/vendor' || p === '/vendorlist') return true
  if (p === '/vendors') return true
  return p.startsWith('/vendors/')
}

/** 客户编辑/详情类路由（含新建、联系人、质保书） */
export function isCustomerEditOrDetailRoute(to: RouteLocationNormalized): boolean {
  const p = to.path.replace(/\/+$/, '').toLowerCase()
  if (p === '/customers/create') return true
  if (!p.startsWith('/customers/')) return false
  const rest = p.slice('/customers/'.length)
  if (!rest || rest === 'recycle-bin' || rest === 'blacklist' || rest === 'frozen') return false
  return true
}

/** 供应商编辑/详情类路由（含新建、联系人、质保书） */
export function isVendorEditOrDetailRoute(to: RouteLocationNormalized): boolean {
  const p = to.path.replace(/\/+$/, '').toLowerCase()
  if (p === '/vendors/create') return true
  if (!p.startsWith('/vendors/')) return false
  const rest = p.slice('/vendors/'.length)
  if (!rest || rest === 'recycle-bin' || rest === 'blacklist' || rest === 'frozen') return false
  return true
}

/** 报关板块路由（/customs/*） */
export function isCustomsModuleRoute(to: RouteLocationNormalized): boolean {
  const p = to.path.replace(/\/+$/, '').toLowerCase()
  return p.startsWith('/customs/')
}

/** 报关板块侧栏/API：仅系统管理员、财务部(5)、物流部(6)。 */
export function canAccessCustomsModule(input: {
  isSysAdmin?: boolean
  identityType?: number
}): boolean {
  if (input.isSysAdmin === true) return true
  const t = input.identityType ?? 0
  return t === 5 || t === 6
}
