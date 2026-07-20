export interface TelemetryClientInfo {
  browser: string
  os: string
  deviceType: string
  screenW: number
  screenH: number
  userAgent: string
}

export function collectClientInfo(): TelemetryClientInfo {
  const ua = typeof navigator !== 'undefined' ? navigator.userAgent || '' : ''
  return {
    browser: detectBrowser(ua),
    os: detectOs(ua),
    deviceType: detectDevice(ua),
    screenW: typeof screen !== 'undefined' ? screen.width : 0,
    screenH: typeof screen !== 'undefined' ? screen.height : 0,
    userAgent: ua.slice(0, 500)
  }
}

function detectBrowser(ua: string): string {
  if (/Edg\//i.test(ua)) return 'Edge'
  if (/Chrome\//i.test(ua) && !/Edg\//i.test(ua)) return 'Chrome'
  if (/Firefox\//i.test(ua)) return 'Firefox'
  if (/Safari\//i.test(ua) && !/Chrome\//i.test(ua)) return 'Safari'
  return 'Other'
}

function detectOs(ua: string): string {
  if (/Windows/i.test(ua)) return 'Windows'
  if (/Mac OS X|Macintosh/i.test(ua)) return 'macOS'
  if (/Android/i.test(ua)) return 'Android'
  if (/iPhone|iPad|iPod/i.test(ua)) return 'iOS'
  if (/Linux/i.test(ua)) return 'Linux'
  return 'Other'
}

function detectDevice(ua: string): string {
  if (/Mobi|Android|iPhone|iPad/i.test(ua)) return 'mobile'
  return 'desktop'
}
