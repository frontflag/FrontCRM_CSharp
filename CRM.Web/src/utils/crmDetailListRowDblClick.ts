/** 《业务详情页面规范》：列表行按住 Ctrl 双击时打开「编辑」（与行操作列「编辑」同入口）。 */

export type CrmDetailListRowDblClickOptions<TRow> = {
  /** 无 Ctrl 时的默认双击行为（可选，如选中行 / 打开侧栏） */
  onDefault?: (row: TRow, event?: MouseEvent) => void
  /** 与行操作「编辑」按钮相同的处理函数 */
  onEdit: (row: TRow, event?: MouseEvent) => void
  /** 与行操作「编辑」按钮 v-if 一致；默认 true */
  canEdit?: boolean | ((row: TRow) => boolean)
}

export function handleCrmDetailListRowDblClick<TRow>(
  row: TRow,
  event: MouseEvent | undefined,
  options: CrmDetailListRowDblClickOptions<TRow>
) {
  const allowed =
    typeof options.canEdit === 'function' ? options.canEdit(row) : options.canEdit !== false

  if (event?.ctrlKey) {
    if (allowed) options.onEdit(row, event)
    return
  }

  options.onDefault?.(row, event)
}

/** Element Plus / CrmDataTable @row-dblclick 适配 */
export function onCrmDetailListRowDblClick<TRow>(
  row: TRow,
  _column: unknown,
  event: MouseEvent | undefined,
  options: CrmDetailListRowDblClickOptions<TRow>
) {
  handleCrmDetailListRowDblClick(row, event, options)
}
