<template>
  <div class="finance-page customs-declaration-list-page">
    <div class="page-header-row">
      <h1 class="finance-list-page-title">{{ t('customsPages.declarations.title') }}</h1>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'declarationType'"
          v-model="filters.declarationType"
          clearable
          :placeholder="t('customsPages.declarations.filterDeclarationType')"
          class="filter-select"
          style="width: 120px"
          @change="handleSearch"
        >
          <el-option :label="t('customsPages.declarations.typeImport')" :value="1" />
          <el-option :label="t('customsPages.declarations.typeExport')" :value="2" />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'internalStatus'"
          v-model="filters.internalStatus"
          clearable
          :placeholder="t('customsPages.declarations.filterInternal')"
          class="filter-select"
          style="width: 130px"
          @change="handleSearch"
        >
          <el-option :label="t('customsPages.declarations.internalPending')" :value="1" />
          <el-option :label="t('customsPages.declarations.internalProcessing')" :value="2" />
          <el-option :label="t('customsPages.declarations.internalDone')" :value="3" />
          <el-option :label="t('customsPages.declarations.internalVoid')" :value="-1" />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'customsClearanceStatus'"
          v-model="filters.customsClearanceStatus"
          clearable
          :placeholder="t('customsPages.declarations.filterClearance')"
          class="filter-select"
          style="width: 120px"
          @change="handleSearch"
        >
          <el-option :label="t('customsPages.declarations.clearanceNone')" :value="0" />
          <el-option :label="t('customsPages.declarations.clearanceReleased')" :value="10" />
          <el-option :label="t('customsPages.declarations.clearanceCleared')" :value="100" />
        </el-select>
        <el-input
          v-model="filters.declarationCode"
          clearable
          :placeholder="t('customsPages.declarations.filterCode')"
          class="search-input"
          style="width: 150px"
          @keyup.enter="handleSearch"
          @clear="handleSearch"
        />
        <el-input
          v-model="filters.stockOutRequestId"
          clearable
          :placeholder="t('customsPages.declarations.filterSor')"
          class="search-input"
          style="width: 170px"
          @keyup.enter="handleSearch"
          @clear="handleSearch"
        />
        <el-date-picker
          v-model="filters.declareRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          unlink-panels
          class="date-picker"
          :start-placeholder="t('customsPages.declarations.filterDeclareDate')"
          @change="handleSearch"
        />
        <el-button type="primary" @click="handleSearch">{{ t('customsPages.declarations.search') }}</el-button>
        <el-button @click="resetFilters">{{ t('customsPages.declarations.reset') }}</el-button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="cdl-list-settings-popper"
        >
          <template #reference>
            <el-button
              class="cdl-settings-gear-btn"
              :title="t('customsPages.declarations.settingsMenu.aria')"
              :aria-label="t('customsPages.declarations.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </el-button>
          </template>
          <div class="cdl-list-settings-menu">
            <button
              type="button"
              class="cdl-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('customsPages.declarations.settingsMenu.closeTabs') }}
            </button>
            <div
              class="cdl-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="cdl-list-settings-menu__item cdl-list-settings-menu__item--parent">
                <span>{{ t('customsPages.declarations.settingsMenu.tabMode') }}</span>
                <el-icon class="cdl-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="cdl-list-settings-menu__flyout">
                <button
                  v-for="dim in CUSTOMS_DECLARATION_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="cdl-list-settings-menu__item"
                  :class="{ 'is-active': tabModeDimension === dim }"
                  @click="enableFilterTabMode(dim)"
                >
                  {{ tabModeDimensionLabel(dim) }}
                </button>
              </div>
            </div>
          </div>
        </el-popover>
      </div>
    </div>

    <div class="cdl-main-panel" :class="{ 'cdl-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="cdl-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="cdl-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="customs-declaration-list-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedRows"
      v-loading="loading"
      :row-class-name="opsPanelRowClassName"
      @row-click="onRowClick"
      @row-dblclick="onDblClick"
    >
      <template #col-internalStatus="{ row }">
        <el-tag :type="internalTagType(row.internalStatus)" size="small" effect="plain">
          {{ internalLabel(row.internalStatus) }}
        </el-tag>
      </template>
      <template #col-customsClearanceStatus="{ row }">
        <el-tag :type="clearanceTagType(row.customsClearanceStatus)" size="small" effect="plain">
          {{ clearanceLabel(row.customsClearanceStatus) }}
        </el-tag>
      </template>
      <template #col-declarationCode="{ row }">
        <span class="code-text">{{ row.declarationCode || '—' }}</span>
      </template>
      <template #col-declareDate="{ row }">
        <span class="text-secondary">{{ row.declareDate ? formatDisplayDate(row.declareDate) : '—' }}</span>
      </template>
      <template #col-customsBrokerName="{ row }">
        <span>{{ row.customsBrokerName || '—' }}</span>
      </template>
      <template #col-totalTaxAmount="{ row }">
        <span class="amount-text dock-quote-tier-line">{{ formatTotalAmountNumber(row.totalTaxAmount) }}</span>
      </template>
      <template #col-remark="{ row }">
        <span>{{ row.remark || '—' }}</span>
      </template>
      <template #col-stockOutRequestCode="{ row }">
        <router-link
          v-if="row.stockOutRequestId"
          :to="{ name: 'StockOutNotifyDetail', params: { id: row.stockOutRequestId } }"
          class="cell-link"
          @click.stop
        >
          {{ row.stockOutRequestCode || row.stockOutRequestId }}
        </router-link>
        <span v-else>—</span>
      </template>
      <template #col-createTime="{ row }">
        <span class="text-secondary">{{ row.createTime ? formatDisplayDateTime(row.createTime) : '—' }}</span>
      </template>
      <template #col-createUserDisplay="{ row }">
        <span>{{ row.createUserDisplay || '—' }}</span>
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
            <el-button v-if="canWriteLogisticsData" link type="primary" size="small" @click.stop="openClearance(row)">
              {{ t('customsPages.declarations.setClearance') }}
            </el-button>
            <el-button
              v-if="canWriteLogisticsData && row.customsClearanceStatus === 100 && row.internalStatus !== -1"
              link
              type="primary"
              size="small"
              @click.stop="handleCreateArrival(row)"
            >
              {{ t('customsPages.declarations.createArrivalNotifies') }}
            </el-button>
            <el-button v-if="canWriteLogisticsData" link type="danger" size="small" @click.stop="handleDelete(row)">
              {{ t('customsPages.declarations.delete') }}
            </el-button>
            <el-button v-if="canForceDelete" link type="danger" size="small" @click.stop="handleForceDelete(row)">
              {{ t('customsPages.declarations.forceDelete') }}
            </el-button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="openClearance(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('customsPages.declarations.setClearance') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canWriteLogisticsData && row.customsClearanceStatus === 100 && row.internalStatus !== -1"
                  @click.stop="handleCreateArrival(row)"
                >
                  <span class="op-more-item op-more-item--primary">{{ t('customsPages.declarations.createArrivalNotifies') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" divided @click.stop="handleDelete(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('customsPages.declarations.delete') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canForceDelete" @click.stop="handleForceDelete(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('customsPages.declarations.forceDelete') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
      <div class="list-footer-left">
        <el-tooltip :content="t('customsPages.declarations.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('customsPages.declarations.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onPageSizeChange"
        @current-change="clampPage"
      />
    </div>
    </div>

    <el-dialog
      v-model="clearanceVisible"
      :title="t('customsPages.declarations.clearanceDialogTitle')"
      width="400px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-select v-model="clearanceForm.status" style="width: 100%">
        <el-option :label="t('customsPages.declarations.clearanceNone')" :value="0" />
        <el-option :label="t('customsPages.declarations.clearanceReleased')" :value="10" />
        <el-option :label="t('customsPages.declarations.clearanceCleared')" :value="100" />
      </el-select>
      <template #footer>
        <el-button @click="clearanceVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="clearanceSaving" @click="saveClearance">{{ t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import {
  CUSTOMS_DECLARATION_LIST_TAB_MODE_OPTIONS,
  CDL_INTERNAL_STATUS_TAB_VALUES,
  CDL_DECLARATION_TYPE_TAB_VALUES,
  CDL_CLEARANCE_STATUS_TAB_VALUES,
  readCustomsDeclarationListTabMode,
  writeCustomsDeclarationListTabMode,
  cdlInternalStatusFilterToTab,
  cdlInternalStatusTabToFilter,
  cdlDeclarationTypeFilterToTab,
  cdlDeclarationTypeTabToFilter,
  cdlClearanceStatusFilterToTab,
  cdlClearanceStatusTabToFilter,
  type CustomsDeclarationListTabModeDimension,
  type CdlInternalStatusTabId,
  type CdlDeclarationTypeTabId,
  type CdlClearanceStatusTabId
} from '@/utils/customsDeclarationListTabMode'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import {
  createCustomsArrivalNotifies,
  deleteCustomsDeclaration,
  fetchCustomsDeclarations,
  forceDeleteCustomsDeclaration,
  patchCustomsClearanceStatus,
  type CustomsDeclarationListItemDto
} from '@/api/customs'
import { useAuthStore } from '@/stores/auth'
import { useCustomsDeclarationOpsPanelStore } from '@/stores/customsDeclarationOpsPanel'
import { useCustomsDeclarationFlowPanelStore } from '@/stores/customsDeclarationFlowPanel'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber } from '@/utils/moneyFormat'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const customsDeclarationOpsStore = useCustomsDeclarationOpsPanelStore()
const customsDeclarationFlowStore = useCustomsDeclarationFlowPanelStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const canForceDelete = computed(() => authStore.canForceDelete())

const loading = ref(false)
const tabModeDimension = ref<CustomsDeclarationListTabModeDimension>(
  readCustomsDeclarationListTabMode()
)
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)
const allRows = ref<CustomsDeclarationListItemDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const filters = reactive<{
  declarationType?: number
  internalStatus?: number
  customsClearanceStatus?: number
  declarationCode: string
  stockOutRequestId: string
  declareRange: string[] | null
}>({
  declarationCode: '',
  stockOutRequestId: '',
  declareRange: null
})

const TAB_MODE_FILTER_I18N: Record<Exclude<CustomsDeclarationListTabModeDimension, 'off'>, string> =
  {
    internalStatus: 'customsPages.declarations.filterInternal',
    declarationType: 'customsPages.declarations.filterDeclarationType',
    customsClearanceStatus: 'customsPages.declarations.filterClearance'
  }

function tabModeDimensionLabel(dim: Exclude<CustomsDeclarationListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeCustomsDeclarationListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<CustomsDeclarationListTabModeDimension, 'off'>) {
  tabModeDimension.value = dim
  writeCustomsDeclarationListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const filterTabStripVisible = computed(() => tabModeDimension.value !== 'off')

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

type CdlFilterTabId = CdlInternalStatusTabId | CdlDeclarationTypeTabId | CdlClearanceStatusTabId

function declarationTypeLabel(v: number) {
  if (v === 1) return t('customsPages.declarations.typeImport')
  if (v === 2) return t('customsPages.declarations.typeExport')
  return String(v)
}

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return [] as Array<{ id: CdlFilterTabId; label: string }>
  if (dim === 'internalStatus') {
    return [
      { id: 'all' as const, label: t('customsPages.declarations.filterTabs.all') },
      ...CDL_INTERNAL_STATUS_TAB_VALUES.map((value) => ({
        id: String(value) as CdlInternalStatusTabId,
        label: internalLabel(value)
      }))
    ]
  }
  if (dim === 'declarationType') {
    return [
      { id: 'all' as const, label: t('customsPages.declarations.filterTabs.all') },
      ...CDL_DECLARATION_TYPE_TAB_VALUES.map((value) => ({
        id: String(value) as CdlDeclarationTypeTabId,
        label: declarationTypeLabel(value)
      }))
    ]
  }
  return [
    { id: 'all' as const, label: t('customsPages.declarations.filterTabs.all') },
    ...CDL_CLEARANCE_STATUS_TAB_VALUES.map((value) => ({
      id: String(value) as CdlClearanceStatusTabId,
      label: clearanceLabel(value)
    }))
  ]
})

