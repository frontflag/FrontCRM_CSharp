import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

/**
 * PRD《RBAC权限系统》§5.2.1：采购方向 + 主部门销售数据「禁止」（`saleDataScope === 4`）时，
 * 在 UI 上对<strong>客户身份与销售侧金额</strong>以占位符展示（出库/拣货/装箱/库存/客户/销售订单等）。
 *
 * 条件（与后端 `SaleSensitiveFieldMask521.ShouldMask` 一致）：
 * - `saleDataScope === 4`、非系统管理员；
 * - 且（`belongsToPurchaseDept === true`，或 `identityType` 为 2 / 3 采购与采购运营）。
 */
export function useSaleSensitiveFieldMask() {
  const authStore = useAuthStore()

  const maskSaleSensitiveFields = computed(() => {
    const u = authStore.user
    if (!u || u.isSysAdmin === true) return false
    if (Number(u.saleDataScope) !== 4) return false
    if (u.belongsToPurchaseDept === true) return true
    const it = Number(u.identityType)
    return it === 2 || it === 3
  })

  return { maskSaleSensitiveFields }
}
