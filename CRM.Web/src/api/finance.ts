// ============================================================
// 财务模块 API 调用层
// 对应后端路由: /api/v1/finance/*
// 实体: FinancePayment / FinanceReceipt / FinancePurchaseInvoice / FinanceSellInvoice
// ============================================================
import apiClient from './client'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'

// ==================== 类型定义 ====================

export interface FinancePayment {
  id: string
  financePaymentCode: string
  vendorId: string
  /** 供应商业务编码（列表/详情由后端填充，不落库） */
  vendorCode?: string
  vendorName?: string
  salesUserId?: string
  status: number       // 1新建 2待审核 10审核通过 100付款完成 -1审核失败 -2取消
  paymentAmount: number
  /** 创建/更新请款时与后端 DTO 对齐（应付金额） */
  paymentAmountToBe?: number
  paymentCurrency: number  // 1:人民币 2:美元 3:欧元
  paymentDate?: string
  paymentUserId?: string
  paymentMode: number  // 1:银行转账 2:现金 3:支票 4:承兑汇票
  bankSlipNo?: string
  remark?: string
  /** 后端 BaseEntity 序列化字段（camelCase: createTime） */
  createTime?: string
  /** 兼容旧前端命名 */
  createdAt?: string
  /** 列表/详情由后端按 CreateByUserId 填充 */
  createUserName?: string
  items?: FinancePaymentItem[]
}

export interface FinancePaymentItem {
  id: string
  financePaymentId: string
  purchaseOrderId?: string
  purchaseOrderItemId?: string
  paymentAmount: number
  paymentAmountToBe: number
  productId?: string
  pn?: string
  brand?: string
  verificationStatus: number  // 0未核销 1部分核销 2核销完成
  verificationDone: number
  verificationToBe: number
}

export interface FinanceReceipt {
  id: string
  financeReceiptCode: string
  customerId: string
  customerName?: string
  salesUserId?: string
  purchaseGroupId?: string
  status: number       // 0草稿 1待审核 2已审核 3已收款 4已取消
  receiptAmount: number
  receiptCurrency: number  // 1:人民币 2:美元 3:欧元
  receiptDate?: string
  receiptUserId?: string
  receiptMode: number  // 1:银行转账 2:现金 3:支票 4:承兑汇票
  receiptBankId?: string
  bankSlipNo?: string
  remark?: string
  createdAt?: string
  /** 列表/详情接口由后端根据 createByUserId 填充 */
  createUserName?: string
  items?: FinanceReceiptItem[]
}

export interface FinanceReceiptItem {
  id: string
  financeReceiptId: string
  sellOrderId?: string
  sellOrderItemId?: string
  receiptAmount: number
  receiptAmountToBe: number
  productId?: string
  pn?: string
  brand?: string
  verificationStatus: number
  verificationDone: number
  verificationToBe: number
}

export interface FinancePurchaseInvoice {
  id: string
  financePurchaseInvoiceCode: string
  vendorId: string
  vendorName?: string
  invoiceNo?: string
  invoiceTotal: number
  makeInvoiceDate?: string
  paymentStatus: number  // 0未付款 1部分付款 2付款完成
  paymentDone: number
  paymentToBe: number
  currency: number
  type: number          // 10:蓝字 20:红字
  invoiceStatus: number // 1未申请 2申请中 100已开票 101开票失败 -1已作废
  purchaseInvoiceType: number  // 100:增值税专用 200:增值税普通
  remark?: string
  createdAt?: string
  items?: FinancePurchaseInvoiceItem[]
}

export interface FinancePurchaseInvoiceItem {
  id: string
  financePurchaseInvoiceId: string
  stockInCode?: string
  purchaseOrderCode?: string
  stockInCost: number
  billCost: number
  billQty: number
  billAmount: number
  taxRate: number
  taxAmount: number
  excludTaxAmount: number
}

function pickStrFromRecord(r: Record<string, unknown>, keys: string[]): string | undefined {
  for (const k of keys) {
    if (!(k in r)) continue
    const v = r[k]
    if (v == null) continue
    const s = String(v).trim()
    if (s) return s
  }
  return undefined
}

function pickNumFromRecord(r: Record<string, unknown>, keys: string[]): number {
  for (const k of keys) {
    if (!(k in r)) continue
    const v = r[k]
    if (v == null || v === '') continue
    const n = typeof v === 'number' ? v : Number(v)
    if (Number.isFinite(n)) return n
  }
  return 0
}

function hasDefinedProp(r: Record<string, unknown>, keys: string[]): boolean {
  return keys.some((k) => k in r && r[k] !== undefined)
}

