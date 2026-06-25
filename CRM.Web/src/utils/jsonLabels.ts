import { humanizeKey, snakeToCamel } from '@/utils/jsonDisplay'

/** 顶层 JSON 键 → materialIntel.sections 的 i18n 后缀（仅影响标题文案） */
export const ROOT_SECTION_I18N: Record<string, string> = {
  brand_info: 'brand',
  spec_params: 'spec',
  application_areas: 'application',
  alternatives: 'alternatives',
  pricing: 'pricing',
  industry_news: 'news'
}

/** 顶层面板显示顺序：alternatives 在 pricing 前，pricing 始终最后 */
export const ROOT_SECTION_ORDER = [
  'brand_info',
  'spec_params',
  'application_areas',
  'industry_news',
  'alternatives',
  'pricing'
] as const

/** 无 i18n 键时的中文标题兜底（物料情报常见 AI 字段） */
export const FIELD_LABEL_ZH: Record<string, string> = {
  name: '名称',
  english_name: '英文名称',
  abbreviation: '缩写',
  description: '描述',
  memory_type: '存储类型',
  memory_capacity: '存储容量',
  voltage_supply: '供电电压',
  bus_width: '总线宽度',
  page_program_time_typ: '页编程时间（典型）',
  block_erase_time_typ: '块擦除时间（典型）',
  read_current_max: '最大读取电流',
  standby_current_max: '最大待机电流',
  program_erase_cycles: '擦写次数',
  data_retention_period: '数据保持时间',
  ecc_support: 'ECC 支持',
  bad_block_management: '坏块管理',
  security_features: '安全特性',
  rohs_compliance: 'RoHS 合规',
  price_notes: '价格说明',
  availability: '库存状态',
  reference_price: '参考价',
  market_price: '市场价格',
  market_conditions: '市场行情',
  status: '状态',
  note: '说明',
  notes: '备注',
  summary: '摘要',
  message: '提示',
  title: '标题',
  headline: '标题',
  url: '链接',
  link: '链接',
  source: '来源',
  vendor: '供应商',
  item: '条目',
  electrical_params: '电气参数',
  technical_features: '技术特点',
  protection_features: '保护功能',
  part_number_breakdown: '型号解析',
  application_areas: '应用领域',
  industry_news: '市场新闻与行业动态',
  alternatives: '可替代料',
  pricing: '价格',
  brand_info: '品牌信息',
  spec_params: '规格参数',
  price_tiers: '阶梯价格',
  distributors: '渠道报价',
  trend: '走势',
  product_line: '产品线',
  product_category: '产品品类',
  series: '系列',
  datasheet_url: '规格书',
  image_url: '物料图片'
}

type LabelResolver = (key: string) => string

function isZhLocale(locale: string | undefined): boolean {
  return (locale ?? '').toLowerCase().startsWith('zh')
}

function fallbackLabel(key: string, locale: string | undefined): string {
  if (isZhLocale(locale) && FIELD_LABEL_ZH[key]) return FIELD_LABEL_ZH[key]
  return humanizeKey(key)
}

export function sortRootSectionEntries(
  entries: { key: string; value: unknown }[]
): { key: string; value: unknown }[] {
  const rank = new Map<string, number>(ROOT_SECTION_ORDER.map((k, i) => [k, i]))
  return [...entries].sort((a, b) => {
    const ra = rank.get(a.key) ?? 999
    const rb = rank.get(b.key) ?? 999
    if (ra !== rb) return ra - rb
    return a.key.localeCompare(b.key)
  })
}

/** 字段标签：i18n 优先，中文环境兜底 FIELD_LABEL_ZH（不参与是否显示） */
export function resolveFieldLabel(
  key: string,
  t: LabelResolver,
  te: (key: string) => boolean,
  locale?: string
): string {
  if (!key) return ''
  const camel = snakeToCamel(key)
  const i18nKey = `materialIntel.fields.${camel}`
  if (te(i18nKey)) return t(i18nKey)
  return fallbackLabel(key, locale)
}

/** 顶层面板标题 */
export function resolveSectionLabel(
  key: string,
  t: LabelResolver,
  te: (key: string) => boolean,
  locale?: string
): string {
  const sectionSuffix = ROOT_SECTION_I18N[key]
  if (sectionSuffix) {
    const i18nKey = `materialIntel.sections.${sectionSuffix}`
    if (te(i18nKey)) return t(i18nKey)
  }
  return resolveFieldLabel(key, t, te, locale)
}

/** Enhancer 表格列标题 */
export function resolveColumnLabel(
  labelKey: string,
  t: LabelResolver,
  te: (key: string) => boolean,
  locale?: string
): string {
  const i18nKey = `materialIntel.fields.${labelKey}`
  if (te(i18nKey)) return t(i18nKey)
  return fallbackLabel(labelKey, locale)
}

/** URL 链接展示文案 */
export function resolveUrlLinkText(
  fieldKey: string,
  value: unknown,
  t: LabelResolver,
  te: (key: string) => boolean,
  locale?: string
): string {
  const k = fieldKey.toLowerCase()
  if (k.includes('datasheet') && te('materialIntel.fields.datasheet')) return t('materialIntel.fields.datasheet')
  if (k.includes('image') && te('materialIntel.fields.image')) return t('materialIntel.fields.image')
  const raw = value == null ? '' : String(value).trim()
  return raw || resolveFieldLabel(fieldKey, t, te, locale)
}
