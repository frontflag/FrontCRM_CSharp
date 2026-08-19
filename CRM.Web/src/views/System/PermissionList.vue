<template>
  <div class="crm-biz-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 11-7.778 7.778 5.5 5.5 0 017.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('systemPermission.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('systemPermission.count', { count: filteredPermissions.length }) }}</div>
      </div>
      <div v-if="canWrite" class="header-right">
        <button type="button" class="btn-primary" @click="router.push({ name: 'PermissionCreate' })">
          {{ t('systemPermission.create') }}
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
            :placeholder="t('systemPermission.columns.permissionName')"
            @keyup.enter="applySearch"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchFilters.codeKw"
            class="search-input"
            :placeholder="t('systemPermission.columns.permissionCode')"
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
        v-show="loading || pagedPermissions.length > 0"
        ref="dataTableRef"
        column-layout-key="system-permission-list-main-v2"
        :columns="permissionTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="pagedPermissions"
        row-key="id"
        :row-class-name="() => 'table-row-pointer'"
        @row-dblclick="onRowDblclick"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? t('systemUser.statusEnabled') : t('systemUser.statusDisabled') }}
          </el-tag>
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
              <el-button v-if="canWrite" link type="primary" @click.stop="goEdit(row.id)">{{ t('systemUser.edit') }}</el-button>
              <el-button v-if="canWrite" link type="danger" @click.stop="handleDelete(row.id)">{{ t('systemUser.delete') }}</el-button>
            </div>
            <el-dropdown v-else-if="canWrite" trigger="click" placement="bottom-end">
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

      <div v-show="!loading && pagedPermissions.length === 0" class="empty-state">
        <p>{{ t('systemPermission.empty') }}</p>
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
        :total="filteredPermissions.length"
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
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { rbacAdminApi, type RbacPermission } from '@/api/rbacAdmin'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const canWrite = computed(() => authStore.canAccessSystemPermission('system.rbac.permissions.write'))

const loading = ref(false)
const permissions = ref<RbacPermission[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const page = ref(1)
const pageSize = ref(20)

const searchFilters = reactive({
  nameKw: '',
  codeKw: '',
  statusFilter: 'all' as string
})
const appliedFilters = reactive({
  nameKw: '',
  codeKw: '',
  statusFilter: 'all' as string
})

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function headerMin(label: string, extra?: { align?: 'left' | 'center' | 'right'; extra?: number }) {
  return estimateListColumnHeaderMinWidth(label, extra)
}

const permissionTableColumns = computed<CrmTableColumnDef[]>(() => {
  const status = t('systemUser.colStatus')
  const name = t('systemPermission.columns.permissionName')
  const type = t('systemPermission.columns.permissionType')
  const resource = t('systemPermission.columns.resource')
  const action = t('systemPermission.columns.action')
  const code = t('systemPermission.columns.permissionCode')
  const createTime = t('systemUser.colCreateTime')
  const createUser = t('systemUser.colCreateUser')
  return [
    { key: 'status', label: status, prop: 'status', width: Math.max(90, headerMin(status, { align: 'center' })), align: 'center' },
    {
      key: 'permissionName',
      label: name,
      prop: 'permissionName',
      minWidth: Math.max(200, headerMin(name)),
      showOverflowTooltip: true,
      sortable: true
    },
    { key: 'permissionType', label: type, prop: 'permissionType', width: Math.max(110, headerMin(type)) },
    { key: 'resource', label: resource, prop: 'resource', minWidth: Math.max(200, headerMin(resource)), showOverflowTooltip: true },
    { key: 'action', label: action, prop: 'action', minWidth: Math.max(140, headerMin(action)), showOverflowTooltip: true },
    {
      key: 'permissionCode',
      label: code,
      prop: 'permissionCode',
      minWidth: Math.max(180, headerMin(code)),
      showOverflowTooltip: true,
      sortable: true
    },
    { key: 'createTime', label: createTime, width: Math.max(160, headerMin(createTime)) },
    { key: 'createUser', label: createUser, width: Math.max(120, headerMin(createUser)), showOverflowTooltip: true },
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
      labelClassName: 'op-col',
      resizable: false
    }
  ]
})

function applySearch() {
  appliedFilters.nameKw = searchFilters.nameKw.trim()
  appliedFilters.codeKw = searchFilters.codeKw.trim()
  const sf = searchFilters.statusFilter?.trim()
  appliedFilters.statusFilter = sf && ['all', '0', '1'].includes(sf) ? sf : 'all'
  page.value = 1
}

function resetSearch() {
  searchFilters.nameKw = ''
  searchFilters.codeKw = ''
  searchFilters.statusFilter = 'all'
  applySearch()
}

const filteredPermissions = computed(() => {
  const nameQ = appliedFilters.nameKw.toLowerCase()
  const codeQ = appliedFilters.codeKw.toLowerCase()
  const statusKey = appliedFilters.statusFilter
  return permissions.value.filter((p) => {
    if (statusKey && statusKey !== 'all') {
      const want = Number(statusKey)
      if (!Number.isNaN(want) && p.status !== want) return false
    }
    if (nameQ && !(p.permissionName ?? '').toLowerCase().includes(nameQ)) return false
    if (codeQ && !(p.permissionCode ?? '').toLowerCase().includes(codeQ)) return false
    return true
  })
})

const pagedPermissions = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredPermissions.value.slice(start, start + pageSize.value)
})

watch(pageSize, () => {
  page.value = 1
})
watch(filteredPermissions, (list) => {
  const maxPage = Math.max(1, Math.ceil(list.length / pageSize.value) || 1)
  if (page.value > maxPage) page.value = maxPage
})

const load = async () => {
  loading.value = true
  try {
    permissions.value = await rbacAdminApi.getPermissions()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemPermission.loadFailed'))
  } finally {
    loading.value = false
  }
}

const goEdit = (id: string) => {
  router.push({ name: 'PermissionEdit', params: { id } })
}

const onRowDblclick = (row: RbacPermission) => {
  if (!canWrite.value) return
  goEdit(row.id)
}

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

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>
