<template>
  <div class="app-layout">
    <!-- 左侧菜单面板 -->
    <aside class="sidebar" :class="{ collapsed: isCollapsed }">
      <!-- Logo 区域 -->
      <div class="sidebar-logo">
        <div class="logo-icon">
          <svg viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
            <polygon points="20,2 36,11 36,29 20,38 4,29 4,11" fill="none" stroke="#00D4FF" stroke-width="1.5"/>
            <polygon points="20,8 30,14 30,26 20,32 10,26 10,14" fill="rgba(0,212,255,0.1)" stroke="#0066FF" stroke-width="1"/>
            <circle cx="20" cy="20" r="5" fill="#00D4FF" opacity="0.9"/>
          </svg>
        </div>
        <transition name="fade">
          <div class="logo-text" v-if="!isCollapsed">
            <span class="logo-title">FrontCRM</span>
            <span class="logo-subtitle">AI智销系统</span>
          </div>
        </transition>
        <button class="collapse-btn" @click="toggleCollapse" :title="isCollapsed ? '展开菜单' : '收起菜单'">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path v-if="!isCollapsed" d="M15 18l-6-6 6-6"/>
            <path v-else d="M9 18l6-6-6-6"/>
          </svg>
        </button>
      </div>

      <!-- 菜单区域 -->
      <nav class="sidebar-nav">
        <!-- 主菜单标签 -->
        <div class="menu-section-label" v-if="!isCollapsed">主菜单</div>

        <!-- 控制台 -->
        <router-link to="/dashboard" class="menu-item" active-class="active" exact>
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <rect x="3" y="3" width="7" height="7" rx="1"/>
              <rect x="14" y="3" width="7" height="7" rx="1"/>
              <rect x="3" y="14" width="7" height="7" rx="1"/>
              <rect x="14" y="14" width="7" height="7" rx="1"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">控制台</span>
          <span class="active-dot" v-if="!isCollapsed"></span>
        </router-link>

        <!-- 基础资料 -->
        <div class="menu-section-label" v-if="!isCollapsed">基础资料</div>

        <div class="menu-group">
          <button class="menu-item has-children" @click="toggleGroup('customers')" :class="{ 'group-open': openGroups.customers }">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
                <circle cx="12" cy="7" r="4"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">客户管理</span>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.customers }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </button>
          <div class="submenu" v-if="!isCollapsed && openGroups.customers">
            <router-link to="/customers" class="submenu-item" active-class="active" exact>客户列表</router-link>
            <router-link to="/customers/recycle-bin" class="submenu-item" active-class="active">回收站</router-link>
            <router-link to="/customers/blacklist" class="submenu-item" active-class="active">黑名单管理</router-link>
          </div>
        </div>

        <button class="menu-item has-children" @click="toggleGroup('vendors')" :class="{ 'group-open': openGroups.vendors }">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <path d="M23 21v-2a4 4 0 00-3-3.87"/>
              <path d="M16 3.13a4 4 0 010 7.75"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">供应商管理</span>
          <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.vendors }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="6 9 12 15 18 9"/>
          </svg>
        </button>
        <div class="submenu" v-if="!isCollapsed && openGroups.vendors">
          <router-link to="/vendors" class="submenu-item" active-class="active" exact>供应商列表</router-link>
          <router-link to="/vendors/recycle-bin" class="submenu-item" active-class="active">回收站</router-link>
          <router-link to="/vendors/blacklist" class="submenu-item" active-class="active">黑名单管理</router-link>
        </div>

        <!-- 询价 -->
        <div class="menu-section-label" v-if="!isCollapsed">询价</div>

        <router-link to="/rfqs" class="menu-item" active-class="active">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
              <polyline points="14 2 14 8 20 8"/>
              <line x1="16" y1="13" x2="8" y2="13"/>
              <line x1="16" y1="17" x2="8" y2="17"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">需求管理</span>
          <span class="active-dot" v-if="!isCollapsed"></span>
        </router-link>

        <router-link to="/boms" class="menu-item" active-class="active">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <rect x="3" y="3" width="18" height="18" rx="2"/>
              <line x1="3" y1="9" x2="21" y2="9"/>
              <line x1="9" y1="21" x2="9" y2="9"/>
              <path d="M13 13h5M13 17h3"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">BOM 快速报价</span>
          <span class="active-dot" v-if="!isCollapsed"></span>
        </router-link>

        <router-link to="/quotes" class="menu-item" active-class="active">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M12 2v20M2 12h20"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">报价管理</span>
          <span class="active-dot" v-if="!isCollapsed"></span>
        </router-link>

        <!-- 订单 -->
        <div class="menu-section-label" v-if="!isCollapsed">订单</div>

        <!-- 销售管理 -->
        <div class="menu-group">
          <button class="menu-item has-children" @click="toggleGroup('sales')" :class="{ 'group-open': openGroups.sales }">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">销售管理</span>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.sales }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </button>
          <div class="submenu" v-if="!isCollapsed && openGroups.sales">
            <router-link to="/sales-orders" class="submenu-item" active-class="active">销售订单</router-link>
            <button class="submenu-item" @click="handleUnimplemented('发货管理')">发货管理</button>
            <button class="submenu-item" @click="handleUnimplemented('销售退货')">销售退货</button>
          </div>
        </div>

        <!-- 采购管理 -->
        <div class="menu-group">
          <button class="menu-item has-children" @click="toggleGroup('purchase')" :class="{ 'group-open': openGroups.purchase }">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/>
                <line x1="3" y1="6" x2="21" y2="6"/>
                <path d="M16 10a4 4 0 01-8 0"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">采购管理</span>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.purchase }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </button>
          <div class="submenu" v-if="!isCollapsed && openGroups.purchase">
            <router-link to="/purchase-orders" class="submenu-item" active-class="active">采购订单</router-link>
            <button class="submenu-item" @click="handleUnimplemented('收货管理')">收货管理</button>
            <button class="submenu-item" @click="handleUnimplemented('采购退货')">采购退货</button>
          </div>
        </div>

        <!-- 库存 -->
        <div class="menu-section-label" v-if="!isCollapsed">库存</div>

        <!-- 库存管理 -->
        <div class="menu-group">
          <button class="menu-item has-children" @click="toggleGroup('inventory')" :class="{ 'group-open': openGroups.inventory }">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
                <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
                <line x1="12" y1="22.08" x2="12" y2="12"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">库存管理</span>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.inventory }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </button>
          <div class="submenu" v-if="!isCollapsed && openGroups.inventory">
            <router-link to="/inventory/stock-in" class="submenu-item" active-class="active">入库管理</router-link>
            <router-link to="/inventory/list" class="submenu-item" active-class="active">库存管理</router-link>
            <router-link to="/inventory/stock-out" class="submenu-item" active-class="active">出库管理</router-link>
            <router-link to="/inventory/transfer" class="submenu-item" active-class="active">库存调拨</router-link>
            <router-link to="/inventory/check" class="submenu-item" active-class="active">库存盘点</router-link>
          </div>
        </div>

        <!-- 财务管理 -->
        <div class="menu-section-label" v-if="!isCollapsed">财务管理</div>
        <div class="menu-group">
          <button class="menu-item has-children" @click="toggleGroup('finance')" :class="{ 'group-open': openGroups.finance }">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <rect x="2" y="5" width="20" height="14" rx="2"/>
                <line x1="2" y1="10" x2="22" y2="10"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">财务管理</span>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.finance }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </button>
          <div class="submenu" v-if="!isCollapsed && openGroups.finance">
            <router-link to="/finance/payments" class="submenu-item" active-class="active">付款管理</router-link>
            <router-link to="/finance/receipts" class="submenu-item" active-class="active">收款管理</router-link>
            <router-link to="/finance/purchase-invoices" class="submenu-item" active-class="active">进项发票</router-link>
            <router-link to="/finance/sell-invoices" class="submenu-item" active-class="active">销项发票</router-link>
          </div>
        </div>

        <!-- 数据分析 -->
        <div class="menu-section-label" v-if="!isCollapsed">数据分析</div>

        <button class="menu-item" @click="handleUnimplemented('报表分析')">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <line x1="18" y1="20" x2="18" y2="10"/>
              <line x1="12" y1="20" x2="12" y2="4"/>
              <line x1="6" y1="20" x2="6" y2="14"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">报表分析</span>
        </button>

        <!-- 系统 -->
        <div class="menu-section-label" v-if="!isCollapsed">系统</div>

        <router-link to="/drafts" class="menu-item" active-class="active">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M4 4h16v16H4z"/>
              <path d="M8 8h8M8 12h8M8 16h5"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">草稿箱</span>
        </router-link>

        <router-link to="/dashboard/settings" class="menu-item" active-class="active">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <circle cx="12" cy="12" r="3"/>
              <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">系统设置</span>
        </router-link>
      </nav>


    </aside>

    <!-- 主内容区域 -->
    <main class="main-content">
      <!-- 顶部栏 -->
      <header class="top-bar">
        <div class="top-bar-left">
          <h1 class="page-title">{{ currentPageTitle }}</h1>
        </div>
        <div class="top-bar-right">
          <button class="top-btn" title="通知">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
              <path d="M13.73 21a2 2 0 01-3.46 0"/>
            </svg>
            <span class="badge">3</span>
          </button>
          <!-- 右上角用户下拉菜单 -->
          <div class="user-dropdown" v-click-outside="closeDropdown">
            <div class="top-user" @click="toggleDropdown">
              <div class="top-user-avatar">{{ userInitial }}</div>
              <span class="top-user-name">{{ userName }}</span>
              <svg class="dropdown-chevron" :class="{ open: dropdownOpen }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 9l6 6 6-6"/>
              </svg>
            </div>
            <transition name="dropdown">
              <div class="dropdown-menu" v-if="dropdownOpen">
                <div class="dropdown-header">
                  <div class="dropdown-avatar">{{ userInitial }}</div>
                  <div class="dropdown-user-info">
                    <span class="dropdown-username">{{ userName }}</span>
                    <span class="dropdown-email">{{ userEmail }}</span>
                  </div>
                </div>
                <div class="dropdown-divider"></div>
                <router-link to="/profile" class="dropdown-item" @click="closeDropdown">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
                    <circle cx="12" cy="7" r="4"/>
                  </svg>
                  个人设置
                </router-link>
                <div class="dropdown-divider"></div>
                <button class="dropdown-item danger" @click="handleLogout">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/>
                    <polyline points="16 17 21 12 16 7"/>
                    <line x1="21" y1="12" x2="9" y2="12"/>
                  </svg>
                  退出系统
                </button>
              </div>
            </transition>
          </div>
        </div>
      </header>

      <!-- Tab Bar 多标签页 -->
      <div class="tab-bar" v-if="tabs.length > 0">
        <div
          v-for="tab in tabs"
          :key="tab.path"
          class="tab-item"
          :class="{ active: tab.path === activeTab }"
          @click="activateTab(tab)"
          @contextmenu.prevent="openContextMenu($event, tab)"
          :draggable="true"
          @dragstart="onDragStart($event, tab)"
          @dragover.prevent="onDragOver($event, tab)"
          @drop="onDrop($event, tab)"
          @dragend="onDragEnd"
        >
          <span class="tab-label">{{ tab.title }}</span>
          <button class="tab-close" @click.stop="closeTab(tab)" v-if="tabs.length > 1">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <line x1="18" y1="6" x2="6" y2="18"/>
              <line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
          <div class="tab-active-bar" v-if="tab.path === activeTab"></div>
        </div>
      </div>

      <!-- 右键菜单 -->
      <div
        class="context-menu"
        v-if="contextMenu.visible"
        :style="{ top: contextMenu.y + 'px', left: contextMenu.x + 'px' }"
        v-click-outside="closeContextMenu"
      >
        <div class="context-menu-title">{{ contextMenu.tab?.title }}</div>
        <div class="context-menu-divider"></div>
        <button class="context-menu-item" @click="contextMenuClose('current')">关闭当前</button>
        <button class="context-menu-item" @click="contextMenuClose('others')" :disabled="tabs.length <= 1">关闭其他</button>
        <button class="context-menu-item" @click="contextMenuClose('right')" :disabled="isLastTab">关闭右侧</button>
        <div class="context-menu-divider"></div>
        <button class="context-menu-item" @click="contextMenuClose('all')">全部关闭</button>
      </div>

      <!-- 页面内容 -->
      <div class="content-wrapper">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, type Directive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores'
