/**
 * 从 axios / fetch / 拦截器抛出的异常中解析人类可读的错误说明。
 * 覆盖：ApiResponse { success, message }、ASP.NET ValidationProblemDetails { errors }、ProblemDetails { title, detail }。
 */

/** 面向业务用户的 AI 超时说明（勿暴露 Nginx/运维配置） */
export const AI_INVOKE_TIMEOUT_USER_MESSAGE =
  'AI 调查耗时较长，连接已超时，请稍后点击「重试」。'

function sanitizeUserFacingMessage(message: string): string {
  const m = message.trim()
  if (!m) return m
  if (
    /proxy_read_timeout|reload nginx|nginx-ai-invoke|scripts\/nginx/i.test(m) ||
    (/AI 调用超时|504/i.test(m) && /nginx|proxy_read|运维|网关|Kimi/i.test(m))
  ) {
    return AI_INVOKE_TIMEOUT_USER_MESSAGE
  }
  return m
}

export function getApiErrorMessage(error: unknown, fallback = '操作失败，请稍后重试'): string {
  if (error === null || error === undefined) return fallback
  if (typeof error === 'string') return sanitizeUserFacingMessage(error) || fallback
  if (error instanceof Error && error.message && !/^Request failed with status code \d+$/i.test(error.message)) {
    return sanitizeUserFacingMessage(error.message)
  }

  const err = error as Record<string, unknown>
  const ax = err as { response?: { status?: number; data?: unknown }; message?: string }
  const data = ax.response?.data
  const status = ax.response?.status

  if (status === 504) {
    return AI_INVOKE_TIMEOUT_USER_MESSAGE
  }

  if (typeof data === 'string' && data.trim()) {
    const text = data.trim()
    if (text.includes('504 Gateway Time-out') || text.includes('<html')) {
      return AI_INVOKE_TIMEOUT_USER_MESSAGE
    }
    return sanitizeUserFacingMessage(text)
  }

  if (data && typeof data === 'object') {
    const d = data as Record<string, unknown>
    if (typeof d.message === 'string' && d.message.trim()) return sanitizeUserFacingMessage(d.message)

    const errors = d.errors
    if (errors && typeof errors === 'object') {
      const firstKey = Object.keys(errors as object)[0]
      if (firstKey) {
        const val = (errors as Record<string, unknown>)[firstKey]
        if (Array.isArray(val) && val.length > 0 && typeof val[0] === 'string') return sanitizeUserFacingMessage(val[0])
        if (typeof val === 'string') return sanitizeUserFacingMessage(val)
      }
    }

    if (typeof d.detail === 'string' && d.detail.trim()) return sanitizeUserFacingMessage(d.detail)
    if (typeof d.title === 'string' && d.title.trim()) return sanitizeUserFacingMessage(d.title)
  }

  if (typeof ax.message === 'string' && ax.message.trim()) return sanitizeUserFacingMessage(ax.message)
  if (error instanceof Error) return sanitizeUserFacingMessage(error.message) || fallback
  return fallback
}
