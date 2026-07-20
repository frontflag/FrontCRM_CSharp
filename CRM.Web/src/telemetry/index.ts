import type { Router } from 'vue-router'
import { installActionCapture } from './actionCapture'
import { installJsErrorTelemetry } from './errors'
import { installPageTracker } from './pageTracker'
import {
  bindTelemetrySession,
  flushTelemetry,
  installTelemetryPageLifecycle,
  setTelemetryEnabled
} from './queue'
import {
  bumpTelemetryActivity,
  endTelemetrySession,
  getTelemetrySessionId,
  startTelemetrySessionOnLogin,
  touchTelemetrySession
} from './session'

export { enqueueTelemetry, flushTelemetry } from './queue'
export { isTelemetryApiExcluded, toPathTemplate } from './constants'
export { trackApiTelemetry } from './apiTiming'
export { trackFormResult } from './formResult'

export function initTelemetry(router: Router) {
  // onActivity 只用 bump，禁止再调用 touch（否则 enqueue↔touch 死循环导致白屏）
  bindTelemetrySession(getTelemetrySessionId, () => bumpTelemetryActivity())
  setTelemetryEnabled(!!localStorage.getItem('token'))
  installTelemetryPageLifecycle()
  installPageTracker(router)
  installActionCapture()
  installJsErrorTelemetry()

  if (localStorage.getItem('token')) {
    touchTelemetrySession('activity')
  }

  // 关页只冲刷队列；session_end 由登出 / 30 分钟空闲处理（避免切后台误结束会话）
  window.addEventListener('pagehide', () => {
    void flushTelemetry()
  })
}

export function onTelemetryLogin() {
  setTelemetryEnabled(true)
  startTelemetrySessionOnLogin()
}

export function onTelemetryLogout() {
  endTelemetrySession('logout')
  setTelemetryEnabled(false)
}
