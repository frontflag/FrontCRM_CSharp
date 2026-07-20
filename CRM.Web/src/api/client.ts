import axios from 'axios'
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { getApiErrorMessage } from '@/utils/apiError'
import { trackApiTelemetry } from '@/telemetry/apiTiming'

const API_BASE_URL = ''  // 使用相对路径，走Vite代理

/** 供调用方识别 HTTP 状态（如 404 时做降级）；勿依赖 message 中的英文固定串 */
export type ApiRejectedError = Error & { httpStatus?: number }

function rejectWithHttpStatus(message: string, status?: number): Promise<never> {
  const err = new Error(message) as ApiRejectedError
  err.httpStatus = status
  return Promise.reject(err)
}

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
      (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('token')
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        ;(config as InternalAxiosRequestConfig & { metadata?: { start: number } }).metadata = {
          start: Date.now()
        }
        return config
      },
      (error) => {
        return Promise.reject(error)
      }
    )

    const reportTiming = (
      config: InternalAxiosRequestConfig | undefined,
      status: number,
      errorId?: string | null,
      message?: string | null
    ) => {
      try {
        const start = (config as InternalAxiosRequestConfig & { metadata?: { start: number } })?.metadata
          ?.start
        const durationMs = start != null ? Date.now() - start : 0
        const url = `${config?.baseURL || ''}${config?.url || ''}` || config?.url || ''
        trackApiTelemetry({
          method: config?.method || 'get',
          url,
          status,
          durationMs,
          errorId,
          message
        })
      } catch {
        /* never break API */
      }
    }

    // Response interceptor
    // 后端统一返回格式: { success: bool, data: T, message: string, errorCode: number }
    // 拦截器统一解包 data 层，让调用方直接拿到业务数据
    this.instance.interceptors.response.use(
      (response: AxiosResponse) => {
        const apiResponse = response.data
        const ok =
          apiResponse &&
          (apiResponse.success === true ||
            apiResponse.Success === true)
        const fail =
          apiResponse &&
          (apiResponse.success === false ||
            apiResponse.Success === false)
        const hasEnvelope =
          apiResponse &&
          (apiResponse.success !== undefined ||
            apiResponse.Success !== undefined ||
            apiResponse.data !== undefined ||
            apiResponse.Data !== undefined)

        if (hasEnvelope) {
          if (ok) {
            reportTiming(response.config, response.status)
            const payload =
              apiResponse.data !== undefined ? apiResponse.data : apiResponse.Data
            if (payload !== undefined) {
              return payload
            }
            return apiResponse
          }
          if (fail) {
            let msg = apiResponse.message ?? apiResponse.Message ?? '请求失败'
            const errorId = apiResponse.errorId ?? apiResponse.ErrorId
            if (
              typeof errorId === 'string' &&
              errorId.trim() &&
              !String(msg).includes(errorId) &&
              !String(msg).includes('错误编号')
            ) {
              msg = `${msg}（错误编号 ${errorId}）`
            }
            reportTiming(
              response.config,
              response.status >= 400 ? response.status : 400,
              typeof errorId === 'string' ? errorId : null,
              String(msg)
            )
            return rejectWithHttpStatus(msg, response.status)
          }
        }
        reportTiming(response.config, response.status)
        return apiResponse
      },
      (error) => {
        const status = error.response?.status ?? 0
        const responseData = error.response?.data
        const errorId =
          responseData && typeof responseData === 'object'
            ? (responseData as { errorId?: string; ErrorId?: string }).errorId ||
              (responseData as { ErrorId?: string }).ErrorId
            : undefined
        reportTiming(
          error.config,
          status,
          errorId,
          getApiErrorMessage(error, '请求失败')
        )

        if (error.response?.status === 401) {
          localStorage.removeItem('token')
          localStorage.removeItem('user')
          localStorage.removeItem('crm_tabs')
          localStorage.removeItem('crm_active_tab')
          localStorage.removeItem('crm_tabs:0')
          localStorage.removeItem('crm_active_tab:0')
          localStorage.removeItem('crm_tabs:')
          localStorage.removeItem('crm_active_tab:')
          window.location.href = '/login'
          return Promise.reject(error)
        }
        if (
          responseData &&
          (responseData.success !== undefined || responseData.Success !== undefined)
        ) {
          return rejectWithHttpStatus(
            responseData.message ?? responseData.Message ?? '请求失败',
            error.response?.status
          )
        }
        // 400 ValidationProblemDetails 等：无 success 字段
        const msg = getApiErrorMessage(error, '请求失败')
        return rejectWithHttpStatus(msg, error.response?.status)
      }
    )
  }

  public get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.get(url, config)
  }

  /**
   * 下载/预览二进制。
   * 注意：响应拦截器在「无 JSON 信封」时直接 return data，对 blob 请求会得到 Blob 本身而非 AxiosResponse，故需兼容两种形态。
   */
  public getBlob(url: string, config?: AxiosRequestConfig): Promise<Blob> {
    return this.instance.get(url, { ...config, responseType: 'blob' }).then((res: unknown) => {
      const d = res instanceof Blob ? res : (res as AxiosResponse<Blob>)?.data
      if (d instanceof Blob) return d
      return Promise.reject(new Error('无效的 Blob 响应'))
    })
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

  public patch<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    return this.instance.patch(url, data, config)
  }
}

export const apiClient = new ApiClient()
export default apiClient