function toDateYmd(v: unknown): string | undefined {
  if (v == null || v === '') return undefined
  const s = String(v).trim()
  if (!s) return undefined
  const m = s.match(/^(\d{4}-\d{2}-\d{2})/)
  return m ? m[1] : s
}

/**
 * 将接口返回的进项发票实体（含 PascalCase、invoiceAmount、confirmStatus 等）规范为前端使用的 FinancePurchaseInvoice。
 * 后端主表暂无付款进度、发票类型等字段时，金额按 0、状态按初始值处理。
 */
export function normalizeFinancePurchaseInvoice(raw: unknown): FinancePurchaseInvoice {
  const r = raw && typeof raw === 'object' ? (raw as Record<string, unknown>) : {}
  const id = pickStrFromRecord(r, ['id', 'Id']) ?? ''
  const invoiceTotal = pickNumFromRecord(r, ['invoiceTotal', 'InvoiceTotal', 'invoiceAmount', 'InvoiceAmount'])
  const paymentDone = pickNumFromRecord(r, ['paymentDone', 'PaymentDone'])
  let paymentToBe: number
  if (hasDefinedProp(r, ['paymentToBe', 'PaymentToBe'])) {
    paymentToBe = pickNumFromRecord(r, ['paymentToBe', 'PaymentToBe'])
  } else {
    paymentToBe = Math.max(0, invoiceTotal - paymentDone)
  }

  let invoiceStatus: number
  if (hasDefinedProp(r, ['invoiceStatus', 'InvoiceStatus'])) {
    invoiceStatus = pickNumFromRecord(r, ['invoiceStatus', 'InvoiceStatus'])
    if (invoiceStatus === 0) invoiceStatus = 1
  } else {
    const red = pickNumFromRecord(r, ['redInvoiceStatus', 'RedInvoiceStatus'])
    if (red === 1) invoiceStatus = -1
    else {
      const conf = pickNumFromRecord(r, ['confirmStatus', 'ConfirmStatus'])
      invoiceStatus = conf === 1 ? 100 : 1
    }
  }

  let paymentStatus = pickNumFromRecord(r, ['paymentStatus', 'PaymentStatus'])
  if (!hasDefinedProp(r, ['paymentStatus', 'PaymentStatus'])) paymentStatus = 0
  else if (!Number.isFinite(paymentStatus)) paymentStatus = 0

  let purchaseInvoiceType = pickNumFromRecord(r, ['purchaseInvoiceType', 'PurchaseInvoiceType'])
  if (!hasDefinedProp(r, ['purchaseInvoiceType', 'PurchaseInvoiceType'])) purchaseInvoiceType = 100
  else if (!Number.isFinite(purchaseInvoiceType) || purchaseInvoiceType <= 0) purchaseInvoiceType = 100

  const typeVal = pickNumFromRecord(r, ['type', 'Type'])
  const currencyVal = pickNumFromRecord(r, ['currency', 'Currency'])

  const code =
    pickStrFromRecord(r, ['financePurchaseInvoiceCode', 'FinancePurchaseInvoiceCode', 'invoiceCode', 'InvoiceCode']) ||
    id

  const itemsRaw = r.items ?? r.Items
  const items = Array.isArray(itemsRaw) ? (itemsRaw as FinancePurchaseInvoiceItem[]) : undefined

  return {
    id,
    financePurchaseInvoiceCode: code,
    vendorId: pickStrFromRecord(r, ['vendorId', 'VendorId']) ?? '',
    vendorName: pickStrFromRecord(r, ['vendorName', 'VendorName']),
    invoiceNo: pickStrFromRecord(r, ['invoiceNo', 'InvoiceNo']),
    invoiceTotal,
    makeInvoiceDate: toDateYmd(pickStrFromRecord(r, ['makeInvoiceDate', 'MakeInvoiceDate', 'invoiceDate', 'InvoiceDate'])),
    paymentStatus,
    paymentDone,
    paymentToBe,
    currency: currencyVal > 0 ? currencyVal : 1,
    type: typeVal > 0 ? typeVal : 10,
    invoiceStatus,
    purchaseInvoiceType,
    remark: pickStrFromRecord(r, ['remark', 'Remark']),
    createdAt: pickStrFromRecord(r, ['createdAt', 'CreatedAt', 'createTime', 'CreateTime']),
    items,
  }
}

