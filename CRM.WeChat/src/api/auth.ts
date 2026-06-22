/**
 * 认证 API
 */
import apiClient from './client'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  userId: string
  userName: string
  displayName: string
  roles: string[]
}

export interface UserInfo {
  userId: string
  userName: string
  displayName: string
  email: string
  phone: string
  avatar: string
  roles: string[]
  permissions: string[]
}

export interface WechatLoginRequest {
  code: string
}

export const authApi = {
  /** 用户名密码登录 */
  login(data: LoginRequest): Promise<LoginResponse> {
    return apiClient.post('/api/v1/auth/login', data)
  },

  /** 微信授权登录 */
  wechatLogin(data: WechatLoginRequest): Promise<LoginResponse> {
    return apiClient.post('/api/v1/auth/wechat-login', data)
  },

  /** 获取当前用户信息 */
  getCurrentUser(): Promise<UserInfo> {
    return apiClient.get('/api/v1/auth/me')
  },

  /** 退出登录 */
  logout(): Promise<void> {
    return apiClient.post('/api/v1/auth/logout')
  },
}
