<template>
  <!-- 业务列表页：结构对齐《业务页面规范》索引的《业务列表规范》《列表搜索栏规范》；表格皮肤见全局 crm-unified-list.scss -->
  <div class="inventory-stock-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z" />
              <polyline points="3.27 6.96 12 12.01 20.73 6.96" />
              <line x1="12" y1="22.08" x2="12" y2="12" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('inventoryStockItemList.title') }}</h1>
          <div class="count-badge">{{ t('inventoryStockItemList.count', { count: listTotal }) }}</div>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-export" :disabled="exporting" @click="() => void handleExport()">
          {{ t('inventoryStockItemList.filters.export') }}
        </button>
      </div>
    </div>

    <div v-if="drillMode === 'stagnant'" class="drill-from-board-banner" role="status">
      {{ t('inventoryStockItemList.drillFromBoard.stagnant') }}
    </div>
    <div v-else-if="drillMode === 'ranking'" class="drill-from-board-banner" role="status">
      {{ rankingDrillBannerText }}
    </div>

    <div class="stat-row">
      <el-card class="stat-card">
        <div class="stat-line">
          <span class="stat-label">{{ t('inventoryStockItemList.stats.qtyInbound') }}</span>
          <span class="stat-value inv-stat-qty">{{ formatQtyCell(qtyInboundTotal) }}</span>
        </div>
      </el-card>
      <el-card class="stat-card stat-info">
        <div class="stat-line">
          <span class="stat-label">{{ t('inventoryStockItemList.stats.qtyStockOut') }}</span>
          <span class="stat-value inv-stat-qty">{{ formatQtyCell(qtyStockOutTotal) }}</span>
        </div>
      </el-card>
      <el-card class="stat-card stat-on-hand">
        <div class="stat-line">
          <span class="stat-label">{{ t('inventoryStockItemList.stats.qtyRepertory') }}</span>
          <span class="stat-value inv-stat-qty">{{ formatQtyCell(qtyRepertoryTotal) }}</span>
        </div>
      </el-card>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'outboundStatus'"
          v-model="filters.outboundStatus"
          clearable
          :placeholder="t('inventoryStockItemList.filters.outboundStatusAll')"
          class="status-select"
          :teleported="false"
          @change="fetchList"
        >
          <el-option :label="t('inventoryStockItemList.filters.outboundNone')" :value="1" />
          <el-option :label="t('inventoryStockItemList.filters.outboundPartial')" :value="2" />
          <el-option :label="t('inventoryStockItemList.filters.outboundDone')" :value="3" />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'stockPresence'"
          v-model="filters.stockPresence"
          class="status-select status-select--stock-presence"
          :teleported="false"
          :aria-label="t('inventoryStockItemList.filters.stockPresenceField')"
          @change="fetchList"
        >
          <el-option :label="t('inventoryStockItemList.filters.stockPresenceBlank')" value="" />
          <el-option :label="t('inventoryStockItemList.filters.stockPresenceHas')" value="has" />
          <el-option :label="t('inventoryStockItemList.filters.stockPresenceNone')" value="none" />
        </el-select>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.stockInCode"
            class="search-input search-input--keyword"
            :placeholder="t('inventoryStockItemList.filters.stockInCode')"
            @keyup.enter="fetchList"
          />
        </div>
        <input
          v-model="filters.stockItemCode"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.stockItemCode')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.freightForwarderOrderNo"
          class="search-input search-input--filter"
          :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
          @keyup.enter="fetchList"
        />
        <div
          class="filter-date-range"
          role="group"
          :aria-label="t('inventoryStockItemList.filters.stockInDateRange')"
        >
          <el-date-picker
            v-model="dateFrom"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :placeholder="t('inventoryStockItemList.filters.stockInDateFrom')"
            class="filter-date-range__picker filter-date-range__picker--start"
            :teleported="false"
            @change="fetchList"
          />
          <span class="filter-date-range__sep">{{ t('inventoryStockItemList.filters.stockInDateSep') }}</span>
          <el-date-picker
            v-model="dateTo"
            type="date"
            value-format="YYYY-MM-DD"
            clearable
            :placeholder="t('inventoryStockItemList.filters.stockInDateTo')"
            class="filter-date-range__picker filter-date-range__picker--end"
            :teleported="false"
            @change="fetchList"
          />
        </div>
        <el-select
          v-if="!warehouseTabStripActive"
          v-model="filters.warehouseId"
          clearable
          :placeholder="t('inventoryStockItemList.filters.warehouse')"
          class="status-select status-select--warehouse"
          :teleported="false"
          @change="fetchList"
        >
          <el-option
            v-for="w in warehouseOptions"
            :key="w.id"
            :label="warehouseOptionLabel(w)"
            :value="w.id"
          />
        </el-select>
        <input
          v-model="filters.purchasePn"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.purchasePn')"
          @keyup.enter="fetchList"
        />
        <input
          v-model="filters.purchaseBrand"
          class="search-input search-input--filter"
          :placeholder="t('inventoryStockItemList.filters.purchaseBrand')"
          @keyup.enter="fetchList"
        />
        <template v-if="!maskSaleSensitiveFields">
          <input
            v-model="filters.customerName"
            class="search-input search-input--filter"
            :placeholder="t('inventoryStockItemList.filters.customerName')"
            @keyup.enter="fetchList"
          />
        </template>
        <template v-if="!maskPurchaseSensitiveFields">
          <input
            v-model="filters.vendorName"
            class="search-input search-input--filter"
            :placeholder="t('inventoryStockItemList.filters.vendorName')"
            @keyup.enter="fetchList"
          />
        </template>
        <template v-if="!maskSaleSensitiveFields">
          <el-select
            v-model="filters.salespersonUserId"
            clearable
            filterable
            :placeholder="t('rfqItemList.filters.allSalesUsers')"
            class="status-select status-select--sales"
            :teleported="false"
            @change="fetchList"
          >
            <el-option v-for="u in salesUsers" :key="u.id" :label="salesUserLabel(u)" :value="u.id" />
          </el-select>
        </template>
        <el-select
          v-model="filters.purchaserUserId"
          clearable
          filterable
          :placeholder="t('rfqItemList.filters.allPurchasers')"
          class="status-select status-select--purchaser"
          :teleported="false"
          @change="fetchList"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaseUserLabel(u)" :value="u.id" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="fetchList">{{ t('inventoryStockItemList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('inventoryStockItemList.filters.reset') }}</button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="isi-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('inventoryStockItemList.settingsMenu.aria')"
              :aria-label="t('inventoryStockItemList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="isi-list-settings-menu">
            <button
              type="button"
              class="isi-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('inventoryStockItemList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="isi-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="isi-list-settings-menu__item isi-list-settings-menu__item--parent">
                <span>{{ t('inventoryStockItemList.settingsMenu.tabMode') }}</span>
                <el-icon class="isi-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="isi-list-settings-menu__flyout">
                <button
                  v-for="dim in INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS"
                  :key="dim"
                  type="button"
                  class="isi-list-settings-menu__item"
                  :class="{
                    'is-active': tabModeDimension === dim,
                    'is-disabled': dim === 'warehouse' && !warehouseTabModeAllowed
                  }"
                  :disabled="dim === 'warehouse' && !warehouseTabModeAllowed"
                  :title="
                    dim === 'warehouse' && !warehouseTabModeAllowed
                      ? t('inventoryStockItemList.settingsMenu.warehouseTabDisabled', {
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

    <div class="isi-main-panel" :class="{ 'isi-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="isi-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="isi-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        :title="tab.label"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <!-- 主表：列设置齿轮 + 行高密度锚点见《业务列表规范》§1.2、§2.3；双击行见《列表双击进入详情规范》 -->
    <CrmDataTable
      ref="dataTableRef"
      class="inventory-stock-item-list-crm-table"
      column-layout-key="inventory-stock-item-list-main-v2"
      :columns="stockItemTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      @row-dblclick="onRowDblclick"
      @header-dragend="onStockItemTableHeaderDragEnd"
    >
      <template #col-outboundStatus="{ row }">
        <span class="outbound-status-chip" :class="`outbound-status-chip--${outboundStatusKind(row.outboundStatus)}`">
          <span>{{ outboundLabel(row.outboundStatus) }}</span>
        </span>
      </template>
      <template #col-stockItemCode="{ row }">
        <span class="stock-item-code-with-badge">
          <span>{{ row.stockItemCode || '—' }}</span>
          <el-tooltip
            v-if="isStockingStockItem(row)"
            :content="t('inventoryList.stockTypes.stocking')"
            placement="top"
            :hide-after="0"
          >
            <span class="inv-stock-item-code-stocking-hit" role="img" :aria-label="t('inventoryList.stockTypes.stocking')">
              <el-icon class="inv-stock-item-code-stocking-icon" aria-hidden="true">
                <Box />
              </el-icon>
            </span>
          </el-tooltip>
        </span>
      </template>
      <template #col-stockInDate="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.stockInDate)]" :key="'sid-' + row.stockItemId">
          <span v-if="!p" class="inv-list-dash">—</span>
          <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
          </span>
          <span v-else class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
        </template>
      </template>
      <template #col-warehouse="{ row }">{{ warehouseCell(row) }}</template>
      <template #col-regionType="{ row }">
        <span class="region-type-chip" :class="`region-type-chip--${regionTypeKind(row)}`">
          <span>{{ stockItemRegionLabel(row) }}</span>
        </span>
      </template>
      <template #col-qtyInbound="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.qtyInbound) }}</span>
      </template>
      <template #col-qtyStockOut="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.qtyStockOut) }}</span>
      </template>
      <template #col-qtyRepertory="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.qtyRepertory) }}</span>
      </template>
      <template #col-customerName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() ? row.customerName : '—') }}</span>
      </template>
      <template #col-salespersonName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.salespersonName?.trim() ? row.salespersonName : '—') }}</span>
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
      <template #col-profitOutBizUsd="{ row }">
        <span v-if="maskPurchaseSensitiveFields || maskSaleSensitiveFields" class="inv-list-dash">—</span>
        <template v-else-if="row.profitOutBizUsd == null">
          <span class="inv-list-dash">—</span>
        </template>
        <div v-else class="inv-list-amount-cell dock-tier-price-line">
          <template v-for="amt in [splitUsdMoneyParts(Number(row.profitOutBizUsd))]" :key="'p-' + row.stockItemId">
            <span class="inv-list-amt">
              <span class="inv-list-amt-int">{{ amt.intPart }}</span><span class="inv-list-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
        </div>
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
            <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteStockItem(row)">删除</button>
            <button v-if="canForceDelete" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteStockItem(row)">强制删除</button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="handleDeleteStockItem(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canForceDelete" divided @click.stop="handleForceDeleteStockItem(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
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
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onStockItemPageSizeChange"
        @current-change="onStockItemPageChange"
      />
    </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowRight, Box, Setting } from '@element-plus/icons-vue'
import {
  INVENTORY_STOCK_ITEM_LIST_TAB_MODE_OPTIONS,
  INVENTORY_WAREHOUSE_TAB_MAX,
  ISI_OUTBOUND_STATUS_TAB_VALUES,
  readInventoryStockItemListTabMode,
  writeInventoryStockItemListTabMode,
  isiOutboundStatusFilterToTab,
  isiOutboundStatusTabToFilter,
  isiStockPresenceFilterToTab,
  isiStockPresenceTabToFilter,
  isiWarehouseFilterToTab,
  isiWarehouseTabToFilter,
  isWarehouseTabModeAllowed,
  type InventoryStockItemListTabModeDimension,
  type IsiOutboundStatusTabId,
  type IsiStockPresenceTabId,
  type IsiWarehouseTabId
} from '@/utils/inventoryStockItemListTabMode'
import { applyStockItemListRouteQuery } from '@/utils/inventoryOnHandBoardDrill'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'
import { inventoryCenterApi, type StockItemListQuery, type StockItemListRow, type WarehouseInfo } from '@/api/inventoryCenter'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { withExportTimestamp } from '@/utils/exportFileName'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import VendorExtendColumnHeader from '@/components/list/VendorExtendColumnHeader.vue'
import VendorExtendCell from '@/components/list/VendorExtendCell.vue'
import { useVendorExtendColumn, isVendorExtendTableColumn } from '@/composables/useVendorExtendColumn'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const {
  expanded: vendorExtendExpanded,
  activeField: vendorExtendActiveField,
  colWidth: vendorExtendColWidth,
  colMinWidth: vendorExtendColMinWidth,
  setActiveField: setVendorExtendActiveField,
  applyOuterWidthFromTable: applyVendorExtendOuterWidth
} = useVendorExtendColumn()

function onStockItemTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isVendorExtendTableColumn(column)) return
  applyVendorExtendOuterWidth(newWidth)
}
const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const canForceDelete = computed(() => authStore.canForceDelete())
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const loading = ref(false)
const exporting = ref(false)
const list = ref<StockItemListRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotal = ref(0)
const qtyInboundTotal = ref(0)
const qtyStockOutTotal = ref(0)
const qtyRepertoryTotal = ref(0)
watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

function onStockItemPageSizeChange() {
  void runStockItemFetch(true)
}

function onStockItemPageChange() {
  void runStockItemFetch(false)
}

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const stockItemTableColumns = computed<CrmTableColumnDef[]>(() => {
  void vendorExtendExpanded.value
  void vendorExtendColWidth.value
  return [
  { key: 'outboundStatus', label: t('inventoryStockItemList.columns.outboundStatus'), width: 110, align: 'center' },
  { key: 'stockItemCode', label: t('inventoryStockItemList.columns.stockItemCode'), prop: 'stockItemCode', width: 168, showOverflowTooltip: true },
  { key: 'stockInCode', label: t('inventoryStockItemList.columns.stockInCode'), prop: 'stockInCode', width: 150, showOverflowTooltip: true },
  { key: 'stockInDate', label: t('inventoryStockItemList.columns.stockInDate'), prop: 'stockInDate', width: 118 },
  { key: 'warehouse', label: t('inventoryStockItemList.columns.warehouse'), minWidth: 120, showOverflowTooltip: true },
  { key: 'regionType', label: t('inventoryStockItemList.columns.regionType'), width: 100, minWidth: 100, align: 'center', showOverflowTooltip: true },
  { key: 'purchasePn', label: t('inventoryStockItemList.columns.purchasePn'), prop: 'purchasePn', minWidth: 130, showOverflowTooltip: true },
  { key: 'purchaseBrand', label: t('inventoryStockItemList.columns.purchaseBrand'), prop: 'purchaseBrand', minWidth: 100, showOverflowTooltip: true },
  {
    key: 'qtyInbound',
    label: t('inventoryStockItemList.columns.qtyInbound'),
    prop: 'qtyInbound',
    minWidth: 124,
    align: 'right',
    showOverflowTooltip: false,
    className: 'inv-stock-item-qty-col',
    labelClassName: 'inv-stock-item-qty-col'
  },
  {
    key: 'qtyStockOut',
    label: t('inventoryStockItemList.columns.qtyStockOut'),
    prop: 'qtyStockOut',
    minWidth: 124,
    align: 'right',
    showOverflowTooltip: false,
    className: 'inv-stock-item-qty-col',
    labelClassName: 'inv-stock-item-qty-col'
  },
  {
    key: 'qtyRepertory',
    label: t('inventoryStockItemList.columns.qtyRepertory'),
    prop: 'qtyRepertory',
    minWidth: 124,
    align: 'right',
    showOverflowTooltip: false,
    className: 'inv-stock-item-qty-col',
    labelClassName: 'inv-stock-item-qty-col'
  },
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
  { key: 'purchaserName', label: t('inventoryStockItemList.columns.purchaserName'), prop: 'purchaserName', width: 112, minWidth: 112, showOverflowTooltip: true },
  {
    key: 'purchaseOrderItemCode',
    label: t('inventoryStockItemList.columns.purchaseOrderItemCode'),
    prop: 'purchaseOrderItemCode',
    width: 168,
    minWidth: 168,
    showOverflowTooltip: true
  },
  {
    key: 'freightForwarderOrderNo',
    label: t('common.freightForwarderOrderNo'),
    prop: 'freightForwarderOrderNo',
    width: 160,
    minWidth: 140,
    showOverflowTooltip: true
  },
  { key: 'customerName', label: t('inventoryStockItemList.columns.customerName'), prop: 'customerName', minWidth: 120, showOverflowTooltip: true },
  {
    key: 'salespersonName',
    label: t('inventoryStockItemList.columns.salespersonName'),
    prop: 'salespersonName',
    width: 112,
    minWidth: 112,
    showOverflowTooltip: true
  },
  { key: 'sellOrderItemCode', label: t('inventoryStockItemList.columns.sellOrderItemCode'), prop: 'sellOrderItemCode', width: 120, showOverflowTooltip: true },
  { key: 'batchNo', label: t('inventoryStockItemList.columns.batchNo'), prop: 'batchNo', width: 100, showOverflowTooltip: true },
  { key: 'locationId', label: t('inventoryStockItemList.columns.locationId'), prop: 'locationId', minWidth: 100, showOverflowTooltip: true },
  {
    key: 'profitOutBizUsd',
    label: t('inventoryStockItemList.columns.profitOutBizUsd'),
    prop: 'profitOutBizUsd',
    width: 200,
    minWidth: 200,
    align: 'right'
  },
  {
    key: 'actions',
    label: t('inventoryList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    className: 'op-col',
    labelClassName: 'op-col',
    hideable: false,
    pinned: 'end',
    reorderable: false
  }
]})
const dateFrom = ref<string | null>(null)
const dateTo = ref<string | null>(null)
const drillMode = ref<'' | 'stagnant' | 'ranking'>('')
const drillRankLabel = ref('')
const drillRankPanel = ref('')
const drillRankCurrencyKey = ref('')

const rankingDrillBannerText = computed(() => {
  const panel = drillRankPanel.value || t('inventoryStockItemList.drillFromBoard.rankingDefaultPanel')
  const name = drillRankLabel.value || '—'
  const currencyKey = drillRankCurrencyKey.value
  if (currencyKey === '1') {
    return t('inventoryStockItemList.drillFromBoard.rankingAmount', { panel, name, currency: 'RMB' })
  }
  if (currencyKey === '2') {
    return t('inventoryStockItemList.drillFromBoard.rankingAmount', { panel, name, currency: 'USD' })
  }
  if (currencyKey) {
    return t('inventoryStockItemList.drillFromBoard.rankingAmount', { panel, name, currency: currencyKey })
  }
  return t('inventoryStockItemList.drillFromBoard.rankingQty', { panel, name })
})

const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])
const warehouses = ref<WarehouseInfo[]>([])
const warehouseOptions = computed(() => warehouses.value.filter((w) => !!(w.id && String(w.id).trim())))

