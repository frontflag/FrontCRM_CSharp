/**
 * 导出文件名追加时间戳：`name.csv` → `name_yyMMddHHmm.csv`
 */
export function withExportTimestamp(fileName: string, at: Date = new Date()): string {
  const raw = String(fileName ?? '').trim() || 'export'
  const pad = (n: number) => String(n).padStart(2, '0')
  const stamp =
    pad(at.getFullYear() % 100) +
    pad(at.getMonth() + 1) +
    pad(at.getDate()) +
    pad(at.getHours()) +
    pad(at.getMinutes())

  const dot = raw.lastIndexOf('.')
  if (dot <= 0) return `${raw}_${stamp}`
  return `${raw.slice(0, dot)}_${stamp}${raw.slice(dot)}`
}

/** 列表导出 query：去掉空值与分页参数。 */
export function toExportQueryString(params?: Record<string, unknown>): string {
  const qs = new URLSearchParams()
  Object.entries(params ?? {}).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return
    if (k === 'page' || k === 'pageSize') return
    if (typeof v === 'boolean') qs.set(k, v ? 'true' : 'false')
    else qs.set(k, String(v))
  })
  return qs.toString()
}

export function downloadCsvBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = withExportTimestamp(fileName)
  a.click()
  URL.revokeObjectURL(url)
}