export interface FinanceSellInvoice {
  id: string
  invoiceCode?: string
  customerId: string
  customerName?: string
  invoiceNo?: string
  invoiceTotal: number
  makeInvoiceDate?: string
  receiveStatus: number  // 0未收款 1部分收款 2收款完成
  receiveDone: number
  receiveToBe: number
  currency: number
  type: number           // 10:蓝字 20:红字
  invoiceStatus: number  // 1未申请 2申请中 100已开票 101开票失败 -1已作废
  sellInvoiceType: number  // 100:增值税专用 200:增值税普通
  remark?: string
  createdAt?: string
  items?: SellInvoiceItem[]
}

export interface SellInvoiceItem {
  id: string
  financeSellInvoiceId: string
  invoiceTotal: number
  taxRate: number
  valueAddedTax: number
  taxFreeTotal: number
  price: number
  qty: number
  stockOutItemId?: string
  currency: number
  receiveStatus: number
}

export interface PageQuery {
  page?: number
  pageSize?: number
  keyword?: string
  status?: number
  startDate?: string
  endDate?: string
}

export interface PageResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// ==================== 付款管理 API ====================
const PAYMENT_BASE = '/api/v1/finance/payments'

export const financePaymentApi = {
  getList: (params: PageQuery) =>
    apiClient.get<PageResult<FinancePayment>>(PAYMENT_BASE, { params }),
  getById: (id: string) =>
    apiClient.get<FinancePayment>(`${PAYMENT_BASE}/${id}`),
  create: (data: Partial<FinancePayment>) =>
    apiClient.post<FinancePayment>(PAYMENT_BASE, data),
  update: (id: string, data: Partial<FinancePayment>) =>
    apiClient.put<FinancePayment>(`${PAYMENT_BASE}/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`${PAYMENT_BASE}/${id}`),
  updateStatus: (id: string, status: number) =>
    apiClient.patch(`${PAYMENT_BASE}/${id}/status`, { status }),
  submit: (id: string) =>
    apiClient.post(`${PAYMENT_BASE}/${id}/submit`, {}),
  approve: (id: string, remark?: string) =>
    apiClient.post(`${PAYMENT_BASE}/${id}/approve`, { remark }),
  reject: (id: string, remark: string) =>
    apiClient.post(`${PAYMENT_BASE}/${id}/reject`, { remark }),
  complete: (id: string) =>
    apiClient.post(`${PAYMENT_BASE}/${id}/complete`, {}),
  cancel: (id: string, remark?: string) =>
    apiClient.post(`${PAYMENT_BASE}/${id}/cancel`, { remark }),
}

// ==================== 收款管理 API ====================
const RECEIPT_BASE = '/api/v1/finance/receipts'

export const financeReceiptApi = {
  getList: (params: PageQuery) =>
    apiClient.get<PageResult<FinanceReceipt>>(RECEIPT_BASE, { params }),
  getById: (id: string) =>
    apiClient.get<FinanceReceipt>(`${RECEIPT_BASE}/${id}`),
  create: (data: Partial<FinanceReceipt>) =>
    apiClient.post<FinanceReceipt>(RECEIPT_BASE, data),
  update: (id: string, data: Partial<FinanceReceipt>) =>
    apiClient.put<FinanceReceipt>(`${RECEIPT_BASE}/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`${RECEIPT_BASE}/${id}`),
  updateStatus: (id: string, status: number) =>
    apiClient.patch(`${RECEIPT_BASE}/${id}/status`, { status }),
  submit: (id: string) =>
    apiClient.post(`${RECEIPT_BASE}/${id}/submit`, {}),
  approve: (id: string) =>
    apiClient.post(`${RECEIPT_BASE}/${id}/approve`, {}),
  confirmReceived: (id: string) =>
    apiClient.post(`${RECEIPT_BASE}/${id}/confirm-received`, {}),
  cancel: (id: string) =>
    apiClient.post(`${RECEIPT_BASE}/${id}/cancel`, {}),
}

// ==================== 进项发票 API ====================
const PURCHASE_INVOICE_BASE = '/api/v1/finance/purchase-invoices'