const tabModeDimension = ref<InventoryStockItemListTabModeDimension>(
  readInventoryStockItemListTabMode()
)
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)

const TAB_MODE_FILTER_I18N: Record<
  Exclude<InventoryStockItemListTabModeDimension, 'off'>,
  string
> = {
  outboundStatus: 'inventoryStockItemList.filters.outboundStatus',
  stockPresence: 'inventoryStockItemList.filters.stockPresenceField',
  warehouse: 'inventoryStockItemList.filters.warehouse'
}

function tabModeDimensionLabel(dim: Exclude<InventoryStockItemListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeInventoryStockItemListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<InventoryStockItemListTabModeDimension, 'off'>) {
  if (dim === 'warehouse' && !warehouseTabModeAllowed.value) {
    ElMessage.warning(
      t('inventoryStockItemList.settingsMenu.warehouseTabDisabled', {
        max: INVENTORY_WAREHOUSE_TAB_MAX
      })
    )
    return
  }
  tabModeDimension.value = dim
  writeInventoryStockItemListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const warehouseTabModeAllowed = computed(() =>
  isWarehouseTabModeAllowed(warehouseOptions.value.length)
)

const warehouseTabStripActive = computed(
  () => tabModeDimension.value === 'warehouse' && warehouseTabModeAllowed.value
)

const filterTabStripVisible = computed(
  () =>
    tabModeDimension.value === 'outboundStatus' ||
    tabModeDimension.value === 'stockPresence' ||
    (tabModeDimension.value === 'warehouse' && warehouseTabModeAllowed.value)
)

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  if (tabModeDimension.value === 'warehouse' && !warehouseTabModeAllowed.value) return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

type IsiFilterTabId = IsiOutboundStatusTabId | IsiStockPresenceTabId | IsiWarehouseTabId

const filters = reactive({
  stockInCode: '',
  stockItemCode: '',
  freightForwarderOrderNo: '',
  purchasePn: '',
  purchaseBrand: '',
  warehouseId: '',
  outboundStatus: undefined as number | undefined,
  /** 是否有库存：''=不限，has=&gt;0，none===0 */
  stockPresence: '' as '' | 'has' | 'none',
  customerName: '',
  vendorName: '',
  salespersonUserId: undefined as string | undefined,
  purchaserUserId: undefined as string | undefined,
  stockType: undefined as number | undefined,
  stagnantOnly: false,
  rankDimension: '',
  rankKey: '',
  rankCurrency: undefined as number | undefined
})

function outboundStatusFilterLabel(n: number) {
  if (n === 1) return t('inventoryStockItemList.filters.outboundNone')
  if (n === 2) return t('inventoryStockItemList.filters.outboundPartial')
  if (n === 3) return t('inventoryStockItemList.filters.outboundDone')
  return String(n)
}

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'outboundStatus') {
    return [
      { id: 'all' as const, label: t('inventoryStockItemList.filterTabs.all') },
      ...ISI_OUTBOUND_STATUS_TAB_VALUES.map((value) => ({
        id: String(value) as IsiOutboundStatusTabId,
        label: outboundStatusFilterLabel(value)
      }))
    ]
  }
  if (dim === 'stockPresence') {
    return [
      { id: 'all' as const, label: t('inventoryStockItemList.filterTabs.all') },
      {
        id: 'has' as const,
        label: t('inventoryStockItemList.filters.stockPresenceHas')
      },
      {
        id: 'none' as const,
        label: t('inventoryStockItemList.filters.stockPresenceNone')
      }
    ]
  }
  if (dim === 'warehouse' && warehouseTabModeAllowed.value) {
    return [
      { id: 'all' as const, label: t('inventoryStockItemList.filterTabs.all') },
      ...warehouseOptions.value.map((w) => ({
        id: String(w.id) as IsiWarehouseTabId,
        label: warehouseOptionLabel(w)
      }))
    ]
  }
  return [] as Array<{ id: IsiFilterTabId; label: string }>
})

