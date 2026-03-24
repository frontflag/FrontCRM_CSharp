// ============================================================
// 财务模块 API 调用层
// 对应后端路由: /api/v1/finance/*
// 实体: FinancePayment / FinanceReceipt / FinancePurchaseInvoice / FinanceSellInvoice
// ============================================================
import apiClient from './client'

// ==================== 类型定义 ====================

export interface FinancePayment {
  id: string
  financePaymentCode: string
  vendorId: string
  vendorName?: string
  salesUserId?: string
  status: number       // 1新建 2待审核 10审核通过 100付款完成 -1审核失败 -2取消
  paymentAmount: number
  paymentCurrency: number  // 1:人民币 2:美元 3:欧元
  paymentDate?: string
  paymentUserId?: string
  paymentMode: number  // 1:银行转账 2:现金 3:支票 4:承兑汇票
  remark?: string
  createdAt?: string
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
  remark?: string
  createdAt?: string
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
  updateStatus: (id: string, status: number) =>
    apiClient.patch(`${PURCHASE_INVOICE_BASE}/${id}/status`, { status }),
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
  updateStatus: (id: string, status: number) =>
    apiClient.patch(`${SELL_INVOICE_BASE}/${id}/status`, { status }),
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

export const CURRENCY_MAP: Record<number, string> = {
  1: 'CNY',
  2: 'USD',
  3: 'EUR',
}

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
