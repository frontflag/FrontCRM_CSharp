/** 账号状态排序：启用 → 已冻结 → 停用 */
const STATUS_SORT_ORDER: Record<number, number> = { 1: 0, 2: 1, 0: 2 }

export function accountStatusSortKey(status: number): number {
  return STATUS_SORT_ORDER[status] ?? 99
}

export function sortByAccountStatusThenUserName<T extends { status: number; userName?: string | null }>(
  list: T[]
): T[] {
  return [...list].sort((a, b) => {
    const sa = accountStatusSortKey(a.status)
    const sb = accountStatusSortKey(b.status)
    if (sa !== sb) return sa - sb
    return (a.userName ?? '').localeCompare(b.userName ?? '', undefined, { sensitivity: 'base' })
  })
}

export function accountStatusLabel(
  status: number,
  labels: { enabled: string; frozen: string; disabled: string }
): string {
  if (status === 1) return labels.enabled
  if (status === 2) return labels.frozen
  return labels.disabled
}
