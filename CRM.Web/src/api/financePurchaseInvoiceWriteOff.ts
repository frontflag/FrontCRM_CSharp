import apiClient from './client'

const BASE = '/api/v1/finance/purchase-invoice-write-offs'

export interface FinancePurchaseInvoiceWriteOffVendorSummary {
  vendorId: string
  vendorName?: string
  vendorEnglishName?: string
  currency: number
  pendingWriteOffTotal: number
  pendingInvoiceCount: number
  earliestInvoiceDate?: string
  latestInvoiceDate?: string
  hasOpenStockIn: boolean
}

export interface FinancePurchaseInvoiceWriteOffInvoiceRow {
  id: string
  invoiceCode?: string
  invoiceNo?: string
  invoiceDate?: string
  invoiceAmount: number
  verifiedDone: number
  verifiedToBe: number
  verificationStatus: number
  currency: number
  confirmStatus: number
  redInvoiceStatus: number
}

export interface FinancePurchaseInvoiceWriteOffStockInItemRow {
  stockInItemId: string
  stockInItemCode?: string
  amount: number
  invoiceMatchDone: number
  invoiceMatchToBe: number
  invoiceMatchStatus: number
  currency?: number
  purchaseOrderItemId?: string
  purchaseOrderItemCode?: string
  purchaseOrderCode?: string
  purchaseUserId?: string
  purchaseUserName?: string
  freightForwarderOrderNo?: string
}

export interface FinancePurchaseInvoiceWriteOffStockInRow {
  stockInId: string
  stockInCode?: string
  stockInDate?: string
  currency: number
  totalAmount: number
  invoiceMatchDone: number
  invoiceMatchToBe: number
  invoiceMatchStatus: number
  totalQuantity: number
  freightForwarderOrderNo?: string
  purchaseOrderCodes?: string
  purchaseUserId?: string
  purchaseUserName?: string
  vendorName?: string
  vendorEnglishName?: string
  items: FinancePurchaseInvoiceWriteOffStockInItemRow[]
}

export interface FinancePurchaseInvoiceWriteOffCandidates {
  vendorId: string
  vendorName?: string
  vendorEnglishName?: string
  currency: number
  invoices: FinancePurchaseInvoiceWriteOffInvoiceRow[]
  stockIns: FinancePurchaseInvoiceWriteOffStockInRow[]
}

export interface FinancePurchaseInvoiceWriteOffAllocation {
  stockInItemId: string
  amount: number
}

export interface FinancePurchaseInvoiceWriteOffRequest {
  financePurchaseInvoiceId: string
  allocations: FinancePurchaseInvoiceWriteOffAllocation[]
}

export interface FinancePurchaseInvoiceWriteOffResult {
  financePurchaseInvoiceId: string
  appliedTotal: number
  allocationCount: number
}

export const financePurchaseInvoiceWriteOffApi = {
  getVendorSummaries(keyword?: string) {
    return apiClient.get<FinancePurchaseInvoiceWriteOffVendorSummary[]>(`${BASE}/vendor-summaries`, {
      params: keyword?.trim() ? { keyword: keyword.trim() } : undefined
    })
  },

  getCandidates(vendorId: string, currency: number) {
    return apiClient.get<FinancePurchaseInvoiceWriteOffCandidates>(`${BASE}/candidates`, {
      params: { vendorId, currency }
    })
  },

  apply(data: FinancePurchaseInvoiceWriteOffRequest) {
    return apiClient.post<FinancePurchaseInvoiceWriteOffResult>(BASE, data)
  }
}
