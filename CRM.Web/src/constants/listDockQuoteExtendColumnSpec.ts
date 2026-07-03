/** 《列表扩展列规范》：采购报价面板 — 晶圆产地 | 封装产地 | 是否包邮 */
export const LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_MIN_WIDTH = 152
export const LIST_DOCK_QUOTE_EXTEND_COL_COLLAPSED_WIDTH = 152

export const DOCK_QUOTE_EXTEND_SUB_COL_DEFAULT_WIDTHS: [number, number, number] = [72, 72, 56]
export const DOCK_QUOTE_EXTEND_SUB_COL_MIN_WIDTH = 48
export const DOCK_QUOTE_EXTEND_SUB_COL_GAP_PX = 8
export const DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX = 32
export const DOCK_QUOTE_EXTEND_COL_PADDING_PX = 16

export const DOCK_QUOTE_EXTEND_COL_STORAGE_KEY =
  'crm-table-extend-col:v1:rfq-item-list:dock-quote'

export type DockQuoteExtendFieldKey = 'waferOrigin' | 'packageOrigin' | 'freeShipping'

export const DOCK_QUOTE_EXTEND_FIELD_KEYS: DockQuoteExtendFieldKey[] = [
  'waferOrigin',
  'packageOrigin',
  'freeShipping'
]

export function sumDockQuoteExtendSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedDockQuoteExtendOuterWidth(widths: readonly number[]): number {
  const gaps = DOCK_QUOTE_EXTEND_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumDockQuoteExtendSubColWidths(widths) +
    gaps +
    DOCK_QUOTE_EXTEND_TOGGLE_RESERVE_PX +
    DOCK_QUOTE_EXTEND_COL_PADDING_PX
  )
}

export function subColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}
