/** 采购订单主状态筛选多选：API 查询串工具。 */

const ALLOWED = new Set([-2, -1, 0, 1, 2, 10, 20, 30, 50, 100])

export function normalizePurchaseOrderStatuses(values: unknown): number[] {
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

/**
 * 写入 API/axios 参数：同名重复 key（ASP.NET `List&lt;short&gt;`）。
 * 空数组不写。
 */
export function assignPurchaseOrderStatusesParam(
  params: Record<string, unknown>,
  key: string,
  values: number[] | undefined | null
): void {
  const n = normalizePurchaseOrderStatuses(values ?? [])
  if (n.length) params[key] = n
}
