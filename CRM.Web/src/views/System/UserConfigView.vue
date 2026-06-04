<template>
  <div class="user-config-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ t('userConfig.pageTitle') }}</h2>
        <p class="page-sub">{{ t('userConfig.pageSubtitle') }}</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav" :aria-label="t('userConfig.navAria')">
        <div
          v-for="item in navItems"
          :key="item.key"
          class="nav-item"
          :class="{ active: activeNav === item.key }"
          @click="activeNav = item.key"
        >
          <el-icon class="nav-icon"><component :is="item.icon" /></el-icon>
          <span>{{ item.label }}</span>
        </div>
      </div>

      <div class="settings-content" v-loading="loading">
        <div v-show="activeNav === 'sales'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title">
                <span class="title-bar"></span>{{ t('userConfig.sales.sectionTitle') }}
              </div>
              <p class="section-hint">{{ t('userConfig.sales.sectionHint') }}</p>
            </div>
            <el-button :loading="loading" @click="load">{{ t('userConfig.refreshBtn') }}</el-button>
          </div>
          <AssistantRelationPanel
            v-if="!loading"
            ref="salesPanelRef"
            :relation-type="SALES_ASSISTANT_TO_SALESPERSON"
            :assistant-title="t('userConfig.sales.assistantListTitle')"
            :target-title="t('userConfig.sales.targetListTitle')"
            :assistants="salesAssistants"
            :targets="salesTargets"
            :assistant-dept-ids="businessDeptIds"
            :target-dept-ids="salesDeptIds"
            :dept-name-by-id="deptNameById"
          />
        </div>

        <div v-show="activeNav === 'purchase'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title">
                <span class="title-bar"></span>{{ t('userConfig.purchase.sectionTitle') }}
              </div>
              <p class="section-hint">{{ t('userConfig.purchase.sectionHint') }}</p>
            </div>
            <el-button :loading="loading" @click="load">{{ t('userConfig.refreshBtn') }}</el-button>
          </div>
          <AssistantRelationPanel
            v-if="!loading"
            ref="purchasePanelRef"
            :relation-type="PURCHASE_ASSISTANT_TO_PURCHASER"
            :assistant-title="t('userConfig.purchase.assistantListTitle')"
            :target-title="t('userConfig.purchase.targetListTitle')"
            :assistants="purchaseAssistants"
            :targets="purchaseTargets"
            :assistant-dept-ids="purchaseOpsDeptIds"
            :target-dept-ids="purchaseDeptIds"
            :dept-name-by-id="deptNameById"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ShoppingCart, User } from '@element-plus/icons-vue'
import { rbacAdminApi, type AdminUserDto, type RbacDepartment } from '@/api/rbacAdmin'
import AssistantRelationPanel from '@/components/System/AssistantRelationPanel.vue'
import {
  isBusinessDepartment,
  isPurchaseDepartment,
  isPurchasingOperationsDepartment,
  isSalesDepartment
} from '@/utils/departmentAssistantRules'
import {
  PURCHASE_ASSISTANT_TO_PURCHASER,
  SALES_ASSISTANT_TO_SALESPERSON
} from '@/constants/sysRelationMapType'

const { t } = useI18n()

type NavKey = 'sales' | 'purchase'

const activeNav = ref<NavKey>('sales')
const loading = ref(false)
const allUsers = ref<AdminUserDto[]>([])
const departments = ref<RbacDepartment[]>([])

const salesPanelRef = ref<InstanceType<typeof AssistantRelationPanel> | null>(null)
const purchasePanelRef = ref<InstanceType<typeof AssistantRelationPanel> | null>(null)

const navItems = computed(() => [
  { key: 'sales' as const, label: t('userConfig.navSalesAssistant'), icon: User },
  { key: 'purchase' as const, label: t('userConfig.navPurchaseAssistant'), icon: ShoppingCart }
])

const deptNameById = computed(() => {
  const m = new Map<string, string>()
  for (const d of departments.value) {
    m.set(d.id, d.departmentName)
  }
  return m
})

const businessDeptIds = computed(
  () => new Set(departments.value.filter(isBusinessDepartment).map((d) => d.id))
)
const salesDeptIds = computed(
  () => new Set(departments.value.filter(isSalesDepartment).map((d) => d.id))
)
const purchaseOpsDeptIds = computed(
  () => new Set(departments.value.filter(isPurchasingOperationsDepartment).map((d) => d.id))
)
const purchaseDeptIds = computed(
  () => new Set(departments.value.filter(isPurchaseDepartment).map((d) => d.id))
)

function usersInDepts(users: AdminUserDto[], deptIds: Set<string>): AdminUserDto[] {
  if (deptIds.size === 0) return []
  return users.filter((u) => (u.departmentIds ?? []).some((id) => deptIds.has(id)))
}

const salesAssistants = computed(() => usersInDepts(allUsers.value, businessDeptIds.value))
const salesTargets = computed(() => usersInDepts(allUsers.value, salesDeptIds.value))
const purchaseAssistants = computed(() => usersInDepts(allUsers.value, purchaseOpsDeptIds.value))
const purchaseTargets = computed(() => usersInDepts(allUsers.value, purchaseDeptIds.value))

async function load() {
  loading.value = true
  try {
    const [users, depts] = await Promise.all([rbacAdminApi.getUsers(), rbacAdminApi.getDepartments()])
    allUsers.value = users
    departments.value = depts
    salesPanelRef.value?.resetSelection()
    purchasePanelRef.value?.resetSelection()
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || t('userConfig.loadFailed'))
    allUsers.value = []
    departments.value = []
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.user-config-page {
  padding: 20px;
  min-height: 100%;
}

.page-header {
  margin-bottom: 20px;
  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0 0 6px;
  }
  .page-sub {
    margin: 0;
    font-size: 13px;
    color: $text-muted;
    line-height: 1.5;
  }
}

.settings-body {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

.settings-nav {
  width: 200px;
  flex-shrink: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 8px;

  .nav-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 6px;
    cursor: pointer;
    color: $text-muted;
    font-size: 13px;
    transition: all 0.2s;

    .nav-icon {
      font-size: 16px;
    }

    &:hover {
      background: rgba(0, 212, 255, 0.06);
      color: $text-secondary;
    }

    &.active {
      background: rgba(0, 212, 255, 0.18);
      color: $cyan-primary;
      font-weight: 500;
    }
  }
}

.settings-content {
  flex: 1;
  min-width: 0;
}

.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 20px 24px;
}

.section-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.section-head__left {
  flex: 1;
  min-width: 0;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 6px;

  .title-bar {
    width: 3px;
    height: 14px;
    background: $cyan-primary;
    border-radius: 2px;
  }
}

.section-hint {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
}
</style>
