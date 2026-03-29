// 客户等级
export enum CustomerLevel {
  D = 1,
  C = 2,
  B = 3,
  BPO = 4,
  VIP = 5,
  VPO = 6
}

// 客户类型
export enum CustomerType {
  OEM = 1,
  ODM = 2,
  EndUser = 3,
  IDH = 4,
  Trader = 5,
  Agent = 6
}

// 客户归属类型
export enum AscriptionType {
  Exclusive = 1,
  PublicSea = 2
}

// 客户状态
export enum CustomerStatus {
  New = 1,
  PendingAudit = 2,
  Approved = 10,
  PendingFinanceAudit = 12,
  FinanceFiled = 20,
  AuditFailed = -1
}

// 地址类型
export enum AddressType {
  Shipping = 1,
  Billing = 2
}

// 性别
export enum Gender {
  Male = 1,
  Female = 2
}

// 客户银行账户
export interface CustomerBankInfo {
  id: string
  customerId: string
  bankName: string
  accountNumber: string
  accountName: string
  bankBranch: string
  currency: number
  swiftCode?: string
  isDefault: boolean
  remark?: string
  createdAt?: string
  updatedAt?: string
}

// 客户联系人
export interface CustomerContactInfo {
  id: string
  customerId: string
  contactName: string
  gender: number
  position?: string
  department?: string
  mobilePhone: string
  phone?: string
  fax?: string
  email?: string
  socialAccount?: string
  isDecisionMaker: boolean
  isDefault: boolean
  remarks?: string
  createdAt?: string
  updatedAt?: string
}

// 客户地址
export interface CustomerAddress {
  id: string
  customerId: string
  addressType: string
  country?: string
  province?: string
  city?: string
  district?: string
  streetAddress: string
  zipCode?: string
  contactPerson?: string
  contactPhone?: string
  isDefault: boolean
  remark?: string
  createdAt?: string
  updatedAt?: string
}

// 客户主信息（用于列表展示）
export interface Customer {
  id: string
  customerCode: string
  customerName?: string
  customerShortName?: string
  customerType?: number
  customerLevel?: string
  industry?: string
  unifiedSocialCreditCode?: string
  salesPersonId?: string
  salesPersonName?: string
  region?: string
  country?: string
  province?: string
  city?: string
  district?: string
  address?: string
  creditLimit?: number
  balance?: number
  paymentTerms?: number
  currency?: number
  taxRate?: number
  invoiceType?: number
  /** 状态：1新建 2待审核 10已审核 12待财务审核 20财务建档 -1审核失败 */
  status?: number
  /** 审核驳回原因（status=-1 时） */
  auditRemark?: string
  isActive?: boolean
  /** 冻结/禁用（对应后端 DisenableStatus） */
  disenableStatus?: boolean
  isFavorite?: boolean
  remarks?: string
  blackList?: boolean
  blackListReason?: string
  blackListTime?: string
  blackListUserName?: string
  deleteReason?: string
  deletedAt?: string
  deletedByUserName?: string
  contacts?: CustomerContactInfo[]
  addresses?: CustomerAddress[]
  banks?: CustomerBankInfo[]
  /** 后端 BaseEntity.CreateTime 序列化名（与 createdAt 二选一存在） */
  createTime?: string
  createdAt?: string
  updatedAt?: string
}

// 客户主信息（旧版）
export interface CustomerInfo {
  id: string
  customerCode: string
  officialName?: string
  standardOfficialName?: string
  nickName?: string
  level: number
  type?: number
  scale?: number
  background?: number
  dealMode?: number
  companyNature?: number
  country?: number
  industry?: string
  product?: string
  product2?: string
  application?: string
  tradeCurrency?: number
  tradeType?: number
  payment?: number
  externalNumber?: string
  creditLine: number
  creditLineRemain: number
  ascriptionType: number
  protectStatus: boolean
  protectFromUserId?: string
  protectTime?: string
  status: number
  blackList: boolean
  disenableStatus: boolean
  disenableType?: number
  commonSeaAuditStatus?: number
  longitude?: number
  latitude?: number
  companyInfo?: string
  remark?: string
  duns?: string
  isControl: boolean
  creditCode?: string
  identityType?: number
  salesUserId?: string
  contacts: CustomerContactInfo[]
  addresses: CustomerAddress[]
  bankAccounts: CustomerBankInfo[]
  createdAt?: string
  updatedAt?: string
}

// 创建客户请求
export interface CreateCustomerRequest {
  customerCode?: string
  customerName: string
  customerShortName?: string
  customerType: number
  customerLevel: string
  industry?: string
  unifiedSocialCreditCode?: string
  salesPersonId?: string
  salesPersonName?: string
  country?: string
  province?: string
  city?: string
  district?: string
  address?: string
  creditLimit?: number
  paymentTerms?: number
  currency?: number
  taxRate?: number
  invoiceType?: number
  isActive?: boolean
  remarks?: string
  // 旧版字段兼容
  officialName?: string
  standardOfficialName?: string
  nickName?: string
  level?: number
  type?: number
  product?: string
  tradeCurrency?: number
  tradeType?: number
  payment?: number
  creditLine?: number
  ascriptionType?: number
  creditCode?: string
  companyInfo?: string
}

