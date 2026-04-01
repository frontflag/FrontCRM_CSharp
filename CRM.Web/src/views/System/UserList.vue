<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">{{ t('systemUser.title') }}</h1>
        <el-button type="primary" @click="router.push({ name: 'UserCreate' })">{{ t('systemUser.create') }}</el-button>
      </div>

      <CrmDataTable
        ref="dataTableRef"
        v-loading="loading"
        column-layout-key="system-user-list-main"
        :columns="userTableColumns"
        :show-column-settings="false"
        :data="users"
        @row-dblclick="onRowDblclick"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
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
              <el-button link type="danger" @click.stop="requestDelete(row.id)">{{ t('systemUser.delete') }}</el-button>
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
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { rbacAdminApi, type AdminUserDto } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { useAuthStore } from '@/stores/auth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const loading = ref(false)
const users = ref<AdminUserDto[]>([])
const impersonateUserId = ref<string | null>(null)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

/** 仅系统管理员（SYS_ADMIN）可见模拟登录 */
const canImpersonate = computed(() => authStore.user?.isSysAdmin === true)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = computed(() => (canImpersonate.value ? 320 : 240))
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH.value : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH.value : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const userTableColumns = computed<CrmTableColumnDef[]>(() => [
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

const formatCreateTime = (v?: string) => formatDisplayDateTime(v)

const load = async () => {
  loading.value = true
  try {
    users.value = await rbacAdminApi.getUsers()
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

const requestDelete = (id: string) => {
  // 便于排查“点击删除无反应”：确认点击事件是否触发
  console.info('[UserList] delete click', { id })
  void handleDelete(id)
}

const onRowCommand = (cmd: string, row: AdminUserDto) => {
  console.info('[UserList] row command', { cmd, id: row.id, userName: row.userName })
  if (cmd === 'edit') {
    goEdit(row.id)
    return
  }
  if (cmd === 'delete') {
    requestDelete(row.id)
    return
  }
  if (cmd === 'impersonate') {
    void handleImpersonate(row)
  }
}

const handleDelete = async (id: string) => {
  try {
    console.info('[UserList] open delete confirm', { id })
    await ElMessageBox.confirm(t('systemUser.deleteConfirmMessage'), t('systemUser.deleteConfirmTitle'), {
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

onMounted(load)
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

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>

