/** 客户地址：国家/地区（方案 A — 中国级联 / 海外自由文本） */

export const CUSTOMER_ADDRESS_COUNTRY_CHINA = '中国'

/** 下拉「其他」时手动填写国家名称 */
export const CUSTOMER_ADDRESS_COUNTRY_OTHER = '__OTHER__'

/** 后端 customeraddress.Country */
export const CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE = 1
export const CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE = 2

/** 常用海外国家/地区预设 */
export const CUSTOMER_ADDRESS_OVERSEAS_PRESETS = [
  '美国',
  '日本',
  '德国',
  '新加坡',
  '韩国',
  '英国',
  '法国',
  '荷兰',
  '意大利',
  '马来西亚',
  '印度',
  '越南',
  '泰国'
] as const

export function isCustomerAddressDomestic(
  countryName?: string | null,
  countryCode?: number | null
): boolean {
  if (countryCode === CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE) return false
  if (countryCode === CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE) return true
  const name = String(countryName ?? '').trim()
  if (!name) return true
  return name === CUSTOMER_ADDRESS_COUNTRY_CHINA
}

/** 解析下拉值 → 实际国家名称 */
export function resolveCustomerAddressCountryName(
  countrySelect: string,
  countryOther: string
): string {
  if (countrySelect === CUSTOMER_ADDRESS_COUNTRY_CHINA) return CUSTOMER_ADDRESS_COUNTRY_CHINA
  if (countrySelect === CUSTOMER_ADDRESS_COUNTRY_OTHER) return countryOther.trim()
  return countrySelect.trim()
}

/** 编辑回填：国家名称 → 下拉值 + 其他输入 */
export function customerAddressCountryToSelect(countryName?: string | null): {
  countrySelect: string
  countryOther: string
} {
  const name = String(countryName ?? '').trim()
  if (!name || name === CUSTOMER_ADDRESS_COUNTRY_CHINA) {
    return { countrySelect: CUSTOMER_ADDRESS_COUNTRY_CHINA, countryOther: '' }
  }
  if ((CUSTOMER_ADDRESS_OVERSEAS_PRESETS as readonly string[]).includes(name)) {
    return { countrySelect: name, countryOther: '' }
  }
  return { countrySelect: CUSTOMER_ADDRESS_COUNTRY_OTHER, countryOther: name }
}

const CHINA_CASCADER_COUNTRY_ALIASES: Record<string, string> = {
  香港: '香港',
  'Hong Kong': '香港',
  HK: '香港',
  台湾: '台湾',
  Taiwan: '台湾',
  TW: '台湾',
  澳门: '澳门',
  Macau: '澳门',
  MO: '澳门'
}

/** 港澳台与国内地址均走「中国」+ 省市区级联 */
export function usesChinaRegionCascader(countryName?: string | null, province?: string | null): boolean {
  const c = String(countryName ?? '').trim()
  if (!c || c === CUSTOMER_ADDRESS_COUNTRY_CHINA) return true
  if (c in CHINA_CASCADER_COUNTRY_ALIASES || ['香港', '澳门', '台湾'].includes(c)) return true
  const p = String(province ?? '').trim()
  return ['香港', '台湾', '澳门'].includes(p)
}

/** 将港澳台等国家名归一为「中国」，并把地区名写入 province（供级联） */
export function normalizeAddressChinaCascaderCountry(
  country: string,
  province: string
): { country: string; province: string } {
  const c = country.trim()
  const p = province.trim()
  if (!c || c === CUSTOMER_ADDRESS_COUNTRY_CHINA) {
    return { country: CUSTOMER_ADDRESS_COUNTRY_CHINA, province: p }
  }
  const mapped = CHINA_CASCADER_COUNTRY_ALIASES[c]
  if (mapped) {
    return { country: CUSTOMER_ADDRESS_COUNTRY_CHINA, province: p || mapped }
  }
  if (['香港', '澳门', '台湾'].includes(c)) {
    return { country: CUSTOMER_ADDRESS_COUNTRY_CHINA, province: p || c }
  }
  return { country: c, province: p }
}
