/** 商务部职员（主部门 IdentityType=4 或 commerce_operator 角色） */
export function isBusinessCommerceStaff(user: {
  identityType?: number
  roleCodes?: string[]
} | null | undefined): boolean {
  if (!user) return false
  if (user.identityType === 4) return true
  return (user.roleCodes ?? []).some((r) => String(r).trim().toLowerCase() === 'commerce_operator')
}

/**
 * 销售助理新建/编辑销售订单：销售助理只读为自己，销售员来自 sys_relation_map(type=100)。
 * 与「用户配置」商务部员工范围一致；不限于 saleDataScope=4（该值为订单数据可见范围）。
 */
export function isSellOrderAssistorLockedMode(
  user: {
    isSysAdmin?: boolean
    identityType?: number
    roleCodes?: string[]
  } | null | undefined
): boolean {
  if (!user || user.isSysAdmin) return false
  if (canPickSalesOrderStaffFreely(user)) return false
  return isBusinessCommerceStaff(user)
}

/** 系统管理员、销售部总监、商务部总监：销售助理/销售员均可全量下拉 */
export function canPickSalesOrderStaffFreely(user: {
  isSysAdmin?: boolean
  identityType?: number
  roleCodes?: string[]
} | null | undefined): boolean {
  if (!user) return false
  if (user.isSysAdmin) return true
  const isDirector = (user.roleCodes ?? []).some((r) => String(r).trim().toUpperCase() === 'DEPT_DIRECTOR')
  if (!isDirector) return false
  const it = user.identityType ?? 0
  return it === 1 || it === 4
}
