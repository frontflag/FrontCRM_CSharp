// ============================================================
// 需求管理模块 - 类型定义
// ============================================================

// 需求流转状态
export enum DemandStatus {
  Draft = 0,        // 草稿
  Pending = 1,      // 待处理
  Assigned = 2,     // 已分配
  Processing = 3,   // 处理中
  Quoted = 4,       // 已报价
  Accepted = 5,     // 已接受
  Rejected = 6,     // 已拒绝
  Closed = 7,       // 已关闭
  Cancelled = 8,    // 已取消
}

// 需求明细状态
export enum DemandItemStatus {
  Pending = 0,      // 待报价
  Quoted = 1,       // 已报价
  Accepted = 2,     // 已接受
  Rejected = 3,     // 已拒绝
  Closed = 4,       // 已关闭
}

// 采购员处理状态
export enum PurchaserHandleStatus {
  Unhandled = 0,    // 未处理
  Processing = 1,   // 处理中
  Completed = 2,    // 已完成
  Deferred = 3,     // 已推迟
}

// 需求来源
export enum DemandSource {
  Manual = 1,       // 手动录入
  Import = 2,       // 导入
  Email = 3,        // 邮件
  Online = 4,       // 在线
  Phone = 5,        // 电话
}

// 需求主表
export interface Demand {
  id: string
  demandCode: string
  customerId: string
  customerName?: string
  salesUserId?: string
  salesUserName?: string
  demandDate: string
  expectedDeliveryDate?: string
  status: DemandStatus
  source?: DemandSource
  currency?: string
  paymentTerms?: string
  shippingMethod?: number
  packagingRequirements?: string
  qualityRequirements?: string
  certificationRequirements?: string
  remark?: string
  isImportant?: boolean
  attachmentCount?: number
  itemCount?: number
  totalAmount?: number
  createdAt?: string
  updatedAt?: string
  createdBy?: string
  items?: DemandItem[]
}

// 需求明细
export interface DemandItem {
  id: string
  demandId: string
  lineNo: number
  materialId?: string
  materialCode?: string
  materialName?: string
  materialModel?: string
  customerMaterialCode?: string
  description?: string
  quantity: number
  unit?: string
  targetPrice?: number
  targetAmount?: number
  quotedPrice?: number
  quotedAmount?: number
  costPrice?: number
  costAmount?: number
  profitRate?: number
  brandRequirement?: string
  originRequirement?: string
  deliveryDate?: string
  moq?: number
  packagingSpec?: string
  remark?: string
  status: DemandItemStatus
  bestQuotePrice?: number
  bestQuoteSupplier?: string
}

// 采购员分配记录
export interface DemandAssignment {
  id: string
  demandId: string
  purchaserId: string
  purchaserName?: string
  purchaserEmail?: string
  assignedAt: string
  handleStatus: PurchaserHandleStatus
  remark?: string
}

// 需求关闭记录
export interface DemandCloseRecord {
  id: string
  demandId: string
  closeReason: string
  closeType: number
  closedBy?: string
  closedByName?: string
  closedAt: string
  remark?: string
}

// 采购员简要信息
export interface PurchaserInfo {
  id: string
  name: string
  email?: string
  handlingCount?: number
  recommendScore?: number
}

// 分配统计
export interface AssignmentStatistics {
  purchaserId: string
  purchaserName: string
  totalCount: number
  pendingCount: number
  completedCount: number
}

// ============================================================
// 请求/响应类型
// ============================================================

// 需求列表查询请求
export interface DemandSearchRequest {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  customerId?: string
  salesUserId?: string
  status?: DemandStatus | ''
  source?: DemandSource | ''
  isImportant?: boolean
  startDate?: string
  endDate?: string
  sortBy?: string
  sortDescending?: boolean
}

// 需求列表响应
export interface DemandSearchResponse {
  items: Demand[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// 需求明细查询请求
export interface DemandItemSearchRequest {
  pageNumber?: number
  pageSize?: number
  demandId?: string
  customerId?: string
  materialCode?: string
  materialName?: string
  status?: DemandItemStatus | ''
  startDate?: string
  endDate?: string
}

// 需求明细列表响应
export interface DemandItemSearchResponse {
  items: DemandItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// 新增需求请求
export interface CreateDemandRequest {
  customerId: string
  salesUserId?: string
  demandDate: string
  expectedDeliveryDate?: string
  source?: DemandSource
  currency?: string
  paymentTerms?: string
  shippingMethod?: number
  packagingRequirements?: string
  qualityRequirements?: string
  certificationRequirements?: string
  remark?: string
  items: CreateDemandItemRequest[]
}

// 新增需求明细请求
export interface CreateDemandItemRequest {
  lineNo?: number
  materialCode?: string
  materialName?: string
  materialModel?: string
  customerMaterialCode?: string
  description?: string
  quantity: number
  unit?: string
  targetPrice?: number
  brandRequirement?: string
  originRequirement?: string
  deliveryDate?: string
  moq?: number
  packagingSpec?: string
  remark?: string
}

// 更新需求请求
export interface UpdateDemandRequest {
  customerId?: string
  salesUserId?: string
  expectedDeliveryDate?: string
  source?: DemandSource
  currency?: string
  paymentTerms?: string
  shippingMethod?: number
  packagingRequirements?: string
  qualityRequirements?: string
  certificationRequirements?: string
  remark?: string
  isImportant?: boolean
  items?: CreateDemandItemRequest[]
}

// 变更状态请求
export interface UpdateDemandStatusRequest {
  status: DemandStatus
  remark?: string
}

// 分配采购员请求
export interface AssignPurchaserRequest {
  purchaserId: string
  remark?: string
}

// 关闭需求请求
export interface CloseDemandRequest {
  closeReason: string
  closeType: number
  remark?: string
}

// 重复物料检查请求
export interface CheckDuplicateMaterialRequest {
  customerId: string
  materialCode: string
  salesUserId?: string
  daysRange?: number
}

// 重复物料检查响应
export interface CheckDuplicateMaterialResponse {
  isDuplicate: boolean
  existingDemandId?: string
  existingDemandCode?: string
  message?: string
}
