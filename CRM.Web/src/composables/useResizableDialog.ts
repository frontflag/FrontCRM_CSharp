import { onUnmounted } from 'vue'

export interface UseResizableDialogOptions {
  /** 优先：通过 el-dialog 组件实例解析真实 DOM */
  resolveDialogEl?: () => HTMLElement | null | undefined
  /** 兜底：el-dialog 根节点 class */
  dialogClass?: string
  minWidth?: number
  minHeight?: number
  /**
   * 标题栏拖拽移动（left/top 像素定位，避免与 Element Plus draggable 的 transform 冲突）。
   * 启用后请勿再给 el-dialog 加 `draggable`。
   */
  draggable?: boolean
}

type ResizeDirection = 'e' | 's' | 'se'

function findDialogElByClass(dialogClass: string): HTMLElement | null {
  const root = document.querySelector(`.${dialogClass}`) as HTMLElement | null
  if (!root) return null
  if (root.classList.contains('el-dialog')) return root
  return (root.querySelector('.el-dialog') as HTMLElement | null) ?? null
}

function applyDialogWidth(el: HTMLElement, width: number) {
  el.style.setProperty('--el-dialog-width', `${width}px`)
  el.style.width = `${width}px`
}

function applyDialogSize(el: HTMLElement, width: number, height: number) {
  applyDialogWidth(el, width)
  el.style.height = `${height}px`
}

/** 将当前视觉位置钉成 fixed + left/top，清除 transform/居中 margin，避免拖放后错位 */
function pinDialogPosition(el: HTMLElement) {
  const rect = el.getBoundingClientRect()
  el.style.position = 'fixed'
  el.style.margin = '0'
  el.style.transform = 'none'
  el.style.left = `${Math.round(rect.left)}px`
  el.style.top = `${Math.round(rect.top)}px`
}

