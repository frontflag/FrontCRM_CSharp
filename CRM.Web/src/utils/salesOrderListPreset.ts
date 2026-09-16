import {
  SO_ITEM_QUICK_FILTER_PRESET_IDS,
  SO_ITEM_TIME_PRESET_IDS,
  isSoItemListPresetId,
  isSoItemQuickFilterPresetId,
  isSoItemTimePresetId,
  resolveSoItemTimePresetDateRange,
  type SoItemListPresetId,
  type SoItemQuickFilterPresetId,
  type SoItemTimePresetId
} from '@/utils/salesOrderItemListPreset'

/** 销售订单主表列表左栏 preset（码与明细列表相同；时间展开为 startDate/endDate）。 */

export const SO_LIST_TIME_PRESET_IDS = SO_ITEM_TIME_PRESET_IDS
export const SO_LIST_QUICK_FILTER_PRESET_IDS = SO_ITEM_QUICK_FILTER_PRESET_IDS

export type SoListTimePresetId = SoItemTimePresetId
export type SoListQuickFilterPresetId = SoItemQuickFilterPresetId
export type SoListPresetId = SoItemListPresetId

export const SO_LIST_PRESET_IDS: readonly SoListPresetId[] = [
  ...SO_LIST_TIME_PRESET_IDS,
  ...SO_LIST_QUICK_FILTER_PRESET_IDS
]

export function isSoListPresetId(v: unknown): v is SoListPresetId {
  return isSoItemListPresetId(v)
}

export function isSoListTimePresetId(v: unknown): v is SoListTimePresetId {
  return isSoItemTimePresetId(v)
}

export function isSoListQuickFilterPresetId(v: unknown): v is SoListQuickFilterPresetId {
  return isSoItemQuickFilterPresetId(v)
}

export type SoListPresetApiParams = {
  startDate?: string
  endDate?: string
  quickFilter?: string
}

export function resolveSoListPresetApiParams(preset: SoListPresetId): SoListPresetApiParams {
  if (isSoListTimePresetId(preset)) {
    const [startDate, endDate] = resolveSoItemTimePresetDateRange(preset)
    return { startDate, endDate }
  }
  return { quickFilter: preset }
}

export const SO_LIST_KEYWORD_QUERY_KEYS = ['code', 'customer', 'salesUserName', 'comment'] as const

export function pickSoListKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of SO_LIST_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v === 'string' && v.trim()) out[key] = v.trim()
  }
  return out
}

export function buildSoListRouteQuery(input: {
  preset?: SoListPresetId | null
  keywords?: Record<string, string>
  advanced?: {
    startDate?: string
    endDate?: string
    status?: string
  }
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }
  if (input.preset) {
    q.preset = input.preset
    const api = resolveSoListPresetApiParams(input.preset)
    if (api.startDate) q.startDate = api.startDate
    if (api.endDate) q.endDate = api.endDate
    if (api.quickFilter) q.quickFilter = api.quickFilter
    return q
  }
  const adv = input.advanced ?? {}
  if (adv.startDate) q.startDate = adv.startDate
  if (adv.endDate) q.endDate = adv.endDate
  if (adv.status) q.status = adv.status
  return q
}

export function presetI18nKey(preset: SoListPresetId): string {
  return `salesOrderList.searchPanel.presets.${preset}`
}
