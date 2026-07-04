/** 只读场景客户名称：中文 / 英文 */
export interface FormatCustomerNameReadonlyOptions {
  separator?: string
  empty?: string
  masked?: boolean
}

export interface CustomerNameReadonlySlice {
  customerName?: string | null
  customerEnglishName?: string | null
}

export function formatCustomerNameReadonly(
  zh?: string | null,
  en?: string | null,
  options?: FormatCustomerNameReadonlyOptions
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

export function formatCustomerNameReadonlyFromRow(
  row: CustomerNameReadonlySlice | null | undefined,
  options?: FormatCustomerNameReadonlyOptions
): string {
  return formatCustomerNameReadonly(row?.customerName, row?.customerEnglishName, options)
}