export function useResizableDialog(options: UseResizableDialogOptions) {
  const minWidth = options.minWidth ?? 560
  const minHeight = options.minHeight ?? 360
  const draggable = options.draggable === true

  let teardown: (() => void) | null = null

  function resolveDialogEl(): HTMLElement | null {
    const fromRef = options.resolveDialogEl?.()
    if (fromRef instanceof HTMLElement) return fromRef
    if (options.dialogClass) return findDialogElByClass(options.dialogClass)
    return null
  }

  function clampSize(width: number, height: number) {
    const maxWidth = window.innerWidth - 32
    const maxHeight = window.innerHeight - 32
    return {
      width: Math.min(maxWidth, Math.max(minWidth, width)),
      height: Math.min(maxHeight, Math.max(minHeight, height))
    }
  }

  function clampPosition(left: number, top: number, width: number, height: number) {
    const maxLeft = Math.max(0, window.innerWidth - Math.min(width, window.innerWidth))
    const maxTop = Math.max(0, window.innerHeight - Math.min(height, window.innerHeight))
    return {
      left: Math.min(maxLeft, Math.max(0, left)),
      top: Math.min(maxTop, Math.max(0, top))
    }
  }

  function enableResizableDialog(): boolean {
    teardown?.()

    const resolved = resolveDialogEl()
    if (!resolved) return false

    const dialogEl = resolved
    dialogEl.classList.add('crm-dialog-resizable')
    if (draggable) dialogEl.classList.add('crm-dialog-draggable')

    const rect = dialogEl.getBoundingClientRect()
    applyDialogWidth(dialogEl, rect.width)
    pinDialogPosition(dialogEl)

    const directions: ResizeDirection[] = ['e', 's', 'se']
    const handles = directions.map((dir) => {
      const handle = document.createElement('div')
      handle.className = `crm-dialog-resize-handle crm-dialog-resize-handle--${dir}`
      handle.dataset.resizeDir = dir
      dialogEl.appendChild(handle)
      return handle
    })

    let activeDir: ResizeDirection | null = null
    let dragging = false
    let startX = 0
    let startY = 0
    let startW = 0
    let startH = 0
    let startLeft = 0
    let startTop = 0

    function onResizeMouseDown(e: MouseEvent) {
      const target = e.target as HTMLElement
      const dir = target.dataset.resizeDir as ResizeDirection | undefined
      if (!dir || !target.classList.contains('crm-dialog-resize-handle')) return

      pinDialogPosition(dialogEl)
      activeDir = dir
      startX = e.clientX
      startY = e.clientY
      startW = dialogEl.offsetWidth
      startH = dialogEl.offsetHeight
      e.preventDefault()
      e.stopPropagation()

      document.addEventListener('mousemove', onMouseMove)
      document.addEventListener('mouseup', onMouseUp)
      document.body.style.userSelect = 'none'
      document.body.style.cursor =
        dir === 'e' ? 'ew-resize' : dir === 's' ? 'ns-resize' : 'nwse-resize'
    }

    function onDragMouseDown(e: MouseEvent) {
      if (!draggable) return
      const target = e.target as HTMLElement
      if (target.closest('.crm-dialog-resize-handle')) return
      if (target.closest('.el-dialog__headerbtn')) return
      const header = target.closest('.el-dialog__header')
      if (!header || !dialogEl.contains(header)) return

      pinDialogPosition(dialogEl)
      dragging = true
      startX = e.clientX
      startY = e.clientY
      startLeft = parseFloat(dialogEl.style.left || '0') || 0
      startTop = parseFloat(dialogEl.style.top || '0') || 0
      e.preventDefault()

      document.addEventListener('mousemove', onMouseMove)
      document.addEventListener('mouseup', onMouseUp)
      document.body.style.userSelect = 'none'
      document.body.style.cursor = 'move'
    }

    function onMouseMove(e: MouseEvent) {
      if (activeDir) {
        let width = startW
        let height = startH
        if (activeDir === 'e' || activeDir === 'se') {
          width = startW + (e.clientX - startX)
        }
        if (activeDir === 's' || activeDir === 'se') {
          height = startH + (e.clientY - startY)
        }
        const size = clampSize(width, height)
        applyDialogSize(dialogEl, size.width, size.height)
        // 缩放时保持左上角钉住，避免 EP 居中样式把窗口拽回去
        pinDialogPosition(dialogEl)
        // pin 会按当前 rect 重算；向右下缩放时左上应不变。上面 pin 在改 size 后用新 rect，left/top 不变（fixed 左上不变）——OK
        return
      }

      if (dragging) {
        const nextLeft = startLeft + (e.clientX - startX)
        const nextTop = startTop + (e.clientY - startY)
        const pos = clampPosition(nextLeft, nextTop, dialogEl.offsetWidth, dialogEl.offsetHeight)
        dialogEl.style.left = `${pos.left}px`
        dialogEl.style.top = `${pos.top}px`
        dialogEl.style.transform = 'none'
        dialogEl.style.margin = '0'
        dialogEl.style.position = 'fixed'
      }
    }

    function onMouseUp() {
      activeDir = null
      dragging = false
      document.removeEventListener('mousemove', onMouseMove)
      document.removeEventListener('mouseup', onMouseUp)
      document.body.style.userSelect = ''
      document.body.style.cursor = ''
      pinDialogPosition(dialogEl)
    }

    dialogEl.addEventListener('mousedown', onResizeMouseDown)
    if (draggable) {
      dialogEl.addEventListener('mousedown', onDragMouseDown)
    }

    teardown = () => {
      dialogEl.removeEventListener('mousedown', onResizeMouseDown)
      dialogEl.removeEventListener('mousedown', onDragMouseDown)
      document.removeEventListener('mousemove', onMouseMove)
      document.removeEventListener('mouseup', onMouseUp)
      document.body.style.userSelect = ''
      document.body.style.cursor = ''
      handles.forEach((handle) => handle.remove())
      dialogEl.classList.remove('crm-dialog-resizable')
      dialogEl.classList.remove('crm-dialog-draggable')
      dialogEl.style.removeProperty('--el-dialog-width')
      dialogEl.style.width = ''
      dialogEl.style.height = ''
      dialogEl.style.left = ''
      dialogEl.style.top = ''
      dialogEl.style.margin = ''
      dialogEl.style.transform = ''
      dialogEl.style.position = ''
    }

    return true
  }

  function enableResizableDialogWithRetry(maxAttempts = 8) {
    let attempt = 0
    const tryEnable = () => {
      if (enableResizableDialog()) return
      attempt += 1
      if (attempt < maxAttempts) {
        requestAnimationFrame(tryEnable)
      }
    }
    tryEnable()
  }

  function fitDialogToContent(): boolean {
    const dialogEl = resolveDialogEl()
    if (!dialogEl) return false

    const body = dialogEl.querySelector('.el-dialog__body') as HTMLElement | null
    const width = dialogEl.offsetWidth || dialogEl.getBoundingClientRect().width

    dialogEl.style.height = 'auto'
    if (body) {
      body.style.height = 'auto'
      body.style.flex = '0 1 auto'
      body.style.overflow = 'visible'
    }

    const naturalHeight = Math.ceil(dialogEl.getBoundingClientRect().height)
    const size = clampSize(width, naturalHeight)
    applyDialogSize(dialogEl, size.width, size.height)
    pinDialogPosition(dialogEl)

    if (body) {
      body.style.height = ''
      body.style.flex = ''
      body.style.overflow = ''
    }

    return true
  }

  function fitDialogToContentWithRetry(maxAttempts = 8) {
    let attempt = 0
    const tryFit = () => {
      if (fitDialogToContent()) return
      attempt += 1
      if (attempt < maxAttempts) {
        requestAnimationFrame(tryFit)
      }
    }
    tryFit()
  }

  function disableResizableDialog() {
    teardown?.()
    teardown = null
  }

  onUnmounted(disableResizableDialog)

  return {
    enableResizableDialog,
    enableResizableDialogWithRetry,
    fitDialogToContent,
    fitDialogToContentWithRetry,
    disableResizableDialog
  }
}
