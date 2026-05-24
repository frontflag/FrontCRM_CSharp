import type { CustomerAddress } from '@/types/customer'

export function formatCustomerAddressLine(addr: CustomerAddress): string {
  return [addr.province, addr.city, addr.district, addr.streetAddress]
    .map((x) => x?.trim())
    .filter(Boolean)
    .join('')
}

/** 取该类型的第一条地址（与客户详情列表顺序一致）。 */
export function firstCustomerAddressByType(
  addresses: CustomerAddress[],
  type: 'Shipping' | 'Billing'
): CustomerAddress | undefined {
  return addresses.find((a) => String(a.addressType) === type)
}

export type PackingAddressFields = {
  company: string
  address: string
  attn: string
  tel: string
}

export function mapCustomerAddressToPackingFields(
  addr: CustomerAddress | undefined,
  companyName: string
): PackingAddressFields {
  const company = companyName.trim()
  if (!addr) {
    return { company, address: '', attn: '', tel: '' }
  }
  return {
    company,
    address: formatCustomerAddressLine(addr),
    attn: addr.contactPerson?.trim() ?? '',
    tel: addr.contactPhone?.trim() ?? ''
  }
}
