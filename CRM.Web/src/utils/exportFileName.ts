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
