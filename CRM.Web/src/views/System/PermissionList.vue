<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">{{ t('systemPermission.title') }}</h1>
        <el-button type="primary" @click="router.push({ name: 'PermissionCreate' })">{{ t('systemPermission.create') }}</el-button>
      </div>

      <CrmDataTable
        ref="dataTableRef"
        v-loading="loading"
        column-layout-key="system-permission-list-main"
        :columns="permissionTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="permissions"
        @row-dblclick="onRowDblclick"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? t('systemUser.statusEnabled') : t('systemUser.statusDisabled') }}
          </el-tag>
        </template>
        <template #col-createTime="{ row }">
          {{ formatCreateTime(row.createTime || row.createdAt) }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || t('quoteList.na') }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('systemPermission.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="goEdit(row.id)">{{ t('systemUser.edit') }}</el-button>
              <el-button link type="danger" @click.stop="handleDelete(row.id)">{{ t('systemUser.delete') }}</el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goEdit(row.id)">
                    <span class="op-more-item op-more-item--primary">{{ t('systemUser.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handleDelete(row.id)">
                    <span class="op-more-item op-more-item--danger">{{ t('systemUser.delete') }}</span>
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { rbacAdminApi, type RbacPermission } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const permissions = ref<RbacPermission[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const formatCreateTime = (v?: string) => formatDisplayDateTime(v)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 240
const OP_COL_EXPANDED_MIN_WIDTH = 240
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const permissionTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'status', label: t('systemUser.colStatus'), prop: 'status', width: 90, align: 'center' },
  { key: 'permissionName', label: t('systemPermission.columns.permissionName'), prop: 'permissionName', minWidth: 200, showOverflowTooltip: true },
  { key: 'permissionType', label: t('systemPermission.columns.permissionType'), prop: 'permissionType', width: 110 },
  { key: 'resource', label: t('systemPermission.columns.resource'), prop: 'resource', minWidth: 200, showOverflowTooltip: true },
  { key: 'action', label: t('systemPermission.columns.action'), prop: 'action', minWidth: 140, showOverflowTooltip: true },
  { key: 'permissionCode', label: t('systemPermission.columns.permissionCode'), prop: 'permissionCode', minWidth: 180, showOverflowTooltip: true },
  { key: 'createTime', label: t('systemUser.colCreateTime'), width: 160 },
  { key: 'createUser', label: t('systemUser.colCreateUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('systemPermission.columns.actions'),
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

const load = async () => {
  loading.value = true
  try {
    permissions.value = await rbacAdminApi.getPermissions()
  } catch (e: any) {
    ElMessage.error(e?.message || t('systemPermission.loadFailed'))
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'PermissionEdit', params: { id } })
}

const onRowDblclick = (row: RbacPermission) => goEdit(row.id)

const handleDelete = async (id: string) => {
  try {
    await ElMessageBox.confirm(t('systemPermission.deleteConfirmMessage'), t('systemPermission.deleteConfirmTitle'), {
      type: 'warning',
      confirmButtonText: t('systemUser.delete'),
      cancelButtonText: t('common.cancel')
    })
    await rbacAdminApi.deletePermission(id)
    ElMessage.success(t('systemPermission.deleteSuccess'))
    await load()
  } catch {
    // cancel
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/system-list-page.scss';

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
</style>

