import apiClient from './client'

export interface AdminUserDto {
  id: string
  userName: string
  realName?: string
  email?: string
  mobile?: string
  status: number
  roleIds: string[]
  roleCodes: string[]
  departmentIds: string[]
  primaryDepartmentId?: string
  primaryDepartmentName?: string
  primaryDepartmentPath?: string
}

export interface RbacRole {
  id: string
  roleCode: string
  roleName: string
  description?: string
  status: number
}

export interface RbacPermission {
  id: string
  permissionCode: string
  permissionName: string
  permissionType: string
  resource?: string
  action?: string
  status: number
}

export interface RbacDepartment {
  id: string
  departmentName: string
  path?: string
  level: number
  saleDataScope: number
  purchaseDataScope: number
  identityType: number
  status: number
}

export interface CreateAdminUserRequest {
  userName: string
  password: string
  realName?: string
  email?: string
  mobile?: string
  status: number
  roleIds?: string[]
  departmentIds?: string[]
  primaryDepartmentId?: string
}

export interface UpdateAdminUserRequest {
  realName?: string
  email?: string
  mobile?: string
  status?: number
  roleIds?: string[]
  departmentIds?: string[]
  primaryDepartmentId?: string
}

export interface CreateAdminRoleRequest {
  roleCode: string
  roleName: string
  description?: string
  status: number
}

export interface UpdateAdminRoleRequest {
  roleCode?: string
  roleName?: string
  description?: string
  status?: number
}

export interface CreateAdminPermissionRequest {
  permissionCode: string
  permissionName: string
  permissionType: string
  resource?: string
  action?: string
  status: number
}

export interface UpdateAdminPermissionRequest {
  permissionCode?: string
  permissionName?: string
  permissionType?: string
  resource?: string
  action?: string
  status?: number
}

export const rbacAdminApi = {
  // users
  async getUsers(): Promise<AdminUserDto[]> {
    return apiClient.get<AdminUserDto[]>('/api/v1/rbac/admin/users')
  },
  async getUserById(userId: string): Promise<AdminUserDto> {
    return apiClient.get<AdminUserDto>(`/api/v1/rbac/admin/users/${encodeURIComponent(userId)}`)
  },
  async createUser(payload: CreateAdminUserRequest): Promise<AdminUserDto> {
    return apiClient.post<AdminUserDto>('/api/v1/rbac/admin/users', payload)
  },
  async updateUser(userId: string, payload: UpdateAdminUserRequest): Promise<AdminUserDto> {
    return apiClient.put<AdminUserDto>(`/api/v1/rbac/admin/users/${encodeURIComponent(userId)}`, payload)
  },
  async deleteUser(userId: string): Promise<void> {
    await apiClient.delete(`/api/v1/rbac/admin/users/${encodeURIComponent(userId)}`)
  },

  // roles
  async getRoles(): Promise<RbacRole[]> {
    return apiClient.get<RbacRole[]>('/api/v1/rbac/roles')
  },
  async createRole(payload: CreateAdminRoleRequest): Promise<RbacRole> {
    return apiClient.post<RbacRole>('/api/v1/rbac/admin/roles', payload)
  },
  async updateRole(roleId: string, payload: UpdateAdminRoleRequest): Promise<RbacRole> {
    return apiClient.put<RbacRole>(`/api/v1/rbac/admin/roles/${encodeURIComponent(roleId)}`, payload)
  },
  async deleteRole(roleId: string): Promise<void> {
    await apiClient.delete(`/api/v1/rbac/admin/roles/${encodeURIComponent(roleId)}`)
  },
  async getRolePermissionIds(roleId: string): Promise<string[]> {
    return apiClient.get<string[]>(`/api/v1/rbac/admin/roles/${encodeURIComponent(roleId)}/permissions`)
  },
  async assignRolePermissions(roleId: string, permissionIds: string[]): Promise<void> {
    await apiClient.post(`/api/v1/rbac/roles/${encodeURIComponent(roleId)}/permissions`, { ids: permissionIds })
  },

  // permissions
  async getPermissions(): Promise<RbacPermission[]> {
    return apiClient.get<RbacPermission[]>('/api/v1/rbac/permissions')
  },
  async createPermission(payload: CreateAdminPermissionRequest): Promise<RbacPermission> {
    return apiClient.post<RbacPermission>('/api/v1/rbac/admin/permissions', payload)
  },
  async updatePermission(permissionId: string, payload: UpdateAdminPermissionRequest): Promise<RbacPermission> {
    return apiClient.put<RbacPermission>(
      `/api/v1/rbac/admin/permissions/${encodeURIComponent(permissionId)}`,
      payload
    )
  },
  async deletePermission(permissionId: string): Promise<void> {
    await apiClient.delete(`/api/v1/rbac/admin/permissions/${encodeURIComponent(permissionId)}`)
  },

  // departments
  async getDepartments(): Promise<RbacDepartment[]> {
    return apiClient.get<RbacDepartment[]>('/api/v1/rbac/departments')
  }
}

