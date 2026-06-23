/** 与后端 RfqItemQuoteAccessRules、purchaseOrderStaffPickRules 采购总监判定一致 */
export function isPurchaseDepartmentDirector(
  user: {
    identityType?: number
    roleCodes?: string[]
  } | null | undefined
): boolean {
  if (!user) return false
  const isDirector = (user.roleCodes ?? []).some(
    (r) => String(r).trim().toUpperCase() === 'DEPT_DIRECTOR'
  )
  if (!isDirector) return false
  const it = user.identityType ?? 0
  return it === 2 || it === 3
}

export type RfqItemQuoteAssignee = {
  assignedPurchaserUserId1?: string | null
  assignedPurchaserUserId2?: string | null
}

export function isAssignedRfqItemQuoter(
  userId: string | null | undefined,
  row: RfqItemQuoteAssignee | null | undefined
): boolean {
  if (!userId || !row) return false
  const uid = userId.trim()
  const id1 = String(row.assignedPurchaserUserId1 ?? '').trim()
  const id2 = String(row.assignedPurchaserUserId2 ?? '').trim()
  return (id1 !== '' && id1 === uid) || (id2 !== '' && id2 === uid)
}

/** 需求明细列表「报价」：系统管理员、采购总监、或该行分配的报价员。 */
export function canQuoteRfqItem(
  user: {
    id?: string
    isSysAdmin?: boolean
    identityType?: number
    roleCodes?: string[]
  } | null | undefined,
  row: RfqItemQuoteAssignee | null | undefined
): boolean {
  if (!user || !row) return false
  if (user.isSysAdmin === true) return true
  if (isPurchaseDepartmentDirector(user)) return true
  return isAssignedRfqItemQuoter(user.id, row)
}
