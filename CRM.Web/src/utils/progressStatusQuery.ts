/** 进度状态筛选（0/1/2）多选：路由与 API 查询串工具。 */

export function normalizeProgressStatuses(values: unknown): number[] {
  const parts: string[] = []
  if (Array.isArray(values)) {
    for (const item of values) {
      if (item === undefined || item === null) continue
      parts.push(...String(item).split(','))
    }
  } else if (values !== undefined && values !== null && values !== '') {
    parts.push(...String(values).split(','))
  }
  const out: number[] = []
  for (const p of parts) {
    const n = Number(p.trim())
    if (n === 0 || n === 1 || n === 2) out.push(n)
  }
  return [...new Set(out)].sort((a, b) => a - b)
}

/** 路由 query：逗号拼接；空数组不写。 */
export function formatProgressStatusesForRoute(values: number[] | undefined | null): string | undefined {
  const n = normalizeProgressStatuses(values ?? [])
  return n.length ? n.join(',') : undefined
}

/**
 * 写入 API/axios 参数：同名重复 key（ASP.NET `List&lt;short&gt;` / `short[]`）。
 * 空数组不写。
 */
export function assignProgressStatusesParam(
  params: Record<string, unknown>,
  key: string,
  values: number[] | undefined | null
): void {
  const n = normalizeProgressStatuses(values ?? [])
  if (n.length) params[key] = n
}

/** 构建查询串：数组用 append 重复键名。 */
export function buildQueryString(params: Record<string, unknown>): string {
  const q = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return
    if (Array.isArray(v)) {
      v.forEach((item) => {
        if (item !== undefined && item !== null && item !== '') q.append(k, String(item))
      })
      return
    }
    q.append(k, String(v))
  })
  return q.toString()
}

/** 页签：仅当恰好选中一档时高亮对应 tab，否则「全部」。 */
export function progressStatusesToTab(values: number[] | undefined | null): 'all' | '0' | '1' | '2' {
  const n = normalizeProgressStatuses(values ?? [])
  if (n.length === 1) return String(n[0]) as '0' | '1' | '2'
  return 'all'
}

export function progressTabToStatuses(tab: 'all' | '0' | '1' | '2'): number[] {
  if (tab === '0' || tab === '1' || tab === '2') return [Number(tab)]
  return []
}
