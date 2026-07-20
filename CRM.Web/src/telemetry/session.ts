import { TELEMETRY_SESSION_IDLE_MS } from './constants'
import { enqueueTelemetry, flushTelemetry } from './queue'

const STORAGE_KEY = 'crm_telemetry_session'

interface SessionState {
  sessionId: string
  lastActivityAt: number
}

let state: SessionState | null = null
/** 防止 touch → enqueue → activity → touch 递归 */
let touching = false

function load(): SessionState | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as SessionState
  } catch {
    return null
  }
}

function save(s: SessionState | null) {
  if (!s) sessionStorage.removeItem(STORAGE_KEY)
  else sessionStorage.setItem(STORAGE_KEY, JSON.stringify(s))
}

function newSessionId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) return crypto.randomUUID()
  return `s-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function getTelemetrySessionId(): string | null {
  return state?.sessionId ?? null
}

/** 仅刷新空闲计时，不创建/结束会话、不上报（供队列回调使用） */
export function bumpTelemetryActivity() {
  if (!localStorage.getItem('token')) return
  if (!state) state = load()
  if (!state) return
  state.lastActivityAt = Date.now()
  save(state)
}

export function touchTelemetrySession(reason: 'login' | 'activity' = 'activity') {
  if (touching) return
  if (!localStorage.getItem('token')) return
  touching = true
  try {
    const now = Date.now()
    if (!state) state = load()

    if (state && now - state.lastActivityAt > TELEMETRY_SESSION_IDLE_MS) {
      enqueueTelemetry({
        eventType: 'session',
        eventName: 'session_end',
        sessionId: state.sessionId,
        payload: { reason: 'idle_30m' }
      })
      state = null
    }

    if (!state) {
      state = { sessionId: newSessionId(), lastActivityAt: now }
      save(state)
      enqueueTelemetry({
        eventType: 'session',
        eventName: 'session_start',
        sessionId: state.sessionId,
        payload: { reason }
      })
      return
    }

    state.lastActivityAt = now
    save(state)
  } finally {
    touching = false
  }
}

export function endTelemetrySession(reason: 'logout' | 'tab_close') {
  if (!state) state = load()
  if (!state) {
    void flushTelemetry()
    return
  }
  enqueueTelemetry({
    eventType: 'session',
    eventName: 'session_end',
    sessionId: state.sessionId,
    payload: { reason }
  })
  state = null
  save(null)
  void flushTelemetry()
}

export function startTelemetrySessionOnLogin() {
  state = null
  save(null)
  touchTelemetrySession('login')
}
