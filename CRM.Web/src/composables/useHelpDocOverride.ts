import { onBeforeUnmount, readonly, ref, watch, type Ref } from 'vue'

/**
 * 页面内视图（如列表/看板）可临时覆盖右侧「帮助」文档路径（相对 /help/）。
 * 离开页面或切回默认视图时须置 null。
 */
const helpDocOverrideRel: Ref<string | null> = ref(null)

export function setHelpDocOverride(relativeToHelpRoot: string | null) {
  const next = relativeToHelpRoot?.replace(/^\/+/, '').trim() || null
  helpDocOverrideRel.value = next
}

export function useHelpDocOverride() {
  return readonly(helpDocOverrideRel)
}

/** 列表页 list/board 切换时覆盖帮助；卸载时清除。 */
export function useListBoardHelpOverride(
  boardHelpRel: string,
  viewMode: Ref<'list' | 'board'>
) {
  watch(
    viewMode,
    (mode) => {
      setHelpDocOverride(mode === 'board' ? boardHelpRel : null)
    },
    { immediate: true }
  )
  onBeforeUnmount(() => setHelpDocOverride(null))
}
