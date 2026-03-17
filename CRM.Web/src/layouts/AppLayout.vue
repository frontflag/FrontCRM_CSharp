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

        <!-- 业务管理 -->
        <div class="menu-section-label" v-if="!isCollapsed">业务管理</div>

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
            <button class="submenu-item" @click="handleUnimplemented('采购订单')">采购订单</button>
            <button class="submenu-item" @click="handleUnimplemented('收货管理')">收货管理</button>
            <button class="submenu-item" @click="handleUnimplemented('采购退货')">采购退货</button>
          </div>
        </div>

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
            <button class="submenu-item" @click="handleUnimplemented('销售订单')">销售订单</button>
            <button class="submenu-item" @click="handleUnimplemented('发货管理')">发货管理</button>
            <button class="submenu-item" @click="handleUnimplemented('销售退货')">销售退货</button>
          </div>
        </div>

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
            <button class="submenu-item" @click="handleUnimplemented('库存列表')">库存列表</button>
            <button class="submenu-item" @click="handleUnimplemented('库存调拨')">库存调拨</button>
            <button class="submenu-item" @click="handleUnimplemented('库存盘点')">库存盘点</button>
          </div>
        </div>

        <!-- 智能采购 -->
        <div class="menu-section-label" v-if="!isCollapsed">智能采购</div>

        <router-link to="/rfqs" class="menu-item" active-class="active">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <circle cx="12" cy="12" r="10"/>
              <path d="M12 8v4l3 3"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">RFQ 管理</span>
          <span class="active-dot" v-if="!isCollapsed"></span>
        </router-link>

        <button class="menu-item" @click="handleUnimplemented('需求匹配报价')">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
              <polyline points="14 2 14 8 20 8"/>
              <line x1="16" y1="13" x2="8" y2="13"/>
              <line x1="16" y1="17" x2="8" y2="17"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">需求匹配报价</span>
        </button>

        <button class="menu-item" @click="handleUnimplemented('BOM 批量报价')">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <rect x="3" y="3" width="18" height="18" rx="2"/>
              <path d="M3 9h18M9 21V9"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">BOM 批量报价</span>
        </button>

        <!-- 基础资料 -->
        <div class="menu-section-label" v-if="!isCollapsed">基础资料</div>

        <button class="menu-item" @click="handleUnimplemented('供应商管理')">
          <span class="menu-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <path d="M23 21v-2a4 4 0 00-3-3.87"/>
              <path d="M16 3.13a4 4 0 010 7.75"/>
            </svg>
          </span>
          <span class="menu-label" v-if="!isCollapsed">供应商管理</span>
        </button>

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

      <!-- 底部用户信息 -->
      <div class="sidebar-footer">
        <div class="user-info" :class="{ collapsed: isCollapsed }">
          <div class="user-avatar">{{ userInitial }}</div>
          <transition name="fade">
            <div class="user-details" v-if="!isCollapsed">
              <span class="user-name">{{ userName }}</span>
              <span class="user-email">{{ userEmail }}</span>
            </div>
          </transition>
          <button class="logout-btn" @click="handleLogout" title="退出登录" v-if="!isCollapsed">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/>
              <polyline points="16 17 21 12 16 7"/>
              <line x1="21" y1="12" x2="9" y2="12"/>
            </svg>
          </button>
        </div>
      </div>
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
          <div class="top-user">
            <div class="top-user-avatar">{{ userInitial }}</div>
            <span class="top-user-name" v-if="!isCollapsed">{{ userName }}</span>
          </div>
        </div>
      </header>

      <!-- 页面内容 -->
      <div class="content-wrapper">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
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
  customers: false
})

const toggleCollapse = () => {
  isCollapsed.value = !isCollapsed.value
  if (isCollapsed.value) {
    openGroups.value = { purchase: false, sales: false, inventory: false, customers: false }
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
  '/suppliers': '供应商管理',
  '/reports': '报表分析',
  '/dashboard/settings': '系统设置',
  '/rfqs': 'RFQ 管理',
  '/smart/quote-match': '需求匹配报价',
  '/smart/bom-quote': 'BOM 批量报价',
}

const currentPageTitle = computed(() => {
  const path = route.path
  if (pageTitleMap[path]) return pageTitleMap[path]
  if (path.includes('/customers/') && path.includes('/edit')) return '编辑客户'
  if (path.includes('/customers/')) return '客户详情'
  return route.meta?.title as string || 'FrontCRM'
})

const handleLogout = async () => {
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
  padding: 7px 10px;
  border-radius: 6px;
  color: rgba(180, 210, 230, 0.6);
  text-decoration: none;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  transition: all 0.2s;
  margin-bottom: 1px;
  position: relative;

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
</style>
