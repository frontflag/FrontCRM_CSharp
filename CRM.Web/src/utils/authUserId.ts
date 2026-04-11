/**
 * 统一解析登录 /me 等载荷中的用户主键。
 * 若误用固定占位（如 '0'）或 uid 为空，会导致全员共用 localStorage key「crm_tabs:0」「crm_tabs:」从而标签串台。
 */
export function normalizeAuthUserId(src: unknown, token?: string | null): string {
  if (src && typeof src === 'object') {
    const o = src as Record<string, unknown>
    for (const k of ['userId', 'UserId', 'id', 'Id'] as const) {
      const c = o[k]
      if (c != null) {
        const s = String(c).trim()
        // 历史前端用 '0' 作为缺省 id，会导致全员共用 crm_tabs:0
        if (s !== '' && s !== '0') return s
      }
    }
  }
  const fromJwt = token ? tryJwtNameIdentifier(token) : ''
  return fromJwt ? fromJwt.trim() : ''
}

function tryJwtNameIdentifier(token: string): string {
  try {
    const parts = token.split('.')
    if (parts.length < 2) return ''
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64 + '==='.slice((base64.length + 3) % 4)
    const json = JSON.parse(atob(padded)) as Record<string, unknown>
    const sub =
      json.sub ??
      json.nameid ??
      json['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
    if (sub != null && String(sub).trim() !== '') return String(sub).trim()
  } catch {
    /* ignore */
  }
  return ''
}
