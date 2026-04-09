/** 与后端 PurchaseOrderCreateGate / 菜单策略一致 */

export type PermissionChecker = (code: string) => boolean

/** 与 API PurchasingRoleCodes 一致：采购业务角色，用于 PR 写建 PO 时的兜底识别 */
const PURCHASING_ROLE_CODES = ['purchase_buyer', 'purchase_operator', 'purchase_ops_operator'] as const

export function hasPurchasingRole(roleCodes?: string[] | null): boolean {
  if (!roleCodes?.length) return false
  const lowered = new Set(roleCodes.map((r) => String(r).trim().toLowerCase()).filter(Boolean))
  return PURCHASING_ROLE_CODES.some((c) => lowered.has(c))
}

export function canGeneratePurchaseOrderFromRequisition(params: {
  isSysAdmin?: boolean
  identityType?: number
  roleCodes?: string[] | null
  hasPermission: PermissionChecker
}): boolean {
  if (params.isSysAdmin) return true
  const t = params.identityType ?? 0
  const buyerDept = t === 2 || t === 3
  if (params.hasPermission('purchase-order.write')) return true
  if (!params.hasPermission('purchase-requisition.write')) return false
  if (buyerDept) return true
  if (params.hasPermission('purchase-order.read')) return true
  return hasPurchasingRole(params.roleCodes)
}

/** 与后端 PurchaseOrderCreateGate.CanCreate 一致（POST 创建采购订单） */
export const canSubmitPurchaseOrderCreate = canGeneratePurchaseOrderFromRequisition

/** 进入新建采购订单页：采购员仅有读权限时也可打开（从申请生成），销售员不可 */
export function canAccessPurchaseOrderCreatePage(params: {
  isSysAdmin?: boolean
  identityType?: number
  roleCodes?: string[] | null
  hasPermission: PermissionChecker
}): boolean {
  if (params.isSysAdmin) return true
  const t = params.identityType ?? 0
  const buyer = t === 2 || t === 3
  if (params.hasPermission('purchase-order.write')) return true
  if (buyer && params.hasPermission('purchase-requisition.write')) return true
  if (buyer && (params.hasPermission('purchase-order.read') || params.hasPermission('purchase-requisition.read')))
    return true
  if (
    params.hasPermission('purchase-requisition.write') &&
    params.hasPermission('purchase-order.read')
  )
    return true
  if (params.hasPermission('purchase-requisition.write') && hasPurchasingRole(params.roleCodes)) return true
  return false
}
