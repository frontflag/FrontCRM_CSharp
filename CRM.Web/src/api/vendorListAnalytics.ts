import apiClient from './client'
import type { PurchaseAnalyticsVendor } from './analytics/purchase'

/** 供应商列表看板查询（与列表 search 同源 listFilter） */
export interface VendorListAnalyticsQuery {
  searchTerm?: string
  status?: number
  level?: number
  industry?: string
  currency?: number
  credit?: number
  ascriptionType?: number
  purchaseUserId?: string
  createdFrom?: string
  createdTo?: string
  favoriteOnly?: boolean
  favoriteIds?: string
  quickFilter?: string
}

function buildParams(q: VendorListAnalyticsQuery): Record<string, string | number | boolean> {
  const p: Record<string, string | number | boolean> = {}
  if (q.searchTerm) p.searchTerm = q.searchTerm
  if (q.status !== undefined && q.status !== null) p.status = q.status
  if (q.level !== undefined && q.level !== null) p.level = q.level
  if (q.industry) p.industry = q.industry
  if (q.currency !== undefined && q.currency !== null) p.currency = q.currency
  if (q.credit !== undefined && q.credit !== null) p.credit = q.credit
  if (q.ascriptionType !== undefined && q.ascriptionType !== null) p.ascriptionType = q.ascriptionType
  if (q.purchaseUserId) p.purchaseUserId = q.purchaseUserId
  if (q.createdFrom) p.createdFrom = q.createdFrom
  if (q.createdTo) p.createdTo = q.createdTo
  if (q.favoriteOnly) p.favoriteOnly = true
  if (q.favoriteIds) p.favoriteIds = q.favoriteIds
  if (q.quickFilter) p.quickFilter = q.quickFilter
  return p
}

export const vendorListAnalyticsApi = {
  getVendor(query: VendorListAnalyticsQuery): Promise<PurchaseAnalyticsVendor> {
    return apiClient.get<PurchaseAnalyticsVendor>('/api/v1/vendors/analytics/vendor', {
      params: buildParams(query)
    })
  }
}
