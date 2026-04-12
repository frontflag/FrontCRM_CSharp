<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">{{ t('systemUser.title') }}</h1>
        <div class="crm-system-list-toolbar-actions">
          <el-tooltip :content="t('systemUser.resetPasswordHint')" placement="bottom">
            <el-button :disabled="selectedUsers.length === 0" @click="openResetDialogFromSelection">
              {{ t('systemUser.resetPassword') }}
            </el-button>
          </el-tooltip>
          <el-button type="primary" @click="router.push({ name: 'UserCreate' })">{{ t('systemUser.create') }}</el-button>
        </div>
      </div>

      <div class="user-list-search-bar">
        <span class="user-list-search-label">{{ t('systemUser.searchDepartment') }}</span>
        <el-select
          v-model="searchFilters.departmentId"
          class="user-list-search-select"
          clearable
          filterable
          :placeholder="t('systemUser.allDepartments')"
          :teleported="false"
        >
          <el-option
            v-for="d in departmentOptions"
            :key="d.id"
            :label="d.departmentName"
            :value="d.id"
          />
        </el-select>
        <span class="user-list-search-label">{{ t('systemUser.searchRole') }}</span>
        <el-select
          v-model="searchFilters.roleId"
          class="user-list-search-select user-list-search-select--role"
          clearable
          filterable
          :placeholder="t('systemUser.allRoles')"
          :teleported="false"
        >
          <el-option
            v-for="r in roleOptions"
            :key="r.id"
            :label="`${r.roleName} (${r.roleCode})`"
            :value="r.id"
          />
        </el-select>
        <span class="user-list-search-label">{{ t('systemUser.searchUserName') }}</span>
        <el-input
          v-model="searchFilters.userNameKw"
          class="user-list-search-input"
          clearable
          :placeholder="t('systemUser.userNameSearchPlaceholder')"
          @keyup.enter="applySearch"
        />
        <span class="user-list-search-label">{{ t('systemUser.searchRealName') }}</span>
        <el-input
          v-model="searchFilters.realNameKw"
          class="user-list-search-input"
          clearable
          :placeholder="t('systemUser.realNameSearchPlaceholder')"
          @keyup.enter="applySearch"
        />
        <span class="user-list-search-label">{{ t('systemUser.searchFreezeFilter') }}</span>
        <el-select
          v-model="searchFilters.statusFilter"
          class="user-list-search-select user-list-search-select--status"
          clearable
          :placeholder="t('systemUser.freezeFilterAll')"
          :teleported="false"
        >
          <el-option :label="t('systemUser.freezeFilterAll')" value="all" />
          <el-option :label="t('systemUser.freezeFilterNormal')" value="1" />
          <el-option :label="t('systemUser.freezeFilterDisabled')" value="0" />
          <el-option :label="t('systemUser.freezeFilterFrozen')" value="2" />
        </el-select>
        <el-button type="primary" @click="applySearch">{{ t('systemUser.searchQuery') }}</el-button>
        <el-button @click="resetSearch">{{ t('systemUser.searchReset') }}</el-button>
      </div>

      <CrmDataTable
        ref="dataTableRef"
        v-loading="loading"
        column-layout-key="system-user-list-main-v2"
        :columns="userTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="displayUsers"
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
        <template #col-roleCodes="{ row }">
          <span>{{ row.roleCodes?.join(', ') || '-' }}</span>
        </template>
        <template #col-primaryDepartmentName="{ row }">
          <span>{{ row.primaryDepartmentName || '-' }}</span>
        </template>
        <template #col-createTime="{ row }">
          {{ formatCreateTime(row.createTime || row.createdAt) }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || '-' }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('systemUser.action') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
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
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
      </div>
    </el-card>

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
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { rbacAdminApi, type AdminUserDto, type RbacDepartment, type RbacRole } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { useAuthStore } from '@/stores/auth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const loading = ref(false)
const allUsers = ref<AdminUserDto[]>([])
const departmentOptions = ref<RbacDepartment[]>([])
const roleOptions = ref<RbacRole[]>([])

/** 表单上的条件（点「查询」后写入 appliedFilters） */
const searchFilters = reactive({
  departmentId: '' as string,
  roleId: '' as string,
  userNameKw: '',
  realNameKw: '',
  /** all | 0 | 1 | 2 */
  statusFilter: 'all' as string
})

/** 当前生效的筛选（与表格联动） */
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

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
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

const displayUsers = computed(() => {
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

function stringEq(a: string, b: string) {
  return (a || '').trim().toLowerCase() === (b || '').trim().toLowerCase()
}

const userTableColumns = computed<CrmTableColumnDef[]>(() => [
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
  { key: 'status', label: t('systemUser.colStatus'), prop: 'status', width: 90, align: 'center' },
  { key: 'userName', label: t('systemUser.colUserName'), prop: 'userName', minWidth: 160, showOverflowTooltip: true },
  { key: 'realName', label: t('systemUser.colRealName'), prop: 'realName', minWidth: 160, showOverflowTooltip: true },
  { key: 'email', label: t('systemUser.colEmail'), prop: 'email', minWidth: 200, showOverflowTooltip: true },
  { key: 'mobile', label: t('systemUser.colMobile'), prop: 'mobile', width: 140 },
  { key: 'roleCodes', label: t('systemUser.colRoleCodes'), minWidth: 220, showOverflowTooltip: true },
  { key: 'primaryDepartmentName', label: t('systemUser.colPrimaryDept'), minWidth: 220, showOverflowTooltip: true },
  { key: 'createTime', label: t('systemUser.colCreateTime'), width: 160 },
  { key: 'createUser', label: t('systemUser.colCreateUser'), width: 120, showOverflowTooltip: true },
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
    labelClassName: 'op-col'
  }
])

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
  return row.status === 2 ? 'user-list-row--frozen' : ''
}

const formatCreateTime = (v?: string) => formatDisplayDateTime(v)

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
    roleOptions.value = [...roles].filter((r) => r.status === 1).sort((a, b) => a.roleName.localeCompare(b.roleName))
  } catch (e: any) {
    ElMessage.error(e?.message || t('systemUser.loadFailed'))
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'UserEdit', params: { id } })
}

const onRowDblclick = (row: AdminUserDto) => goEdit(row.id)

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
    // 用户点击取消：静默返回；其它异常明确提示，避免“删除后还在”却无反馈
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
  } catch (e: any) {
    ElMessage.error(e?.message || '模拟登录失败')
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
@import '@/assets/styles/system-list-page.scss';
@import '@/assets/styles/variables.scss';

.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
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

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
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

.crm-system-list-toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.user-list-search-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px 12px;
  margin-bottom: 16px;
  padding: 12px 14px;
  border-radius: 8px;
  background: rgba(0, 212, 255, 0.06);
  border: 1px solid rgba(0, 212, 255, 0.12);
}

.user-list-search-label {
  font-size: 13px;
  color: rgba(148, 163, 184, 0.95);
  white-space: nowrap;
}

.user-list-search-select {
  width: 200px;
}

.user-list-search-select--role {
  min-width: 220px;
  width: 240px;
}

.user-list-search-select--status {
  width: 140px;
}

.user-list-search-input {
  width: 160px;
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

