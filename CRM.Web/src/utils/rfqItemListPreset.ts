/** 需求明细列表左栏 preset（与后端 quickFilter / 时间窗参数一致）。 */

export const RFQ_ITEM_DEMAND_TIME_PRESET_IDS = [
  'item_last_10m',
  'item_last_30m',
  'item_last_1h',
  'item_today',
  'item_today_yesterday',
  'item_last_3_days',
  'item_last_7_days'
] as const

export type RfqItemDemandTimePresetId = (typeof RFQ_ITEM_DEMAND_TIME_PRESET_IDS)[number]

export const RFQ_ITEM_QUOTE_TIME_PRESET_IDS = [
  'quote_last_10m',
  'quote_last_30m',
  'quote_last_1h',
  'quote_today',
  'quote_today_yesterday',
  'quote_last_3_days',
  'quote_last_7_days'
] as const

export type RfqItemQuoteTimePresetId = (typeof RFQ_ITEM_QUOTE_TIME_PRESET_IDS)[number]

export const RFQ_ITEM_QUICK_FILTER_PRESET_IDS = [
  'important',
  'converted',
  'pending_quote',
  'no_quote',
  'multi_quote',
  'has_deleted_quote'
] as const

export type RfqItemQuickFilterPresetId = (typeof RFQ_ITEM_QUICK_FILTER_PRESET_IDS)[number]

export type RfqItemListPresetId =
  | RfqItemDemandTimePresetId
  | RfqItemQuoteTimePresetId
  | RfqItemQuickFilterPresetId

export const RFQ_ITEM_LIST_PRESET_IDS: readonly RfqItemListPresetId[] = [
  ...RFQ_ITEM_DEMAND_TIME_PRESET_IDS,
  ...RFQ_ITEM_QUOTE_TIME_PRESET_IDS,
  ...RFQ_ITEM_QUICK_FILTER_PRESET_IDS
]

