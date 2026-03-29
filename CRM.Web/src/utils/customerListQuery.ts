import type { LocationQuery } from 'vue-router'

/** 与列表栏、URL query 同步的筛选字段（不含分页、排序） */
export interface CustomerListFilterQuery {
  searchTerm: string
  customerType?: number
  /** 等级：D / C / B / BPO / VIP / VPO */
  customerLevel?: string
  industry?: string
  /** 工作流状态，与列表「状态」列一致 */
  status?: number
  /** 业务员用户 ID */
  salesUserId?: string
  /** 创建日期 YYYY-MM-DD */
  createdFrom?: string
  createdTo?: string
  favoriteOnly: boolean
}

function firstParam(q: LocationQuery, key: string): string | undefined {
  const v = q[key]
  if (Array.isArray(v)) return v[0] ?? undefined
  if (v === null || v === undefined) return undefined
  return v
}

export function parseCustomerListQuery(query: LocationQuery): CustomerListFilterQuery {
  const searchTerm = firstParam(query, 'searchTerm') ?? ''
  const ctRaw = firstParam(query, 'customerType')
  let customerType: number | undefined
  if (ctRaw !== undefined && ctRaw !== '') {
    const n = Number(ctRaw)
    customerType = Number.isFinite(n) ? n : undefined
  }
  const industry = firstParam(query, 'industry') || undefined
  const customerLevel = firstParam(query, 'customerLevel') || undefined
  const salesUserId = firstParam(query, 'salesUserId') || undefined
  const createdFrom = firstParam(query, 'createdFrom') || undefined
  const createdTo = firstParam(query, 'createdTo') || undefined
  const stRaw = firstParam(query, 'status')
  let status: number | undefined
  if (stRaw !== undefined && stRaw !== '') {
    const n = Number(stRaw)
    status = Number.isFinite(n) ? n : undefined
  }
  const fav = firstParam(query, 'favoriteOnly')
  const favoriteOnly = fav === '1' || fav === 'true'
  return {
    searchTerm,
    customerType,
    customerLevel,
    industry,
    status,
    salesUserId,
    createdFrom,
    createdTo,
    favoriteOnly
  }
}

/** 仅包含非默认/有值的项，便于分享链接 */
export function buildCustomerListQuery(f: CustomerListFilterQuery): Record<string, string> {
  const q: Record<string, string> = {}
  if (f.searchTerm.trim()) q.searchTerm = f.searchTerm.trim()
  if (f.customerType != null && !Number.isNaN(f.customerType)) q.customerType = String(f.customerType)
  if (f.customerLevel) q.customerLevel = f.customerLevel
  if (f.industry) q.industry = f.industry
  if (f.status !== undefined && f.status !== null && !Number.isNaN(f.status)) q.status = String(f.status)
  if (f.salesUserId) q.salesUserId = f.salesUserId
  if (f.createdFrom) q.createdFrom = f.createdFrom
  if (f.createdTo) q.createdTo = f.createdTo
  if (f.favoriteOnly) q.favoriteOnly = '1'
  return q
}
