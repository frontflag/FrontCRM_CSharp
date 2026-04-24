import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

function hasSalesOrderPermission(permissionCodes: string[] | undefined): boolean {
  if (!permissionCodes?.length) return false
  return permissionCodes.some(c => {
    const x = String(c).trim().toLowerCase()
    return x === 'sales-order.read' || x === 'sales-order.write'
  })
}

/**
 * PRD《RBAC权限系统》§5.1.1：销售方向 + 主部门采购数据「禁止」时，
 * 对<strong>采购执行结果</strong>敏感列在 UI 上以占位符替代：入库/库存/采购单/进项/付款等链路中的<strong>采购价</strong>、
 * <strong>供应商标识</strong>（主数据、编码、到货/质检对方、付款对方等）。
 *
 * RFQ 报价：销售员可看<strong>询价单价</strong>；报价行上的<strong>供应商名称/编码/联系人</strong>仍应脱敏（与后端 `ApplyQuoteVendorIdentityOnly` 一致）。
 *
 * 条件概要：
 * - `purchaseDataScope === 4`、非系统管理员；
 * - 非采购侧部门（`belongsToPurchaseDept` 为真时不脱敏，避免采购兼岗被误伤）；
 * - **销售方向**：`identityType === 1`，或与后端 `RbacService` 一致——**主部门 `IdentityType` 未维护为 1 但已合并销售订单权限**（`identityType === 0` 且具备 `sales-order.read/write`）的业务员，仍视为销售侧需脱敏；
 * - PRD 写明客服/物流/财务等**默认不套用**本表：`identityType` 为 4 / 5 / 6 时不自动脱敏。
 */
export function usePurchaseSensitiveFieldMask() {
  const authStore = useAuthStore()

  const maskPurchaseSensitiveFields = computed(() => {
    const u = authStore.user
    if (!u || u.isSysAdmin === true) return false
    if (Number(u.purchaseDataScope) !== 4) return false
    if (u.belongsToPurchaseDept === true) return false

    const it = Number(u.identityType)
    if (it === 4 || it === 5 || it === 6) return false
    if (it === 2 || it === 3) return false
    if (it === 1) return true
    if (it === 0 && hasSalesOrderPermission(u.permissionCodes)) return true
    return false
  })

  return { maskPurchaseSensitiveFields }
}