export function isRfqItemListPresetId(v: unknown): v is RfqItemListPresetId {
  return typeof v === 'string' && (RFQ_ITEM_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export function isRfqItemDemandTimePresetId(v: unknown): v is RfqItemDemandTimePresetId {
  return typeof v === 'string' && (RFQ_ITEM_DEMAND_TIME_PRESET_IDS as readonly string[]).includes(v)
}

export function isRfqItemQuoteTimePresetId(v: unknown): v is RfqItemQuoteTimePresetId {
  return typeof v === 'string' && (RFQ_ITEM_QUOTE_TIME_PRESET_IDS as readonly string[]).includes(v)
}

export function isRfqItemQuickFilterPresetId(v: unknown): v is RfqItemQuickFilterPresetId {
  return typeof v === 'string' && (RFQ_ITEM_QUICK_FILTER_PRESET_IDS as readonly string[]).includes(v)
}

function localDayStart(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0)
}

function addLocalDays(d: Date, days: number): Date {
  const n = new Date(d)
  n.setDate(n.getDate() + days)
  return n
}

function toIso(d: Date): string {
  return d.toISOString()
}

/** 相对「现在」的分钟窗：[now-minutes, now) */
function resolveRecentMinutesRange(minutes: number): [string, string] {
  const end = new Date()
  const start = new Date(end.getTime() - minutes * 60_000)
  return [toIso(start), toIso(end)]
}

/**
 * 本地日历日窗：[startDay 0:00, endDayExclusive 0:00)
 * lastNDaysIncludingToday=3 → 今日往前共 3 天
 */
function resolveLocalCalendarDayRange(
  kind: 'today' | 'today_yesterday' | 'last_n',
  lastNDaysIncludingToday = 3
): [string, string] {
  const todayStart = localDayStart(new Date())
  const tomorrowStart = addLocalDays(todayStart, 1)
  if (kind === 'today') return [toIso(todayStart), toIso(tomorrowStart)]
  if (kind === 'today_yesterday') {
    const yesterdayStart = addLocalDays(todayStart, -1)
    return [toIso(yesterdayStart), toIso(tomorrowStart)]
  }
  const start = addLocalDays(todayStart, -(lastNDaysIncludingToday - 1))
  return [toIso(start), toIso(tomorrowStart)]
}

function resolveTimeWindowForSuffix(
  suffix: 'last_10m' | 'last_30m' | 'last_1h' | 'today' | 'today_yesterday' | 'last_3_days' | 'last_7_days'
): [string, string] {
  if (suffix === 'last_10m') return resolveRecentMinutesRange(10)
  if (suffix === 'last_30m') return resolveRecentMinutesRange(30)
  if (suffix === 'last_1h') return resolveRecentMinutesRange(60)
  if (suffix === 'today') return resolveLocalCalendarDayRange('today')
  if (suffix === 'today_yesterday') return resolveLocalCalendarDayRange('today_yesterday')
  if (suffix === 'last_3_days') return resolveLocalCalendarDayRange('last_n', 3)
  return resolveLocalCalendarDayRange('last_n', 7)
}

export type RfqItemPresetApiParams = {
  itemCreateStart?: string
  itemCreateEndExclusive?: string
  quoteCreateStart?: string
  quoteCreateEndExclusive?: string
  quickFilter?: string
}

export function resolveRfqItemPresetApiParams(preset: RfqItemListPresetId): RfqItemPresetApiParams {
  if (isRfqItemQuickFilterPresetId(preset)) {
    return { quickFilter: preset }
  }
  if (isRfqItemDemandTimePresetId(preset)) {
    const suffix = preset.slice('item_'.length) as Parameters<typeof resolveTimeWindowForSuffix>[0]
    const [itemCreateStart, itemCreateEndExclusive] = resolveTimeWindowForSuffix(suffix)
    return { itemCreateStart, itemCreateEndExclusive }
  }
  const suffix = preset.slice('quote_'.length) as Parameters<typeof resolveTimeWindowForSuffix>[0]
  const [quoteCreateStart, quoteCreateEndExclusive] = resolveTimeWindowForSuffix(suffix)
  return { quoteCreateStart, quoteCreateEndExclusive }
}

/** keyword 区 query 键（可与 preset 叠加） */
export const RFQ_ITEM_KEYWORD_QUERY_KEYS = [
  'rfqCode',
  'customerKeyword',
  'materialModel',
  'brandId',
  'salesUserId',
  'purchaserUserId'
] as const

export function pickRfqItemKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of RFQ_ITEM_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v === 'string' && v.trim()) out[key] = v.trim()
  }
  return out
}

export function buildRfqItemListRouteQuery(input: {
  preset?: RfqItemListPresetId | null
  keywords?: Record<string, string>
  advanced?: {
    startDate?: string
    endDate?: string
    itemStatus?: string
    hasQuotesOnly?: boolean
  }
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }
  if (input.preset) {
    q.preset = input.preset
    const api = resolveRfqItemPresetApiParams(input.preset)
    if (api.itemCreateStart) q.itemCreateStart = api.itemCreateStart
    if (api.itemCreateEndExclusive) q.itemCreateEndExclusive = api.itemCreateEndExclusive
    if (api.quoteCreateStart) q.quoteCreateStart = api.quoteCreateStart
    if (api.quoteCreateEndExclusive) q.quoteCreateEndExclusive = api.quoteCreateEndExclusive
    if (api.quickFilter) q.quickFilter = api.quickFilter
    return q
  }
  const adv = input.advanced ?? {}
  if (adv.startDate) q.startDate = adv.startDate
  if (adv.endDate) q.endDate = adv.endDate
  if (adv.itemStatus !== undefined && adv.itemStatus !== '') q.itemStatus = adv.itemStatus
  if (adv.hasQuotesOnly) q.hasQuotesOnly = '1'
  return q
}

export function presetI18nKey(preset: RfqItemListPresetId): string {
  return `rfqItemList.searchPanel.presets.${preset}`
}
