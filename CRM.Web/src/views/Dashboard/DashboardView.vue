<template>
  <!-- 控制台内容区域（外层布局由 AppLayout.vue 提供，此处不含侧边菜单和顶部栏） -->
  <div class="dashboard-content">
    <!-- 采购主部门：供应商统计 -->
    <div v-if="showPurchaseStats" class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon cyan">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
            <path d="M9 22V12h6v10" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.totalVendors') }}</span>
          <span class="stat-value">{{ stats.totalVendors }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon amber">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <circle cx="12" cy="12" r="10" />
            <path d="M12 8v4l3 3" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.pendingTasks') }}</span>
          <span class="stat-value">{{ stats.pendingTasks }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon mint">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.monthlyNew') }}</span>
          <span class="stat-value">{{ stats.monthlyNew }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon blue">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <line x1="18" y1="20" x2="18" y2="10" />
            <line x1="12" y1="20" x2="12" y2="4" />
            <line x1="6" y1="20" x2="6" y2="14" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.vendorPayableAmount') }}</span>
          <span class="stat-value">¥ {{ stats.vendorPayable }}</span>
        </div>
      </div>
    </div>

    <!-- 非采购主部门且具备客户数据权限：客户 + 销售口径统计 -->
    <div v-else-if="showCustomerStats" class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon cyan">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
            <circle cx="12" cy="7" r="4" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.totalCustomers') }}</span>
          <span class="stat-value">{{ stats.totalCustomers }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon amber">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <circle cx="12" cy="12" r="10" />
            <path d="M12 8v4l3 3" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.pendingTasks') }}</span>
          <span class="stat-value">{{ stats.pendingTasks }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon mint">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.monthlyNew') }}</span>
          <span class="stat-value">{{ stats.monthlyNew }}</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon blue">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <line x1="18" y1="20" x2="18" y2="10" />
            <line x1="12" y1="20" x2="12" y2="4" />
            <line x1="6" y1="20" x2="6" y2="14" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.monthlySales') }}</span>
          <span class="stat-value">¥ {{ stats.monthlySales }}</span>
        </div>
      </div>
    </div>

    <!-- 无客户/供应商统计权限：仅待办 -->
    <div v-else class="stats-grid stats-grid--single">
      <div class="stat-card">
        <div class="stat-icon amber">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <circle cx="12" cy="12" r="10" />
            <path d="M12 8v4l3 3" />
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-label">{{ t('dashboard.stats.pendingTasks') }}</span>
          <span class="stat-value">{{ stats.pendingTasks }}</span>
        </div>
      </div>
    </div>

    <!-- 欢迎卡片 -->
    <div class="welcome-card">
      <div class="welcome-left">
        <h2 class="welcome-title">
          {{ t('dashboard.welcomeBack', { name: authStore.user?.userName || t('dashboard.fallbackName') }) }}
        </h2>
        <p class="welcome-sub">
          {{ isPurchasePrimary ? t('dashboard.welcomeSubPurchase') : t('dashboard.welcomeSub') }}
        </p>
      </div>
      <div class="welcome-right">
        <div class="quick-links">
          <router-link v-if="canUseCustomerUi" to="/custome" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            {{ t('dashboard.quickCustomerManagement') }}
          </router-link>
          <router-link v-if="canCreateCustomer" to="/customers/create" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            {{ t('dashboard.quickNewCustomer') }}
          </router-link>
          <router-link v-if="canUseVendorUi" to="/vendorlist" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
              <path d="M9 22V12h6v10" />
            </svg>
            {{ t('dashboard.quickVendorManagement') }}
          </router-link>
          <router-link v-if="canCreateVendor" to="/vendors/create" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            {{ t('dashboard.quickNewVendor') }}
          </router-link>
          <router-link to="/dashboard/settings" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <circle cx="12" cy="12" r="3" />
              <path
                d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"
              />
            </svg>
            {{ t('dashboard.quickSystemSettings') }}
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores'
import { customerApi } from '@/api/customer'
import { vendorApi } from '@/api/vendor'
import { approvalsApi } from '@/api/approvals'

const { t } = useI18n()
const authStore = useAuthStore()

/** 主部门为采购 / 采购助理（与 auth 中 identityType 约定一致） */
const isPurchasePrimary = computed(() => {
  const t0 = authStore.user?.identityType ?? 0
  return t0 === 2 || t0 === 3
})

const canUseCustomerUi = computed(
  () =>
    authStore.hasPermission('customer.read') &&
    !authStore.isIdentityBlockedForPermission('customer.read')
)

const canCreateCustomer = computed(
  () =>
    authStore.hasPermission('customer.write') &&
    !authStore.isIdentityBlockedForPermission('customer.write')
)

const canUseVendorUi = computed(
  () =>
    authStore.hasPermission('vendor.read') &&
    !authStore.isIdentityBlockedForPermission('vendor.read')
)

const canCreateVendor = computed(
  () =>
    authStore.hasPermission('vendor.write') &&
    !authStore.isIdentityBlockedForPermission('vendor.write')
)

/** 采购工作台统计块：主部门为采购侧且具备供应商读权限 */
const showPurchaseStats = computed(() => isPurchasePrimary.value && canUseVendorUi.value)

/** 客户工作台统计块：非采购主部门且具备客户读权限 */
const showCustomerStats = computed(() => !isPurchasePrimary.value && canUseCustomerUi.value)

const stats = reactive({
  totalCustomers: 0,
  totalVendors: 0,
  pendingTasks: 0,
  monthlyNew: 0,
  monthlySales: '0.00',
  vendorPayable: '0.00'
})

function formatMoneyAmount(n: number) {
  if (!Number.isFinite(n)) return '0.00'
  return n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function loadDashboardStats() {
  try {
    const summary = await approvalsApi.getApprovalSummary({})
    stats.pendingTasks = summary?.pendingCount ?? 0
  } catch {
    stats.pendingTasks = 0
  }

  if (showPurchaseStats.value) {
    try {
      const v = await vendorApi.getVendorStatistics()
      stats.totalVendors = v.totalVendors ?? 0
      stats.monthlyNew = v.newThisMonth ?? 0
      stats.vendorPayable = formatMoneyAmount(Number(v.payableAmount ?? 0))
    } catch {
      stats.totalVendors = 0
      stats.monthlyNew = 0
      stats.vendorPayable = '0.00'
    }
    return
  }

  if (showCustomerStats.value) {
    try {
      const c = await customerApi.getCustomerStatistics()
      stats.totalCustomers = c.totalCustomers ?? 0
      stats.monthlyNew = c.newThisMonth ?? 0
      stats.monthlySales = formatMoneyAmount(Number(c.receivableGoodsAmount ?? 0))
    } catch {
      stats.totalCustomers = 0
      stats.monthlyNew = 0
      stats.monthlySales = '0.00'
    }
  }
}

onMounted(() => {
  void loadDashboardStats()
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

  @media (max-width: 1200px) {
    grid-template-columns: repeat(2, 1fr);
  }
  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }

  &--single {
    grid-template-columns: 1fr;
    max-width: 360px;
  }
}

.stat-card {
  background: vars.$layer-3;
  border: 1px solid vars.$border-card;
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition:
    border-color 0.2s,
    transform 0.2s;
  &:hover {
    border-color: var(--crm-accent-03);
    transform: translateY(-2px);
  }
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  svg {
    width: 22px;
    height: 22px;
  }
  &.cyan {
    background: var(--crm-accent-008);
    svg {
      stroke: vars.$cyan-primary;
    }
  }
  &.amber {
    background: var(--crm-accent-008);
    svg {
      stroke: vars.$warning-color;
    }
  }
  &.mint {
    background: var(--crm-accent-008);
    svg {
      stroke: vars.$success-color;
    }
  }
  &.blue {
    background: var(--crm-accent-008);
    svg {
      stroke: vars.$info-color;
    }
  }
}

.stat-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.stat-label {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12px;
  color: vars.$text-muted;
}
.stat-value {
  font-family: 'Space Mono', monospace;
  font-size: 22px;
  font-weight: 700;
  color: vars.$text-primary;
}

.welcome-card {
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 12px;
  padding: 28px 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  @media (max-width: 768px) {
    flex-direction: column;
    align-items: flex-start;
  }
}

.welcome-title {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 20px;
  font-weight: 600;
  color: #e8f4ff;
  margin: 0 0 8px;
}
.welcome-sub {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  color: rgba(200, 220, 240, 0.6);
  margin: 0;
  line-height: 1.6;
  max-width: 480px;
}

.quick-links {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.quick-link {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border-radius: 8px;
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.06);
  color: rgba(80, 187, 227, 0.9);
  text-decoration: none;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  transition: all 0.2s;
  white-space: nowrap;
  svg {
    width: 15px;
    height: 15px;
    stroke: currentColor;
  }
  &:hover {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.5);
    color: #00d4ff;
    transform: translateY(-1px);
  }
}
</style>
