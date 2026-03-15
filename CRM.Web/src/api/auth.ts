import apiClient from './client'

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  userName: string
  email: string
  password: string
}

export interface AuthResponse {
  token: string
  userName: string
  email: string
}

export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errorCode: number
}

export const authApi = {
  login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post('/api/v1/auth/login', data)
  },

  register(data: RegisterRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post('/api/v1/auth/register', data)
  },

  getCurrentUser(): Promise<ApiResponse<any>> {
    return apiClient.get('/api/v1/auth/me')
  }
}
