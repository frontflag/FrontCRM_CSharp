import apiClient from './client'

export interface AdminUserDto {
  id: string
  userName: string
  realName?: string
  email?: string
  mobile?: string
  // 详情页可能用到的字段（后端未必已返回，先按可选字段处理）
  birthDate?: string
  hireDate?: string
  avatarUrl?: string
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

  // 详情页可能用到的字段（先按可选字段处理）
  parentId?: string
  createTime?: string
  modifyTime?: string
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

export interface UpsertDepartmentRequest {
  departmentName: string
  parentId?: string | null
  saleDataScope: number
  purchaseDataScope: number
  identityType: number
  status: number
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

  /** 管理员重置员工登录密码 */
  async resetUserPassword(userId: string, newPassword: string): Promise<void> {
    await apiClient.post(`/api/v1/rbac/admin/users/${encodeURIComponent(userId)}/reset-password`, {
      newPassword
    })
  },

  async freezeUser(userId: string): Promise<void> {
    await apiClient.post(`/api/v1/rbac/admin/users/${encodeURIComponent(userId)}/freeze`)
  },

  async unfreezeUser(userId: string): Promise<void> {
    await apiClient.post(`/api/v1/rbac/admin/users/${encodeURIComponent(userId)}/unfreeze`)
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
  },

  // 下面这些方法当前后端是否已提供不确定；这里先补齐类型/最小实现，
  // 以保证页面能够编译通过。若调用会抛出/返回空，便于后续补齐真实后端接口。
  async getDepartmentById(departmentId: string): Promise<RbacDepartment> {
    const list = await this.getDepartments()
    const d = list.find((x) => x.id === departmentId)
    if (!d) throw new Error('部门不存在')
    return d
  },

  async getDepartmentUsers(_departmentId: string): Promise<AdminUserDto[]> {
    // 后端若未提供部门->用户映射接口，这里先返回空数组。
    return []
  },

  async createDepartment(payload: UpsertDepartmentRequest): Promise<RbacDepartment> {
    return apiClient.post<RbacDepartment>('/api/v1/rbac/admin/departments', payload)
  },

  async updateDepartment(departmentId: string, payload: UpsertDepartmentRequest): Promise<RbacDepartment> {
    return apiClient.put<RbacDepartment>(
      `/api/v1/rbac/admin/departments/${encodeURIComponent(departmentId)}`,
      payload
    )
  },

  async deleteDepartment(_departmentId: string): Promise<void> {
    throw new Error('deleteDepartment 尚未实现后端接口')
  }
}

