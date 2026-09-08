<template>
  <div class="user-level-list-page">
    <div class="page-header">
      <div class="header-left">
        <h2 class="page-title">{{ t('systemUserLevel.title') }}</h2>
        <p class="page-sub">{{ t('systemUserLevel.pageSubtitle') }}</p>
      </div>
    </div>

    <div class="settings-body">
      <div class="settings-nav" aria-label="user-level-nav">
        <div
          class="nav-item"
          :class="{ active: activeNav === 'catalog' }"
          @click="activeNav = 'catalog'"
        >
          <el-icon class="nav-icon"><CollectionTag /></el-icon>
          <span>{{ t('systemUserLevel.navCatalog') }}</span>
        </div>
        <div
          class="nav-item"
          :class="{ active: activeNav === 'users' }"
          @click="activeNav = 'users'"
        >
          <el-icon class="nav-icon"><User /></el-icon>
          <span>{{ t('systemUserLevel.navUsers') }}</span>
        </div>
      </div>

      <div class="settings-content">
        <div v-show="activeNav === 'catalog'" class="form-section">
          <div class="section-head">
            <div class="section-head__left">
              <div class="section-title"><span class="title-bar"></span>{{ t('systemUserLevel.catalogTitle') }}</div>
              <p class="section-hint">{{ t('systemUserLevel.catalogHint') }}</p>
            </div>
          </div>
          <div class="table-wrapper" v-loading="catalogLoading">
            <CrmDataTable
              v-show="catalogLoading || catalogRows.length > 0"
              ref="catalogTableRef"
              column-layout-key="system-user-level-catalog-v1"
              :columns="catalogColumns"
              :show-column-settings="false"
              :density-toggle-anchor-el="catalogDensityAnchorEl"
              :data="catalogRows"
              row-key="id"
              @row-dblclick="onCatalogDblclick"
            >
              <template #col-description="{ row }">{{ row.description || '—' }}</template>
              <template #col-actions-header>
                <div class="list-op-col-header--icon-only">
                  <button
                    type="button"
                    class="op-col-toggle-btn list-op-col-toggle"
                    :aria-label="catalogOpExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                    @click.stop="catalogOpExpanded = !catalogOpExpanded"
                  >
                    {{ catalogOpExpanded ? '>' : '<' }}
                  </button>
                </div>
              </template>
              <template #col-actions="{ row }">
                <div @click.stop @dblclick.stop>
                  <div v-if="catalogOpExpanded" class="action-btns">
                    <el-button v-if="canWrite" link type="primary" @click.stop="openCatalogEdit(row)">
                      {{ t('systemUserLevel.editDescription') }}
                    </el-button>
                  </div>
                  <el-dropdown v-else-if="canWrite" trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="openCatalogEdit(row)">
                          <span class="op-more-item op-more-item--primary">{{ t('systemUserLevel.editDescription') }}</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </CrmDataTable>
            <div v-show="!catalogLoading && catalogRows.length === 0" class="empty-state">
              <p>{{ t('systemUserLevel.catalogEmpty') }}</p>
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
                  @click="catalogTableRef?.openColumnSettings?.()"
                >
                  <el-icon><Setting /></el-icon>
                </el-button>
              </el-tooltip>
              <span ref="catalogDensityAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
              <div class="list-footer-spacer" aria-hidden="true" />
            </div>
          </div>
        </div>

        <div v-show="activeNav === 'users'" class="users-panel">
          <div class="users-panel-head">
            <div class="count-badge">{{ t('systemUserLevel.count', { count: filteredUsers.length }) }}</div>
          </div>
    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchFilters.realNameKw"
            class="search-input search-input--narrow"
            :placeholder="t('systemUserLevel.colRealName')"
            @keyup.enter="applySearch"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchFilters.userNameKw"
            class="search-input search-input--narrow"
            :placeholder="t('systemUserLevel.colUserName')"
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
          v-model="searchFilters.statusFilter"
          class="status-select status-select--status"
          clearable
          :placeholder="t('systemUser.colStatus')"
          :teleported="false"
          @change="applySearch"
        >
          <el-option :label="t('systemUser.allStatuses')" value="all" />
          <el-option :label="t('systemUser.statusEnabled')" value="1" />
          <el-option :label="t('systemUser.statusDisabled')" value="0" />
          <el-option :label="t('systemUser.statusFrozen')" value="2" />
        </el-select>
        <el-select
          v-model="searchFilters.level"
          class="status-select"
          clearable
          :placeholder="t('systemUserLevel.allLevels')"
          :teleported="false"
          @change="applySearch"
        >
          <el-option
            v-for="n in USER_LEVEL_OPTIONS"
            :key="n"
            :label="levelOptionLabel(n)"
            :value="n"
          />
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
        column-layout-key="system-user-level-list-main-v3"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="pagedUsers"
        row-key="id"
        :row-class-name="rowClassName"
        @row-click="onRowClick"
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
        <template #col-levelChangedAt="{ row }">
          <template v-for="p in [row.levelChangedAt ? formatDisplayDateTime2DigitYearParts(row.levelChangedAt) : null]" :key="'lv-' + row.id">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-levelRemark="{ row }">{{ row.levelRemark || '—' }}</template>
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
              <el-button
                v-if="canWrite"
                link
                type="primary"
                @click.stop="openEdit(row)"
              >
                {{ t('systemUserLevel.setLevel') }}
              </el-button>
            </div>
            <el-dropdown v-else-if="canWrite" trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="openEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('systemUserLevel.setLevel') }}</span>
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
        <p>{{ t('systemUserLevel.empty') }}</p>
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
        </div>
      </div>
    </div>

    <el-dialog
      v-model="catalogEditVisible"
      :title="t('systemUserLevel.editDescriptionTitle')"
      width="480px"
      destroy-on-close
      @closed="catalogEditRow = null"
    >
      <el-form v-if="catalogEditRow" label-width="96px">
        <el-form-item :label="t('systemUserLevel.colLevel')">
          <span>{{ catalogEditRow.userLevel }}</span>
        </el-form-item>
        <el-form-item :label="t('systemUserLevel.colDescription')">
          <el-input
            v-model="catalogEditForm.description"
            type="textarea"
            :rows="3"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="catalogEditVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="catalogSaving" @click="submitCatalogEdit">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="editVisible"
      :title="t('systemUserLevel.editTitle')"
      width="480px"
      destroy-on-close
      @closed="editRow = null"
    >
      <el-form v-if="editRow" label-width="96px">
        <el-form-item :label="t('systemUserLevel.colRealName')">
          <span>{{ editRow.realName || '—' }}</span>
        </el-form-item>
        <el-form-item :label="t('systemUserLevel.colUserName')">
          <span>{{ editRow.userName }}</span>
        </el-form-item>
        <el-form-item :label="t('systemUserLevel.colLevel')">
          <el-select v-model="editForm.level" class="edit-level-select">
            <el-option
            v-for="n in USER_LEVEL_OPTIONS"
            :key="n"
            :label="levelOptionLabel(n)"
            :value="n"
          />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('systemUserLevel.colRemark')">
          <el-input
            v-model="editForm.remark"
            type="textarea"
            :rows="3"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="submitEdit">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { CollectionTag, Setting, User } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { rbacAdminApi, type AdminUserDto, type RbacDepartment } from '@/api/rbacAdmin'
