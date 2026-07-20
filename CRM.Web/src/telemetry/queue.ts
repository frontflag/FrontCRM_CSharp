import { TELEMETRY_FLUSH_INTERVAL_MS, TELEMETRY_FLUSH_MAX_BATCH } from './constants'
import { collectClientInfo } from './clientInfo'

export interface TelemetryEventInput {
  eventType: string
  eventName: string
  sessionId?: string | null
  pageKey?: string | null
  routePath?: string | null
  payload?: Record<string, unknown> | null
  occurredAt?: string
}

interface TelemetryEventDto extends TelemetryEventInput {
  eventId: string
  occurredAt: string
  browser?: string
  os?: string
  deviceType?: string
  screenW?: number
  screenH?: number
  userAgent?: string
}

type EnqueueFn = (event: TelemetryEventInput) => void

let queue: TelemetryEventDto[] = []
let flushTimer: number | null = null
let flushing = false
let enabled = false
let getSessionId: (() => string | null) | null = null
let onActivity: (() => void) | null = null

function newId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) return crypto.randomUUID()
  return `t-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function setTelemetryEnabled(on: boolean) {
  enabled = on
  if (on) ensureTimer()
  else if (flushTimer != null) {
    window.clearInterval(flushTimer)
    flushTimer = null
  }
}

export function bindTelemetrySession(getter: () => string | null, activity: () => void) {
  getSessionId = getter
  onActivity = activity
}

export const enqueueTelemetry: EnqueueFn = (event) => {
  if (!enabled) return
  if (!localStorage.getItem('token')) return
  // 非 session 事件才刷新空闲时钟；避免 session_start/end 经 onActivity 再 touch 造成递归白屏
  if (event.eventType !== 'session') {
    onActivity?.()
  }

  const client = collectClientInfo()
  const dto: TelemetryEventDto = {
    ...event,
    eventId: newId(),
    occurredAt: event.occurredAt || new Date().toISOString(),
    sessionId: event.sessionId ?? getSessionId?.() ?? undefined,
    browser: client.browser,
    os: client.os,
    deviceType: client.deviceType,
    screenW: client.screenW,
    screenH: client.screenH,
    userAgent: client.userAgent
  }
  queue.push(dto)
  if (queue.length >= TELEMETRY_FLUSH_MAX_BATCH) void flushTelemetry()
}

function ensureTimer() {
  if (flushTimer != null || typeof window === 'undefined') return
  flushTimer = window.setInterval(() => {
    void flushTelemetry()
  }, TELEMETRY_FLUSH_INTERVAL_MS)
}

/** 批量上报；关页/登出时用 keepalive，卸载后仍可能发出 */
export async function flushTelemetry(): Promise<void> {
  if (flushing || queue.length === 0) return
  // 先同步取 token，避免登出清 storage 后竞态
  const token = localStorage.getItem('token') || ''
  if (!token) {
    queue = []
    return
  }

  flushing = true
  const batch = queue.splice(0, 100)
  const body = JSON.stringify({ events: batch })
  const url = '/api/v1/telemetry/events'

  try {
    void fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body,
      keepalive: true
    }).catch(() => {
      queue = [...batch.slice(0, 40), ...queue].slice(0, 200)
    })
  } catch {
    queue = [...batch.slice(0, 40), ...queue].slice(0, 200)
  } finally {
    flushing = false
  }
}

export function installTelemetryPageLifecycle() {
  if (typeof window === 'undefined') return
  const flush = () => {
    void flushTelemetry()
  }
  window.addEventListener('pagehide', flush)
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') flush()
  })
}