const activeFilterTabId = computed((): IsiFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'outboundStatus') return isiOutboundStatusFilterToTab(filters.outboundStatus)
  if (dim === 'stockPresence') return isiStockPresenceFilterToTab(filters.stockPresence)
  if (dim === 'warehouse') return isiWarehouseFilterToTab(filters.warehouseId)
  return 'all'
})

function onFilterTabClick(tab: IsiFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'outboundStatus') {
    const next = isiOutboundStatusTabToFilter(tab as IsiOutboundStatusTabId)
    if (filters.outboundStatus === next) return
    filters.outboundStatus = next
    fetchList()
    return
  }
  if (dim === 'stockPresence') {
    const next = isiStockPresenceTabToFilter(tab as IsiStockPresenceTabId)
    if (filters.stockPresence === next) return
    filters.stockPresence = next
    fetchList()
    return
  }
  if (dim === 'warehouse' && warehouseTabModeAllowed.value) {
    const next = isiWarehouseTabToFilter(tab as IsiWarehouseTabId)
    if (filters.warehouseId === next) return
    filters.warehouseId = next
    fetchList()
  }
}

function salesUserLabel(u: SalesUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}(${u.userName})` : name
}

function purchaseUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}(${u.userName})` : name
}

function buildQuery(): StockItemListQuery {
  const q: StockItemListQuery = {
    stockInCode: filters.stockInCode.trim() || undefined,
    stockItemCode: filters.stockItemCode.trim() || undefined,
    freightForwarderOrderNo: filters.freightForwarderOrderNo.trim() || undefined,
    stockInDateFrom: dateFrom.value?.trim() || undefined,
    stockInDateTo: dateTo.value?.trim() || undefined,
    warehouseId: filters.warehouseId.trim() || undefined,
    purchasePn: filters.purchasePn.trim() || undefined,
    purchaseBrand: filters.purchaseBrand.trim() || undefined,
    outboundStatus: filters.outboundStatus,
    vendorName: filters.vendorName.trim() || undefined,
    purchaserUserId: filters.purchaserUserId?.trim() || undefined
  }
  if (filters.stockPresence === 'has') q.repertoryHasStock = true
  else if (filters.stockPresence === 'none') q.repertoryHasStock = false
  if (filters.stockType != null && filters.stockType >= 1 && filters.stockType <= 3) {
    q.stockType = filters.stockType
  }
  if (filters.stagnantOnly) q.stagnantOnly = true
  if (filters.rankDimension.trim()) q.rankDimension = filters.rankDimension.trim()
  if (filters.rankKey.trim()) q.rankKey = filters.rankKey.trim()
  if (filters.rankCurrency != null && filters.rankCurrency >= 1) {
    q.rankCurrency = filters.rankCurrency
  }
  if (!maskSaleSensitiveFields.value) {
    q.customerName = filters.customerName.trim() || undefined
    q.salespersonUserId = filters.salespersonUserId?.trim() || undefined
  }
  return q
}

