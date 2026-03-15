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
      const response = await authApi.login(credentials)

      if (response.success && response.data) {
        token.value = response.data.token
        user.value = {
          email: response.data.email,
          userName: response.data.userName,
          id: '0'
        }

        localStorage.setItem('token', response.data.token)
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
      const response = await authApi.register(data)

      if (response.success && response.data) {
        token.value = response.data.token
        user.value = {
          email: response.data.email,
          userName: response.data.userName,
          id: '0'
        }

        localStorage.setItem('token', response.data.token)
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
      const response = await authApi.getCurrentUser()
      if (response.success && response.data) {
        user.value = response.data
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
