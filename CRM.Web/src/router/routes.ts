import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Auth/LoginView.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Auth/RegisterView.vue')
  },
  // 带左侧菜单的布局（需要认证）
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard/DashboardView.vue'),
        meta: { requiresAuth: true, title: '控制台' }
      },
      // 客户管理
      {
        path: 'customers',
        name: 'CustomerList',
        component: () => import('@/views/Customer/CustomerList.vue'),
        meta: { requiresAuth: true, title: '客户管理' }
      },
      {
        path: 'customers/create',
        name: 'CustomerCreate',
        component: () => import('@/views/Customer/CustomerEdit.vue'),
        meta: { requiresAuth: true, title: '新增客户' }
      },
      // 静态路由必须在 :id 动态路由之前
      {
        path: 'customers/recycle-bin',
        name: 'CustomerRecycleBin',
        component: () => import('@/views/Customer/CustomerRecycleBin.vue'),
        meta: { requiresAuth: true, title: '客户回收站' }
      },
      {
        path: 'customers/blacklist',
        name: 'CustomerBlacklist',
        component: () => import('@/views/Customer/CustomerBlacklist.vue'),
        meta: { requiresAuth: true, title: '黑名单管理' }
      },
      {
        path: 'customers/:id',
        name: 'CustomerDetail',
        component: () => import('@/views/Customer/CustomerDetail.vue'),
        meta: { requiresAuth: true, title: '客户详情' }
      },
      {
        path: 'customers/:id/edit',
        name: 'CustomerEdit',
        component: () => import('@/views/Customer/CustomerEdit.vue'),
        meta: { requiresAuth: true, title: '编辑客户' }
      },
      // 需求管理
      {
        path: 'demands',
        name: 'DemandList',
        component: () => import('@/views/Demand/DemandList.vue'),
        meta: { requiresAuth: true, title: '需求管理' }
      },
      {
        path: 'demands/create',
        name: 'DemandCreate',
        component: () => import('@/views/Demand/DemandEdit.vue'),
        meta: { requiresAuth: true, title: '新增需求' }
      },
      {
        path: 'demands/:id',
        name: 'DemandDetail',
        component: () => import('@/views/Demand/DemandDetail.vue'),
        meta: { requiresAuth: true, title: '需求详情' }
      },
      {
        path: 'demands/:id/edit',
        name: 'DemandEdit',
        component: () => import('@/views/Demand/DemandEdit.vue'),
        meta: { requiresAuth: true, title: '编辑需求' }
      },
      // 系统设置
      {
        path: 'dashboard/settings',
        name: 'Settings',
        component: () => import('@/views/Dashboard/SettingsView.vue'),
        meta: { requiresAuth: true, title: '系统设置' }
      }
    ]
  }
]

export default routes