async function runStockItemFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const res = await inventoryCenterApi.searchStockItems({
      ...buildQuery(),
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = res.items
    listTotal.value = res.total
    qtyInboundTotal.value = Number(res.qtyInboundTotal ?? 0)
    qtyStockOutTotal.value = Number(res.qtyStockOutTotal ?? 0)
    qtyRepertoryTotal.value = Number(res.qtyRepertoryTotal ?? 0)
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryStockItemList.messages.loadFailed')))
    list.value = []
    listTotal.value = 0
    qtyInboundTotal.value = 0
    qtyStockOutTotal.value = 0
    qtyRepertoryTotal.value = 0
  } finally {
    loading.value = false
  }
}

const fetchList = () => void runStockItemFetch(true)

const resetFilters = () => {
  filters.stockInCode = ''
  filters.stockItemCode = ''
  filters.freightForwarderOrderNo = ''
  filters.purchasePn = ''
  filters.purchaseBrand = ''
  filters.warehouseId = ''
  filters.outboundStatus = undefined
  filters.stockPresence = ''
  filters.customerName = ''
  filters.vendorName = ''
  filters.salespersonUserId = undefined
  filters.purchaserUserId = undefined
  filters.stockType = undefined
  filters.stagnantOnly = false
  filters.rankDimension = ''
  filters.rankKey = ''
  filters.rankCurrency = undefined
  drillMode.value = ''
  drillRankLabel.value = ''
  drillRankPanel.value = ''
  drillRankCurrencyKey.value = ''
  dateFrom.value = null
  dateTo.value = null
  void router.replace({ name: 'InventoryStockItemList', query: {} })
  void fetchList()
}

