import { getDisplayTimeZoneId } from './displayTimeZone'

function toDate(input: Date | string | undefined | null): Date | null {
  if (input == null || input === '') return null
  const d = input instanceof Date ? input : new Date(input)
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
