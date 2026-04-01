<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">权限管理</h1>
        <el-button type="primary" @click="router.push({ name: 'PermissionCreate' })">新增权限</el-button>
      </div>

      <CrmDataTable
        ref="dataTableRef"
        v-loading="loading"
        column-layout-key="system-permission-list-main"
        :columns="permissionTableColumns"
        :show-column-settings="false"
        :data="permissions"
        @row-dblclick="onRowDblclick"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? '启用' : '禁用' }}
          </el-tag>
        </template>
        <template #col-createTime="{ row }">
          {{ formatCreateTime(row.createTime || row.createdAt) }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || '-' }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">操作</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="goEdit(row.id)">编辑</el-button>
              <el-button link type="danger" @click.stop="handleDelete(row.id)">删除</el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goEdit(row.id)">
                    <span class="op-more-item op-more-item--primary">编辑</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handleDelete(row.id)">
                    <span class="op-more-item op-more-item--danger">删除</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip content="列设置" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
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
import { rbacAdminApi, type RbacPermission } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()

const loading = ref(false)
const permissions = ref<RbacPermission[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
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
  { key: 'status', label: '状态', prop: 'status', width: 90, align: 'center' },
  { key: 'permissionCode', label: '权限编码', prop: 'permissionCode', minWidth: 180, showOverflowTooltip: true },
  { key: 'permissionName', label: '权限名称', prop: 'permissionName', minWidth: 200, showOverflowTooltip: true },
  { key: 'permissionType', label: '类型', prop: 'permissionType', width: 110 },
  { key: 'resource', label: '资源', prop: 'resource', minWidth: 200, showOverflowTooltip: true },
  { key: 'action', label: '动作', prop: 'action', minWidth: 140, showOverflowTooltip: true },
  { key: 'createTime', label: '创建时间', width: 160 },
  { key: 'createUser', label: '创建人', width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: '操作',
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
    ElMessage.error(e?.message || '加载权限列表失败')
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
    await ElMessageBox.confirm('确定删除该权限吗？', '删除确认', {
      type: 'warning',
      confirmButtonText: '删除',
      cancelButtonText: '取消'
    })
    await rbacAdminApi.deletePermission(id)
    ElMessage.success('删除成功')
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

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>