const activeFilterTabId = computed((): CdlFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'internalStatus') return cdlInternalStatusFilterToTab(filters.internalStatus)
  if (dim === 'declarationType') return cdlDeclarationTypeFilterToTab(filters.declarationType)
  if (dim === 'customsClearanceStatus') {
    return cdlClearanceStatusFilterToTab(filters.customsClearanceStatus)
  }
  return 'all'
})

function onFilterTabClick(tab: CdlFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'internalStatus') {
    const next = cdlInternalStatusTabToFilter(tab as CdlInternalStatusTabId)
    if (filters.internalStatus === next) return
    filters.internalStatus = next
    handleSearch()
    return
  }
  if (dim === 'declarationType') {
    const next = cdlDeclarationTypeTabToFilter(tab as CdlDeclarationTypeTabId)
    if (filters.declarationType === next) return
    filters.declarationType = next
    handleSearch()
    return
  }
  if (dim === 'customsClearanceStatus') {
    const next = cdlClearanceStatusTabToFilter(tab as CdlClearanceStatusTabId)
    if (filters.customsClearanceStatus === next) return
    filters.customsClearanceStatus = next
    handleSearch()
  }
}

const query = reactive({ page: 1, pageSize: 20 })

const clearanceVisible = ref(false)
const clearanceSaving = ref(false)
const clearanceRow = ref<CustomsDeclarationListItemDto | null>(null)
const clearanceForm = reactive({ status: 0 })

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 380
const OP_COL_EXPANDED_MIN_WIDTH = 340
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'internalStatus', label: t('customsPages.declarations.colInternal'), prop: 'internalStatus', width: 120, align: 'center' },
  { key: 'customsClearanceStatus', label: t('customsPages.declarations.colClearance'), prop: 'customsClearanceStatus', width: 110, align: 'center' },
  { key: 'declarationCode', label: t('customsPages.declarations.colDecCode'), prop: 'declarationCode', width: 140, minWidth: 130 },
  { key: 'declareDate', label: t('customsPages.declarations.colDeclareDate'), prop: 'declareDate', width: 120 },
  { key: 'customsBrokerName', label: t('customsPages.declarations.colBroker'), prop: 'customsBrokerName', minWidth: 140, showOverflowTooltip: true },
  { key: 'totalTaxAmount', label: t('customsPages.declarations.colTotal'), prop: 'totalTaxAmount', width: 120, align: 'right' },
  { key: 'remark', label: t('customsPages.declarations.colRemark'), prop: 'remark', minWidth: 120, showOverflowTooltip: true },
  { key: 'stockOutRequestCode', label: t('customsPages.declarations.colSor'), prop: 'stockOutRequestCode', minWidth: 160, showOverflowTooltip: true },
  { key: 'createTime', label: t('customsPages.declarations.colCreateTime'), prop: 'createTime', width: 170 },
  { key: 'createUserDisplay', label: t('customsPages.declarations.colCreator'), prop: 'createUserDisplay', width: 110, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('customsPages.declarations.colActions'),
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
])

