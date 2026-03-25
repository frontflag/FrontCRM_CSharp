/** 与后端 SysParamCodes.DefaultDisplayTimeZoneId 一致 */
export const DEFAULT_DISPLAY_TIME_ZONE_ID = 'Asia/Shanghai'

let currentTimeZoneId = DEFAULT_DISPLAY_TIME_ZONE_ID

function isValidIanaTimeZone(id: string): boolean {
  try {
    Intl.DateTimeFormat(undefined, { timeZone: id })
    return true
  } catch {
    return false
  }
}

/** 由 App 启动时根据参数表拉取后设置 */
export function setDisplayTimeZoneId(id: string | undefined | null): void {
  const t = (id ?? '').trim()
  if (!t) {
    currentTimeZoneId = DEFAULT_DISPLAY_TIME_ZONE_ID
    return
  }
  currentTimeZoneId = isValidIanaTimeZone(t) ? t : DEFAULT_DISPLAY_TIME_ZONE_ID
}

export function getDisplayTimeZoneId(): string {
  return currentTimeZoneId
}
