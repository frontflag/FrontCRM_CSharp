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