const listTotal = computed(() => allRows.value.length)

const pagedRows = computed(() => {
  const start = (query.page - 1) * query.pageSize
  return allRows.value.slice(start, start + query.pageSize)
})

function internalLabel(v: number) {
  if (v === -1) return t('customsPages.declarations.internalVoid')
  const m: Record<number, string> = {
    1: t('customsPages.declarations.internalPending'),
    2: t('customsPages.declarations.internalProcessing'),
    3: t('customsPages.declarations.internalDone')
  }
  return m[v] ?? String(v)
}

function internalTagType(v: number) {
  if (v === -1) return 'info'
  if (v === 3) return 'success'
  if (v === 2) return 'warning'
  return 'info'
}

function clearanceLabel(v: number) {
  const m: Record<number, string> = {
    0: t('customsPages.declarations.clearanceNone'),
    10: t('customsPages.declarations.clearanceReleased'),
    100: t('customsPages.declarations.clearanceCleared')
  }
  return m[v] ?? String(v)
}

function clearanceTagType(v: number) {
  if (v === 100) return 'success'
  if (v === 10) return 'warning'
  return 'info'
}

function clampPage() {
  const maxPage = Math.max(1, Math.ceil(listTotal.value / query.pageSize) || 1)
  if (query.page > maxPage) query.page = maxPage
}

