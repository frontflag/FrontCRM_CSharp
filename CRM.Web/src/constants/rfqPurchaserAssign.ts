/**
 * 需求详情「手动分配采购员」入口：
 * SuperAdmin / Admin / Manager（HasBizDataBypass），或主职为采购部/采购运营且部门总监。
 */
export const IDENTITY_PURCHASE = 2
export const IDENTITY_PURCHASE_OPS = 3

export const ROLE_DEPT_DIRECTOR = 'DEPT_DIRECTOR'

export function canManualAssignRfqPurchaser(
  user:
    | {
        isSysAdmin?: boolean
        isSysManager?: boolean
        isBizManager?: boolean
        hasBizDataBypass?: boolean
        identityType?: number
        roleCodes?: string[]
      }
    | null
    | undefined
): boolean {
  if (!user) return false
  if (
    user.isSysAdmin === true ||
    user.isSysManager === true ||
    user.isBizManager === true ||
    user.hasBizDataBypass === true
  ) {
    return true
  }
  const identity = Number(user.identityType)
  if (identity !== IDENTITY_PURCHASE && identity !== IDENTITY_PURCHASE_OPS) return false
  const codes = user.roleCodes ?? []
  return codes.some((c) => String(c).toUpperCase() === ROLE_DEPT_DIRECTOR)
}
