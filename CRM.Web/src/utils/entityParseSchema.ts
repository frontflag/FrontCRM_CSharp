/** AI entity.parse.* 输出契约与表单映射 */

import { enrichCustomerRegionFields } from '@/constants/region'
import {
  CUSTOMER_ADDRESS_COUNTRY_CHINA,
  CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE,
  CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE,
  normalizeAddressChinaCascaderCountry,
  usesChinaRegionCascader
} from '@/constants/customerAddress'
import {
  normalizeVendorAddressType,
  VENDOR_ADDRESS_COUNTRY_CHINA,
  VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE,
  VENDOR_ADDRESS_COUNTRY_OVERSEAS_CODE,
  vendorAddressCountryCode
} from '@/constants/vendorAddress'

export type ParsedCustomerFields = {
  customerName: string
  customerShortName: string
  englishOfficialName: string
  customerType: number | null
  customerLevel: string
  industry: string
  country: string
  province: string
  city: string
  district: string
  address: string
  unifiedSocialCreditCode: string
  creditLimit: number | null
  paymentTerms: number | null
  currency: number | null
  taxRate: number | null
  invoiceType: number | null
  companyInfo: string
  remarks: string
}

export type ParsedRfqItemFields = {
  customerMpn: string
  customerBrand: string
  mpn: string
  brand: string
  targetPrice: number | null
  priceCurrency: number | null
  quantity: number | null
  productionDate: string
  expiryDate: string
  minPackageQty: number | null
  minOrderQty: number | null
  alternativeMaterials: string
  remark: string
}

export type ParsedRfqFields = {
  customerName: string
  customerId: string
  contactEmail: string
  industry: string
  product: string
  rfqType: number | null
  targetType: number | null
  quoteMethod: number | null
  assignMethod: number | null
  importance: number | null
  projectBackground: string
  competitor: string
  remark: string
  items: ParsedRfqItemFields[]
}

export type ParsedVendorFields = {
  officialName: string
  englishOfficialName: string
  nickName: string
  industry: string
  level: number | null
  credit: number | null
  officeAddress: string
  website: string
  currency: number | null
  paymentMethod: string
  paymentDays: number | null
  taxNumber: string
  companyInfo: string
  remark: string
}

export type ParsedCustomerContactFields = {
  cName: string
  eName: string
  gender: number
  department: string
  position: string
  mobilePhone: string
  phone: string
  email: string
  fax: string
  socialAccount: string
  isDefault: boolean
  isDecisionMaker: boolean
  remarks: string
}

export type ParsedVendorContactFields = {
  cName: string
  eName: string
  gender: number
  title: string
  department: string
  mobile: string
  tel: string
  email: string
  isMain: boolean
  remark: string
}

export type ParsedCustomerAddressFields = {
  addressType: string
  country: string
  countryCode: number
  isDomestic: boolean
  province: string
  city: string
  district: string
  streetAddress: string
  companyName: string
  zipCode: string
  contactPerson: string
  contactPhone: string
  isDefault: boolean
}

export type ParsedVendorAddressFields = {
  addressType: number
  countryName: string
  country: number
  province: string
  city: string
  area: string
  address: string
  contactName: string
  contactPhone: string
  isDefault: boolean
  remark: string
}

function str(v: unknown): string {
  if (v == null) return ''
  return String(v).trim()
}

