/** 采购订单明细列表左栏 preset（与后端 quickFilter / URL preset 一致）。 */

export const PO_ITEM_TIME_PRESET_IDS = [
  'order_today',
  'order_today_yesterday',
  'order_last_7_days',
  'order_last_30_days',
  'order_this_week',
  'order_this_month'
] as const

export type PoItemTimePresetId = (typeof PO_ITEM_TIME_PRESET_IDS)[number]

export const PO_ITEM_QUICK_FILTER_PRESET_IDS = [
  'pending_submit_audit',
  'pending_vendor_confirm',
  'pending_submit_payment_request',
  'pending_submit_arrival_notify',
  'pay_later',
  'confirmed_unpaid',
  'stocked_in_unpaid',
  'payment_partial',
  'payment_complete',
  'confirmed_pending_stock_in',
  'paid_pending_stock_in',
  'stocked_in'
] as const

export type PoItemQuickFilterPresetId = (typeof PO_ITEM_QUICK_FILTER_PRESET_IDS)[number]

export type PoItemListPresetId = PoItemTimePresetId | PoItemQuickFilterPresetId

export const PO_ITEM_LIST_PRESET_IDS: readonly PoItemListPresetId[] = [
  ...PO_ITEM_TIME_PRESET_IDS,
  ...PO_ITEM_QUICK_FILTER_PRESET_IDS
]

export function isPoItemListPresetId(v: unknown): v is PoItemListPresetId {
  return typeof v === 'string' && (PO_ITEM_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export function isPoItemTimePresetId(v: unknown): v is PoItemTimePresetId {
  return typeof v === 'string' && (PO_ITEM_TIME_PRESET_IDS as readonly string[]).includes(v)
}

export function isPoItemQuickFilterPresetId(v: unknown): v is PoItemQuickFilterPresetId {
  return typeof v === 'string' && (PO_ITEM_QUICK_FILTER_PRESET_IDS as readonly string[]).includes(v)
}

function formatYmd(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/** 与列表 startDate/endDate 一致：本地日历日 YYYY-MM-DD；本周以周一为一周起点 */
export function resolvePoItemTimePresetDateRange(preset: PoItemTimePresetId): [string, string] {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const end = formatYmd(today)

  if (preset === 'order_today') {
    return [end, end]
  }
  if (preset === 'order_today_yesterday') {
    const yesterday = new Date(today)
    yesterday.setDate(yesterday.getDate() - 1)
    return [formatYmd(yesterday), end]
  }
  if (preset === 'order_last_7_days') {
    const start = new Date(today)
    start.setDate(start.getDate() - 6)
    return [formatYmd(start), end]
  }
  if (preset === 'order_last_30_days') {
    const start = new Date(today)
    start.setDate(start.getDate() - 29)
    return [formatYmd(start), end]
  }
  if (preset === 'order_this_week') {
    const daysFromMonday = (today.getDay() + 6) % 7
    const start = new Date(today)
    start.setDate(start.getDate() - daysFromMonday)
    return [formatYmd(start), end]
  }
  const start = new Date(today.getFullYear(), today.getMonth(), 1)
  return [formatYmd(start), end]
}

export type PoItemPresetApiParams = {
  startDate?: string
  endDate?: string
  quickFilter?: string
}

export function resolvePoItemPresetApiParams(preset: PoItemListPresetId): PoItemPresetApiParams {
  if (isPoItemTimePresetId(preset)) {
    const [startDate, endDate] = resolvePoItemTimePresetDateRange(preset)
    return { startDate, endDate }
  }
  return { quickFilter: preset }
}

/** keyword 区 query 键（可与 preset 叠加；取消 preset 时一并清除） */
export const PO_ITEM_KEYWORD_QUERY_KEYS = [
  'purchaseOrderCode',
  'freightForwarderOrderNo',
  'vendorName',
  'purchaseUserName',
  'pn',
  'sellOrderItemCode',
  'transactionCurrency',
  'orderType'
] as const

export function pickPoItemKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of PO_ITEM_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v === 'string' && v.trim()) out[key] = v.trim()
  }
  return out
}

export function buildPoItemListRouteQuery(input: {
  preset?: PoItemListPresetId | null
  keywords?: Record<string, string>
  advanced?: {
    startDate?: string
    endDate?: string
    paymentProgressStatus?: string
    purchaseProgressStatus?: string
    stockInProgressStatus?: string
    invoiceProgressStatus?: string
  }
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }
  if (input.preset) {
    q.preset = input.preset
    const api = resolvePoItemPresetApiParams(input.preset)
    if (api.startDate) q.startDate = api.startDate
    if (api.endDate) q.endDate = api.endDate
    if (api.quickFilter) q.quickFilter = api.quickFilter
    return q
  }
  const adv = input.advanced ?? {}
  if (adv.startDate) q.startDate = adv.startDate
  if (adv.endDate) q.endDate = adv.endDate
  for (const k of [
    'paymentProgressStatus',
    'purchaseProgressStatus',
    'stockInProgressStatus',
    'invoiceProgressStatus'
  ] as const) {
    const v = adv[k]
    if (v !== undefined && v !== '') q[k] = v
  }
  return q
}

export function presetI18nKey(preset: PoItemListPresetId): string {
  return `purchaseOrderItemList.searchPanel.presets.${preset}`
}
