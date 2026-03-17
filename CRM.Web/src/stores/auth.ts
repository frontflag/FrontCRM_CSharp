import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type LoginRequest, type RegisterRequest } from '@/api'

interface User {
  id: string
  email: string
  userName: string
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
          id: '0'
        }
        localStorage.setItem('token', tokenVal)
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
          id: '0'
        }
        localStorage.setItem('token', tokenVal)
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
        user.value = userData
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

  return {
    token,
    user,
    loading,
    isAuthenticated,
    login,
    register,
    logout,
    fetchCurrentUser
  }
})
