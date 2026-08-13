import apiClient from './client'

const BASE = '/api/v1/finance/sell-invoice-write-offs'

export interface FinanceSellInvoiceWriteOffCustomerSummary {
  customerId: string
  customerName?: string
  customerEnglishName?: string
  currency: number
  pendingWriteOffTotal: number
  pendingInvoiceCount: number
  earliestInvoiceDate?: string
  latestInvoiceDate?: string
  salesUserId?: string
  salesUserName?: string
  hasOpenReceivable: boolean
}

export interface FinanceSellInvoiceWriteOffInvoiceRow {
  id: string
  invoiceCode?: string
  invoiceNo?: string
  invoiceDate?: string
  invoiceAmount: number
  matchDone: number
  matchToBe: number
  matchStatus: number
  currency: number
  type: number
  invoiceStatus: number
  sellInvoiceType: number
}

export interface FinanceSellInvoiceWriteOffReceivableRow {
  financeReceivableId: string
  receivableCode?: string
  stockOutId?: string
  stockOutCode?: string
  stockOutDate?: string
  sellOrderCode?: string
  salesUserId?: string
  salesUserName?: string
  freightForwarderOrderNo?: string
  stockInCode?: string
  amount: number
  invoiceMatchDone: number
  invoiceMatchToBe: number
  invoiceMatchStatus: number
  currency: number
  customerName?: string
  customerEnglishName?: string
  stockOutTotalQuantity?: number
  stockOutTotalAmount?: number
}

export interface FinanceSellInvoiceWriteOffCandidates {
  customerId: string
  customerName?: string
  customerEnglishName?: string
  currency: number
  invoices: FinanceSellInvoiceWriteOffInvoiceRow[]
  receivables: FinanceSellInvoiceWriteOffReceivableRow[]
}

export interface FinanceSellInvoiceWriteOffAllocation {
  financeReceivableId: string
  amount: number
}

export interface FinanceSellInvoiceWriteOffRequest {
  financeSellInvoiceId: string
  allocations: FinanceSellInvoiceWriteOffAllocation[]
}

export interface FinanceSellInvoiceWriteOffResult {
  financeSellInvoiceId: string
  appliedTotal: number
  allocationCount: number
}

export const financeSellInvoiceWriteOffApi = {
  getCustomerSummaries(keyword?: string) {
    return apiClient.get<FinanceSellInvoiceWriteOffCustomerSummary[]>(`${BASE}/customer-summaries`, {
      params: keyword?.trim() ? { keyword: keyword.trim() } : undefined
    })
  },

  getCandidates(customerId: string, currency: number) {
    return apiClient.get<FinanceSellInvoiceWriteOffCandidates>(`${BASE}/candidates`, {
      params: { customerId, currency }
    })
  },

  apply(data: FinanceSellInvoiceWriteOffRequest) {
    return apiClient.post<FinanceSellInvoiceWriteOffResult>(BASE, data)
  }
}
