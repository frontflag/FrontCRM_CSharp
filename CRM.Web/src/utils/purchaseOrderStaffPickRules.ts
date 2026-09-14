/** 采购运营部职员（主部门 IdentityType=3 或 purchase_ops_operator 角色） */
export function isPurchasingOperationsStaff(user: {
  identityType?: number
  roleCodes?: string[]
} | null | undefined): boolean {
  if (!user) return false
  if (user.identityType === 3) return true
  return (user.roleCodes ?? []).some((r) => String(r).trim().toLowerCase() === 'purchase_ops_operator')
}

/**
 * 采购助理新建/编辑采购订单：采购助理只读为自己，采购员来自 sys_relation_map(type=101)。
 */
export function isPurchaseOrderAssistorLockedMode(
  user: {
    isSysAdmin?: boolean
    identityType?: number
    roleCodes?: string[]
  } | null | undefined
): boolean {
  if (!user || user.isSysAdmin) return false
  if (canPickPurchaseOrderStaffFreely(user)) return false
  return isPurchasingOperationsStaff(user)
}

/** 系统管理员、采购部总监、采购运营部总监：采购助理/采购员均可全量下拉 */
export function canPickPurchaseOrderStaffFreely(user: {
  isSysAdmin?: boolean
  identityType?: number
  roleCodes?: string[]
} | null | undefined): boolean {
  if (!user) return false
  if (user.isSysAdmin) return true
  const isDirector = (user.roleCodes ?? []).some((r) => String(r).trim().toUpperCase() === 'DEPT_DIRECTOR')
  if (!isDirector) return false
  const it = user.identityType ?? 0
  return it === 2 || it === 3
}

export type PurchaseOrderVendorChangeUser = {
  isSysAdmin?: boolean
  isSysManager?: boolean
  identityType?: number
  roleCodes?: string[]
  permissionCodes?: string[]
  hasPermission?: (code: string) => boolean
}

function hasRole(user: PurchaseOrderVendorChangeUser, code: string): boolean {
  return (user.roleCodes ?? []).some((r) => String(r).trim().toUpperCase() === code)
}

function hasExplicitPermission(user: PurchaseOrderVendorChangeUser, code: string): boolean {
  const want = code.trim().toLowerCase()
  return (user.permissionCodes ?? []).some((c) => String(c).trim().toLowerCase() === want)
}

/** 采购订单更换供应商 / 联系人：SYS_ADMIN、SYS_MANAGER、采购侧总监，或显式 purchase-order.change-vendor（不含 Manager bypass） */
export function canChangePurchaseOrderVendor(
  user: PurchaseOrderVendorChangeUser | null | undefined
): boolean {
  if (!user) return false
  if (user.isSysAdmin || user.isSysManager) return true
  if (hasRole(user, 'SYS_ADMIN') || hasRole(user, 'SYS_MANAGER')) return true
  if (hasExplicitPermission(user, 'purchase-order.change-vendor')) return true
  return canPickPurchaseOrderStaffFreely(user)
}

/** 新建 / 待审核 / 审核失败：采购员可换供应商 */
export function isPurchaseOrderPreAuditVendorChangeStatus(
  status: number | null | undefined
): boolean {
  const s = Number(status)
  return s === 1 || s === 2 || s === -1
}

/**
 * 按订单主状态：审核前采购员凭 write 可换；审核通过后仍须总监 / change-vendor。
 * 脱敏身份由调用方另行拦截。
 */
export function canChangePurchaseOrderVendorOnOrder(
  user: PurchaseOrderVendorChangeUser | null | undefined,
  orderStatus: number | null | undefined
): boolean {
  if (!user) return false
  if (canChangePurchaseOrderVendor(user)) return true
  if (!isPurchaseOrderPreAuditVendorChangeStatus(orderStatus)) return false
  return (
    user.isSysAdmin === true ||
    user.isSysManager === true ||
    hasRole(user, 'SYS_ADMIN') ||
    hasRole(user, 'SYS_MANAGER') ||
    hasExplicitPermission(user, 'purchase-order.write') ||
    user.hasPermission?.('purchase-order.write') === true
  )
}
