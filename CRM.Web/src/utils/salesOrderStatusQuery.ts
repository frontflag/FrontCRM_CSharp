/** 销售订单主状态筛选多选：路由与 API 查询串工具。 */

const ALLOWED = new Set([-2, -1, 1, 2, 10, 20, 100])

export function normalizeSalesOrderStatuses(values: unknown): number[] {
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
    if (ALLOWED.has(n)) out.push(n)
  }
  return [...new Set(out)].sort((a, b) => a - b)
}

/** 路由 query：逗号拼接；空数组不写。 */
export function formatSalesOrderStatusesForRoute(values: number[] | undefined | null): string | undefined {
  const n = normalizeSalesOrderStatuses(values ?? [])
  return n.length ? n.join(',') : undefined
}

/**
 * 写入 API/axios 参数：同名重复 key（ASP.NET `List&lt;short&gt;`）。
 * 空数组不写。
 */
export function assignSalesOrderStatusesParam(
  params: Record<string, unknown>,
  key: string,
  values: number[] | undefined | null
): void {
  const n = normalizeSalesOrderStatuses(values ?? [])
  if (n.length) params[key] = n
}
