/** 《列表扩展列规范 PRD》：客户列收起/展开宽度参考值 */
export const LIST_CUSTOMER_EXTEND_COL_COLLAPSED_MIN_WIDTH = 160
export const LIST_CUSTOMER_EXTEND_COL_COLLAPSED_WIDTH = 160

/** 展开态内部三列默认宽度：中文 | 英文 | 编号 */
export const CUSTOMER_EXTEND_SUB_COL_DEFAULT_WIDTHS: [number, number, number] = [180, 180, 100]
export const CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH = 56
export const CUSTOMER_EXTEND_SUB_COL_GAP_PX = 8
/** 列头右侧展开/收起按钮占位 */
export const CUSTOMER_EXTEND_TOGGLE_RESERVE_PX = 32
/** 列头/单元格左右内边距余量 */
export const CUSTOMER_EXTEND_COL_PADDING_PX = 16

export const CUSTOMER_EXTEND_COL_STORAGE_KEY = 'crm-table-extend-col:v1:global:customer'

export type CustomerExtendFieldKey = 'nameZh' | 'nameEn' | 'code'

export const CUSTOMER_EXTEND_FIELD_KEYS: CustomerExtendFieldKey[] = ['nameZh', 'nameEn', 'code']

export interface CustomerExtendRowSlice {
  customerName?: string | null
  customerEnglishName?: string | null
  customerCode?: string | null
}

export function sumCustomerExtendSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedCustomerExtendOuterWidth(widths: readonly number[]): number {
  const gaps = CUSTOMER_EXTEND_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumCustomerExtendSubColWidths(widths) +
    gaps +
    CUSTOMER_EXTEND_TOGGLE_RESERVE_PX +
    CUSTOMER_EXTEND_COL_PADDING_PX
  )
}

export function subColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}
