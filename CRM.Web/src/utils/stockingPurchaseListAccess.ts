export const STOCKING_PURCHASE_ROLE_CODES = [
  'purchase_buyer',
  'purchase_operator',
  'purchase_ops_operator'
] as const

export type StockingPurchaseListAccessUser = {
  isSysAdmin?: boolean
  hasBizDataBypass?: boolean
  belongsToPurchaseDept?: boolean
  identityType?: number
  purchaseDataScope?: number
  roleCodes?: string[]
}

function hasPurchasingRole(roleCodes?: string[]): boolean {
  if (!roleCodes?.length) return false
  return roleCodes.some((r) =>
    STOCKING_PURCHASE_ROLE_CODES.some((p) => String(r).trim().toLowerCase() === p)
  )
}

/** 备货采购清单菜单/路由：采购部职员、采购助理、采购运营 + purchase-order.read */
export function canAccessStockingPurchaseList(
  user: StockingPurchaseListAccessUser | null | undefined,
  hasPurchaseOrderRead: boolean
): boolean {
  if (!user) return false
  const scope = Number(user.purchaseDataScope ?? 1)
  if (scope === 4 && !user.isSysAdmin && !user.hasBizDataBypass) return false
  if (user.isSysAdmin || user.hasBizDataBypass) return true
  if (!hasPurchaseOrderRead) return false
  if (user.belongsToPurchaseDept) return true
  const identity = Number(user.identityType ?? 0)
  if (identity === 2 || identity === 3) return true
  return hasPurchasingRole(user.roleCodes)
}
