<template>
  <div class="stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 5h18M3 12h18M3 19h18" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutNotifyList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockOutNotifyList.count', { count: listTotal }) }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'status'"
          v-model="filterForm.status"
          :placeholder="t('stockOutNotifyList.filters.statusPlaceholder')"
          clearable
          class="status-select"
          :teleported="false"
        >
          <el-option
            v-for="opt in statusFilterOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'regionType'"
          v-model="filterForm.regionType"
          :placeholder="t('stockOutNotifyList.filters.regionPlaceholder')"
          clearable
          class="status-select status-select--region"
          :teleported="false"
        >
          <el-option :label="t('inventoryList.warehouse.regionDomestic')" :value="REGION_TYPE_DOMESTIC" />
          <el-option :label="t('inventoryList.warehouse.regionOverseas')" :value="REGION_TYPE_OVERSEAS" />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'stockOutType'"
          v-model="filterForm.stockOutType"
          :placeholder="t('stockOutNotifyList.filters.stockOutTypePlaceholder')"
          clearable
          class="status-select status-select--stock-out-type"
          :teleported="false"
        >
          <el-option
            v-for="v in STOCK_OUT_TYPE_FILTER_VALUES"
            :key="v"
            :label="notifyStockOutTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <div v-if="!maskSaleSensitiveFields" class="search-input-wrap">
          <input
            v-model="filterForm.customerName"
            class="search-input search-input--customer"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.customerPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <div v-if="!maskSaleSensitiveFields" class="search-input-wrap">
          <input
            v-model="filterForm.salesUserName"
            class="search-input search-input--sales"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.salesUserPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="search-input-wrap">
          <input
            v-model="filterForm.materialModel"
            class="search-input search-input--material"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.materialModelPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-date-picker
          v-model="filterForm.requestDateRange"
          type="daterange"
          :range-separator="t('stockOutNotifyList.filters.dateTo')"
          :start-placeholder="t('stockOutNotifyList.filters.dateStart')"
          :end-placeholder="t('stockOutNotifyList.filters.dateEnd')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('stockOutNotifyList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('stockOutNotifyList.filters.reset') }}</button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="son-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('stockOutNotifyList.settingsMenu.aria')"
              :aria-label="t('stockOutNotifyList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="son-list-settings-menu">
            <button
              type="button"
              class="son-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('stockOutNotifyList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="son-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="son-list-settings-menu__item son-list-settings-menu__item--parent">
                <span>{{ t('stockOutNotifyList.settingsMenu.tabMode') }}</span>
                <el-icon class="son-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="son-list-settings-menu__flyout">
                <button
                  v-for="dim in SON_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="son-list-settings-menu__item"
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

    <div class="son-main-panel" :class="{ 'son-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="son-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="son-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-notify-list-main-v7"
      :columns="stockOutNotifyColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      row-key="id"
      :row-class-name="customsPanelRowClassName"
      v-loading="loading"
      @selection-change="onSelectionChange"
      @row-click="onRowClick"
      @row-dblclick="goDetail"
    >
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-customsStatus="{ row }">{{ customsStatusLabel(row.customsStatus) }}</template>
      <template #col-stockOutType="{ row }">
        <StockBizTypeTag
          biz="out"
          :type="row.stockOutType"
          :customs-declaration-id="row.customsDeclarationId"
          :customs-declaration-code="row.customsDeclarationCode"
        />
      </template>
      <template #col-requestCode="{ row }">
        <span class="notify-code-cell">
          <span class="notify-code-text">{{ row.requestCode || '—' }}</span>
          <el-tooltip
            v-if="isCustomsNotify(row) && salesNotifyTooltip(row)"
            :content="salesNotifyTooltip(row)"
            placement="top"
            :hide-after="0"
          >
            <span class="customs-notify-tag">{{ t('stockOutNotifyList.customsNotifyTag') }}</span>
          </el-tooltip>
        </span>
      </template>
      <template #col-outQuantity="{ row }">{{ row.outQuantity }}</template>
      <template #col-regionType="{ row }">{{ regionTypeLabel(row) }}</template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      <template #col-packingCode="{ row }">
        <router-link
          v-if="row.packingId?.trim() && row.packingCode?.trim()"
          :to="{ name: 'PackingDetail', params: { id: row.packingId.trim() } }"
          class="cell-link"
          @click.stop
        >
          {{ row.packingCode.trim() }}
        </router-link>
        <span v-else-if="row.packingCode?.trim()">{{ row.packingCode.trim() }}</span>
        <span v-else>—</span>
      </template>
      <template #col-salesOrderCode="{ row }">
        <router-link
          v-if="row.salesOrderId?.trim() && row.salesOrderCode?.trim()"
          class="link-text"
          :to="`/sales-orders/${row.salesOrderId.trim()}`"
          @click.stop
        >
          {{ row.salesOrderCode.trim() }}
        </router-link>
        <span v-else>{{ row.salesOrderCode?.trim() || '—' }}</span>
      </template>
      <template #col-requestDate="{ row }">{{ formatRequestDateTime(row.requestDate) }}</template>
      <template #col-createTime="{ row }">{{ formatRequestDateTime(row.createTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.requestUserName || '--' }}</template>
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
          <template v-if="opColExpanded">
            <div
              v-if="Number(row.status) !== STOCK_OUT_REQUEST_STATUS.StockedOut && Number(row.status) !== STOCK_OUT_REQUEST_STATUS.Cancelled"
              class="action-btns"
            >
              <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
              <button v-if="isSysAdmin" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteRow(row)">
                强制删除
              </button>
            </div>
            <span v-else class="op-done">{{ t('stockOutNotifyList.actions.alreadyShipped') }}</span>
          </template>
          <template v-else>
            <el-tooltip
              v-if="Number(row.status) === STOCK_OUT_REQUEST_STATUS.StockedOut"
            :content="t('stockOutNotifyList.actions.alreadyShipped')"
            placement="left"
            :hide-after="0"
          >
            <span class="op-done op-done--collapsed" :aria-label="t('stockOutNotifyList.actions.alreadyShipped')">—</span>
            </el-tooltip>
            <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="isSysAdmin" @click.stop="handleForceDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
            </el-dropdown>
          </template>
        </div>
      </template>
    </CrmDataTable>
    <div v-if="listTotal > 0" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('stockOutNotifyList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('stockOutNotifyList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          {{ t('stockOutNotifyList.basket.open') }}<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          {{ t('stockOutNotifyList.basket.clear') }}
        </el-button>
        <el-tooltip
          :content="t('stockOutNotifyList.packing.createPackingSelectFirst')"
          placement="top"
          :disabled="!!basketCount"
          :hide-after="0"
        >
          <span class="basket-batch-purchase-btn-wrap">
            <button
              type="button"
              class="btn-primary btn-sm basket-batch-purchase-btn"
              :disabled="!basketCount"
              @click="handleCreatePacking"
            >
              {{ t('stockOutNotifyList.actions.createPacking') }}
            </button>
          </span>
        </el-tooltip>
      </div>
      <el-pagination
        class="list-main-pagination quantum-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void runNotifyFetch(false)"
        @size-change="onNotifyPageSizeChange"
      />
    </div>
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      :title="t('stockOutNotifyList.basket.drawerTitle')"
      direction="rtl"
      size="min(560px, 94vw)"
      class="stock-out-notify-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('stockOutNotifyList.basket.emptyHint') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('stockOutNotifyList.basket.summaryBeforeBtn', { count: basketCount }) }}
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            {{ t('stockOutNotifyList.basket.clear') }}
          </el-button>
          {{ t('stockOutNotifyList.basket.summaryAfterBtn') }}
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column
              :label="t('stockOutNotifyList.columns.requestCode')"
              prop="requestCode"
              min-width="140"
              show-overflow-tooltip
            />
            <el-table-column
              :label="t('stockOutNotifyList.columns.materialModel')"
              prop="materialModel"
              min-width="130"
              show-overflow-tooltip
            />
            <el-table-column
              :label="t('stockOutNotifyList.columns.customer')"
              prop="customerName"
              min-width="120"
              show-overflow-tooltip
            />
            <el-table-column :label="t('stockOutNotifyList.columns.status')" width="100" align="center">
              <template #default="{ row }">
                <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
              </template>
            </el-table-column>
            <el-table-column :label="t('stockOutNotifyList.columns.actions')" width="72" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="warning" size="small" @click="removeOneFromBasket(row.id)">
                  {{ t('stockOutNotifyList.basket.remove') }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import {
  SON_LIST_TAB_MODE_OPTIONS,
  SON_STATUS_TAB_VALUES,
  SON_STOCK_OUT_TYPE_TAB_VALUES,
  readSonListTabMode,
  writeSonListTabMode,
  statusFilterToTab,
  statusTabToFilter,
  regionFilterToTab,
  regionTabToFilter,
  stockOutTypeFilterToTab,
  stockOutTypeTabToFilter,
  type SonListTabModeDimension,
  type SonStatusTabId,
  type SonRegionTabId,
  type SonStockOutTypeTabId
} from '@/utils/stockOutNotifyListTabMode'
import { storeToRefs } from 'pinia'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import { normalizeRegionType, REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { buildStockOutNotifyListColumns } from '@/composables/buildStockOutNotifyListColumns'
import { useAuthStore } from '@/stores/auth'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { useStockOutNotifyListBasketStore } from '@/stores/stockOutNotifyListBasket'
import { useStockOutNotifyCustomsPanelStore } from '@/stores/stockOutNotifyCustomsPanel'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { StockOutTypeCode, STOCK_OUT_TYPE_FILTER_VALUES } from '@/constants/stockOutType'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const router = useRouter()
const { t, locale } = useI18n()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const stockOutNotifyCustomsPanelStore = useStockOutNotifyCustomsPanelStore()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const loading = ref(false)
const tabModeDimension = ref<SonListTabModeDimension>(readSonListTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)

const TAB_MODE_FILTER_I18N: Record<Exclude<SonListTabModeDimension, 'off'>, string> = {
  status: 'stockOutNotifyList.filters.status',
  regionType: 'stockOutNotifyList.filters.regionType',
  stockOutType: 'stockOutNotifyList.filters.stockOutType'
}

function tabModeDimensionLabel(dim: Exclude<SonListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeSonListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<SonListTabModeDimension, 'off'>) {
  tabModeDimension.value = dim
  writeSonListTabMode(dim)
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

type SonFilterTabId = SonStatusTabId | SonRegionTabId | SonStockOutTypeTabId

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return [] as Array<{ id: SonFilterTabId; label: string }>
  if (dim === 'status') {
    return [
      { id: 'all' as const, label: t('stockOutNotifyList.filterTabs.all') },
      ...SON_STATUS_TAB_VALUES.map((value) => ({
        id: String(value) as SonStatusTabId,
        label: statusFilterOptions.value.find((o) => o.value === value)?.label ?? String(value)
      }))
    ]
  }
  if (dim === 'regionType') {
    return [
      { id: 'all' as const, label: t('stockOutNotifyList.filterTabs.all') },
      { id: '10' as const, label: t('inventoryList.warehouse.regionDomestic') },
      { id: '20' as const, label: t('inventoryList.warehouse.regionOverseas') }
    ]
  }
  return [
    { id: 'all' as const, label: t('stockOutNotifyList.filterTabs.all') },
    ...SON_STOCK_OUT_TYPE_TAB_VALUES.map((value) => ({
      id: String(value) as SonStockOutTypeTabId,
      label: notifyStockOutTypeLabel(value)
    }))
  ]
})

const activeFilterTabId = computed((): SonFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'status') return statusFilterToTab(filterForm.status)
  if (dim === 'regionType') return regionFilterToTab(filterForm.regionType)
  if (dim === 'stockOutType') return stockOutTypeFilterToTab(filterForm.stockOutType)
  return 'all'
})

