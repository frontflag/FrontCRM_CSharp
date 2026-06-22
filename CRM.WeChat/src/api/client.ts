/**
 * API 客户端 - 微信端
 * 封装 uni.request，提供与 Web 端一致的 API 调用体验
 */

// 后端 API 基础地址
// H5 端：通过 Vite 代理转发到后端
// 小程序端：直接请求后端地址（需配置服务器域名白名单）
const API_BASE_URL = process.env.VITE_API_BASE_URL || ''

// 生产环境后端地址（小程序端使用）
const PROD_API_BASE = 'https://your-domain.com'

interface ApiResponse<T = any> {
  success: boolean
  data: T
  message?: string
  errorCode?: string
}

interface RequestOptions {
  url: string
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'
  data?: any
  params?: Record<string, any>
  header?: Record<string, string>
  showLoading?: boolean
  loadingText?: string
}

/**
 * 获取存储的 token
 */
function getToken(): string {
  // #ifdef H5
  return uni.getStorageSync('token') || ''
  // #endif
  // #ifdef MP-WEIXIN
  return uni.getStorageSync('token') || ''
  // #endif
}

/**
 * 构建完整 URL
 */
function buildUrl(url: string, params?: Record<string, any>): string {
  let fullUrl = url.startsWith('http') ? url : `${API_BASE_URL}${url}`

  if (params && Object.keys(params).length > 0) {
    const query = Object.entries(params)
      .filter(([, v]) => v !== undefined && v !== null && v !== '')
      .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
      .join('&')
    if (query) {
      fullUrl += `?${query}`
    }
  }

  return fullUrl
}

/**
 * 核心请求方法
 */
function request<T = any>(options: RequestOptions): Promise<T> {
  const { url, method = 'GET', data, params, header = {}, showLoading = false, loadingText = '加载中...' } = options

  if (showLoading) {
    uni.showLoading({ title: loadingText, mask: true })
  }

  const token = getToken()

  return new Promise<T>((resolve, reject) => {
    uni.request({
      url: buildUrl(url, method === 'GET' ? params : undefined),
      method: method as any,
      data: method !== 'GET' ? data : undefined,
      header: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...header,
      },
      success: (res: any) => {
        if (showLoading) uni.hideLoading()

        const statusCode = res.statusCode
        const body = res.data as ApiResponse<T>

        if (statusCode === 401) {
          // token 过期，清除登录状态
          uni.removeStorageSync('token')
          uni.removeStorageSync('userInfo')
          uni.reLaunch({ url: '/pages/login/login' })
          reject(new Error('登录已过期，请重新登录'))
          return
        }

        if (statusCode >= 200 && statusCode < 300) {
          // 兼容后端统一返回格式 { success, data, message }
          if (body && typeof body === 'object' && 'success' in body) {
            if (body.success) {
              resolve(body.data as T)
            } else {
              reject(new Error(body.message || '请求失败'))
            }
          } else {
            resolve(body as unknown as T)
          }
        } else {
          const msg = body?.message || `请求失败 (${statusCode})`
          reject(new Error(msg))
        }
      },
      fail: (err: any) => {
        if (showLoading) uni.hideLoading()
        reject(new Error(err.errMsg || '网络请求失败'))
      },
    })
  })
}

// 导出便捷方法
export const apiClient = {
  get<T = any>(url: string, params?: Record<string, any>): Promise<T> {
    return request<T>({ url, method: 'GET', params })
  },

  post<T = any>(url: string, data?: any): Promise<T> {
    return request<T>({ url, method: 'POST', data })
  },

  put<T = any>(url: string, data?: any): Promise<T> {
    return request<T>({ url, method: 'PUT', data })
  },

  delete<T = any>(url: string, data?: any): Promise<T> {
    return request<T>({ url, method: 'DELETE', data })
  },

  request<T = any>(options: RequestOptions): Promise<T> {
    return request<T>(options)
  },
}

export default apiClient
