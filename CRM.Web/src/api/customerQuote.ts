import apiClient from './client'

export interface CustomerQuoteDraftRow {
  id: string
  sourceQuoteItemId: string
  sourceQuoteId?: string | null
  rfqItemId?: string | null
  customerId?: string | null
  customerName?: string | null
  salesUserId?: string | null
  salesUserName?: string | null
  purchaseUserId?: string | null
  purchaseUserName?: string | null
  mpn?: string | null
  brand?: string | null
  quantity: number
  purchasePrice: number
  purchaseCurrency: number
  customerMpn?: string | null
  customerBrand?: string | null
  sourceQuoteCode?: string | null
  sourceQuoteDate?: string | null
  leadTime?: string | null
  dateCode?: string | null
  remark?: string | null
  status: number
  createTime?: string | null
}

export interface CustomerQuoteItemRow {
  id: string
  lineNo: number
  sourceQuoteItemId: string
  mpn?: string | null
  brand?: string | null
  quantity: number
  purchasePrice: number
  purchaseCurrency: number
  sendPrice: number
  sendCurrency: number
  isLocked: boolean
  customerMpn?: string | null
  customerBrand?: string | null
  leadTime?: string | null
  dateCode?: string | null
  remark?: string | null
  sourceQuoteCode?: string | null
  sourceQuoteDate?: string | null
  purchaseUserId?: string | null
  purchaseUserName?: string | null
}

export interface CustomerQuoteRow {
  id: string
  groupId: string
  customerQuoteCode: string
  versionNo: number
  displayCode?: string
  status: number
  customerId?: string | null
  customerName?: string | null
  customerContactId?: string | null
  contactName?: string | null
  contactEmail?: string | null
  salesUserId?: string | null
  salesUserName?: string | null
  profitFactor: number
  sentAt?: string | null
  createTime?: string | null
  items?: CustomerQuoteItemRow[]
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface UpdateCustomerQuotePayload {
  customerContactId?: string | null
  contactName?: string | null
  contactEmail?: string | null
  profitFactor?: number
  items?: Array<{
    id: string
    sendPrice?: number
    sendCurrency?: number
    isLocked?: boolean
    leadTime?: string | null
    dateCode?: string | null
    remark?: string | null
  }>
}

export const customerQuoteApi = {
  getDrafts(params: { page?: number; pageSize?: number } = {}) {
    return apiClient.get<PagedResult<CustomerQuoteDraftRow>>('/api/v1/customer-quote-drafts', {
      params
    })
  },

  addDraft(body: { quoteItemId?: string; quoteId?: string }) {
    return apiClient.post<CustomerQuoteDraftRow | CustomerQuoteDraftRow[]>(
      '/api/v1/customer-quote-drafts',
      body
    )
  },

  deleteDraft(id: string) {
    return apiClient.delete<void>(`/api/v1/customer-quote-drafts/${id}`)
  },

  generateQuote(draftIds: string[]) {
    return apiClient.post<CustomerQuoteRow>('/api/v1/customer-quote-drafts/generate-quote', {
      draftIds
    })
  },

  getQuotes(params: {
    page?: number
    pageSize?: number
    status?: number
    keyword?: string
  } = {}) {
    return apiClient.get<PagedResult<CustomerQuoteRow>>('/api/v1/customer-quotes', { params })
  },

  getQuoteById(id: string) {
    return apiClient.get<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}`)
  },

  updateQuote(id: string, payload: UpdateCustomerQuotePayload) {
    return apiClient.put<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}`, payload)
  },

  applyProfitFactor(id: string) {
    return apiClient.post<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}/apply-profit-factor`)
  }
}