// 更新客户请求
export interface UpdateCustomerRequest {
  id?: string
  customerName?: string
  customerShortName?: string
  customerType?: number
  customerLevel?: string
  industry?: string
  unifiedSocialCreditCode?: string
  salesPersonId?: string
  salesPersonName?: string
  country?: string
  province?: string
  city?: string
  district?: string
  address?: string
  creditLimit?: number
  paymentTerms?: number
  currency?: number
  taxRate?: number
  invoiceType?: number
  isActive?: boolean
  remarks?: string
  // 旧版字段兼容
  officialName?: string
  standardOfficialName?: string
  nickName?: string
  level?: number
  type?: number
  scale?: number
  background?: number
  dealMode?: number
  companyNature?: number
  externalNumber?: string
  creditLine?: number
  companyInfo?: string
  remark?: string
  duns?: string
  creditCode?: string
  identityType?: number
  salesUserId?: string
}

// 客户搜索请求
export interface CustomerSearchRequest {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  customerType?: number
  /** 等级字母：D/C/B/BPO/VIP/VPO 等，请求前会映射为后端 level 数字 */
  customerLevel?: string
  industry?: string
  region?: string
  /** 业务员用户 ID（后端 query: salesUserId） */
  salesPersonId?: string
  /** 创建日期起 YYYY-MM-DD */
  createdFrom?: string
  /** 创建日期止 YYYY-MM-DD（含当日） */
  createdTo?: string
  /** 工作流状态，对应后端 query: status */
  status?: number
  isActive?: boolean
  sortBy?: string
  sortDescending?: boolean
}

// 客户搜索响应
export interface CustomerSearchResponse {
  items: Customer[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// 客户统计
export interface CustomerStatistics {
  totalCustomers: number
  activeCustomers: number
  newThisMonth: number
  /** 近 30 天新建客户（滚动窗口） */
  newLast30Days: number
  /** 至少有一条有效销售订单的 distinct 客户数 */
  customersWithDeals: number
  totalBalance: number
  /** 未全部收款的销售订单折算总额（本位币） */
  receivableGoodsAmount: number
  receivableCustomerCount: number
  /** 未全部出库的销售订单折算总额（本位币） */
  pendingOutboundAmount: number
  pendingOutboundCustomerCount: number
  byLevel: Record<string, number>
  byIndustry: Record<string, number>
}

// 创建联系人请求
export interface CreateContactRequest {
  contactName: string
  gender: number
  department?: string
  position?: string
  mobilePhone: string
  phone?: string
  email?: string
  fax?: string
  socialAccount?: string
  isDefault: boolean
  isDecisionMaker: boolean
  remarks?: string
}

// 创建地址请求
export interface CreateAddressRequest {
  addressType: string
  country?: string
  province?: string
  city?: string
  district?: string
  streetAddress: string
  zipCode?: string
  contactPerson?: string
  contactPhone?: string
  isDefault: boolean
}

// 创建银行信息请求
export interface CreateBankInfoRequest {
  accountName: string
  bankName: string
  bankBranch: string
  accountNumber: string
  currency: number
  swiftCode?: string
  isDefault: boolean
}

// 客户查询参数
export interface CustomerQueryParams {
  keyword?: string
  level?: number
  type?: number
  status?: number
  ascriptionType?: number
  salesUserId?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
}

// 分页结果
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// 客户等级选项
export const customerLevelOptions = [
  { value: 1, label: 'D级' },
  { value: 2, label: 'C级' },
  { value: 3, label: 'B级' },
  { value: 4, label: 'BPO' },
  { value: 5, label: 'VIP' },
  { value: 6, label: 'VPO' }
]

// 客户类型选项
export const customerTypeOptions = [
  { value: 1, label: 'OEM' },
  { value: 2, label: 'ODM' },
  { value: 3, label: '终端' },
  { value: 4, label: 'IDH' },
  { value: 5, label: '贸易商' },
  { value: 6, label: '代理商' }
]

// 客户状态选项
export const customerStatusOptions = [
  { value: 1, label: '新建', type: 'info' },
  { value: 2, label: '待审核', type: 'warning' },
  { value: 10, label: '已审核', type: 'success' },
  { value: 12, label: '待财务审核', type: 'warning' },
  { value: 20, label: '财务建档', type: 'primary' },
  { value: -1, label: '审核失败', type: 'danger' }
]

// 归属类型选项
export const ascriptionTypeOptions = [
  { value: 1, label: '专属', type: 'success' },
  { value: 2, label: '公海', type: 'info' }
]

// 地址类型选项
export const addressTypeOptions = [
  { value: 1, label: '收货地址' },
  { value: 2, label: '账单地址' }
]

// 性别选项
export const genderOptions = [
  { value: 1, label: '男' },
  { value: 2, label: '女' }
]

/** 与客户「结算货币」一致；请优先从 @/constants/currency 引用 */
export { SETTLEMENT_CURRENCY_OPTIONS as currencyOptions } from '@/constants/currency'
