function pad(n: number) {
  return String(n).padStart(2, '0')
}

function toYmd(d: Date) {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** 上一个自然月（本地日历）：今天 9/7 → 8/1–8/31 */
export function lastCalendarMonthRange(today = new Date()): [string, string] {
  const from = new Date(today.getFullYear(), today.getMonth() - 1, 1)
  const to = new Date(today.getFullYear(), today.getMonth(), 0)
  return [toYmd(from), toYmd(to)]
}

export function todayYmd(today = new Date()) {
  return toYmd(today)
}
