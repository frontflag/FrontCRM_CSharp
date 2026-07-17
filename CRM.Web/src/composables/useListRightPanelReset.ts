/**
 * 列表数据重新加载后，将右侧操作/调查/物料等面板恢复为「请先选择记录」初始态。
 * 各列表页在筛选、分页、刷新等触发 loadList/fetch 成功后调用。
 */
export type ListRightPanelResetStore = {
  clear?: () => void
  clearBound?: () => void
}

export function resetListRightPanelOnReload(store: ListRightPanelResetStore) {
  if (typeof store.clear === 'function') {
    store.clear()
    return
  }
  if (typeof store.clearBound === 'function') {
    store.clearBound()
  }
}
