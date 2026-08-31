<template>
  <div class="stockin-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <path d="M3 9h18" />
              <path d="M9 21V9" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockInList.title') }}</h1>
        </div>
        <div v-if="viewMode === 'list'" class="count-badge">{{ t('stockInList.count', { count: listTotalServer }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-export" :disabled="exporting" @click="() => void handleExport()">
          {{ t('stockInList.filters.export') }}
        </button>
      </div>
    </div>

    <!-- 查询栏（与客户列表一致的结构与样式） -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="!warehouseTabStripActive"
          v-model="filters.warehouseId"
          class="search-select search-select--filter"
          clearable
          :placeholder="t('stockInList.filters.warehouse')"
          :teleported="false"
        >
          <el-option
            v-for="w in warehouses"
            :key="w.id"
            :label="w.warehouseName || w.warehouseCode || w.id"
            :value="w.id"
          />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'stockInType'"
          v-model="filters.stockInType"
          class="search-select search-select--filter search-select--stock-in-type"
          clearable
          :placeholder="t('stockInList.filters.stockInTypePlaceholder')"
          :teleported="false"
        >
          <el-option
            v-for="v in STOCK_IN_TYPE_FILTER_VALUES"
            :key="v"
            :label="listStockInTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <input
          v-model="filters.stockInCode"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.stockInCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.sourceDisplayNo"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.sourceDisplayNo')"
          @keyup.enter="handleSearch"
        />
        <el-date-picker
          v-model="filters.stockInDateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          class="search-date-range search-date-range--filter"
          :start-placeholder="t('stockInList.filters.stockInDateFrom')"
          :end-placeholder="t('stockInList.filters.stockInDateTo')"
          :range-separator="t('stockInList.filters.stockInDateSep')"
          clearable
          :teleported="false"
        />
        <input
          v-model="filters.remark"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.remark')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.model"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.materialModelPlaceholder')"
          @keyup.enter="handleSearch"
        />
        <input
          v-if="!maskPurchaseSensitiveFields"
          v-model="filters.vendorName"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.vendorName')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.purchaseOrderCode"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.purchaseOrderCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.freightForwarderOrderNo"
          class="search-input search-input--filter"
          :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.salesOrderCode"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.salesOrderCode')"
          @keyup.enter="handleSearch"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('stockInList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('stockInList.filters.reset') }}</button>
        <button
          class="btn-ghost btn-sm btn-board-active"
          type="button"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('stockInList.filters.listView') : t('stockInList.filters.boardView') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="stock-in-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('stockInList.settingsMenu.aria')"
              :aria-label="t('stockInList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="stock-in-list-settings-menu">
            <button
              type="button"
              class="stock-in-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('stockInList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="stock-in-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="stock-in-list-settings-menu__item stock-in-list-settings-menu__item--parent">
                <span>{{ t('stockInList.settingsMenu.tabMode') }}</span>
                <el-icon class="stock-in-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="stock-in-list-settings-menu__flyout">
                <button
                  v-for="dim in STOCK_IN_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="stock-in-list-settings-menu__item"
                  :class="{
                    'is-active': tabModeDimension === dim,
                    'is-disabled': dim === 'warehouse' && !warehouseTabModeAllowed
                  }"
                  :disabled="dim === 'warehouse' && !warehouseTabModeAllowed"
                  :title="
                    dim === 'warehouse' && !warehouseTabModeAllowed
                      ? t('stockInList.settingsMenu.warehouseTabDisabled', {
                          max: INVENTORY_WAREHOUSE_TAB_MAX
                        })
                      : undefined
                  "
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

    <div class="sil-main-panel" :class="{ 'sil-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="sil-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="sil-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        :title="tab.label"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <StockInListBoard v-if="viewMode === 'board'" :filters="boardFilters" />

    <CrmDataTable
      v-show="viewMode === 'list'"
      ref="dataTableRef"
      column-layout-key="stock-in-list-main-v3"
      :columns="stockInTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      :row-class-name="opsPanelRowClassName"
      v-loading="loading"
      @row-click="onRowClick"
      @row-dblclick="handleView"
      @header-dragend="onStockInTableHeaderDragEnd"
    >
      <template #col-stockInCode="{ row }">
        <span class="stock-in-code-cell">
          <span class="code-link" @click.stop="handleView(row)">{{ row.stockInCode }}</span>
          <el-tooltip
            v-if="isCustomsStockIn(row) && arrivalNotifyTooltip(row)"
            :content="arrivalNotifyTooltip(row)"
            placement="top"
            :hide-after="0"
          >
            <span class="customs-notify-tag">{{ t('stockInList.customsNotifyTag') }}</span>
          </el-tooltip>
        </span>
      </template>
      <template #col-stockInType="{ row }">
        <StockBizTypeTag
          biz="in"
          :type="row.stockInType"
          :customs-declaration-id="row.customsDeclarationId"
          :customs-declaration-code="row.customsDeclarationCode"
        />
      </template>
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-materialModel="{ row }">
        <CrmListCopyableTextCell :text="stockInMaterialModelCopyValue(row)" />
      </template>
      <template #col-materialBrand="{ row }">
        <CrmListCopyableTextCell :text="stockInMaterialBrandCopyValue(row)" />
      </template>
      <template #col-warehouseName="{ row }">{{ warehouseNameOf(row.warehouseId) }}</template>
      <template #col-stockInDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.stockInDate) }}</span>
      </template>
      <template #col-totalQuantity="{ row }">{{ formatNum(row.totalQuantity) }}</template>
      <template #col-hasBatchEntered="{ row }">
        <span :class="row.hasBatchEntered ? 'batch-flag batch-flag--yes' : 'batch-flag batch-flag--no'">
          {{ row.hasBatchEntered ? t('stockInList.hasBatchEntered.yes') : t('stockInList.hasBatchEntered.no') }}
        </span>
      </template>
      <template #col-vendor-header>
        <VendorExtendColumnHeader
          :active-field="vendorExtendActiveField"
          @set-active-field="setVendorExtendActiveField"
        />
      </template>
      <template #col-vendor="{ row }">
        <VendorExtendCell
          :row="row"
          :active-field="vendorExtendActiveField"
          :masked="maskPurchaseSensitiveFields"
          :empty-text="t('quoteList.na')"
        />
      </template>
      <template #col-totalAmount="{ row }">
        <span v-if="maskPurchaseSensitiveFields">—</span>
        <span v-else>{{ formatMoney(row.totalAmount) }}<template v-if="stockInCurrencyLabel(row)"><span class="text-secondary"> {{ stockInCurrencyLabel(row) }}</span></template></span>
      </template>
      <template #col-createTime="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || t('quoteList.na') }}</template>
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
            <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--info" @click.stop="handleEditRemark(row)">{{ t('stockInList.actions.editRemark') }}</button>
            <button
              v-if="canWriteLogisticsData && row.status !== 2 && row.status !== 3"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="handleFinish(row)"
            >
              {{ t('stockInList.actions.markStockedIn') }}
            </button>
            <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
            <button v-if="canForceDelete" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteRow(row)">强制删除</button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="handleEditRemark(row)">
                  <span class="op-more-item op-more-item--info">{{ t('stockInList.actions.editRemark') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canWriteLogisticsData && row.status !== 2 && row.status !== 3"
                  @click.stop="handleFinish(row)"
                >
                  <span class="op-more-item op-more-item--warning">{{ t('stockInList.actions.markStockedIn') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" divided @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canForceDelete" @click.stop="handleForceDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    </div>
    <div v-show="viewMode === 'list'" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotalServer"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void fetchList(false)"
        @size-change="onStockInPageSizeChange"
      />
    </div>

    <el-dialog v-model="remarkDialogVisible" :title="t('stockInList.actions.editRemark')" width="420px">
      <el-input v-model="remarkForm.remark" type="textarea" :rows="4" :placeholder="t('stockInList.remarkPlaceholder')" />
      <template #footer>
        <button class="btn-secondary" @click="remarkDialogVisible = false">{{ t('common.cancel') }}</button>
        <button class="btn-primary" @click="submitRemark">{{ t('common.confirm') }}</button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowRight, Setting } from '@element-plus/icons-vue'
import { withExportTimestamp } from '@/utils/exportFileName'
import { stockInApi, type StockInListItemDto } from '@/api/stockIn'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { inventoryCenterApi, type WarehouseInfo } from '@/api/inventoryCenter'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { useAuthStore } from '@/stores/auth'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import VendorExtendColumnHeader from '@/components/list/VendorExtendColumnHeader.vue'
import VendorExtendCell from '@/components/list/VendorExtendCell.vue'
import { useVendorExtendColumn, isVendorExtendTableColumn } from '@/composables/useVendorExtendColumn'
import { StockInTypeCode, STOCK_IN_TYPE_FILTER_VALUES, resolveStockInTypeLabelKey } from '@/constants/stockInType'
import {
  INVENTORY_WAREHOUSE_TAB_MAX,
  STOCK_IN_LIST_TAB_MODE_OPTIONS,
  STOCK_IN_TYPE_TAB_VALUES,
  isWarehouseTabModeAllowed,
  readStockInListTabMode,
  stockInTypeFilterToTab,
  stockInTypeTabToFilter,
  stockInWarehouseFilterToTab,
  stockInWarehouseTabToFilter,
  writeStockInListTabMode,
  type StockInListTabModeDimension,
  type StockInTypeTabId,
  type StockInWarehouseTabId
} from '@/utils/stockInListTabMode'
import StockInListBoard from '@/views/Inventory/StockInListBoard.vue'
import { useListBoardHelpOverride } from '@/composables/useHelpDocOverride'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { useStockInOpsPanelStore } from '@/stores/stockInOpsPanel'
import type { StockInListAnalyticsQuery } from '@/api/stockInAnalytics'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const viewMode = ref<'list' | 'board'>('list')
useListBoardHelpOverride('pages/入库单看板_MENU_STOCK_IN_BOARD.md', viewMode)
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const stockInOpsStore = useStockInOpsPanelStore()
const {
  expanded: vendorExtendExpanded,
  activeField: vendorExtendActiveField,
  colWidth: vendorExtendColWidth,
  colMinWidth: vendorExtendColMinWidth,
  setActiveField: setVendorExtendActiveField,
  applyOuterWidthFromTable: applyVendorExtendOuterWidth
} = useVendorExtendColumn()

function onStockInTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isVendorExtendTableColumn(column)) return
  applyVendorExtendOuterWidth(newWidth)
}
const { canWriteLogisticsData } = useDepartmentDataReadOnly()

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const canForceDelete = computed(() => authStore.canForceDelete())
const loading = ref(false)
const exporting = ref(false)
const list = ref<StockInListItemDto[]>([])
const listTotalServer = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const warehouses = ref<WarehouseInfo[]>([])
const warehouseOptions = computed(() => warehouses.value.filter((w) => !!(w.id && String(w.id).trim())))
const tabModeDimension = ref<StockInListTabModeDimension>(readStockInListTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const stockInTableColumns = computed<CrmTableColumnDef[]>(() => {
  void vendorExtendExpanded.value
  void vendorExtendColWidth.value
  return [
  { key: 'status', label: t('stockInList.columns.status'), prop: 'status', width: 110, align: 'center' },
  {
    key: 'stockInType',
    label: t('stockInList.columns.stockInType'),
    prop: 'stockInType',
    width: 140,
    minWidth: 130,
    align: 'center',
    className: 'stock-in-type-col',
    labelClassName: 'stock-in-type-col'
  },
  { key: 'materialModel', label: t('stockInList.columns.materialModel'), minWidth: 140, showOverflowTooltip: true },
  { key: 'materialBrand', label: t('stockInList.columns.brand'), minWidth: 120, showOverflowTooltip: true },
  { key: 'warehouseName', label: t('stockInList.columns.warehouse'), minWidth: 160, showOverflowTooltip: true },
  {
    key: 'vendor',
    label: t('common.vendorExtendCol.columnTitle'),
    prop: 'vendor',
    minWidth: vendorExtendColMinWidth.value,
    width: vendorExtendColWidth.value,
    showOverflowTooltip: true,
    className: 'vendor-extend-col',
    labelClassName: 'vendor-extend-col'
  },
  { key: 'stockInDate', label: t('stockInList.columns.stockInDate'), prop: 'stockInDate', width: 160 },
  { key: 'totalQuantity', label: t('stockInList.columns.totalQuantity'), prop: 'totalQuantity', width: 110, align: 'right' },
  { key: 'hasBatchEntered', label: t('stockInList.columns.hasBatchEntered'), prop: 'hasBatchEntered', width: 120, align: 'center' },
  { key: 'totalAmount', label: t('stockInList.columns.totalAmount'), prop: 'totalAmount', width: 130, align: 'right' },
  { key: 'remark', label: t('stockInList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
  { key: 'stockInCode', label: t('stockInList.columns.stockInCode'), prop: 'stockInCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'sourceDisplayNo', label: t('stockInList.columns.sourceCode'), prop: 'sourceDisplayNo', width: 160, showOverflowTooltip: true },
  { key: 'purchaseOrderCode', label: t('stockInList.columns.purchaseOrderCode'), prop: 'purchaseOrderCode', minWidth: 160, showOverflowTooltip: true },
  {
    key: 'freightForwarderOrderNo',
    label: t('common.freightForwarderOrderNo'),
    prop: 'freightForwarderOrderNo',
    minWidth: 160,
    showOverflowTooltip: true
  },
  { key: 'salesOrderCode', label: t('stockInList.columns.salesOrderCode'), prop: 'salesOrderCode', minWidth: 170, showOverflowTooltip: true },
  { key: 'createTime', label: t('stockInList.columns.createTime'), width: 160 },
  { key: 'createUser', label: t('stockInList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('stockInList.columns.actions'),
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
]})

const warehouseNameOf = (warehouseId?: string) => {
  if (!warehouseId) return t('quoteList.na')
  const byId = warehouses.value.find(w => w.id === warehouseId)
  if (byId?.warehouseName) return byId.warehouseName
  const byCode = warehouses.value.find(w => (w.warehouseCode || '').trim() === warehouseId.trim())
  return byCode?.warehouseName || warehouseId
}

const warehouseOptionLabel = (w: WarehouseInfo) => {
  const name = (w.warehouseName || '').trim()
  const code = (w.warehouseCode || '').trim()
  if (name && code && name !== code) return `${name} (${code})`
  return name || code || w.id
}

const TAB_MODE_FILTER_I18N: Record<Exclude<StockInListTabModeDimension, 'off'>, string> = {
  warehouse: 'stockInList.filters.warehouse',
  stockInType: 'stockInList.columns.stockInType'
}

function tabModeDimensionLabel(dim: Exclude<StockInListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeStockInListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<StockInListTabModeDimension, 'off'>) {
  if (dim === 'warehouse' && !warehouseTabModeAllowed.value) {
    ElMessage.warning(
      t('stockInList.settingsMenu.warehouseTabDisabled', {
        max: INVENTORY_WAREHOUSE_TAB_MAX
      })
    )
    return
  }
  tabModeDimension.value = dim
  writeStockInListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const warehouseTabModeAllowed = computed(() => isWarehouseTabModeAllowed(warehouseOptions.value.length))

const warehouseTabStripActive = computed(
  () => tabModeDimension.value === 'warehouse' && warehouseTabModeAllowed.value
)

const filterTabStripVisible = computed(
  () => tabModeDimension.value === 'stockInType' || warehouseTabStripActive.value
)

const filterTabStripAriaLabel = computed(() => {
  if (!filterTabStripVisible.value) return ''
  return tabModeDimensionLabel(tabModeDimension.value as Exclude<StockInListTabModeDimension, 'off'>)
})

type StockInFilterTabId = StockInTypeTabId | StockInWarehouseTabId

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'stockInType') {
    return [
      { id: 'all' as const, label: t('stockInList.filterTabs.all') },
      ...STOCK_IN_TYPE_TAB_VALUES.map((value) => ({
        id: String(value) as StockInTypeTabId,
        label: listStockInTypeLabel(value)
      }))
    ]
  }
  if (dim === 'warehouse' && warehouseTabModeAllowed.value) {
    return [
      { id: 'all' as const, label: t('stockInList.filterTabs.all') },
      ...warehouseOptions.value.map((w) => ({
        id: String(w.id ?? '').trim() as StockInWarehouseTabId,
        label: warehouseOptionLabel(w)
      }))
    ]
  }
  return [] as Array<{ id: StockInFilterTabId; label: string }>
})

const activeFilterTabId = computed((): StockInFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'stockInType') return stockInTypeFilterToTab(filters.stockInType)
  if (dim === 'warehouse') return stockInWarehouseFilterToTab(filters.warehouseId)
  return 'all'
})

function onFilterTabClick(tab: StockInFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'stockInType') {
    const next = stockInTypeTabToFilter(tab as StockInTypeTabId)
    if (filters.stockInType === next) return
    filters.stockInType = next
    handleSearch()
    return
  }
  if (dim === 'warehouse') {
    const next = stockInWarehouseTabToFilter(tab)
    if (filters.warehouseId === next) return
    filters.warehouseId = next
    handleSearch()
  }
}

const filters = reactive({
  stockInCode: '',
  sourceDisplayNo: '',
  warehouseId: '',
  stockInType: undefined as number | undefined,
  stockInDateRange: [] as string[],
  remark: '',
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  freightForwarderOrderNo: '',
  salesOrderCode: ''
})

const boardFilters = computed<StockInListAnalyticsQuery>(() => ({
  stockInCode: filters.stockInCode.trim() || undefined,
  sourceDisplayNo: filters.sourceDisplayNo.trim() || undefined,
  warehouseId: filters.warehouseId || undefined,
  stockInDateStart: filters.stockInDateRange[0] || undefined,
  stockInDateEnd: filters.stockInDateRange[1] || undefined,
  remark: filters.remark.trim() || undefined,
  model: filters.model.trim() || undefined,
  vendorName: maskPurchaseSensitiveFields.value ? undefined : filters.vendorName.trim() || undefined,
  purchaseOrderCode: filters.purchaseOrderCode.trim() || undefined,
  freightForwarderOrderNo: filters.freightForwarderOrderNo.trim() || undefined,
  salesOrderCode: filters.salesOrderCode.trim() || undefined,
  stockInType: filters.stockInType
}))

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
  stockInOpsStore.setBoardMode(viewMode.value === 'board')
  if (viewMode.value === 'list') void fetchList(false)
}

const remarkDialogVisible = ref(false)
const remarkForm = reactive<{ id: string; remark: string }>({
  id: '',
  remark: ''
})

const formatNum = (v: number) => (v == null ? t('quoteList.na') : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? t('quoteList.na') : Number(v).toFixed(2))
const formatDate = (v?: string) => formatDisplayDateTime(v)

function pickRowStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal]
  return typeof v === 'string' ? v.trim() : ''
}

const stockInMaterialModelCopyValue = (row: StockInListItemDto) => {
  const r = row as unknown as Record<string, unknown>
  return pickRowStr(r, 'materialModelSummary', 'MaterialModelSummary')
}

const stockInMaterialBrandCopyValue = (row: StockInListItemDto) => {
  const r = row as unknown as Record<string, unknown>
  return pickRowStr(r, 'materialBrandSummary', 'MaterialBrandSummary')
}

/** 列表金额后展示的 ISO 币别（RMB/USD 等）；无编码时返回空串 */
const stockInCurrencyLabel = (row: StockInListItemDto) => {
  const r = row as unknown as Record<string, unknown>
  const raw = r.currencyCode ?? r.CurrencyCode
  if (raw == null || raw === '') return ''
  const n = Number(raw)
  if (Number.isNaN(n)) return ''
  return CURRENCY_CODE_TO_TEXT[n] ?? String(n)
}

const statusLabel = (s: number) => {
  switch (s) {
    case 0: return t('stockInList.status.draft')
    case 1: return t('stockInList.status.pending')
    case 2: return t('stockInList.status.done')
    case 3: return t('stockInList.status.cancelled')
    default: return t('rfqDetail.unknown')
  }
}

function listStockInTypeLabel(type: number | undefined | null): string {
  return t(`stockInList.stockInTypeLabels.${resolveStockInTypeLabelKey(type)}`)
}

function isCustomsStockIn(row: StockInListItemDto): boolean {
  return Number(row.stockInType) === StockInTypeCode.Customs
}

function arrivalNotifyTooltip(row: StockInListItemDto): string {
  const code = String(row.sourceDisplayNo ?? '').trim()
  if (!code) return ''
  return t('stockInList.arrivalNotifyCodeTooltip', { code })
}

async function ensureWarehouses() {
  if (warehouses.value.length) return
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch {
    warehouses.value = []
  }
}

function syncFiltersFromRoute() {
  if (route.name !== 'StockInList') return
  const q = route.query
  filters.model = typeof q.model === 'string' ? q.model : ''
  filters.stockInCode = typeof q.stockInCode === 'string' ? q.stockInCode : ''
  filters.sourceDisplayNo = typeof q.sourceDisplayNo === 'string' ? q.sourceDisplayNo : ''
  filters.warehouseId = typeof q.warehouseId === 'string' ? q.warehouseId : ''
  filters.remark = typeof q.remark === 'string' ? q.remark : ''
  const dateStart = typeof q.stockInDateStart === 'string' ? q.stockInDateStart : ''
  const dateEnd = typeof q.stockInDateEnd === 'string' ? q.stockInDateEnd : ''
  filters.stockInDateRange = dateStart && dateEnd ? [dateStart, dateEnd] : []
  filters.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  filters.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  filters.freightForwarderOrderNo =
    typeof q.freightForwarderOrderNo === 'string' ? q.freightForwarderOrderNo : ''
  filters.salesOrderCode = typeof q.salesOrderCode === 'string' ? q.salesOrderCode : ''
  const typeRaw = q.stockInType
  filters.stockInType =
    typeRaw === undefined || typeRaw === null || typeRaw === ''
      ? undefined
      : Number(typeRaw)
}

const fetchList = async (resetPage = true) => {
  if (resetPage) listPage.value = 1
  await ensureWarehouses()
  if (viewMode.value === 'board') return
  loading.value = true
  try {
    const paged = await stockInApi.getListPaged({
      stockInCode: filters.stockInCode || undefined,
      sourceDisplayNo: filters.sourceDisplayNo || undefined,
      warehouseId: filters.warehouseId || undefined,
      stockInDateStart: filters.stockInDateRange[0] || undefined,
      stockInDateEnd: filters.stockInDateRange[1] || undefined,
      remark: filters.remark || undefined,
      model: filters.model || undefined,
      vendorName: maskPurchaseSensitiveFields.value ? undefined : filters.vendorName || undefined,
      purchaseOrderCode: filters.purchaseOrderCode || undefined,
      freightForwarderOrderNo: filters.freightForwarderOrderNo.trim() || undefined,
      salesOrderCode: filters.salesOrderCode || undefined,
      stockInType: filters.stockInType,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = paged.items
    listTotalServer.value = paged.total
    void stockInOpsStore.refreshFromListRows(list.value, t('stockInList.opsPanel.loadFailed'))
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockInList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

function onStockInPageSizeChange() {
  listPage.value = 1
  void fetchList(false)
}

async function handleExport() {
  try {
    await ElMessageBox.confirm(
      t('stockInList.messages.exportConfirmMessage'),
      t('stockInList.messages.exportConfirmTitle'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }
  exporting.value = true
  try {
    const blob = await stockInApi.exportList({
      sourceDisplayNo: filters.sourceDisplayNo || undefined,
      warehouseId: filters.warehouseId || undefined,
      stockInDateStart: filters.stockInDateRange[0] || undefined,
      stockInDateEnd: filters.stockInDateRange[1] || undefined,
      remark: filters.remark || undefined,
      model: filters.model || undefined,
      vendorName: maskPurchaseSensitiveFields.value ? undefined : filters.vendorName || undefined,
      purchaseOrderCode: filters.purchaseOrderCode || undefined,
      freightForwarderOrderNo: filters.freightForwarderOrderNo.trim() || undefined,
      salesOrderCode: filters.salesOrderCode || undefined,
      stockInCode: filters.stockInCode || undefined,
      stockInType: filters.stockInType
    })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = withExportTimestamp('入库单列表.csv')
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('stockInList.messages.exportSuccess'))
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('stockInList.messages.exportFailed'))
  } finally {
    exporting.value = false
  }
}

watch(
  () => [route.name, route.query] as const,
  () => {
    syncFiltersFromRoute()
    if (route.name === 'StockInList' && viewMode.value === 'list') void fetchList(true)
  },
  { deep: true, immediate: true }
)

/** 与左侧检索面板共用 URL query */
const handleSearch = () => {
  resetListRightPanelOnReload(stockInOpsStore)
  const query: Record<string, string> = {}
  const m = filters.model.trim()
  if (m) query.model = m
  const sic = filters.stockInCode.trim()
  if (sic) query.stockInCode = sic
  const src = filters.sourceDisplayNo.trim()
  if (src) query.sourceDisplayNo = src
  if (filters.warehouseId) query.warehouseId = filters.warehouseId
  if (filters.stockInDateRange.length === 2 && filters.stockInDateRange[0] && filters.stockInDateRange[1]) {
    query.stockInDateStart = filters.stockInDateRange[0]
    query.stockInDateEnd = filters.stockInDateRange[1]
  }
  const rk = filters.remark.trim()
  if (rk) query.remark = rk
  const v = filters.vendorName.trim()
  if (v && !maskPurchaseSensitiveFields.value) query.vendorName = v
  const p = filters.purchaseOrderCode.trim()
  if (p) query.purchaseOrderCode = p
  const ff = filters.freightForwarderOrderNo.trim()
  if (ff) query.freightForwarderOrderNo = ff
  const s = filters.salesOrderCode.trim()
  if (s) query.salesOrderCode = s
  if (filters.stockInType !== undefined && !Number.isNaN(filters.stockInType)) {
    query.stockInType = String(filters.stockInType)
  }
  router.replace({ name: 'StockInList', query })
}

watch(listTotalServer, () => {
  const maxP = Math.max(1, Math.ceil(listTotalServer.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

const resetFilters = () => {
  resetListRightPanelOnReload(stockInOpsStore)
  filters.model = ''
  filters.stockInCode = ''
  filters.sourceDisplayNo = ''
  filters.warehouseId = ''
  filters.stockInDateRange = []
  filters.remark = ''
  filters.vendorName = ''
  filters.purchaseOrderCode = ''
  filters.freightForwarderOrderNo = ''
  filters.salesOrderCode = ''
  filters.stockInType = undefined
  router.replace({ name: 'StockInList', query: {} })
}

const handleView = (row: StockInListItemDto) => {
  const id = String(row?.id ?? (row as { Id?: string }).Id ?? '').trim()
  if (!id) {
    ElMessage.warning('无法打开详情：缺少入库单 ID')
    return
  }
  void router.push({ name: 'StockInDetail', params: { id } })
}

const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'StockInList',
  hasSelectedRow: () => !!stockInOpsStore.row,
  setRowOnly: row => stockInOpsStore.setRowOnly(row),
  selectRow: row => stockInOpsStore.selectRow(row, t('stockInList.opsPanel.loadFailed')),
  loadSelected: () => {
    void stockInOpsStore.loadAggregates(t('stockInList.opsPanel.loadFailed'))
  },
  shouldBlockRowClick: () => viewMode.value === 'board'
})

async function onRowClick(row: StockInListItemDto) {
  if (viewMode.value === 'board') return
  await onOpsPanelRowClick(row as unknown as Record<string, unknown>)
}

function opsPanelRowClassName({ row }: { row: StockInListItemDto }) {
  if (!stockInOpsStore.row) return 'table-row-pointer'
  return stockInOpsStore.rowKey(row as unknown as Record<string, unknown>) ===
    stockInOpsStore.rowKey(stockInOpsStore.row)
    ? 'so-item-row--active'
    : 'table-row-pointer'
}

onMounted(() => {
  void ensureWarehouses()
  stockInOpsStore.setBoardMode(viewMode.value === 'board')
  stockInOpsStore.registerHandlers({
    editRemark: row => handleEditRemark(row as unknown as StockInListItemDto)
  })
})

onUnmounted(() => {
  stockInOpsStore.unregisterHandlers()
  stockInOpsStore.setBoardMode(false)
})

const handleEditRemark = (row: StockInListItemDto) => {
  remarkForm.id = row.id
  remarkForm.remark = row.remark || ''
  remarkDialogVisible.value = true
}

const submitRemark = async () => {
  try {
    await stockInApi.update(remarkForm.id, { remark: remarkForm.remark })
    ElMessage.success(t('stockInList.messages.remarkUpdated'))
    remarkDialogVisible.value = false
    void fetchList(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockInList.messages.remarkUpdateFailed'))
  }
}

const handleFinish = async (row: StockInListItemDto) => {
  try {
    await stockInApi.updateStatus(row.id, 2)
    ElMessage.success(t('stockInList.messages.markDoneSuccess'))
    void fetchList(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockInList.messages.updateStatusFailed'))
  }
}

const handleDeleteRow = async (row: StockInListItemDto) => {
  try {
    await ElMessageBox.confirm(`确认删除入库单 ${row.stockInCode} 吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }
  try {
    await stockInApi.delete(row.id)
    ElMessage.success('删除成功')
    void fetchList(false)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '删除失败')
  }
}

const handleForceDeleteRow = async (row: StockInListItemDto) => {
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt('请输入入库单号以确认强制删除', '强制删除确认', {
      inputPlaceholder: row.stockInCode
    })
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered !== String(row.stockInCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await stockInApi.forceDelete(row.id, entered)
    ElMessage.success('强制删除成功')
    void fetchList(false)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '强制删除失败')
  }
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockin-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-shrink: 0;
  }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
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
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 查询栏（对齐客户列表 CustomerList.vue）----
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

.list-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  white-space: nowrap;
}

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 12px;
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

  &--filter {
    width: 160px;
  }
}

.search-select {
  width: 180px;
  &--filter {
    width: 180px;
  }
}

.search-date-range {
  width: 280px;
  &--filter {
    width: 280px;
  }
}

.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
  font-family: 'Noto Sans SC', sans-serif;
  transition: all 0.2s;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.35);
    color: $text-primary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}
.btn-export {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  border: 1px solid rgba(230, 162, 60, 0.35);
  background: rgba(230, 162, 60, 0.16);
  color: #c9902e;
  font-weight: 500;
  letter-spacing: 0.3px;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: rgba(230, 162, 60, 0.24);
    border-color: rgba(230, 162, 60, 0.5);
    color: #b8821f;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
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

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;

    &.btn-icon-only {
      width: 32px;
      padding-left: 0;
      padding-right: 0;
      justify-content: center;
    }
  }
}
.code-link {
  color: $cyan-primary;
  cursor: pointer;
  &:hover { text-decoration: underline; }
}
.text-secondary { color: $text-muted; }
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,255,255,0.05); color: $text-muted; }
  &.status-1 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-2 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-3 { background: rgba(201,87,69,0.18); color: #C95745; }
}

.batch-flag {
  font-size: 12px;
  &--yes { color: #46BF91; }
  &--no { color: $text-muted; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  margin-right: 4px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover { text-decoration: underline; }
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

.stock-in-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

:deep(.stock-in-type-col .cell) {
  overflow: visible;
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

:deep(.el-table__body tr.el-table__row.so-item-row--active > td.el-table__cell) {
  background: rgba(0, 160, 220, 0.1) !important;
}

.sil-main-panel {
  width: 100%;
}

.sil-main-panel--with-filter-tabs {
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

.sil-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.sil-filter-tabs__item {
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
</style>

<style lang="scss">
html[data-theme='dark'] .sil-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

.stock-in-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.stock-in-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.stock-in-list-settings-menu__item {
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

.stock-in-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.stock-in-list-settings-menu__submenu {
  position: relative;
}

.stock-in-list-settings-menu__flyout {
  position: absolute;
  top: 0;
  left: calc(100% + 4px);
  min-width: 148px;
  padding: 6px;
  border-radius: 8px;
  border: 1px solid var(--crm-border-panel, rgba(0, 212, 255, 0.15));
  background: var(--crm-layer-2, #0d1e35);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.28);
}
</style>

