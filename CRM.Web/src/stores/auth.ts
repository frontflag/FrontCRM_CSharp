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

function readUserFromStorage(): User | null {
  const raw = localStorage.getItem('user')
  if (!raw) return null
  try {
    return JSON.parse(raw) as User
  } catch {
    localStorage.removeItem('user')
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const user = ref<User | null>(readUserFromStorage())
  const loading = ref(false)

  const isAuthenticated = computed(() => !!token.value)

  /** 将登录/注册/模拟登录返回的载荷写入会话（含 permission-summary） */
  async function applyAuthPayload(authData: any): Promise<boolean> {
    const tokenVal = authData?.token
    if (!tokenVal) return false
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
    const summary = await authApi.getPermissionSummary().catch(() => null) as any
    if (summary) {
      const prevIsSysAdmin = user.value.isSysAdmin === true
      user.value = {
        ...user.value,
        isSysAdmin: prevIsSysAdmin || !!summary?.isSysAdmin,
        roleCodes: summary?.roleCodes || [],
        permissionCodes: summary?.permissionCodes || [],
        departmentIds: summary?.departmentIds || [],
        identityType: Number(summary?.identityType ?? 0),
        saleDataScope: Number(summary?.saleDataScope ?? 1),
        purchaseDataScope: Number(summary?.purchaseDataScope ?? 1)
      }
    } else {
      user.value.identityType = user.value.identityType ?? 0
    }
    localStorage.setItem('user', JSON.stringify(user.value))
    return true
  }

  async function login(credentials: LoginRequest): Promise<boolean> {
    try {
      loading.value = true
      const authData = await authApi.login(credentials) as any
      return await applyAuthPayload(authData)
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
      const authData = await authApi.register(data) as any
      return await applyAuthPayload(authData)
    } catch (error) {
      console.error('Registration failed:', error)
      return false
    } finally {
      loading.value = false
    }
  }

  /** 管理员模拟登录为员工：服务端校验 SYS_ADMIN 后签发目标账号 JWT；失败时抛错（含后端 message） */
  async function impersonate(targetUserId: string): Promise<void> {
    try {
      loading.value = true
      const authData = await authApi.impersonate(targetUserId) as any
      const ok = await applyAuthPayload(authData)
      if (!ok) throw new Error('模拟登录失败')
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

  /**
   * 主部门身份与「客户 / 供应商」路由隔离（与 AppLayout 主菜单一致）。
   * IdentityType: 0 None, 1 Sales, 2 Purchaser, 3 PurchaseAssistant, 4 CustService, 5 Finance, 6 Logistics
   */
  function isIdentityBlockedForPermission(permissionCode: string): boolean {
    if (!user.value || user.value.isSysAdmin) return false
    const t = user.value.identityType ?? 0
    if (permissionCode.startsWith('customer.')) {
      return t === 2 || t === 3 || t === 6
    }
    if (permissionCode.startsWith('vendor.')) {
      return t === 1 || t === 6
    }
    return false
  }

  return {
    token,
    user,
    loading,
    isAuthenticated,
    login,
    register,
    impersonate,
    logout,
    fetchCurrentUser,
    hasPermission,
    isIdentityBlockedForPermission
  }
})