import { ElMessageBox, ElNotification } from 'element-plus'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const isCollapsed = ref(false)
const openGroups = ref({
  purchase: false,
  sales: false,
  inventory: false,
  customers: false,
  vendors: false,
  finance: false
})

const toggleCollapse = () => {
  isCollapsed.value = !isCollapsed.value
  if (isCollapsed.value) {
    openGroups.value = { purchase: false, sales: false, inventory: false, customers: false, vendors: false, finance: false }
  }
}

const toggleGroup = (group: keyof typeof openGroups.value) => {
  openGroups.value[group] = !openGroups.value[group]
}

const userName = computed(() => authStore.user?.userName || '管理员')
const userEmail = computed(() => authStore.user?.email || '')
const userInitial = computed(() => (authStore.user?.userName || '管')[0].toUpperCase())

const pageTitleMap: Record<string, string> = {
  '/dashboard': '控制台',
  '/customers': '客户管理',
  '/customers/create': '新增客户',
  '/customers/recycle-bin': '客户回收站',
  '/customers/blacklist': '黑名单管理',
  '/vendors': '供应商管理',
  '/vendors/create': '新增供应商',
  '/inventory/list': '库存列表',
  '/inventory/stock-in': '入库管理',
  '/inventory/stock-out': '出库管理',
  '/inventory/transfer': '库存调拨',
  '/inventory/check': '库存盘点',
  '/reports': '报表分析',
  '/dashboard/settings': '系统设置',
  '/rfqs': 'RFQ 管理',
  '/quotes': '报价管理',
  '/purchase-orders': '采购订单',
  '/sales-orders': '销售订单',
  '/profile': '个人设置',
  '/drafts': '草稿箱',
}

