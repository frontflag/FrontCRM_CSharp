/**
 * 从 axios / fetch / 拦截器抛出的异常中解析人类可读的错误说明。
 * 覆盖：ApiResponse { success, message }、ASP.NET ValidationProblemDetails { errors }、ProblemDetails { title, detail }。
 */
export function getApiErrorMessage(error: unknown, fallback = '操作失败，请稍后重试'): string {
  if (error === null || error === undefined) return fallback
  if (typeof error === 'string') return error || fallback
  if (error instanceof Error && error.message && !/^Request failed with status code \d+$/i.test(error.message)) {
    return error.message
  }

  const err = error as Record<string, unknown>
  const ax = err as { response?: { status?: number; data?: unknown }; message?: string }
  const data = ax.response?.data
  const status = ax.response?.status

  if (status === 504) {
    return 'AI 调用超时（504）：Nginx 代理默认 60 秒会中断 Kimi 请求，请将 proxy_read_timeout 调整为 300s 后 reload nginx。'
  }

  if (typeof data === 'string' && data.trim()) {
    const text = data.trim()
    if (text.includes('504 Gateway Time-out') || text.includes('<html')) {
      return 'AI 调用超时（504）：网关等待后端响应超时，请运维加大 Nginx proxy_read_timeout（建议 300s）。'
    }
    return text
  }

  if (data && typeof data === 'object') {
    const d = data as Record<string, unknown>
    if (typeof d.message === 'string' && d.message.trim()) return d.message.trim()

    const errors = d.errors
    if (errors && typeof errors === 'object') {
      const firstKey = Object.keys(errors as object)[0]
      if (firstKey) {
        const val = (errors as Record<string, unknown>)[firstKey]
        if (Array.isArray(val) && val.length > 0 && typeof val[0] === 'string') return val[0]
        if (typeof val === 'string') return val
      }
    }

    if (typeof d.detail === 'string' && d.detail.trim()) return d.detail.trim()
    if (typeof d.title === 'string' && d.title.trim()) return d.title.trim()
  }

  if (typeof ax.message === 'string' && ax.message.trim()) return ax.message.trim()
  if (error instanceof Error) return error.message || fallback
  return fallback
}
