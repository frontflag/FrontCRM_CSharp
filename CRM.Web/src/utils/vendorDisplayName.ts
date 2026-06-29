/** 只读场景供应商名称：中文 / 英文 */
export interface FormatVendorNameReadonlyOptions {
  separator?: string
  empty?: string
  masked?: boolean
}

export interface VendorNameReadonlySlice {
  vendorName?: string | null
  vendorEnglishName?: string | null
}

export function formatVendorNameReadonly(
  zh?: string | null,
  en?: string | null,
  options?: FormatVendorNameReadonlyOptions
): string {
  if (options?.masked) return options.empty ?? '—'

  const separator = options?.separator ?? ' / '
  const empty = options?.empty ?? '—'
  const nameZh = String(zh ?? '').trim()
  const nameEn = String(en ?? '').trim()

  if (nameZh && nameEn) return `${nameZh}${separator}${nameEn}`
  if (nameZh) return nameZh
  if (nameEn) return nameEn
  return empty
}

export function formatVendorNameReadonlyFromRow(
  row: VendorNameReadonlySlice | null | undefined,
  options?: FormatVendorNameReadonlyOptions
): string {
  return formatVendorNameReadonly(row?.vendorName, row?.vendorEnglishName, options)
}
