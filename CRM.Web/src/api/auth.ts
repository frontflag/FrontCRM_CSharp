import apiClient from './client'

export interface LoginRequest {
  userName: string
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
  userId: string
  isSysAdmin: boolean
  roleCodes: string[]
  permissionCodes: string[]
  departmentIds: string[]
}

export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
  errorCode: number
}

export interface SalesUserTreeNode {
  value: string
  label: string
  isUser: boolean
  children?: SalesUserTreeNode[]
}

/** 销售/采购组织树节点（结构相同） */
export type OrgUserTreeNode = SalesUserTreeNode

export const authApi = {
  login(data: LoginRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post('/api/v1/auth/login', data)
  },

  register(data: RegisterRequest): Promise<ApiResponse<AuthResponse>> {
    return apiClient.post('/api/v1/auth/register', data)
  },

  getCurrentUser(): Promise<ApiResponse<any>> {
    return apiClient.get('/api/v1/auth/me')
  },

  getPermissionSummary(): Promise<ApiResponse<any>> {
    return apiClient.get('/api/v1/auth/permission-summary')
  },

  getUsers(): Promise<ApiResponse<Array<{ id: string; label: string; userName: string; realName?: string }>>> {
    return apiClient.get('/api/v1/auth/users')
  },

  getSalesUsersTree(): Promise<ApiResponse<SalesUserTreeNode[]>> {
    return apiClient.get('/api/v1/auth/sales-users-tree')
  },

  getPurchaseUsersTree(): Promise<ApiResponse<OrgUserTreeNode[]>> {
    return apiClient.get('/api/v1/auth/purchase-users-tree')
  }
}
