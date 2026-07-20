import { enqueueTelemetry } from './queue'
import { getTelemetrySessionId } from './session'

/** 表单提交结果埋点（校验失败 / 成功 / API 失败） */
export function trackFormResult(opts: {
  formId: string
  outcome: 'success' | 'validation_fail' | 'api_fail'
  pageKey?: string
  message?: string
}) {
  if (!localStorage.getItem('token')) return
  enqueueTelemetry({
    eventType: 'result',
    eventName: 'form_submit',
    sessionId: getTelemetrySessionId(),
    pageKey: opts.pageKey || (typeof location !== 'undefined' ? location.pathname : undefined),
    routePath: typeof location !== 'undefined' ? location.pathname + location.search : undefined,
    payload: {
      formId: opts.formId,
      outcome: opts.outcome,
      message: opts.message?.slice(0, 200)
    }
  })
}
