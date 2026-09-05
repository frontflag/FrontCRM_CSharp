const PURCHASE_OPS_OPERATOR = 'purchase_ops_operator'

export type PurchaseOpsFinanceMenuUser = {
  isSysAdmin?: boolean
  hasBizDataBypass?: boolean
  purchaseDataScope?: number
  roleCodes?: string[]
}

/** 采购运营职员：主部门 FinanceDataScope=4 时仍显示付款管理/进项发票菜单 */
export function canAccessPurchaseOpsPaymentMenus(
  user: PurchaseOpsFinanceMenuUser | null | undefined
): boolean {
  if (!user) return false
  const scope = Number(user.purchaseDataScope ?? 1)
  if (scope === 4 && !user.isSysAdmin && !user.hasBizDataBypass) return false
  if (user.isSysAdmin || user.hasBizDataBypass) return true
  return (user.roleCodes ?? []).some(
    (r) => String(r).trim().toLowerCase() === PURCHASE_OPS_OPERATOR
  )
}
