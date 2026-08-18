type RecycleUser = {
  isSysAdmin?: boolean
  isSysManager?: boolean
  isBizManager?: boolean
  identityType?: number
  roleCodes?: string[]
} | null | undefined

/** 与后端 RfqRecycleBinAccessRules 一致：管理员/业务经理，或销售部门总监。 */
export function canAccessRfqRecycleBin(user: RecycleUser): boolean {
  if (!user) return false
  if (user.isSysAdmin || user.isSysManager || user.isBizManager) return true
  if (Number(user.identityType ?? 0) !== 1) return false
  return (user.roleCodes ?? []).some((c) => String(c).trim().toUpperCase() === 'DEPT_DIRECTOR')
}
