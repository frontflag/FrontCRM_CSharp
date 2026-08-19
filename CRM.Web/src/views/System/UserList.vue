<template>
  <div class="user-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M23 21v-2a4 4 0 00-3-3.87" />
              <path d="M16 3.13a4 4 0 010 7.75" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('systemUser.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('systemUser.count', { count: filteredUsers.length }) }}</div>
      </div>
      <div v-if="canWrite" class="header-right">
        <el-tooltip :content="t('systemUser.resetPasswordHint')" placement="bottom">
          <button
            type="button"
            class="btn-ghost"
            :disabled="selectedUsers.length === 0"
            @click="openResetDialogFromSelection"
          >
            {{ t('systemUser.resetPassword') }}
          </button>
        </el-tooltip>
        <button type="button" class="btn-primary" @click="router.push({ name: 'UserCreate' })">
          {{ t('systemUser.create') }}
        </button>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchFilters.userNameKw"
            class="search-input search-input--narrow"
            :placeholder="t('systemUser.colUserName')"
            @keyup.enter="applySearch"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchFilters.realNameKw"
            class="search-input search-input--narrow"
            :placeholder="t('systemUser.colRealName')"
            @keyup.enter="applySearch"
          />
        </div>
        <el-select
          v-model="searchFilters.departmentId"
          class="status-select status-select--dept"
          clearable
          filterable
          :placeholder="t('systemUser.allDepartments')"
          :teleported="false"
          @change="applySearch"
        >
          <el-option
            v-for="d in departmentOptions"
            :key="d.id"
            :label="d.departmentName"
            :value="d.id"
          />
        </el-select>
        <el-select
          v-model="searchFilters.roleId"
          class="status-select status-select--role"
          clearable
          filterable
          :placeholder="t('systemUser.allRoles')"
          :teleported="false"
          @change="applySearch"
        >
          <el-option
            v-for="r in roleOptions"
            :key="r.id"
            :label="`${r.roleName} (${r.roleCode})`"
            :value="r.id"
          />
        </el-select>
        <el-select
          v-model="searchFilters.statusFilter"
          class="status-select status-select--status"
          clearable
          :placeholder="t('systemUser.allStatuses')"
          :teleported="false"
          @change="applySearch"
        >
          <el-option :label="t('systemUser.allStatuses')" value="all" />
          <el-option :label="t('systemUser.freezeFilterNormal')" value="1" />
          <el-option :label="t('systemUser.freezeFilterDisabled')" value="0" />
          <el-option :label="t('systemUser.freezeFilterFrozen')" value="2" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="applySearch">
          {{ t('systemUser.searchQuery') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetSearch">
          {{ t('systemUser.searchReset') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || pagedUsers.length > 0"
        ref="dataTableRef"
        column-layout-key="system-user-list-main-v3"
        :columns="userTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="pagedUsers"
        row-key="id"
        :row-class-name="userRowClassName"
        @selection-change="onSelectionChange"
        @row-dblclick="onRowDblclick"
      >
        <template #col-status="{ row }">
          <el-tag
            v-if="row.status === 2"
            effect="dark"
            type="danger"
            size="small"
          >
            {{ t('systemUser.statusFrozen') }}
          </el-tag>
          <el-tag v-else effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? t('systemUser.statusEnabled') : t('systemUser.statusDisabled') }}
          </el-tag>
        </template>
        <template #col-level="{ row }">{{ row.level ?? 1 }}</template>
        <template #col-roleCodes="{ row }">
          <span>{{ row.roleCodes?.join(', ') || '-' }}</span>
        </template>
        <template #col-primaryDepartmentName="{ row }">
          <span>{{ row.primaryDepartmentName || '-' }}</span>
        </template>
        <template #col-createTime="{ row }">
          <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime || row.createdAt)]" :key="'ct-' + row.id">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || '-' }}
        </template>
        <template #col-actions-header>
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
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="goEdit(row.id)">{{ t('systemUser.edit') }}</el-button>
              <el-button link type="primary" @click.stop="openResetDialogForRow(row)">{{ t('systemUser.resetPassword') }}</el-button>
              <el-button
                v-if="freezeRestoreVisibleForRow(row) && row.status !== 2"
                link
                type="warning"
                :loading="freezeRestoreUserId === row.id"
                @click.stop="handleFreeze(row)"
              >
                {{ t('systemUser.freeze') }}
              </el-button>
              <el-button
                v-if="row.status === 2"
                link
                type="success"
                :loading="freezeRestoreUserId === row.id"
                @click.stop="handleRestore(row)"
              >
                {{ t('systemUser.restore') }}
              </el-button>
              <el-button link type="danger" @click.stop="requestDelete(row)">{{ t('systemUser.delete') }}</el-button>
              <el-button
                v-if="canImpersonate && impersonateVisibleForRow(row)"
                link
                type="warning"
                :loading="impersonateUserId === row.id"
                @click.stop="handleImpersonate(row)"
              >
                {{ t('systemUser.impersonate') }}
              </el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end" @command="(cmd: string) => onRowCommand(cmd, row)">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="edit">
                    <span class="op-more-item op-more-item--primary">{{ t('systemUser.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item command="resetPassword">
                    <span class="op-more-item op-more-item--primary">{{ t('systemUser.resetPassword') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="freezeRestoreVisibleForRow(row) && row.status !== 2"
                    :disabled="freezeRestoreUserId === row.id"
                    command="freeze"
                  >
                    <span class="op-more-item op-more-item--warning">{{ t('systemUser.freeze') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="row.status === 2"
                    :disabled="freezeRestoreUserId === row.id"
                    command="restore"
                  >
                    <span class="op-more-item op-more-item--success">{{ t('systemUser.restore') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item command="delete">
                    <span class="op-more-item op-more-item--danger">{{ t('systemUser.delete') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="canImpersonate && impersonateVisibleForRow(row)"
                    :disabled="impersonateUserId === row.id"
                    command="impersonate"
                  >
                    <span class="op-more-item op-more-item--warning">
                      {{ t('systemUser.impersonate') }}
                    </span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <div v-show="!loading && pagedUsers.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" aria-hidden="true">
          <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M23 21v-2a4 4 0 00-3-3.87" />
          <path d="M16 3.13a4 4 0 010 7.75" />
        </svg>
        <p>{{ t('systemUser.empty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="filteredUsers.length"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
      />
    </div>

    <el-dialog
      v-model="resetDialogVisible"
      :title="t('systemUser.resetPasswordTitle')"
      width="480px"
      destroy-on-close
      append-to-body
      @closed="onResetDialogClosed"
    >
      <p class="reset-pwd-intro">{{ t('systemUser.resetPasswordTargets') }}</p>
      <ul class="reset-pwd-user-list">
        <li v-for="u in usersToReset" :key="u.id">{{ u.realName || u.userName }}（{{ u.userName }}）</li>
      </ul>
      <el-form label-width="100px" @submit.prevent>
        <el-form-item :label="t('systemUser.resetPasswordNew')">
          <el-input
            v-model="resetPwdForm.newPassword"
            type="password"
            show-password
            autocomplete="new-password"
            :placeholder="t('systemUser.resetPasswordMin')"
          />
        </el-form-item>
        <el-form-item :label="t('systemUser.resetPasswordConfirm')">
          <el-input
            v-model="resetPwdForm.confirmPassword"
            type="password"
            show-password
            autocomplete="new-password"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="resetDialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="resetPwdSaving" @click="submitResetPassword">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { rbacAdminApi, type AdminUserDto, type RbacDepartment, type RbacRole } from '@/api/rbacAdmin'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { useAuthStore } from '@/stores/auth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const canWrite = computed(() => authStore.canAccessSystemPermission('system.org.users.write'))

const loading = ref(false)
const allUsers = ref<AdminUserDto[]>([])
const departmentOptions = ref<RbacDepartment[]>([])
const roleOptions = ref<RbacRole[]>([])
const page = ref(1)
const pageSize = ref(20)

const searchFilters = reactive({
  departmentId: '' as string,
  roleId: '' as string,
  userNameKw: '',
  realNameKw: '',
  /** all | 0 | 1 | 2 */
  statusFilter: 'all' as string
})

const appliedFilters = reactive({
  departmentId: '' as string,
  roleId: '' as string,
  userNameKw: '',
  realNameKw: '',
  statusFilter: 'all' as string
})

const selectedUsers = ref<AdminUserDto[]>([])
const impersonateUserId = ref<string | null>(null)
const freezeRestoreUserId = ref<string | null>(null)
const dataTableRef = ref<{
  openColumnSettings?: () => void
  clearSelection?: () => void
} | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const resetDialogVisible = ref(false)
const usersToReset = ref<AdminUserDto[]>([])
const resetPwdForm = ref({ newPassword: '', confirmPassword: '' })
const resetPwdSaving = ref(false)

/** 仅系统管理员（SYS_ADMIN）可见模拟登录 */
const canImpersonate = computed(() => authStore.user?.isSysAdmin === true)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = computed(() => (canImpersonate.value ? 520 : 420))
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH.value : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH.value : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function applySearch() {
  appliedFilters.departmentId = searchFilters.departmentId?.trim() ?? ''
  appliedFilters.roleId = searchFilters.roleId?.trim() ?? ''
  appliedFilters.userNameKw = searchFilters.userNameKw?.trim() ?? ''
  appliedFilters.realNameKw = searchFilters.realNameKw?.trim() ?? ''
  const sf = searchFilters.statusFilter?.trim()
  appliedFilters.statusFilter = sf && ['all', '0', '1', '2'].includes(sf) ? sf : 'all'
  page.value = 1
  dataTableRef.value?.clearSelection?.()
  selectedUsers.value = []
}

function resetSearch() {
  searchFilters.departmentId = ''
  searchFilters.roleId = ''
  searchFilters.userNameKw = ''
  searchFilters.realNameKw = ''
  searchFilters.statusFilter = 'all'
  applySearch()
}

const filteredUsers = computed(() => {
  const deptId = appliedFilters.departmentId
  const roleId = appliedFilters.roleId
  const uname = appliedFilters.userNameKw.toLowerCase()
  const rname = appliedFilters.realNameKw.toLowerCase()
  const statusKey = appliedFilters.statusFilter

  return allUsers.value.filter((u) => {
    if (statusKey && statusKey !== 'all') {
      const want = Number(statusKey)
      if (!Number.isNaN(want) && u.status !== want) return false
    }
    if (deptId) {
      const ids = u.departmentIds || []
      const inDept = ids.some((id) => stringEq(id, deptId))
      if (!inDept) return false
    }
    if (roleId) {
      const rids = u.roleIds || []
      if (!rids.some((id) => stringEq(id, roleId))) return false
    }
    if (uname) {
      const v = (u.userName || '').toLowerCase()
      if (!v.includes(uname)) return false
    }
    if (rname) {
      const v = (u.realName || '').toLowerCase()
      if (!v.includes(rname)) return false
    }
    return true
  })
})

const pagedUsers = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredUsers.value.slice(start, start + pageSize.value)
})

watch(pageSize, () => {
  page.value = 1
})

watch(filteredUsers, (list) => {
  const maxPage = Math.max(1, Math.ceil(list.length / pageSize.value) || 1)
  if (page.value > maxPage) page.value = maxPage
})

function stringEq(a: string, b: string) {
  return (a || '').trim().toLowerCase() === (b || '').trim().toLowerCase()
}

function headerMin(label: string, extra?: { align?: 'left' | 'center' | 'right'; extra?: number }) {
  return estimateListColumnHeaderMinWidth(label, extra)
}

const userTableColumns = computed<CrmTableColumnDef[]>(() => {
  const status = t('systemUser.colStatus')
  const userName = t('systemUser.colUserName')
  const realName = t('systemUser.colRealName')
  const level = t('systemUser.colLevel')
  const email = t('systemUser.colEmail')
  const mobile = t('systemUser.colMobile')
  const roleCodes = t('systemUser.colRoleCodes')
  const dept = t('systemUser.colPrimaryDept')
  const createTime = t('systemUser.colCreateTime')
  const createUser = t('systemUser.colCreateUser')
  return [
    {
      key: 'selection',
      type: 'selection',
      width: 48,
      align: 'center',
      fixed: 'left',
      hideable: false,
      reorderable: false,
      pinned: 'start'
    },
    { key: 'status', label: status, prop: 'status', width: Math.max(90, headerMin(status, { align: 'center' })), align: 'center' },
    { key: 'userName', label: userName, prop: 'userName', minWidth: Math.max(160, headerMin(userName)), showOverflowTooltip: true },
    { key: 'realName', label: realName, prop: 'realName', minWidth: Math.max(160, headerMin(realName)), showOverflowTooltip: true },
    { key: 'level', label: level, width: Math.max(80, headerMin(level, { align: 'center' })), align: 'center' },
    { key: 'email', label: email, prop: 'email', minWidth: Math.max(200, headerMin(email)), showOverflowTooltip: true },
    { key: 'mobile', label: mobile, prop: 'mobile', width: Math.max(140, headerMin(mobile)) },
    { key: 'roleCodes', label: roleCodes, minWidth: Math.max(220, headerMin(roleCodes)), showOverflowTooltip: true },
    { key: 'primaryDepartmentName', label: dept, prop: 'primaryDepartmentName', minWidth: Math.max(220, headerMin(dept)), showOverflowTooltip: true },
    { key: 'createTime', label: createTime, width: Math.max(160, headerMin(createTime)) },
    { key: 'createUser', label: createUser, width: Math.max(120, headerMin(createUser)), showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('systemUser.action'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
      resizable: false
    }
  ]
})

function impersonateVisibleForRow(row: AdminUserDto) {
  if (row.status !== 1) return false
  const selfId = authStore.user?.id
  if (selfId && row.id === selfId) return false
  return true
}

function freezeRestoreVisibleForRow(row: AdminUserDto) {
  const selfId = authStore.user?.id
  if (selfId && row.id === selfId) return false
  return true
}

function userRowClassName({ row }: { row: AdminUserDto }) {
  return ['table-row-pointer', row.status === 2 ? 'user-list-row--frozen' : ''].filter(Boolean).join(' ')
}

function onSelectionChange(rows: AdminUserDto[]) {
  selectedUsers.value = rows
}

function openResetDialog(targets: AdminUserDto[]) {
  if (targets.length === 0) return
  usersToReset.value = targets
  resetPwdForm.value = { newPassword: '', confirmPassword: '' }
  resetDialogVisible.value = true
}

function openResetDialogFromSelection() {
  if (selectedUsers.value.length === 0) {
    ElMessage.warning(t('systemUser.resetPasswordSelectFirst'))
    return
  }
  openResetDialog([...selectedUsers.value])
}

function openResetDialogForRow(row: AdminUserDto) {
  openResetDialog([row])
}

function onResetDialogClosed() {
  usersToReset.value = []
}

async function submitResetPassword() {
  const { newPassword, confirmPassword } = resetPwdForm.value
  if (!newPassword || newPassword.length < 6) {
    ElMessage.warning(t('systemUser.resetPasswordMin'))
    return
  }
  if (newPassword !== confirmPassword) {
    ElMessage.warning(t('systemUser.resetPasswordMismatch'))
    return
  }
  resetPwdSaving.value = true
  try {
    for (const u of usersToReset.value) {
      await rbacAdminApi.resetUserPassword(u.id, newPassword)
    }
    ElMessage.success(t('systemUser.resetPasswordSuccess'))
    resetDialogVisible.value = false
    dataTableRef.value?.clearSelection?.()
    selectedUsers.value = []
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('systemUser.resetPasswordFailed')
    ElMessage.error(msg)
  } finally {
    resetPwdSaving.value = false
  }
}

const load = async () => {
  loading.value = true
  try {
    const [userList, depts, roles] = await Promise.all([
      rbacAdminApi.getUsers(),
      rbacAdminApi.getDepartments().catch(() => [] as RbacDepartment[]),
      rbacAdminApi.getRoles().catch(() => [] as RbacRole[])
    ])
    allUsers.value = userList
    departmentOptions.value = [...depts].filter((d) => d.status === 1).sort((a, b) => a.departmentName.localeCompare(b.departmentName))
    roleOptions.value = [...roles]
      .filter((r) => r.status === 1)
      .filter((r) => authStore.user?.isSysAdmin === true || r.roleCode !== 'SYS_ADMIN')
      .sort((a, b) => a.roleName.localeCompare(b.roleName))
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemUser.loadFailed'))
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'UserEdit', params: { id } })
}

const onRowDblclick = (row: AdminUserDto) => {
  if (!canWrite.value) return
  goEdit(row.id)
}

const requestDelete = (row: AdminUserDto) => {
  console.info('[UserList] delete click', { id: row.id, userName: row.userName })
  void handleDelete(row)
}

const onRowCommand = (cmd: string, row: AdminUserDto) => {
  console.info('[UserList] row command', { cmd, id: row.id, userName: row.userName })
  if (cmd === 'edit') {
    goEdit(row.id)
    return
  }
  if (cmd === 'resetPassword') {
    openResetDialogForRow(row)
    return
  }
  if (cmd === 'delete') {
    requestDelete(row)
    return
  }
  if (cmd === 'freeze') {
    void handleFreeze(row)
    return
  }
  if (cmd === 'restore') {
    void handleRestore(row)
    return
  }
  if (cmd === 'impersonate') {
    void handleImpersonate(row)
  }
}

function deleteEmployeeConfirmMessage(row: AdminUserDto): string {
  const userName = row.userName?.trim() || '—'
  const realName = row.realName?.trim() || '—'
  return t('systemUser.deleteConfirmMessageWithUser', { userName, realName })
}

const handleDelete = async (row: AdminUserDto) => {
  const id = row.id
  try {
    console.info('[UserList] open delete confirm', { id, userName: row.userName })
    await ElMessageBox.confirm(deleteEmployeeConfirmMessage(row), t('systemUser.deleteConfirmTitle'), {
      type: 'warning',
      confirmButtonText: t('systemUser.delete'),
      cancelButtonText: t('common.cancel')
    })
    console.info('[UserList] delete confirmed, calling api', { id })
    await rbacAdminApi.deleteUser(id)
    ElMessage.success(t('systemUser.deleteSuccess'))
    console.info('[UserList] delete api success, reloading list', { id })
    await load()
  } catch (e: unknown) {
    if (e === 'cancel' || e === 'close') {
      console.info('[UserList] delete canceled', { id, reason: e })
      return
    }
    console.error('[UserList] delete failed', { id, error: e })
    const msg = e instanceof Error ? e.message : t('systemUser.deleteFailed')
    ElMessage.error(msg)
  }
}

function freezeConfirmMessage(row: AdminUserDto): string {
  const userName = row.userName?.trim() || '—'
  const realName = row.realName?.trim() || '—'
  return t('systemUser.freezeConfirmMessage', { userName, realName })
}

function restoreConfirmMessage(row: AdminUserDto): string {
  const userName = row.userName?.trim() || '—'
  const realName = row.realName?.trim() || '—'
  return t('systemUser.restoreConfirmMessage', { userName, realName })
}

async function handleFreeze(row: AdminUserDto) {
  if (!freezeRestoreVisibleForRow(row)) {
    ElMessage.warning(t('systemUser.cannotFreezeSelf'))
    return
  }
  if (row.status === 2) return
  try {
    await ElMessageBox.confirm(freezeConfirmMessage(row), t('systemUser.freezeConfirmTitle'), {
      type: 'warning',
      confirmButtonText: t('systemUser.freeze'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }
  freezeRestoreUserId.value = row.id
  try {
    await rbacAdminApi.freezeUser(row.id)
    ElMessage.success(t('systemUser.freezeSuccess'))
    await load()
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('systemUser.freezeFailed')
    ElMessage.error(msg)
  } finally {
    freezeRestoreUserId.value = null
  }
}

async function handleRestore(row: AdminUserDto) {
  if (row.status !== 2) return
  try {
    await ElMessageBox.confirm(restoreConfirmMessage(row), t('systemUser.restoreConfirmTitle'), {
      type: 'warning',
      confirmButtonText: t('systemUser.restore'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }
  freezeRestoreUserId.value = row.id
  try {
    await rbacAdminApi.unfreezeUser(row.id)
    ElMessage.success(t('systemUser.restoreSuccess'))
    await load()
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : t('systemUser.restoreFailed')
    ElMessage.error(msg)
  } finally {
    freezeRestoreUserId.value = null
  }
}

async function handleImpersonate(row: AdminUserDto) {
  const label = row.realName || row.userName
  try {
    await ElMessageBox.confirm(
      `将以「${label}」（${row.userName}）身份登录，当前管理员会话将结束。是否继续？`,
      '模拟登录',
      { type: 'warning', confirmButtonText: '模拟登录', cancelButtonText: '取消' }
    )
  } catch {
    return
  }
  impersonateUserId.value = row.id
  try {
    await authStore.impersonate(row.id)
    ElMessage.success(`已切换为 ${row.userName}`)
    await router.replace({ name: 'Dashboard' })
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '模拟登录失败')
  } finally {
    impersonateUserId.value = null
  }
}

onMounted(async () => {
  await load()
  applySearch()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.user-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder { color: $text-muted; }
  &:focus { border-color: rgba(0, 212, 255, 0.4); }

  &--narrow {
    width: 160px;
  }
}

.status-select {
  width: 120px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.status-select--dept {
  width: 180px;
}

.status-select--role {
  width: 240px;
}

.status-select--status {
  width: 140px;
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 6px 16px rgba(0, 212, 255, 0.25);
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.user-list-page .table-wrapper {
  position: relative;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: $text-muted;
  p {
    margin: 12px 0 0;
    font-size: 14px;
  }
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--warning {
  color: $color-amber;
}

.op-more-item--danger {
  color: $color-red-brown;
}

.op-more-item--success {
  color: #22c55e;
}

:deep(.el-table__body tr.user-list-row--frozen > td.el-table__cell) {
  color: #f87171 !important;
  background-color: rgba(239, 68, 68, 0.07) !important;
}

:deep(.el-table__body tr.user-list-row--frozen:hover > td.el-table__cell) {
  background-color: rgba(239, 68, 68, 0.12) !important;
}

.reset-pwd-intro {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--crm-text-secondary, rgba(148, 163, 184, 0.95));
}

.reset-pwd-user-list {
  margin: 0 0 16px;
  padding-left: 1.25rem;
  max-height: 120px;
  overflow-y: auto;
  font-size: 13px;
  line-height: 1.5;
}
</style>
