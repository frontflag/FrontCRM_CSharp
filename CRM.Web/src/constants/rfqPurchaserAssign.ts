/**
 * 需求「手动分配采购员」入口权限：系统管理员，或主职为采购运营部且担任部门总监（DEPT_DIRECTOR）。
 * identityType 与库中部门业务身份一致（permission-summary 主职维度）；3 = 采购运营。
 */
export const IDENTITY_PURCHASE_OPS = 3

export const ROLE_DEPT_DIRECTOR = 'DEPT_DIRECTOR'

export function canManualAssignRfqPurchaser(
  user:
    | {
        isSysAdmin?: boolean
        identityType?: number
        roleCodes?: string[]
      }
    | null
    | undefined
): boolean {
  if (!user) return false
  if (user.isSysAdmin === true) return true
  if (Number(user.identityType) !== IDENTITY_PURCHASE_OPS) return false
  const codes = user.roleCodes ?? []
  return codes.some((c) => String(c).toUpperCase() === ROLE_DEPT_DIRECTOR)
}
