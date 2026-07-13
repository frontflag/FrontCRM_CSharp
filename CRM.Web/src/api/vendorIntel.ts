import apiClient from './client'

export type VendorIntelReportSummary = {
  id: string
  vendorId?: string | null
  companyName: string
  creditCode?: string | null
  source: string
  isLatest: boolean
  createdBy?: string | null
  createdByUserName?: string | null
  createdAt: string
}

export type VendorIntelReportDetail = VendorIntelReportSummary & {
  report?: Record<string, unknown> | null
  schemaVersion?: string
  invocationLogId?: string | null
  fromCache?: boolean
}

export type VendorIntelInvestigateResult = {
  report: VendorIntelReportDetail
  fromCache: boolean
  invocationId: string
}

export type VendorIntelInvestigateRequest = {
  vendorId?: string | null
  companyName: string
  creditCode?: string | null
  region?: string | null
  forceRefresh?: boolean
}

export const vendorIntelApi = {
  async investigate(body: VendorIntelInvestigateRequest): Promise<VendorIntelInvestigateResult> {
    return apiClient.post<VendorIntelInvestigateResult>('/api/v1/vendor-intel-reports/investigate', body, {
      timeout: 180000
    })
  },

  async getLatestByVendorId(vendorId: string): Promise<VendorIntelReportDetail | null> {
    const raw = await apiClient.get<VendorIntelReportDetail | null>(
      `/api/v1/vendors/${encodeURIComponent(vendorId)}/intel-reports/latest`
    )
    return raw ?? null
  },

  async listByVendorId(vendorId: string, take = 20): Promise<VendorIntelReportSummary[]> {
    const raw = await apiClient.get<VendorIntelReportSummary[]>(
      `/api/v1/vendors/${encodeURIComponent(vendorId)}/intel-reports`,
      { params: { take } }
    )
    return Array.isArray(raw) ? raw : []
  },

  async getById(reportId: string): Promise<VendorIntelReportDetail> {
    return apiClient.get<VendorIntelReportDetail>(
      `/api/v1/vendor-intel-reports/${encodeURIComponent(reportId)}`
    )
  }
}