const currentPageTitle = computed(() => {
  const path = route.path
  if (pageTitleMap[path]) return pageTitleMap[path]
  if (path.includes('/customers/') && path.includes('/edit')) return '编辑客户'
  if (path.includes('/customers/')) return '客户详情'
  if (path.includes('/vendors/') && path.includes('/edit')) return '编辑供应商'
  if (path === '/vendors/create') return '新增供应商'
  return route.meta?.title as string || 'FrontCRM'
})

// ===== 用户下拉菜单 =====
const dropdownOpen = ref(false)
const toggleDropdown = () => { dropdownOpen.value = !dropdownOpen.value }
const closeDropdown = () => { dropdownOpen.value = false }

const handleLogout = async () => {
  closeDropdown()
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '退出登录', {
      confirmButtonText: '确定退出',
      cancelButtonText: '取消',
      type: 'warning'
    })
    authStore.logout()
    router.push('/login')
  } catch {
    // 用户取消
  }
}

const handleUnimplemented = (name: string) => {
  ElNotification.info({ title: '功能开发中', message: `「${name}」功能正在开发中，敬请期待` })
}

// ===== Tab Bar 多标签页 =====
interface TabItem {
  path: string
  title: string
}

const STORAGE_KEY = 'crm_tabs'
const ACTIVE_KEY = 'crm_active_tab'

