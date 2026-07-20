import { enqueueTelemetry } from './queue'
import { getTelemetrySessionId } from './session'

export function installJsErrorTelemetry() {
  if (typeof window === 'undefined') return

  window.addEventListener('error', (ev) => {
    if (!localStorage.getItem('token')) return
    const msg = String(ev.message || 'js_error').slice(0, 500)
    const stack = (ev.error && (ev.error as Error).stack ? String((ev.error as Error).stack) : '')
      .slice(0, 2000)
    enqueueTelemetry({
      eventType: 'error',
      eventName: 'js_error',
      sessionId: getTelemetrySessionId(),
      pageKey: location.pathname,
      routePath: location.pathname + location.search,
      payload: { message: msg, stack, source: ev.filename, line: ev.lineno, col: ev.colno }
    })
  })

  window.addEventListener('unhandledrejection', (ev) => {
    if (!localStorage.getItem('token')) return
    const reason = ev.reason
    const message =
      typeof reason === 'string'
        ? reason
        : reason instanceof Error
          ? reason.message
          : 'unhandledrejection'
    const stack = reason instanceof Error && reason.stack ? reason.stack.slice(0, 2000) : ''
    enqueueTelemetry({
      eventType: 'error',
      eventName: 'js_error',
      sessionId: getTelemetrySessionId(),
      pageKey: location.pathname,
      routePath: location.pathname + location.search,
      payload: { message: String(message).slice(0, 500), stack, kind: 'unhandledrejection' }
    })
  })
}
