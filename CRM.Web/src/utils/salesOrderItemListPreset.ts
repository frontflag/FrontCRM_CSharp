/** 销售订单明细列表左栏 preset（与后端 quickFilter / URL preset 一致）。 */

export const SO_ITEM_TIME_PRESET_IDS = [
  'order_today',
  'order_today_yesterday',
  'order_last_7_days',
  'order_last_30_days',
  'order_this_week',
  'order_this_month'
] as const

export type SoItemTimePresetId = (typeof SO_ITEM_TIME_PRESET_IDS)[number]

export const SO_ITEM_QUICK_FILTER_PRESET_IDS = [
  'pending_submit_audit',
  'pending_submit_purchase_req',
  'pending_submit_stock_out_notify',
  'applied_pending_po',
  'purchased_pending_stock_in',
  'notify_pending_packing',
  'packed_pending_stock_out',
  'in_stock_pending_out',
  'used_stocking',
  'stock_out_pending_receipt',
  'receipt_partial',
  'receipt_complete'
] as const

export type SoItemQuickFilterPresetId = (typeof SO_ITEM_QUICK_FILTER_PRESET_IDS)[number]

export type SoItemListPresetId = SoItemTimePresetId | SoItemQuickFilterPresetId

export const SO_ITEM_LIST_PRESET_IDS: readonly SoItemListPresetId[] = [
  ...SO_ITEM_TIME_PRESET_IDS,
  ...SO_ITEM_QUICK_FILTER_PRESET_IDS
]

export function isSoItemListPresetId(v: unknown): v is SoItemListPresetId {
  return typeof v === 'string' && (SO_ITEM_LIST_PRESET_IDS as readonly string[]).includes(v)
}

export function isSoItemTimePresetId(v: unknown): v is SoItemTimePresetId {
  return typeof v === 'string' && (SO_ITEM_TIME_PRESET_IDS as readonly string[]).includes(v)
}

export function isSoItemQuickFilterPresetId(v: unknown): v is SoItemQuickFilterPresetId {
  return typeof v === 'string' && (SO_ITEM_QUICK_FILTER_PRESET_IDS as readonly string[]).includes(v)
}

function formatYmd(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/** 与列表 orderCreateStart/End 一致：本地日历日 YYYY-MM-DD；本周以周一为一周起点 */
export function resolveSoItemTimePresetDateRange(preset: SoItemTimePresetId): [string, string] {
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
    // getDay: 0=周日 … 1=周一；距本周一天数 = (dow + 6) % 7
    const daysFromMonday = (today.getDay() + 6) % 7
    const start = new Date(today)
    start.setDate(start.getDate() - daysFromMonday)
    return [formatYmd(start), end]
  }
  // order_this_month
  const start = new Date(today.getFullYear(), today.getMonth(), 1)
  return [formatYmd(start), end]
}

export type SoItemPresetApiParams = {
  orderCreateStart?: string
  orderCreateEnd?: string
  quickFilter?: string
}

export function resolveSoItemPresetApiParams(preset: SoItemListPresetId): SoItemPresetApiParams {
  if (isSoItemTimePresetId(preset)) {
    const [orderCreateStart, orderCreateEnd] = resolveSoItemTimePresetDateRange(preset)
    return { orderCreateStart, orderCreateEnd }
  }
  return { quickFilter: preset }
}

/** keyword 区 query 键（可与 preset 叠加；取消 preset 时一并清除） */
export const SO_ITEM_KEYWORD_QUERY_KEYS = [
  'sellOrderCode',
  'customerName',
  'salesUserName',
  'purchaseUserAccount',
  'pn',
  'customerSo',
  'customerPn',
  'transactionCurrency'
] as const

export function pickSoItemKeywordQuery(query: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {}
  for (const key of SO_ITEM_KEYWORD_QUERY_KEYS) {
    const v = query[key]
    if (typeof v === 'string' && v.trim()) out[key] = v.trim()
  }
  return out
}

export function buildSoItemListRouteQuery(input: {
  preset?: SoItemListPresetId | null
  keywords?: Record<string, string>
  advanced?: {
    orderCreateStart?: string
    orderCreateEnd?: string
    purchaseProgressStatus?: string
    stockInProgressStatus?: string
    stockOutNotifyProgressStatus?: string
    stockOutProgressStatus?: string
    receiptProgressStatus?: string
    invoiceProgressStatus?: string
  }
}): Record<string, string> {
  const q: Record<string, string> = { ...(input.keywords ?? {}) }
  if (input.preset) {
    q.preset = input.preset
    const api = resolveSoItemPresetApiParams(input.preset)
    if (api.orderCreateStart) q.orderCreateStart = api.orderCreateStart
    if (api.orderCreateEnd) q.orderCreateEnd = api.orderCreateEnd
    if (api.quickFilter) q.quickFilter = api.quickFilter
    return q
  }
  const adv = input.advanced ?? {}
  if (adv.orderCreateStart) q.orderCreateStart = adv.orderCreateStart
  if (adv.orderCreateEnd) q.orderCreateEnd = adv.orderCreateEnd
  for (const k of [
    'purchaseProgressStatus',
    'stockInProgressStatus',
    'stockOutNotifyProgressStatus',
    'stockOutProgressStatus',
    'receiptProgressStatus',
    'invoiceProgressStatus'
  ] as const) {
    const v = adv[k]
    if (v !== undefined && v !== '') q[k] = v
  }
  return q
}

export function presetI18nKey(preset: SoItemListPresetId): string {
  return `salesOrderItemList.searchPanel.presets.${preset}`
}