const loadTabs = (): TabItem[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return saved ? JSON.parse(saved) : []
  } catch { return [] }
}

const tabs = ref<TabItem[]>(loadTabs())
const activeTab = ref<string>(localStorage.getItem(ACTIVE_KEY) || '')

const saveTabs = () => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(tabs.value))
  localStorage.setItem(ACTIVE_KEY, activeTab.value)
}

// 监听路由变化，自动添加/激活标签
watch(() => route.path, (newPath) => {
  const title = currentPageTitle.value
  const exists = tabs.value.find(t => t.path === newPath)
  if (!exists) {
    tabs.value.push({ path: newPath, title })
  }
  activeTab.value = newPath
  saveTabs()
}, { immediate: true })

const activateTab = (tab: TabItem) => {
  activeTab.value = tab.path
  router.push(tab.path)
  saveTabs()
}

const closeTab = (tab: TabItem) => {
  const idx = tabs.value.findIndex(t => t.path === tab.path)
  tabs.value.splice(idx, 1)
  if (activeTab.value === tab.path) {
    const next = tabs.value[idx] || tabs.value[idx - 1]
    if (next) {
      activeTab.value = next.path
      router.push(next.path)
    } else {
      activeTab.value = ''
      router.push('/dashboard')
    }
  }
  saveTabs()
}

