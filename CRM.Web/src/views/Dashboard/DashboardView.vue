<template>
  <!-- 控制台内容区域（外层布局由 AppLayout.vue 提供，此处不含侧边菜单和顶部栏） -->
  <div class="dashboard-content">
    <div class="dashboard-main">
    <!-- 欢迎卡片（置顶） -->
    <div class="welcome-card">
      <h2 class="welcome-title">
        {{ t('dashboard.welcomeBack', { name: authStore.user?.userName || t('dashboard.fallbackName') }) }}
      </h2>
      <div class="quick-links">
          <router-link v-if="isSalesSide && canCreateCustomer" to="/customers/create" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            {{ t('dashboard.quickNewCustomer') }}
          </router-link>
          <router-link v-if="isSalesSide && canCreateRfq" to="/rfqs/create" class="quick-link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            {{ t('dashboard.quickNewRfq') }}
          </router-link>
          <router-link v-if="isPurchaseSide && canCreateVendor" to="/vendors/create" class="quick-link">
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

    <IncentiveTargetPanel />
    <DashboardOverviewPanel />
    <RiskAlertPanel />
    </div>
    <aside class="dashboard-aside">
      <WorkCalendarPanel />
      <section v-if="showTodoPanel" class="todo-panel" aria-labelledby="dashboard-todo-title">
        <h3 id="dashboard-todo-title" class="todo-panel__title">{{ t('dashboard.todo.title') }}</h3>
        <ul class="todo-panel__list">
          <li v-if="showSetupMailboxTask" class="todo-row">
            <div class="todo-row__body">
              <h4 class="todo-row__title">{{ t('dashboard.todo.setupMailboxTitle') }}</h4>
              <p class="todo-row__desc">{{ t('dashboard.todo.setupMailboxDesc') }}</p>
            </div>
            <router-link class="todo-row__action" :to="mailboxSetupTo">
              {{ t('dashboard.todo.setupMailboxAction') }}
            </router-link>
          </li>
          <li v-if="showFeedbackTask" class="todo-row">
            <div class="todo-row__body">
              <h4 class="todo-row__title">
                {{ t('dashboard.todo.feedbackTitle') }}
                <span class="todo-row__count">{{ pendingFeedbackCount }}</span>
              </h4>
              <p class="todo-row__desc">
                {{ t('dashboard.todo.feedbackDesc', { count: pendingFeedbackCount }) }}
              </p>
            </div>
            <router-link class="todo-row__action" :to="feedbackTodoTo">
              {{ t('dashboard.todo.feedbackAction') }}
            </router-link>
          </li>
        </ul>
      </section>
      <DashboardNoticePanel />
    </aside>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onActivated } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores'
import { fetchMyMailSummary } from '@/api/myMails'
import { feedbackApi } from '@/api/feedback'
import { profileMailboxLocation } from '@/utils/profileMailboxLink'
import WorkCalendarPanel from '@/components/Dashboard/WorkCalendarPanel.vue'
import DashboardOverviewPanel from '@/components/Dashboard/DashboardOverviewPanel.vue'
import IncentiveTargetPanel from '@/components/Dashboard/IncentiveTargetPanel.vue'
import DashboardNoticePanel from '@/components/Dashboard/DashboardNoticePanel.vue'
import RiskAlertPanel from '@/components/Dashboard/RiskAlertPanel.vue'

const { t } = useI18n()
const authStore = useAuthStore()

const identityType = computed(() => authStore.user?.identityType ?? 0)
/** 销售 / 商务 */
const isSalesSide = computed(() => identityType.value === 1 || identityType.value === 4)
/** 采购员 / 采购助理 */
const isPurchaseSide = computed(() => identityType.value === 2 || identityType.value === 3)

const canCreateCustomer = computed(
  () =>
    authStore.hasPermission('customer.write') &&
    !authStore.isIdentityBlockedForPermission('customer.write')
)

const canCreateRfq = computed(
  () =>
    authStore.hasPermission('rfq.create') &&
    !authStore.isIdentityBlockedForPermission('rfq.create')
)

const canCreateVendor = computed(
  () =>
    authStore.hasPermission('vendor.write') &&
    !authStore.isIdentityBlockedForPermission('vendor.write')
)

/** null=未返回；失败当未验证，仍显示引导 */
const hasVerifiedMailbox = ref<boolean | null>(null)
const showSetupMailboxTask = computed(() => hasVerifiedMailbox.value === false)
/** null=未返回；仅系统管理员拉取 */
const pendingFeedbackCount = ref<number | null>(null)
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const showFeedbackTask = computed(
  () => isSysAdmin.value && (pendingFeedbackCount.value ?? 0) > 0
)
const showTodoPanel = computed(() => showSetupMailboxTask.value || showFeedbackTask.value)
const mailboxSetupTo = profileMailboxLocation('/dashboard')
const feedbackTodoTo = { path: '/ops/user-feedback', query: { handling: 'need' } }

async function loadMailboxTodo() {
  try {
    const s = await fetchMyMailSummary()
    hasVerifiedMailbox.value = !!s.hasVerifiedMailbox
  } catch {
    hasVerifiedMailbox.value = false
  }
}

async function loadFeedbackTodo() {
  if (!isSysAdmin.value) {
    pendingFeedbackCount.value = 0
    return
  }
  try {
    const res = await feedbackApi.adminList({ needsHandling: true, page: 1, pageSize: 1 })
    pendingFeedbackCount.value = Number(res?.total ?? 0)
  } catch {
    pendingFeedbackCount.value = 0
  }
}

onMounted(() => {
  void loadMailboxTodo()
  void loadFeedbackTodo()
})

onActivated(() => {
  void loadMailboxTodo()
  void loadFeedbackTodo()
})
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;

.dashboard-content {
  padding: 24px;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: 24px;
  align-items: start;

  @media (max-width: 1100px) {
    grid-template-columns: 1fr;
  }
}

.dashboard-main {
  display: flex;
  flex-direction: column;
  gap: 24px;
  min-width: 0;
}

.dashboard-aside {
  display: flex;
  flex-direction: column;
  gap: 16px;
  position: sticky;
  top: 16px;
}

.welcome-card {
  background: vars.$layer-2;
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 12px;
  padding: 16px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}

.welcome-title {
  margin: 0;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 20px;
  font-weight: 600;
  color: vars.$text-primary;
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

.todo-panel {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  padding: 16px;
}

.todo-panel__title {
  margin: 0 0 8px;
  font-size: 15px;
  font-weight: 600;
  color: vars.$text-primary;
}

.todo-panel__list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.todo-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
  padding: 10px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);

  &:last-child {
    padding-bottom: 0;
    border-bottom: none;
  }
}

.todo-row__body {
  min-width: 0;
}

.todo-row__title {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 0 0 4px;
  font-size: 13px;
  font-weight: 600;
  color: vars.$text-primary;
}

.todo-row__count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 18px;
  height: 18px;
  padding: 0 5px;
  border-radius: 9px;
  font-size: 11px;
  font-weight: 600;
  line-height: 1;
  color: vars.$text-primary;
  background: var(--el-fill-color);
}

.todo-row__desc {
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
  color: vars.$text-muted;
}

.todo-row__action {
  flex-shrink: 0;
  margin-top: 1px;
  color: var(--el-color-primary);
  text-decoration: none;
  font-size: 12px;
  white-space: nowrap;

  &:hover {
    text-decoration: underline;
  }
}
</style>
