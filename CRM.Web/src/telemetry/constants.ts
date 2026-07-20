/** API 埋点排除前缀（代码常量，开发维护） */
export const TELEMETRY_API_EXCLUDE_PREFIXES = [
  '/api/v1/telemetry/',
  '/api/v1/auth/wechat/qr',
  '/api/v1/auth/wechat/status',
  '/api/v1/auth/wechat/login',
  '/api/v1/wechat/'
] as const

export const TELEMETRY_FLUSH_INTERVAL_MS = 30_000
export const TELEMETRY_FLUSH_MAX_BATCH = 20
export const TELEMETRY_SESSION_IDLE_MS = 30 * 60 * 1000
export const TELEMETRY_ACTIVE_WINDOW_MS = 5 * 60 * 1000

export function isTelemetryApiExcluded(url: string): boolean {
  const path = normalizeApiPath(url)
  return TELEMETRY_API_EXCLUDE_PREFIXES.some((p) => path.startsWith(p))
}

export function normalizeApiPath(url: string): string {
  try {
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return new URL(url).pathname
    }
  } catch {
    /* ignore */
  }
  const q = url.indexOf('?')
  return (q >= 0 ? url.slice(0, q) : url).trim() || '/'
}

/** 将具体 Id 收成模板，避免聚合打散 */
export function toPathTemplate(url: string): string {
  let path = normalizeApiPath(url)
  path = path.replace(
    /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi,
    ':id'
  )
  path = path.replace(/\/\d+(?=\/|$)/g, '/:id')
  return path
}