// 拖拽排序
const dragSource = ref<TabItem | null>(null)

const onDragStart = (_e: DragEvent, tab: TabItem) => {
  dragSource.value = tab
}

const onDragOver = (_e: DragEvent, _tab: TabItem) => {}

const onDrop = (_e: DragEvent, target: TabItem) => {
  if (!dragSource.value || dragSource.value.path === target.path) return
  const fromIdx = tabs.value.findIndex(t => t.path === dragSource.value!.path)
  const toIdx = tabs.value.findIndex(t => t.path === target.path)
  const newTabs = [...tabs.value]
  newTabs.splice(fromIdx, 1)
  newTabs.splice(toIdx, 0, dragSource.value)
  tabs.value = newTabs
  saveTabs()
}

const onDragEnd = () => {
  dragSource.value = null
}

// 右键菜单
const contextMenu = ref({
  visible: false,
  x: 0,
  y: 0,
  tab: null as TabItem | null
})

const isLastTab = computed(() => {
  if (!contextMenu.value.tab) return true
  const idx = tabs.value.findIndex(t => t.path === contextMenu.value.tab!.path)
  return idx === tabs.value.length - 1
})

const openContextMenu = (e: MouseEvent, tab: TabItem) => {
  contextMenu.value = { visible: true, x: e.clientX, y: e.clientY, tab }
}

const closeContextMenu = () => {
  contextMenu.value.visible = false
}

const contextMenuClose = (type: 'current' | 'others' | 'right' | 'all') => {
  const tab = contextMenu.value.tab
  if (!tab) return
  const idx = tabs.value.findIndex(t => t.path === tab.path)
  if (type === 'current') {
    closeTab(tab)
  } else if (type === 'others') {
    tabs.value = [tab]
    activeTab.value = tab.path
    router.push(tab.path)
  } else if (type === 'right') {
    tabs.value = tabs.value.slice(0, idx + 1)
    if (!tabs.value.find(t => t.path === activeTab.value)) {
      activeTab.value = tab.path
      router.push(tab.path)
    }
  } else if (type === 'all') {
    tabs.value = []
    activeTab.value = ''
    router.push('/dashboard')
  }
  saveTabs()
  closeContextMenu()
}

// v-click-outside 指令
const vClickOutside: Directive = {
  mounted(el, binding) {
    el._clickOutsideHandler = (event: MouseEvent) => {
      if (!el.contains(event.target as Node)) {
        binding.value()
      }
    }
    document.addEventListener('mousedown', el._clickOutsideHandler)
  },
  unmounted(el) {
    document.removeEventListener('mousedown', el._clickOutsideHandler)
  }
}

onMounted(() => {
  // 如果没有标签，初始化当前路由
  if (tabs.value.length === 0) {
    const title = currentPageTitle.value
    tabs.value.push({ path: route.path, title })
    activeTab.value = route.path
    saveTabs()
  }
})
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;

// ============================================================
// AppLayout — 深海量子风布局
// 设计规范：三层面板 #192A3F / #0A1628 / #162233
// 侧边栏宽度：展开 240px / 收起 64px
// ============================================================

