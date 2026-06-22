/**
 * 认证状态管理
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type LoginResponse, type UserInfo } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string>('')
  const userInfo = ref<UserInfo | null>(null)

  const isLoggedIn = computed(() => !!token.value)
  const displayName = computed(() => userInfo.value?.displayName || userInfo.value?.userName || '')

  /** 初始化：从本地存储恢复登录状态 */
  function init() {
    const savedToken = uni.getStorageSync('token')
    if (savedToken) {
      token.value = savedToken
    }
  }

  /** 用户名密码登录 */
  async function login(username: string, password: string): Promise<LoginResponse> {
    const res = await authApi.login({ username, password })
    token.value = res.token
    uni.setStorageSync('token', res.token)
    return res
  }

  /** 微信授权登录 */
  async function wechatLogin(code: string): Promise<LoginResponse> {
    const res = await authApi.wechatLogin({ code })
    token.value = res.token
    uni.setStorageSync('token', res.token)
    return res
  }

  /** 获取用户信息 */
  async function fetchUserInfo(): Promise<UserInfo> {
    const info = await authApi.getCurrentUser()
    userInfo.value = info
    uni.setStorageSync('userInfo', JSON.stringify(info))
    return info
  }

  /** 退出登录 */
  async function logout(): Promise<void> {
    try {
      await authApi.logout()
    } catch {
      // 忽略退出请求错误
    }
    token.value = ''
    userInfo.value = null
    uni.removeStorageSync('token')
    uni.removeStorageSync('userInfo')
    uni.reLaunch({ url: '/pages/login/login' })
  }

  /** 清除认证状态（401 时调用） */
  function clearAuth() {
    token.value = ''
    userInfo.value = null
    uni.removeStorageSync('token')
    uni.removeStorageSync('userInfo')
  }

  return {
    token,
    userInfo,
    isLoggedIn,
    displayName,
    init,
    login,
    wechatLogin,
    fetchUserInfo,
    logout,
    clearAuth,
  }
})
