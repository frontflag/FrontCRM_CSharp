<template>
  <div class="crm-system-list-page">
    <el-card class="crm-system-list-card" shadow="never">
      <div class="crm-system-list-toolbar">
        <h1 class="crm-system-list-title">{{ t('systemDepartment.title') }}</h1>
        <el-button type="primary" @click="router.push({ name: 'DepartmentCreate' })">{{ t('systemDepartment.create') }}</el-button>
      </div>

      <CrmDataTable
        ref="dataTableRef"
        v-loading="loading"
        column-layout-key="system-department-list-main"
        :columns="departmentTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="departments"
        row-key="id"
        @row-dblclick="goDetail"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? t('systemUser.statusEnabled') : t('systemUser.statusDisabled') }}
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
          {{ row.createUserName || row.createdBy || t('quoteList.na') }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('systemDepartment.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="goDetail(row)">{{ t('rfqItemList.actions.detail') }}</el-button>
              <el-button link type="primary" @click.stop="goEdit(row.id)">{{ t('systemUser.edit') }}</el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goDetail(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('rfqItemList.actions.detail') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="goEdit(row.id)">
                    <span class="op-more-item op-more-item--primary">{{ t('systemUser.edit') }}</span>
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
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { rbacAdminApi, type RbacDepartment } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const departments = ref<RbacDepartment[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

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
  { key: 'status', label: t('systemUser.colStatus'), prop: 'status', width: 90, align: 'center' },
  { key: 'departmentName', label: t('systemDepartment.columns.departmentName'), prop: 'departmentName', minWidth: 160, showOverflowTooltip: true },
  { key: 'parentName', label: t('systemDepartment.columns.parentName'), minWidth: 140, showOverflowTooltip: true },
  { key: 'level', label: t('systemDepartment.columns.level'), prop: 'level', width: 72 },
  { key: 'saleDataScope', label: t('systemDepartment.columns.saleDataScope'), minWidth: 120 },
  { key: 'purchaseDataScope', label: t('systemDepartment.columns.purchaseDataScope'), minWidth: 120 },
  { key: 'identityType', label: t('systemDepartment.columns.identityType'), minWidth: 100 },
  { key: 'createTime', label: t('systemUser.colCreateTime'), width: 160 },
  { key: 'createUser', label: t('systemUser.colCreateUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('systemDepartment.columns.actions'),
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
  if (!parentId) return t('quoteList.na')
  return nameById.value[parentId] || parentId
}

const scopeLabel = (v: number) => {
  const map: Record<number, string> = {
    0: t('systemDepartment.scope.all'),
    1: t('systemDepartment.scope.self'),
    2: t('systemDepartment.scope.department'),
    3: t('systemDepartment.scope.departmentAndChildren'),
    4: t('systemDepartment.scope.forbidden')
  }
  return map[v] ?? String(v)
}

const identityLabel = (v: number) => {
  const map: Record<number, string> = {
    0: t('systemDepartment.identity.none'),
    1: t('systemDepartment.identity.sales'),
    2: t('systemDepartment.identity.purchase'),
    3: t('systemDepartment.identity.purchaseOps'),
    4: t('systemDepartment.identity.business'),
    5: t('systemDepartment.identity.finance'),
    6: t('systemDepartment.identity.logistics')
  }
  return map[v] ?? String(v)
}

const load = async () => {
  loading.value = true
  try {
    departments.value = await rbacAdminApi.getDepartments()
  } catch (e: any) {
    ElMessage.error(e?.message || t('systemDepartment.loadFailed'))
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
