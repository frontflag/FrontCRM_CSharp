import { onUnmounted } from 'vue'

export interface UseResizableDialogOptions {
  /** 优先：通过 el-dialog 组件实例解析真实 DOM */
  resolveDialogEl?: () => HTMLElement | null | undefined
  /** 兜底：el-dialog 根节点 class */
  dialogClass?: string
  minWidth?: number
  minHeight?: number
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

export function useResizableDialog(options: UseResizableDialogOptions) {
  const minWidth = options.minWidth ?? 560
  const minHeight = options.minHeight ?? 360

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

  function enableResizableDialog(): boolean {
    teardown?.()

    const resolved = resolveDialogEl()
    if (!resolved) return false

    const dialogEl = resolved
    dialogEl.classList.add('crm-dialog-resizable')
    const rect = dialogEl.getBoundingClientRect()
    applyDialogWidth(dialogEl, rect.width)

    const directions: ResizeDirection[] = ['e', 's', 'se']
    const handles = directions.map((dir) => {
      const handle = document.createElement('div')
      handle.className = `crm-dialog-resize-handle crm-dialog-resize-handle--${dir}`
      handle.dataset.resizeDir = dir
      dialogEl.appendChild(handle)
      return handle
    })

    let activeDir: ResizeDirection | null = null
    let startX = 0
    let startY = 0
    let startW = 0
    let startH = 0

    function onMouseDown(e: MouseEvent) {
      const target = e.target as HTMLElement
      const dir = target.dataset.resizeDir as ResizeDirection | undefined
      if (!dir || !target.classList.contains('crm-dialog-resize-handle')) return

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

    function onMouseMove(e: MouseEvent) {
      if (!activeDir) return

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
    }

    function onMouseUp() {
      activeDir = null
      document.removeEventListener('mousemove', onMouseMove)
      document.removeEventListener('mouseup', onMouseUp)
      document.body.style.userSelect = ''
      document.body.style.cursor = ''
    }

    dialogEl.addEventListener('mousedown', onMouseDown)

    teardown = () => {
      dialogEl.removeEventListener('mousedown', onMouseDown)
      document.removeEventListener('mousemove', onMouseMove)
      document.removeEventListener('mouseup', onMouseUp)
      document.body.style.userSelect = ''
      document.body.style.cursor = ''
      handles.forEach((handle) => handle.remove())
      dialogEl.classList.remove('crm-dialog-resizable')
      dialogEl.style.removeProperty('--el-dialog-width')
      dialogEl.style.width = ''
      dialogEl.style.height = ''
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