export const financePurchaseInvoiceApi = {
  getList: (params: PageQuery) =>
    apiClient.get<PageResult<FinancePurchaseInvoice>>(PURCHASE_INVOICE_BASE, { params }),
  getById: (id: string) =>
    apiClient.get<FinancePurchaseInvoice>(`${PURCHASE_INVOICE_BASE}/${id}`),
  create: (data: Partial<FinancePurchaseInvoice>) =>
    apiClient.post<FinancePurchaseInvoice>(PURCHASE_INVOICE_BASE, data),
  update: (id: string, data: Partial<FinancePurchaseInvoice>) =>
    apiClient.put<FinancePurchaseInvoice>(`${PURCHASE_INVOICE_BASE}/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`${PURCHASE_INVOICE_BASE}/${id}`),
  confirm: (id: string, confirmDate?: string) =>
    apiClient.post(`${PURCHASE_INVOICE_BASE}/${id}/confirm`, { confirmDate }),
  unconfirm: (id: string) =>
    apiClient.post(`${PURCHASE_INVOICE_BASE}/${id}/unconfirm`, {}),
  redInvoice: (id: string) =>
    apiClient.post(`${PURCHASE_INVOICE_BASE}/${id}/red-invoice`, {}),
}

// ==================== 销项发票 API ====================
const SELL_INVOICE_BASE = '/api/v1/finance/sell-invoices'

export const financeSellInvoiceApi = {
  getList: (params: PageQuery) =>
    apiClient.get<PageResult<FinanceSellInvoice>>(SELL_INVOICE_BASE, { params }),
  getById: (id: string) =>
    apiClient.get<FinanceSellInvoice>(`${SELL_INVOICE_BASE}/${id}`),
  create: (data: Partial<FinanceSellInvoice>) =>
    apiClient.post<FinanceSellInvoice>(SELL_INVOICE_BASE, data),
  update: (id: string, data: Partial<FinanceSellInvoice>) =>
    apiClient.put<FinanceSellInvoice>(`${SELL_INVOICE_BASE}/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`${SELL_INVOICE_BASE}/${id}`),
  updateInvoiceStatus: (id: string, invoiceStatus: number) =>
    apiClient.patch(`${SELL_INVOICE_BASE}/${id}/invoice-status`, { invoiceStatus }),
  submitApplication: (id: string) =>
    apiClient.post(`${SELL_INVOICE_BASE}/${id}/submit-application`, {}),
  markIssued: (id: string) =>
    apiClient.post(`${SELL_INVOICE_BASE}/${id}/mark-issued`, {}),
  markIssueFailed: (id: string) =>
    apiClient.post(`${SELL_INVOICE_BASE}/${id}/mark-issue-failed`, {}),
  void: (id: string) =>
    apiClient.post(`${SELL_INVOICE_BASE}/${id}/void`, {}),
}

// ==================== 枚举辅助 ====================
export const PAYMENT_STATUS_MAP: Record<number, { label: string; type: string }> = {
  1: { label: '新建', type: 'info' },
  2: { label: '待审核', type: 'warning' },
  10: { label: '审核通过', type: 'primary' },
  100: { label: '付款完成', type: 'success' },
  [-1]: { label: '审核失败', type: 'danger' },
  [-2]: { label: '取消', type: 'info' },
}

export const RECEIPT_STATUS_MAP: Record<number, { label: string; type: string }> = {
  0: { label: '草稿', type: 'info' },
  1: { label: '待审核', type: 'warning' },
  2: { label: '已审核', type: 'primary' },
  3: { label: '已收款', type: 'success' },
  4: { label: '已取消', type: 'danger' },
}

export const INVOICE_STATUS_MAP: Record<number, { label: string; type: string }> = {
  1: { label: '未申请', type: 'info' },
  2: { label: '申请中', type: 'warning' },
  100: { label: '已开票', type: 'success' },
  101: { label: '开票失败', type: 'danger' },
  [-1]: { label: '已作废', type: 'danger' },
}

export const PAYMENT_MODE_MAP: Record<number, string> = {
  1: '银行转账',
  2: '现金',
  3: '支票',
  4: '承兑汇票',
}

export const CURRENCY_MAP: Record<number, string> = CURRENCY_CODE_TO_TEXT

export const INVOICE_TYPE_MAP: Record<number, string> = {
  10: '蓝字发票',
  20: '红字发票',
}

export const PURCHASE_INVOICE_TYPE_MAP: Record<number, string> = {
  100: '增值税专用发票',
  200: '增值税普通发票',
}

export const SELL_INVOICE_TYPE_MAP: Record<number, string> = {
  100: '增值税专用发票',
  200: '增值税普通发票',
}

export const RECEIVE_STATUS_MAP: Record<number, { label: string; type: string }> = {
  0: { label: '未收款', type: 'info' },
  1: { label: '部分收款', type: 'warning' },
  2: { label: '收款完成', type: 'success' },
}

export const PAYMENT_DONE_STATUS_MAP: Record<number, { label: string; type: string }> = {
  0: { label: '未付款', type: 'info' },
  1: { label: '部分付款', type: 'warning' },
  2: { label: '付款完成', type: 'success' },
}
