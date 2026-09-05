export type CommerceAssistantFinanceMenuUser = {
  isSysAdmin?: boolean
  hasBizDataBypass?: boolean
  identityType?: number
}

/** 商务部商务助理：主部门 FinanceDataScope=4 时仍显示收款管理菜单 */
export function canAccessCommerceAssistantReceiptMenus(
  user: CommerceAssistantFinanceMenuUser | null | undefined
): boolean {
  if (!user) return false
  if (user.isSysAdmin || user.hasBizDataBypass) return false
  return Number(user.identityType ?? 0) === 4
}