function onPageSizeChange() {
  query.page = 1
  clampPage()
}

function handleSearch() {
  query.page = 1
  void load()
}

function resetFilters() {
  filters.declarationType = undefined
  filters.internalStatus = undefined
  filters.customsClearanceStatus = undefined
  filters.declarationCode = ''
  filters.stockOutRequestId = ''
  filters.declareRange = null
  handleSearch()
}

async function load() {
  loading.value = true
  try {
    const params: Record<string, unknown> = { take: 500 }
    if (filters.declarationType != null) params.declarationType = filters.declarationType
    if (filters.internalStatus != null) params.internalStatus = filters.internalStatus
    if (filters.customsClearanceStatus != null) params.customsClearanceStatus = filters.customsClearanceStatus
    if (filters.declarationCode.trim()) params.declarationCode = filters.declarationCode.trim()
    if (filters.stockOutRequestId.trim()) params.stockOutRequestId = filters.stockOutRequestId.trim()
    if (filters.declareRange?.length === 2) {
      params.declareDateFrom = filters.declareRange[0]
      params.declareDateTo = filters.declareRange[1]
    }
    allRows.value = await fetchCustomsDeclarations(params)
    clampPage()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    loading.value = false
  }
  resetListRightPanelOnReload(customsDeclarationOpsStore)
  resetListRightPanelOnReload(customsDeclarationFlowStore)
}

