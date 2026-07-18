import { watch } from 'vue'
import type { WorkspaceLayoutApi } from '@/composables/useWorkspaceLayout'

/**
 * 列表页右侧「操作」页签（r-ops）交互门控（与销售订单明细列表 §2.4 一致）：
 * - 右栏收起：单击忽略
 * - 展开但非「操作」：仅 setRowOnly，不请求
 * - 展开且「操作」：selectRow / 加载
 * - 切到「操作」或展开右栏且已在「操作」：有选中行则 loadSelected
 */
export function useListRightOpsPanelInteraction(options: {
  workspaceLayout: WorkspaceLayoutApi | null | undefined
  isActiveRoute: () => boolean
  hasSelectedRow: () => boolean
  setRowOnly: (row: Record<string, unknown>) => void
  selectRow: (row: Record<string, unknown>) => Promise<void>
  loadSelected: () => void
  shouldBlockRowClick?: () => boolean
}) {
  function isRightPanelVisible() {
    return options.workspaceLayout?.rightPanelVisible.value ?? false
  }

  function isRightOpsTabActive() {
    return options.workspaceLayout?.rightActiveTabId.value === 'r-ops'
  }

  function loadOpsPanelIfReady() {
    if (!options.isActiveRoute()) return
    if (!isRightPanelVisible() || !isRightOpsTabActive()) return
    if (!options.hasSelectedRow()) return
    options.loadSelected()
  }

  async function onOpsPanelRowClick(row: Record<string, unknown>) {
    if (options.shouldBlockRowClick?.()) return
    if (!isRightPanelVisible()) return
    if (!isRightOpsTabActive()) {
      options.setRowOnly(row)
      return
    }
    await options.selectRow(row)
  }

  watch(
    () => options.workspaceLayout?.rightPanelVisible.value,
    (visible, wasVisible) => {
      if (!visible || wasVisible) return
      loadOpsPanelIfReady()
    }
  )

  watch(
    () => options.workspaceLayout?.rightActiveTabId.value,
    (tabId, prevTabId) => {
      if (tabId !== 'r-ops' || prevTabId === 'r-ops') return
      loadOpsPanelIfReady()
    }
  )

  return {
    isRightPanelVisible,
    isRightOpsTabActive,
    loadOpsPanelIfReady,
    onOpsPanelRowClick
  }
}
