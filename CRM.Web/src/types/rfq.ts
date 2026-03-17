// ============================================================
// RFQ（需求询价）管理模块 - 类型定义
// ============================================================

// RFQ 流转状态
export enum RFQStatus {
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

// RFQ 明细状态
export enum RFQItemStatus {
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

// RFQ 来源
export enum RFQSource {
  Manual = 1,       // 手动录入
  Import = 2,       // 导入
  Email = 3,        // 邮件
  Online = 4,       // 在线
  Phone = 5,        // 电话
}

// RFQ 主表
export interface RFQ {
  id: string
  rfqCode: string
  customerId: string
  customerName?: string
  salesUserId?: string
  salesUserName?: string
  rfqDate: string
  expectedDeliveryDate?: string
  status: RFQStatus
  source?: RFQSource
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
  items?: RFQItem[]
}

// RFQ 明细
export interface RFQItem {
  id: string
  rfqId: string
  lineNo: number
  materialId?: string
  materialCode?: string
  materialName?: string
  materialModel?: string
  specification?: string
  customerMaterialCode?: string
  description?: string
  quantity: number
  unit?: string
  targetPrice?: number
  targetUnitPrice?: number
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
  status: RFQItemStatus
  bestQuotePrice?: number
  bestQuoteSupplier?: string
}

// 采购员分配记录
export interface RFQAssignment {
  id: string
  rfqId: string
  purchaserId: string
  purchaserName?: string
  purchaserEmail?: string
  assignedAt: string
  handleStatus: PurchaserHandleStatus
  remark?: string
}

// RFQ 关闭记录
export interface RFQCloseRecord {
  id: string
  rfqId: string
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

// RFQ 列表查询请求
export interface RFQSearchRequest {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  customerId?: string
  salesUserId?: string
  status?: RFQStatus | ''
  source?: RFQSource | ''
  isImportant?: boolean
  startDate?: string
  endDate?: string
  sortBy?: string
  sortDescending?: boolean
}

// RFQ 列表响应
export interface RFQSearchResponse {
  items: RFQ[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// RFQ 明细查询请求
export interface RFQItemSearchRequest {
  pageNumber?: number
  pageSize?: number
  rfqId?: string
  customerId?: string
  materialCode?: string
  materialName?: string
  status?: RFQItemStatus | ''
  startDate?: string
  endDate?: string
}

// RFQ 明细列表响应
export interface RFQItemSearchResponse {
  items: RFQItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// 新增 RFQ 请求
export interface CreateRFQRequest {
  customerId: string
  salesUserId?: string
  rfqDate: string
  expectedDeliveryDate?: string
  source?: RFQSource
  currency?: string
  paymentTerms?: string
  shippingMethod?: number
  packagingRequirements?: string
  qualityRequirements?: string
  certificationRequirements?: string
  remark?: string
  items: CreateRFQItemRequest[]
}

// 新增 RFQ 明细请求
export interface CreateRFQItemRequest {
  lineNo?: number
  materialCode?: string
  materialName?: string
  materialModel?: string
  specification?: string
  customerMaterialCode?: string
  description?: string
  quantity: number
  unit?: string
  targetPrice?: number
  targetUnitPrice?: number
  brandRequirement?: string
  originRequirement?: string
  deliveryDate?: string
  moq?: number
  packagingSpec?: string
  remark?: string
}

// 更新 RFQ 请求
export interface UpdateRFQRequest {
  customerId?: string
  salesUserId?: string
  expectedDeliveryDate?: string
  source?: RFQSource
  currency?: string
  paymentTerms?: string
  shippingMethod?: number
  packagingRequirements?: string
  qualityRequirements?: string
  certificationRequirements?: string
  remark?: string
  isImportant?: boolean
  items?: CreateRFQItemRequest[]
}

// 变更状态请求
export interface UpdateRFQStatusRequest {
  status: RFQStatus
  remark?: string
}

// 分配采购员请求
export interface AssignPurchaserRequest {
  purchaserId: string
  remark?: string
}

// 关闭 RFQ 请求
export interface CloseRFQRequest {
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
  existingRFQId?: string
  existingRFQCode?: string
  message?: string
}