async function handleExport() {
  try {
    await ElMessageBox.confirm(
      t('inventoryStockItemList.messages.exportConfirmMessage'),
      t('inventoryStockItemList.messages.exportConfirmTitle'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }
  exporting.value = true
  try {
    const blob = await inventoryCenterApi.exportStockItems(buildQuery())
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = withExportTimestamp('库存明细列表.csv')
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success(t('inventoryStockItemList.messages.exportSuccess'))
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('inventoryStockItemList.messages.exportFailed'))
  } finally {
    exporting.value = false
  }
}

/** 仅日期（无时区时刻）时入库日常为 00:00，不重复展示时分 */
function isTimeMidnightOnly(time: string) {
  const t0 = (time || '').trim()
  return t0 === '00:00' || t0.startsWith('00:00:')
}

/** 数量列：与《业务列表规范》§3.2 一致 */
const stockItemRegionLabel = (row: StockItemListRow) => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

const regionTypeKind = (row: StockItemListRow): 'domestic' | 'overseas' => {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? 'overseas' : 'domestic'
}

const formatQtyCell = (v: unknown) => {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

const splitUsdMoneyParts = (n: number): { intPart: string; fracPart: string } => {
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).formatToParts(n)
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) fracPart = '.00'
  return { intPart, fracPart }
}

