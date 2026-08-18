type OpsCheckUser = {
  isSysAdmin?: boolean
  isSysManager?: boolean
  identityType?: number
  roleCodes?: string[]
} | null | undefined

/** 与后端 InventoryOpsCheckAccessRules 一致：管理员，或财务部门总监。 */
export function canAccessInventoryOpsCheck(user: OpsCheckUser): boolean {
  if (!user) return false
  if (user.isSysAdmin || user.isSysManager) return true
  if (Number(user.identityType ?? 0) !== 5) return false
  return (user.roleCodes ?? []).some((c) => String(c).trim().toUpperCase() === 'DEPT_DIRECTOR')
}