function numOrNull(v: unknown): number | null {
  if (v == null || v === '') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

/** 目标价币别：1=RMB 2=USD 3=EUR 4=HKD；支持 AI 返回数字或 ISO/中文别名 */
export function mapPriceCurrency(v: unknown): number | null {
  if (v == null || v === '') return null
  if (typeof v === 'number' && Number.isFinite(v)) {
    const n = Math.round(v)
    if (n >= 1 && n <= 4) return n
    return null
  }
  const s = String(v).trim().toUpperCase()
  if (!s) return null
  if (s === '1' || s.includes('RMB') || s.includes('CNY') || s.includes('人民币') || s === '￥' || s === '¥') {
    return 1
  }
  if (s === '2' || s.includes('USD') || s.includes('美元') || s === '$') return 2
  if (s === '3' || s.includes('EUR') || s.includes('欧元')) return 3
  if (s === '4' || s.includes('HKD') || s.includes('港币') || s.includes('港元')) return 4
  const n = Number(v)
  if (Number.isFinite(n)) {
    const r = Math.round(n)
    if (r >= 1 && r <= 4) return r
  }
  return null
}

function normalizeCustomerLevel(v: unknown): string {
  const s = str(v).toUpperCase()
  if (['D', 'C', 'B', 'BPO', 'VIP', 'VPO'].includes(s)) return s
  return ''
}

const VALID_LEVELS = new Set(['D', 'C', 'B', 'BPO', 'VIP', 'VPO'])

export function emptyParsedCustomer(): ParsedCustomerFields {
  return {
    customerName: '',
    customerShortName: '',
    englishOfficialName: '',
    customerType: null,
    customerLevel: '',
    industry: '',
    country: '',
    province: '',
    city: '',
    district: '',
    address: '',
    unifiedSocialCreditCode: '',
    creditLimit: null,
    paymentTerms: null,
    currency: null,
    taxRate: null,
    invoiceType: null,
    companyInfo: '',
    remarks: ''
  }
}

export function emptyParsedRfqItem(): ParsedRfqItemFields {
  return {
    customerMpn: '',
    customerBrand: '',
    mpn: '',
    brand: '',
    targetPrice: null,
    priceCurrency: 1,
    quantity: 1,
    productionDate: '',
    expiryDate: '',
    minPackageQty: null,
    minOrderQty: null,
    alternativeMaterials: '',
    remark: ''
  }
}

export function emptyParsedRfq(): ParsedRfqFields {
  return {
    customerName: '',
    customerId: '',
    contactEmail: '',
    industry: '',
    product: '',
    rfqType: null,
    targetType: null,
    quoteMethod: null,
    assignMethod: null,
    importance: null,
    projectBackground: '',
    competitor: '',
    remark: '',
    items: [emptyParsedRfqItem()]
  }
}

export function emptyParsedVendor(): ParsedVendorFields {
  return {
    officialName: '',
    englishOfficialName: '',
    nickName: '',
    industry: '',
    level: null,
    credit: null,
    officeAddress: '',
    website: '',
    currency: 1,
    paymentMethod: '',
    paymentDays: null,
    taxNumber: '',
    companyInfo: '',
    remark: ''
  }
}

export function emptyParsedCustomerContact(): ParsedCustomerContactFields {
  return {
    cName: '',
    eName: '',
    gender: 0,
    department: '',
    position: '',
    mobilePhone: '',
    phone: '',
    email: '',
    fax: '',
    socialAccount: '',
    isDefault: false,
    isDecisionMaker: false,
    remarks: ''
  }
}

export function emptyParsedVendorContact(): ParsedVendorContactFields {
  return {
    cName: '',
    eName: '',
    gender: 1,
    title: '',
    department: '',
    mobile: '',
    tel: '',
    email: '',
    isMain: false,
    remark: ''
  }
}

export function emptyParsedCustomerAddress(): ParsedCustomerAddressFields {
  return {
    addressType: 'Office',
    country: CUSTOMER_ADDRESS_COUNTRY_CHINA,
    countryCode: CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE,
    isDomestic: true,
    province: '',
    city: '',
    district: '',
    streetAddress: '',
    companyName: '',
    zipCode: '',
    contactPerson: '',
    contactPhone: '',
    isDefault: false
  }
}

export function emptyParsedVendorAddress(): ParsedVendorAddressFields {
  return {
    addressType: 1,
    countryName: VENDOR_ADDRESS_COUNTRY_CHINA,
    country: VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE,
    province: '',
    city: '',
    area: '',
    address: '',
    contactName: '',
    contactPhone: '',
    isDefault: false,
    remark: ''
  }
}

function normalizeAddressType(v: unknown): string {
  const raw = str(v)
  if (!raw) return 'Office'
  const lower = raw.toLowerCase()
  const map: Record<string, string> = {
    office: 'Office',
    billing: 'Billing',
    shipping: 'Shipping',
    registered: 'Registered'
  }
  if (map[lower]) return map[lower]
  if (['Office', 'Billing', 'Shipping', 'Registered'].includes(raw)) return raw
  if (raw.includes('办公')) return 'Office'
  if (raw.includes('账单')) return 'Billing'
  if (raw.includes('收货') || raw.includes('送货')) return 'Shipping'
  if (raw.includes('注册')) return 'Registered'
  return 'Office'
}

function normalizeContactGender(v: unknown): number {
  const n = numOrNull(v)
  if (n == null) return 0
  const r = Math.round(n)
  return r === 1 || r === 2 ? r : 0
}

function defaultBusinessCardContactGender(v: unknown): number {
  const g = normalizeContactGender(v)
  return g === 1 || g === 2 ? g : 1
}

const SHORT_NAME_SUFFIXES = [
  '股份有限公司',
  '有限责任公司',
  '有限公司',
  '集团公司',
  '集团',
  '公司'
]

/** 从全称推断简称；若已有简称则原样返回 */
export function inferEntityShortName(fullName: string, existingShort = ''): string {
  const short = existingShort.trim()
  if (short) return short
  let name = fullName.trim()
  if (!name) return ''
  for (const suffix of SHORT_NAME_SUFFIXES) {
    if (name.endsWith(suffix) && name.length > suffix.length) {
      name = name.slice(0, -suffix.length).trim()
      break
    }
  }
  name = name.replace(/,?\s*Inc\.?$/i, '').trim()
  name = name.replace(/\s+Ltd\.?$/i, '').trim()
  name = name.replace(/\s+LLC\.?$/i, '').trim()
  name = name.replace(/\s+Co\.?,?\s*Ltd\.?$/i, '').trim()
  return name
}

function boolOrFalse(v: unknown): boolean {
  if (v === true || v === 1 || v === '1') return true
  if (typeof v === 'string') {
    const u = v.trim().toLowerCase()
    if (u === 'true' || u === 'yes' || u === '是') return true
  }
  return false
}

function normalizeVendorLevel(v: unknown): number | null {
  const n = numOrNull(v)
  if (n == null) return null
  const r = Math.round(n)
  return r >= 1 && r <= 13 ? r : null
}

function normalizeVendorCredit(v: unknown): number | null {
  const n = numOrNull(v)
  if (n == null) return null
  const r = Math.round(n)
  return r >= 1 && r <= 10 ? r : null
}

export function normalizeCustomerContactParseResult(raw: Record<string, unknown>): ParsedCustomerContactFields {
  const cName = str(raw.c_name ?? raw.cName ?? raw.contact_name ?? raw.contactName ?? raw.name)
  const eName = str(raw.e_name ?? raw.eName ?? raw.english_name)
  return {
    cName,
    eName,
    gender: normalizeContactGender(raw.gender ?? raw.sex),
    department: str(raw.department),
    position: str(raw.position ?? raw.title ?? raw.job_title),
    mobilePhone: str(raw.mobile_phone ?? raw.mobile ?? raw.cellphone),
    phone: str(raw.phone ?? raw.landline ?? raw.tel),
    email: str(raw.email ?? raw.mail),
    fax: str(raw.fax),
    socialAccount: str(raw.social_account ?? raw.qq ?? raw.wechat ?? raw.weixin),
    isDefault: boolOrFalse(raw.is_default ?? raw.default),
    isDecisionMaker: boolOrFalse(raw.is_decision_maker ?? raw.decision_maker),
    remarks: str(raw.remarks ?? raw.remark ?? raw.notes)
  }
}

/** 客户联系人 AI 预填 → CustomerContactEdit formData */
export function customerContactPrefillToFormPayload(parsed: ParsedCustomerContactFields): Record<string, unknown> {
  return {
    cName: parsed.cName || undefined,
    eName: parsed.eName || undefined,
    gender: parsed.gender,
    department: parsed.department || undefined,
    position: parsed.position || undefined,
    mobilePhone: parsed.mobilePhone || undefined,
    phone: parsed.phone || undefined,
    email: parsed.email || undefined,
    fax: parsed.fax || undefined,
    socialAccount: parsed.socialAccount || undefined,
    isDefault: parsed.isDefault,
    isDecisionMaker: parsed.isDecisionMaker,
    remarks: parsed.remarks || undefined
  }
}

export function normalizeCustomerAddressParseResult(raw: Record<string, unknown>): ParsedCustomerAddressFields {
  let country = str(raw.country ?? raw.country_name)
  let province = str(raw.province ?? raw.state)
  const city = str(raw.city)
  const district = str(raw.district ?? raw.area)
  const streetAddress = str(raw.street_address ?? raw.address ?? raw.detail_address)

  const domestic = usesChinaRegionCascader(country, province)
  if (domestic) {
    const normalized = normalizeAddressChinaCascaderCountry(country, province)
    country = normalized.country
    province = normalized.province
    const enriched = enrichCustomerRegionFields({
      country,
      province,
      city,
      district,
      address: streetAddress
    })
    return {
      addressType: normalizeAddressType(raw.address_type ?? raw.type),
      country: enriched.country || CUSTOMER_ADDRESS_COUNTRY_CHINA,
      countryCode: CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE,
      isDomestic: true,
      province: enriched.province,
      city: enriched.city,
      district: enriched.district,
      streetAddress: streetAddress || enriched.address,
      companyName: str(raw.company_name),
      zipCode: str(raw.zip_code ?? raw.postal_code),
      contactPerson: str(raw.contact_person ?? raw.contact_name),
      contactPhone: str(raw.contact_phone ?? raw.phone),
      isDefault: boolOrFalse(raw.is_default ?? raw.default)
    }
  }

  return {
    addressType: normalizeAddressType(raw.address_type ?? raw.type),
    country,
    countryCode: CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE,
    isDomestic: false,
    province,
    city,
    district: '',
    streetAddress,
    companyName: str(raw.company_name),
    zipCode: str(raw.zip_code ?? raw.postal_code),
    contactPerson: str(raw.contact_person ?? raw.contact_name),
    contactPhone: str(raw.contact_phone ?? raw.phone),
    isDefault: boolOrFalse(raw.is_default ?? raw.default)
  }
}

/** 客户地址 AI 预填 → CustomerAddressEdit formData */
export function customerAddressPrefillToFormPayload(parsed: ParsedCustomerAddressFields): Record<string, unknown> {
  const domestic = usesChinaRegionCascader(parsed.country, parsed.province)
  const countryCode = domestic
    ? CUSTOMER_ADDRESS_COUNTRY_DOMESTIC_CODE
    : CUSTOMER_ADDRESS_COUNTRY_OVERSEAS_CODE
  return {
    addressType: parsed.addressType,
    country: parsed.country,
    countryCode,
    province: parsed.province || undefined,
    city: parsed.city || undefined,
    district: parsed.district || undefined,
    streetAddress: parsed.streetAddress || undefined,
    companyName: parsed.companyName || undefined,
    zipCode: parsed.zipCode || undefined,
    contactPerson: parsed.contactPerson || undefined,
    contactPhone: parsed.contactPhone || undefined,
    isDefault: parsed.isDefault
  }
}

export function normalizeVendorAddressParseResult(raw: Record<string, unknown>): ParsedVendorAddressFields {
  let countryName = str(raw.country ?? raw.country_name)
  let province = str(raw.province ?? raw.state)
  const city = str(raw.city)
  const area = str(raw.area ?? raw.district)
  const address = str(raw.address ?? raw.street_address ?? raw.detail_address)

  const domestic = usesChinaRegionCascader(countryName, province)
  if (domestic) {
    const normalized = normalizeAddressChinaCascaderCountry(countryName, province)
    countryName = normalized.country
    province = normalized.province
    const enriched = enrichCustomerRegionFields({
      country: countryName,
      province,
      city,
      district: area,
      address
    })
    return {
      addressType: normalizeVendorAddressType(raw.address_type ?? raw.type),
      countryName: enriched.country || VENDOR_ADDRESS_COUNTRY_CHINA,
      country: VENDOR_ADDRESS_COUNTRY_DOMESTIC_CODE,
      province: enriched.province,
      city: enriched.city,
      area: enriched.district,
      address: address || enriched.address,
      contactName: str(raw.contact_name ?? raw.contact_person),
      contactPhone: str(raw.contact_phone ?? raw.phone),
      isDefault: boolOrFalse(raw.is_default ?? raw.default),
      remark: str(raw.remark ?? raw.remarks)
    }
  }

  return {
    addressType: normalizeVendorAddressType(raw.address_type ?? raw.type),
    countryName,
    country: VENDOR_ADDRESS_COUNTRY_OVERSEAS_CODE,
    province: province || countryName,
    city,
    area: '',
    address,
    contactName: str(raw.contact_name ?? raw.contact_person),
    contactPhone: str(raw.contact_phone ?? raw.phone),
    isDefault: boolOrFalse(raw.is_default ?? raw.default),
    remark: str(raw.remark ?? raw.remarks)
  }
}

/** 供应商地址 AI 预填 → VendorAddressEdit formData */
export function vendorAddressPrefillToFormPayload(parsed: ParsedVendorAddressFields): Record<string, unknown> {
  const country = parsed.countryName
  const countryCode = vendorAddressCountryCode(country, parsed.province)
  return {
    addressType: parsed.addressType,
    countryName: country,
    country: countryCode,
    province: parsed.province || undefined,
    city: parsed.city || undefined,
    area: parsed.area || undefined,
    address: parsed.address || undefined,
    contactName: parsed.contactName || undefined,
    contactPhone: parsed.contactPhone || undefined,
    isDefault: parsed.isDefault,
    remark: parsed.remark || undefined
  }
}

export function normalizeVendorContactParseResult(raw: Record<string, unknown>): ParsedVendorContactFields {
  const cName = str(raw.c_name ?? raw.cName ?? raw.contact_name ?? raw.contactName ?? raw.name)
  const eName = str(raw.e_name ?? raw.english_name ?? raw.eName)
  return {
    cName,
    eName,
    gender: normalizeContactGender(raw.gender ?? raw.sex),
    title: str(raw.title ?? raw.position ?? raw.job_title),
    department: str(raw.department),
    mobile: str(raw.mobile ?? raw.mobile_phone ?? raw.cellphone),
    tel: str(raw.tel ?? raw.phone ?? raw.landline),
    email: str(raw.email ?? raw.mail),
    isMain: boolOrFalse(raw.is_main ?? raw.is_default ?? raw.default),
    remark: str(raw.remark ?? raw.remarks ?? raw.notes)
  }
}

/** 供应商联系人 AI 预填 → VendorContactEdit formData */
export function vendorContactPrefillToFormPayload(parsed: ParsedVendorContactFields): Record<string, unknown> {
  return {
    cName: parsed.cName || undefined,
    eName: parsed.eName || undefined,
    gender: parsed.gender,
    title: parsed.title || undefined,
    department: parsed.department || undefined,
    mobile: parsed.mobile || undefined,
    tel: parsed.tel || undefined,
    email: parsed.email || undefined,
    isMain: parsed.isMain,
    remark: parsed.remark || undefined
  }
}

export function normalizeVendorParseResult(raw: Record<string, unknown>): ParsedVendorFields {
  const officialName = str(raw.official_name ?? raw.vendor_name ?? raw.name)
  return {
    officialName,
    englishOfficialName: str(raw.english_official_name),
    nickName: inferEntityShortName(officialName, str(raw.nick_name ?? raw.short_name ?? raw.vendor_short_name)),
    industry: str(raw.industry),
    level: normalizeVendorLevel(raw.level ?? raw.vendor_level),
    credit: normalizeVendorCredit(raw.credit ?? raw.identity ?? raw.vendor_credit),
    officeAddress: str(raw.office_address ?? raw.address),
    website: str(raw.website),
    currency: mapPriceCurrency(raw.trade_currency ?? raw.currency) ?? 1,
    paymentMethod: str(raw.payment_method),
    paymentDays: numOrNull(raw.payment_days ?? raw.payment_terms),
    taxNumber: str(raw.credit_code ?? raw.tax_number ?? raw.unified_social_credit_code),
    companyInfo: str(raw.company_info),
    remark: str(raw.remark ?? raw.remarks)
  }
}

/** 供应商 AI 预填 → VendorEdit formData 片段 */
export function vendorPrefillToFormPayload(parsed: ParsedVendorFields): Record<string, unknown> {
  const payload: Record<string, unknown> = {
    officialName: parsed.officialName || undefined,
    englishOfficialName: parsed.englishOfficialName || undefined,
    nickName: parsed.nickName || undefined,
    industry: parsed.industry || undefined,
    officeAddress: parsed.officeAddress || undefined,
    website: parsed.website || undefined,
    paymentMethod: parsed.paymentMethod || undefined,
    taxNumber: parsed.taxNumber || undefined,
    companyInfo: parsed.companyInfo || undefined,
    remark: parsed.remark || undefined,
    contacts: []
  }
  if (parsed.level != null && parsed.level >= 1 && parsed.level <= 13) payload.level = parsed.level
  if (parsed.credit != null && parsed.credit >= 1 && parsed.credit <= 10) payload.credit = parsed.credit
  if (parsed.currency != null && parsed.currency >= 1 && parsed.currency <= 4) payload.currency = parsed.currency
  if (parsed.paymentDays != null && parsed.paymentDays >= 0) payload.paymentDays = parsed.paymentDays
  return payload
}

function normalizeRfqItemFields(itemRaw: Record<string, unknown>): ParsedRfqItemFields {
  const item = emptyParsedRfqItem()
  item.customerMpn = str(itemRaw.customer_mpn)
  item.customerBrand = str(itemRaw.customer_brand)
  item.mpn = str(itemRaw.mpn)
  item.brand = str(itemRaw.brand)
  item.targetPrice = numOrNull(itemRaw.target_price)
  item.priceCurrency =
    mapPriceCurrency(itemRaw.price_currency ?? itemRaw.target_price_currency ?? itemRaw.currency) ?? 1
  item.quantity = numOrNull(itemRaw.quantity)
  item.productionDate = str(itemRaw.production_date)
  item.expiryDate = str(itemRaw.expiry_date)
  item.minPackageQty = numOrNull(itemRaw.min_package_qty)
  item.minOrderQty = numOrNull(itemRaw.moq ?? itemRaw.min_order_qty)
  item.alternativeMaterials = str(itemRaw.alternatives)
  item.remark = str(itemRaw.remark ?? itemRaw.remarks ?? itemRaw.notes)
  return item
}

function hasItemContent(item: ParsedRfqItemFields): boolean {
  return !!(
    item.customerMpn ||
    item.mpn ||
    item.customerBrand ||
    item.brand ||
    item.remark ||
    item.targetPrice != null ||
    (item.quantity != null && item.quantity > 0)
  )
}

export function normalizeCustomerParseResult(raw: Record<string, unknown>): ParsedCustomerFields {
  const level = normalizeCustomerLevel(raw.customer_level)
  const customerName = str(raw.customer_name)
  return enrichCustomerRegionFields({
    customerName,
    customerShortName: inferEntityShortName(customerName, str(raw.customer_short_name)),
    englishOfficialName: str(raw.english_official_name),
    customerType: numOrNull(raw.customer_type),
    customerLevel: VALID_LEVELS.has(level) ? level : '',
    industry: str(raw.industry),
    country: str(raw.country),
    province: str(raw.province),
    city: str(raw.city),
    district: str(raw.district),
    address: str(raw.address),
    unifiedSocialCreditCode: str(raw.unified_social_credit_code),
    creditLimit: numOrNull(raw.credit_limit),
    paymentTerms: numOrNull(raw.payment_terms),
    currency: numOrNull(raw.currency),
    taxRate: numOrNull(raw.tax_rate),
    invoiceType: numOrNull(raw.invoice_type),
    companyInfo: str(raw.company_info ?? raw.companyInfo),
    remarks: str(raw.remarks ?? raw.remark)
  })
}

export function normalizeRfqParseResult(raw: Record<string, unknown>): ParsedRfqFields {
  const topRemark = str(raw.remark ?? raw.remarks ?? raw.notes)
  let items: ParsedRfqItemFields[] = []

  if (Array.isArray(raw.items)) {
    items = raw.items
      .filter((x) => x && typeof x === 'object' && !Array.isArray(x))
      .map((x) => normalizeRfqItemFields(x as Record<string, unknown>))
      .filter(hasItemContent)
  } else if (raw.item && typeof raw.item === 'object' && !Array.isArray(raw.item)) {
    const legacyItem = normalizeRfqItemFields(raw.item as Record<string, unknown>)
    if (!legacyItem.remark && topRemark) legacyItem.remark = topRemark
    if (hasItemContent(legacyItem)) items = [legacyItem]
  }

  if (items.length === 0) {
    const fallback = emptyParsedRfqItem()
    if (topRemark) fallback.remark = topRemark
    items = [fallback]
  } else if (items.length === 1 && !items[0].remark && topRemark) {
    items[0].remark = topRemark
  }

  return {
    customerName: str(raw.customer_name),
    customerId: '',
    contactEmail: str(raw.contact_email),
    industry: str(raw.industry),
    product: str(raw.product),
    rfqType: numOrNull(raw.rfq_type),
    targetType: numOrNull(raw.target_type),
    quoteMethod: numOrNull(raw.quote_method),
    assignMethod: numOrNull(raw.assign_method),
    importance: numOrNull(raw.importance),
    projectBackground: str(raw.project_background),
    competitor: str(raw.competitor),
    remark: topRemark,
    items
  }
}

/** 客户 AI 预填 → CustomerEdit formData 片段 */
export function customerPrefillToFormPayload(parsed: ParsedCustomerFields): Record<string, unknown> {
  const payload: Record<string, unknown> = {
    customerName: parsed.customerName || undefined,
    customerShortName: parsed.customerShortName || undefined,
    englishOfficialName: parsed.englishOfficialName || undefined,
    industry: parsed.industry || undefined,
    country: parsed.country || undefined,
    province: parsed.province || undefined,
    city: parsed.city || undefined,
    district: parsed.district || undefined,
    address: parsed.address || undefined,
    unifiedSocialCreditCode: parsed.unifiedSocialCreditCode || undefined,
    companyInfo: parsed.companyInfo || undefined,
    remarks: parsed.remarks || undefined,
    contacts: []
  }
  if (parsed.customerType != null && parsed.customerType >= 1 && parsed.customerType <= 3) {
    payload.customerType = parsed.customerType
  }
  if (parsed.customerLevel) payload.customerLevel = parsed.customerLevel
  if (parsed.creditLimit != null) payload.creditLimit = parsed.creditLimit
  if (parsed.paymentTerms != null) payload.paymentTerms = parsed.paymentTerms
  if (parsed.currency != null && parsed.currency >= 1 && parsed.currency <= 4) {
    payload.currency = parsed.currency
  }
  if (parsed.taxRate != null) payload.taxRate = parsed.taxRate
  if (parsed.invoiceType != null) payload.invoiceType = parsed.invoiceType
  return payload
}

/** RFQ AI 预填 → RFQCreate formData 片段 */
export function rfqPrefillToFormPayload(parsed: ParsedRfqFields): Record<string, unknown> {
  const sourceItems = parsed.items.length ? parsed.items : [emptyParsedRfqItem()]
  const items = sourceItems.map((it) => {
    const customerMpn = it.customerMpn.trim()
    const mpn = it.mpn.trim() || customerMpn
    return {
      customerMpn,
      customerBrand: it.customerBrand,
      mpn,
      brand: it.brand,
      brandId: undefined,
      quantity: it.quantity != null && it.quantity > 0 ? it.quantity : 1,
      targetPrice: it.targetPrice ?? undefined,
      productionDate: it.productionDate,
      expiryDate: it.expiryDate,
      minPackageQty: it.minPackageQty ?? undefined,
      minOrderQty: it.minOrderQty ?? undefined,
      alternativeMaterials: it.alternativeMaterials,
      remark: it.remark,
      priceCurrency:
        it.priceCurrency != null && it.priceCurrency >= 1 && it.priceCurrency <= 4 ? it.priceCurrency : 1
    }
  })

  const payload: Record<string, unknown> = {
    customerId: parsed.customerId || '',
    customerName: parsed.customerName || '',
    contactEmail: parsed.contactEmail || '',
    industry: parsed.industry || '',
    product: parsed.product || '',
    projectBackground: parsed.projectBackground || '',
    competitor: parsed.competitor || '',
    remark: parsed.remark || '',
    items
  }
  if (parsed.rfqType != null && parsed.rfqType >= 1 && parsed.rfqType <= 2) payload.rfqType = parsed.rfqType
  if (parsed.targetType != null && parsed.targetType >= 1 && parsed.targetType <= 2) payload.targetType = parsed.targetType
  if (parsed.quoteMethod != null && parsed.quoteMethod >= 1 && parsed.quoteMethod <= 2) {
    payload.quoteMethod = parsed.quoteMethod
  }
  if (parsed.assignMethod != null && parsed.assignMethod >= 1 && parsed.assignMethod <= 2) {
    payload.assignMethod = parsed.assignMethod
  }
  if (parsed.importance != null && parsed.importance >= 1 && parsed.importance <= 3) {
    payload.importance = parsed.importance
  }
  return payload
}

export type ParsedCustomerBusinessCardFields = {
  customer: ParsedCustomerFields
  contact: ParsedCustomerContactFields
  address: ParsedCustomerAddressFields | null
}

export type ParsedVendorBusinessCardFields = {
  vendor: ParsedVendorFields
  contact: ParsedVendorContactFields
  address: ParsedVendorAddressFields | null
}

export function normalizeCustomerBusinessCardParseResult(raw: Record<string, unknown>): ParsedCustomerBusinessCardFields {
  const customerRaw =
    raw.customer && typeof raw.customer === 'object' && !Array.isArray(raw.customer)
      ? (raw.customer as Record<string, unknown>)
      : raw
  const contactRaw =
    raw.contact && typeof raw.contact === 'object' && !Array.isArray(raw.contact)
      ? (raw.contact as Record<string, unknown>)
      : {}
  const customer = normalizeCustomerParseResult(customerRaw)
  const cardCompanyInfo = str(
    customerRaw.company_info ??
      customerRaw.companyInfo ??
      customerRaw.remarks ??
      customerRaw.remark
  )
  if (cardCompanyInfo) {
    customer.companyInfo = cardCompanyInfo
    if (customer.remarks === cardCompanyInfo) customer.remarks = ''
  }
  const contact = {
    ...normalizeCustomerContactParseResult(contactRaw),
    isDefault: true,
    gender: defaultBusinessCardContactGender(contactRaw.gender ?? contactRaw.sex)
  }
  let address: ParsedCustomerAddressFields | null = null
  if (raw.address && typeof raw.address === 'object' && !Array.isArray(raw.address)) {
    const addr = normalizeCustomerAddressParseResult(raw.address as Record<string, unknown>)
    if (addr.streetAddress.trim()) {
      address = { ...addr, isDefault: true }
    }
  }
  return { customer, contact, address }
}

/** 兼容后端 camelCase 归一化与 AI 原始 snake_case 输出 */
export function hydrateCustomerBusinessCardBundle(raw: Record<string, unknown>): ParsedCustomerBusinessCardFields {
  const customerRaw =
    raw.customer && typeof raw.customer === 'object' && !Array.isArray(raw.customer)
      ? (raw.customer as Record<string, unknown>)
      : raw
  const isBackendNormalized =
    'customerName' in customerRaw ||
    'customerShortName' in customerRaw ||
    'englishOfficialName' in customerRaw

  const bundle: ParsedCustomerBusinessCardFields = isBackendNormalized
    ? (JSON.parse(JSON.stringify(raw)) as ParsedCustomerBusinessCardFields)
    : normalizeCustomerBusinessCardParseResult(raw)

  const c = bundle.customer
  if (!c.companyInfo?.trim()) {
    const fromRemarks = c.remarks?.trim()
    if (fromRemarks) {
      c.companyInfo = fromRemarks
      c.remarks = ''
    }
  }
  return bundle
}

export function normalizeVendorBusinessCardParseResult(raw: Record<string, unknown>): ParsedVendorBusinessCardFields {
  const vendorRaw =
    raw.vendor && typeof raw.vendor === 'object' && !Array.isArray(raw.vendor)
      ? (raw.vendor as Record<string, unknown>)
      : raw
  const contactRaw =
    raw.contact && typeof raw.contact === 'object' && !Array.isArray(raw.contact)
      ? (raw.contact as Record<string, unknown>)
      : {}
  const vendor = normalizeVendorParseResult(vendorRaw)
  const cardCompanyInfo = str(vendorRaw.company_info ?? vendorRaw.remarks ?? vendorRaw.remark)
  if (cardCompanyInfo) {
    vendor.companyInfo = cardCompanyInfo
    if (vendor.remark === cardCompanyInfo) vendor.remark = ''
  }
  const contact = {
    ...normalizeVendorContactParseResult(contactRaw),
    isMain: true,
    gender: defaultBusinessCardContactGender(contactRaw.gender ?? contactRaw.sex)
  }
  let address: ParsedVendorAddressFields | null = null
  if (raw.address && typeof raw.address === 'object' && !Array.isArray(raw.address)) {
    const addr = normalizeVendorAddressParseResult(raw.address as Record<string, unknown>)
    if (addr.address.trim()) {
      address = { ...addr, isDefault: true }
    }
  }
  return { vendor, contact, address }
}

export function customerBusinessCardPrefillToFormPayload(
  parsed: ParsedCustomerBusinessCardFields,
  contactKey: string
): Record<string, unknown> {
  return {
    ...customerPrefillToFormPayload(parsed.customer),
    contacts: [
      {
        _key: contactKey,
        _fromBusinessCard: true,
        cName: parsed.contact.cName,
        eName: parsed.contact.eName,
        gender: parsed.contact.gender,
        department: parsed.contact.department,
        position: parsed.contact.position,
        mobilePhone: parsed.contact.mobilePhone,
        phone: parsed.contact.phone,
        email: parsed.contact.email,
        fax: parsed.contact.fax,
        isDefault: true,
        isDecisionMaker: parsed.contact.isDecisionMaker,
        remarks: parsed.contact.remarks
      }
    ],
    _businessCardFlow: true,
    _businessCardContactKey: contactKey,
    _businessCardAddress: parsed.address ? customerAddressPrefillToFormPayload(parsed.address) : null
  }
}

export function vendorBusinessCardPrefillToFormPayload(
  parsed: ParsedVendorBusinessCardFields,
  contactKey: string
): Record<string, unknown> {
  return {
    ...vendorPrefillToFormPayload(parsed.vendor),
    contacts: [
      {
        _key: contactKey,
        _fromBusinessCard: true,
        cName: parsed.contact.cName,
        eName: parsed.contact.eName,
        gender: parsed.contact.gender,
        title: parsed.contact.title,
        department: parsed.contact.department,
        mobile: parsed.contact.mobile,
        tel: parsed.contact.tel,
        email: parsed.contact.email,
        isMain: true,
        remark: parsed.contact.remark
      }
    ],
    _businessCardFlow: true,
    _businessCardContactKey: contactKey,
    _businessCardAddress: parsed.address ? vendorAddressPrefillToFormPayload(parsed.address) : null
  }
}

export function customerBusinessCardConfirmPayload(parsed: ParsedCustomerBusinessCardFields): Record<string, unknown> {
  return {
    customer: parsed.customer,
    contact: parsed.contact,
    address: parsed.address
  }
}

export function vendorBusinessCardConfirmPayload(parsed: ParsedVendorBusinessCardFields): Record<string, unknown> {
  return {
    vendor: parsed.vendor,
    contact: parsed.contact,
    address: parsed.address
  }
}
