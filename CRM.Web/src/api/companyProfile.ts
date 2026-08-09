import apiClient from './client'
import { REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'

export interface CompanyBasicRow {
  id: string
  isDefault: boolean
  /** 人民币抬头；与 isDefaultForeign 互斥；全列表最多一组 */
  isDefaultRmb?: boolean
  /** 外币抬头；与 isDefaultRmb 互斥；全列表最多一组 */
  isDefaultForeign?: boolean
  enabled: boolean
  companyName: string
  taxId: string
  legalPerson: string
  address: string
  postalCode: string
  phone: string
  fax: string
  email: string
}

export interface CompanyBankRow {
  id: string
  isDefault: boolean
  enabled: boolean
  /** 可用付款：勾选后出现在付款单付款银行下拉 */
  availableForPayment?: boolean
  bankName: string
  accountName: string
  bankAddress: string
  swift: string
  iban: string
  bankCode: string
  accountNumber: string
  currency: string
  country: string
  bankType: string
  purposeType: string
  remark: string
}

export interface CompanyLogoRow {
  id: string
  isDefault: boolean
  enabled: boolean
  logoName: string
  documentId?: string
  fileName?: string
}

export interface CompanySealRow {
  id: string
  isDefault: boolean
  enabled: boolean
  sealName: string
  useScene: string
  documentId?: string
  fileName?: string
}

export interface CompanyWarehouseRow {
  id: string
  isDefault: boolean
  enabled: boolean
  warehouseName: string
  address: string
  contactName: string
  contactPhone: string
  workHours: string
}

/** 与后端 CompanySmtpEmailSettingsDto 一致；password 仅提交新值，留空表示保留；passwordSet 为 GET 时是否已存密码 */
export interface CompanySmtpEmailSettings {
  enabled: boolean
  smtpHost: string
  smtpPort: number
  user: string
  password: string
  fromAddress: string
  fromName: string
  useSsl: boolean
  passwordSet?: boolean
}

export interface CompanyReportRemarks {
  remarkCn: string
  remarkEn: string
}

export interface CompanyReportInfo {
  invoice: CompanyReportRemarks
  packingList: CompanyReportRemarks
}

export function isRmbCurrency(currency: string | undefined | null): boolean {
  const c = (currency ?? '').trim().toUpperCase()
  return c === 'RMB' || c === 'CNY' || c === 'CNH'
}

/** 按人民币/外币分组选取默认银行账户；可指定优先分组。 */
export function pickDefaultBank(
  rows: CompanyBankRow[] | undefined | null,
  options?: { preferRmb?: boolean }
): CompanyBankRow | undefined {
  if (!rows?.length) return undefined
  const pickFrom = (list: CompanyBankRow[]) => {
    if (!list.length) return undefined
    return list.find((r) => r.isDefault && r.enabled !== false) ?? list[0]
  }
  const rmb = rows.filter((r) => isRmbCurrency(r.currency))
  const fx = rows.filter((r) => !isRmbCurrency(r.currency))
  if (options?.preferRmb === true) {
    return pickFrom(rmb) ?? pickFrom(fx) ?? pickFrom(rows)
  }
  if (options?.preferRmb === false) {
    return pickFrom(fx) ?? pickFrom(rmb) ?? pickFrom(rows)
  }
  return rows.find((r) => r.isDefault && r.enabled !== false) ?? rows[0]
}

/** 按仓库地域选取默认银行：海外仓→外币默认，大陆仓→人民币默认。 */
export function pickDefaultBankByRegion(
  rows: CompanyBankRow[] | undefined | null,
  regionType: number | undefined | null
): CompanyBankRow | undefined {
  const preferRmb = normalizeRegionType(regionType) !== REGION_TYPE_OVERSEAS
  return pickDefaultBank(rows, { preferRmb })
}

export function emptyCompanyReportInfo(): CompanyReportInfo {
  return {
    invoice: { remarkCn: '', remarkEn: '' },
    packingList: { remarkCn: '', remarkEn: '' }
  }
}

/** 多行备注按行拆分；空行忽略 */
export function splitReportRemarkLines(text: string | undefined | null): string[] {
  if (!text?.trim()) return []
  return text
    .split(/\r?\n/)
    .map((s) => s.trim())
    .filter(Boolean)
}

/** Packing List 报表页脚备注：固定读取 sysparam Remark.EN */
export function packingListReportRemarkLines(remarks: CompanyReportRemarks | undefined | null): string[] {
  return splitReportRemarkLines(remarks?.remarkEn)
}

/** 按界面语言取备注行；未配置时回退 fallback */
export function pickReportRemarkLines(
  remarks: CompanyReportRemarks | undefined,
  locale: string,
  fallback: string[] = []
): string[] {
  const en = splitReportRemarkLines(remarks?.remarkEn)
  const cn = splitReportRemarkLines(remarks?.remarkCn)
  if (locale === 'en-US') return en.length ? en : fallback
  return cn.length ? cn : fallback
}

export interface CompanyProfileBundle {
  basicInfos: CompanyBasicRow[]
  bankInfos: CompanyBankRow[]
  /** 公司 Logo（多组）；旧接口可能缺省，按空列表处理 */
  logos?: CompanyLogoRow[]
  seals: CompanySealRow[]
  warehouses: CompanyWarehouseRow[]
  smtpEmail?: CompanySmtpEmailSettings | null
  reportInfo?: CompanyReportInfo | null
}

const BASE = '/api/v1/company-profile'

/** 登录页左侧品牌图（匿名 GET，与「公司信息」公司 Logo 默认/首条有文件记录同源） */
export const COMPANY_LOGIN_LOGO_URL = `${BASE}/login-logo`

export async function fetchCompanyProfile(): Promise<CompanyProfileBundle> {
  const res = await apiClient.get<CompanyProfileBundle>(BASE)
  return res as CompanyProfileBundle
}

/** 采购/销售/供应商质保书等只读场景（需 purchase-order.read、sales-order.read 或 vendor.read 之一，无需参数管理权限） */
export async function fetchCompanyProfileForReport(): Promise<CompanyProfileBundle> {
  const res = await apiClient.get<CompanyProfileBundle>(`${BASE}/report-bundle`)
  return res as CompanyProfileBundle
}

export async function saveCompanyProfile(body: CompanyProfileBundle): Promise<void> {
  await apiClient.put(BASE, body)
}

export async function checkCompanyBankCanDelete(bankId: string): Promise<{ canDelete: boolean }> {
  const id = String(bankId || '').trim()
  const res = await apiClient.get<{ canDelete: boolean }>(
    `${BASE}/bank/${encodeURIComponent(id)}/can-delete`
  )
  return { canDelete: Boolean(res?.canDelete) }
}
