import { customerApi } from '@/api/customer'

export type CustomerMatchOption = {
  id: string
  label: string
}

function customerDisplayName(c: Record<string, unknown>): string {
  return (
    str(c.customerName) ||
    str(c.officialName) ||
    str(c.customerShortName) ||
    str(c.nickName) ||
    str(c.customerCode) ||
    '客户'
  )
}

function str(v: unknown): string {
  if (v == null) return ''
  return String(v).trim()
}

/** 按名称模糊搜索客户（与 RFQ 客户下拉同 API） */
export async function searchCustomersByName(name: string, limit = 20): Promise<CustomerMatchOption[]> {
  const term = name.trim()
  if (!term) return []
  const res = await customerApi.searchCustomers({
    pageNumber: 1,
    pageSize: limit,
    searchTerm: term
  })
  return (res.items || []).map((c) => {
    const raw = c as unknown as Record<string, unknown>
    return {
      id: String(c.id ?? raw.id ?? ''),
      label: customerDisplayName(raw)
    }
  }).filter((o) => o.id)
}

/** 查重提示：用解析出的客户名称搜索相似记录 */
export async function findSimilarCustomers(name: string, limit = 5): Promise<CustomerMatchOption[]> {
  return searchCustomersByName(name, limit)
}
