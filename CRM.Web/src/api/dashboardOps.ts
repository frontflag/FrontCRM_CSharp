import apiClient from './client'
import { buildQueryString } from '@/utils/progressStatusQuery'

export type DashboardOpsDateRange = {
  dateFrom: string
  dateTo: string
}

export interface DashboardOpsTrendPoint {
  period: string
  count: number
}

export interface DashboardLogisticsOverview {
  stockInItemCount: number
  stockOutItemCount: number
  customsDeclarationItemCount: number
  stockInTrends: DashboardOpsTrendPoint[]
  stockOutTrends: DashboardOpsTrendPoint[]
}

export interface DashboardFinanceWriteOffOverview {
  purchaseInvoiceWriteOffCount?: number | null
  receivableWriteOffCount?: number | null
  sellInvoiceWriteOffCount?: number | null
}

function rangeUrl(path: string, range: DashboardOpsDateRange): string {
  const qs = buildQueryString({ dateFrom: range.dateFrom, dateTo: range.dateTo })
  return qs ? `${path}?${qs}` : path
}

const BASE = '/api/v1/dashboard/ops'

export const dashboardOpsApi = {
  getLogistics(range: DashboardOpsDateRange): Promise<DashboardLogisticsOverview> {
    return apiClient.get<DashboardLogisticsOverview>(rangeUrl(`${BASE}/logistics`, range))
  },
  getFinanceWriteOffs(range: DashboardOpsDateRange): Promise<DashboardFinanceWriteOffOverview> {
    return apiClient.get<DashboardFinanceWriteOffOverview>(rangeUrl(`${BASE}/finance-write-offs`, range))
  }
}
