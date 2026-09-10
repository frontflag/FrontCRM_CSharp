/** 《列表扩展列规范 PRD》：报关列收起/展开宽度参考值 */
export const LIST_CUSTOMS_EXTEND_COL_COLLAPSED_MIN_WIDTH = 160
export const LIST_CUSTOMS_EXTEND_COL_COLLAPSED_WIDTH = 160

/** 展开态内部四列默认宽度：图标 | 报关单号 | 报关公司 | 报关状态 */
export const CUSTOMS_EXTEND_SUB_COL_DEFAULT_WIDTHS: [number, number, number, number] = [72, 168, 160, 110]
export const CUSTOMS_EXTEND_SUB_COL_MIN_WIDTH = 56
export const CUSTOMS_EXTEND_SUB_COL_GAP_PX = 8
/** 列头左侧展开/收起按钮占位 */
export const CUSTOMS_EXTEND_TOGGLE_RESERVE_PX = 32
/** 列头/单元格左右内边距余量 */
export const CUSTOMS_EXTEND_COL_PADDING_PX = 16

export const CUSTOMS_EXTEND_COL_STORAGE_KEY = 'crm-table-extend-col:v3:global:customs'

export type CustomsExtendFieldKey = 'icon' | 'declarationCode' | 'broker' | 'status'

export const CUSTOMS_EXTEND_FIELD_KEYS: CustomsExtendFieldKey[] = [
  'icon',
  'declarationCode',
  'broker',
  'status'
]

export type CustomsExtendSubColWidths = [number, number, number, number]

export interface CustomsExtendRowSlice {
  customsDeclarationId?: string | null
  CustomsDeclarationId?: string | null
  customsDeclarationCode?: string | null
  CustomsDeclarationCode?: string | null
  customsBrokerName?: string | null
  CustomsBrokerName?: string | null
  /** 出库通知业务报关状态（StockOutNotifyCustomsStatus） */
  customsStatus?: number | null
  CustomsStatus?: number | null
  /** 报关单海关状态（CustomsClearanceStatus） */
  customsClearanceStatus?: number | null
  CustomsClearanceStatus?: number | null
}

export type CustomsExtendStatusKind = 'notify' | 'clearance'

export function sumCustomsExtendSubColWidths(widths: readonly number[]): number {
  return widths.reduce((a, b) => a + b, 0)
}

export function expandedCustomsExtendOuterWidth(widths: readonly number[]): number {
  const gaps = CUSTOMS_EXTEND_SUB_COL_GAP_PX * (widths.length - 1)
  return (
    sumCustomsExtendSubColWidths(widths) +
    gaps +
    CUSTOMS_EXTEND_TOGGLE_RESERVE_PX +
    CUSTOMS_EXTEND_COL_PADDING_PX
  )
}

export function customsSubColWidthsToGridTemplate(widths: readonly number[]): string {
  return widths.map((w) => `${w}px`).join(' ')
}

function pickRowStr(row: CustomsExtendRowSlice, camel: keyof CustomsExtendRowSlice, pascal: keyof CustomsExtendRowSlice): string {
  const r = row as Record<string, unknown>
  return String(r[camel] ?? r[pascal] ?? '').trim()
}

export function pickCustomsDeclarationId(row: CustomsExtendRowSlice): string {
  return pickRowStr(row, 'customsDeclarationId', 'CustomsDeclarationId')
}

export function pickCustomsDeclarationCode(row: CustomsExtendRowSlice): string {
  return pickRowStr(row, 'customsDeclarationCode', 'CustomsDeclarationCode')
}

export function pickCustomsBrokerName(row: CustomsExtendRowSlice): string {
  return pickRowStr(row, 'customsBrokerName', 'CustomsBrokerName')
}

function pickOptionalNumber(
  row: CustomsExtendRowSlice,
  camel: keyof CustomsExtendRowSlice,
  pascal: keyof CustomsExtendRowSlice
): number | null {
  const r = row as Record<string, unknown>
  const raw = r[camel] ?? r[pascal]
  if (raw == null || raw === '') return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
}

/** 出库通知优先用业务报关状态；入库单/到货通知用报关单海关状态。 */
export function pickCustomsExtendStatusKind(row: CustomsExtendRowSlice): CustomsExtendStatusKind | null {
  if (pickOptionalNumber(row, 'customsStatus', 'CustomsStatus') != null) return 'notify'
  if (pickOptionalNumber(row, 'customsClearanceStatus', 'CustomsClearanceStatus') != null) return 'clearance'
  return null
}

export function pickCustomsExtendStatusCode(row: CustomsExtendRowSlice): number | null {
  const kind = pickCustomsExtendStatusKind(row)
  if (kind === 'notify') return pickOptionalNumber(row, 'customsStatus', 'CustomsStatus')
  if (kind === 'clearance') return pickOptionalNumber(row, 'customsClearanceStatus', 'CustomsClearanceStatus')
  return null
}

export function pickCustomsExtendFieldValue(
  row: CustomsExtendRowSlice,
  field: CustomsExtendFieldKey
): string {
  switch (field) {
    case 'icon':
      return pickCustomsDeclarationId(row)
    case 'declarationCode':
      return pickCustomsDeclarationCode(row)
    case 'broker':
      return pickCustomsBrokerName(row)
    case 'status': {
      const code = pickCustomsExtendStatusCode(row)
      return code == null ? '' : String(code)
    }
    default:
      return ''
  }
}
