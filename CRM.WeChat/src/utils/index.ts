/**
 * 工具函数
 */

/**
 * 日期格式化
 */
export function formatDate(dateStr: string | Date, format = 'YYYY-MM-DD'): string {
  const d = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
  if (isNaN(d.getTime())) return '—'

  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hours = String(d.getHours()).padStart(2, '0')
  const minutes = String(d.getMinutes()).padStart(2, '0')
  const seconds = String(d.getSeconds()).padStart(2, '0')

  return format
    .replace('YYYY', String(year))
    .replace('MM', month)
    .replace('DD', day)
    .replace('HH', hours)
    .replace('mm', minutes)
    .replace('ss', seconds)
}

/**
 * 金额格式化
 */
export function formatMoney(amount: number, currency = 1): string {
  if (amount === null || amount === undefined) return '—'

  const symbol = currency === 2 ? '$' : '¥'
  if (Math.abs(amount) >= 10000) {
    return `${symbol}${(amount / 10000).toFixed(2)}万`
  }
  return `${symbol}${amount.toFixed(2)}`
}

/**
 * 手机号脱敏
 */
export function maskPhone(phone: string): string {
  if (!phone || phone.length < 7) return phone || '—'
  return phone.replace(/(\d{3})\d{4}(\d{4})/, '$1****$2')
}

/**
 * Toast 提示
 */
export function showToast(title: string, icon: 'success' | 'error' | 'none' = 'none') {
  uni.showToast({ title, icon, duration: 2000 })
}

/**
 * 确认弹窗
 */
export function showConfirm(content: string, title = '提示'): Promise<boolean> {
  return new Promise((resolve) => {
    uni.showModal({
      title,
      content,
      success: (res) => resolve(res.confirm),
      fail: () => resolve(false),
    })
  })
}

/**
 * 获取当前平台类型
 */
export function getPlatform(): 'h5' | 'mp-weixin' | 'app' {
  // #ifdef H5
  return 'h5'
  // #endif
  // #ifdef MP-WEIXIN
  return 'mp-weixin'
  // #endif
  // #ifdef APP-PLUS
  return 'app'
  // #endif
  return 'h5'
}
