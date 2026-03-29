<template>
  <!-- 控制台内容区域（外层布局由 AppLayout.vue 提供，此处不含侧边菜单和顶部栏） -->
  <div class="dashboard-content">
    <!-- 统计卡片 -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon cyan">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
            <circle cx="12" cy="7" r="4"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">总客户数</span>
          <span class="stat-value">{{ stats.totalCustomers }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon amber">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <circle cx="12" cy="12" r="10"/>
            <path d="M12 8v4l3 3"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">待处理事项</span>
          <span class="stat-value">{{ stats.pendingTasks }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon mint">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">本月新增</span>
          <span class="stat-value">{{ stats.monthlyNew }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon blue">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <line x1="18" y1="20" x2="18" y2="10"/>
            <line x1="12" y1="20" x2="12" y2="4"/>
            <line x1="6" y1="20" x2="6" y2="14"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">本月销售额</span>
          <span class="stat-value">¥ {{ stats.monthlySales }}</span>
        </div>
      </div>
    </div>

    <!-- 欢迎卡片 -->
    <div class="welcome-card">
      <div class="welcome-left">
        <h2 class="welcome-title">欢迎回来，{{ authStore.user?.userName }}！</h2>
        <p class="welcome-sub">这是 FrontCRM 智能进销存管理系统的控制台，您可以从左侧菜单进入各功能模块。</p>
      </div>
      <div class="welcome-right">
        <div class="quick-links">
          <router-link to="/custome" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
              <circle cx="12" cy="7" r="4"/>
            </svg>
            客户管理
          </router-link>
          <router-link to="/customers/create" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <line x1="12" y1="5" x2="12" y2="19"/>
              <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            新增客户
          </router-link>
          <router-link to="/dashboard/settings" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <circle cx="12" cy="12" r="3"/>
              <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/>
            </svg>
            系统设置
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import { useAuthStore } from '@/stores'

const authStore = useAuthStore()

const stats = reactive({
  totalCustomers: 0,
  pendingTasks: 0,
  monthlyNew: 0,
  monthlySales: '0.00'
})
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;

.dashboard-content {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;

  @media (max-width: 1200px) { grid-template-columns: repeat(2, 1fr); }
  @media (max-width: 640px)  { grid-template-columns: 1fr; }
}

.stat-card {
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: border-color 0.2s, transform 0.2s;
  &:hover { border-color: rgba(0, 212, 255, 0.3); transform: translateY(-2px); }
}

.stat-icon {
  width: 48px; height: 48px;
  border-radius: 10px;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
  svg { width: 22px; height: 22px; }
  &.cyan  { background: rgba(0, 212, 255, 0.12); svg { stroke: #00D4FF; } }
  &.amber { background: rgba(201, 154, 69, 0.12); svg { stroke: #C99A45; } }
  &.mint  { background: rgba(70, 191, 145, 0.12); svg { stroke: #46BF91; } }
  &.blue  { background: rgba(50, 149, 201, 0.12); svg { stroke: #3295C9; } }
}

.stat-info { display: flex; flex-direction: column; gap: 4px; }
.stat-label { font-family: 'Noto Sans SC', sans-serif; font-size: 12px; color: rgba(200, 220, 240, 0.6); }
.stat-value { font-family: 'Space Mono', monospace; font-size: 22px; font-weight: 700; color: #E8F4FF; }

.welcome-card {
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 12px;
  padding: 28px 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  @media (max-width: 768px) { flex-direction: column; align-items: flex-start; }
}

.welcome-title {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 20px; font-weight: 600; color: #E8F4FF; margin: 0 0 8px;
}
.welcome-sub {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px; color: rgba(200, 220, 240, 0.6); margin: 0; line-height: 1.6; max-width: 480px;
}

.quick-links { display: flex; gap: 10px; flex-wrap: wrap; }

.quick-link {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 16px; border-radius: 8px;
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.06);
  color: rgba(80, 187, 227, 0.9);
  text-decoration: none;
  font-family: 'Noto Sans SC', sans-serif; font-size: 13px;
  transition: all 0.2s; white-space: nowrap;
  svg { width: 15px; height: 15px; stroke: currentColor; }
  &:hover {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.5);
    color: #00D4FF; transform: translateY(-1px);
  }
}
</style>
