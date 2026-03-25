/**
 * 日期格式化（展示用：按系统参数配置的 IANA 时区）
 */
import { formatDisplayDate, formatDisplayDateTime } from './displayDateTime'

/**
 * @param format 支持 YYYY-MM-DD、YYYY-MM-DD HH:mm；其它模板回退为带时间的格式
 */
export function formatDate(date: Date | string | undefined, format: string = 'YYYY-MM-DD HH:mm'): string {
  if (!date) return '-'
  const wantTime = format.includes('HH')
  const s = wantTime ? formatDisplayDateTime(date) : formatDisplayDate(date)
  if (s === '--') return '-'
  return s
}

/**
 * 格式化日期为相对时间
 */
export function formatRelativeTime(date: Date | string | undefined): string {
  if (!date) return '-'

  const d = typeof date === 'string' ? new Date(date) : date
  const now = new Date()
  const diff = now.getTime() - d.getTime()

  const minute = 60 * 1000
  const hour = 60 * minute
  const day = 24 * hour

  if (diff < minute) {
    return '刚刚'
  } else if (diff < hour) {
    return `${Math.floor(diff / minute)}分钟前`
  } else if (diff < day) {
    return `${Math.floor(diff / hour)}小时前`
  } else if (diff < 2 * day) {
    return '昨天'
  } else if (diff < 7 * day) {
    return `${Math.floor(diff / day)}天前`
  } else {
    return formatDisplayDate(d)
  }
}