function bindBoth(row: Record<string, unknown>) {
  customsDeclarationOpsStore.setRowOnly(row)
  customsDeclarationFlowStore.setRowOnly(row)
}

async function loadActiveRightTab() {
  const tab = workspaceLayout?.rightActiveTabId.value
  if (tab === 'r-flow') {
    await customsDeclarationFlowStore.loadSelected(t('customsPages.declarations.flowPanel.loadFailed'))
    return
  }
  await customsDeclarationOpsStore.loadDetail(t('customsPages.declarations.opsPanel.loadFailed'))
}

const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'CustomsDeclarationList',
  hasSelectedRow: () => !!customsDeclarationOpsStore.row || !!customsDeclarationFlowStore.row,
  setRowOnly: (row) => bindBoth(row),
  selectRow: async (row) => {
    bindBoth(row)
    await loadActiveRightTab()
  },
  loadSelected: () => {
    void loadActiveRightTab()
  },
  dataTabIds: ['r-ops', 'r-flow']
})

async function onRowClick(row: CustomsDeclarationListItemDto) {
  await onOpsPanelRowClick(row as unknown as Record<string, unknown>)
}

function opsPanelRowClassName({ row }: { row: CustomsDeclarationListItemDto }) {
  if (!customsDeclarationOpsStore.row) return 'table-row-pointer'
  return customsDeclarationOpsStore.rowKey(row as unknown as Record<string, unknown>) ===
    customsDeclarationOpsStore.rowKey(customsDeclarationOpsStore.row)
    ? 'so-item-row--active'
    : 'table-row-pointer'
}

function openClearance(row: CustomsDeclarationListItemDto) {
  clearanceRow.value = row
  clearanceForm.status = row.customsClearanceStatus
  clearanceVisible.value = true
}

async function saveClearance() {
  if (!clearanceRow.value) return
  clearanceSaving.value = true
  try {
    await patchCustomsClearanceStatus(clearanceRow.value.id, clearanceForm.status)
    ElMessage.success(t('customsPages.declarations.clearanceSaved'))
    clearanceVisible.value = false
    await load()
    if (
      customsDeclarationOpsStore.row &&
      customsDeclarationOpsStore.rowKey(customsDeclarationOpsStore.row) === clearanceRow.value.id
    ) {
      await customsDeclarationOpsStore.loadDetail(t('customsPages.declarations.opsPanel.loadFailed'))
    }
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    clearanceSaving.value = false
  }
}

