import apiClient from './client'

export interface CompanyBasicRow {
  id: string
  isDefault: boolean
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
  bankName: string
  accountName: string
  bankAddress: string
  swift: string
  iban: string
  bankCode: string
  currency: string
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

export interface CompanyProfileBundle {
  basicInfos: CompanyBasicRow[]
  bankInfos: CompanyBankRow[]
  /** 公司 Logo（多组）；旧接口可能缺省，按空列表处理 */
  logos?: CompanyLogoRow[]
  seals: CompanySealRow[]
  warehouses: CompanyWarehouseRow[]
  smtpEmail?: CompanySmtpEmailSettings | null
}

const BASE = '/api/v1/company-profile'

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
