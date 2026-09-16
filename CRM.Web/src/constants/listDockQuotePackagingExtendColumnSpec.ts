/** 《列表扩展列规范》：采购报价面板 — 库存 | 最小包装 | 起订量 */
export const LIST_DOCK_QUOTE_PACKAGING_EXTEND_COL_COLLAPSED_MIN_WIDTH = 152
export const LIST_DOCK_QUOTE_PACKAGING_EXTEND_COL_COLLAPSED_WIDTH = 152

export const DOCK_QUOTE_PACKAGING_EXTEND_SUB_COL_DEFAULT_WIDTHS: [number, number, number] = [64, 64, 64]
export const DOCK_QUOTE_PACKAGING_EXTEND_SUB_COL_MIN_WIDTH = 48
export const DOCK_QUOTE_PACKAGING_EXTEND_SUB_COL_GAP_PX = 8
export const DOCK_QUOTE_PACKAGING_EXTEND_TOGGLE_RESERVE_PX = 32
export const DOCK_QUOTE_PACKAGING_EXTEND_COL_PADDING_PX = 16

export const DOCK_QUOTE_PACKAGING_EXTEND_COL_STORAGE_KEY =
  'crm-table-extend-col:v1:rfq-item-list:dock-quote-packaging'

export type DockQuotePackagingExtendFieldKey = 'stock' | 'minPackage' | 'moq'

export const DOCK_QUOTE_PACKAGING_EXTEND_FIELD_KEYS: DockQuotePackagingExtendFieldKey[] = [
  'stock',
  'minPackage',
  'moq'
]

export function sumDockQuotePackagingExtendSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedDockQuotePackagingExtendOuterWidth(widths: readonly number[]): number {
  const gaps = DOCK_QUOTE_PACKAGING_EXTEND_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumDockQuotePackagingExtendSubColWidths(widths) +
    gaps +
    DOCK_QUOTE_PACKAGING_EXTEND_TOGGLE_RESERVE_PX +
    DOCK_QUOTE_PACKAGING_EXTEND_COL_PADDING_PX
  )
}

export function dockQuotePackagingSubColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}

const STOCK_KEYS = ['stockQty', 'StockQty']
const MIN_PACKAGE_KEYS = ['minPackageQty', 'MinPackageQty']
const MOQ_KEYS = ['moq', 'Moq', 'minOrderQty', 'MinOrderQty']

function quoteItemsRaw(row: Record<string, unknown>): Record<string, unknown>[] {
  const rawItems = (row.items ?? row.Items) as unknown[] | undefined
  if (!rawItems?.length) return []
  return rawItems.map((it) => it as Record<string, unknown>)
}

function uniqueIntDisplay(items: Record<string, unknown>[], keys: string[]): string {
  const set = new Set<string>()
  let found = false
  for (const o of items) {
    for (const k of keys) {
      const raw = o[k]
      if (raw == null || raw === '') continue
      const n = Number(raw)
      if (!Number.isFinite(n)) continue
      found = true
      set.add(String(Math.trunc(n)))
      break
    }
  }
  if (!found) return ''
  return [...set].join('、')
}

function keysForField(field: DockQuotePackagingExtendFieldKey): string[] {
  if (field === 'stock') return STOCK_KEYS
  if (field === 'minPackage') return MIN_PACKAGE_KEYS
  return MOQ_KEYS
}

export function pickDockQuotePackagingField(
  row: Record<string, unknown>,
  field: DockQuotePackagingExtendFieldKey
): string {
  const keys = keysForField(field)
  const items = quoteItemsRaw(row)
  const fromItems = uniqueIntDisplay(items, keys)
  if (fromItems) return fromItems
  return uniqueIntDisplay([row], keys)
}
