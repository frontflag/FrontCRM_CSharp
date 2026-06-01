import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

/**
 * 主部门数据访问模式（部门编辑页 Sale/Purchase/Logistics DataAccess）：
 * 0=读写，1=只读。只读时列表操作列应隐藏编辑/删除等写操作（API 仍会拦截）。
 */
export function useDepartmentDataReadOnly() {
  const auth = useAuthStore()

  const saleDataReadOnly = computed(() => auth.isSaleDataReadOnly())
  const purchaseDataReadOnly = computed(() => auth.isPurchaseDataReadOnly())
  const logisticsDataReadOnly = computed(() => auth.isLogisticsDataReadOnly())
  const financeDataReadOnly = computed(() => auth.isFinanceDataReadOnly())

  const canWriteSaleData = computed(() => !saleDataReadOnly.value)
  const canWritePurchaseData = computed(() => !purchaseDataReadOnly.value)
  const canWriteLogisticsData = computed(() => !logisticsDataReadOnly.value)
  const canWriteFinanceData = computed(() => !financeDataReadOnly.value)

  return {
    saleDataReadOnly,
    purchaseDataReadOnly,
    logisticsDataReadOnly,
    financeDataReadOnly,
    canWriteSaleData,
    canWritePurchaseData,
    canWriteLogisticsData,
    canWriteFinanceData
  }
}

/** 销售订单写权限 = RBAC sales-order.write 且主部门销售数据非只读 */
export function useSaleOrderWriteGate() {
  const auth = useAuthStore()
  const { canWriteSaleData } = useDepartmentDataReadOnly()
  const canWriteSo = computed(
    () => auth.hasPermission('sales-order.write') && canWriteSaleData.value
  )
  return { canWriteSaleData, canWriteSo }
}

/** 财务写权限：主部门财务数据非只读且非禁止（具体 finance-* 写码仍由 RBAC 控制） */
export function useFinanceWriteGate() {
  const auth = useAuthStore()
  const { canWriteFinanceData } = useDepartmentDataReadOnly()
  const canWriteFinancePayment = computed(
    () =>
      auth.hasPermission('finance-payment.write') &&
      !auth.isIdentityBlockedForPermission('finance-payment.write') &&
      canWriteFinanceData.value
  )
  const canWriteFinanceReceipt = computed(
    () =>
      auth.hasPermission('finance-receipt.write') &&
      !auth.isIdentityBlockedForPermission('finance-receipt.write') &&
      canWriteFinanceData.value
  )
  const canWriteFinancePurchaseInvoice = computed(
    () =>
      auth.hasPermission('finance-purchase-invoice.write') &&
      !auth.isIdentityBlockedForPermission('finance-purchase-invoice.write') &&
      canWriteFinanceData.value
  )
  const canWriteFinanceSellInvoice = computed(
    () =>
      auth.hasPermission('finance-sell-invoice.write') &&
      !auth.isIdentityBlockedForPermission('finance-sell-invoice.write') &&
      canWriteFinanceData.value
  )
  return {
    canWriteFinanceData,
    canWriteFinancePayment,
    canWriteFinanceReceipt,
    canWriteFinancePurchaseInvoice,
    canWriteFinanceSellInvoice
  }
}

/** 采购订单写权限 = RBAC purchase-order.write 且主部门采购数据非只读 */
export function usePurchaseOrderWriteGate() {
  const auth = useAuthStore()
  const { canWritePurchaseData } = useDepartmentDataReadOnly()
  const canWritePo = computed(
    () => auth.hasPermission('purchase-order.write') && canWritePurchaseData.value
  )
  return { canWritePurchaseData, canWritePo }
}
