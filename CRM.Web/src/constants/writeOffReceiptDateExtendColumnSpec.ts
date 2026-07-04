export const WRITE_OFF_RECEIPT_DATE_SUB_COL_DEFAULT_WIDTHS: [number, number] = [108, 108]
export const WRITE_OFF_RECEIPT_DATE_SUB_COL_MIN_WIDTH = 72
export const WRITE_OFF_RECEIPT_DATE_SUB_COL_GAP_PX = 8
export const WRITE_OFF_RECEIPT_DATE_TOGGLE_RESERVE_PX = 32
export const WRITE_OFF_RECEIPT_DATE_COL_PADDING_PX = 16
export const WRITE_OFF_RECEIPT_DATE_COL_COLLAPSED_WIDTH = 160
export const WRITE_OFF_RECEIPT_DATE_COL_COLLAPSED_MIN_WIDTH = 160

export type WriteOffReceiptDateFieldKey = 'earliest' | 'latest'

export const WRITE_OFF_RECEIPT_DATE_FIELD_KEYS: WriteOffReceiptDateFieldKey[] = ['earliest', 'latest']

export interface WriteOffReceiptDateRowSlice {
  earliestReceiptDate?: string | null
  latestReceiptDate?: string | null
}

export function sumWriteOffReceiptDateSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedWriteOffReceiptDateOuterWidth(widths: readonly number[]): number {
  const gaps = WRITE_OFF_RECEIPT_DATE_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumWriteOffReceiptDateSubColWidths(widths) +
    gaps +
    WRITE_OFF_RECEIPT_DATE_TOGGLE_RESERVE_PX +
    WRITE_OFF_RECEIPT_DATE_COL_PADDING_PX
  )
}

export function writeOffReceiptDateSubColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}
