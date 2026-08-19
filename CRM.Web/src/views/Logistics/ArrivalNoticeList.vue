<template>
  <div class="arrival-notice-list-page">
    <div class="page-header">
      <h2>{{ t('arrivalNoticeList.title') }}</h2>
      <div class="ops">
        <el-button @click="refreshArrivalList">{{ t('arrivalNoticeList.refresh') }}</el-button>
      </div>
    </div>

    <!-- 搜索栏：与客户列表 CustomerList 同款布局与控件皮肤 -->
    <div class="search-bar">
      <div v-if="activePreset" class="search-preset-chip-row">
        <span class="search-preset-chip">
          {{ t(presetI18nKey(activePreset)) }}
          <button
            type="button"
            class="search-preset-chip__clear"
            :title="t('arrivalNoticeList.searchPanel.clearPreset')"
            @click="clearPresetChip"
          >×</button>
        </span>
      </div>
      <div class="search-left">
        <el-select
          v-if="tabModeDimension !== 'status' && !presetConflictsStatus"
          v-model="filters.status"
          :placeholder="t('arrivalNoticeList.filters.allStatus')"
          clearable
          class="status-select"
          :teleported="false"
        >
          <el-option :label="t('arrivalNoticeList.status.new')" :value="1" />
          <el-option :label="t('arrivalNoticeList.status.notArrived')" :value="10" />
          <el-option :label="t('arrivalNoticeList.status.pendingQc')" :value="20" />
          <el-option :label="t('arrivalNoticeList.status.qcDone')" :value="30" />
          <el-option :label="t('arrivalNoticeList.status.stocked')" :value="100" />
        </el-select>
        <el-select
          v-if="tabModeDimension !== 'stockInType' && !presetConflictsStockInType"
          v-model="filters.stockInType"
          clearable
          :placeholder="t('arrivalNoticeList.filters.arrivalTypePlaceholder')"
          class="arrival-type-select"
          :teleported="false"
        >
          <el-option
            v-for="v in STOCK_IN_TYPE_FILTER_VALUES"
            :key="v"
            :label="arrivalTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.purchaseOrderCode"
            class="search-input"
            :placeholder="t('arrivalNoticeList.filters.poCodePlaceholder')"
            @keyup.enter="runSearch"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.freightForwarderOrderNo"
            class="search-input"
            :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
            @keyup.enter="runSearch"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.pn"
            class="search-input"
            :placeholder="t('arrivalNoticeList.filters.pnPlaceholder')"
            @keyup.enter="runSearch"
          />
        </div>
        <div v-if="!maskPurchaseSensitiveFields" class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.vendorName"
            class="search-input"
            :placeholder="t('arrivalNoticeList.filters.vendorPlaceholder')"
            @keyup.enter="runSearch"
          />
        </div>
        <el-select
          v-model="filters.purchaseCurrency"
          clearable
          :placeholder="t('arrivalNoticeList.filters.purchaseCurrencyPlaceholder')"
          class="purchase-currency-select"
          :teleported="false"
        >
          <el-option
            v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-date-picker
          v-if="!presetHidesExpectedDate"
          v-model="filters.expectedArrivalDate"
          type="date"
          value-format="YYYY-MM-DD"
          :placeholder="t('arrivalNoticeList.filters.datePlaceholder')"
          clearable
          class="filter-date-single"
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="runSearch">
          {{ t('arrivalNoticeList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('arrivalNoticeList.filters.reset') }}
        </button>
        <el-popover
          v-model:visible="settingsMenuOpen"
          trigger="click"
          placement="bottom-end"
          :width="168"
          :show-arrow="false"
          popper-class="arrival-notice-list-settings-popper"
        >
          <template #reference>
            <button
              type="button"
              class="btn-ghost btn-sm btn-icon-only"
              :title="t('arrivalNoticeList.settingsMenu.aria')"
              :aria-label="t('arrivalNoticeList.settingsMenu.aria')"
            >
              <el-icon :size="14"><Setting /></el-icon>
            </button>
          </template>
          <div class="arrival-notice-list-settings-menu">
            <button
              type="button"
              class="arrival-notice-list-settings-menu__item"
              :disabled="tabModeDimension === 'off'"
              @click="closeFilterTabMode"
            >
              {{ t('arrivalNoticeList.settingsMenu.closeTabs') }}
            </button>
            <div
              class="arrival-notice-list-settings-menu__submenu"
              @mouseenter="settingsSubmenuOpen = true"
              @mouseleave="settingsSubmenuOpen = false"
            >
              <div class="arrival-notice-list-settings-menu__item arrival-notice-list-settings-menu__item--parent">
                <span>{{ t('arrivalNoticeList.settingsMenu.tabMode') }}</span>
                <el-icon class="arrival-notice-list-settings-menu__caret"><ArrowRight /></el-icon>
              </div>
              <div v-show="settingsSubmenuOpen" class="arrival-notice-list-settings-menu__flyout">
                <button
                  v-for="dim in visibleTabModeMenuOptions"
                  :key="dim"
                  type="button"
                  class="arrival-notice-list-settings-menu__item"
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

    <div class="sol-main-panel" :class="{ 'sol-main-panel--with-filter-tabs': filterTabStripVisible }">
    <div
      v-if="filterTabStripVisible"
      class="sol-filter-tabs"
      role="tablist"
      :aria-label="filterTabStripAriaLabel"
    >
      <button
        v-for="tab in filterTabOptions"
        :key="tab.id"
        type="button"
        role="tab"
        class="sol-filter-tabs__item"
        :class="{ 'is-active': activeFilterTabId === tab.id }"
        :aria-selected="activeFilterTabId === tab.id"
        @click="onFilterTabClick(tab.id)"
      >
        {{ tab.label }}
      </button>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="arrival-notice-list-main"
      :columns="arrivalNoticeColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      :row-class-name="opsPanelRowClassName"
      v-loading="loading"
      @row-click="onRowClick"
    >
      <template #col-status="{ row }">
        <el-tag effect="dark" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
      </template>
      <template #col-stockInType="{ row }">
        <StockBizTypeTag
          biz="in"
          :type="row.stockInType"
          :customs-declaration-id="row.customsDeclarationId"
          :customs-declaration-code="row.customsDeclarationCode"
        />
      </template>
      <template #col-pn="{ row }">
        <CrmListCopyableTextCell :text="rawPn(row)" />
      </template>
      <template #col-brand="{ row }">
        <CrmListCopyableTextCell :text="rawBrand(row)" />
      </template>
      <template #col-expectedArrivalDate="{ row }">{{ formatExpected(row.expectedArrivalDate) }}</template>
      <template #col-actualArrivalDate="{ row }">{{ formatExpected(row.actualArrivalDate) }}</template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(pickShipmentMethod(row)) }}</template>
      <template #col-courierTrackingNo="{ row }">{{ displayCourierTrackingNo(row) }}</template>
      <template #col-regionType="{ row }">{{ regionTypeLabel(row) }}</template>
      <template #col-vendorName="{ row }">
        <vendor-name-readonly-text
          :name-zh="row.vendorName"
          :name-en="row.vendorEnglishName"
          :masked="maskPurchaseSensitiveFields"
        />
      </template>
      <template #col-expectQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(expectQty(row)) }}</span>
      </template>
      <template #col-receiveQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(receiveQty(row)) }}</span>
      </template>
      <template #col-passedQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(passedQty(row)) }}</span>
      </template>
      <template #col-createTime="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime)]" :key="'ct-' + row.id">
          <span v-if="p" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
          <span v-else class="inv-list-dash">—</span>
        </template>
      </template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '--' }}</template>
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
            <button
              v-if="row.status === 10"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="markArrived(row)"
            >
              {{ t('arrivalNoticeList.actions.confirmArrived') }}
            </button>
            <button
              v-if="row.status === 20"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="goCreateQc(row)"
            >
              {{ t('arrivalNoticeList.actions.qc') }}
            </button>
            <button
              v-if="canEditArrivalInfo"
              type="button"
              class="action-btn action-btn--info"
              @click.stop="openArrivalInfoDialog(row)"
            >
              {{ t('arrivalNoticeList.actions.editArrivalInfo') }}
            </button>
            <button type="button" class="action-btn action-btn--info" @click.stop="viewItems(row)">
              {{ t('arrivalNoticeList.actions.detail') }}
            </button>
            <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
            <button v-if="canForceDelete" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteRow(row)">强制删除</button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="row.status === 10" @click.stop="markArrived(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('arrivalNoticeList.actions.confirmArrived') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 20" @click.stop="goCreateQc(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('arrivalNoticeList.actions.qc') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canEditArrivalInfo" @click.stop="openArrivalInfoDialog(row)">
                  <span class="op-more-item op-more-item--info">{{ t('arrivalNoticeList.actions.editArrivalInfo') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="viewItems(row)">
                  <span class="op-more-item op-more-item--info">{{ t('arrivalNoticeList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item divided @click.stop="handleDeleteRow(row)">
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
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('arrivalNoticeList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('arrivalNoticeList.columnSettings')"
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
        @current-change="() => void applyArrivalList(false)"
        @size-change="onArrivalPageSizeChange"
      />
    </div>
    </div>

    <el-dialog
      v-model="itemsVisible"
      :title="t('arrivalNoticeList.detailDialog.title')"
      width="720px"
      align-center
      destroy-on-close
      class="arrival-detail-dialog"
      @closed="onDetailClosed"
    >
      <el-descriptions
        v-if="detailNotice"
        :column="2"
        border
        size="small"
        class="arrival-detail-desc"
        :label-style="arrivalDetailLabelStyle"
      >
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.vendorName')">
          <vendor-name-readonly-text
            :name-zh="detailNotice.vendorName"
            :name-en="detailNotice.vendorEnglishName"
            :masked="maskPurchaseSensitiveFields"
          />
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.vendorCode')">
          {{ maskPurchaseSensitiveFields ? '—' : (detailNotice.vendorCode?.trim() || '—') }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.purchaseOrderCode')">
          {{ detailNotice.purchaseOrderCode?.trim() || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.pn')">
          {{ displayPn(detailNotice) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.brand')">
          {{ displayBrand(detailNotice) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.expectedArrivalDate')">
          {{ formatExpected(detailNotice.expectedArrivalDate) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.regionType')">
          {{ regionTypeLabel(detailNotice) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.purchaser')">
          {{ detailNotice.purchaseUserName?.trim() || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.noticeQty')">
          {{ formatQtyCell(expectQty(detailNotice)) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.receivedQty')">
          {{ formatQtyCell(receiveQty(detailNotice)) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.passedQty')">
          {{ formatQtyCell(passedQty(detailNotice)) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.stockInQty')">
          {{ stockInQtyText(detailNotice) }}
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>

    <ArrivalNoticeArrivalInfoDialog
      v-model:visible="arrivalInfoDialogVisible"
      :notice="arrivalInfoNotice"
      @saved="onArrivalInfoSaved"
    />

  </div>
</template>

<script setup lang="ts">
import { computed, inject, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting, ArrowRight } from '@element-plus/icons-vue'
import { logisticsApi, type StockInNotifyDto, type StockInNotifyItemDto } from '@/api/logistics'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { useRouter, useRoute } from 'vue-router'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { buildArrivalNoticeListColumns } from '@/composables/buildArrivalNoticeListColumns'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { useAuthStore } from '@/stores/auth'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { STOCK_IN_TYPE_FILTER_VALUES, resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { SETTLEMENT_CURRENCY_OPTIONS, CurrencyCode } from '@/constants/currency'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { resetListRightPanelOnReload } from '@/composables/useListRightPanelReset'
import { useArrivalNoticeOpsPanelStore } from '@/stores/arrivalNoticeOpsPanel'
import ArrivalNoticeArrivalInfoDialog from '@/components/Logistics/ArrivalNoticeArrivalInfoDialog.vue'
import { canEditArrivalNoticeArrivalInfo } from '@/utils/arrivalNoticeArrivalInfoAccess'
import {
  ARRIVAL_NOTICE_LIST_TAB_MODE_OPTIONS,
  ARRIVAL_NOTICE_STATUS_TAB_VALUES,
  ARRIVAL_NOTICE_STOCK_IN_TYPE_TAB_VALUES,
  arrivalNoticeStatusFilterToTab,
  arrivalNoticeStatusTabToFilter,
  arrivalNoticeStockInTypeFilterToTab,
  arrivalNoticeStockInTypeTabToFilter,
  readArrivalNoticeListTabMode,
  writeArrivalNoticeListTabMode,
  type ArrivalNoticeListTabModeDimension,
  type ArrivalNoticeStatusTabId,
  type ArrivalNoticeStockInTypeTabId
} from '@/utils/arrivalNoticeListTabMode'
import {
  buildArrivalNoticeListRouteQuery,
  isArrivalNoticeListPresetId,
  presetConflictsStatusField,
  presetConflictsStatusTab,
  presetConflictsStockInTypeField,
  presetConflictsStockInTypeTab,
  presetHidesExpectedDateField,
  presetI18nKey,
  type ArrivalNoticeListPresetId
} from '@/utils/arrivalNoticeListPreset'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const authStore = useAuthStore()
const arrivalNoticeOpsStore = useArrivalNoticeOpsPanelStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const canForceDelete = computed(() => authStore.canForceDelete())
const canEditArrivalInfo = computed(() => canEditArrivalNoticeArrivalInfo(authStore.user))
const arrivalInfoDialogVisible = ref(false)
const arrivalInfoNotice = ref<StockInNotifyDto | null>(null)
const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions } = useLogisticsFormDict()
const loading = ref(false)
const tabModeDimension = ref<ArrivalNoticeListTabModeDimension>(readArrivalNoticeListTabMode())
const settingsMenuOpen = ref(false)
const settingsSubmenuOpen = ref(false)
const list = ref<StockInNotifyDto[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotal = ref(0)
watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 200
const OP_COL_EXPANDED_MIN_WIDTH = 180
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const arrivalNoticeColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildArrivalNoticeListColumns({
    t,
    opColWidth: opColWidth.value,
    opColMinWidth: opColMinWidth.value,
    withActions: true
  })
})

const itemsVisible = ref(false)
const detailNotice = ref<StockInNotifyDto | null>(null)

/** 标签区至少单行容纳 6 个汉字 */
const arrivalDetailLabelStyle = { minWidth: '8.5em', whiteSpace: 'nowrap' as const }
const filters = ref<{
  status?: number
  stockInType?: number
  purchaseOrderCode: string
  freightForwarderOrderNo: string
  pn: string
  vendorName: string
  purchaseCurrency?: number
  expectedArrivalDate: string
}>({
  status: undefined,
  stockInType: undefined,
  purchaseOrderCode: '',
  freightForwarderOrderNo: '',
  pn: '',
  vendorName: '',
  purchaseCurrency: undefined,
  expectedArrivalDate: ''
})

const activePreset = computed((): ArrivalNoticeListPresetId | null => {
  const p = route.query.preset
  return typeof p === 'string' && isArrivalNoticeListPresetId(p) ? p : null
})

const presetActive = computed(() => !!activePreset.value)

const presetConflictsStatus = computed(() => {
  const p = activePreset.value
  return p ? presetConflictsStatusField(p) : false
})

const presetConflictsStockInType = computed(() => {
  const p = activePreset.value
  return p ? presetConflictsStockInTypeField(p) : false
})

const presetHidesExpectedDate = computed(() => {
  const p = activePreset.value
  return p ? presetHidesExpectedDateField(p) : false
})

const visibleTabModeMenuOptions = computed(() =>
  ARRIVAL_NOTICE_LIST_TAB_MODE_OPTIONS.filter((dim) => {
    if (presetActive.value && presetConflictsStatusTab(activePreset.value!) && dim === 'status') return false
    if (presetActive.value && presetConflictsStockInTypeTab(activePreset.value!) && dim === 'stockInType') {
      return false
    }
    return true
  })
)

const statusFilterOptions = computed(() => [
  { value: 1, label: t('arrivalNoticeList.status.new') },
  { value: 10, label: t('arrivalNoticeList.status.notArrived') },
  { value: 20, label: t('arrivalNoticeList.status.pendingQc') },
  { value: 30, label: t('arrivalNoticeList.status.qcDone') },
  { value: 100, label: t('arrivalNoticeList.status.stocked') }
])

const TAB_MODE_FILTER_I18N: Record<Exclude<ArrivalNoticeListTabModeDimension, 'off'>, string> = {
  status: 'arrivalNoticeList.filters.allStatus',
  stockInType: 'arrivalNoticeList.filters.arrivalTypePlaceholder'
}

function tabModeDimensionLabel(dim: Exclude<ArrivalNoticeListTabModeDimension, 'off'>) {
  return t(TAB_MODE_FILTER_I18N[dim])
}

function closeFilterTabMode() {
  if (tabModeDimension.value === 'off') return
  tabModeDimension.value = 'off'
  writeArrivalNoticeListTabMode('off')
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

function enableFilterTabMode(dim: Exclude<ArrivalNoticeListTabModeDimension, 'off'>) {
  if (presetActive.value && presetConflictsStatusTab(activePreset.value!) && dim === 'status') return
  if (presetActive.value && presetConflictsStockInTypeTab(activePreset.value!) && dim === 'stockInType') return
  tabModeDimension.value = dim
  writeArrivalNoticeListTabMode(dim)
  settingsMenuOpen.value = false
  settingsSubmenuOpen.value = false
}

watch(settingsMenuOpen, (open) => {
  if (!open) settingsSubmenuOpen.value = false
})

const filterTabStripVisible = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return false
  if (dim === 'status' && presetActive.value && presetConflictsStatusTab(activePreset.value!)) return false
  if (dim === 'stockInType' && presetActive.value && presetConflictsStockInTypeTab(activePreset.value!)) {
    return false
  }
  return true
})

const filterTabStripAriaLabel = computed(() => {
  if (tabModeDimension.value === 'off') return ''
  return tabModeDimensionLabel(tabModeDimension.value)
})

type ArrivalNoticeFilterTabId = ArrivalNoticeStatusTabId | ArrivalNoticeStockInTypeTabId

const filterTabOptions = computed(() => {
  const dim = tabModeDimension.value
  if (dim === 'off') return [] as Array<{ id: ArrivalNoticeFilterTabId; label: string }>
  if (dim === 'status') {
    const labelByValue = new Map(statusFilterOptions.value.map((o) => [o.value, o.label]))
    return [
      { id: 'all' as const, label: t('arrivalNoticeList.filterTabs.all') },
      ...ARRIVAL_NOTICE_STATUS_TAB_VALUES.map((value) => ({
        id: String(value) as ArrivalNoticeStatusTabId,
        label: labelByValue.get(value) ?? String(value)
      }))
    ]
  }
  return [
    { id: 'all' as const, label: t('arrivalNoticeList.filterTabs.all') },
    ...ARRIVAL_NOTICE_STOCK_IN_TYPE_TAB_VALUES.map((value) => ({
      id: String(value) as ArrivalNoticeStockInTypeTabId,
      label: arrivalTypeLabel(value)
    }))
  ]
})

const activeFilterTabId = computed((): ArrivalNoticeFilterTabId => {
  const dim = tabModeDimension.value
  if (dim === 'status') return arrivalNoticeStatusFilterToTab(filters.value.status)
  if (dim === 'stockInType') return arrivalNoticeStockInTypeFilterToTab(filters.value.stockInType)
  return 'all'
})

function onFilterTabClick(tab: ArrivalNoticeFilterTabId) {
  const dim = tabModeDimension.value
  if (dim === 'status') {
    const next = arrivalNoticeStatusTabToFilter(tab as ArrivalNoticeStatusTabId)
    if (filters.value.status === next) return
    filters.value.status = next
    runSearch()
    return
  }
  if (dim === 'stockInType') {
    const next = arrivalNoticeStockInTypeTabToFilter(tab as ArrivalNoticeStockInTypeTabId)
    if (filters.value.stockInType === next) return
    filters.value.stockInType = next
    runSearch()
  }
}

function arrivalTypeLabel(type: number): string {
  return t(`stockInList.stockInTypeLabels.${resolveStockInTypeLabelKey(type)}`)
}

const num = (v: unknown) => Number(v ?? 0)

const qtyFromItems = (items: StockInNotifyItemDto[] | undefined, key: 'arrivedQty' | 'qty' | 'passedQty') =>
  Number((items || []).reduce((s, x) => s + num(x?.[key]), 0).toFixed(4))

/** 行级优先，与单表到货通知模型一致；缺省再从 items 汇总 */
const pickQty = (
  rowVal: number | undefined | null,
  items: StockInNotifyItemDto[] | undefined,
  itemKey: 'qty' | 'arrivedQty' | 'passedQty'
) => (rowVal != null && !Number.isNaN(Number(rowVal)) ? Number(rowVal) : qtyFromItems(items, itemKey))

const expectQty = (row: StockInNotifyDto) => pickQty(row.expectQty, row.items, 'qty')
const receiveQty = (row: StockInNotifyDto) => pickQty(row.receiveQty, row.items, 'arrivedQty')
const passedQty = (row: StockInNotifyDto) => pickQty(row.passedQty, row.items, 'passedQty')

const rawPn = (row: StockInNotifyDto) => (row.pn != null && row.pn !== '' ? row.pn : row.items?.[0]?.pn) || ''
const rawBrand = (row: StockInNotifyDto) => (row.brand != null && row.brand !== '' ? row.brand : row.items?.[0]?.brand) || ''
const displayPn = (row: StockInNotifyDto) => rawPn(row) || '—'
const displayBrand = (row: StockInNotifyDto) => rawBrand(row) || '—'

/** 《业务列表规范》§3.2：数量千分位、tabular-nums（与 InventoryList 一致） */
const formatQtyCell = (v: unknown) => {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

/** 已入库(100)后展示实收入库数量；此前尚无独立「入库数」字段时显示 — */
const stockInQtyText = (row: StockInNotifyDto) =>
  row.status === 100 ? formatQtyCell(receiveQty(row)) : '—'

const statusText = (s: number) => {
  const keyMap: Record<number, 'new' | 'notArrived' | 'pendingQc' | 'qcDone' | 'stocked'> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[s]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
}
const statusType = (s: number) => ({ 1: 'info', 10: 'warning', 20: 'primary', 30: 'success', 100: 'success' }[s] || 'info')
const formatExpected = (v?: string | null) => (v ? formatDisplayDate(v) : '—')

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of shipmentArrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

function pickShipmentMethod(row: StockInNotifyDto): string | null | undefined {
  const r = row as unknown as Record<string, unknown>
  return (r.shipmentMethod ?? r.ShipmentMethod) as string | null | undefined
}

function pickCourierTrackingNo(row: StockInNotifyDto): string | null | undefined {
  const r = row as unknown as Record<string, unknown>
  return (r.courierTrackingNo ?? r.CourierTrackingNo) as string | null | undefined
}

function shipmentMethodDisplay(code?: string | number | null): string {
  if (code === null || code === undefined || code === '') return '—'
  const c = String(code).trim()
  if (!c) return '—'
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function displayCourierTrackingNo(row: StockInNotifyDto): string {
  const v = pickCourierTrackingNo(row)
  const s = String(v ?? '').trim()
  return s || '—'
}

const regionTypeLabel = (row: StockInNotifyDto) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function onArrivalPageSizeChange() {
  listPage.value = 1
  void applyArrivalList(false)
}

function isKnownPurchaseCurrency(v: number): boolean {
  return v >= CurrencyCode.RMB && v <= CurrencyCode.GBP
}

function collectKeywordQuery(): Record<string, string> {
  const keywords: Record<string, string> = {}
  const poc = filters.value.purchaseOrderCode.trim()
  if (poc) keywords.purchaseOrderCode = poc
  const ffo = filters.value.freightForwarderOrderNo.trim()
  if (ffo) keywords.freightForwarderOrderNo = ffo
  const pn = filters.value.pn.trim()
  if (pn) keywords.pn = pn
  if (!maskPurchaseSensitiveFields.value) {
    const vn = filters.value.vendorName.trim()
    if (vn) keywords.vendorName = vn
  }
  const ccy = filters.value.purchaseCurrency
  if (ccy != null && isKnownPurchaseCurrency(ccy)) keywords.purchaseCurrency = String(ccy)
  return keywords
}

function buildListRouteQueryFromUi(): Record<string, string> {
  const keywords = collectKeywordQuery()
  if (activePreset.value) {
    return buildArrivalNoticeListRouteQuery({ preset: activePreset.value, keywords })
  }
  const advanced: Record<string, string> = {}
  if (filters.value.status !== undefined && filters.value.status !== null) {
    advanced.status = String(filters.value.status)
  }
  if (filters.value.stockInType !== undefined && filters.value.stockInType !== null) {
    advanced.stockInType = String(filters.value.stockInType)
  }
  if (filters.value.expectedArrivalDate) {
    advanced.expectedArrivalDate = filters.value.expectedArrivalDate
  }
  const noticeId = String(route.query.noticeId ?? '').trim()
  if (noticeId) advanced.noticeId = noticeId
  return buildArrivalNoticeListRouteQuery({ keywords, advanced })
}

function syncFiltersFromRoute() {
  if (route.name !== 'ArrivalNoticeList') return
  const q = route.query
  filters.value.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  filters.value.freightForwarderOrderNo =
    typeof q.freightForwarderOrderNo === 'string' ? q.freightForwarderOrderNo : ''
  filters.value.pn = typeof q.pn === 'string' ? q.pn : ''
  filters.value.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  const ccy = typeof q.purchaseCurrency === 'string' ? Number(q.purchaseCurrency) : NaN
  filters.value.purchaseCurrency = isKnownPurchaseCurrency(ccy) ? ccy : undefined

  const preset = activePreset.value
  if (preset) {
    filters.value.status = undefined
    filters.value.stockInType = undefined
    filters.value.expectedArrivalDate = ''
    return
  }

  const st = typeof q.status === 'string' ? Number(q.status) : NaN
  filters.value.status = st === 1 || st === 10 || st === 20 || st === 30 || st === 100 ? st : undefined
  const sit = typeof q.stockInType === 'string' ? Number(q.stockInType) : NaN
  filters.value.stockInType = (STOCK_IN_TYPE_FILTER_VALUES as readonly number[]).includes(sit) ? sit : undefined
  filters.value.expectedArrivalDate = typeof q.expectedArrivalDate === 'string' ? q.expectedArrivalDate : ''
}

function runSearch() {
  resetListRightPanelOnReload(arrivalNoticeOpsStore)
  listPage.value = 1
  router.replace({ name: 'ArrivalNoticeList', query: buildListRouteQueryFromUi() })
}

function clearPresetChip() {
  router.replace({ name: 'ArrivalNoticeList', query: {} })
}

function applyArrivalList(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  const noticeIdFromRoute = String(route.query.noticeId ?? '').trim() || undefined
  const preset = activePreset.value ?? undefined
  const params: Parameters<typeof logisticsApi.getArrivalNotices>[0] = {
    purchaseOrderCode: filters.value.purchaseOrderCode.trim() || undefined,
    freightForwarderOrderNo: filters.value.freightForwarderOrderNo.trim() || undefined,
    pn: filters.value.pn.trim() || undefined,
    vendorName: maskPurchaseSensitiveFields.value
      ? undefined
      : filters.value.vendorName.trim() || undefined,
    purchaseCurrency:
      filters.value.purchaseCurrency != null && isKnownPurchaseCurrency(filters.value.purchaseCurrency)
        ? filters.value.purchaseCurrency
        : undefined,
    id: noticeIdFromRoute,
    page: listPage.value,
    pageSize: listPageSize.value
  }
  if (preset) {
    params.preset = preset
  } else {
    params.status = filters.value.status
    params.stockInType = filters.value.stockInType
    params.expectedArrivalDate = filters.value.expectedArrivalDate || undefined
  }
  logisticsApi
    .getArrivalNotices(params)
    .then(res => {
      list.value = res.items || []
      listTotal.value = res.total
      void arrivalNoticeOpsStore.refreshFromListRows(
        list.value,
        t('arrivalNoticeList.opsPanel.loadFailed')
      )
    })
    .finally(() => {
      loading.value = false
    })
}

const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'ArrivalNoticeList',
  hasSelectedRow: () => !!arrivalNoticeOpsStore.row,
  setRowOnly: (row) => arrivalNoticeOpsStore.setRowOnly(row),
  selectRow: (row) =>
    arrivalNoticeOpsStore.selectRow(row, t('arrivalNoticeList.opsPanel.loadFailed')),
  loadSelected: () => {
    void arrivalNoticeOpsStore.loadAggregates(t('arrivalNoticeList.opsPanel.loadFailed'))
  }
})

async function onRowClick(row: StockInNotifyDto) {
  await onOpsPanelRowClick(row as unknown as Record<string, unknown>)
}

function opsPanelRowClassName({ row }: { row: StockInNotifyDto }) {
  if (!arrivalNoticeOpsStore.row) return 'table-row-pointer'
  return arrivalNoticeOpsStore.rowKey(row as unknown as Record<string, unknown>) ===
    arrivalNoticeOpsStore.rowKey(arrivalNoticeOpsStore.row)
    ? 'so-item-row--active'
    : 'table-row-pointer'
}

async function confirmArrivedFromOpsPanel(row: Record<string, unknown>) {
  const id = arrivalNoticeOpsStore.rowKey(row)
  if (!id) return
  await logisticsApi.updateArrivalStatus(id, 20)
  ElMessage.success(t('arrivalNoticeList.messages.arrivedSuccess'))
  applyArrivalList(false)
}

const refreshArrivalList = () => applyArrivalList(false)

const resetFilters = () => {
  listPage.value = 1
  router.replace({ name: 'ArrivalNoticeList', query: {} })
}

function openArrivalInfoDialog(row: StockInNotifyDto) {
  arrivalInfoNotice.value = row
  arrivalInfoDialogVisible.value = true
}

function onArrivalInfoSaved(updated: StockInNotifyDto) {
  const id = updated.id?.trim()
  if (id) {
    const idx = list.value.findIndex((r) => r.id === id)
    if (idx >= 0) list.value[idx] = { ...list.value[idx], ...updated }
    if (arrivalNoticeOpsStore.row && arrivalNoticeOpsStore.rowKey(arrivalNoticeOpsStore.row) === id) {
      arrivalNoticeOpsStore.syncNoticeRow(updated)
    }
  }
  arrivalInfoNotice.value = updated
}

const markArrived = async (row: StockInNotifyDto) => {
  await logisticsApi.updateArrivalStatus(row.id, 20)
  applyArrivalList(false)
  ElMessage.success(t('arrivalNoticeList.messages.arrivedSuccess'))
}

const handleDeleteRow = async (row: StockInNotifyDto) => {
  const ok = window.confirm(`确认删除到货通知 ${row.noticeCode} 吗？`)
  if (!ok) return
  try {
    await logisticsApi.deleteArrivalNotice(row.id)
    ElMessage.success('删除成功')
    applyArrivalList(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '删除失败')
  }
}

const handleForceDeleteRow = async (row: StockInNotifyDto) => {
  const entered = window.prompt('请输入到货通知单号以确认强制删除', row.noticeCode || '')?.trim() ?? ''
  if (!entered) return
  if (entered !== String(row.noticeCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await logisticsApi.forceDeleteArrivalNotice(row.id, entered)
    ElMessage.success('强制删除成功')
    applyArrivalList(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '强制删除失败')
  }
}

const goCreateQc = (row: StockInNotifyDto) => {
  router.push({ name: 'QcCreate', query: { noticeId: row.id } })
}

const viewItems = (row: StockInNotifyDto) => {
  detailNotice.value = row
  itemsVisible.value = true
}

const onDetailClosed = () => {
  detailNotice.value = null
}

watch(
  () => [route.name, route.query] as const,
  () => {
    syncFiltersFromRoute()
    if (route.name === 'ArrivalNoticeList') {
      applyArrivalList(true)
    }
  },
  { deep: true, immediate: true }
)

onMounted(async () => {
  arrivalNoticeOpsStore.registerHandlers({
    confirmArrived: (row) => {
      void confirmArrivedFromOpsPanel(row)
    },
    editArrivalInfo: (row) => {
      openArrivalInfoDialog(row as unknown as StockInNotifyDto)
    }
  })
  try {
    await ensureLogisticsDict()
  } catch {
    /* 字典失败时仍回退显示原始码 */
  }
})

onBeforeUnmount(() => {
  arrivalNoticeOpsStore.unregisterHandlers()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.arrival-notice-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

/** 《业务列表规范》§3.2：数量字重与字色（与 InventoryList 一致） */
.inv-list-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .inv-list-qty {
  color: $text-primary;
}

.inv-list-dash {
  color: $text-muted;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;

  h2 {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
  }
}

.ops {
  display: flex;
  gap: 8px;
}

// ---- 搜索栏（与 CustomerList / PurchaseRequisitionListPage 一致）----
.search-bar {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  margin-bottom: 12px;
}

.search-preset-chip-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.search-preset-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px 4px 10px;
  font-size: 12px;
  color: $text-primary;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.35);
  border-radius: 20px;
}

.search-preset-chip__clear {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  padding: 0;
  border: none;
  border-radius: 50%;
  background: transparent;
  color: $text-muted;
  font-size: 14px;
  line-height: 1;
  cursor: pointer;

  &:hover {
    color: $text-primary;
    background: rgba(255, 255, 255, 0.08);
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
}

.status-select,
.arrival-type-select,
.purchase-currency-select {
  width: 140px;
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

.filter-date-single {
  width: 170px;
  flex-shrink: 0;
  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    font-size: 13px !important;
  }
  :deep(.el-input__prefix-inner .el-icon) {
    color: $text-muted !important;
  }
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
    opacity: 0.55;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
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

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }

  &.btn-sm.btn-icon-only {
    width: 32px;
    padding-left: 0;
    padding-right: 0;
    justify-content: center;
  }
}

.sol-main-panel {
  width: 100%;
}

.sol-main-panel--with-filter-tabs {
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

.sol-filter-tabs {
  display: flex;
  align-items: stretch;
  width: 100%;
  margin: 0;
  padding: 0;
  gap: 4px;
}

.sol-filter-tabs__item {
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

html[data-theme='dark'] .sol-filter-tabs__item:not(.is-active) {
  background: var(--crm-layer-1);

  &:hover {
    background: color-mix(in srgb, var(--crm-cyan-primary) 12%, var(--crm-layer-1));
  }
}

.arrival-detail-dialog {
  :deep(.arrival-detail-desc .el-descriptions__label) {
    font-weight: 500;
    color: $text-secondary;
  }
  :deep(.arrival-detail-desc .el-descriptions__content) {
    color: $text-primary;
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

:deep(.stock-in-type-col .cell) {
  overflow: visible;
}
</style>

<style lang="scss">
.arrival-notice-list-settings-popper.el-popover.el-popper {
  padding: 6px;
  min-width: 160px;
  overflow: visible;
}

.arrival-notice-list-settings-menu {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.arrival-notice-list-settings-menu__item {
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

.arrival-notice-list-settings-menu__caret {
  margin-left: 8px;
  font-size: 12px;
  color: var(--crm-text-muted, rgba(200, 216, 232, 0.55));
}

.arrival-notice-list-settings-menu__submenu {
  position: relative;
}

.arrival-notice-list-settings-menu__flyout {
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
</style>
