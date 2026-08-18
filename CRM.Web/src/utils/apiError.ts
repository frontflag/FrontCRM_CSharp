/**
 * 从 axios / fetch / 拦截器抛出的异常中解析人类可读的错误说明。
 * 覆盖：ApiResponse { success, message }、ASP.NET ValidationProblemDetails { errors }、ProblemDetails { title, detail }。
 */

/** 面向业务用户的 AI 超时说明（勿暴露 Nginx/运维配置） */
export const AI_INVOKE_TIMEOUT_USER_MESSAGE =
  'AI 调查耗时较长，连接已超时，请稍后点击「重试」。'

/** 厂商额度/鉴权失败时勿把组织号、密钥片段、原始 JSON 展示给业务用户 */
export const AI_PROVIDER_QUOTA_USER_MESSAGE =
  'AI 服务额度不足或账号已暂停，请联系管理员检查套餐与余额后重试。'

export const AI_PROVIDER_AUTH_USER_MESSAGE =
  'AI 服务鉴权失败，请联系管理员检查密钥配置。'

function sanitizeUserFacingMessage(message: string): string {
  const m = message.trim()
  if (!m) return m
  if (
    /proxy_read_timeout|reload nginx|nginx-ai-invoke|scripts\/nginx/i.test(m) ||
    (/AI 调用超时|504/i.test(m) && /nginx|proxy_read|运维|网关|Kimi/i.test(m))
  ) {
    return AI_INVOKE_TIMEOUT_USER_MESSAGE
  }
  if (
    /insufficient balance|exceeded_current_quota|quota_exceeded|exceeded your current quota/i.test(m) ||
    (/AI 调用失败\s*\(429\)/i.test(m) && /suspended|recharge|quota|org-|ak-/i.test(m)) ||
    (/org-[a-z0-9]+/i.test(m) && /suspended|insufficient|quota/i.test(m))
  ) {
    return AI_PROVIDER_QUOTA_USER_MESSAGE
  }
  if (/invalid authentication|incorrect api key|invalid_api_key|AI 调用失败\s*\(40[13]\)/i.test(m)) {
    return AI_PROVIDER_AUTH_USER_MESSAGE
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
    const errorId =
      (typeof d.errorId === 'string' && d.errorId.trim()) ||
      (typeof d.ErrorId === 'string' && d.ErrorId.trim()) ||
      ''
    if (typeof d.message === 'string' && d.message.trim()) {
      const msg = sanitizeUserFacingMessage(d.message)
      if (errorId && !msg.includes(errorId) && !msg.includes('错误编号')) {
        return `${msg}（错误编号 ${errorId}）`
      }
      return msg
    }

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
