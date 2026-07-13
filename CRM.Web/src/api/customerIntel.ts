import apiClient from './client'

export type CustomerIntelReportSummary = {
  id: string
  customerId?: string | null
  companyName: string
  creditCode?: string | null
  source: string
  isLatest: boolean
  createdBy?: string | null
  createdByUserName?: string | null
  createdAt: string
}

export type CustomerIntelReportDetail = CustomerIntelReportSummary & {
  report?: Record<string, unknown> | null
  schemaVersion?: string
  invocationLogId?: string | null
  fromCache?: boolean
}

export type CustomerIntelInvestigateResult = {
  report: CustomerIntelReportDetail
  fromCache: boolean
  invocationId: string
}

export type CustomerIntelInvestigateRequest = {
  customerId?: string | null
  companyName: string
  creditCode?: string | null
  region?: string | null
  forceRefresh?: boolean
}

export const customerIntelApi = {
  async investigate(body: CustomerIntelInvestigateRequest): Promise<CustomerIntelInvestigateResult> {
    return apiClient.post<CustomerIntelInvestigateResult>('/api/v1/customer-intel-reports/investigate', body, {
      timeout: 180000
    })
  },

  async getLatestByCustomerId(customerId: string): Promise<CustomerIntelReportDetail | null> {
    const raw = await apiClient.get<CustomerIntelReportDetail | null>(
      `/api/v1/customers/${encodeURIComponent(customerId)}/intel-reports/latest`
    )
    return raw ?? null
  },

  async listByCustomerId(customerId: string, take = 20): Promise<CustomerIntelReportSummary[]> {
    const raw = await apiClient.get<CustomerIntelReportSummary[]>(
      `/api/v1/customers/${encodeURIComponent(customerId)}/intel-reports`,
      { params: { take } }
    )
    return Array.isArray(raw) ? raw : []
  },

  async getById(reportId: string): Promise<CustomerIntelReportDetail> {
    return apiClient.get<CustomerIntelReportDetail>(
      `/api/v1/customer-intel-reports/${encodeURIComponent(reportId)}`
    )
  }
}