/** 备货库存：<c>stock_type === 2</c>，与汇总库存列表一致 */
function isStockingStockItem(row: StockItemListRow): boolean {
  return Number(row.stockType ?? 0) === 2
}

const outboundLabel = (s: number) => {
  if (s === 1) return t('inventoryStockItemList.filters.outboundNone')
  if (s === 2) return t('inventoryStockItemList.filters.outboundPartial')
  if (s === 3) return t('inventoryStockItemList.filters.outboundDone')
  return '—'
}

const outboundStatusKind = (s: number): 'none' | 'partial' | 'done' | 'unknown' => {
  if (s === 1) return 'none'
  if (s === 2) return 'partial'
  if (s === 3) return 'done'
  return 'unknown'
}

const warehouseOptionLabel = (w: WarehouseInfo) => {
  const name = (w.warehouseName || '').trim()
  const code = (w.warehouseCode || '').trim()
  if (name && code) return `${name}（${code}）`
  return name || code || '—'
}

const warehouseCell = (row: StockItemListRow) => {
  const code = row.warehouseCode?.trim()
  if (code) return code
  return t('quoteList.na')
}

const onRowDblclick = (row: StockItemListRow) => {
  const sid = (row.stockAggregateId || '').trim()
  if (!sid) {
    ElMessage.warning(t('inventoryStockItemList.messages.missingAggregateId'))
    return
  }
  router.push({
    path: `/inventory/stocks/${encodeURIComponent(sid)}`,
    query: {
      materialId: row.materialId || undefined,
      stockCode: undefined,
      materialModel: row.purchasePn || undefined,
      materialBrand: row.purchaseBrand || undefined,
      warehouseId: row.warehouseId || undefined
    }
  })
}

