// ============================================================
// BOM（批量快速报价）管理模块 - 类型定义
// BOM 与 RFQ 的区别：RFQ 是高价值需求（人工报价），BOM 是低价值需求（系统快速报价）
// ============================================================

// BOM 主体状态
export enum BOMStatus {
  Draft = 0,        // 草稿
  Pending = 1,      // 待报价
  Quoting = 2,      // 报价中
  Quoted = 3,       // 已报价
  Accepted = 4,     // 已接受
  Closed = 5,       // 已关闭
  Cancelled = 6,    // 已取消
}

// BOM 明细报价状态
export enum BOMItemQuoteStatus {
  Pending = 0,      // 待报价
  AutoQuoted = 1,   // 系统自动报价
  ManualQuoted = 2, // 人工报价
  NoStock = 3,      // 无货
  Accepted = 4,     // 已接受
  Rejected = 5,     // 已拒绝
}

// 来源（与 RFQ 共用）
export enum BOMSource {
  Offline = 1,
  Online = 2,
  Email = 3,
  Phone = 4,
  Import = 5,
}

// 需求类型（与 RFQ 共用）
export enum BOMType {
  Spot = 1,   // 现货
  Future = 2, // 期货
  Sample = 3, // 样品
  Bulk = 4,   // 批量
}

// ============================================================
// BOM 主体
// ============================================================
export interface BOM {
  id: string
  bomCode: string                     // BOM 单号（自动生成）
  status: BOMStatus

  // 客户信息
  customerId: string
  customerName?: string
  contactPersonId?: string
  contactPersonName?: string
  contactEmail?: string

  // 业务信息
  salesUserId?: string
  salesUserName?: string
  bomDate: string                     // 创建/需求日期
  bomType?: BOMType                   // 需求类型
  source?: BOMSource                  // 来源
  industry?: string
  product?: string
  currency?: string                   // 结算货币
  remark?: string

  // 统计
  itemCount?: number
  quotedCount?: number                // 已报价明细数
  totalAmount?: number                // 报价总金额（RMB）

  createdAt?: string
  updatedAt?: string
  createdBy?: string
  items?: BOMItem[]
}

// ============================================================
// BOM 明细（每条询价物料）
// ============================================================
export interface BOMItem {
  id: string
  bomId: string
  lineNo: number

  // 物料信息
  customerMaterialModel?: string      // 客户物料型号
  materialModel?: string              // 物料型号（MPN）
  customerBrand?: string              // 客户品牌
  brand?: string                      // 品牌

  // 价格与数量
  targetPrice?: number                // 目标价
  currency?: string                   // 货币（RMB/USD/EUR/HKD）
  quantity: number                    // 数量

  // 起订量
  minPackageQty?: number
  minOrderQty?: number

  // 可替代料
  alternativeMaterials?: string

  // 报价结果
  quoteStatus: BOMItemQuoteStatus
  quotedPrice?: number                // 报价单价
  quotedCurrency?: string             // 报价货币
  quotedStock?: number                // 可供货数量
  quotedBrand?: string                // 报价品牌
  quotedDeliveryDays?: number         // 交货天数
  quotedAt?: string                   // 报价时间
  quotedBy?: string                   // 报价人（系统/人工）
  quoteRemark?: string                // 报价备注

  remark?: string
}

// ============================================================
// 请求/响应类型
// ============================================================

export interface BOMSearchRequest {
  pageNumber?: number
  pageSize?: number
  keyword?: string
  customerId?: string
  salesUserId?: string
  status?: BOMStatus | ''
  bomType?: BOMType | ''
  source?: BOMSource | ''
  startDate?: string
  endDate?: string
}

export interface BOMSearchResponse {
  items: BOM[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// 新建 BOM 请求
export interface CreateBOMRequest {
  customerId: string
  contactId?: string
  contactEmail?: string
  salesUserId?: string
  bomDate?: string
  bomType?: BOMType
  source?: BOMSource
  industry?: string
  product?: string
  currency?: string
  remark?: string
  items: CreateBOMItemRequest[]
}

// 新建 BOM 明细请求
export interface CreateBOMItemRequest {
  lineNo?: number
  customerMpn?: string              // 客户物料型号
  mpn?: string                      // 物料型号（MPN）
  customerBrand?: string
  brand?: string
  targetPrice?: number
  priceCurrency?: number            // 货币枚举：1=RMB,2=USD,3=EUR,4=HKD
  currency?: string                 // UI 展示用
  quantity: number
  minPackageQty?: number
  moq?: number
  alternatives?: string
  remark?: string
  // 内部 UI 状态
  _key?: number
  _hasError?: boolean
  _errorMsg?: string
}

// 更新 BOM 请求
export interface UpdateBOMRequest {
  customerId?: string
  contactId?: string
  contactEmail?: string
  salesUserId?: string
  bomType?: BOMType
  source?: BOMSource
  industry?: string
  product?: string
  currency?: string
  remark?: string
}

// 一键快速报价请求
export interface AutoQuoteBOMRequest {
  bomId: string
  itemIds?: string[]                 // 为空则对所有待报价明细报价
}

// 一键快速报价响应
export interface AutoQuoteBOMResponse {
  bomId: string
  totalItems: number
  quotedItems: number
  noStockItems: number
  failedItems: number
  message?: string
}

// 人工报价/修改报价请求
export interface ManualQuoteBOMItemRequest {
  quotedPrice: number
  quotedCurrency?: string
  quotedStock?: number
  quotedBrand?: string
  quotedDeliveryDays?: number
  quoteRemark?: string
}

// 删除 BOM 请求
export interface DeleteBOMRequest {
  ids: string[]
}
