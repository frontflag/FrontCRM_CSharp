import { BIZ_BRAND_COUNTRY_RAW } from './bizBrandCountryData'

export type BizBrandCountryOption = {
  label: string
  /** 下拉展示 / 搜索用短码（如 TW） */
  code: string
  /** 写入 biz_brand.country_code 的实际值 */
  storageCode: string
  nameEn: string
  /** 用于 filterable 下拉本地搜索 */
  searchText: string
}

/** 下拉「其他」：手动输入不在列表中的国家名称 */
export const BIZ_BRAND_COUNTRY_OTHER = '__OTHER__'

/** ISO 短码 → 品牌库实际存储的国家代码 */
const STORAGE_CODE_OVERRIDES: Record<string, string> = {
  TW: 'TAIWAN,CHINA'
}

/** 国家中文名别名 → 标准 label（编辑回填、模糊匹配） */
const LABEL_ALIASES: Record<string, string> = {
  台湾: '中国台湾',
  台湾省: '中国台湾',
  中国大陆: '中国',
  中华人民共和国: '中国',
  香港特别行政区: '香港',
  澳门特别行政区: '澳门',
  美利坚合众国: '美国'
}

function normalizeKey(value: string): string {
  return value.trim().toLowerCase()
}

function buildSearchText(
  label: string,
  code: string,
  storageCode: string,
  nameEn: string,
  aliases: string[]
): string {
  return [label, code, storageCode, nameEn, ...aliases].join(' ').toLowerCase()
}

function rawToOption(row: (typeof BIZ_BRAND_COUNTRY_RAW)[number]): BizBrandCountryOption {
  const [label, code, nameEn, ...aliases] = row
  const storageCode = STORAGE_CODE_OVERRIDES[code] ?? code
  return {
    label,
    code,
    storageCode,
    nameEn,
    searchText: buildSearchText(label, code, storageCode, nameEn, aliases)
  }
}

export const BIZ_BRAND_COUNTRY_OPTIONS: readonly BizBrandCountryOption[] =
  BIZ_BRAND_COUNTRY_RAW.map(rawToOption)

const byLabel = new Map<string, BizBrandCountryOption>()
const byCode = new Map<string, BizBrandCountryOption>()

for (const opt of BIZ_BRAND_COUNTRY_OPTIONS) {
  byLabel.set(normalizeKey(opt.label), opt)
  byCode.set(normalizeKey(opt.code), opt)
  if (opt.storageCode !== opt.code) {
    byCode.set(normalizeKey(opt.storageCode), opt)
  }
}

for (const [alias, canonical] of Object.entries(LABEL_ALIASES)) {
  const target = byLabel.get(normalizeKey(canonical))
  if (target) byLabel.set(normalizeKey(alias), target)
}

/** 下拉展示：美国 (US) */
export function bizBrandCountryOptionLabel(opt: BizBrandCountryOption): string {
  return `${opt.label} (${opt.code})`
}

export function findBizBrandCountryByLabel(countryName?: string | null): BizBrandCountryOption | null {
  const key = normalizeKey(String(countryName ?? ''))
  if (!key) return null
  return byLabel.get(key) ?? null
}

export function findBizBrandCountryByCode(code?: string | null): BizBrandCountryOption | null {
  const key = normalizeKey(String(code ?? ''))
  if (!key) return null
  return byCode.get(key) ?? null
}

/** 按中文名 / 英文名 / ISO 代码解析国家码 */
export function resolveBizBrandCountryCode(countryName?: string | null): string | null {
  const raw = String(countryName ?? '').trim()
  if (!raw) return null

  const byLabelHit = findBizBrandCountryByLabel(raw)
  if (byLabelHit) return byLabelHit.storageCode

  const byCodeHit = findBizBrandCountryByCode(raw)
  if (byCodeHit) return byCodeHit.storageCode

  const lower = raw.toLowerCase()
  for (const opt of BIZ_BRAND_COUNTRY_OPTIONS) {
    if (opt.searchText.includes(lower)) return opt.storageCode
  }
  return null
}

/** 编辑回填：国家名称 → 下拉值 + 其他输入 */
export function bizBrandCountryToSelect(countryName?: string | null): {
  select: string
  other: string
} {
  const name = String(countryName ?? '').trim()
  if (!name) return { select: '', other: '' }

  const found = findBizBrandCountryByLabel(name)
  if (found) return { select: found.label, other: '' }

  const byCode = findBizBrandCountryByCode(name)
  if (byCode) return { select: byCode.label, other: '' }

  return { select: BIZ_BRAND_COUNTRY_OTHER, other: name }
}

/** 下拉选中值 → 实际保存的国家名称 */
export function resolveBizBrandCountryName(countrySelect: string, countryOther: string): string {
  if (!countrySelect) return ''
  if (countrySelect === BIZ_BRAND_COUNTRY_OTHER) return countryOther.trim()
  return countrySelect.trim()
}

/** el-select filter-method：按关键字过滤 */
export function filterBizBrandCountryOptions(
  query: string,
  options: readonly BizBrandCountryOption[] = BIZ_BRAND_COUNTRY_OPTIONS
): BizBrandCountryOption[] {
  const q = query.trim().toLowerCase()
  if (!q) return [...options]
  return options.filter((opt) => opt.searchText.includes(q))
}

/** 是否应保留已有国家代码（与自动解析不一致时视为用户/历史手工值） */
export function shouldPreserveBizBrandCountryCode(
  countryName: string,
  existingCode: string
): boolean {
  const code = existingCode.trim()
  if (!code) return false
  const resolved = resolveBizBrandCountryCode(countryName)
  if (!resolved) return true
  return code.toUpperCase() !== resolved.toUpperCase()
}
