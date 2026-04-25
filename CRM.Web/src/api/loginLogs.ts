import apiClient from './client'

export interface LoginLogRow {
  id: string
  userId: string
  userName: string
  loginAt: string
  clientIp: string
  addressLine?: string | null
  loginMethod: number
}

export interface LoginLogPaged {
  total: number
  page: number
  pageSize: number
  items: LoginLogRow[]
}

export interface LoginLogQueryParams {
  loginAtFrom?: string
  loginAtTo?: string
  userId?: string
  page?: number
  pageSize?: number
}

export const loginLogsApi = {
  list(params: LoginLogQueryParams) {
    return apiClient.get<LoginLogPaged>('/api/v1/login-logs', { params })
  }
}