.app-layout {
  display: flex;
  width: 100vw;
  height: 100vh;
  background: vars.$layer-1;
  overflow: clip; // 使用 clip 而非 hidden，避免裁剪 position:fixed 的元素（如 ElMessage）
}

// ── 侧边栏 ──────────────────────────────────────────────────
.sidebar {
  width: 240px;
  height: 100vh;
  background: vars.$layer-2;
  border-right: 1px solid rgba(0, 212, 255, 0.12);
  display: flex;
  flex-direction: column;
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  flex-shrink: 0;
  position: relative;
  z-index: 10;

  &.collapsed {
    width: 64px;
  }
}

// Logo 区域
.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 16px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
  min-height: 72px;
  position: relative;

  .logo-icon {
    width: 36px;
    height: 36px;
    flex-shrink: 0;

    svg {
      width: 100%;
      height: 100%;
    }
  }

  .logo-text {
    display: flex;
    flex-direction: column;
    overflow: hidden;
    flex: 1;
  }

  .logo-title {
    font-family: 'Orbitron', monospace;
    font-size: 15px;
    font-weight: 700;
    color: #E8F4FF;
    letter-spacing: 1px;
    white-space: nowrap;
  }

  .logo-subtitle {
    font-family: 'Noto Sans SC', sans-serif;
    font-size: 10px;
    color: rgba(0, 212, 255, 0.6);
    white-space: nowrap;
    margin-top: 2px;
  }

  .collapse-btn {
    position: absolute;
    right: -12px;
    top: 50%;
    transform: translateY(-50%);
    width: 24px;
    height: 24px;
    background: vars.$layer-2;
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    color: rgba(0, 212, 255, 0.7);
    transition: all 0.2s;
    z-index: 20;

    svg {
      width: 12px;
      height: 12px;
    }

    &:hover {
      background: rgba(0, 212, 255, 0.15);
      color: #00D4FF;
      border-color: #00D4FF;
    }
  }
}

// 导航区域
.sidebar-nav {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 12px 8px;

  &::-webkit-scrollbar {
    width: 3px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.2);
    border-radius: 2px;
  }
}

// 分组标签
.menu-section-label {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 10px;
  font-weight: 600;
  color: rgba(80, 187, 227, 0.5);
  letter-spacing: 1.5px;
  text-transform: uppercase;
  padding: 14px 10px 6px;
  white-space: nowrap;
  overflow: hidden;
}

// 菜单项
.menu-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 10px;
  border-radius: 8px;
  cursor: pointer;
  color: rgba(200, 220, 240, 0.7);
  text-decoration: none;
  transition: all 0.2s;
  position: relative;
  width: 100%;
  border: none;
  background: transparent;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13.5px;
  white-space: nowrap;
  overflow: hidden;
  margin-bottom: 2px;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    color: #E8F4FF;

    .menu-icon svg {
      stroke: #00D4FF;
    }
  }

  &.active, &.router-link-active {
    background: rgba(0, 102, 255, 0.2);
    color: #E8F4FF;
    border-left: 2px solid #00D4FF;
    padding-left: 8px;

    .menu-icon svg {
      stroke: #00D4FF;
    }
  }

  &.has-children {
    justify-content: flex-start;
  }

  .active-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #00D4FF;
    margin-left: auto;
    flex-shrink: 0;
    box-shadow: 0 0 6px #00D4FF;
  }
}

.menu-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;

  svg {
    width: 18px;
    height: 18px;
    stroke: rgba(80, 187, 227, 0.6);
    transition: stroke 0.2s;
  }
}

.menu-label {
  flex: 1;
  text-align: left;
  overflow: hidden;
  text-overflow: ellipsis;
}

.chevron {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
  margin-left: auto;
  transition: transform 0.25s;
  stroke: rgba(80, 187, 227, 0.5);

  &.rotated {
    transform: rotate(180deg);
  }
}

// 子菜单
.submenu {
  padding: 2px 0 2px 30px;
  overflow: hidden;
}

