import type { LocationQuery } from 'vue-router'

/** 与供应商列表、URL query 同步的筛选字段（不含分页） */
export interface VendorListFilterQuery {
  searchTerm: string
  /** 工作流状态：1 新建、2 待审核、10 已审核 等 */
  status?: number
  /** 等级 VendorLevelCode */
  level?: number
  /** 身份（vendorinfo.Credit，VendorIdentityCode） */
  credit?: number
  /** 类型：1 专属 2 公海（AscriptionType） */
  ascriptionType?: number
  industry?: string
  purchaseUserId?: string
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

function parseOptionalNumber(raw: string | undefined): number | undefined {
  if (raw === undefined || raw === '') return undefined
  const n = Number(raw)
  return Number.isFinite(n) ? n : undefined
}

export function parseVendorListQuery(query: LocationQuery): VendorListFilterQuery {
  const searchTerm =
    firstParam(query, 'searchTerm') ?? firstParam(query, 'keyword') ?? ''
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
    status,
    level: parseOptionalNumber(firstParam(query, 'level')),
    credit: parseOptionalNumber(firstParam(query, 'credit')),
    ascriptionType: parseOptionalNumber(firstParam(query, 'ascriptionType')),
    industry: firstParam(query, 'industry')?.trim() || undefined,
    purchaseUserId: firstParam(query, 'purchaseUserId')?.trim() || undefined,
    createdFrom: firstParam(query, 'createdFrom')?.trim() || undefined,
    createdTo: firstParam(query, 'createdTo')?.trim() || undefined,
    favoriteOnly
  }
}

/** 仅包含非默认/有值的项，便于分享链接 */
export function buildVendorListQuery(f: VendorListFilterQuery): Record<string, string> {
  const q: Record<string, string> = {}
  if (f.searchTerm.trim()) q.searchTerm = f.searchTerm.trim()
  if (f.status !== undefined && f.status !== null && !Number.isNaN(f.status)) {
    q.status = String(f.status)
  }
  if (f.level !== undefined && f.level !== null && !Number.isNaN(f.level)) {
    q.level = String(f.level)
  }
  if (f.credit !== undefined && f.credit !== null && !Number.isNaN(f.credit)) {
    q.credit = String(f.credit)
  }
  if (f.ascriptionType !== undefined && f.ascriptionType !== null && !Number.isNaN(f.ascriptionType)) {
    q.ascriptionType = String(f.ascriptionType)
  }
  if (f.industry?.trim()) q.industry = f.industry.trim()
  if (f.purchaseUserId?.trim()) q.purchaseUserId = f.purchaseUserId.trim()
  if (f.createdFrom?.trim()) q.createdFrom = f.createdFrom.trim()
  if (f.createdTo?.trim()) q.createdTo = f.createdTo.trim()
  if (f.favoriteOnly) q.favoriteOnly = '1'
  return q
}
