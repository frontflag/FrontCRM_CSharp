import {
  PO_ITEM_QUICK_FILTER_PRESET_IDS,
  PO_ITEM_TIME_PRESET_IDS,
  isPoItemListPresetId,
  isPoItemQuickFilterPresetId,
  isPoItemTimePresetId,
  resolvePoItemTimePresetDateRange,
  type PoItemListPresetId,
  type PoItemQuickFilterPresetId,
  type PoItemTimePresetId
} from '@/utils/purchaseOrderItemListPreset'

/** 采购订单主表列表左栏 preset（码与明细列表相同；时间展开为 startDate/endDate）。 */

export const PO_LIST_TIME_PRESET_IDS = PO_ITEM_TIME_PRESET_IDS
export const PO_LIST_QUICK_FILTER_PRESET_IDS = PO_ITEM_QUICK_FILTER_PRESET_IDS

export type PoListTimePresetId = PoItemTimePresetId
export type PoListQuickFilterPresetId = PoItemQuickFilterPresetId
export type PoListPresetId = PoItemListPresetId

export const PO_LIST_PRESET_IDS: readonly PoListPresetId[] = [
  ...PO_LIST_TIME_PRESET_IDS,
  ...PO_LIST_QUICK_FILTER_PRESET_IDS
]

export function isPoListPresetId(v: unknown): v is PoListPresetId {
  return isPoItemListPresetId(v)
}

export function isPoListTimePresetId(v: unknown): v is PoListTimePresetId {
  return isPoItemTimePresetId(v)
}

export function isPoListQuickFilterPresetId(v: unknown): v is PoListQuickFilterPresetId {
  return isPoItemQuickFilterPresetId(v)
}

export type PoListPresetApiParams = {
  startDate?: string
  endDate?: string
  quickFilter?: string
}

export function resolvePoListPresetApiParams(preset: PoListPresetId): PoListPresetApiParams {
  if (isPoListTimePresetId(preset)) {
    const [startDate, endDate] = resolvePoItemTimePresetDateRange(preset)
    return { startDate, endDate }
  }
  return { quickFilter: preset }
}

export const PO_LIST_KEYWORD_QUERY_KEYS = [
  'code',
  'vendor',
  'purchaseUserName',
  'freightForwarderOrderNo',
  'comment',
  'orderType'
] as const

const ORDER_TYPE_QUERY_VALUES = new Set(['1', '2', '3'])

export function pickPoListKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of PO_LIST_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v !== 'string' && typeof v !== 'number') continue
    const s = String(v).trim()
    if (!s) continue
    if (key === 'orderType' && !ORDER_TYPE_QUERY_VALUES.has(s)) continue
    out[key] = s
  }
  return out
}

export function buildPoListRouteQuery(input: {
  preset?: PoListPresetId | null
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
    const api = resolvePoListPresetApiParams(input.preset)
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

export function presetI18nKey(preset: PoListPresetId): string {
  return `purchaseOrderList.searchPanel.presets.${preset}`
}