import { USER_LEVEL_OPTIONS, userLevelApi, type UserLevelDefinition } from '@/api/userLevel'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { useAuthStore } from '@/stores/auth'
import { useUserLevelLogStore } from '@/stores/userLevelLog'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t } = useI18n()
const route = useRoute()
const authStore = useAuthStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const logStore = useUserLevelLogStore()

const canWrite = computed(() => authStore.canAccessSystemPermission('system.org.users.write'))

const activeNav = ref<'catalog' | 'users'>('users')
const catalogLoading = ref(false)
const catalogSaving = ref(false)
const catalogRows = ref<UserLevelDefinition[]>([])
const catalogTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const catalogDensityAnchorEl = ref<HTMLElement | null>(null)
const catalogOpExpanded = ref(false)
const catalogEditVisible = ref(false)
const catalogEditRow = ref<UserLevelDefinition | null>(null)
const catalogEditForm = reactive({ description: '' })
const catalogOpWidth = computed(() => (catalogOpExpanded.value ? 120 : 43))

const loading = ref(false)
const saving = ref(false)
const allUsers = ref<AdminUserDto[]>([])
const departmentOptions = ref<RbacDepartment[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const page = ref(1)
const pageSize = ref(20)

const searchFilters = reactive({
  realNameKw: '',
  userNameKw: '',
  departmentId: '' as string,
  statusFilter: '1' as string,
  level: null as number | null
})
const appliedFilters = reactive({
  realNameKw: '',
  userNameKw: '',
  departmentId: '',
  statusFilter: '1' as string,
  level: null as number | null
})

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 120
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function headerMin(label: string, extra?: { align?: 'left' | 'center' | 'right'; extra?: number }) {
  return estimateListColumnHeaderMinWidth(label, extra)
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const realName = t('systemUserLevel.colRealName')
  const userName = t('systemUserLevel.colUserName')
  const dept = t('systemUserLevel.colDept')
  const status = t('systemUserLevel.colStatus')
  const level = t('systemUserLevel.colLevel')
  const changedAt = t('systemUserLevel.colChangedAt')
  const remark = t('systemUserLevel.colRemark')
  return [
    { key: 'realName', label: realName, prop: 'realName', minWidth: Math.max(120, headerMin(realName)), showOverflowTooltip: true },
    { key: 'userName', label: userName, prop: 'userName', minWidth: Math.max(140, headerMin(userName)), showOverflowTooltip: true },
    { key: 'primaryDepartmentName', label: dept, prop: 'primaryDepartmentName', minWidth: Math.max(180, headerMin(dept)), showOverflowTooltip: true },
    { key: 'status', label: status, width: Math.max(96, headerMin(status, { align: 'center' })), align: 'center' },
    { key: 'level', label: level, width: Math.max(80, headerMin(level, { align: 'center' })), align: 'center' },
    { key: 'levelChangedAt', label: changedAt, width: Math.max(160, headerMin(changedAt)) },
    { key: 'levelRemark', label: remark, minWidth: Math.max(160, headerMin(remark)), showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('systemUser.action'),
      width: opColWidth.value,
      minWidth: opColWidth.value,
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

const catalogColumns = computed<CrmTableColumnDef[]>(() => {
  const level = t('systemUserLevel.colLevel')
  const desc = t('systemUserLevel.colDescription')
  return [
    { key: 'userLevel', label: level, prop: 'userLevel', width: Math.max(88, headerMin(level, { align: 'center' })), align: 'center' },
    { key: 'description', label: desc, minWidth: Math.max(240, headerMin(desc)), showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('systemUser.action'),
      width: catalogOpWidth.value,
      minWidth: catalogOpWidth.value,
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

function levelOptionLabel(n: number) {
  const desc = catalogRows.value.find((r) => r.userLevel === n)?.description?.trim()
  return desc ? `${n} — ${desc}` : String(n)
}

function openCatalogEdit(row: UserLevelDefinition) {
  if (!canWrite.value) return
  catalogEditRow.value = row
  catalogEditForm.description = row.description || ''
  catalogEditVisible.value = true
}

function onCatalogDblclick(row: UserLevelDefinition) {
  if (!canWrite.value) return
  openCatalogEdit(row)
}

async function submitCatalogEdit() {
  if (!catalogEditRow.value) return
  catalogSaving.value = true
  try {
    const updated = await userLevelApi.updateDefinition(catalogEditRow.value.userLevel, catalogEditForm.description)
    const idx = catalogRows.value.findIndex((r) => r.id === updated.id)
    if (idx >= 0) catalogRows.value[idx] = updated
    else await loadCatalog()
    ElMessage.success(t('systemUserLevel.descriptionSaved'))
    catalogEditVisible.value = false
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemUserLevel.saveFailed'))
  } finally {
    catalogSaving.value = false
  }
}

async function loadCatalog() {
  catalogLoading.value = true
  try {
    catalogRows.value = await userLevelApi.listDefinitions()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemUser.loadFailed'))
  } finally {
    catalogLoading.value = false
  }
}

function applySearch() {
  appliedFilters.realNameKw = searchFilters.realNameKw.trim()
  appliedFilters.userNameKw = searchFilters.userNameKw.trim()
  appliedFilters.departmentId = searchFilters.departmentId?.trim() ?? ''
  const sf = searchFilters.statusFilter?.trim()
  appliedFilters.statusFilter = sf && ['all', '0', '1', '2'].includes(sf) ? sf : 'all'
  appliedFilters.level = searchFilters.level
  page.value = 1
}

function resetSearch() {
  searchFilters.realNameKw = ''
  searchFilters.userNameKw = ''
  searchFilters.departmentId = ''
  searchFilters.statusFilter = '1'
  searchFilters.level = null
  applySearch()
}

const filteredUsers = computed(() => {
  const rname = appliedFilters.realNameKw.toLowerCase()
  const uname = appliedFilters.userNameKw.toLowerCase()
  const deptId = appliedFilters.departmentId
  const statusKey = appliedFilters.statusFilter
  const lv = appliedFilters.level
  return allUsers.value.filter((u) => {
    if (rname && !(u.realName || '').toLowerCase().includes(rname)) return false
    if (uname && !(u.userName || '').toLowerCase().includes(uname)) return false
    if (deptId) {
      const ids = u.departmentIds || []
      if (!ids.some((id) => id === deptId)) return false
    }
    if (statusKey && statusKey !== 'all') {
      const want = Number(statusKey)
      if (!Number.isNaN(want) && u.status !== want) return false
    }
    if (lv != null && (u.level ?? 1) !== lv) return false
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

function rowClassName({ row }: { row: AdminUserDto }) {
  const selected = logStore.row?.id === row.id ? 'so-item-row--active' : ''
  return ['table-row-pointer', selected].filter(Boolean).join(' ')
}

const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'UserLevelList',
  hasSelectedRow: () => !!logStore.row,
  setRowOnly: (row) => logStore.setRowOnly(row as unknown as AdminUserDto),
  selectRow: (row) => logStore.selectRow(row as unknown as AdminUserDto, t('systemUserLevel.loadHistoryFailed')),
  loadSelected: () => {
    void logStore.loadHistory(t('systemUserLevel.loadHistoryFailed'))
  },
  dataTabIds: ['r-user-level-log']
})

async function onRowClick(row: AdminUserDto) {
  await onOpsPanelRowClick(row as unknown as Record<string, unknown>)
}

function onRowDblclick(row: AdminUserDto) {
  if (!canWrite.value) return
  openEdit(row)
}

const editVisible = ref(false)
const editRow = ref<AdminUserDto | null>(null)
const editForm = reactive({ level: 1, remark: '' })

function openEdit(row: AdminUserDto) {
  if (!canWrite.value) return
  editRow.value = row
  editForm.level = row.level ?? 1
  editForm.remark = row.levelRemark || ''
  editVisible.value = true
}

async function submitEdit() {
  if (!editRow.value) return
  saving.value = true
  try {
    const result = await userLevelApi.change(editRow.value.id, {
      level: editForm.level,
      remark: editForm.remark
    })
    ElMessage.success(result.levelChanged ? t('systemUserLevel.levelUpdated') : t('systemUserLevel.remarkSaved'))
    editVisible.value = false
    await load()
    const refreshed = allUsers.value.find((u) => u.id === result.userId)
    if (refreshed && logStore.row?.id === result.userId) {
      await logStore.selectRow(refreshed, t('systemUserLevel.loadHistoryFailed'))
    }
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemUserLevel.saveFailed'))
  } finally {
    saving.value = false
  }
}

async function load() {
  loading.value = true
  try {
    const [userList, depts] = await Promise.all([
      rbacAdminApi.getUsers(),
      rbacAdminApi.getDepartments().catch(() => [] as RbacDepartment[]),
      loadCatalog()
    ])
    allUsers.value = userList
    departmentOptions.value = [...depts]
      .filter((d) => d.status === 1)
      .sort((a, b) => a.departmentName.localeCompare(b.departmentName))
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('systemUser.loadFailed'))
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.user-level-list-page {
  padding: 20px;
  min-height: 320px;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  margin-bottom: 20px;
}

.header-left {
  display: block;
}

.page-title {
  margin: 0 0 6px;
  font-size: 18px;
  font-weight: 600;
  color: $text-primary;
}

.page-sub {
  margin: 0;
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
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
  margin-bottom: 16px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.title-bar {
  width: 3px;
  height: 14px;
  border-radius: 2px;
  background: $cyan-primary;
}

.section-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: $text-muted;
}

.users-panel-head {
  display: flex;
  align-items: center;
  margin-bottom: 12px;
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
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.user-level-list-page .table-wrapper {
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

.edit-level-select {
  width: 160px;
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
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger) {
  opacity: 1;
}

.op-more-item--primary {
  color: $cyan-primary;
  font-size: 13px;
}

:deep(.el-table__body tr.el-table__row.so-item-row--active > td.el-table__cell) {
  background: rgba(0, 160, 220, 0.1) !important;
}
</style>
