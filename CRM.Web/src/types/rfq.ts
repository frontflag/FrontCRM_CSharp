// ============================================================
// RFQ（需求询价）管理模块 - 类型定义
// 字段标准：对齐参考系统 RF2603J6UB 页面
// ============================================================

// RFQ 流转状态
export enum RFQStatus {
  Draft = 0,        // 草稿/新建
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

// RFQ 来源（线下/线上等）
export enum RFQSource {
  Offline = 1,      // 线下
  Online = 2,       // 线上
  Email = 3,        // 邮件
  Phone = 4,        // 电话
  Import = 5,       // 导入
}

// 需求类型（与 rfqFormEnums / DB rfq_type 一致）
export enum RFQType {
  Spot = 1, // 现货
  Scheduled = 2, // 排单
  Agency = 3, // 代理
  SelfOperated = 4, // 自营
  InfoService = 5 // 信息服务
}

// 报价方式（与 rfqFormEnums / DB quote_method 一致）
export enum QuoteMethod {
  None = 1, // 不接受任何消息
  SystemPush = 2, // 系统推送
  Email = 3, // 邮件
  Sms = 4 // 短信
}

// 分配方式（与 rfqFormEnums / DB assign_method 一致）
export enum AssignMethod {
  SamePurchaser = 1, // 系统分配同一采购
  MultiPurchaser = 2, // 系统分配多人采购
  SameBrandSamePurchaser = 3, // 相同品牌分配同一采购
  DesignatedPurchaser = 4 // 指定采购员
}

// 目标类型
export enum TargetType {
  PriceComparison = 1, // 比价需求
  Exclusive = 2,       // 独家需求
  Urgent = 3,          // 紧急需求
  Regular = 4,         // 常规需求
}

// ============================================================
// 主表
// ============================================================
export interface RFQ {
  id: string
  rfqCode: string                    // 需求单号（自动生成）
  status: RFQStatus

  // 基础信息
  customerId: string
  customerName?: string
  contactPersonId?: string           // 客户联系人 ID
  contactPersonName?: string         // 客户联系人姓名
  contactPersonEmail?: string        // 联系人邮箱
  salesUserId?: string
  salesUserName?: string

  // 需求信息
  rfqType?: RFQType                  // 需求类型（见 rfqFormEnums）
  quoteMethod?: QuoteMethod          // 报价方式
  assignMethod?: AssignMethod        // 分配方式
  industry?: string                  // 行业
  product?: string                   // 产品
  targetType?: TargetType            // 目标类型（比价需求等）
  importanceLevel?: number           // 重要程度（1-10）
  isLastQuote?: boolean              // 最后一次询价
  projectBackground?: string         // 项目背景
  competitor?: string                // 竞争对手
  source?: RFQSource                 // 来源（线下/线上等）
  currency?: string                  // 结算货币
  remark?: string

  // 汇率（只读展示）
  exchangeRateCNY?: number           // 人民币汇率
  exchangeRateHKD?: number           // 港币汇率
  exchangeRateEUR?: number           // 欧元汇率
  exchangeRateTax?: number           // 含税汇率

  // 统计
  itemCount?: number
  createdAt?: string
  updatedAt?: string
  createdBy?: string
  items?: RFQItem[]
}

// ============================================================
// 明细
// ============================================================
export interface RFQItem {
  id: string
  rfqId: string
  lineNo: number

  // 来自主表的扩展字段（用于列表展示/跳转）
  rfqCode?: string
  rfqCreateTime?: string
  customerName?: string

  // 物料信息
  customerMaterialModel?: string     // 客户物料型号
  materialModel?: string             // 物料型号（主）
  customerBrand?: string             // 客户品牌
  brand?: string                     // 品牌（我方，如 VISHAY/威世）

  // 价格与数量
  targetPrice?: number               // 目标价
  currency?: string                  // 货币单位（RMB/USD等）
  quantity: number                   // 数量

  // 日期
  productionDate?: string            // 生产日期（如"2年内"）
  expiryDate?: string                // 失效日期

