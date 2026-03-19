/**
 * 日期格式化工具函数
 */

/**
 * 格式化日期为字符串
 * @param date 日期对象或字符串
 * @param format 格式模板，默认为 'YYYY-MM-DD HH:mm'
 * @returns 格式化后的字符串
 */
export function formatDate(date: Date | string | undefined, format: string = 'YYYY-MM-DD HH:mm'): string {
  if (!date) return '-'

  const d = typeof date === 'string' ? new Date(date) : date

  if (isNaN(d.getTime())) return '-'

  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hour = String(d.getHours()).padStart(2, '0')
  const minute = String(d.getMinutes()).padStart(2, '0')
  const second = String(d.getSeconds()).padStart(2, '0')

  return format
    .replace('YYYY', String(year))
    .replace('MM', month)
    .replace('DD', day)
    .replace('HH', hour)
    .replace('mm', minute)
    .replace('ss', second)
}

/**
 * 格式化日期为相对时间
 * @param date 日期对象或字符串
 * @returns 相对时间字符串，如：刚刚、5分钟前、1小时前、昨天、3天前
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
    return formatDate(d, 'YYYY-MM-DD')
  }
}
