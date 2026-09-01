type RfqItemReferenceUser = {
  isSysAdmin?: boolean
  isSysManager?: boolean
  isBizManager?: boolean
  hasBizDataBypass?: boolean
  identityType?: number
  belongsToPurchaseDept?: boolean
}

/** 与后端 <c>RfqItemReferenceAccessRules.CanEnterPage</c> 一致（另需 rfq.read）。 */
export function canAccessRfqItemReference(user: RfqItemReferenceUser | null | undefined): boolean {
  if (!user) return false
  if (user.hasBizDataBypass || user.isSysAdmin || user.isSysManager || user.isBizManager) return true
  const t = Number(user.identityType ?? 0)
  if (t === 1 || t === 2 || t === 3) return true
  return user.belongsToPurchaseDept === true
}
