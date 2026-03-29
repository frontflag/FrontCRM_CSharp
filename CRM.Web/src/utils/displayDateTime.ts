import { getDisplayTimeZoneId } from './displayTimeZone'

function normalizeDateInputString(s: string): string {
  const t = s.trim()
  if (!t) return t

  // If backend sends ISO string without timezone, treat it as UTC.
  // Examples:
  // - 2026-03-26T12:34:56      -> 2026-03-26T12:34:56Z
  // - 2026-03-26T12:34:56.123 -> 2026-03-26T12:34:56.123Z
  if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?$/.test(t)) {
    return `${t}Z`
  }

  // If backend sends "YYYY-MM-DD HH:mm:ss" (no timezone), treat it as UTC too.
  if (/^\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?$/.test(t)) {
    return `${t.replace(/\s+/, 'T')}Z`
  }

  return t
}

function toDate(input: Date | string | undefined | null): Date | null {
  if (input == null || input === '') return null
  const d = input instanceof Date ? input : new Date(normalizeDateInputString(String(input)))
  return Number.isNaN(d.getTime()) ? null : d
}

/** 按配置时区显示：YYYY-MM-DD HH:mm（24 小时制） */
export function formatDisplayDateTime(input: Date | string | undefined | null): string {
  const d = toDate(input)
  if (!d) return '--'
  const tz = getDisplayTimeZoneId()
  const ymd = new Intl.DateTimeFormat('sv-SE', {
    timeZone: tz,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(d)
  const hm = new Intl.DateTimeFormat('sv-SE', {
    timeZone: tz,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  }).format(d)
  return `${ymd} ${hm}`.trim()
}

/** 按配置时区显示：YY-MM-DD HH:mm（年份 2 位，24 小时制） */
export function formatDisplayDateTime2DigitYear(input: Date | string | undefined | null): string {
  const d = toDate(input)
  if (!d) return '--'
  const tz = getDisplayTimeZoneId()
  const ymd = new Intl.DateTimeFormat('sv-SE', {
    timeZone: tz,
    year: '2-digit',
    month: '2-digit',
    day: '2-digit'
  }).format(d)
  const hm = new Intl.DateTimeFormat('sv-SE', {
    timeZone: tz,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  }).format(d)
  return `${ymd} ${hm}`.trim()
}

/** 按配置时区显示：YYYY-MM-DD */
export function formatDisplayDate(input: Date | string | undefined | null): string {
  const d = toDate(input)
  if (!d) return '--'
  const tz = getDisplayTimeZoneId()
  return new Intl.DateTimeFormat('sv-SE', {
    timeZone: tz,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(d)
}
