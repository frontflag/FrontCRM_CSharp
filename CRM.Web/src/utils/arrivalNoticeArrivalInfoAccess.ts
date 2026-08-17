type ArrivalInfoAccessUser = {
  isSysAdmin?: boolean
  isSysManager?: boolean
  isBizManager?: boolean
  identityType?: number
  roleCodes?: string[]
  logisticsDataAccess?: number
}

const LOGISTICS_IDENTITY_TYPE = 6

export function isArrivalInfoManagementEditor(
  user: Pick<ArrivalInfoAccessUser, 'isSysAdmin' | 'isSysManager' | 'isBizManager'> | null | undefined
): boolean {
  if (!user) return false
  return user.isSysAdmin === true || user.isSysManager === true || user.isBizManager === true
}

/** 主部门身份为物流，且组织角色为部门总监或部门经理。 */
export function isLogisticsDepartmentLead(
  user: Pick<ArrivalInfoAccessUser, 'identityType' | 'roleCodes'> | null | undefined
): boolean {
  if (!user) return false
  if (Number(user.identityType ?? 0) !== LOGISTICS_IDENTITY_TYPE) return false
  return (user.roleCodes ?? []).some((r) => {
    const c = String(r).trim().toUpperCase()
    return c === 'DEPT_DIRECTOR' || c === 'DEPT_MANAGER'
  })
}

/** 与后端 ArrivalNoticeArrivalInfoAccessRules.CanEdit 一致。 */
export function canEditArrivalNoticeArrivalInfo(user: ArrivalInfoAccessUser | null | undefined): boolean {
  if (!user) return false
  if (isArrivalInfoManagementEditor(user)) return true
  if (!isLogisticsDepartmentLead(user)) return false
  return Number(user.logisticsDataAccess ?? 0) !== 1
}
