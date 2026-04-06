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
  /** 部分接口与 officialName 同义返回 name */
  name?: string
  officialName?: string
  /** 公司英文全称 */
  englishOfficialName?: string
  nickName?: string
  industry?: string
  /** 等级（VendorLevelCode，vendorinfo.Level） */
  level?: number
  /** 身份（VendorIdentityCode，存于 vendorinfo.Credit） */
  credit?: number
  officeAddress?: string
  companyInfo?: string
  remark?: string
  website?: string
  purchaserName?: string
  tradeCurrency?: number
  /** 付款方式编码 */
  paymentMethod?: string
  /** 账期（天），后端字段 payment */
  payment?: number
  creditCode?: string
  /** 状态：1新建 2待审核 10已审核 12待财务审核 20财务建档 -1审核失败 */
  status?: number
  /** 审核驳回原因（status=-1 时） */
  auditRemark?: string
  isFavorite?: boolean
  /** 冻结/禁用（对应后端 IsDisenable） */
  isDisenable?: boolean
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
  officialName?: string
  englishOfficialName?: string
  nickName?: string
  industry?: string
  level?: number
  /** 身份枚举值（vendorinfo.Credit） */
  credit?: number
  status?: number
  officeAddress?: string
  website?: string
  purchaserName?: string
  tradeCurrency?: number
  currency?: number
  paymentMethod?: string
  paymentDays?: number
  creditCode?: string
  taxNumber?: string
  companyInfo?: string
  remark?: string
}

// 更新供应商请求（与后端 UpdateVendorRequest 对应）
export interface UpdateVendorRequest {
  name?: string
  englishOfficialName?: string
  nickName?: string
  industry?: string
  product?: string
  /** 身份枚举值（vendorinfo.Credit） */
  credit?: number
  status?: number
  officeAddress?: string
  website?: string
  purchaserName?: string
  level?: number
  tradeCurrency?: number
  paymentMethod?: string
  paymentDays?: number
  payment?: number
  creditCode?: string
  companyInfo?: string
  remark?: string
  externalNumber?: string
}

// 供应商列表查询参数（与后端 VendorQueryRequest 对应，前端用 pageNumber/pageSize）
export interface VendorSearchRequest {
  pageNumber?: number
  pageSize?: number
  keyword?: string
  /** 与客户列表 searchTerm 对齐 */
  searchTerm?: string
  status?: number
  level?: number
  industry?: string
  /** 身份（vendorinfo.Credit） */
  credit?: number
  /** 1 专属 2 公海 */
  ascriptionType?: number
  purchaseUserId?: string
  createdFrom?: string
  createdTo?: string
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
  /** 近 30 天新建供应商（未删除） */
  newLast30Days: number
  /** 至少有一条有效采购订单的去重供应商数 */
  vendorsWithDeals: number
  /** 未全部付款的采购订单折算总额（本位币） */
  payableAmount: number
  payableVendorCount: number
  /** 未全部入库的采购订单折算总额（本位币） */
  pendingInboundAmount: number
  pendingInboundVendorCount: number
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
