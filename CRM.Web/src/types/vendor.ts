// 供应商联系人
export interface VendorContactInfo {
  id: string
  vendorId: string
  cName?: string
  eName?: string
  title?: string
  department?: string
  mobile?: string
  tel?: string
  email?: string
  qq?: string
  weChat?: string
  isMain: boolean
  remark?: string
  createTime?: string
  modifyTime?: string
}

// 供应商主信息（列表/详情）
export interface Vendor {
  id: string
  code: string
  officialName?: string
  nickName?: string
  industry?: string
  credit?: number
  officeAddress?: string
  companyInfo?: string
  status?: number
  blackList?: boolean
  isDeleted?: boolean
  deleteTime?: string
  deleteReason?: string
  createTime?: string
  modifyTime?: string
  contacts?: VendorContactInfo[]
  addresses?: VendorAddress[]
  bankAccounts?: VendorBankInfo[]
}

// 创建供应商请求（与后端 CreateVendorRequest 对应）
export interface CreateVendorRequest {
  code?: string
  name?: string
  remark?: string
}

// 更新供应商请求（与后端 UpdateVendorRequest 对应）
export interface UpdateVendorRequest {
  name?: string
  remark?: string
}

// 供应商列表查询参数（与后端 VendorQueryRequest 对应，前端用 pageNumber/pageSize）
export interface VendorSearchRequest {
  pageNumber?: number
  pageSize?: number
  keyword?: string
  status?: number
}

// 供应商列表响应（与后端分页结果对应）
export interface VendorSearchResponse {
  items: Vendor[]
  totalCount: number
  pageNumber?: number
  pageSize?: number
  totalPages?: number
}

// 供应商统计信息
export interface VendorStatistics {
  totalVendors: number
  activeVendors: number
  newThisMonth: number
  byLevel: Record<string, number>
  byIndustry: Record<string, number>
}

// 添加供应商联系人请求（与后端 AddVendorContactRequest 对应）
export interface AddVendorContactRequest {
  cName?: string
  eName?: string
  title?: string
  department?: string
  mobile?: string
  tel?: string
  email?: string
  isMain?: boolean
  remark?: string
}

// 更新供应商联系人请求（与后端 UpdateVendorContactRequest 对应）
export interface UpdateVendorContactRequest {
  cName?: string
  eName?: string
  title?: string
  department?: string
  mobile?: string
  tel?: string
  email?: string
  isMain?: boolean
  remark?: string
}

// 供应商地址
export interface VendorAddress {
  id: string
  vendorId: string
  addressType: number
  country?: number
  province?: string
  city?: string
  area?: string
  address?: string
  contactName?: string
  contactPhone?: string
  isDefault: boolean
  remark?: string
  createTime?: string
  modifyTime?: string
}

// 供应商银行账户
export interface VendorBankInfo {
  id: string
  vendorId: string
  bankName?: string
  bankAccount?: string
  accountName?: string
  bankBranch?: string
  currency?: number
  isDefault: boolean
  remark?: string
  createTime?: string
  modifyTime?: string
}
