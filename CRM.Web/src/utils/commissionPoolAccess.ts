/** 与后端 <c>CommissionPoolAccessRules</c> 一致。 */
export type CommissionPoolAccessUser = {
  isSysAdmin?: boolean
  isSysManager?: boolean
  hasBizDataBypass?: boolean
  identityType?: number
  belongsToPurchaseDept?: boolean
}

export function canSeeAllCommissionPoolRows(
  user: CommissionPoolAccessUser | null | undefined
): boolean {
  if (!user) return false
  return user.isSysAdmin === true || user.isSysManager === true || user.hasBizDataBypass === true
}

export function isCommissionPoolSalesperson(
  user: CommissionPoolAccessUser | null | undefined
): boolean {
  return Number(user?.identityType ?? 0) === 1
}

export function isCommissionPoolPurchaser(
  user: CommissionPoolAccessUser | null | undefined
): boolean {
  if (!user) return false
  const t = Number(user.identityType ?? 0)
  return t === 2 || t === 3 || user.belongsToPurchaseDept === true
}

export function canAccessCommissionPool(
  user: CommissionPoolAccessUser | null | undefined
): boolean {
  if (!user) return false
  if (canSeeAllCommissionPoolRows(user)) return true
  return isCommissionPoolSalesperson(user) || isCommissionPoolPurchaser(user)
}
