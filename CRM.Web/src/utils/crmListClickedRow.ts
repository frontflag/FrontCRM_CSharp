import { ref, watch, type Ref } from 'vue'

export type CrmTableRowKeyProp = string | ((row: Record<string, unknown>) => string) | undefined

export function resolveCrmTableRowKey(row: Record<string, unknown>, rowKey?: CrmTableRowKeyProp): string {
  if (typeof rowKey === 'function') return String(rowKey(row) ?? '').trim()
  if (typeof rowKey === 'string' && rowKey.trim()) return String(row[rowKey] ?? '').trim()
  if (row.id != null && row.id !== '') return String(row.id).trim()
  if (row.Id != null && row.Id !== '') return String(row.Id).trim()
  return ''
}

export function mergeCrmListRowClassName(
  userClassName: string | ((ctx: { row: Record<string, unknown>; rowIndex: number }) => string) | undefined,
  ctx: { row: Record<string, unknown>; rowIndex: number },
  clickedRowKey: string | null,
  rowKey?: CrmTableRowKeyProp
): string {
  const parts: string[] = []
  if (typeof userClassName === 'function') {
    const c = userClassName(ctx)
    if (c) parts.push(c)
  } else if (typeof userClassName === 'string' && userClassName.trim()) {
    parts.push(userClassName.trim())
  }
  const key = resolveCrmTableRowKey(ctx.row, rowKey)
  if (key && clickedRowKey === key) parts.push('crm-list-row--clicked')
  return parts.join(' ')
}

/** 非 CrmDataTable 的列表页：自行在 @row-click 中调用 markClickedRow。 */
export function useCrmListClickedRow(
  data: Ref<readonly unknown[] | unknown[] | undefined>,
  rowKey?: CrmTableRowKeyProp
) {
  const clickedRowKey = ref<string | null>(null)

  watch(
    data,
    (rows) => {
      if (!clickedRowKey.value) return
      const arr = rows ?? []
      const stillThere = arr.some(
        (r) => resolveCrmTableRowKey(r as Record<string, unknown>, rowKey) === clickedRowKey.value
      )
      if (!stillThere) clickedRowKey.value = null
    },
    { deep: false }
  )

  function markClickedRow(row: Record<string, unknown>) {
    const key = resolveCrmTableRowKey(row, rowKey)
    clickedRowKey.value = key || null
  }

  function clickedRowClassName({ row }: { row: Record<string, unknown> }): string {
    const key = resolveCrmTableRowKey(row, rowKey)
    return key && clickedRowKey.value === key ? 'crm-list-row--clicked' : ''
  }

  function clearClickedRow() {
    clickedRowKey.value = null
  }

  return { clickedRowKey, markClickedRow, clickedRowClassName, clearClickedRow }
}
