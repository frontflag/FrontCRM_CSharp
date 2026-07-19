/** 供应商列表左栏 preset（与 URL query.preset 一致）。 */

import type { VendorListFilterQuery } from '@/utils/vendorListQuery'

export const VENDOR_TIME_PRESET_IDS = [
  'created_today',
  'created_last_7_days',
  'created_last_30_days'
] as const

export type VendorTimePresetId = (typeof VENDOR_TIME_PRESET_IDS)[number]

export const VENDOR_ATTENTION_PRESET_IDS = ['favorite'] as const

export type VendorAttentionPresetId = (typeof VENDOR_ATTENTION_PRESET_IDS)[number]

export const VENDOR_TODO_PRESET_IDS = ['pending_submit', 'pending_audit'] as const

export type VendorTodoPresetId = (typeof VENDOR_TODO_PRESET_IDS)[number]

export const VENDOR_QUOTE_PRESET_IDS = [
  'has_quote',
  'quote_last_7_days',
  'quote_last_30_days',
  'quote_stale_6m',
  'quote_stale_1y'
] as const

export type VendorQuotePresetId = (typeof VENDOR_QUOTE_PRESET_IDS)[number]

export const VENDOR_PURCHASE_PRESET_IDS = [
  'has_purchase',
  'purchase_last_7_days',
  'purchase_last_30_days',
  'purchase_stale_6m',
  'purchase_stale_1y'
] as const

export type VendorPurchasePresetId = (typeof VENDOR_PURCHASE_PRESET_IDS)[number]

export const VENDOR_BUSINESS_PRESET_IDS = ['pending_inbound', 'has_payable'] as const

export type VendorBusinessPresetId = (typeof VENDOR_BUSINESS_PRESET_IDS)[number]

export const VENDOR_QUICK_FILTER_PRESET_IDS = [
  ...VENDOR_QUOTE_PRESET_IDS,
  ...VENDOR_PURCHASE_PRESET_IDS,
  ...VENDOR_BUSINESS_PRESET_IDS
] as const

export type VendorQuickFilterPresetId = (typeof VENDOR_QUICK_FILTER_PRESET_IDS)[number]

export const VENDOR_LIST_PRESET_IDS = [
  ...VENDOR_TIME_PRESET_IDS,
  ...VENDOR_ATTENTION_PRESET_IDS,
  ...VENDOR_TODO_PRESET_IDS,
  ...VENDOR_QUICK_FILTER_PRESET_IDS
] as const

export type VendorListPresetId =
  | VendorTimePresetId
  | VendorAttentionPresetId
  | VendorTodoPresetId
  | VendorQuickFilterPresetId

export function isVendorListPresetId(v: unknown): v is VendorListPresetId {
  return typeof v === 'string' && (VENDOR_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export function isVendorTimePresetId(v: unknown): v is VendorTimePresetId {
  return typeof v === 'string' && (VENDOR_TIME_PRESET_IDS as readonly string[]).includes(v)
}

export function isVendorQuickFilterPresetId(v: unknown): v is VendorQuickFilterPresetId {
  return typeof v === 'string' && (VENDOR_QUICK_FILTER_PRESET_IDS as readonly string[]).includes(v)
}

function formatYmd(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

export function resolveVendorTimePresetDateRange(preset: VendorTimePresetId): [string, string] {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const end = formatYmd(today)

  if (preset === 'created_today') {
    return [end, end]
  }
  if (preset === 'created_last_7_days') {
    const start = new Date(today)
    start.setDate(start.getDate() - 6)
    return [formatYmd(start), end]
  }
  const start = new Date(today)
  start.setDate(start.getDate() - 29)
  return [formatYmd(start), end]
}

export type VendorPresetExpandedQuery = Partial<
  Pick<VendorListFilterQuery, 'status' | 'createdFrom' | 'createdTo' | 'favoriteOnly'>
>

export function resolveVendorPresetExpandedQuery(preset: VendorListPresetId): VendorPresetExpandedQuery {
  if (isVendorQuickFilterPresetId(preset)) return {}
  if (isVendorTimePresetId(preset)) {
    const [createdFrom, createdTo] = resolveVendorTimePresetDateRange(preset)
    return { createdFrom, createdTo }
  }
  if (preset === 'favorite') return { favoriteOnly: true }
  if (preset === 'pending_submit') return { status: 1 }
  if (preset === 'pending_audit') return { status: 2 }
  return {}
}

export const VENDOR_KEYWORD_QUERY_KEYS = [
  'searchTerm',
  'credit',
  'ascriptionType',
  'industry',
  'purchaseUserId',
  'currency'
] as const

export function pickVendorKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  const st = query.searchTerm
  if (typeof st === 'string' && st.trim()) out.searchTerm = st.trim()
  const cr = query.credit
  if (cr !== undefined && cr !== null && cr !== '') out.credit = String(cr)
  const at = query.ascriptionType
  if (at !== undefined && at !== null && at !== '') out.ascriptionType = String(at)
  const ind = query.industry
  if (typeof ind === 'string' && ind.trim()) out.industry = ind.trim()
  const pu = query.purchaseUserId
  if (typeof pu === 'string' && pu.trim()) out.purchaseUserId = pu.trim()
  const cur = query.currency
  if (cur !== undefined && cur !== null && cur !== '') out.currency = String(cur)
  return out
}

export function buildVendorListRouteQuery(input: {
  preset?: VendorListPresetId | null
  keywords?: Record<string, string>
  advanced?: Partial<VendorListFilterQuery>
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }

  if (input.preset) {
    q.preset = input.preset
    if (isVendorQuickFilterPresetId(input.preset)) {
      q.quickFilter = input.preset
      return q
    }
    const expanded = resolveVendorPresetExpandedQuery(input.preset)
    if (expanded.createdFrom) q.createdFrom = expanded.createdFrom
    if (expanded.createdTo) q.createdTo = expanded.createdTo
    if (expanded.status != null && !Number.isNaN(expanded.status)) {
      q.status = String(expanded.status)
    }
    if (expanded.favoriteOnly) q.favoriteOnly = '1'
    return q
  }

  const adv = input.advanced ?? {}
  if (adv.searchTerm?.trim()) q.searchTerm = adv.searchTerm.trim()
  if (adv.status != null && !Number.isNaN(adv.status)) q.status = String(adv.status)
  if (adv.level != null && !Number.isNaN(adv.level)) q.level = String(adv.level)
  if (adv.credit != null && !Number.isNaN(adv.credit)) q.credit = String(adv.credit)
  if (adv.ascriptionType != null && !Number.isNaN(adv.ascriptionType)) {
    q.ascriptionType = String(adv.ascriptionType)
  }
  if (adv.industry) q.industry = adv.industry
  if (adv.currency != null && !Number.isNaN(adv.currency)) q.currency = String(adv.currency)
  if (adv.purchaseUserId) q.purchaseUserId = adv.purchaseUserId
  if (adv.createdFrom) q.createdFrom = adv.createdFrom
  if (adv.createdTo) q.createdTo = adv.createdTo
  if (adv.favoriteOnly) q.favoriteOnly = '1'
  return q
}

export function presetI18nKey(preset: VendorListPresetId): string {
  return `vendorList.searchPanel.presets.${preset}`
}
