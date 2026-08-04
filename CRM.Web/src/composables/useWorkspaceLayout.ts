import { ref, computed, watch, onMounted, onBeforeUnmount, provide, type InjectionKey, type Ref } from 'vue'

const STORAGE_KEY = 'frontcrm_workspace_layout_v1'
/** 各路由上次激活的左右扩展栏页签（与布局宽高分离，便于独立迁移） */
const AUX_TABS_STORAGE_KEY = 'frontcrm_aux_tabs_by_route_v1'

type AuxTabsMemoryEntry = { left?: string; right?: string }
type AuxTabsMemoryMap = Record<string, AuxTabsMemoryEntry>

export type SidebarMode = 'full' | 'narrow'

export interface WorkspaceTabItem {
  id: string
  /** vue-i18n key, e.g. layout.auxTabs.search */
  labelKey: string
}

export interface WorkspaceLayoutApi {
  sidebarMode: Ref<SidebarMode>
  leftPanelVisible: Ref<boolean>
  rightPanelVisible: Ref<boolean>
  leftFullscreen: Ref<boolean>
  centerFullscreen: Ref<boolean>
  rightFullscreen: Ref<boolean>
  toggleLeftPanel: (visible?: boolean) => void
  toggleRightPanel: (visible?: boolean) => void
  toggleLeftFullscreen: (fullscreen?: boolean) => void
  toggleRightFullscreen: (fullscreen?: boolean) => void
  toggleCenterFullscreen: (fullscreen?: boolean) => void
  cycleSidebarMode: () => void
  rightActiveTabId: Ref<string>
  setRightActiveTab: (tabId: string) => void
}

export const WorkspaceLayoutKey: InjectionKey<WorkspaceLayoutApi> = Symbol('workspaceLayout')

function clamp(n: number, min: number, max: number) {
  return Math.min(max, Math.max(min, n))
}

function routeKey(routeName: string | symbol | null | undefined): string {
  if (routeName == null || routeName === '') return ''
  return String(routeName)
}

