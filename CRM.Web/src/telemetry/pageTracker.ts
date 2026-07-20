import type { Router } from 'vue-router'
import { TELEMETRY_ACTIVE_WINDOW_MS } from './constants'
import { enqueueTelemetry } from './queue'
import { touchTelemetrySession } from './session'

interface PageClock {
  pageKey: string
  routePath: string
  enteredAt: number
  visibleMs: number
  activeMs: number
  lastTickAt: number
  lastVisible: boolean
  lastActive: boolean
}

let clock: PageClock | null = null
let lastInteractAt = 0
let ticker: number | null = null

function resolvePageKey(route: { name?: unknown; path: string; matched: { path: string }[] }): string {
  if (typeof route.name === 'string' && route.name.trim()) return route.name.trim()
  const m = route.matched[route.matched.length - 1]
  if (m?.path) return m.path
  return route.path
}

function isActiveNow(): boolean {
  return Date.now() - lastInteractAt <= TELEMETRY_ACTIVE_WINDOW_MS
}

function tick() {
  if (!clock) return
  const now = Date.now()
  const dt = Math.max(0, now - clock.lastTickAt)
  clock.lastTickAt = now
  const visible = typeof document !== 'undefined' && document.visibilityState === 'visible'
  const active = visible && isActiveNow()
  if (visible) clock.visibleMs += dt
  if (active) clock.activeMs += dt
  clock.lastVisible = visible
  clock.lastActive = active
}

function leaveCurrent() {
  if (!clock) return
  tick()
  enqueueTelemetry({
    eventType: 'engagement',
    eventName: 'page_timing',
    pageKey: clock.pageKey,
    routePath: clock.routePath,
    payload: {
      visibleMs: Math.round(clock.visibleMs),
      activeMs: Math.round(clock.activeMs)
    }
  })
  enqueueTelemetry({
    eventType: 'page',
    eventName: 'page_leave',
    pageKey: clock.pageKey,
    routePath: clock.routePath
  })
  clock = null
}

function enter(pageKey: string, routePath: string) {
  const now = Date.now()
  clock = {
    pageKey,
    routePath,
    enteredAt: now,
    visibleMs: 0,
    activeMs: 0,
    lastTickAt: now,
    lastVisible: typeof document !== 'undefined' && document.visibilityState === 'visible',
    lastActive: false
  }
  enqueueTelemetry({
    eventType: 'page',
    eventName: 'page_view',
    pageKey,
    routePath
  })
}

export function markTelemetryInteraction() {
  lastInteractAt = Date.now()
  touchTelemetrySession('activity')
}

export function installPageTracker(router: Router) {
  if (ticker == null) {
    ticker = window.setInterval(tick, 1000)
  }

  window.addEventListener(
    'pointerdown',
    () => markTelemetryInteraction(),
    { capture: true, passive: true }
  )
  window.addEventListener(
    'keydown',
    () => markTelemetryInteraction(),
    { capture: true, passive: true }
  )
  window.addEventListener(
    'scroll',
    () => markTelemetryInteraction(),
    { capture: true, passive: true }
  )

  document.addEventListener('visibilitychange', () => {
    tick()
  })

  router.afterEach((to) => {
    if (!localStorage.getItem('token')) return
    if (to.path === '/login' || to.name === 'Login') {
      leaveCurrent()
      return
    }
    touchTelemetrySession('activity')
    const pageKey = resolvePageKey(to)
    if (clock && clock.pageKey === pageKey && clock.routePath === to.fullPath) return
    leaveCurrent()
    enter(pageKey, to.fullPath)
  })

  window.addEventListener('pagehide', () => {
    leaveCurrent()
  })
}
