import apiClient from './client'

export type BizBrandRow = {
  id: number
  brandEName?: string | null
  brandCName?: string | null
  standardBrand?: string | null
  alias?: string | null
  countryCode?: string | null
  country?: string | null
  remark?: string | null
  createByUserId?: string | null
  createUserName?: string | null
  createTime?: string | null
  auditStatus?: number | null
  auditByUserId?: string | null
  auditUserName?: string | null
  auditTime?: string | null
}

export type BizBrandPaged = {
  items: BizBrandRow[]
  total: number
}

export type UpsertBizBrandPayload = {
  brandEName?: string | null
  brandCName?: string | null
  standardBrand?: string | null
  alias?: string | null
  countryCode?: string | null
  country?: string | null
  remark?: string | null
}

/** 品牌下拉选项（RFQ 等场景） */
export type BizBrandOption = {
  id: number
  standardBrand?: string | null
  auditStatus?: number | null
  brandEName?: string | null
  brandCName?: string | null
  alias?: string | null
}

function normalizeBizBrandOption(raw: Record<string, unknown>): BizBrandOption {
  const id = Number(raw.id ?? raw.Id ?? 0)
  return {
    id: Number.isFinite(id) && id > 0 ? id : 0,
    standardBrand: pickString(raw.standardBrand ?? raw.StandardBrand),
    auditStatus: pickNumber(raw.auditStatus ?? raw.AuditStatus),
    brandEName: pickString(raw.brandEName ?? raw.BrandEName),
    brandCName: pickString(raw.brandCName ?? raw.BrandCName),
    alias: pickString(raw.alias ?? raw.Alias)
  }
}

export function bizBrandRowToOption(row: BizBrandRow): BizBrandOption {
  return {
    id: row.id,
    standardBrand: row.standardBrand,
    auditStatus: row.auditStatus,
    brandEName: row.brandEName,
    brandCName: row.brandCName
  }
}

const BASE = '/api/v1/biz-brands'

function pickString(v: unknown): string | null | undefined {
  if (v == null) return v as null | undefined
  if (typeof v === 'string') return v
  if (typeof v === 'number' || typeof v === 'boolean') return String(v)
  return null
}

function pickNumber(v: unknown): number | null | undefined {
  if (v == null || v === '') return v === '' ? null : (v as null | undefined)
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

function normalizeBizBrandRow(raw: Record<string, unknown>): BizBrandRow {
  return {
    id: Number(raw.id ?? raw.Id ?? 0),
    brandEName: pickString(raw.brandEName ?? raw.BrandEName),
    brandCName: pickString(raw.brandCName ?? raw.BrandCName),
    standardBrand: pickString(raw.standardBrand ?? raw.StandardBrand),
    alias: pickString(raw.alias ?? raw.Alias),
    countryCode: pickString(raw.countryCode ?? raw.CountryCode),
    country: pickString(raw.country ?? raw.Country),
    remark: pickString(raw.remark ?? raw.Remark),
    createByUserId: pickString(raw.createByUserId ?? raw.CreateByUserId),
    createUserName: pickString(raw.createUserName ?? raw.CreateUserName),
    createTime: pickString(raw.createTime ?? raw.CreateTime),
    auditStatus: pickNumber(raw.auditStatus ?? raw.AuditStatus),
    auditByUserId: pickString(raw.auditByUserId ?? raw.AuditByUserId),
    auditUserName: pickString(raw.auditUserName ?? raw.AuditUserName),
    auditTime: pickString(raw.auditTime ?? raw.AuditTime)
  }
}

export const bizBrandApi = {
  async fetchList(params: {
    brandCName?: string
    brandEName?: string
    standardBrand?: string
    alias?: string
    country?: string
    remark?: string
    auditStatus?: number
    createTimeFrom?: string
    createTimeTo?: string
    exactMatch?: boolean
    page?: number
    pageSize?: number
  }): Promise<BizBrandPaged> {
    const raw = await apiClient.get<Record<string, unknown>>(BASE, {
      params: {
        brandCName: params.brandCName?.trim() || undefined,
        brandEName: params.brandEName?.trim() || undefined,
        standardBrand: params.standardBrand?.trim() || undefined,
        alias: params.alias?.trim() || undefined,
        country: params.country?.trim() || undefined,
        remark: params.remark?.trim() || undefined,
        auditStatus: params.auditStatus ?? undefined,
        createTimeFrom: params.createTimeFrom || undefined,
        createTimeTo: params.createTimeTo || undefined,
        exactMatch: params.exactMatch ? true : undefined,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20
      }
    })
    const itemsRaw = (raw.items ?? raw.Items) as Record<string, unknown>[] | undefined
    return {
      items: (itemsRaw ?? []).map((row) => normalizeBizBrandRow(row)),
      total: Number(raw.total ?? raw.Total ?? 0)
    }
  },

  async update(id: number, body: UpsertBizBrandPayload): Promise<BizBrandRow> {
    const raw = await apiClient.put<Record<string, unknown>>(`${BASE}/${id}`, body)
    return normalizeBizBrandRow(raw)
  },

  async create(body: UpsertBizBrandPayload): Promise<BizBrandRow> {
    const raw = await apiClient.post<Record<string, unknown>>(BASE, body)
    return normalizeBizBrandRow(raw)
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`${BASE}/${id}`)
  },

  async approve(id: number): Promise<BizBrandRow> {
    const raw = await apiClient.post<Record<string, unknown>>(`${BASE}/${id}/approve`, {})
    return normalizeBizBrandRow(raw)
  },

  async fetchOptions(params?: { keyword?: string; pageSize?: number }): Promise<BizBrandOption[]> {
    const raw = await apiClient.get<Record<string, unknown>[] | Record<string, unknown>>(`${BASE}/options`, {
      params: {
        keyword: params?.keyword?.trim() || undefined,
        pageSize: params?.pageSize ?? 50
      }
    })
    const list = Array.isArray(raw) ? raw : ((raw.items ?? raw.Items) as Record<string, unknown>[] | undefined)
    return (list ?? []).map((row) => normalizeBizBrandOption(row))
  },

  async getById(id: number): Promise<BizBrandRow> {
    const raw = await apiClient.get<Record<string, unknown>>(`${BASE}/${id}`)
    return normalizeBizBrandRow(raw)
  },

  async rememberLearnedMapping(body: { sourceText: string; brandId: number }): Promise<void> {
    await apiClient.post(`${BASE}/learned-mappings/remember`, body)
  },

  async resolveLearnedMappings(body: { sourceTexts: string[] }): Promise<
    Array<{ sourceText: string; sourceKey: string; brandId: number; standardBrand?: string | null }>
  > {
    const raw = await apiClient.post<
      Array<Record<string, unknown>> | Record<string, unknown>
    >(`${BASE}/learned-mappings/resolve`, body)
    const list = Array.isArray(raw) ? raw : ((raw.items ?? raw.Items) as Record<string, unknown>[] | undefined)
    return (list ?? []).map((row) => ({
      sourceText: String(row.sourceText ?? row.SourceText ?? ''),
      sourceKey: String(row.sourceKey ?? row.SourceKey ?? ''),
      brandId: Number(row.brandId ?? row.BrandId ?? 0),
      standardBrand: pickString(row.standardBrand ?? row.StandardBrand)
    }))
  }
}
