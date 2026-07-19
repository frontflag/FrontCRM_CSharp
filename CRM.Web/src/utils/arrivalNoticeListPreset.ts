/** 到货通知列表左栏 preset（与后端 ArrivalNoticeListQuickFilterCodes / URL preset 一致）。 */

export const ARRIVAL_NOTICE_OVERDUE_PRESET_IDS = [
  'overdue_all',
  'overdue_1_day',
  'overdue_3_days',
  'overdue_1_week'
] as const

export const ARRIVAL_NOTICE_EXPECTED_PRESET_IDS = [
  'expected_today',
  'expected_tomorrow',
  'expected_within_3_days',
  'expected_within_7_days'
] as const

export const ARRIVAL_NOTICE_ARRIVED_PRESET_IDS = [
  'not_arrived',
  'arrived_today',
  'arrived_today_yesterday',
  'arrived_within_3_days',
  'arrived_within_7_days',
  'arrived_within_30_days'
] as const

export const ARRIVAL_NOTICE_TYPE_PRESET_IDS = ['type_purchase', 'type_customs'] as const

export const ARRIVAL_NOTICE_TODO_PRESET_IDS = ['todo_pending_qc', 'todo_pending_stock_in'] as const

export const ARRIVAL_NOTICE_STATUS_PRESET_IDS = ['status_qc_done', 'status_stocked_in'] as const

export type ArrivalNoticeOverduePresetId = (typeof ARRIVAL_NOTICE_OVERDUE_PRESET_IDS)[number]
export type ArrivalNoticeExpectedPresetId = (typeof ARRIVAL_NOTICE_EXPECTED_PRESET_IDS)[number]
export type ArrivalNoticeArrivedPresetId = (typeof ARRIVAL_NOTICE_ARRIVED_PRESET_IDS)[number]
export type ArrivalNoticeTypePresetId = (typeof ARRIVAL_NOTICE_TYPE_PRESET_IDS)[number]
export type ArrivalNoticeTodoPresetId = (typeof ARRIVAL_NOTICE_TODO_PRESET_IDS)[number]
export type ArrivalNoticeStatusPresetId = (typeof ARRIVAL_NOTICE_STATUS_PRESET_IDS)[number]

export type ArrivalNoticeListPresetId =
  | ArrivalNoticeOverduePresetId
  | ArrivalNoticeExpectedPresetId
  | ArrivalNoticeArrivedPresetId
  | ArrivalNoticeTypePresetId
  | ArrivalNoticeTodoPresetId
  | ArrivalNoticeStatusPresetId

export const ARRIVAL_NOTICE_LIST_PRESET_IDS: readonly ArrivalNoticeListPresetId[] = [
  ...ARRIVAL_NOTICE_OVERDUE_PRESET_IDS,
  ...ARRIVAL_NOTICE_EXPECTED_PRESET_IDS,
  ...ARRIVAL_NOTICE_ARRIVED_PRESET_IDS,
  ...ARRIVAL_NOTICE_TYPE_PRESET_IDS,
  ...ARRIVAL_NOTICE_TODO_PRESET_IDS,
  ...ARRIVAL_NOTICE_STATUS_PRESET_IDS
]

export function isArrivalNoticeListPresetId(v: unknown): v is ArrivalNoticeListPresetId {
  return typeof v === 'string' && (ARRIVAL_NOTICE_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export const ARRIVAL_NOTICE_KEYWORD_QUERY_KEYS = ['purchaseOrderCode', 'freightForwarderOrderNo'] as const

export function pickArrivalNoticeKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of ARRIVAL_NOTICE_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v === 'string' && v.trim()) out[key] = v.trim()
  }
  return out
}

export function buildArrivalNoticeListRouteQuery(input: {
  preset?: ArrivalNoticeListPresetId | null
  keywords?: Record<string, string>
  advanced?: {
    status?: string
    stockInType?: string
    expectedArrivalDate?: string
    noticeId?: string
  }
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }
  if (input.preset) {
    q.preset = input.preset
    return q
  }
  const adv = input.advanced ?? {}
  if (adv.status) q.status = adv.status
  if (adv.stockInType) q.stockInType = adv.stockInType
  if (adv.expectedArrivalDate) q.expectedArrivalDate = adv.expectedArrivalDate
  if (adv.noticeId) q.noticeId = adv.noticeId
  return q
}

export function presetI18nKey(preset: ArrivalNoticeListPresetId): string {
  return `arrivalNoticeList.searchPanel.presets.${preset}`
}

export function presetConflictsStatusField(preset: ArrivalNoticeListPresetId): boolean {
  return (
    (ARRIVAL_NOTICE_OVERDUE_PRESET_IDS as readonly string[]).includes(preset) ||
    preset === 'not_arrived' ||
    (ARRIVAL_NOTICE_TODO_PRESET_IDS as readonly string[]).includes(preset) ||
    (ARRIVAL_NOTICE_STATUS_PRESET_IDS as readonly string[]).includes(preset)
  )
}

export function presetConflictsStockInTypeField(preset: ArrivalNoticeListPresetId): boolean {
  return (ARRIVAL_NOTICE_TYPE_PRESET_IDS as readonly string[]).includes(preset)
}

export function presetHidesExpectedDateField(preset: ArrivalNoticeListPresetId): boolean {
  return (
    (ARRIVAL_NOTICE_OVERDUE_PRESET_IDS as readonly string[]).includes(preset) ||
    (ARRIVAL_NOTICE_EXPECTED_PRESET_IDS as readonly string[]).includes(preset) ||
    (['arrived_today', 'arrived_today_yesterday', 'arrived_within_3_days', 'arrived_within_7_days', 'arrived_within_30_days'] as readonly string[]).includes(
      preset
    )
  )
}

export function presetConflictsStatusTab(preset: ArrivalNoticeListPresetId): boolean {
  return presetConflictsStatusField(preset)
}

export function presetConflictsStockInTypeTab(preset: ArrivalNoticeListPresetId): boolean {
  return presetConflictsStockInTypeField(preset)
}