function onFilterTabClick(tab: SonFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'status') {
    const next = statusTabToFilter(tab as SonStatusTabId)
    if (filterForm.status === next) return
    filterForm.status = next
    handleSearch()
    return
  }
  if (dim === 'regionType') {
    const next = regionTabToFilter(tab as SonRegionTabId)
    if (filterForm.regionType === next) return
    filterForm.regionType = next
    handleSearch()
    return
  }
  if (dim === 'stockOutType') {
    const next = stockOutTypeTabToFilter(tab as SonStockOutTypeTabId)
    if (filterForm.stockOutType === next) return
    filterForm.stockOutType = next
    handleSearch()
  }
}

const filterForm = reactive({
  status: undefined as number | undefined,
  regionType: undefined as number | undefined,
  stockOutType: undefined as number | undefined,
  customerName: '',
  salesUserName: '',
  materialModel: '',
  requestDateRange: null as [string, string] | null
})
const list = ref<StockOutRequestDto[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const dataTableRef = ref<{
  openColumnSettings?: () => void
  clearSelection?: () => void
  toggleRowSelection?: (row: unknown, selected?: boolean) => void
} | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const basketStore = useStockOutNotifyListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)
const basketDrawerVisible = ref(false)
const suppressBasketMerge = ref(false)

// 列表操作列：默认收起（Collapsed）
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

