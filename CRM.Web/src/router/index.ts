import { createRouter, createWebHistory } from 'vue-router'
import type { RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores'
import { canAccessPurchaseOrderCreatePage } from '@/utils/purchaseOrderCreateGate'
import { canAccessStockingPurchaseList } from '@/utils/stockingPurchaseListAccess'
import { canAccessInventoryOpsCheck } from '@/utils/inventoryOpsCheckAccess'
import {
  canAccessCustomsModule,
  isCustomerEditOrDetailRoute,
  isCustomerManagementRoute,
  isCustomsModuleRoute,
  isVendorEditOrDetailRoute,
  isVendorManagementRoute
} from '@/utils/departmentModuleGate'
import routes from './routes'

const router = createRouter({
  history: createWebHistory((import.meta as any).env.BASE_URL),
  routes
})

router.onError((err) => {
  console.error('[router] 路由或异步组件加载失败:', err)
})

/** meta.permission / meta.permissions / meta.paramsModule */
function routePermissionAllowed(authStore: ReturnType<typeof useAuthStore>, to: RouteLocationNormalized): boolean {
  const check = (p: string) => {
    const ok = p.startsWith('system.') || p === 'rbac.manage' || p === 'biz.ai.admin'
      ? authStore.canAccessSystemPermission(p)
      : authStore.hasPermission(p)
    return ok && !authStore.isIdentityBlockedForPermission(p)
  }
  const paramsModule = to.meta.paramsModule as 'sales' | 'purchase' | 'finance' | undefined
  if (paramsModule && !authStore.canAccessParamsModule(paramsModule)) return false
  const multi = to.meta.permissions
  if (Array.isArray(multi) && multi.length > 0) {
    return multi.some((p) => typeof p === 'string' && check(p))
  }
  const one = to.meta.permission as string | undefined
  if (one) return check(one)
  // 仅 paramsModule、无 permission：模块入口通过即可
  if (paramsModule) return true
  return true
}

// Navigation guard
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()
  // PREVIEW MODE: skip auth check when preview_mode is set in localStorage
  const previewMode = localStorage.getItem('preview_mode') === 'true'

  // Debug 主列表页：免登录（与 routes 中 DebugList requiresAuth: false 一致；避免父级 requiresAuth 合并歧义）
  const isDebugList =
    to.name === 'DebugList' || to.path === '/debug' || to.path === '/debug/'

  const isPublicPage =
    isDebugList ||
    to.name === 'ReleaseNotes' ||
    to.name === 'NotFound' ||
    to.path === '/release-notes' ||
    to.path === '/404'

  // /debug/super 等隐蔽页：未登录 / 非 SA → 404（不跳登录、不跳 dashboard）
  if (to.meta.denyAs404 === true) {
    const allowed =
      authStore.isAuthenticated &&
      (to.meta.sysAdminOnly !== true || authStore.user?.isSysAdmin === true)
    if (!allowed) {
      next({ name: 'NotFound', replace: true })
      return
    }
  }

  if (previewMode || isPublicPage) {
    next()
  } else if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (to.meta.requiresAuth && to.meta.sysAdminOnly === true && authStore.user?.isSysAdmin !== true) {
    next(to.meta.denyAs404 === true ? { name: 'NotFound', replace: true } : '/dashboard')
  } else if (to.meta.requiresAuth && to.meta.adminOrManagerOnly === true && !authStore.canForceDelete()) {
    next('/dashboard')
  } else if (to.meta.requiresAuth && to.meta.inventoryOpsCheckAccess === true && !canAccessInventoryOpsCheck(authStore.user)) {
    next('/dashboard')
  } else if (
    to.meta.requiresAuth &&
    to.meta.stockingPurchaseListAccess === true &&
    !canAccessStockingPurchaseList(authStore.user, authStore.hasPermission('purchase-order.read'))
  ) {
    next('/dashboard')
  } else if (to.meta.requiresAuth && to.meta.purchaseOrderCreateAccess === true) {
    const ok = canAccessPurchaseOrderCreatePage({
      isSysAdmin: authStore.user?.isSysAdmin,
      identityType: authStore.user?.identityType,
      roleCodes: authStore.user?.roleCodes,
      hasPermission: (code) => authStore.hasPermission(code)
    })
    if (!ok) next('/dashboard')
    else next()
  } else if (
    to.meta.requiresAuth &&
    authStore.isCustomerManagementHidden() &&
    (isCustomerManagementRoute(to) || isCustomerEditOrDetailRoute(to))
  ) {
    next('/dashboard')
  } else if (
    to.meta.requiresAuth &&
    authStore.isVendorManagementHidden() &&
    (isVendorManagementRoute(to) || isVendorEditOrDetailRoute(to))
  ) {
    next('/dashboard')
  } else if (
    to.meta.requiresAuth &&
    isCustomsModuleRoute(to) &&
    !canAccessCustomsModule({
      isSysAdmin: authStore.user?.isSysAdmin,
      isSysManager: authStore.user?.isSysManager,
      hasBizDataBypass: authStore.user?.hasBizDataBypass,
      identityType: authStore.user?.identityType
    })
  ) {
    next('/dashboard')
  } else if (
    to.meta.requiresAuth &&
    (to.meta.permission || (Array.isArray(to.meta.permissions) && to.meta.permissions.length > 0)) &&
    !routePermissionAllowed(authStore, to)
  ) {
    next('/dashboard')
  } else if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    next('/dashboard')
  } else {
    next()
  }
})

export default router
