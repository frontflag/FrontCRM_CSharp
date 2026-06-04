import type { RbacDepartment } from '@/api/rbacAdmin'

/** 与后端 BusinessDepartmentRules.IsBusinessDepartment 一致 */
export function isBusinessDepartment(d: RbacDepartment): boolean {
  if (d.status !== 1) return false
  if (d.identityType === 4) return true
  const n = d.departmentName ?? ''
  return n.includes('商务') || /business/i.test(n)
}

/** 与后端 PurchasingDepartmentRules.IsPurchasingOperationsDepartment 一致 */
export function isPurchasingOperationsDepartment(d: RbacDepartment): boolean {
  const n = d.departmentName ?? ''
  if (!n) return false
  return (
    n.includes('采购运营') ||
    /purchasing operations/i.test(n) ||
    /purchase operations/i.test(n) ||
    /procurement operations/i.test(n)
  )
}

/** 与后端 SalesDepartmentRules.IsSalesDepartment 一致 */
export function isSalesDepartment(d: RbacDepartment): boolean {
  if (d.identityType === 1) return true
  const n = d.departmentName ?? ''
  return n.includes('销售') || /sales/i.test(n)
}

/** 与后端 PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer 一致（采购部，不含采购运营） */
export function isPurchaseDepartment(d: RbacDepartment): boolean {
  if (d.status !== 1) return false
  if (isPurchasingOperationsDepartment(d)) return false
  if (d.identityType === 2 || d.identityType === 3) return true
  const n = d.departmentName ?? ''
  return n.includes('采购') || /purchase/i.test(n)
}
