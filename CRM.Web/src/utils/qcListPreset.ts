/** 质检列表左栏 preset（与后端 QcListQuickFilterCodes / URL preset 一致）。 */

export const QC_TIME_PRESET_IDS = [
  'qc_today',
  'qc_today_yesterday',
  'qc_within_3_days',
  'qc_within_7_days',
  'qc_within_30_days'
] as const

export const QC_STATUS_PRESET_IDS = [
  'status_passed',
  'status_partial',
  'status_rejected',
  'has_qc_images',
  'no_qc_images'
] as const

export type QcTimePresetId = (typeof QC_TIME_PRESET_IDS)[number]
export type QcStatusPresetId = (typeof QC_STATUS_PRESET_IDS)[number]
export type QcListPresetId = QcTimePresetId | QcStatusPresetId

export const QC_LIST_PRESET_IDS: readonly QcListPresetId[] = [
  ...QC_TIME_PRESET_IDS,
  ...QC_STATUS_PRESET_IDS
]

export function isQcListPresetId(v: unknown): v is QcListPresetId {
  return typeof v === 'string' && (QC_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export const QC_KEYWORD_QUERY_KEYS = [
  'qcCode',
  'model',
  'vendorName',
  'purchaseOrderCode',
  'freightForwarderOrderNo',
  'salesOrderCode',
  'stockInType'
] as const

export function pickQcKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of QC_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v === 'string' && v.trim()) out[key] = v.trim()
  }
  return out
}

export function buildQcListRouteQuery(input: {
  preset?: QcListPresetId | null
  keywords?: Record<string, string>
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }
  if (input.preset) q.preset = input.preset
  return q
}

export function presetI18nKey(preset: QcListPresetId): string {
  return `qcList.searchPanel.presets.${preset}`
}
