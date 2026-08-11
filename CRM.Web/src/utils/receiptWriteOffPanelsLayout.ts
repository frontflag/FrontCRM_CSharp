/** 收款核销：待核销收款 / 待核销应收 面板排列偏好（localStorage）。 */

const STORAGE_KEY = 'crm.receipt-write-off.panels-layout'

export type ReceiptWriteOffPanelsLayout = 'row' | 'column'

const LAYOUT_SET = new Set<string>(['row', 'column'])

function isLayout(value: string): value is ReceiptWriteOffPanelsLayout {
  return LAYOUT_SET.has(value)
}

/** 默认左右排列 */
export function readReceiptWriteOffPanelsLayout(): ReceiptWriteOffPanelsLayout {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw && isLayout(raw)) return raw
    return 'row'
  } catch {
    return 'row'
  }
}

export function writeReceiptWriteOffPanelsLayout(layout: ReceiptWriteOffPanelsLayout): void {
  try {
    localStorage.setItem(STORAGE_KEY, layout)
  } catch {
    /* ignore quota / private mode */
  }
}
