import {
  alternativeRows,
  breakdownRows,
  formatScalar,
  isBreakdownArray,
  isEmptyValue,
  isObjectArray,
  isPlainObject,
  isPriceTiersArray,
  isScalar,
  isStringArray,
  isUrlField,
  priceTierRows,
  type JsonRenderMode
} from '@/utils/jsonDisplay'

export type JsonEnhancerContext = {
  fieldKey: string
  path: string
  value: unknown
}

export type EnhancerColumnDef = {
  prop: string
  labelKey: string
  width?: number
  minWidth?: number
}

export type JsonEnhancerDef = {
  id: string
  priority: number
  match: (ctx: JsonEnhancerContext) => boolean
  mode: JsonRenderMode
  columns?: EnhancerColumnDef[]
  tableData?: (value: unknown) => Record<string, unknown>[]
  /** labeled-rows 字段显示顺序 */
  rowOrder?: string[]
  /** object-list 每项标题优先取的字段名 */
  itemTitleKeys?: string[]
  /** object-list 卡片内不再重复展示的字段（默认与 itemTitleKeys 相同） */
  omitItemFieldKeys?: string[]
}

function hasNestedComplexValues(rows: Record<string, unknown>[]): boolean {
  return rows.some((row) =>
    Object.values(row).some(
      (v) => isPlainObject(v) || (Array.isArray(v) && v.length > 0 && !isStringArray(v))
    )
  )
}

/** 已注册的 Enhancer（仅优化展示；不匹配时仍走通用渲染，不会隐藏数据） */
const ENHANCER_DEFS: JsonEnhancerDef[] = [
  {
    id: 'part_number_breakdown',
    priority: 100,
    match: (ctx) => ctx.fieldKey === 'part_number_breakdown' || isBreakdownArray(ctx.value),
    mode: 'breakdown-table',
    tableData: (value) => breakdownRows(value) as Record<string, unknown>[],
    columns: [
      { prop: 'segment', labelKey: 'segment', width: 100 },
      { prop: 'meaning', labelKey: 'meaning', minWidth: 200 }
    ]
  },
  {
    id: 'price_tiers',
    priority: 100,
    match: (ctx) => ctx.fieldKey === 'price_tiers' || isPriceTiersArray(ctx.value),
    mode: 'price-tiers-table',
    tableData: (value) => priceTierRows(value) as Record<string, unknown>[],
    columns: [
      { prop: 'quantity', labelKey: 'quantity', minWidth: 220 },
      { prop: 'unitPrice', labelKey: 'unitPrice', minWidth: 140 }
    ]
  },
  {
    id: 'market_conditions_rows',
    priority: 91,
    match: (ctx) => ctx.fieldKey === 'market_conditions' && isPlainObject(ctx.value),
    mode: 'labeled-rows',
    rowOrder: ['availability', 'trend', 'note']
  },
  {
    id: 'market_price_rows',
    priority: 91,
    match: (ctx) => ctx.fieldKey === 'market_price' && isPlainObject(ctx.value),
    mode: 'labeled-rows',
    rowOrder: ['reference_price', 'currency', 'note']
  },
  {
    id: 'industry_news_list',
    priority: 88,
    match: (ctx) => ctx.fieldKey === 'industry_news' && isObjectArray(ctx.value),
    mode: 'industry-news-list'
  },
  {
    id: 'alternatives_list',
    priority: 85,
    match: (ctx) => ctx.fieldKey === 'alternatives' && isObjectArray(ctx.value),
    mode: 'alternatives-table',
    tableData: (value) => alternativeRows(value) as Record<string, unknown>[],
    columns: [
      { prop: 'partNumber', labelKey: 'altPartNumber', minWidth: 160 },
      { prop: 'brand', labelKey: 'altBrand', minWidth: 120 },
      { prop: 'note', labelKey: 'altNote', minWidth: 240 }
    ]
  },
  {
    id: 'pricing_distributors',
    priority: 82,
    match: (ctx) => ctx.fieldKey === 'distributors' && isObjectArray(ctx.value),
    mode: 'object-list',
    itemTitleKeys: ['distributor', 'vendor', 'name'],
    omitItemFieldKeys: ['distributor', 'vendor', 'source', 'name']
  },
  {
    id: 'url_link',
    priority: 90,
    match: (ctx) => isUrlField(ctx.fieldKey, ctx.value),
    mode: 'url'
  },
  {
    id: 'pricing_distributor_list_legacy',
    priority: 78,
    match: (ctx) => ctx.fieldKey === 'pricing' && isObjectArray(ctx.value),
    mode: 'object-list',
    itemTitleKeys: ['distributor', 'source', 'vendor', 'name'],
    omitItemFieldKeys: ['distributor', 'source', 'vendor', 'name']
  },
  {
    id: 'alternatives_nested_list_legacy',
    priority: 72,
    match: (ctx) =>
      ctx.fieldKey === 'alternatives' && isObjectArray(ctx.value) && hasNestedComplexValues(ctx.value),
    mode: 'object-list',
    itemTitleKeys: ['part_number', 'pn', 'model', 'name', 'brand']
  },
  {
    id: 'nested_object_list',
    priority: 20,
    match: (ctx) =>
      ctx.fieldKey !== 'pricing' &&
      ctx.fieldKey !== 'alternatives' &&
      isObjectArray(ctx.value) &&
      hasNestedComplexValues(ctx.value),
    mode: 'object-list',
    itemTitleKeys: ['distributor', 'source', 'vendor', 'part_number', 'pn', 'name', 'brand', 'title']
  }
]

export const MATERIAL_INTEL_JSON_ENHANCERS: JsonEnhancerDef[] = [...ENHANCER_DEFS].sort(
  (a, b) => b.priority - a.priority
)

export type ResolvedJsonRender = {
  mode: JsonRenderMode
  enhancer: JsonEnhancerDef | null
}

/** 先匹配 Enhancer，否则走通用基础模式（保证完整显示） */
export function resolveJsonRender(ctx: JsonEnhancerContext): ResolvedJsonRender {
  if (isEmptyValue(ctx.value)) return { mode: 'skip', enhancer: null }

  for (const enhancer of MATERIAL_INTEL_JSON_ENHANCERS) {
    if (enhancer.match(ctx)) return { mode: enhancer.mode, enhancer }
  }

  return { mode: detectBaseRenderMode(ctx.fieldKey, ctx.value), enhancer: null }
}

function detectBaseRenderMode(_fieldKey: string, value: unknown): JsonRenderMode {
  if (isScalar(value)) return 'scalar'
  if (isStringArray(value)) return 'string-list'
  if (isObjectArray(value)) return 'object-table'
  if (isPlainObject(value)) return 'object'
  if (Array.isArray(value)) return 'mixed-list'
  return 'scalar'
}

export function objectListItemTitle(
  item: Record<string, unknown>,
  idx: number,
  enhancer: JsonEnhancerDef | null,
  fallbackLabel: string
): string {
  const keys = enhancer?.itemTitleKeys ?? [
    'distributor',
    'source',
    'vendor',
    'part_number',
    'pn',
    'name',
    'brand',
    'title'
  ]
  for (const key of keys) {
    const text = formatScalar(item[key])
    if (text) return text
  }
  return `${fallbackLabel} ${idx + 1}`
}
