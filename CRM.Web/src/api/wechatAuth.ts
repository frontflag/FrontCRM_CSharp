import request from '@/api/client'

// ==================== 扫码登录相关 ====================

/**
 * 获取微信登录二维码
 */
export function getWechatQrCode(data: { deviceType: string }) {
  return request.post<{
    ticket: string
    qrCodeUrl: string
    expireSeconds: number
  }>('/api/v1/auth/wechat/qrcode', data)
}

/**
 * 检查微信登录状态
 */
export function checkWechatLoginStatus(ticket: string) {
  return request.get<{
    status: number
    message: string
    authData?: {
      token: string
      userName: string
      email: string
      userId: string
      isSysAdmin: boolean
      roleCodes: string[]
      permissionCodes: string[]
      departmentIds: string[]
    }
  }>(`/api/v1/auth/wechat/status/${ticket}`)
}

// ==================== 微信绑定相关 ====================

/**
 * 获取当前微信绑定信息
 */
export function getWechatBindInfo() {
  return request.get<{
    isBound: boolean
    nickname?: string
    avatarUrl?: string
    bindTime?: string
  }>('/api/v1/auth/wechat/bind-info')
}

/**
 * 生成微信绑定二维码
 */
export function generateBindQrCode() {
  return request.post<{
    bindId: string
    qrCodeUrl: string
    expireSeconds: number
  }>('/api/v1/auth/wechat/bind-qrcode')
}

/**
 * 检查微信绑定状态
 */
export function checkBindStatus(bindId: string) {
  return request.get<{
    status: string
    message: string
    nickname?: string
  }>(`/api/v1/auth/wechat/bind-status/${bindId}`)
}

/**
 * 解除微信绑定
 */
export function unbindWechat() {
  return request.post<boolean>('/api/v1/auth/wechat/unbind')
}