.submenu-item {
  display: block;
  width: 100%;
  padding: 7px 10px;
  border-radius: 6px;
  color: rgba(180, 210, 230, 0.6);
  text-decoration: none;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  transition: all 0.2s;
  margin-bottom: 1px;
  position: relative;
  /* Reset button defaults */
  background: transparent;
  border: none;
  outline: none;
  cursor: pointer;
  text-align: left;
  box-sizing: border-box;

  &::before {
    content: '';
    position: absolute;
    left: 0;
    top: 50%;
    transform: translateY(-50%);
    width: 4px;
    height: 4px;
    border-radius: 50%;
    background: rgba(80, 187, 227, 0.3);
    transition: background 0.2s;
  }

  &:hover {
    color: #E8F4FF;
    background: rgba(0, 212, 255, 0.06);

    &::before {
      background: #00D4FF;
    }
  }

  &.active, &.router-link-active {
    color: #50BBE3;

    &::before {
      background: #00D4FF;
      box-shadow: 0 0 4px #00D4FF;
    }
  }
}

// 底部用户信息
.sidebar-footer {
  border-top: 1px solid rgba(0, 212, 255, 0.08);
  padding: 12px 8px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px;
  border-radius: 8px;
  background: rgba(0, 212, 255, 0.05);
  border: 1px solid rgba(0, 212, 255, 0.1);

  &.collapsed {
    justify-content: center;
    padding: 8px 4px;
  }
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 13px;
  font-weight: 700;
  color: #fff;
  flex-shrink: 0;
}

.user-details {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

.user-name {
  display: block;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  color: #E8F4FF;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-email {
  display: block;
  font-size: 10.5px;
  color: rgba(80, 187, 227, 0.5);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.logout-btn {
  width: 28px;
  height: 28px;
  border: none;
  background: transparent;
  color: rgba(80, 187, 227, 0.5);
  cursor: pointer;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.2s;

  svg {
    width: 15px;
    height: 15px;
  }

  &:hover {
    background: rgba(201, 87, 69, 0.15);
    color: #C95745;
  }
}

// ── 主内容区域 ───────────────────────────────────────────────
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-width: 0;
}

// 顶部栏
.top-bar {
  height: 56px;
  background: vars.$layer-2;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  flex-shrink: 0;
}

.top-bar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 16px;
  font-weight: 600;
  color: #E8F4FF;
  margin: 0;
}

.top-bar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.top-btn {
  width: 36px;
  height: 36px;
  border: 1px solid rgba(0, 212, 255, 0.15);
  background: rgba(0, 212, 255, 0.05);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: rgba(80, 187, 227, 0.7);
  position: relative;
  transition: all 0.2s;

  svg {
    width: 18px;
    height: 18px;
  }

  &:hover {
    background: rgba(0, 212, 255, 0.12);
    color: #00D4FF;
    border-color: rgba(0, 212, 255, 0.4);
  }

  .badge {
    position: absolute;
    top: -4px;
    right: -4px;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #C95745;
    font-size: 9px;
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: 'Space Mono', monospace;
  }
}

.top-user {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 10px 4px 4px;
  border-radius: 8px;
  border: 1px solid rgba(0, 212, 255, 0.12);
  background: rgba(0, 212, 255, 0.05);
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(0, 212, 255, 0.1);
  }
}

.top-user-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 11px;
  font-weight: 700;
  color: #fff;
}

.top-user-name {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  color: #E8F4FF;
}

// 内容区域
.content-wrapper {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  background: vars.$layer-1;

  &::-webkit-scrollbar {
    width: 4px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.2);
    border-radius: 2px;
  }
}

// 过渡动画
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s, transform 0.2s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateX(-8px);
}

// ===== 用户下拉菜单 =====
.user-dropdown {
  position: relative;
}

.dropdown-chevron {
  width: 14px;
  height: 14px;
  color: rgba(80, 187, 227, 0.6);
  transition: transform 0.2s;
  margin-left: 2px;

  &.open {
    transform: rotate(180deg);
  }
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 220px;
  background: #162233;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 10px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4), 0 0 0 1px rgba(0, 212, 255, 0.05);
  z-index: 1000;
  overflow: hidden;
  padding: 6px;
}

