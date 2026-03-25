import apiClient from './client'

export interface DisplaySettingsDto {
  displayTimeZoneId: string
}

/** 无需登录；用于全站展示时区 */
export async function fetchDisplaySettings(): Promise<DisplaySettingsDto> {
  const res = await apiClient.get<any>('/api/v1/system/display')
  if (res && typeof res === 'object' && typeof res.displayTimeZoneId === 'string')
    return res as DisplaySettingsDto
  return { displayTimeZoneId: 'Asia/Shanghai' }
}
