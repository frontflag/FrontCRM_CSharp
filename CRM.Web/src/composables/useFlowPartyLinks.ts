import { useFinanceAccumulatedPartyAccess } from '@/composables/useFinanceAccumulatedPartyAccess'

export function useFlowPartyLinks() {
  const { canOpenCustomerDetail, canOpenVendorDetail } = useFinanceAccumulatedPartyAccess()

  function customerTo(customerId?: string | null, masked = false) {
    if (masked || !canOpenCustomerDetail(customerId)) return undefined
    return { name: 'CustomerDetail', params: { id: String(customerId ?? '').trim() } }
  }

  function vendorTo(vendorId?: string | null, masked = false) {
    if (masked || !canOpenVendorDetail(vendorId)) return undefined
    return { name: 'VendorDetail', params: { id: String(vendorId ?? '').trim() } }
  }

  return { customerTo, vendorTo }
}