.dropdown-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 8px;
}

.dropdown-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 13px;
  font-weight: 700;
  color: #fff;
  flex-shrink: 0;
}

.dropdown-user-info {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.dropdown-username {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  color: #E8F4FF;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dropdown-email {
  font-size: 11px;
  color: rgba(80, 187, 227, 0.5);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 2px;
}

.dropdown-divider {
  height: 1px;
  background: rgba(0, 212, 255, 0.08);
  margin: 4px 0;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 10px;
  border-radius: 6px;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  color: rgba(180, 210, 230, 0.85);
  background: transparent;
  border: none;
  cursor: pointer;
  text-decoration: none;
  transition: all 0.15s;

  svg {
    width: 15px;
    height: 15px;
    flex-shrink: 0;
  }

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    color: #E8F4FF;
  }

  &.danger {
    color: rgba(201, 87, 69, 0.8);
    &:hover {
      background: rgba(201, 87, 69, 0.1);
      color: #C95745;
    }
  }
}

// 下拉动画
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s, transform 0.15s;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

// ===== Tab Bar =====
.tab-bar {
  display: flex;
  align-items: center;
  height: 40px;
  background: #0A1628;
  border-bottom: 1px solid rgba(0, 212, 255, 0.1);
  padding: 0 8px;
  gap: 2px;
  overflow-x: auto;
  flex-shrink: 0;

  &::-webkit-scrollbar {
    height: 2px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.2);
    border-radius: 1px;
  }
}

.tab-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 10px;
  height: 30px;
  border-radius: 6px 6px 0 0;
  cursor: pointer;
  user-select: none;
  flex-shrink: 0;
  border: 1px solid transparent;
  border-bottom: none;
  background: rgba(255, 255, 255, 0.03);
  transition: all 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
    border-color: rgba(0, 212, 255, 0.1);
  }

  &.active {
    background: linear-gradient(180deg, rgba(0, 212, 255, 0.12) 0%, rgba(0, 212, 255, 0.05) 100%);
    border-color: rgba(0, 212, 255, 0.25);
    transform: translateY(-1px);
    box-shadow: 0 0 14px rgba(0, 212, 255, 0.15);

    .tab-label {
      color: #00D4FF;
      font-weight: 600;
      text-shadow: 0 0 8px rgba(0, 212, 255, 0.4);
    }
  }
}

.tab-label {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12px;
  color: rgba(180, 210, 230, 0.6);
  white-space: nowrap;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tab-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  border-radius: 3px;
  background: transparent;
  border: none;
  cursor: pointer;
  color: rgba(80, 187, 227, 0.4);
  padding: 0;
  flex-shrink: 0;
  transition: all 0.15s;

  svg {
    width: 10px;
    height: 10px;
  }

  &:hover {
    background: rgba(201, 87, 69, 0.2);
    color: #C95745;
  }
}

.tab-active-bar {
  position: absolute;
  bottom: -1px;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, #00D4FF, transparent);
  box-shadow: 0 0 8px rgba(0, 212, 255, 0.6);
  border-radius: 1px;
}

// ===== 右键菜单 =====
.context-menu {
  position: fixed;
  z-index: 9999;
  background: #162233;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
  padding: 4px;
  min-width: 140px;
}

.context-menu-title {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 11px;
  color: rgba(80, 187, 227, 0.5);
  padding: 6px 10px 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 160px;
}

.context-menu-divider {
  height: 1px;
  background: rgba(0, 212, 255, 0.08);
  margin: 3px 0;
}

.context-menu-item {
  display: block;
  width: 100%;
  padding: 7px 10px;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  color: rgba(180, 210, 230, 0.85);
  background: transparent;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  text-align: left;
  transition: all 0.15s;

  &:hover:not(:disabled) {
    background: rgba(0, 212, 255, 0.08);
    color: #E8F4FF;
  }

  &:disabled {
    opacity: 0.35;
    cursor: not-allowed;
  }
}
</style>
