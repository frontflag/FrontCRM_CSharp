/** 主菜单分组展开/收起偏好（localStorage，按用户隔离）。 */

export const SIDEBAR_MENU_GROUP_KEYS = [
  'mine',
  'analytics',
  'purchase',
  'sales',
  'inventory',
  'stockInManagement',
  'customs',
  'stockOutManagement',
  'customers',
  'vendors',
  'rfqs',
  'quotes',
  'finance',
  'financePayments',
  'financeReceipts',
  'financeInventoryReports',
  'ops',
  'systemManagement',
  'paramManagement',
  'systemLogs'
] as const

export type SidebarMenuGroupKey = (typeof SIDEBAR_MENU_GROUP_KEYS)[number]
export type SidebarMenuGroupsState = Record<SidebarMenuGroupKey, boolean>

const STORAGE_PREFIX = 'crm-sidebar-menu-groups:v1:'

export function sidebarMenuGroupsStorageKey(userId: string): string {
  return STORAGE_PREFIX + userId.trim()
}

export function defaultSidebarMenuGroups(): SidebarMenuGroupsState {
  return {
    mine: true,
    analytics: false,
    purchase: false,
    sales: false,
    inventory: false,
    stockInManagement: false,
    customs: false,
    stockOutManagement: false,
    customers: false,
    vendors: false,
    rfqs: false,
    quotes: false,
    finance: false,
    financePayments: false,
    financeReceipts: false,
    financeInventoryReports: false,
    ops: false,
    systemManagement: false,
    paramManagement: false,
    systemLogs: false
  }
}

export function collapsedSidebarMenuGroups(): SidebarMenuGroupsState {
  const next = defaultSidebarMenuGroups()
  for (const key of SIDEBAR_MENU_GROUP_KEYS) next[key] = false
  return next
}

export function expandedSidebarMenuGroups(): SidebarMenuGroupsState {
  const next = defaultSidebarMenuGroups()
  for (const key of SIDEBAR_MENU_GROUP_KEYS) next[key] = true
  return next
}

export function readSidebarMenuGroups(userId: string): SidebarMenuGroupsState | null {
  const uid = userId.trim()
  if (!uid) return null
  try {
    const raw = localStorage.getItem(sidebarMenuGroupsStorageKey(uid))
    if (!raw) return null
    const parsed = JSON.parse(raw) as Partial<Record<string, unknown>>
    if (!parsed || typeof parsed !== 'object') return null
    const next = defaultSidebarMenuGroups()
    let hit = false
    for (const key of SIDEBAR_MENU_GROUP_KEYS) {
      if (typeof parsed[key] === 'boolean') {
        next[key] = parsed[key]
        hit = true
      }
    }
    return hit ? next : null
  } catch {
    return null
  }
}

export function writeSidebarMenuGroups(userId: string, state: SidebarMenuGroupsState): void {
  const uid = userId.trim()
  if (!uid) return
  try {
    localStorage.setItem(sidebarMenuGroupsStorageKey(uid), JSON.stringify(state))
  } catch {
    /* ignore quota / private mode */
  }
}
