import { enqueueTelemetry } from './queue'
import { getTelemetrySessionId, touchTelemetrySession } from './session'

function normalizeLabel(text: string): string {
  return text
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 40)
    .replace(/[^\w\u4e00-\u9fff\-./]+/g, '_')
    .replace(/_+/g, '_')
    .replace(/^_|_$/g, '')
    .toLowerCase()
}

function currentPageKey(): string {
  try {
    const path = location.pathname || '/'
    return path.replace(/\/[0-9a-f]{8}-[0-9a-f-]{27}/gi, '/:id').replace(/\/\d+(?=\/|$)/g, '/:id')
  } catch {
    return 'unknown'
  }
}

function resolveActionTarget(el: Element | null): { actionId: string; label: string } | null {
  if (!el) return null
  const tracked = el.closest('[data-track]') as HTMLElement | null
  if (tracked) {
    const id = (tracked.getAttribute('data-track') || '').trim()
    if (id) {
      return {
        actionId: id.slice(0, 200),
        label: (tracked.getAttribute('data-track-label') || tracked.innerText || id).trim().slice(0, 80)
      }
    }
  }

  const btn = el.closest(
    'button, a.submenu-item, a.el-button, .el-button, [role="button"], .el-dropdown-menu__item'
  ) as HTMLElement | null
  if (!btn) return null

  // 忽略纯关闭/图标无文案且无 data-track 的噪音可仍记录
  const label = (btn.getAttribute('aria-label') || btn.innerText || btn.getAttribute('title') || '')
    .replace(/\s+/g, ' ')
    .trim()
  if (!label && !btn.classList.contains('submenu-item')) return null

  const norm = normalizeLabel(label || 'action')
  if (!norm) return null

  if (btn.classList.contains('submenu-item') || btn.closest('.sidebar-menu')) {
    return { actionId: `menu.${norm}`, label: label || norm }
  }
  return { actionId: `btn.${currentPageKey()}.${norm}`.slice(0, 200), label: label || norm }
}

let lastSig = ''
let lastAt = 0

export function installActionCapture() {
  if (typeof document === 'undefined') return
  document.addEventListener(
    'click',
    (ev) => {
      if (!localStorage.getItem('token')) return
      const target = ev.target as Element | null
      const resolved = resolveActionTarget(target)
      if (!resolved) return

      const sig = `${resolved.actionId}|${resolved.label}`
      const now = Date.now()
      if (sig === lastSig && now - lastAt < 400) return
      lastSig = sig
      lastAt = now

      touchTelemetrySession('activity')
      enqueueTelemetry({
        eventType: 'action',
        eventName: btnEventName(resolved.actionId),
        sessionId: getTelemetrySessionId(),
        pageKey: currentPageKey(),
        routePath: location.pathname + location.search,
        payload: {
          actionId: resolved.actionId,
          label: resolved.label
        }
      })
    },
    true
  )
}

function btnEventName(actionId: string): string {
  if (actionId.startsWith('menu.')) return 'menu_click'
  return 'btn_click'
}
