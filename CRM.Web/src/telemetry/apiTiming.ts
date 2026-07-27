import { isTelemetryApiExcluded, toPathTemplate } from './constants'
import { isTelemetryApiSystemFailure } from './apiFailure'
import { enqueueTelemetry } from './queue'
import { getTelemetrySessionId } from './session'

export function trackApiTelemetry(opts: {
  method: string
  url: string
  status: number
  durationMs: number
  errorId?: string | null
  message?: string | null
}) {
  if (isTelemetryApiExcluded(opts.url)) return
  if (!localStorage.getItem('token')) return

  const pathTemplate = toPathTemplate(opts.url)
  const method = (opts.method || 'GET').toUpperCase()
  const durationMs = Math.max(0, Math.round(opts.durationMs))
  const status = opts.status || 0

  enqueueTelemetry({
    eventType: 'perf',
    eventName: 'api_timing',
    sessionId: getTelemetrySessionId(),
    pageKey: typeof location !== 'undefined' ? location.pathname : undefined,
    routePath: typeof location !== 'undefined' ? location.pathname + location.search : undefined,
    payload: {
      method,
      pathTemplate,
      durationMs,
      status
    }
  })

  // 仅系统异常记 api_error：超时/断网(status=0)、5xx；业务 404/400 等不记
  if (isTelemetryApiSystemFailure(status)) {
    enqueueTelemetry({
      eventType: 'error',
      eventName: 'api_error',
      sessionId: getTelemetrySessionId(),
      pageKey: typeof location !== 'undefined' ? location.pathname : undefined,
      routePath: typeof location !== 'undefined' ? location.pathname + location.search : undefined,
      payload: {
        method,
        pathTemplate,
        durationMs,
        status,
        errorId: opts.errorId || undefined,
        message: (opts.message || '').slice(0, 500) || undefined
      }
    })
  }
}
