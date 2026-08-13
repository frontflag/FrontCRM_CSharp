/** 销项发票核销：待核销发票 / 待核销应收 面板排列偏好（localStorage）。 */

const STORAGE_KEY = 'crm.sell-invoice-write-off.panels-layout'

export type SellInvoiceWriteOffPanelsLayout = 'row' | 'column'

const LAYOUT_SET = new Set<string>(['row', 'column'])

function isLayout(value: string): value is SellInvoiceWriteOffPanelsLayout {
  return LAYOUT_SET.has(value)
}

/** 默认左右排列 */
export function readSellInvoiceWriteOffPanelsLayout(): SellInvoiceWriteOffPanelsLayout {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw && isLayout(raw)) return raw
    return 'row'
  } catch {
    return 'row'
  }
}

export function writeSellInvoiceWriteOffPanelsLayout(
  layout: SellInvoiceWriteOffPanelsLayout
): void {
  try {
    localStorage.setItem(STORAGE_KEY, layout)
  } catch {
    /* ignore */
  }
}
