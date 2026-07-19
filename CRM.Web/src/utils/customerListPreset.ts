/** 客户列表左栏 preset（与 URL query.preset 一致）。 */

import type { CustomerListFilterQuery } from '@/utils/customerListQuery'

export const CUSTOMER_TIME_PRESET_IDS = [
  'created_today',
  'created_last_7_days',
  'created_last_30_days'
] as const

export type CustomerTimePresetId = (typeof CUSTOMER_TIME_PRESET_IDS)[number]

export const CUSTOMER_ATTENTION_PRESET_IDS = ['favorite', 'vip'] as const

export type CustomerAttentionPresetId = (typeof CUSTOMER_ATTENTION_PRESET_IDS)[number]

export const CUSTOMER_TODO_PRESET_IDS = ['pending_submit', 'pending_audit'] as const

export type CustomerTodoPresetId = (typeof CUSTOMER_TODO_PRESET_IDS)[number]

export const CUSTOMER_DEMAND_PRESET_IDS = [
  'has_demand',
  'demand_last_7_days',
  'demand_last_30_days',
  'demand_stale_6m',
  'demand_stale_1y'
] as const

export type CustomerDemandPresetId = (typeof CUSTOMER_DEMAND_PRESET_IDS)[number]

export const CUSTOMER_DEAL_PRESET_IDS = [
  'has_deal',
  'deal_last_7_days',
  'deal_last_30_days',
  'deal_stale_6m',
  'deal_stale_1y'
] as const

export type CustomerDealPresetId = (typeof CUSTOMER_DEAL_PRESET_IDS)[number]

export const CUSTOMER_BUSINESS_PRESET_IDS = ['pending_shipment', 'has_receivable'] as const

export type CustomerBusinessPresetId = (typeof CUSTOMER_BUSINESS_PRESET_IDS)[number]

/** 需后端 quickFilter 的 preset（第二、三阶段） */
export const CUSTOMER_QUICK_FILTER_PRESET_IDS = [
  ...CUSTOMER_DEMAND_PRESET_IDS,
  ...CUSTOMER_DEAL_PRESET_IDS,
  ...CUSTOMER_BUSINESS_PRESET_IDS
] as const

export type CustomerQuickFilterPresetId = (typeof CUSTOMER_QUICK_FILTER_PRESET_IDS)[number]

export const CUSTOMER_LIST_PRESET_IDS = [
  ...CUSTOMER_TIME_PRESET_IDS,
  ...CUSTOMER_ATTENTION_PRESET_IDS,
  ...CUSTOMER_TODO_PRESET_IDS,
  ...CUSTOMER_QUICK_FILTER_PRESET_IDS
] as const

export type CustomerListPresetId =
  | CustomerTimePresetId
  | CustomerAttentionPresetId
  | CustomerTodoPresetId
  | CustomerQuickFilterPresetId

export function isCustomerListPresetId(v: unknown): v is CustomerListPresetId {
  return typeof v === 'string' && (CUSTOMER_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export function isCustomerTimePresetId(v: unknown): v is CustomerTimePresetId {
  return typeof v === 'string' && (CUSTOMER_TIME_PRESET_IDS as readonly string[]).includes(v)
}

export function isCustomerQuickFilterPresetId(v: unknown): v is CustomerQuickFilterPresetId {
  return typeof v === 'string' && (CUSTOMER_QUICK_FILTER_PRESET_IDS as readonly string[]).includes(v)
}

function formatYmd(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/** 与 CustomerListQuery 一致：本地日历日 YYYY-MM-DD，含起止日 */
export function resolveCustomerTimePresetDateRange(preset: CustomerTimePresetId): [string, string] {
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

export type CustomerPresetExpandedQuery = Partial<
  Pick<CustomerListFilterQuery, 'status' | 'customerLevel' | 'createdFrom' | 'createdTo' | 'favoriteOnly'>
>

export function resolveCustomerPresetExpandedQuery(
  preset: CustomerListPresetId
): CustomerPresetExpandedQuery {
  if (isCustomerQuickFilterPresetId(preset)) return {}
  if (isCustomerTimePresetId(preset)) {
    const [createdFrom, createdTo] = resolveCustomerTimePresetDateRange(preset)
    return { createdFrom, createdTo }
  }
  if (preset === 'favorite') return { favoriteOnly: true }
  if (preset === 'vip') return { customerLevel: 'VIP' }
  if (preset === 'pending_submit') return { status: 1 }
  if (preset === 'pending_audit') return { status: 2 }
  return {}
}

/** keyword 区 query 键（可与 preset 叠加） */
export const CUSTOMER_KEYWORD_QUERY_KEYS = [
  'searchTerm',
  'customerType',
  'industry',
  'salesUserId',
  'currency'
] as const

export function pickCustomerKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  const st = query.searchTerm
  if (typeof st === 'string' && st.trim()) out.searchTerm = st.trim()
  const ct = query.customerType
  if (ct !== undefined && ct !== null && ct !== '') out.customerType = String(ct)
  const ind = query.industry
  if (typeof ind === 'string' && ind.trim()) out.industry = ind.trim()
  const su = query.salesUserId
  if (typeof su === 'string' && su.trim()) out.salesUserId = su.trim()
  const cur = query.currency
  if (cur !== undefined && cur !== null && cur !== '') out.currency = String(cur)
  return out
}

export function buildCustomerListRouteQuery(input: {
  preset?: CustomerListPresetId | null
  keywords?: Record<string, string>
  advanced?: Partial<CustomerListFilterQuery>
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }

  if (input.preset) {
    q.preset = input.preset
    if (isCustomerQuickFilterPresetId(input.preset)) {
      q.quickFilter = input.preset
      return q
    }
    const expanded = resolveCustomerPresetExpandedQuery(input.preset)
    if (expanded.createdFrom) q.createdFrom = expanded.createdFrom
    if (expanded.createdTo) q.createdTo = expanded.createdTo
    if (expanded.status != null && !Number.isNaN(expanded.status)) {
      q.status = String(expanded.status)
    }
    if (expanded.customerLevel) q.customerLevel = expanded.customerLevel
    if (expanded.favoriteOnly) q.favoriteOnly = '1'
    return q
  }

  const adv = input.advanced ?? {}
  if (adv.searchTerm?.trim()) q.searchTerm = adv.searchTerm.trim()
  if (adv.customerType != null && !Number.isNaN(adv.customerType)) {
    q.customerType = String(adv.customerType)
  }
  if (adv.customerLevel) q.customerLevel = adv.customerLevel
  if (adv.industry) q.industry = adv.industry
  if (adv.currency != null && !Number.isNaN(adv.currency)) q.currency = String(adv.currency)
  if (adv.status != null && !Number.isNaN(adv.status)) q.status = String(adv.status)
  if (adv.salesUserId) q.salesUserId = adv.salesUserId
  if (adv.createdFrom) q.createdFrom = adv.createdFrom
  if (adv.createdTo) q.createdTo = adv.createdTo
  if (adv.favoriteOnly) q.favoriteOnly = '1'
  return q
}

export function presetI18nKey(preset: CustomerListPresetId): string {
  return `customerList.searchPanel.presets.${preset}`
}