  // 起订量
  minPackageQty?: number             // 最小包装数
  minOrderQty?: number               // 最小起订量

  // 其他
  alternativeMaterials?: string      // 可替代料（逗号分隔）
  remark?: string

  /** 系统轮询分配的询价采购员（与后端 assignedPurchaserUserId1/2 对齐） */
  assignedPurchaserUserId1?: string
  assignedPurchaserUserId2?: string
  assignedPurchaserName1?: string
  assignedPurchaserName2?: string

  status: RFQItemStatus
}

// ============================================================
// 采购员分配记录
// ============================================================
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

// ============================================================
// RFQ 关闭记录
// ============================================================
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

// ============================================================
// 采购员信息
// ============================================================
export interface PurchaserInfo {
  id: string
  name: string
  email?: string
  handlingCount?: number
  recommendScore?: number
}

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

export interface RFQSearchRequest {
  pageNumber?: number
  pageSize?: number
  keyword?: string
  searchTerm?: string
  customerId?: string
  salesUserId?: string
  status?: RFQStatus | ''
  rfqType?: RFQType | ''
  source?: RFQSource | ''
  startDate?: string
  endDate?: string
  sortBy?: string
  sortDescending?: boolean
}

export interface RFQSearchResponse {
  items: RFQ[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface RFQItemSearchRequest {
  pageNumber?: number
  pageSize?: number
  rfqId?: string
  customerId?: string
  customerKeyword?: string
  materialModel?: string
  customerMaterialModel?: string
  /** 主表业务员用户 ID（与 auth sales-users-tree 范围一致，由前端下拉传入） */
  salesUserId?: string
  salesUserKeyword?: string
  status?: RFQItemStatus | ''
  startDate?: string
  endDate?: string
}

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
  contactId?: string           // 对应后端 contact_id
  contactEmail?: string        // 对应后端 contact_email
  salesUserId?: string
  rfqType?: RFQType
  quoteMethod?: QuoteMethod
  assignMethod?: AssignMethod
  industry?: string
  product?: string
  targetType?: TargetType
  importance?: number          // 对应后端 importance
  isLastInquiry?: boolean      // 对应后端 is_last_inquiry
  projectBackground?: string
  competitor?: string
  source?: RFQSource
  currency?: string
  remark?: string
  items: CreateRFQItemRequest[]
}

// 新增 RFQ 明细请求
export interface CreateRFQItemRequest {
  lineNo?: number
  // 后端字段名
  customerMpn?: string         // 客户物料型号，对应后端 customer_mpn
  mpn?: string                 // 物料型号，对应后端 mpn
  // UI 兼容字段名（前端表单使用）
  customerMaterialModel?: string  // 映射到 customerMpn
  materialModel?: string          // 映射到 mpn
  customerBrand?: string
  brand?: string
  targetPrice?: number
  priceCurrency?: number       // 货币枚举：1=RMB,2=USD,3=EUR,4=HKD
  currency?: string            // UI 展示用货币字符串（RMB/USD/EUR/HKD）
  quantity: number
  productionDate?: string
  expiryDate?: string
  minPackageQty?: number
  moq?: number                 // 最小起订量，对应后端 moq
  minOrderQty?: number         // UI 展示用，映射到 moq
  alternatives?: string        // 可替代料，对应后端 alternatives
  alternativeMaterials?: string // UI 展示用，映射到 alternatives
  remark?: string
  // 内部 UI 状态字段
  _key?: number
  _isDuplicate?: boolean
}

// 更新 RFQ 请求
export interface UpdateRFQRequest {
  customerId?: string
  contactId?: string
  contactEmail?: string
  salesUserId?: string
  rfqType?: RFQType
  quoteMethod?: QuoteMethod
  assignMethod?: AssignMethod
  industry?: string
  product?: string
  targetType?: TargetType
  importance?: number
  isLastInquiry?: boolean
  projectBackground?: string
  competitor?: string
  source?: RFQSource
  currency?: string
  remark?: string
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
  materialModel: string
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
