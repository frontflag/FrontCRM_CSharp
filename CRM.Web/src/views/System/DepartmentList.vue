<template>
  <div class="crm-biz-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 21V8l9-5 9 5v13" />
              <path d="M9 21v-8h6v8" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('systemDepartment.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('systemDepartment.count', { count: filteredDepartments.length }) }}</div>
      </div>
      <div v-if="canWrite" class="header-right">
        <button type="button" class="btn-primary" @click="router.push({ name: 'DepartmentCreate' })">
          {{ t('systemDepartment.create') }}
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
            v-model="searchFilters.nameKw"
            class="search-input"
            :placeholder="t('systemDepartment.columns.departmentName')"
            @keyup.enter="applySearch"
          />
        </div>
        <el-select
          v-model="searchFilters.statusFilter"
          class="status-select"
          clearable
          :placeholder="t('systemUser.allStatuses')"
          :teleported="false"
          @change="applySearch"
        >
          <el-option :label="t('systemUser.allStatuses')" value="all" />
          <el-option :label="t('systemUser.statusEnabled')" value="1" />
          <el-option :label="t('systemUser.statusDisabled')" value="0" />
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
        v-show="loading || pagedDepartments.length > 0"
        ref="dataTableRef"
        column-layout-key="system-department-list-main-v2"
        :columns="departmentTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="pagedDepartments"
        row-key="id"
        :row-class-name="() => 'table-row-pointer'"
        @row-dblclick="onRowDblclick"
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
          {{ scopeAccessLabel(row.saleDataScope, row.saleDataAccess) }}
        </template>
        <template #col-purchaseDataScope="{ row }">
          {{ scopeAccessLabel(row.purchaseDataScope, row.purchaseDataAccess) }}
        </template>
        <template #col-logisticsDataScope="{ row }">
          {{ scopeAccessLabel(row.logisticsDataScope ?? 0, row.logisticsDataAccess) }}
        </template>
        <template #col-financeDataScope="{ row }">
          {{ scopeAccessLabel(row.financeDataScope ?? 0, row.financeDataAccess) }}
        </template>
        <template #col-identityType="{ row }">
          {{ identityLabel(row.identityType) }}
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
          {{ row.createUserName || row.createdBy || t('quoteList.na') }}
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
              <el-button link type="primary" @click.stop="goDetail(row)">{{ t('rfqItemList.actions.detail') }}</el-button>
              <el-button v-if="canWrite" link type="primary" @click.stop="goEdit(row.id)">{{ t('systemUser.edit') }}</el-button>
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
                  <el-dropdown-item v-if="canWrite" @click.stop="goEdit(row.id)">
                    <span class="op-more-item op-more-item--primary">{{ t('systemUser.edit') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <div v-show="!loading && pagedDepartments.length === 0" class="empty-state">
        <p>{{ t('systemDepartment.empty') }}</p>
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
        :total="filteredDepartments.length"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { rbacAdminApi, type RbacDepartment } from '@/api/rbacAdmin'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { useAuthStore } from '@/stores/auth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const canWrite = computed(() => authStore.canAccessSystemPermission('system.org.departments.write'))

const loading = ref(false)
const departments = ref<RbacDepartment[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const page = ref(1)
const pageSize = ref(20)

const searchFilters = reactive({
  nameKw: '',
  statusFilter: 'all' as string
})
const appliedFilters = reactive({
  nameKw: '',
  statusFilter: 'all' as string
})

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function headerMin(label: string, extra?: { align?: 'left' | 'center' | 'right'; extra?: number }) {
  return estimateListColumnHeaderMinWidth(label, extra)
}

const departmentTableColumns = computed<CrmTableColumnDef[]>(() => {
  const status = t('systemUser.colStatus')
  const name = t('systemDepartment.columns.departmentName')
  const parent = t('systemDepartment.columns.parentName')
  const level = t('systemDepartment.columns.level')
  const sale = t('systemDepartment.columns.saleDataScope')
  const purchase = t('systemDepartment.columns.purchaseDataScope')
  const logistics = t('systemDepartment.columns.logisticsDataScope')
  const finance = t('systemDepartment.columns.financeDataScope')
  const identity = t('systemDepartment.columns.identityType')
  const createTime = t('systemUser.colCreateTime')
  const createUser = t('systemUser.colCreateUser')
  return [
    { key: 'status', label: status, prop: 'status', width: Math.max(90, headerMin(status, { align: 'center' })), align: 'center' },
    { key: 'departmentName', label: name, prop: 'departmentName', minWidth: Math.max(160, headerMin(name)), showOverflowTooltip: true },
    { key: 'parentName', label: parent, minWidth: Math.max(140, headerMin(parent)), showOverflowTooltip: true },
    { key: 'level', label: level, prop: 'level', width: Math.max(108, headerMin(level)) },
    { key: 'saleDataScope', label: sale, minWidth: Math.max(120, headerMin(sale)) },
    { key: 'purchaseDataScope', label: purchase, minWidth: Math.max(120, headerMin(purchase)) },
    { key: 'logisticsDataScope', label: logistics, minWidth: Math.max(120, headerMin(logistics)) },
    { key: 'financeDataScope', label: finance, minWidth: Math.max(120, headerMin(finance)) },
    { key: 'identityType', label: identity, minWidth: Math.max(100, headerMin(identity)) },
    { key: 'createTime', label: createTime, width: Math.max(160, headerMin(createTime)) },
    { key: 'createUser', label: createUser, width: Math.max(120, headerMin(createUser)), showOverflowTooltip: true },
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
      labelClassName: 'op-col',
      resizable: false
    }
  ]
})

function applySearch() {
  appliedFilters.nameKw = searchFilters.nameKw.trim()
  const sf = searchFilters.statusFilter?.trim()
  appliedFilters.statusFilter = sf && ['all', '0', '1'].includes(sf) ? sf : 'all'
  page.value = 1
}

function resetSearch() {
  searchFilters.nameKw = ''
  searchFilters.statusFilter = 'all'
  applySearch()
}

const filteredDepartments = computed(() => {
  const nameQ = appliedFilters.nameKw.toLowerCase()
  const statusKey = appliedFilters.statusFilter
  return departments.value.filter((d) => {
    if (statusKey && statusKey !== 'all') {
      const want = Number(statusKey)
      if (!Number.isNaN(want) && d.status !== want) return false
    }
    if (nameQ && !(d.departmentName || '').toLowerCase().includes(nameQ)) return false
    return true
  })
})

const pagedDepartments = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredDepartments.value.slice(start, start + pageSize.value)
})

watch(pageSize, () => {
  page.value = 1
})
watch(filteredDepartments, (list) => {
  const maxPage = Math.max(1, Math.ceil(list.length / pageSize.value) || 1)
  if (page.value > maxPage) page.value = maxPage
})

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

const scopeAccessLabel = (scope: number, access?: number) => {
  const base = scopeLabel(scope)
  if (scope === 4) return base
  const mode = access === 1 ? t('systemDepartment.access.readOnly') : t('systemDepartment.access.readWrite')
  return `${base} / ${mode}`
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
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemDepartment.loadFailed'))
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

function onRowDblclick(row: RbacDepartment, _column: unknown, event?: MouseEvent) {
  if (event?.ctrlKey && canWrite.value) {
    goEdit(row.id)
    return
  }
  goDetail(row)
}

onMounted(load)
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>