const statusFilterOptions = computed(() => [
  { value: STOCK_OUT_REQUEST_STATUS.PendingCustoms, label: t('stockOutNotifyList.status.pendingCustoms') },
  { value: STOCK_OUT_REQUEST_STATUS.PendingPacking, label: t('stockOutNotifyList.status.pendingPacking') },
  { value: STOCK_OUT_REQUEST_STATUS.Packed, label: t('stockOutNotifyList.status.packed') },
  { value: STOCK_OUT_REQUEST_STATUS.StockedOut, label: t('stockOutNotifyList.status.stockedOut') },
  { value: STOCK_OUT_REQUEST_STATUS.Cancelled, label: t('stockOutNotifyList.status.cancelled') }
])

function buildListQueryParams() {
  const params: Parameters<typeof stockOutApi.getRequestListPaged>[0] = {
    page: listPage.value,
    pageSize: listPageSize.value
  }
  if (filterForm.status != null) params.status = filterForm.status
  if (filterForm.regionType === REGION_TYPE_DOMESTIC || filterForm.regionType === REGION_TYPE_OVERSEAS) {
    params.regionType = filterForm.regionType
  }
  if (filterForm.stockOutType != null) params.stockOutType = filterForm.stockOutType
  const customer = filterForm.customerName.trim()
  if (customer) params.customerName = customer
  const salesUser = filterForm.salesUserName.trim()
  if (salesUser) params.salesUserName = salesUser
  const material = filterForm.materialModel.trim()
  if (material) params.materialModel = material
  if (filterForm.requestDateRange?.[0]) params.requestDateFrom = filterForm.requestDateRange[0]
  if (filterForm.requestDateRange?.[1]) params.requestDateTo = filterForm.requestDateRange[1]
  return params
}

const stockOutNotifyColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildStockOutNotifyListColumns({
    t,
    opColWidth: opColWidth.value,
    opColMinWidth: opColMinWidth.value,
    withSelection: true,
    withActions: true
  })
})

const statusLabel = (s: number) => {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function customsStatusLabel(code?: number | null): string {
  const n = Number(code ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.NotRequired) return '—'
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) return t('stockOutNotifyList.customsStatus.pendingCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) return t('stockOutNotifyList.customsStatus.inCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return t('stockOutNotifyList.customsStatus.completed')
  return '—'
}

function notifyStockOutTypeLabel(type?: number | null): string {
  const n = Number(type ?? StockOutTypeCode.Sales)
  if (n === StockOutTypeCode.Customs) return t('stockOutNotifyList.stockOutTypeLabels.customs')
  if (n === StockOutTypeCode.Return) return t('stockOutNotifyList.stockOutTypeLabels.return')
  if (n === StockOutTypeCode.Scrap) return t('stockOutNotifyList.stockOutTypeLabels.scrap')
  return t('stockOutNotifyList.stockOutTypeLabels.sales')
}

function isCustomsNotify(row: StockOutRequestDto): boolean {
  return resolveNotifyStockOutType(row.stockOutType) === StockOutTypeCode.Customs
}

async function onRowClick(row: StockOutRequestDto) {
  if (!isCustomsNotify(row)) {
    stockOutNotifyCustomsPanelStore.clear()
    return
  }

  await stockOutNotifyCustomsPanelStore.selectNotifyRow(
    row,
    t('stockOutNotifyList.customsTab.loadFailed')
  )
  workspaceLayout?.toggleRightPanel(true)
  await nextTick()
  workspaceLayout?.setRightActiveTab('r-stock-out-customs')
}

function customsPanelRowClassName({ row }: { row: StockOutRequestDto }) {
  if (!stockOutNotifyCustomsPanelStore.notifyRow) return 'table-row-pointer'
  return stockOutNotifyCustomsPanelStore.notifyRowKey(row) ===
    stockOutNotifyCustomsPanelStore.notifyRowKey(stockOutNotifyCustomsPanelStore.notifyRow)
    ? 'so-item-row--active'
    : 'table-row-pointer'
}

function salesNotifyTooltip(row: StockOutRequestDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutNotifyList.salesNotifyCodeTooltip', { code })
}

const regionTypeLabel = (row: StockOutRequestDto) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

/** 按本地时区显示年月日 + 时分 */
const formatRequestDateTime = (v?: string | null) => {
  if (v == null || v === '') return '--'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

async function runNotifyFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const reqPage = await stockOutApi.getRequestListPaged(buildListQueryParams())
    list.value = reqPage.items
    listTotal.value = reqPage.total
    await restoreTableSelectionFromBasket()
    resetListRightPanelOnReload(stockOutNotifyCustomsPanelStore)
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutNotifyList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  void runNotifyFetch(true)
}

function onNotifyPageSizeChange() {
  listPage.value = 1
  void runNotifyFetch(false)
}

function handleReset() {
  filterForm.status = undefined
  filterForm.regionType = undefined
  filterForm.stockOutType = undefined
  filterForm.customerName = ''
  filterForm.salesUserName = ''
  filterForm.materialModel = ''
  filterForm.requestDateRange = null
  void runNotifyFetch(true)
}

function resolveNotifyId(row: StockOutRequestDto): string {
  return String(row?.id || '').trim()
}

function isPendingNotify(row: StockOutRequestDto): boolean {
  return Number(row.status) === STOCK_OUT_REQUEST_STATUS.PendingPacking
}

async function onSelectionChange(rows: StockOutRequestDto[]) {
  if (suppressBasketMerge.value) return
  const eligible = rows.filter(isPendingNotify)
  const ineligible = rows.filter((r) => !isPendingNotify(r))
  if (ineligible.length > 0) {
    suppressBasketMerge.value = true
    for (const row of ineligible) {
      dataTableRef.value?.toggleRowSelection?.(row, false)
    }
    await nextTick()
    suppressBasketMerge.value = false
    ElMessage.warning(t('stockOutNotifyList.packing.onlyPendingSelectable'))
  }
  basketStore.mergePageSelection(list.value, eligible)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  table.clearSelection?.()
  await nextTick()
  for (const row of list.value) {
    const id = resolveNotifyId(row)
    if (id && basketStore.has(id)) {
      table.toggleRowSelection?.(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(id: string) {
  const rid = String(id || '').trim()
  if (!rid) return
  basketStore.remove(rid)
  suppressBasketMerge.value = true
  const row = list.value.find((r) => resolveNotifyId(r) === rid)
  if (row) {
    dataTableRef.value?.toggleRowSelection?.(row, false)
  }
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleClearBasket() {
  if (!basketStore.count) return
  try {
    await ElMessageBox.confirm(
      t('stockOutNotifyList.messages.clearBasketConfirm'),
      t('stockOutNotifyList.messages.clearBasketTitle'),
      {
        type: 'warning',
        confirmButtonText: t('stockOutNotifyList.messages.clearBasketOk'),
        cancelButtonText: t('stockOutNotifyList.messages.clearBasketCancel')
      }
    )
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection?.()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success(t('stockOutNotifyList.messages.basketCleared'))
}

type PackingSelectionValidation = { ok: true } | { ok: false; reasons: string[] }

function resolveNotifyStockOutType(v: unknown): number {
  const n = Number(v)
  if (
    n === StockOutTypeCode.Sales ||
    n === StockOutTypeCode.Customs ||
    n === StockOutTypeCode.Return ||
    n === StockOutTypeCode.Scrap
  ) {
    return n
  }
  return StockOutTypeCode.Sales
}

function validatePackingSelection(rows: StockOutRequestDto[]): PackingSelectionValidation {
  const reasons: string[] = []
  if (rows.length === 0) {
    reasons.push(t('stockOutNotifyList.packing.noSelection'))
    return { ok: false, reasons }
  }

  const customerIds = rows.map((r) => String(r.customerId || '').trim())
  const customerNames = rows.map((r) => String(r.customerName || '').trim())
  const uniqueCustomerIds = new Set(customerIds.filter(Boolean))
  const uniqueCustomerNames = new Set(customerNames.filter(Boolean))
  if (uniqueCustomerIds.size === 0 && uniqueCustomerNames.size === 0) {
    reasons.push(t('stockOutNotifyList.packing.ruleCustomerRequired'))
  } else if (uniqueCustomerIds.size > 1 || (uniqueCustomerIds.size === 0 && uniqueCustomerNames.size > 1)) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameCustomer'))
  }

  const currencyKeys = rows.map((r) => {
    const raw = r.currency
    if (raw == null) return null
    const n = Number(raw)
    return Number.isFinite(n) ? n : null
  })
  const uniqueCurrencies = new Set(
    currencyKeys.filter((c): c is number => c !== null).map((c) => String(c))
  )
  const currencyMismatch =
    currencyKeys.some((c) => c === null) || uniqueCurrencies.size !== 1
  if (currencyMismatch) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameCurrency'))
  }

  const stockOutTypes = rows.map((r) => resolveNotifyStockOutType(r.stockOutType))
  if (new Set(stockOutTypes).size !== 1) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameStockOutType'))
  }

  const hasMissingShipment = rows.some((r) => !String(r.shipmentMethod ?? '').trim())
  if (hasMissingShipment) {
    reasons.push(t('stockOutNotifyList.packing.ruleShipmentMethodRequired'))
  } else {
    const uniqueShipmentMethods = new Set(rows.map((r) => String(r.shipmentMethod ?? '').trim()))
    if (uniqueShipmentMethods.size > 1) {
      reasons.push(t('stockOutNotifyList.packing.ruleSameShipmentMethod'))
    }
  }

  const expressKeys = rows.map((r) => String(r.expressCompany ?? '').trim())
  if (new Set(expressKeys).size > 1) {
    reasons.push(t('stockOutNotifyList.packing.ruleSameExpressCompany'))
  }

  if (reasons.length > 0) return { ok: false, reasons }
  return { ok: true }
}

async function showPackingValidationAlert(reasons: string[]) {
  const body =
    reasons.length === 1 && reasons[0] === t('stockOutNotifyList.packing.noSelection')
      ? reasons[0]
      : `${t('stockOutNotifyList.packing.cannotCreateIntro')}\n\n${reasons.map((r) => `• ${r}`).join('\n')}`
  await ElMessageBox.alert(body, t('stockOutNotifyList.packing.cannotCreateTitle'), {
    confirmButtonText: t('stockOutNotifyList.packing.cannotCreateOk'),
    type: 'warning'
  })
}

async function handleCreatePacking() {
  if (!basketCount.value) return
  const rows = basketStore.items.filter(isPendingNotify)
  const validation = validatePackingSelection(rows)
  if (!validation.ok) {
    await showPackingValidationAlert(validation.reasons)
    return
  }
  void router.push({
    name: 'PackingCreate',
    query: { ids: rows.map((r) => r.id).join(',') }
  })
}

function goDetail(row: StockOutRequestDto) {
  const id = String(row?.id || '').trim()
  if (!id) {
    ElMessage.warning(t('stockOutNotifyList.messages.missingId'))
    return
  }
  router.push({ name: 'StockOutNotifyDetail', params: { id } })
}

const handleDeleteRow = async (row: StockOutRequestDto) => {
  const ok = window.confirm(`确认删除出库通知 ${row.requestCode} 吗？`)
  if (!ok) return
  try {
    await stockOutApi.deleteStockOutRequest(row.id)
    ElMessage.success('删除成功')
    await runNotifyFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '删除失败')
  }
}

const handleForceDeleteRow = async (row: StockOutRequestDto) => {
  const entered = window.prompt('请输入出库通知单号以确认强制删除', row.requestCode || '')?.trim() ?? ''
  if (!entered) return
  if (entered !== String(row.requestCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await stockOutApi.forceDeleteStockOutRequest(row.id, entered)
    ElMessage.success('强制删除成功')
    await runNotifyFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '强制删除失败')
  }
}

onMounted(() => {
  void ensureLogisticsDict()
  void runNotifyFetch(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-notify-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 搜索栏（与 CustomerList.vue 一致）----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.btn-icon-only {
  width: 32px;
  padding-left: 0;
  padding-right: 0;
  justify-content: center;
}

.son-main-panel {
  width: 100%;
}

.son-main-panel--with-filter-tabs {
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

.son-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.son-filter-tabs__item {
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

html[data-theme='dark'] .son-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
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

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }

  &--wide {
    width: 280px;
  }

  &--customer {
    width: 160px;
  }

  &--sales {
    width: 120px;
  }

  &--material {
    width: 160px;
  }
}

.status-select--region {
  width: 110px;
}

.status-select--stock-out-type {
  width: 120px;
}

.notify-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.notify-code-text {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.customs-notify-tag {
  flex: 0 0 auto;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.4;
  color: #ffb84d;
  background: rgba(255, 184, 77, 0.14);
  border: 1px solid rgba(255, 184, 77, 0.45);
  cursor: default;
  user-select: none;
}

.filter-date-range {
  width: 260px !important;

  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
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

.status-select--workflow {
  width: 168px;
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-5 { background: rgba(156, 89, 182, 0.18); color: #9c59b6; }
  &.status-10 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-20 { background: rgba(0,212,255,0.15); color: $cyan-primary; }
  &.status-100 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status--1 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  &:hover { text-decoration: underline; }
}
.op-done {
  font-size: 12px;
  color: $text-muted;
}

.op-done--collapsed {
  display: inline-block;
  width: 100%;
  text-align: center;
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

.basket-batch-purchase-btn-wrap {
  display: inline-flex;
  vertical-align: middle;
}

.basket-batch-purchase-btn {
  margin-left: 10px;
  letter-spacing: normal;

  &:hover:not(:disabled) {
    transform: none;
    box-shadow: none;
  }
}

.basket-open-btn {
  padding: 4px 6px 4px 8px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-clear-btn {
  padding: 4px 8px 4px 2px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-count-label {
  color: $cyan-primary;
  font-weight: 600;
  margin-left: 2px;
}

.pagination-wrapper .list-main-pagination {
  margin-left: auto;
  align-self: flex-start;
}

.cell-link {
  color: var(--el-color-primary);
  text-decoration: none;
}
.cell-link:hover {
  text-decoration: underline;
}

.link-text {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}
</style>

<style lang="scss">
@import '@/assets/styles/variables.scss';

.son-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.son-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.son-list-settings-menu__item {
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

.son-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.son-list-settings-menu__submenu {
  position: relative;
}

.son-list-settings-menu__flyout {
  position: absolute;
  top: 0;
  left: calc(100% + 4px);
  min-width: 148px;
  padding: 6px;
  border-radius: 8px;
  border: 1px solid var(--crm-border-panel, rgba(0, 212, 255, 0.15));
  background: var(--crm-layer-2, #0d1e35);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.28);
  z-index: 10;
}

.stock-out-notify-basket-drawer {
  .basket-drawer-hint {
    font-size: 13px;
    color: rgba(255, 255, 255, 0.55);
    line-height: 1.6;
    margin: 0 0 12px;
  }

  .basket-drawer-summary {
    font-size: 13px;
    color: rgba(232, 244, 255, 0.75);
    margin: 0 0 12px;
    line-height: 1.6;
  }

  .basket-clear-btn--drawer-inline {
    vertical-align: baseline;
    height: auto !important;
    min-height: 0 !important;
    padding: 0 2px !important;
    margin: 0 1px;
    font-size: 13px !important;
    font-weight: 500;
  }
}

:deep(.stock-out-type-col .cell) {
  overflow: visible;
}
</style>
