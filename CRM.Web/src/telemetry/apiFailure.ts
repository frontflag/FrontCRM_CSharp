/** API 埋点「系统失败」判定：超时/断网与 5xx；不含业务 4xx（如查无 404）。 */
export function isTelemetryApiSystemFailure(status: number): boolean {
  const s = status || 0
  return s === 0 || s >= 500
}
