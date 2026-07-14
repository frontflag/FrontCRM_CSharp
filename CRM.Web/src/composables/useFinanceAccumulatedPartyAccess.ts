import { useAuthStore } from '@/stores/auth'

export function useFinanceAccumulatedPartyAccess() {
  const authStore = useAuthStore()

  function canOpenVendorDetail(vendorId?: string | null): boolean {
    return Boolean(
      vendorId?.trim() &&
        authStore.hasPermission('vendor.read') &&
        !authStore.isIdentityBlockedForPermission('vendor.read') &&
        !authStore.isVendorManagementHidden()
    )
  }

  function canOpenCustomerDetail(customerId?: string | null): boolean {
    return Boolean(
      customerId?.trim() &&
        authStore.hasPermission('customer.read') &&
        !authStore.isIdentityBlockedForPermission('customer.read') &&
        !authStore.isCustomerManagementHidden()
    )
  }

  function canOpenStockInDetail(row: { stockInId?: string | null; billCode?: string | null }): boolean {
    return Boolean(
      row.stockInId?.trim() &&
        row.billCode?.trim() &&
        authStore.hasPermission('inventory.read') &&
        !authStore.isIdentityBlockedForPermission('inventory.read')
    )
  }

  return { canOpenVendorDetail, canOpenCustomerDetail, canOpenStockInDetail }
}
