import axios from 'axios'
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios'

const API_BASE_URL = ''  // 使用相对路径，走Vite代理

class ApiClient {
  private instance: AxiosInstance

  constructor() {
    this.instance = axios.create({
      baseURL: API_BASE_URL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json'
      }
    })

    this.setupInterceptors()
  }

  private setupInterceptors(): void {
    // Request interceptor
    this.instance.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => {
        return Promise.reject(error)
      }
    )

    // Response interceptor
    // 后端统一返回格式: { success: bool, data: T, message: string, errorCode: number }
    // 拦截器统一解包 data 层，让调用方直接拿到业务数据
    this.instance.interceptors.response.use(
      (response: AxiosResponse) => {
        const apiResponse = response.data
        if (apiResponse && apiResponse.success !== undefined) {
          if (apiResponse.success) {
            // 成功时返回 data 字段（业务数据）
            return apiResponse.data !== undefined ? apiResponse.data : apiResponse
          } else {
            // 业务失败时抛出错误，携带后端 message
            return Promise.reject(new Error(apiResponse.message || '请求失败'))
          }
        }
        return apiResponse
      },
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          window.location.href = '/login'
          return Promise.reject(error)
        }
        const responseData = error.response?.data
        if (responseData && responseData.success !== undefined) {
          return Promise.reject(new Error(responseData.message || '请求失败'))
        }
        return Promise.reject(error)
      }
    )
  }

  public get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.get(url, config)
  }

  public post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.post(url, data, config)
  }

  public put<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.put(url, data, config)
  }

  public delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.delete(url, config)
  }
}

export const apiClient = new ApiClient()
export default apiClient
