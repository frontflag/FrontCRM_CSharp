import { vendorApi } from '@/api/vendor'

export type VendorMatchOption = {
  id: string
  label: string
}

function vendorDisplayName(v: Record<string, unknown>): string {
  return (
    str(v.officialName) ||
    str(v.nickName) ||
    str(v.englishOfficialName) ||
    str(v.code) ||
    '供应商'
  )
}

function str(v: unknown): string {
  if (v == null) return ''
  return String(v).trim()
}

export async function searchVendorsByName(name: string, limit = 20): Promise<VendorMatchOption[]> {
  const term = name.trim()
  if (!term) return []
  const res = await vendorApi.searchVendors({
    pageNumber: 1,
    pageSize: limit,
    searchTerm: term
  })
  return (res.items || []).map((v) => {
    const raw = v as unknown as Record<string, unknown>
    return {
      id: String(v.id ?? raw.id ?? ''),
      label: vendorDisplayName(raw)
    }
  }).filter((o) => o.id)
}

export async function findSimilarVendors(name: string, limit = 5): Promise<VendorMatchOption[]> {
  return searchVendorsByName(name, limit)
}
