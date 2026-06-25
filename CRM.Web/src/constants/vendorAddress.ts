/** 供应商地址：国家/级联规则与客户地址一致（中国 + 省市区；港澳台走中国 + 级联） */
import {
  CUSTOMER_ADDRESS_COUNTRY_CHINA,
  CUSTOMER_ADDRESS_COUNTRY_OTHER,
  CUSTOMER_ADDRESS_OVERSEAS_PRESETS,
  customerAddressCountryToSelect,
  normalizeAddressChinaCascaderCountry,
  resolveCustomerAddressCountryName,
  usesChinaRegionCascader
} from '@/constants/customerAddress'

export const VENDOR_ADDRESS_COUNTRY_CHINA = CUSTOMER_ADDRESS_COUNTRY_CHINA
export const VENDOR_ADDRESS_COUNTRY_OTHER = CUSTOMER_ADDRESS_COUNTRY_OTHER
export const VENDOR_ADDRESS_OVERSEAS_PRESETS = CUSTOMER_ADDRESS_OVERSEAS_PRESETS
export const vendorAddressCountryToSelect = customerAddressCountryToSelect
export const resolveVendorAddressCountryName = resolveCustomerAddressCountryName
export { normalizeAddressChinaCascaderCountry, usesChinaRegionCascader }

/** 供应商地址 Country 字段：1=国内（含港澳台级联） 2=海外 */
export const VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE = 1
export const VENDOR_ADDRESS_COUNTRY_OVERSEAS_CODE = 2

export function vendorAddressCountryCode(countryName: string, province: string): number {
  return usesChinaRegionCascader(countryName, province)
    ? VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE
    : VENDOR_ADDRESS_COUNTRY_OVERSEAS_CODE
}

/** 1=收货 2=账单 */
export function normalizeVendorAddressType(v: unknown): number {
  const raw = String(v ?? '').trim()
  if (!raw) return 1
  const n = Number(raw)
  if (n === 1 || n === 2) return n
  const lower = raw.toLowerCase()
  if (lower === 'billing' || raw.includes('账单')) return 2
  if (lower === 'shipping' || raw.includes('收货') || raw.includes('送货')) return 1
  return 1
}
