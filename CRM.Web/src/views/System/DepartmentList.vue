<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">部门管理</h1>
        <el-button type="primary" @click="router.push({ name: 'DepartmentCreate' })">新增部门</el-button>
      </div>

      <CrmDataTable
        ref="dataTableRef"
        v-loading="loading"
        column-layout-key="system-department-list-main"
        :columns="departmentTableColumns"
        :show-column-settings="false"
        :data="departments"
        row-key="id"
        @row-dblclick="goDetail"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? '启用' : '禁用' }}
          </el-tag>
        </template>
        <template #col-parentName="{ row }">
          {{ parentLabel(row.parentId) }}
        </template>
        <template #col-saleDataScope="{ row }">
          {{ scopeLabel(row.saleDataScope) }}
        </template>
        <template #col-purchaseDataScope="{ row }">
          {{ scopeLabel(row.purchaseDataScope) }}
        </template>
        <template #col-identityType="{ row }">
          {{ identityLabel(row.identityType) }}
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
              <el-button link type="primary" @click.stop="goDetail(row)">详情</el-button>
              <el-button link type="primary" @click.stop="goEdit(row.id)">编辑</el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goDetail(row)">
                    <span class="op-more-item op-more-item--primary">详情</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="goEdit(row.id)">
                    <span class="op-more-item op-more-item--primary">编辑</span>
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
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { rbacAdminApi, type RbacDepartment } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const loading = ref(false)
const departments = ref<RbacDepartment[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 148
const OP_COL_EXPANDED_MIN_WIDTH = 148
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const departmentTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'status', label: '状态', prop: 'status', width: 90, align: 'center' },
  { key: 'departmentName', label: '部门名称', prop: 'departmentName', minWidth: 160, showOverflowTooltip: true },
  { key: 'parentName', label: '上级部门', minWidth: 140, showOverflowTooltip: true },
  { key: 'level', label: '层级', prop: 'level', width: 72 },
  { key: 'saleDataScope', label: '销售数据范围', minWidth: 120 },
  { key: 'purchaseDataScope', label: '采购数据范围', minWidth: 120 },
  { key: 'identityType', label: '业务身份', minWidth: 100 },
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

const formatCreateTime = (v?: string) => formatDisplayDateTime(v)

const nameById = computed(() => {
  const m: Record<string, string> = {}
  for (const d of departments.value) {
    m[d.id] = d.departmentName
  }
  return m
})

const parentLabel = (parentId?: string | null) => {
  if (!parentId) return '—'
  return nameById.value[parentId] || parentId
}

const scopeLabel = (v: number) => {
  const map: Record<number, string> = {
    0: '全部',
    1: '自己',
    2: '本部门',
    3: '本部门及下级',
    4: '禁止'
  }
  return map[v] ?? String(v)
}

const identityLabel = (v: number) => {
  const map: Record<number, string> = {
    0: '无',
    1: '销售',
    2: '采购',
    3: '采购运营',
    4: '商务',
    5: '财务',
    6: '物流'
  }
  return map[v] ?? String(v)
}

const load = async () => {
  loading.value = true
  try {
    departments.value = await rbacAdminApi.getDepartments()
  } catch (e: any) {
    ElMessage.error(e?.message || '加载部门列表失败')
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'DepartmentEdit', params: { id } })
}

const goDetail = (row: RbacDepartment) => {
  router.push({ name: 'DepartmentDetail', params: { id: row.id } })
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