export function useWorkspaceLayout() {
  const sidebarMode = ref<SidebarMode>('full')
  const sidebarWidthPx = ref(240)

  const leftPanelVisible = ref(true)
  const rightPanelVisible = ref(true)
  const leftPanelWidth = ref(260)
  const rightPanelWidth = ref(280)

  const leftFullscreen = ref(false)
  const centerFullscreen = ref(false)
  const rightFullscreen = ref(false)

  const leftTabs = ref<WorkspaceTabItem[]>([
    { id: 'l1', labelKey: 'layout.auxTabs.search' },
    { id: 'l2', labelKey: 'layout.auxTabs.favorites' },
    { id: 'l3', labelKey: 'layout.auxTabs.history' }
  ])
  const rightTabs = ref<WorkspaceTabItem[]>([{ id: 'r4', labelKey: 'layout.auxTabs.help' }])
  const leftActiveTabId = ref('l1')
  const rightActiveTabId = ref('r4')
  /** 当前路由名；用于按页记忆左右页签 */
  const auxTabsRouteKey = ref('')
  let auxTabsByRoute: AuxTabsMemoryMap = {}
  let auxTabsPersistPaused = false

  const dragging = ref<null | 'sidebar' | 'left' | 'right'>(null)
  let dragStartX = 0
  let dragStartWidth = 0

  const loadAuxTabsMemory = () => {
    try {
      const raw = localStorage.getItem(AUX_TABS_STORAGE_KEY)
      if (!raw) {
        auxTabsByRoute = {}
        return
      }
      const o = JSON.parse(raw) as unknown
      if (!o || typeof o !== 'object' || Array.isArray(o)) {
        auxTabsByRoute = {}
        return
      }
      const next: AuxTabsMemoryMap = {}
      for (const [k, v] of Object.entries(o as Record<string, unknown>)) {
        if (!k || !v || typeof v !== 'object' || Array.isArray(v)) continue
        const entry = v as Record<string, unknown>
        const left = typeof entry.left === 'string' ? entry.left : undefined
        const right = typeof entry.right === 'string' ? entry.right : undefined
        if (left || right) next[k] = { left, right }
      }
      auxTabsByRoute = next
    } catch {
      auxTabsByRoute = {}
    }
  }

  const persistAuxTabsMemory = () => {
    try {
      localStorage.setItem(AUX_TABS_STORAGE_KEY, JSON.stringify(auxTabsByRoute))
    } catch {
      /* ignore */
    }
  }

  const rememberAuxTabsForRoute = (routeName: string | symbol | null | undefined) => {
    const key = routeKey(routeName)
    if (!key) return
    const leftIds = new Set(leftTabs.value.map((t) => t.id))
    const rightIds = new Set(rightTabs.value.map((t) => t.id))
    const left = leftIds.has(leftActiveTabId.value) ? leftActiveTabId.value : undefined
    const right = rightIds.has(rightActiveTabId.value) ? rightActiveTabId.value : undefined
    if (!left && !right) return
    auxTabsByRoute[key] = {
      left: left ?? auxTabsByRoute[key]?.left,
      right: right ?? auxTabsByRoute[key]?.right
    }
    persistAuxTabsMemory()
  }

  /**
   * 进入某路由并完成 rightTabs 注册后调用：优先恢复上次页签，否则用 defaults，再否则首个可用页签。
   * 收起→展开仍走 prefer*OnExpand，不受本函数影响。
   */
  const restoreAuxTabsForRoute = (
    routeName: string | symbol | null | undefined,
    defaults?: { left?: string; right?: string }
  ) => {
    auxTabsPersistPaused = true
    try {
      const key = routeKey(routeName)
      auxTabsRouteKey.value = key
      const saved = key ? auxTabsByRoute[key] : undefined
      const leftIds = new Set(leftTabs.value.map((t) => t.id))
      const rightIds = new Set(rightTabs.value.map((t) => t.id))

      let left =
        saved?.left && leftIds.has(saved.left)
          ? saved.left
          : defaults?.left && leftIds.has(defaults.left)
            ? defaults.left
            : undefined
      if (!left) left = leftIds.has('l1') ? 'l1' : leftTabs.value[0]?.id

      let right =
        saved?.right && rightIds.has(saved.right)
          ? saved.right
          : defaults?.right && rightIds.has(defaults.right)
            ? defaults.right
            : undefined
      if (!right) right = rightTabs.value[0]?.id

      if (left) leftActiveTabId.value = left
      if (right) rightActiveTabId.value = right
    } finally {
      auxTabsPersistPaused = false
    }
  }

  const load = () => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return
      const o = JSON.parse(raw) as Record<string, unknown>
      // 旧版曾保存 hidden，现仅支持 full / narrow
      if (o.sidebarMode === 'narrow') {
        sidebarMode.value = 'narrow'
      } else {
        sidebarMode.value = 'full'
      }
      if (typeof o.sidebarWidthPx === 'number') sidebarWidthPx.value = clamp(o.sidebarWidthPx, 200, 320)
      if (typeof o.leftPanelWidth === 'number') leftPanelWidth.value = clamp(o.leftPanelWidth, 160, 560)
      if (typeof o.rightPanelWidth === 'number') rightPanelWidth.value = clamp(o.rightPanelWidth, 200, 560)
      if (typeof o.leftPanelVisible === 'boolean') leftPanelVisible.value = o.leftPanelVisible
      if (typeof o.rightPanelVisible === 'boolean') rightPanelVisible.value = o.rightPanelVisible
    } catch {
      /* ignore */
    }
  }

  // 须在 AppLayout immediate 路由 watch 之前可读；不能只放在 onMounted
  loadAuxTabsMemory()

  const save = () => {
    try {
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          sidebarMode: sidebarMode.value,
          sidebarWidthPx: sidebarWidthPx.value,
          leftPanelWidth: leftPanelWidth.value,
          rightPanelWidth: rightPanelWidth.value,
          leftPanelVisible: leftPanelVisible.value,
          rightPanelVisible: rightPanelVisible.value
        })
      )
    } catch {
      /* ignore */
    }
  }

  watch(
    [sidebarMode, sidebarWidthPx, leftPanelWidth, rightPanelWidth, leftPanelVisible, rightPanelVisible],
    () => save(),
    { deep: true }
  )

  /** 右栏 Tab 删减后，避免仍停留在已移除的 tab（如 r5）导致内容区空白 */
  watch(
    () => rightTabs.value.map((t) => t.id),
    (ids) => {
      if (!ids.length) return
      if (!ids.includes(rightActiveTabId.value)) rightActiveTabId.value = ids[0]!
    },
    { immediate: true }
  )

  /** 用户切换页签时写入当前路由记忆 */
  watch([leftActiveTabId, rightActiveTabId], () => {
    if (auxTabsPersistPaused || !auxTabsRouteKey.value) return
    rememberAuxTabsForRoute(auxTabsRouteKey.value)
  })

  /** 与历史模板一致：仅边条时视为 collapsed */
  const isSidebarCollapsed = computed(() => sidebarMode.value === 'narrow')

  const cycleSidebarMode = () => {
    sidebarMode.value = sidebarMode.value === 'full' ? 'narrow' : 'full'
  }

  const preferLeftSearchTabOnExpand = () => {
    if (leftTabs.value.some((t) => t.id === 'l1')) leftActiveTabId.value = 'l1'
  }

  const preferRightOpsTabOnExpand = () => {
    if (rightTabs.value.some((t) => t.id === 'r-ops')) rightActiveTabId.value = 'r-ops'
  }

  const toggleLeftPanel = (visible?: boolean) => {
    const nextVisible = typeof visible === 'boolean' ? visible : !leftPanelVisible.value
    if (nextVisible && !leftPanelVisible.value) preferLeftSearchTabOnExpand()
    leftPanelVisible.value = nextVisible
  }

  const toggleRightPanel = (visible?: boolean) => {
    const nextVisible = typeof visible === 'boolean' ? visible : !rightPanelVisible.value
    if (nextVisible && !rightPanelVisible.value) preferRightOpsTabOnExpand()
    rightPanelVisible.value = nextVisible
  }

  const toggleLeftFullscreen = (fullscreen?: boolean) => {
    if (typeof fullscreen === 'boolean') leftFullscreen.value = fullscreen
    else leftFullscreen.value = !leftFullscreen.value
    if (leftFullscreen.value) {
      centerFullscreen.value = false
      rightFullscreen.value = false
    }
  }

  const toggleRightFullscreen = (fullscreen?: boolean) => {
    if (typeof fullscreen === 'boolean') rightFullscreen.value = fullscreen
    else rightFullscreen.value = !rightFullscreen.value
    if (rightFullscreen.value) {
      centerFullscreen.value = false
      leftFullscreen.value = false
    }
  }

  const toggleCenterFullscreen = (fullscreen?: boolean) => {
    if (typeof fullscreen === 'boolean') centerFullscreen.value = fullscreen
    else centerFullscreen.value = !centerFullscreen.value
    if (centerFullscreen.value) {
      leftFullscreen.value = false
      rightFullscreen.value = false
    }
  }

  const onResizeStart = (which: 'sidebar' | 'left' | 'right', e: MouseEvent) => {
    e.preventDefault()
    dragging.value = which
    dragStartX = e.clientX
    if (which === 'sidebar') dragStartWidth = sidebarWidthPx.value
    else if (which === 'left') dragStartWidth = leftPanelWidth.value
    else dragStartWidth = rightPanelWidth.value
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'
  }

  const onResizeMove = (e: MouseEvent) => {
    if (!dragging.value) return
    const dx = e.clientX - dragStartX
    if (dragging.value === 'sidebar') {
      sidebarWidthPx.value = clamp(dragStartWidth + dx, 200, 320)
    } else if (dragging.value === 'left') {
      leftPanelWidth.value = clamp(dragStartWidth + dx, 160, 560)
    } else {
      rightPanelWidth.value = clamp(dragStartWidth - dx, 200, 560)
    }
  }

  const onResizeEnd = () => {
    if (!dragging.value) return
    dragging.value = null
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
  }

  const onWindowWorkspaceEvent = (ev: Event) => {
    const e = ev as CustomEvent<{ visible?: boolean }>
    const d = e.detail
    if (ev.type === 'workspace:toggle-left') {
      if (d && typeof d.visible === 'boolean') {
        if (d.visible && !leftPanelVisible.value) preferLeftSearchTabOnExpand()
        leftPanelVisible.value = d.visible
      } else toggleLeftPanel()
    } else if (ev.type === 'workspace:toggle-right') {
      if (d && typeof d.visible === 'boolean') {
        if (d.visible && !rightPanelVisible.value) preferRightOpsTabOnExpand()
        rightPanelVisible.value = d.visible
      } else toggleRightPanel()
    } else if (ev.type === 'workspace:toggle-center-fullscreen') {
      if (d && typeof d.visible === 'boolean') centerFullscreen.value = d.visible
      else toggleCenterFullscreen()
    }
  }

  onMounted(() => {
    load()
    window.addEventListener('mousemove', onResizeMove)
    window.addEventListener('mouseup', onResizeEnd)
    window.addEventListener('workspace:toggle-left', onWindowWorkspaceEvent)
    window.addEventListener('workspace:toggle-right', onWindowWorkspaceEvent)
    window.addEventListener('workspace:toggle-center-fullscreen', onWindowWorkspaceEvent)
  })

  onBeforeUnmount(() => {
    window.removeEventListener('mousemove', onResizeMove)
    window.removeEventListener('mouseup', onResizeEnd)
    window.removeEventListener('workspace:toggle-left', onWindowWorkspaceEvent)
    window.removeEventListener('workspace:toggle-right', onWindowWorkspaceEvent)
    window.removeEventListener('workspace:toggle-center-fullscreen', onWindowWorkspaceEvent)
  })

  const setRightActiveTab = (tabId: string) => {
    if (rightTabs.value.some((t) => t.id === tabId)) rightActiveTabId.value = tabId
  }

  const api: WorkspaceLayoutApi = {
    sidebarMode,
    leftPanelVisible,
    rightPanelVisible,
    leftFullscreen,
    centerFullscreen,
    rightFullscreen,
    toggleLeftPanel,
    toggleRightPanel,
    toggleLeftFullscreen,
    toggleRightFullscreen,
    toggleCenterFullscreen,
    cycleSidebarMode,
    rightActiveTabId,
    setRightActiveTab
  }

  provide(WorkspaceLayoutKey, api)

  return {
    sidebarMode,
    sidebarWidthPx,
    isSidebarCollapsed,
    cycleSidebarMode,
    leftPanelVisible,
    rightPanelVisible,
    leftPanelWidth,
    rightPanelWidth,
    leftFullscreen,
    centerFullscreen,
    rightFullscreen,
    leftTabs,
    rightTabs,
    leftActiveTabId,
    rightActiveTabId,
    onResizeStart,
    toggleLeftPanel,
    toggleRightPanel,
    toggleLeftFullscreen,
    toggleRightFullscreen,
    toggleCenterFullscreen,
    rememberAuxTabsForRoute,
    restoreAuxTabsForRoute,
    api
  }
}
