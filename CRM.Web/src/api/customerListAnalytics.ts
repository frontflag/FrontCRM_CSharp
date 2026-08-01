import apiClient from './client'
import { mapCustomerLevelToInt } from './customer'
import type { SalesAnalyticsCustomer } from './analytics/sales'

/** 客户列表看板查询（与列表 search 同源 listFilter） */
export interface CustomerListAnalyticsQuery {
  searchTerm?: string
  customerType?: number
  /** 与列表一致：等级码 D/C/B/… 或数字 */
  customerLevel?: string | number
  industry?: string
  currency?: number
  region?: string
  salesUserId?: string
  createdFrom?: string
  createdTo?: string
  status?: number
  favoriteOnly?: boolean
  favoriteIds?: string
  quickFilter?: string
}

function buildParams(q: CustomerListAnalyticsQuery): Record<string, string | number | boolean> {
  const p: Record<string, string | number | boolean> = {}
  if (q.searchTerm) p.searchTerm = q.searchTerm
  if (q.customerType !== undefined && q.customerType !== null) p.customerType = q.customerType
  if (q.customerLevel !== undefined && q.customerLevel !== null && q.customerLevel !== '') {
    p.customerLevel = mapCustomerLevelToInt(q.customerLevel)
  }
  if (q.industry) p.industry = q.industry
  if (q.currency !== undefined && q.currency !== null) p.currency = q.currency
  if (q.region) p.region = q.region
  if (q.salesUserId) p.salesUserId = q.salesUserId
  if (q.createdFrom) p.createdFrom = q.createdFrom
  if (q.createdTo) p.createdTo = q.createdTo
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.favoriteOnly) p.favoriteOnly = true
  if (q.favoriteIds) p.favoriteIds = q.favoriteIds
  if (q.quickFilter) p.quickFilter = q.quickFilter
  return p
}

export const customerListAnalyticsApi = {
  getCustomer(query: CustomerListAnalyticsQuery): Promise<SalesAnalyticsCustomer> {
    return apiClient.get<SalesAnalyticsCustomer>('/api/v1/customers/analytics/customer', {
      params: buildParams(query)
    })
  }
}