async function handleDelete(row: CustomsDeclarationListItemDto) {
  try {
    await ElMessageBox.confirm(
      t('customsPages.declarations.deleteConfirm', { code: row.declarationCode }),
      t('customsPages.declarations.deleteTitle'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    await deleteCustomsDeclaration(row.id)
    ElMessage.success(t('customsPages.declarations.deleteSuccess'))
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

async function handleForceDelete(row: CustomsDeclarationListItemDto) {
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt(
      t('customsPages.declarations.forceDeletePrompt'),
      t('customsPages.declarations.forceDeleteTitle'),
      { inputPlaceholder: row.declarationCode }
    )
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered !== String(row.declarationCode || '').trim()) {
    ElMessage.error(t('customsPages.declarations.forceDeleteMismatch'))
    return
  }
  try {
    await forceDeleteCustomsDeclaration(row.id, entered)
    ElMessage.success(t('customsPages.declarations.forceDeleteSuccess'))
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

function onDblClick(row: CustomsDeclarationListItemDto) {
  router.push({ name: 'CustomsDeclarationDetail', params: { id: row.id } })
}

async function handleCreateArrival(row: CustomsDeclarationListItemDto) {
  try {
    await ElMessageBox.confirm(
      t('customsPages.declarations.createArrivalConfirm'),
      t('customsPages.declarations.createArrivalNotifies'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  customsDeclarationOpsStore.actionLoading = true
  try {
    const result = await createCustomsArrivalNotifies(row.id)
    const codes = result.created?.map((c) => c.noticeCode).filter(Boolean).join('、')
    ElMessage.success(
      codes
        ? t('customsPages.declarations.createArrivalOkWithCodes', { codes })
        : t('customsPages.declarations.createArrivalOk', { count: result.createdCount })
    )
    await load()
    if (
      customsDeclarationOpsStore.row &&
      customsDeclarationOpsStore.rowKey(customsDeclarationOpsStore.row) === row.id
    ) {
      await customsDeclarationOpsStore.loadDetail(t('customsPages.declarations.opsPanel.loadFailed'))
    }
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    customsDeclarationOpsStore.actionLoading = false
  }
}

onMounted(() => {
  customsDeclarationOpsStore.registerHandlers({
    setClearance: (row) => {
      openClearance(row as unknown as CustomsDeclarationListItemDto)
    },
    createArrival: (row) => {
      void handleCreateArrival(row as unknown as CustomsDeclarationListItemDto)
    }
  })
  void load()
})

onBeforeUnmount(() => {
  customsDeclarationOpsStore.unregisterHandlers()
  customsDeclarationOpsStore.clear()
  customsDeclarationFlowStore.clear()
})
</script>

<style lang="scss" scoped>
@import '../Finance/finance-common.scss';

.page-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.pagination-wrap {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
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

.cell-link {
  color: var(--el-color-primary);
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.cdl-settings-gear-btn {
  padding: 8px 10px;
}

.cdl-main-panel {
  width: 100%;
}

.cdl-main-panel--with-filter-tabs {
  :deep(.crm-data-table-root) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }

  :deep(.el-table),
  :deep(.el-table__inner-wrapper),
  :deep(.el-table__header-wrapper) {
    border-top-left-radius: 0;
    border-top-right-radius: 0;
  }
}

.cdl-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.cdl-filter-tabs__item {
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 8px;
  border: 1px solid var(--crm-border-panel, #e2e8f0);
  border-bottom: none;
  border-radius: 8px 8px 0 0;
  background: #e8edf5;
  color: var(--crm-text-primary);
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  font-weight: 500;
  text-align: center;
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s, color 0.12s, box-shadow 0.12s;

  &:hover {
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 45%, var(--crm-border-panel));
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }

  &.is-active {
    background: color-mix(in srgb, var(--crm-cyan-primary) 16%, var(--crm-layer-2, #fff));
    border-color: color-mix(in srgb, var(--crm-cyan-primary) 55%, var(--crm-border-panel));
    box-shadow: inset 0 2px 0 0 var(--crm-cyan-primary);
    font-weight: 600;
    z-index: 1;
  }
}

html[data-theme='dark'] .cdl-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}
</style>

<style lang="scss">
.cdl-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.cdl-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.cdl-list-settings-menu__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 8px 10px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--crm-text-secondary, rgba(224, 244, 255, 0.7));
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  text-align: left;
  cursor: pointer;

  &:hover:not(:disabled) {
    background: var(--crm-accent-008, rgba(0, 212, 255, 0.08));
    color: var(--crm-text-primary, #e8f4ff);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  &.is-active {
    color: var(--crm-cyan-primary, #00d4ff);
  }

  &--parent {
    cursor: default;
  }
}

.cdl-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.cdl-list-settings-menu__submenu {
  position: relative;
}

.cdl-list-settings-menu__flyout {
  position: absolute;
  top: 0;
  left: calc(100% + 4px);
  min-width: 168px;
  padding: 6px;
  border-radius: 8px;
  border: 1px solid var(--crm-border-panel, rgba(0, 212, 255, 0.15));
  background: var(--crm-layer-2, #0d1e35);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.28);
  z-index: 10;
}
</style>
