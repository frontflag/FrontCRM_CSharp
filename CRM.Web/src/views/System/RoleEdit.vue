<template>
  <div class="role-edit-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ isEdit ? t('systemRole.editTitle') : t('systemRole.createTitle') }}</h2>
        <p class="page-sub">{{ pageSubtitle }}</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav">
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

      <div class="settings-content">
        <div v-show="activeNav === 'settings'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>{{ t('systemRole.navSettings') }}</div>
              <p class="section-hint">{{ t('systemRole.settingsHint') }}</p>
            </div>
          </div>
      <el-form :model="formData" label-width="120px" :disabled="loading">
        <el-form-item :label="t('systemRole.columns.roleCode')">
          <el-input v-model="formData.roleCode" :disabled="isEdit" />
        </el-form-item>
        <el-form-item :label="t('systemRole.columns.roleName')">
          <el-input v-model="formData.roleName" />
        </el-form-item>
        <el-form-item :label="t('systemRole.columns.description')">
          <el-input v-model="formData.description" />
        </el-form-item>
        <el-form-item :label="t('systemUser.colStatus')">
          <el-select v-model="formData.status" style="width: 160px">
            <el-option :value="1" :label="t('systemUser.statusEnabled')" />
            <el-option :value="0" :label="t('systemUser.statusDisabled')" />
          </el-select>
        </el-form-item>

        <el-form-item :label="t('layout.menu.permissionManagement')">
          <div class="role-perm-picker">
            <div class="role-perm-picker__tabs">
              <el-radio-group v-model="permKindFilter" size="small">
                <el-radio-button value="all">{{ t('systemRole.permTabAll') }}</el-radio-button>
                <el-radio-button value="menu">{{ t('systemRole.permTabMenu') }}</el-radio-button>
                <el-radio-button value="sub">{{ t('systemRole.permTabSub') }}</el-radio-button>
              </el-radio-group>
            </div>
            <div class="role-perm-picker__toolbar">
              <el-input
                v-model="permFilter"
                clearable
                :placeholder="t('systemRole.permissionFilterPlaceholder')"
                class="role-perm-picker__filter"
              />
              <span class="role-perm-picker__count">
                {{ t('systemRole.permissionSelectedCount', { count: formData.permissionIds.length }) }}
              </span>
            </div>
            <div class="role-perm-picker__legend" :title="legendTitle">
              <span class="role-perm-picker__legend-label">{{ t('systemRole.permKindLegend') }}</span>
              <el-tag size="small" type="primary" effect="plain">{{ t('systemRole.permKindMenu') }}</el-tag>
              <span class="role-perm-picker__legend-hint">{{ t('systemRole.permKindMenuHint') }}</span>
              <el-tag size="small" type="warning" effect="plain">{{ t('systemRole.permKindSub') }}</el-tag>
              <span class="role-perm-picker__legend-hint">{{ t('systemRole.permKindSubHint') }}</span>
              <el-tag size="small" type="info" effect="plain">{{ t('systemRole.permKindFeature') }}</el-tag>
              <span class="role-perm-picker__legend-hint">{{ t('systemRole.permKindFeatureHint') }}</span>
            </div>
            <div v-if="permissionGroups.length === 0" class="role-perm-picker__empty">
              {{ t('systemRole.permissionFilterEmpty') }}
            </div>
            <div v-else class="role-perm-picker__list">
              <section v-for="group in permissionGroups" :key="group.key" class="role-perm-picker__section">
                <div class="role-perm-picker__group-title">{{ group.label }}</div>
                <el-checkbox-group v-model="formData.permissionIds" class="role-perm-picker__group">
                  <el-checkbox
                    v-for="p in group.items"
                    :key="p.id || p.permissionCode"
                    :value="p.id || `__missing__:${p.permissionCode}`"
                    :disabled="!p.id"
                    class="role-perm-picker__item"
                  >
                    <el-tag
                      size="small"
                      :type="permKindTagType(p.permissionCode)"
                      effect="plain"
                      class="role-perm-picker__kind"
                    >
                      {{ permKindLabel(p.permissionCode) }}
                    </el-tag>
                    <span class="role-perm-picker__code">{{ p.permissionCode }}</span>
                    <span class="role-perm-picker__name">{{ p.permissionName }}</span>
                    <span v-if="permMenuLabel(p.permissionCode)" class="role-perm-picker__menu-hint">
                      {{ t('systemRole.permMenuHintPrefix') }}{{ permMenuLabel(p.permissionCode) }}
                    </span>
                    <span v-if="!p.id" class="role-perm-picker__missing">
                      {{ t('systemRole.permMissingInDb') }}
                    </span>
                  </el-checkbox>
                </el-checkbox-group>
              </section>
            </div>
          </div>
        </el-form-item>

        <div class="footer-bar">
          <el-button @click="router.push({ name: 'RoleList' })">{{ t('rfqDetail.back') }}</el-button>
          <el-button type="primary" :loading="saving" @click="handleSubmit">
            {{ isEdit ? t('common.save') : t('systemRole.create') }}
          </el-button>
        </div>
      </el-form>
        </div>

        <div v-show="activeNav === 'users'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>{{ t('systemRole.navUsers') }}</div>
              <p class="section-hint">{{ isEdit ? t('systemRole.usersHint') : t('systemRole.usersCreateHint') }}</p>
            </div>
            <div v-if="isEdit" class="section-head__right">
              <span class="role-users-count">{{ t('systemRole.usersCount', { count: roleUsers.length }) }}</span>
              <el-button v-if="canWriteUsers" type="primary" @click="openAddUsersDialog">
                {{ t('systemRole.addUsers') }}
              </el-button>
            </div>
          </div>
      <el-table
        v-if="isEdit"
        v-loading="usersLoading"
        :data="roleUsers"
        row-key="id"
        class="role-users-table crm-data-table"
        @row-dblclick="onRoleUserDblclick"
      >
        <el-table-column prop="userName" :label="t('systemUser.colUserName')" min-width="140">
          <template #default="{ row }">
            <button
              v-if="canOpenUser"
              type="button"
              class="role-users-link"
              @click.stop="goEditUser(row.id)"
            >
              {{ row.userName }}
            </button>
            <span v-else>{{ row.userName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="realName" :label="t('systemUser.colRealName')" min-width="120">
          <template #default="{ row }">{{ row.realName || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('systemUser.colPrimaryDept')" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">{{ row.primaryDepartmentName || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('systemUser.colStatus')" width="88">
          <template #default="{ row }">{{ userStatusLabel(row.status) }}</template>
        </el-table-column>
        <el-table-column
          v-if="canWriteUsers"
          :label="t('systemRole.columns.actions')"
          :width="opColWidth"
          :min-width="opColMinWidth"
          fixed="right"
          align="center"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="list-op-col-header--icon-only">
              <button
                type="button"
                class="op-col-toggle-btn list-op-col-toggle"
                :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                @click.stop="toggleOpCol"
              >
                {{ opColExpanded ? '>' : '<' }}
              </button>
            </div>
          </template>
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="opColExpanded" class="action-btns">
                <button
                  type="button"
                  class="action-btn action-btn--primary"
                  @click.stop="openUserEdit(row.id)"
                >
                  {{ t('systemRole.editAccount') }}
                </button>
                <button
                  type="button"
                  class="action-btn action-btn--danger"
                  :disabled="removingUserId === row.id"
                  @click.stop="handleRemoveRole(row)"
                >
                  {{ t('systemRole.removeRole') }}
                </button>
              </div>
              <el-dropdown v-else trigger="click" placement="bottom-end">
                <div class="op-more-dropdown-trigger">
                  <button type="button" class="op-more-trigger">...</button>
                </div>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="openUserEdit(row.id)">
                      <span class="op-more-item op-more-item--primary">{{ t('systemRole.editAccount') }}</span>
                    </el-dropdown-item>
                    <el-dropdown-item :disabled="removingUserId === row.id" @click.stop="handleRemoveRole(row)">
                      <span class="op-more-item op-more-item--danger">{{ t('systemRole.removeRole') }}</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
        <template #empty>{{ t('systemRole.usersEmpty') }}</template>
      </el-table>
        </div>
      </div>
    </div>

    <el-dialog
      v-model="addUsersVisible"
      :title="t('systemRole.addUsers')"
      width="720px"
      destroy-on-close
      @closed="onAddUsersDialogClosed"
    >
      <p class="add-users-hint">{{ t('systemRole.addUsersHint') }}</p>
      <div class="add-users-search">
        <el-input
          v-model="addUsersKw"
          clearable
          :placeholder="t('systemRole.addUsersSearchPlaceholder')"
          @keyup.enter="applyAddUsersSearch"
        />
        <el-button type="primary" @click="applyAddUsersSearch">{{ t('systemUser.searchQuery') }}</el-button>
      </div>
      <el-table
        ref="addUsersTableRef"
        v-loading="addUsersLoading"
        :data="addUsersFiltered"
        row-key="id"
        max-height="420"
        class="role-users-table crm-data-table"
        @selection-change="onAddUsersSelectionChange"
      >
        <el-table-column type="selection" width="48" align="center" />
        <el-table-column prop="userName" :label="t('systemUser.colUserName')" min-width="140" />
        <el-table-column prop="realName" :label="t('systemUser.colRealName')" min-width="120">
          <template #default="{ row }">{{ row.realName || '—' }}</template>
        </el-table-column>
        <el-table-column :label="t('systemUser.colPrimaryDept')" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">{{ row.primaryDepartmentName || '—' }}</template>
        </el-table-column>
        <template #empty>
          {{ addUsersAppliedKw.trim() ? t('systemRole.addUsersSearchEmpty') : t('systemRole.addUsersEmpty') }}
        </template>
      </el-table>
      <template #footer>
        <el-button @click="addUsersVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="addUsersSaving" :disabled="addUsersSelected.length === 0" @click="confirmAddUsers">
          {{ t('systemRole.addUsersConfirm') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting, User } from '@element-plus/icons-vue'
import { rbacAdminApi, type AdminUserDto, type RbacPermission, type RbacRole } from '@/api/rbacAdmin'
import { useAuthStore } from '@/stores'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()

const roleId = route.params.id as string | undefined
const isEdit = !!roleId
const activeNav = ref<'settings' | 'users'>('settings')
const navItems = computed(() => [
  { key: 'settings' as const, label: t('systemRole.navSettings'), icon: Setting },
  { key: 'users' as const, label: t('systemRole.navUsers'), icon: User }
])
const pageSubtitle = computed(() => {
  if (!isEdit) return t('systemRole.createSubtitle')
  const code = formData.value.roleCode?.trim()
  const name = formData.value.roleName?.trim()
  if (code && name) return `${code} · ${name}`
  return t('systemRole.editSubtitle')
})

const loading = ref(false)
const saving = ref(false)
const usersLoading = ref(false)
const roleUsers = ref<AdminUserDto[]>([])
const removingUserId = ref<string | null>(null)
const addUsersVisible = ref(false)
const addUsersLoading = ref(false)
const addUsersSaving = ref(false)
const addUsersCandidates = ref<AdminUserDto[]>([])
const addUsersSelected = ref<AdminUserDto[]>([])
const addUsersKw = ref('')
const addUsersAppliedKw = ref('')
const addUsersTableRef = ref<{ clearSelection?: () => void } | null>(null)
const addUsersFiltered = computed(() => {
  const q = addUsersAppliedKw.value.trim().toLowerCase()
  if (!q) return addUsersCandidates.value
  return addUsersCandidates.value.filter((u) => {
    const name = (u.userName || '').toLowerCase()
    const real = (u.realName || '').toLowerCase()
    return name.includes(q) || real.includes(q)
  })
})
const canOpenUser = computed(() => authStore.canAccessSystemPermission('system.org.users.read'))
const canWriteUsers = computed(() => authStore.canAccessSystemPermission('system.org.users.write'))

/** 《列表操作列规范》：角色用户表操作列，默认收起、仅图标列头 */
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 220
const OP_COL_EXPANDED_MIN_WIDTH = 200
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function userStatusLabel(status: number) {
  if (status === 1) return t('systemUser.statusEnabled')
  if (status === 2) return t('systemUser.statusFrozen')
  return t('systemUser.statusDisabled')
}

function goEditUser(userId: string) {
  if (!canOpenUser.value || !userId) return
  router.push({ name: 'UserEdit', params: { id: userId } })
}

function openUserEdit(userId: string) {
  if (!canWriteUsers.value || !userId) return
  const { href } = router.resolve({ name: 'UserEdit', params: { id: userId } })
  window.open(href, '_blank', 'noopener,noreferrer')
}

function onRoleUserDblclick(row: AdminUserDto) {
  goEditUser(row.id)
}

function roleUserLabel(row: AdminUserDto) {
  const real = (row.realName || '').trim()
  const name = (row.userName || '').trim()
  if (real && name && real !== name) return `${real}（${name}）`
  return real || name || row.id
}

function applyRoleUsers(list: AdminUserDto[]) {
  roleUsers.value = Array.isArray(list) ? list : []
}

async function loadRoleUsers(opts?: { silent?: boolean }) {
  if (!isEdit || !roleId) {
    roleUsers.value = []
    return
  }
  if (!opts?.silent) usersLoading.value = true
  try {
    applyRoleUsers(await rbacAdminApi.getRoleUsers(roleId))
  } catch (e: unknown) {
    if (!opts?.silent) {
      roleUsers.value = []
      ElMessage.error(e instanceof Error ? e.message : t('systemRole.usersLoadFailed'))
    }
  } finally {
    if (!opts?.silent) usersLoading.value = false
  }
}

function onAddUsersSelectionChange(rows: AdminUserDto[]) {
  addUsersSelected.value = rows
}

function applyAddUsersSearch() {
  addUsersAppliedKw.value = addUsersKw.value.trim()
}

function onAddUsersDialogClosed() {
  addUsersCandidates.value = []
  addUsersSelected.value = []
  addUsersKw.value = ''
  addUsersAppliedKw.value = ''
  addUsersTableRef.value?.clearSelection?.()
}

async function openAddUsersDialog() {
  if (!canWriteUsers.value || !isEdit || !roleId) return
  addUsersVisible.value = true
  addUsersLoading.value = true
  addUsersSelected.value = []
  addUsersKw.value = ''
  addUsersAppliedKw.value = ''
  try {
    const all = await rbacAdminApi.getUsers()
    const already = new Set(roleUsers.value.map((u) => (u.id || '').toLowerCase()))
    addUsersCandidates.value = all
      .filter((u) => u.status === 1 && !already.has((u.id || '').toLowerCase()))
      .filter((u) => !(u.roleIds || []).some((id) => id.toLowerCase() === roleId.toLowerCase()))
      .sort((a, b) => (a.userName || '').localeCompare(b.userName || '', 'zh-CN'))
  } catch (e: unknown) {
    addUsersCandidates.value = []
    ElMessage.error(e instanceof Error ? e.message : t('systemRole.addUsersLoadFailed'))
  } finally {
    addUsersLoading.value = false
  }
}

async function confirmAddUsers() {
  if (!canWriteUsers.value || !roleId || addUsersSaving.value) return
  const ids = addUsersSelected.value.map((u) => u.id).filter(Boolean)
  if (ids.length === 0) {
    ElMessage.warning(t('systemRole.addUsersNeedSelect'))
    return
  }
  addUsersSaving.value = true
  try {
    await rbacAdminApi.addRoleUsers(roleId, ids)
    ElMessage.success(t('systemRole.addUsersSuccess'))
    addUsersVisible.value = false
    await loadRoleUsers()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemRole.addUsersFailed'))
  } finally {
    addUsersSaving.value = false
  }
}

async function handleRemoveRole(row: AdminUserDto) {
  if (!canWriteUsers.value || !roleId || !row.id || removingUserId.value) return
  try {
    await ElMessageBox.confirm(
      t('systemRole.removeRoleConfirmMessage', { user: roleUserLabel(row) }),
      t('systemRole.removeRoleConfirmTitle'),
      {
        type: 'warning',
        confirmButtonText: t('systemRole.removeRole'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }
  removingUserId.value = row.id
  try {
    await rbacAdminApi.removeRoleUser(roleId, row.id)
    const removedId = row.id.toLowerCase()
    applyRoleUsers(roleUsers.value.filter((u) => (u.id || '').toLowerCase() !== removedId))
    ElMessage.success(t('systemRole.removeRoleSuccess'))
    await loadRoleUsers({ silent: true })
    applyRoleUsers(roleUsers.value.filter((u) => (u.id || '').toLowerCase() !== removedId))
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemRole.removeRoleFailed'))
  } finally {
    removingUserId.value = null
  }
}

const permissions = ref<RbacPermission[]>([])
const permFilter = ref('')
const permKindFilter = ref<'all' | 'menu' | 'sub'>('all')

const formData = ref({
  roleCode: '',
  roleName: '',
  description: '',
  status: 1,
  permissionIds: [] as string[]
})

/** 与 AppLayout 侧栏一致：组顺序 + 组内项顺序；标题走同一套 i18n */
type SidebarMenuItemDef = { code: string; titleKey: string }
type SidebarMenuGroupDef = { key: string; titleKey: string; items: SidebarMenuItemDef[] }

const SIDEBAR_MENU_GROUPS: SidebarMenuGroupDef[] = [
  {
    key: 'analytics',
    titleKey: 'layout.sections.analytics',
    items: [
      { code: 'analytics-sales.read', titleKey: 'salesAnalytics.title' },
      { code: 'analytics-purchase.read', titleKey: 'purchaseAnalytics.title' },
      { code: 'analytics-logistics.read', titleKey: 'logisticsAnalytics.title' },
      { code: 'analytics-finance.read', titleKey: 'financeAnalytics.title' }
    ]
  },
  {
    key: 'commission',
    titleKey: 'layout.sections.commission',
    items: [
      { code: 'commission-estimated-sales.read', titleKey: 'layout.menu.commissionEstimatedSales' },
      { code: 'commission-estimated-purchase.read', titleKey: 'layout.menu.commissionEstimatedPurchase' },
      { code: 'commission-official-sales.read', titleKey: 'layout.menu.commissionOfficialSales' },
      { code: 'commission-official-purchase.read', titleKey: 'layout.menu.commissionOfficialPurchase' }
    ]
  },
  {
    key: 'business',
    titleKey: 'layout.sections.businessManagement',
    items: [{ code: 'biz-brand.read', titleKey: 'layout.menu.brandManagement' }]
  },
  {
    key: 'ops',
    titleKey: 'layout.sections.ops',
    items: [
      { code: 'biz.feedback.admin', titleKey: 'layout.menu.userFeedback' },
      { code: 'sys.errorlog.read', titleKey: 'layout.menu.systemErrors' },
      { code: 'biz.telemetry.analytics', titleKey: 'layout.menu.telemetryAnalytics' }
    ]
  },
  {
    key: 'org',
    titleKey: 'layout.menu.systemManagement',
    items: [
      { code: 'system.org.users.read', titleKey: 'layout.menu.userManagement' },
      { code: 'system.org.departments.read', titleKey: 'layout.menu.departmentManagement' },
      { code: 'system.rbac.roles.read', titleKey: 'layout.menu.roleManagement' },
      { code: 'system.rbac.permissions.read', titleKey: 'layout.menu.permissionManagement' },
      { code: 'system.org.user-config.read', titleKey: 'layout.menu.userConfig' }
    ]
  },
  {
    key: 'params',
    titleKey: 'layout.menu.paramManagement',
    items: [
      { code: 'system.params.company.read', titleKey: 'layout.menu.companyInfo' },
      { code: 'system.params.dict.read', titleKey: 'layout.menu.dictItems' },
      { code: 'biz.ai.admin', titleKey: 'layout.menu.aiConfig' },
      { code: 'system.params.sales.read', titleKey: 'layout.menu.salesParams' },
      { code: 'system.params.purchase.read', titleKey: 'layout.menu.purchaseParams' },
      { code: 'system.params.finance.read', titleKey: 'layout.menu.financeParams' },
      { code: 'system.params.report.read', titleKey: 'layout.menu.reportParams' }
    ]
  },
  {
    key: 'logs',
    titleKey: 'layout.menu.systemLogs',
    items: [
      { code: 'system.logs.login.read', titleKey: 'layout.menu.loginLog' },
      { code: 'system.logs.operation.read', titleKey: 'layout.menu.operationLog' }
    ]
  }
]

const MENU_ENTRY_BY_CODE = (() => {
  const map = new Map<string, { groupKey: string; groupOrder: number; itemOrder: number; titleKey: string }>()
  SIDEBAR_MENU_GROUPS.forEach((g, groupOrder) => {
    g.items.forEach((item, itemOrder) => {
      map.set(item.code, { groupKey: g.key, groupOrder, itemOrder, titleKey: item.titleKey })
    })
  })
  return map
})()

const MENU_GROUP_TITLE_KEY = Object.fromEntries(
  SIDEBAR_MENU_GROUPS.map((g) => [g.key, g.titleKey])
) as Record<string, string>

/** 参数页内部左侧子导航（非侧栏一级）；未来新增按 system.params.{area}.{feature}.read|write 命名即可自动识别 */
const PAGE_SUB_LABELS: Record<string, string> = {
  'system.params.sales.refresh-customer.read': '销售参数 → 刷新客户',
  'system.params.sales.refresh-customer.write': '销售参数 → 刷新客户（写）',
  'system.params.purchase.assignee-count.read': '采购参数 → 报价人数',
  'system.params.purchase.assignee-count.write': '采购参数 → 报价人数（写）',
  'system.params.purchase.quoter-pool.read': '采购参数 → 报价员池',
  'system.params.purchase.quoter-pool.write': '采购参数 → 报价员池（写）',
  'system.params.purchase.default-assign-method.read': '采购参数 → 默认分配方式',
  'system.params.purchase.default-assign-method.write': '采购参数 → 默认分配方式（写）',
  'system.params.purchase.demand-protection.read': '采购参数 → 需求保护',
  'system.params.purchase.demand-protection.write': '采购参数 → 需求保护（写）',
  'system.params.purchase.refresh-vendor.read': '采购参数 → 分面刷新',
  'system.params.purchase.refresh-vendor.write': '采购参数 → 分面刷新（写）',
  'system.params.finance.exchange-rates.read': '财务参数 → 汇率',
  'system.params.finance.exchange-rates.write': '财务参数 → 汇率（写）',
  'system.params.finance.purchase-cost-params.read': '财务参数 → 采购系数',
  'system.params.finance.purchase-cost-params.write': '财务参数 → 采购系数（写）',
  'system.params.finance.payment-banks.read': '财务参数 → 付款银行',
  'system.params.finance.payment-banks.write': '财务参数 → 付款银行（写）',
  'system.params.report.global.read': '报表参数 → 报表全局参数',
  'system.params.report.global.write': '报表参数 → 报表全局参数（写）'
}

type PermKind = 'menu' | 'sub' | 'feature'

function isParamsPageSub(code: string): boolean {
  if (PAGE_SUB_LABELS[code]) return true
  // 约定：system.params.{area}.{feature…}.(read|write)，段数 ≥ 5
  const parts = code.split('.')
  if (parts.length < 5) return false
  if (parts[0] !== 'system' || parts[1] !== 'params') return false
  if (!['sales', 'purchase', 'finance', 'report'].includes(parts[2])) return false
  const action = parts[parts.length - 1]
  return action === 'read' || action === 'write'
}

function resolvePermKind(code: string): PermKind {
  if (MENU_ENTRY_BY_CODE.has(code)) return 'menu'
  if (isParamsPageSub(code)) return 'sub'
  return 'feature'
}

function permKindLabel(code: string): string {
  const kind = resolvePermKind(code)
  if (kind === 'menu') return t('systemRole.permKindMenu')
  if (kind === 'sub') return t('systemRole.permKindSub')
  return t('systemRole.permKindFeature')
}

function permKindTagType(code: string): 'primary' | 'warning' | 'info' {
  const kind = resolvePermKind(code)
  if (kind === 'menu') return 'primary'
  if (kind === 'sub') return 'warning'
  return 'info'
}

function permMenuLabel(code: string): string {
  const menu = MENU_ENTRY_BY_CODE.get(code)
  if (menu) return t(menu.titleKey)
  if (PAGE_SUB_LABELS[code]) return PAGE_SUB_LABELS[code]
  if (isParamsPageSub(code)) {
    const parts = code.split('.')
    const area = parts[2]
    const feature = parts.slice(3, -1).join('.')
    const areaLabel =
      area === 'sales'
        ? t('layout.menu.salesParams')
        : area === 'purchase'
          ? t('layout.menu.purchaseParams')
          : area === 'finance'
            ? t('layout.menu.financeParams')
            : area === 'report'
              ? t('layout.menu.reportParams')
              : area
    return `${areaLabel} → ${feature}`
  }
  return ''
}

const legendTitle = computed(
  () =>
    `${t('systemRole.permKindMenuHint')}；${t('systemRole.permKindSubHint')}；${t('systemRole.permKindFeatureHint')}`
)

type GroupMeta = {
  key: string
  label: string
  sort: number
  itemSort: (code: string) => number
}

function resolveGroupMeta(p: RbacPermission): GroupMeta {
  const code = p.permissionCode ?? ''
  const menu = MENU_ENTRY_BY_CODE.get(code)
  if (menu) {
    return {
      key: `menu:${menu.groupKey}`,
      label: t(MENU_GROUP_TITLE_KEY[menu.groupKey] || menu.groupKey),
      sort: menu.groupOrder,
      itemSort: (c) => MENU_ENTRY_BY_CODE.get(c)?.itemOrder ?? 999
    }
  }
  if (isParamsPageSub(code)) {
    const area = code.split('.')[2]
    if (area === 'sales') {
      return { key: 'sub:sales', label: `${t('layout.menu.salesParams')} · ${t('systemRole.permKindSub')}`, sort: 100, itemSort: () => 0 }
    }
    if (area === 'purchase') {
      return { key: 'sub:purchase', label: `${t('layout.menu.purchaseParams')} · ${t('systemRole.permKindSub')}`, sort: 101, itemSort: () => 0 }
    }
    if (area === 'finance') {
      return { key: 'sub:finance', label: `${t('layout.menu.financeParams')} · ${t('systemRole.permKindSub')}`, sort: 102, itemSort: () => 0 }
    }
    if (area === 'report') {
      return { key: 'sub:report', label: `${t('layout.menu.reportParams')} · ${t('systemRole.permKindSub')}`, sort: 103, itemSort: () => 0 }
    }
    return { key: 'sub:params', label: `${t('layout.menu.paramManagement')} · ${t('systemRole.permKindSub')}`, sort: 104, itemSort: () => 0 }
  }
  if (code.startsWith('biz.ai.')) {
    return { key: 'feat:ai', label: 'AI', sort: 200, itemSort: () => 0 }
  }
  if (code.startsWith('biz.feedback.') || code.startsWith('sys.errorlog.') || code.startsWith('biz.telemetry.')) {
    return { key: 'feat:ops', label: `${t('layout.sections.ops')} · ${t('systemRole.permKindFeature')}`, sort: 201, itemSort: () => 0 }
  }
  if (code === 'biz-brand.write' || code.startsWith('biz-brand.')) {
    return { key: 'feat:brand', label: `${t('layout.sections.businessManagement')} · ${t('systemRole.permKindFeature')}`, sort: 202, itemSort: () => 0 }
  }
  const resource = (p.resource ?? '').trim()
  if (resource) {
    return { key: `feat:res:${resource}`, label: resource, sort: 800, itemSort: () => 0 }
  }
  const dot = code.indexOf('.')
  const prefix = dot > 0 ? code.slice(0, dot) : code || 'other'
  return { key: `feat:${prefix}`, label: prefix, sort: 900, itemSort: () => 0 }
}

const permissionGroups = computed(() => {
  const q = permFilter.value.trim().toLowerCase()
  const kindFilter = permKindFilter.value
  const byCode = new Map(
    permissions.value.filter((p) => p.status === 1).map((p) => [p.permissionCode, p] as const)
  )

  // 主菜单入口：始终按侧栏结构完整展示（库中缺失时禁用并提示执行种子）
  if (kindFilter === 'menu') {
    return SIDEBAR_MENU_GROUPS.map((g, groupOrder) => {
      const items = g.items
        .map((item) => {
          const existing = byCode.get(item.code)
          const row: RbacPermission = existing ?? {
            id: '',
            permissionCode: item.code,
            permissionName: t(item.titleKey),
            permissionType: 'api',
            status: 1
          }
          return row
        })
        .filter((p) => {
          if (!q) return true
          const menuHint = permMenuLabel(p.permissionCode).toLowerCase()
          return (
            p.permissionCode.toLowerCase().includes(q) ||
            p.permissionName.toLowerCase().includes(q) ||
            menuHint.includes(q)
          )
        })
      if (items.length === 0) return null
      return {
        key: `menu:${g.key}`,
        label: t(g.titleKey),
        items,
        sort: groupOrder
      }
    }).filter((g): g is NonNullable<typeof g> => g != null)
  }

  const filtered = permissions.value.filter((p) => {
    if (p.status !== 1) return false
    if ((p.permissionCode ?? '').toLowerCase().startsWith('system.params.commission')) return false
    const kind = resolvePermKind(p.permissionCode)
    if (kindFilter !== 'all' && kind !== kindFilter) return false
    if (!q) return true
    const menuHint = permMenuLabel(p.permissionCode).toLowerCase()
    return (
      p.permissionCode.toLowerCase().includes(q) ||
      p.permissionName.toLowerCase().includes(q) ||
      (p.resource ?? '').toLowerCase().includes(q) ||
      menuHint.includes(q)
    )
  })
  const map = new Map<string, { meta: GroupMeta; items: RbacPermission[] }>()
  for (const p of filtered) {
    const meta = resolveGroupMeta(p)
    const bucket = map.get(meta.key)
    if (bucket) bucket.items.push(p)
    else map.set(meta.key, { meta, items: [p] })
  }
  return [...map.values()]
    .sort((a, b) => a.meta.sort - b.meta.sort || a.meta.label.localeCompare(b.meta.label, 'zh-CN'))
    .map(({ meta, items }) => ({
      key: meta.key,
      label: meta.label,
      items: [...items].sort((a, b) => {
        const byMenu = meta.itemSort(a.permissionCode) - meta.itemSort(b.permissionCode)
        if (byMenu !== 0) return byMenu
        return a.permissionCode.localeCompare(b.permissionCode, 'zh-CN')
      })
    }))
})

const load = async () => {
  loading.value = true
  try {
    permissions.value = await rbacAdminApi.getPermissions()

    if (isEdit && roleId) {
      const roles: RbacRole[] = await rbacAdminApi.getRoles()
      const role = roles.find(r => r.id === roleId)
      const isSysAdminRole = String(role?.roleCode || '').toUpperCase() === 'SYS_ADMIN'
      if (!role || (isSysAdminRole && !authStore.hasSysAdminRole())) {
        ElMessage.error(t('systemRole.notFound'))
        await router.replace({ name: 'RoleList' })
        return
      }

      formData.value.roleCode = role.roleCode
      formData.value.roleName = role.roleName
      formData.value.description = role.description || ''
      formData.value.status = role.status ?? 1

      formData.value.permissionIds = await rbacAdminApi.getRolePermissionIds(roleId)
      await loadRoleUsers()
    }
  } catch (e: any) {
    ElMessage.error(e?.message || t('systemRole.loadDetailFailed'))
  } finally {
    loading.value = false
  }
}

function permissionIdsForSave(): string[] {
  const roleCode = (formData.value.roleCode || '').toUpperCase()
  const keepCommission = roleCode === 'SYS_ADMIN' || roleCode === 'SYS_MANAGER'
  const commissionIds = new Set(
    permissions.value
      .filter((p) => (p.permissionCode ?? '').toLowerCase().startsWith('system.params.commission'))
      .map((p) => p.id)
  )
  return formData.value.permissionIds.filter((id) => {
    if (!id || id.startsWith('__missing__:')) return false
    if (keepCommission) return true
    return !commissionIds.has(id)
  })
}

const handleSubmit = async () => {
  if (saving.value) return

  if (!formData.value.roleCode.trim() && !isEdit) {
    ElMessage.warning(t('systemRole.fillRoleCode'))
    return
  }
  if (!formData.value.roleName.trim()) {
    ElMessage.warning(t('systemRole.fillRoleName'))
    return
  }

  saving.value = true
  try {
    if (isEdit && roleId) {
      await rbacAdminApi.updateRole(roleId, {
        roleName: formData.value.roleName,
        description: formData.value.description || undefined,
        status: formData.value.status
      })

      await rbacAdminApi.assignRolePermissions(roleId, permissionIdsForSave())
      ElMessage.success(t('common.saveSuccess'))
    } else {
      const created = await rbacAdminApi.createRole({
        roleCode: formData.value.roleCode,
        roleName: formData.value.roleName,
        description: formData.value.description || undefined,
        status: formData.value.status
      })
      await rbacAdminApi.assignRolePermissions(created.id, permissionIdsForSave())
      ElMessage.success(t('common.createSuccess'))
    }

    router.push({ name: 'RoleList' })
  } catch (e: any) {
    ElMessage.error(e?.message || t('common.saveFailed'))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.role-edit-page {
  padding: 20px;
  min-height: 320px;
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
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-bar {
  display: inline-block;
  width: 3px;
  height: 14px;
  border-radius: 2px;
  background: $cyan-primary;
}

.section-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.section-head__right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.role-users-count {
  font-size: 13px;
  color: $text-muted;
}

.add-users-hint {
  margin: 0 0 12px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;
}

.add-users-search {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.add-users-search .el-input {
  flex: 1;
  max-width: 280px;
}

.role-users-table {
  width: 100%;
}

.role-users-link {
  padding: 0;
  border: none;
  background: transparent;
  color: var(--el-color-primary);
  cursor: pointer;
  font: inherit;
}

.role-users-link:hover {
  text-decoration: underline;
}

.action-btn--primary {
  color: $cyan-primary;
}

.action-btn--danger {
  color: $color-red-brown;
}

.op-more-item {
  font-size: 13px;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--danger {
  color: $color-red-brown;
}

.role-users-empty {
  padding: 16px 0 4px;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-secondary, #909399);
}

.footer-bar {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 18px;
}

.role-perm-picker {
  width: 100%;
  border: 1px solid var(--el-border-color, #dcdfe6);
  border-radius: 8px;
  background: var(--el-fill-color-blank, #fff);
}

.role-perm-picker__tabs {
  display: flex;
  align-items: center;
  padding: 10px 12px 0;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
}

.role-perm-picker__toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
}

.role-perm-picker__filter {
  flex: 1 1 auto;
  min-width: 0;
}

.role-perm-picker__legend {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
  padding: 8px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter, #ebeef5);
  background: var(--el-fill-color-lighter, #f5f7fa);
  font-size: 12px;
  color: var(--el-text-color-secondary, #909399);
}

.role-perm-picker__legend-label {
  font-weight: 600;
  color: var(--el-text-color-regular, #606266);
}

.role-perm-picker__legend-hint {
  margin-right: 8px;
}

.role-perm-picker__count {
  flex: 0 0 auto;
  font-size: 13px;
  color: var(--el-text-color-secondary, #909399);
  white-space: nowrap;
}

.role-perm-picker__list {
  max-height: 420px;
  overflow: auto;
  padding: 8px 12px 12px;
}

.role-perm-picker__empty {
  padding: 24px 12px;
  text-align: center;
  font-size: 13px;
  color: var(--el-text-color-secondary, #909399);
}

.role-perm-picker__section + .role-perm-picker__section {
  margin-top: 12px;
  padding-top: 12px;
  border-top: 1px dashed var(--el-border-color-lighter, #ebeef5);
}

.role-perm-picker__group-title {
  margin-bottom: 6px;
  font-size: 12px;
  font-weight: 600;
  color: var(--el-text-color-secondary, #909399);
  letter-spacing: 0.02em;
}

.role-perm-picker__group {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.role-perm-picker__item {
  display: flex;
  align-items: flex-start;
  margin-right: 0;
  height: auto;
  padding: 4px 0;
}

.role-perm-picker__item :deep(.el-checkbox__label) {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  line-height: 1.45;
  white-space: normal;
}

.role-perm-picker__kind {
  flex: 0 0 auto;
}

.role-perm-picker__code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
  color: var(--el-text-color-primary, #303133);
}

.role-perm-picker__name {
  font-size: 13px;
  color: var(--el-text-color-regular, #606266);
}

.role-perm-picker__menu-hint {
  font-size: 12px;
  color: var(--el-color-primary);
}

.role-perm-picker__missing {
  font-size: 12px;
  color: var(--el-color-warning-dark-2, #b88230);
}
</style>