const handleDeleteStockItem = async (row: StockItemListRow) => {
  const sid = String(row.stockItemId || '').trim()
  if (!sid) return
  try {
    await ElMessageBox.confirm(`确认删除库存明细 ${row.stockItemCode || sid} 吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }
  try {
    await inventoryCenterApi.deleteStockItem(sid)
    ElMessage.success('删除成功')
    fetchList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '删除失败'))
  }
}

const handleForceDeleteStockItem = async (row: StockItemListRow) => {
  const sid = String(row.stockItemId || '').trim()
  if (!sid) return
  const expectCode = String(row.stockItemCode || sid).trim()
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt('请输入库存明细编号以确认强制删除', '强制删除确认', {
      inputPlaceholder: expectCode
    })
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered !== expectCode) {
    ElMessage.error('输入编号不匹配，已取消')
    return
  }
  try {
    await inventoryCenterApi.forceDeleteStockItem(sid, entered)
    ElMessage.success('强制删除成功')
    fetchList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '强制删除失败'))
  }
}

onMounted(async () => {
  applyStockItemListRouteQuery(route.query as Record<string, unknown>, {
    filters,
    dateFrom,
    dateTo,
    drillMode,
    drillRankLabel,
    drillRankPanel,
    drillRankCurrencyKey
  })
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch {
    warehouses.value = []
  }
  try {
    salesUsers.value = await authApi.getSalesUsersForSelect()
  } catch {
    salesUsers.value = []
  }
  try {
    purchaseUsers.value = await authApi.getPurchaseUsersForSelect()
  } catch {
    purchaseUsers.value = []
  }
  void fetchList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-stock-item-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  gap: 12px;
}

.drill-from-board-banner {
  margin: -8px 0 16px;
  padding: 10px 14px;
  border-radius: $border-radius-md;
  background: rgba(64, 158, 255, 0.08);
  border: 1px solid rgba(64, 158, 255, 0.22);
  color: #409eff;
  font-size: 13px;
  line-height: 1.5;
}

.header-right {
  flex-shrink: 0;
  min-height: 1px;
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

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  display: flex;
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
}

.stat-row {
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 20px;
}

.stat-card {
  flex: 0 0 auto;
  width: 320px;
  max-width: 100%;
  background: $layer-3;
  border: 1px solid $border-card;
  :deep(.el-card__body) {
    padding: 12px 15px;
    box-sizing: border-box;
  }
  .stat-line {
    display: grid;
    grid-template-columns: 1fr;
    align-items: baseline;
    width: 100%;
    min-height: 32px;
  }
  .stat-label {
    grid-area: 1 / 1;
    justify-self: start;
    z-index: 1;
    font-size: 14px;
    color: $text-muted;
    white-space: nowrap;
  }
  .stat-value {
    grid-area: 1 / 1;
    justify-self: center;
    font-size: 24px;
    font-weight: bold;
    color: $cyan-primary;
  }
  &.stat-info .stat-value {
    color: $info-color;
  }
  &.stat-on-hand .stat-value {
    color: #b8821f;
  }
}

.inv-stat-qty {
  font-variant-numeric: tabular-nums;
}

.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
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
}

.search-input--keyword {
  width: 200px;
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

.status-select--sales {
  width: 150px;
}

.status-select--purchaser {
  width: 168px;
  min-width: 140px;
}

.status-select--warehouse {
  width: 190px;
}

.status-select--stock-presence {
  width: 128px;
  min-width: 120px;
}

.stock-item-code-with-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.inv-stock-item-code-stocking-hit {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  cursor: default;
  line-height: 1;
}

.inv-stock-item-code-stocking-icon {
  font-size: 16px;
  color: #e6a23c;
}

html[data-theme='dark'] .inv-stock-item-code-stocking-icon {
  color: #ebb563;
}

.outbound-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  justify-content: center;
  min-width: 56px;
  padding: 3px 10px;
  border-radius: 5px;
  font-size: 12px;
  line-height: 1.1;
  font-weight: 400;
  color: #fff;
  border: none;
  white-space: nowrap;
}

.outbound-status-chip--none {
  background: #9ca3af;
}

.outbound-status-chip--partial {
  background: #e6a23c;
}

.outbound-status-chip--done {
  background: #67c23a;
}

.outbound-status-chip--unknown {
  background: #9ca3af;
}

.region-type-chip {
  display: inline-flex;
  align-items: center;
  gap: 0;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  line-height: 1.2;
}

.region-type-chip--domestic {
  color: #e6a23c;
  background: rgba(230, 162, 60, 0.14);
}

.region-type-chip--overseas {
  color: #409eff;
  background: rgba(64, 158, 255, 0.14);
}

.inv-list-qty {
  display: inline-block;
  max-width: 100%;
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

html[data-theme='dark'] .inv-list-qty {
  color: $text-primary;
}

.inv-list-dash {
  color: $text-muted;
}

.inv-list-amount-cell {
  display: inline-flex;
  align-items: baseline;
  justify-content: flex-end;
  flex-wrap: nowrap;
  width: 100%;
  font-size: 12px;
  line-height: 1.4;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.inv-list-amt-int,
.inv-list-amt-frac {
  font-weight: 700;
  color: #27292c;
}

html[data-theme='dark'] .inv-list-amt-int,
html[data-theme='dark'] .inv-list-amt-frac {
  color: $text-primary;
}

.search-input--filter {
  width: 140px;
  padding: 6px 10px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-sm;
  background: $layer-2;
  color: $text-primary;
  font-size: 13px;
}

.filter-date-range {
  display: inline-flex;
  align-items: stretch;
  border: 1px solid $border-panel;
  border-radius: $border-radius-sm;
  background: $layer-2;
  overflow: hidden;
  vertical-align: middle;
}

.filter-date-range__sep {
  display: inline-flex;
  align-items: center;
  padding: 0 2px;
  font-size: 12px;
  color: $text-muted;
  flex-shrink: 0;
  user-select: none;
  border-left: 1px solid $border-panel;
  border-right: 1px solid $border-panel;
}

.filter-date-range__picker {
  width: 108px;
  flex: 0 0 108px;
  max-width: 108px;
}

.filter-date-range__picker--start :deep(.el-input__wrapper),
.filter-date-range__picker--end :deep(.el-input__wrapper) {
  box-shadow: none !important;
  border: none !important;
  border-radius: 0 !important;
  background: transparent;
  padding-left: 4px;
  padding-right: 4px;
}

.filter-date-range__picker--start :deep(.el-input__prefix),
.filter-date-range__picker--end :deep(.el-input__prefix) {
  margin-inline-end: 2px;
}

.filter-date-range__picker :deep(.el-input__inner) {
  font-variant-numeric: tabular-nums;
}

.btn-primary,
.btn-ghost {
  padding: 6px 14px;
  border-radius: $border-radius-sm;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}

.btn-icon-only {
  width: 32px;
  padding-left: 0;
  padding-right: 0;
  justify-content: center;
  display: inline-flex;
  align-items: center;
}

.isi-main-panel {
  width: 100%;
}

.isi-main-panel--with-filter-tabs {
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

.isi-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.isi-filter-tabs__item {
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
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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

html[data-theme='dark'] .isi-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

.btn-primary {
  background: $primary-color;
  color: #fff;
}

.btn-ghost {
  background: transparent;
  border-color: $border-panel;
  color: $text-secondary;
}

.action-btns {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.action-btn {
  background: transparent;
  border: none;
  cursor: pointer;
  font-size: 12px;
  padding: 0;
}

.action-btn--danger {
  color: $color-red-brown;
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

// ---- 表格：.table-wrapper / CrmDataTable 全局样式见 assets/styles/crm-unified-list.scss ----
.inventory-stock-item-list-crm-table.table-wrapper {
  :deep(.el-table .cell) {
    line-height: 1.2;
  }

  /* 入库量 / 已出库 / 在库：列宽与单元格勿省略，标题与千分位数字完整展示 */
  :deep(.el-table th.inv-stock-item-qty-col .cell),
  :deep(.el-table td.inv-stock-item-qty-col .cell) {
    overflow: visible;
    text-overflow: clip;
    white-space: nowrap;
  }
}
</style>

<style lang="scss">
.isi-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.isi-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.isi-list-settings-menu__item {
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

  &:disabled,
  &.is-disabled {
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

.isi-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.isi-list-settings-menu__submenu {
  position: relative;
}

.isi-list-settings-menu__flyout {
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
