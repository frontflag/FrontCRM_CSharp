import apiClient, { type ApiRejectedError } from './client'
import { fetchCompanyProfileForReport } from './companyProfile'

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
  createByUserId?: string | null
  createByUserName?: string | null
  profitFactor: number
  sentAt?: string | null
  sentByEmail?: boolean
  remark?: string | null
  itemCount?: number
  createTime?: string | null
  items?: CustomerQuoteItemRow[]
}

export interface CustomerQuoteActionLogRow {
  id: string
  operationTime: string
  operatorUserName?: string | null
  actionType: string
  remark?: string | null
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
  remark?: string | null
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

export interface CustomerQuoteBillTo {
  company?: string | null
  companyEn?: string | null
  attn?: string | null
  tel?: string | null
  email?: string | null
  address?: string | null
}

export interface CustomerQuoteReportData {
  quote: CustomerQuoteRow
  billTo: CustomerQuoteBillTo
  companyProfile: {
    basicInfos?: Array<{
      id: string
      isDefault?: boolean
      isDefaultRmb?: boolean
      isDefaultForeign?: boolean
      enabled?: boolean
      companyName?: string
      address?: string
      phone?: string
      fax?: string
      email?: string
    }>
    bankInfos?: Array<{
      id: string
      isDefault?: boolean
      enabled?: boolean
      bankName?: string
      accountName?: string
      bankAddress?: string
      swift?: string
      iban?: string
      accountNumber?: string
      currency?: string
    }>
    logos?: Array<{
      id: string
      isDefault?: boolean
      enabled?: boolean
      logoName?: string
      documentId?: string
      fileName?: string
    }>
  }
}

export interface SendCustomerQuoteEmailPayload {
  to: string
  pdfBase64: string
  fileName?: string
  subject?: string
  body?: string
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

  async getReportData(id: string) {
    const enc = encodeURIComponent(id)
    try {
      return await apiClient.get<CustomerQuoteReportData>(
        `/api/v1/customer-quotes/${enc}/report-data`
      )
    } catch (e: unknown) {
      const status =
        typeof e === 'object' && e !== null ? (e as ApiRejectedError).httpStatus : undefined
      if (status !== 404) throw e
      const quote = await apiClient.get<CustomerQuoteRow>(`/api/v1/customer-quotes/${enc}`)
      let companyProfile: CustomerQuoteReportData['companyProfile']
      try {
        companyProfile = await fetchCompanyProfileForReport()
      } catch {
        companyProfile = { basicInfos: [], bankInfos: [], logos: [] }
      }
      return {
        quote,
        billTo: {
          company: quote.customerName,
          attn: quote.contactName,
          email: quote.contactEmail
        },
        companyProfile
      }
    }
  },

  sendEmail(id: string, payload: SendCustomerQuoteEmailPayload) {
    return apiClient.post<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}/send-email`, payload, {
      timeout: 120_000
    })
  },

  updateQuote(id: string, payload: UpdateCustomerQuotePayload) {
    return apiClient.put<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}`, payload)
  },

  markSent(id: string) {
    return apiClient.post<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}/mark-sent`)
  },

  deleteQuote(id: string) {
    return apiClient.delete<void>(`/api/v1/customer-quotes/${id}`)
  },

  applyProfitFactor(id: string) {
    return apiClient.post<CustomerQuoteRow>(`/api/v1/customer-quotes/${id}/apply-profit-factor`)
  },

  getActionLogs(id: string) {
    return apiClient.get<CustomerQuoteActionLogRow[]>(`/api/v1/customer-quotes/${id}/action-logs`)
  },

  appendActionLog(id: string, payload: { action: 'print' | 'export'; remark?: string }) {
    return apiClient.post<void>(`/api/v1/customer-quotes/${id}/action-logs`, payload)
  }
}
