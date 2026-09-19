/** 设置计划提成对话框括号里的区间文案。优先用接口字段，缺了再按上海日历补。 */

type Rec = Record<string, unknown>

function asRec(v: unknown): Rec | null {
  return v && typeof v === 'object' ? (v as Rec) : null
}

function text(...vals: unknown[]): string {
  for (const v of vals) {
    if (typeof v === 'string' && v.trim()) return v.trim()
  }
  return ''
}

function pick(src: unknown, camel: string, pascal: string): unknown {
  const rec = asRec(src)
  return rec?.[camel] ?? rec?.[pascal]
}

function parseMonth(raw: unknown): number | null {
  if (typeof raw === 'string') {
    const m = raw.match(/^(\d{4})-(\d{2})/)
    if (m) {
      const month = Number(m[2])
      return month >= 1 && month <= 12 ? month : null
    }
  }
  if (raw instanceof Date && !Number.isNaN(raw.getTime())) {
    return raw.getUTCMonth() + 1
  }
  return null
}

export function formatMonthSpan(from: unknown, to: unknown): string {
  const a = parseMonth(from)
  const b = parseMonth(to)
  if (a == null || b == null) return ''
  return `${a}–${b}月`
}

export function shanghaiYmd(now = new Date()): { y: number; m: number; d: number } {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).formatToParts(now)
  const num = (type: Intl.DateTimeFormatPartTypes) =>
    Number(parts.find((p) => p.type === type)?.value)
  return { y: num('year'), m: num('month'), d: num('day') }
}

function addMonths(y: number, m: number, delta: number): { y: number; m: number } {
  const dt = new Date(Date.UTC(y, m - 1 + delta, 1))
  return { y: dt.getUTCFullYear(), m: dt.getUTCMonth() + 1 }
}

/** 与后端 `CommissionTerm.GetOpenWindow` 同一双月，用于接口缺字段时的标签。 */
export function openWindowMonthSpan(now = new Date()): string {
  const { y, m, d } = shanghaiYmd(now)
  const even = m % 2 === 0
  const lock = even ? (d < 2 ? { y, m } : addMonths(y, m, 2)) : addMonths(y, m, 1)
  const start = addMonths(lock.y, lock.m, -2)
  const end = addMonths(lock.y, lock.m, -1)
  return `${start.m}–${end.m}月`
}

export function resolveIncentiveTermPeriod(src: unknown, now = new Date()): string {
  const term = pick(src, 'term', 'Term')
  return (
    text(pick(term, 'monthSpan', 'MonthSpan')) ||
    formatMonthSpan(pick(term, 'from', 'From'), pick(term, 'to', 'To')) ||
    openWindowMonthSpan(now)
  )
}

export function resolveIncentiveYearPeriod(src: unknown, now = new Date()): string {
  const year = pick(src, 'year', 'Year')
  return text(pick(year, 'periodKey', 'PeriodKey')) || String(shanghaiYmd(now).y)
}
