import { watch } from 'vue'
import type { WorkspaceLayoutApi } from '@/composables/useWorkspaceLayout'

/**
 * 列表页右侧「操作」等数据页签交互门控（与销售订单明细列表 §2.4 一致）：
 * - 右栏收起：单击忽略
 * - 展开但非数据页签：仅 setRowOnly，不请求
 * - 展开且为数据页签（默认 r-ops；销售明细含 r-flow）：selectRow / 加载
 * - 切到数据页签或展开右栏且已在数据页签：有选中行则 loadSelected
 */
export function useListRightOpsPanelInteraction(options: {
  workspaceLayout: WorkspaceLayoutApi | null | undefined
  isActiveRoute: () => boolean
  hasSelectedRow: () => boolean
  setRowOnly: (row: Record<string, unknown>) => void
  selectRow: (row: Record<string, unknown>) => Promise<void>
  loadSelected: () => void
  shouldBlockRowClick?: () => boolean
  /** 需要加载面板数据的右栏页签；默认仅「操作」 */
  dataTabIds?: string[]
}) {
  const dataTabIds = () => options.dataTabIds ?? ['r-ops']

  function isRightPanelVisible() {
    return options.workspaceLayout?.rightPanelVisible.value ?? false
  }

  function isRightOpsTabActive() {
    return isRightDataTabActive()
  }

  function isRightDataTabActive() {
    const tabId = options.workspaceLayout?.rightActiveTabId.value
    return !!tabId && dataTabIds().includes(tabId)
  }

  function loadOpsPanelIfReady() {
    if (!options.isActiveRoute()) return
    if (!isRightPanelVisible() || !isRightDataTabActive()) return
    if (!options.hasSelectedRow()) return
    options.loadSelected()
  }

  async function onOpsPanelRowClick(row: Record<string, unknown>) {
    if (options.shouldBlockRowClick?.()) return
    if (!isRightPanelVisible()) return
    if (!isRightDataTabActive()) {
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
      if (!tabId || !dataTabIds().includes(tabId)) return
      if (prevTabId === tabId) return
      loadOpsPanelIfReady()
    }
  )

  return {
    isRightPanelVisible,
    isRightOpsTabActive,
    isRightDataTabActive,
    loadOpsPanelIfReady,
    onOpsPanelRowClick
  }
}
