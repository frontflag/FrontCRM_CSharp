import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores'
import routes from './routes'

const router = createRouter({
  history: createWebHistory((import.meta as any).env.BASE_URL),
  routes
})

router.onError((err) => {
  console.error('[router] 路由或异步组件加载失败:', err)
})

// Navigation guard
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()
  // PREVIEW MODE: skip auth check when preview_mode is set in localStorage
  const previewMode = localStorage.getItem('preview_mode') === 'true'

  // Debug 主列表页：免登录（与 routes 中 DebugList requiresAuth: false 一致；避免父级 requiresAuth 合并歧义）
  const isDebugList =
    to.name === 'DebugList' || to.path === '/debug' || to.path === '/debug/'

  if (previewMode || isDebugList) {
    next()
  } else if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (to.meta.permission && !authStore.hasPermission(to.meta.permission as string)) {
    next('/dashboard')
  } else if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    next('/dashboard')
  } else {
    next()
  }
})

export default router
