import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type LoginRequest, type RegisterRequest } from '@/api'

interface User {
  id: string
  email: string
  userName: string
  isSysAdmin?: boolean
  roleCodes?: string[]
  permissionCodes?: string[]
  departmentIds?: string[]
  // 来自后端 `permission-summary`：用于菜单/功能展示的“部门数据隔离”维度判断
  identityType?: number
  saleDataScope?: number
  purchaseDataScope?: number
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const user = ref<User | null>(
    localStorage.getItem('user') ? JSON.parse(localStorage.getItem('user')!) : null
  )
  const loading = ref(false)

  const isAuthenticated = computed(() => !!token.value)

  async function login(credentials: LoginRequest): Promise<boolean> {
    try {
      loading.value = true
      // client.ts 拦截器已解包 data 层，返回的直接是 { token, userName, email }
      const authData = await authApi.login(credentials) as any
      const tokenVal = authData?.token
      if (tokenVal) {
        token.value = tokenVal
        user.value = {
          email: authData.email,
          userName: authData.userName,
          id: authData.userId || '0',
          isSysAdmin: !!authData.isSysAdmin,
          roleCodes: authData.roleCodes || [],
          permissionCodes: authData.permissionCodes || [],
          departmentIds: authData.departmentIds || []
        }
        localStorage.setItem('token', tokenVal)
        // 登录后补齐 permission-summary（用于 identityType / dataScope），否则菜单隔离可能失效
        const summary = await authApi.getPermissionSummary().catch(() => null) as any
        if (summary) {
          const prevIsSysAdmin = user.value.isSysAdmin === true
          user.value = {
            ...user.value,
            // 不要让 permission-summary 覆盖掉原本的 SYS_ADMIN 标识（避免 identityType 门禁误触发）
            isSysAdmin: prevIsSysAdmin || !!summary?.isSysAdmin,
            roleCodes: summary?.roleCodes || [],
            permissionCodes: summary?.permissionCodes || [],
            departmentIds: summary?.departmentIds || [],
            identityType: Number(summary?.identityType ?? 0),
            saleDataScope: Number(summary?.saleDataScope ?? 1),
            purchaseDataScope: Number(summary?.purchaseDataScope ?? 1)
          }
        } else {
          // 兜底：避免 identityType 仍是默认值导致菜单错误
          user.value.identityType = user.value.identityType ?? 0
        }
        localStorage.setItem('user', JSON.stringify(user.value))
        return true
      }
      return false
    } catch (error) {
      console.error('Login failed:', error)
      return false
    } finally {
      loading.value = false
    }
  }

  async function register(data: RegisterRequest): Promise<boolean> {
    try {
      loading.value = true
      // client.ts 拦截器已解包 data 层，返回的直接是 { token, userName, email }
      const authData = await authApi.register(data) as any
      const tokenVal = authData?.token
      if (tokenVal) {
        token.value = tokenVal
        user.value = {
          email: authData.email,
          userName: authData.userName,
          id: authData.userId || '0',
          isSysAdmin: !!authData.isSysAdmin,
          roleCodes: authData.roleCodes || [],
          permissionCodes: authData.permissionCodes || [],
          departmentIds: authData.departmentIds || []
        }
        localStorage.setItem('token', tokenVal)
        // 注册后同样补齐 permission-summary（用于菜单隔离）
        const summary = await authApi.getPermissionSummary().catch(() => null) as any
        if (summary) {
          const prevIsSysAdmin = user.value.isSysAdmin === true
          user.value = {
            ...user.value,
            // 不要让 permission-summary 覆盖掉原本的 SYS_ADMIN 标识
            isSysAdmin: prevIsSysAdmin || !!summary?.isSysAdmin,
            roleCodes: summary?.roleCodes || [],
            permissionCodes: summary?.permissionCodes || [],
            departmentIds: summary?.departmentIds || [],
            identityType: Number(summary?.identityType ?? 0),
            saleDataScope: Number(summary?.saleDataScope ?? 1),
            purchaseDataScope: Number(summary?.purchaseDataScope ?? 1)
          }
        }
        localStorage.setItem('user', JSON.stringify(user.value))
        return true
      }
      return false
    } catch (error) {
      console.error('Registration failed:', error)
      return false
    } finally {
      loading.value = false
    }
  }

  async function fetchCurrentUser(): Promise<void> {
    if (!token.value) return

    try {
      // client.ts 拦截器已解包 data 层，返回的直接是用户对象
      const userData = await authApi.getCurrentUser() as any
      if (userData) {
        const summary = await authApi.getPermissionSummary() as any
        const prevIsSysAdmin = user.value?.isSysAdmin === true
        user.value = {
          ...userData,
          // 避免 permission-summary 覆盖掉原本的 SYS_ADMIN 标识
          isSysAdmin: prevIsSysAdmin || !!summary?.isSysAdmin,
          roleCodes: summary?.roleCodes || [],
          permissionCodes: summary?.permissionCodes || [],
          departmentIds: summary?.departmentIds || [],
          identityType: Number(summary?.identityType ?? 0),
          saleDataScope: Number(summary?.saleDataScope ?? 1),
          purchaseDataScope: Number(summary?.purchaseDataScope ?? 1)
        }
        localStorage.setItem('user', JSON.stringify(user.value))
      }
    } catch (error) {
      console.error('Failed to fetch current user:', error)
      logout()
    }
  }

  function logout(): void {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  function hasPermission(permissionCode?: string): boolean {
    if (!permissionCode) return true
    if (!user.value) return false
    if (user.value.isSysAdmin) return true
    return (user.value.permissionCodes || []).includes(permissionCode)
  }

  return {
    token,
    user,
    loading,
    isAuthenticated,
    login,
    register,
    logout,
    fetchCurrentUser,
    hasPermission
  }
})
