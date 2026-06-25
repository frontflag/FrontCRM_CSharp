/** 付款记录「收款银行」扩展列：账户名称 | 开户银行 */
export const LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_MIN_WIDTH = 176
export const LIST_VENDOR_RECEIVING_BANK_EXTEND_COL_COLLAPSED_WIDTH = 176

export const VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_DEFAULT_WIDTHS: [number, number] = [140, 180]
export const VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_MIN_WIDTH = 56
export const VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_GAP_PX = 8
export const VENDOR_RECEIVING_BANK_EXTEND_TOGGLE_RESERVE_PX = 32
export const VENDOR_RECEIVING_BANK_EXTEND_COL_PADDING_PX = 16

export const VENDOR_RECEIVING_BANK_EXTEND_COL_STORAGE_KEY =
  'crm-table-extend-col:v2:global:vendorReceivingBank'

export type VendorReceivingBankExtendFieldKey = 'accountName' | 'openingBank'

export const VENDOR_RECEIVING_BANK_EXTEND_FIELD_KEYS: VendorReceivingBankExtendFieldKey[] = [
  'accountName',
  'openingBank'
]

export interface VendorReceivingBankExtendRowSlice {
  vendorBankAccountName?: string | null
  vendorBankOpeningBank?: string | null
  vendorBankName?: string | null
}

export function sumVendorReceivingBankExtendSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedVendorReceivingBankExtendOuterWidth(widths: readonly number[]): number {
  const gaps = VENDOR_RECEIVING_BANK_EXTEND_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumVendorReceivingBankExtendSubColWidths(widths) +
    gaps +
    VENDOR_RECEIVING_BANK_EXTEND_TOGGLE_RESERVE_PX +
    VENDOR_RECEIVING_BANK_EXTEND_COL_PADDING_PX
  )
}

export function subColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}
