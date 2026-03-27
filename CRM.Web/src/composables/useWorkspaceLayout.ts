import { ref, computed, watch, onMounted, onBeforeUnmount, provide, type InjectionKey, type Ref } from 'vue'

const STORAGE_KEY = 'frontcrm_workspace_layout_v1'

export type SidebarMode = 'full' | 'narrow' | 'hidden'

export interface WorkspaceTabItem {
  id: string
  label: string
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
}

export const WorkspaceLayoutKey: InjectionKey<WorkspaceLayoutApi> = Symbol('workspaceLayout')

function clamp(n: number, min: number, max: number) {
  return Math.min(max, Math.max(min, n))
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
    { id: 'l1', label: '检索' },
    { id: 'l2', label: '收藏' },
    { id: 'l3', label: '历史' }
  ])
  const rightTabs = ref<WorkspaceTabItem[]>([
    { id: 'r1', label: '辅助' },
    { id: 'r2', label: '扩展' },
    { id: 'r3', label: '备注' }
  ])
  const leftActiveTabId = ref('l1')
  const rightActiveTabId = ref('r1')

  const dragging = ref<null | 'sidebar' | 'left' | 'right'>(null)
  let dragStartX = 0
  let dragStartWidth = 0

  const load = () => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return
      const o = JSON.parse(raw) as Record<string, unknown>
      if (o.sidebarMode === 'full' || o.sidebarMode === 'narrow' || o.sidebarMode === 'hidden') {
        sidebarMode.value = o.sidebarMode
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

  /** 与历史模板一致：仅边条时视为 collapsed */
  const isSidebarCollapsed = computed(() => sidebarMode.value === 'narrow')

  const cycleSidebarMode = () => {
    const order: SidebarMode[] = ['full', 'narrow', 'hidden']
    const i = order.indexOf(sidebarMode.value)
    sidebarMode.value = order[(i + 1) % order.length]
  }

  const toggleLeftPanel = (visible?: boolean) => {
    if (typeof visible === 'boolean') leftPanelVisible.value = visible
    else leftPanelVisible.value = !leftPanelVisible.value
  }

  const toggleRightPanel = (visible?: boolean) => {
    if (typeof visible === 'boolean') rightPanelVisible.value = visible
    else rightPanelVisible.value = !rightPanelVisible.value
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
      if (d && typeof d.visible === 'boolean') leftPanelVisible.value = d.visible
      else toggleLeftPanel()
    } else if (ev.type === 'workspace:toggle-right') {
      if (d && typeof d.visible === 'boolean') rightPanelVisible.value = d.visible
      else toggleRightPanel()
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
    cycleSidebarMode
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
    api
  }
}
