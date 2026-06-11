/** 《列表扩展列规范 PRD》：供应商列收起/展开宽度参考值 */
export const LIST_VENDOR_EXTEND_COL_COLLAPSED_MIN_WIDTH = 160
export const LIST_VENDOR_EXTEND_COL_COLLAPSED_WIDTH = 160

/** 展开态内部三列默认宽度：中文 | 英文 | 编号 */
export const VENDOR_EXTEND_SUB_COL_DEFAULT_WIDTHS: [number, number, number] = [180, 180, 100]
export const VENDOR_EXTEND_SUB_COL_MIN_WIDTH = 56
export const VENDOR_EXTEND_SUB_COL_GAP_PX = 8
/** 列头左侧展开/收起按钮占位 */
export const VENDOR_EXTEND_TOGGLE_RESERVE_PX = 32
/** 列头/单元格左右内边距余量 */
export const VENDOR_EXTEND_COL_PADDING_PX = 16

export const VENDOR_EXTEND_COL_STORAGE_KEY = 'crm-table-extend-col:v1:global:vendor'

export type VendorExtendFieldKey = 'nameZh' | 'nameEn' | 'code'

export const VENDOR_EXTEND_FIELD_KEYS: VendorExtendFieldKey[] = ['nameZh', 'nameEn', 'code']

export interface VendorExtendRowSlice {
  vendorName?: string | null
  vendorEnglishName?: string | null
  vendorCode?: string | null
}

export function sumVendorExtendSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedVendorExtendOuterWidth(widths: readonly number[]): number {
  const gaps = VENDOR_EXTEND_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumVendorExtendSubColWidths(widths) +
    gaps +
    VENDOR_EXTEND_TOGGLE_RESERVE_PX +
    VENDOR_EXTEND_COL_PADDING_PX
  )
}

export function subColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}
