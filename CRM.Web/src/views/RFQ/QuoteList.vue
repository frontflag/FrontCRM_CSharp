<template>
  <div class="quote-list-page">
    <!-- 页面头部：与《业务列表规范》及 RFQList / CustomerList 一致 -->
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">价</div>
          <h1 class="page-title">{{ t('quoteList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('quoteList.count', { count: totalCount }) }}</div>
      </div>
    </div>

    <!-- 统计卡片（非 el-card，与 RFQList 一致） -->
    <div v-if="viewMode === 'list'" class="statistics-row">
      <div class="stat-card">
        <div class="stat-value">{{ stats.total }}</div>
        <div class="stat-label">{{ t('quoteList.stats.total') }}</div>
      </div>
      <div class="stat-card stat-card--pending">
        <div class="stat-value">{{ stats.newCount }}</div>
        <div class="stat-label">{{ t('quoteList.stats.new') }}</div>
      </div>
      <div class="stat-card stat-card--sent">
        <div class="stat-value">{{ stats.wonCount }}</div>
        <div class="stat-label">{{ t('quoteList.stats.won') }}</div>
      </div>
      <div class="stat-card stat-card--accepted">
        <div class="stat-value">{{ stats.closedCount }}</div>
        <div class="stat-label">{{ t('quoteList.stats.closed') }}</div>
      </div>
    </div>

    <!-- 筛选栏：非 el-card，与业务列表规范一致 -->
    <div class="search-bar">
      <div class="search-left">
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('quoteList.filters.to')"
          :start-placeholder="t('quoteList.filters.startDate')"
          :end-placeholder="t('quoteList.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.keyword"
            type="search"
            class="search-input search-input--w280"
            :placeholder="t('quoteList.filters.placeholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="searchForm.status"
          :placeholder="t('quoteList.filters.allStatus')"
          clearable
          class="status-select status-select--quote-status"
          :teleported="false"
        >
          <el-option :label="t('quoteList.status.new')" :value="0" />
          <el-option :label="t('quoteList.status.won')" :value="1" />
          <el-option :label="t('quoteList.status.closed')" :value="2" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">
          <el-icon><Search /></el-icon>{{ t('quoteList.filters.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('quoteList.filters.reset') }}</button>
        <button
          type="button"
          class="btn-ghost btn-sm btn-board-active"
          @click="toggleViewMode"
        >
          {{ viewMode === 'board' ? t('quoteList.filters.listView') : t('quoteList.filters.boardView') }}
        </button>
      </div>
    </div>

    <div v-if="viewMode === 'board'" class="quote-board-scroll">
      <QuoteListBoard :filters="boardFilters" />
    </div>

    <!-- 主表：.table-wrapper + CrmDataTable（全局 crm-unified-list） -->
    <div v-show="viewMode === 'list'" class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        class="dock-quote-table"
        column-layout-key="quote-list-main-v4"
        :columns="quoteTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="quoteListRows"
        row-key="id"
        highlight-current-row
        @selection-change="onQuoteSelectionChange"
        @row-dblclick="handleEdit"
        @header-dragend="onQuoteTableHeaderDragEnd"
      >
        <template #col-quoteCode="{ row }">
          <span class="quote-code-cell">{{ displayQuoteCode(row) }}</span>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-rfqCode="{ row }">
          <span>{{ displayRfqCode(row) }}</span>
        </template>
        <template #col-brand="{ row }">
          <CrmListCopyableTextCell :text="firstItemBrandRaw(row)" />
        </template>
        <template #col-productionDateDc="{ row }">
          <span>{{ displayQuoteProductionDateDc(row) }}</span>
        </template>
        <template #col-leadTime="{ row }">
          <span>{{ displayQuoteLeadTime(row) }}</span>
        </template>
        <template #col-lineUnitPrice="{ row }">
          <span class="amount-with-code">
            <span>{{ displayFirstItemUnitPriceValue(row) }}</span>
            <span
              v-if="displayFirstItemUnitPriceValue(row) !== t('quoteList.na')"
              :class="['dock-tier-ccy', displayFirstItemUnitPriceCurrencyClass(row)]"
            >
              {{ displayFirstItemUnitPriceCurrency(row) }}
            </span>
          </span>
        </template>
        <template #col-lineQuantity="{ row }">
          <span>{{ displayFirstItemQuantity(row) }}</span>
        </template>
        <template #col-vendorCount="{ row }">
          {{ maskPurchaseSensitiveFields ? '—' : (row.items?.length || 0) }}
        </template>
        <template #col-dockQuoteExtend-header>
          <DockQuoteExtendColumnHeader
            :active-field="dockQuoteExtendActiveField"
            @set-active-field="setDockQuoteExtendActiveField"
          />
        </template>
        <template #col-dockQuoteExtend="{ row }">
          <DockQuoteExtendCell
            :row="row as Record<string, unknown>"
            :active-field="dockQuoteExtendActiveField"
            :empty-text="t('quoteList.na')"
          />
        </template>
        <template #col-quoteDate="{ row }">
          {{ formatDisplayDate(row.quoteDate) }}
        </template>
        <template #col-createTime="{ row }">
          <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime)]" :key="`ct-${row.id}`">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-createUser="{ row }">
          {{
            row.createUserName ||
              row.createdBy ||
              (!maskSaleSensitiveFields ? row.salesUserName : '') ||
              row.purchaseUserName ||
              '—'
          }}
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
              <button type="button" class="action-btn" @click.stop="handleCopyQuoteSummary(row)">
                {{ t('quoteList.actions.copy') }}
              </button>
              <button
                v-if="canEditQuoteRow(row)"
                type="button"
                class="action-btn action-btn--primary"
                @click.stop="handleEdit(row)"
              >
                {{ t('quoteList.actions.edit') }}
              </button>
              <button
                v-if="canDeleteQuoteRow(row)"
                type="button"
                class="action-btn action-btn--danger"
                @click.stop="handleDelete(row)"
              >
                {{ t('quoteList.actions.delete') }}
              </button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleCopyQuoteSummary(row)">
                    <span class="op-more-item">{{ t('quoteList.actions.copy') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="canEditQuoteRow(row)" @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('quoteList.actions.edit') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="canDeleteQuoteRow(row)" @click.stop="handleDelete(row)">
                    <span class="op-more-item op-more-item--danger">{{ t('quoteList.actions.delete') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div v-if="viewMode === 'list' && pageInfo.total > 0" class="pagination-wrapper">
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

        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          复选篮子<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          清空篮子
        </el-button>
        <button
          type="button"
          class="btn-primary btn-sm basket-batch-purchase-btn"
          :disabled="!basketCount || salesOrderPreflightLoading"
          @click="handleGenerateSalesOrder"
        >
          <el-icon v-if="salesOrderPreflightLoading" class="quote-list-toolbar-action-icon is-loading">
            <Loading />
          </el-icon>
          <el-icon v-else class="quote-list-toolbar-action-icon"><Document /></el-icon>
          {{ salesOrderPreflightLoading ? '…' : t('quoteList.generateSalesOrder') }}
        </button>
      </div>
      <el-pagination
        class="quantum-pagination"
        v-model:current-page="pageInfo.page"
        v-model:page-size="pageInfo.pageSize"
        :total="pageInfo.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      title="复选篮子"
      direction="rtl"
      size="min(560px, 94vw)"
      class="quote-list-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">
        篮子里暂无记录。在列表中勾选行即可加入篮子，翻页后已选记录会保留。
      </p>
      <template v-else>
        <p class="basket-drawer-summary">
          共 <strong>{{ basketCount }}</strong> 条，可在此移除单条或点击
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            清空篮子
          </el-button>
          全部清除。
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column :label="t('quoteList.columns.quoteCode')" min-width="140" show-overflow-tooltip>
              <template #default="{ row }">{{ displayQuoteCode(row) }}</template>
            </el-table-column>
            <el-table-column
              v-if="!maskSaleSensitiveFields"
              prop="customerName"
              :label="t('quoteList.columns.customer')"
              min-width="120"
              show-overflow-tooltip
            />
            <el-table-column :label="t('quoteList.columns.mpn')" min-width="130">
              <template #default="{ row }">
                <CrmListCopyableTextCell :text="row.mpn || firstQuoteItemMpn(row) || ''" />
              </template>
            </el-table-column>
            <el-table-column :label="t('quoteList.columns.status')" width="100" align="center">
              <template #default="{ row }">
                <el-tag effect="dark" :type="getStatusType(Number(row.status))" size="small">
                  {{ getStatusText(Number(row.status)) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column
              :label="t('quoteList.actions.column')"
              :width="quoteBasketOpColWidth"
              :min-width="quoteBasketOpColMinWidth"
              fixed="right"
              align="center"
              class-name="op-col"
              label-class-name="op-col"
              :resizable="false"
            >
              <template #header>
                <div class="list-op-col-header--icon-only">
                  <button
                    type="button"
                    class="op-col-toggle-btn list-op-col-toggle"
                    :aria-label="quoteBasketOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                    @click.stop="toggleQuoteBasketOpCol"
                  >
                    {{ quoteBasketOpColExpanded ? '>' : '<' }}
                  </button>
                </div>
              </template>
              <template #default="{ row }">
                <div @click.stop @dblclick.stop>
                  <div v-if="quoteBasketOpColExpanded" class="action-btns">
                    <button type="button" class="action-btn action-btn--danger" @click.stop="removeOneFromBasket(resolveQuoteId(row))">
                      移除
                    </button>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger">
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="removeOneFromBasket(resolveQuoteId(row))">
                          <span class="op-more-item op-more-item--danger">移除</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { Search, Setting, Document, Loading } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { quoteApi } from '@/api/quote'
import { useQuoteListBasketStore } from '@/stores/quoteListBasket'
import { listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { copyQuoteSummaryToClipboard } from '@/utils/quoteSummaryCopy'
import { assertQuotesSameCustomer } from '@/utils/quoteSalesOrderPrefill'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CrmDataTable from '@/components/CrmDataTable.vue'
import QuoteListBoard from './QuoteListBoard.vue'
import type { QuoteListAnalyticsQuery } from '@/api/quoteListAnalytics'
import DockQuoteExtendColumnHeader from '@/components/list/DockQuoteExtendColumnHeader.vue'
import DockQuoteExtendCell from '@/components/list/DockQuoteExtendCell.vue'
import {
  useDockQuoteExtendColumn,
  isDockQuoteExtendTableColumn
} from '@/composables/useDockQuoteExtendColumn'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { productionDateDisplayLabel, useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import {
  isQuoteDeleteForbidden,
  isQuoteReadOnly,
  quoteMainStatusI18nKey,
  quoteMainStatusTagType
} from '@/utils/quoteMainStatus'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const {
  activeField: dockQuoteExtendActiveField,
  colWidth: dockQuoteExtendColWidth,
  colMinWidth: dockQuoteExtendColMinWidth,
  setActiveField: setDockQuoteExtendActiveField,
  applyOuterWidthFromTable: applyDockQuoteExtendOuterWidth
} = useDockQuoteExtendColumn()

function onQuoteTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isDockQuoteExtendTableColumn(column)) return
  applyDockQuoteExtendOuterWidth(newWidth)
}

const { options: materialPdOptions, ensureLoaded: ensureMaterialPdDict } = useMaterialProductionDateDict()
const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const salesOrderPreflightLoading = ref(false)
/** 当前页数据（后端分页） */
const quoteListRows = ref<Record<string, unknown>[]>([])
const basketStore = useQuoteListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)
const basketDrawerVisible = ref(false)
/** 《列表操作列规范》：复选篮子抽屉内表 */
const quoteBasketOpColExpanded = ref(false)
const QUOTE_BASKET_OP_COL_COLLAPSED = 43
const QUOTE_BASKET_OP_COL_EXPANDED = 173
const QUOTE_BASKET_OP_COL_EXPANDED_MIN = 160
const quoteBasketOpColWidth = computed(() =>
  quoteBasketOpColExpanded.value ? QUOTE_BASKET_OP_COL_EXPANDED : QUOTE_BASKET_OP_COL_COLLAPSED
)
const quoteBasketOpColMinWidth = computed(() =>
  quoteBasketOpColExpanded.value ? QUOTE_BASKET_OP_COL_EXPANDED_MIN : QUOTE_BASKET_OP_COL_COLLAPSED
)
function toggleQuoteBasketOpCol() {
  quoteBasketOpColExpanded.value = !quoteBasketOpColExpanded.value
}
const suppressBasketMerge = ref(false)
const stats = ref({ total: 0, newCount: 0, wonCount: 0, closedCount: 0 })
const viewMode = ref<'list' | 'board'>('list')
const dateRange = ref<[string, string] | null>(null)

// 搜索表单
const searchForm = ref({
  keyword: '',
  status: undefined as number | undefined
})

const boardFilters = computed((): QuoteListAnalyticsQuery => {
  const q: QuoteListAnalyticsQuery = {}
  if (dateRange.value?.[0]) q.startDate = dateRange.value[0]
  if (dateRange.value?.[1]) q.endDate = dateRange.value[1]
  const kw = searchForm.value.keyword.trim()
  if (kw) q.keyword = kw
  if (searchForm.value.status !== undefined && searchForm.value.status !== null) {
    q.status = searchForm.value.status
  }
  return q
})

function toggleViewMode() {
  viewMode.value = viewMode.value === 'list' ? 'board' : 'list'
}

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

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

/** 报价列表主表可配置列（localStorage：crm-table-columns:v1:quote-list-main） */
const quoteTableColumns = computed<CrmTableColumnDef[]>(() => {
  void dockQuoteExtendColWidth.value
  const cols: CrmTableColumnDef[] = [
    {
      key: 'sel',
      type: 'selection',
      width: 48,
      hideable: false,
      pinned: 'start',
      resizable: false,
      reserveSelection: true
    },
    { key: 'status', label: t('quoteList.columns.status'), prop: 'status', width: 160, align: 'center' },
    { key: 'mpn', label: t('quoteList.columns.mpn'), prop: 'mpn', minWidth: 150, showOverflowTooltip: true },
    { key: 'brand', label: t('quoteList.columns.brand'), width: 100, minWidth: 90, showOverflowTooltip: true },
    {
      key: 'productionDateDc',
      label: t('quoteList.columns.productionDateDc'),
      width: 150,
      minWidth: 130,
      showOverflowTooltip: true
    },
    {
      key: 'leadTime',
      label: t('quoteList.columns.leadTime'),
      width: 120,
      minWidth: 100,
      showOverflowTooltip: true
    },
    {
      key: 'lineUnitPrice',
      label: t('quoteList.columns.unitPrice'),
      width: 130,
      minWidth: 120,
      align: 'right',
      showOverflowTooltip: true
    },
    { key: 'lineQuantity', label: t('quoteList.columns.quantity'), width: 104, minWidth: 96, align: 'right' }
  ]
  if (!maskSaleSensitiveFields.value) {
    cols.push(
      { key: 'customerName', label: t('quoteList.columns.customer'), prop: 'customerName', minWidth: 200, showOverflowTooltip: true },
      { key: 'salesUserName', label: t('quoteList.columns.salesUser'), prop: 'salesUserName', width: 100 }
    )
  }
  cols.push(
    { key: 'purchaseUserName', label: t('quoteList.columns.purchaseUser'), prop: 'purchaseUserName', width: 100 },
    { key: 'vendorCount', label: t('quoteList.columns.vendorCount'), width: 132, minWidth: 132, align: 'center' },
    {
      key: 'dockQuoteExtend',
      label: t('common.dockQuoteExtendCol.columnTitle'),
      prop: 'dockQuoteExtend',
      width: dockQuoteExtendColWidth.value,
      minWidth: dockQuoteExtendColMinWidth.value,
      align: 'center',
      className: 'customer-extend-col dock-quote-extend-col',
      labelClassName: 'customer-extend-col dock-quote-extend-col',
      resizable: true
    },
    { key: 'quoteDate', label: t('quoteList.columns.quoteDate'), prop: 'quoteDate', width: 160 },
    {
      key: 'quoteCode',
      label: t('quoteList.columns.quoteCode'),
      prop: 'quoteCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true,
      sortable: true
    },
    { key: 'rfqCode', label: t('quoteList.columns.rfqCode'), width: 160, minWidth: 160, showOverflowTooltip: true },
    { key: 'createTime', label: t('quoteList.columns.createTime'), prop: 'createTime', width: 160 },
    { key: 'createUser', label: t('quoteList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('quoteList.actions.column'),
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
  )
  return cols
})

const totalCount = computed(() => pageInfo.value.total)

/** 兼容 camelCase / PascalCase / 后端字段，避免编号列空白 */
function displayQuoteCode(row: Record<string, unknown>) {
  const v =
    row.quoteCode ??
    row.quoteNumber ??
    row.QuoteCode ??
    row.QuoteNumber
  if (v != null && String(v).trim() !== '') return String(v)
  return t('quoteList.na')
}

/** 主需求单需求编号（与明细关联的 rfqId 对应主表 rfqCode；后端可直接返回 rfqCode） */
function displayRfqCode(row: Record<string, unknown>) {
  const v =
    row.rfqCode ??
    row.RfqCode ??
    row.RFQCode ??
    row.rfqNumber ??
    row.RfqNumber
  if (v != null && String(v).trim() !== '') return String(v)
  return t('quoteList.na')
}

function firstQuoteItem(row: Record<string, unknown>): Record<string, unknown> | null {
  const items = row.items ?? row.Items
  if (!Array.isArray(items) || items.length === 0) return null
  return items[0] as Record<string, unknown>
}

function firstQuoteItemMpn(row: Record<string, unknown>): string {
  const hdr = row.mpn ?? row.Mpn ?? row.MPN
  if (hdr != null && String(hdr).trim() !== '') return String(hdr).trim()
  const it = firstQuoteItem(row)
  if (!it) return ''
  const m = it.mpn ?? it.Mpn ?? it.MPN
  return m != null && String(m).trim() !== '' ? String(m).trim() : ''
}

function firstItemBrandRaw(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return ''
  const b = it.brand ?? it.Brand
  if (b != null && String(b).trim() !== '') return String(b).trim()
  return ''
}

function displayFirstItemUnitPriceValue(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return t('quoteList.na')
  const p = it.unitPrice ?? it.UnitPrice
  if (p == null || p === '') return t('quoteList.na')
  const n = Number(p)
  if (Number.isNaN(n)) return t('quoteList.na')
  return n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function displayFirstItemUnitPriceCurrency(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return 'RMB'
  const ccy = Number(it.currency ?? it.Currency ?? 1)
  return listAmountCurrencyIso(ccy)
}

function displayFirstItemUnitPriceCurrencyClass(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  const ccy = Number(it?.currency ?? it?.Currency ?? 1)
  return listAmountCurrencyDockClass(ccy)
}

function displayFirstItemQuantity(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return t('quoteList.na')
  const q = it.quantity ?? it.Quantity
  if (q == null || q === '') return t('quoteList.na')
  const n = Number(q)
  if (Number.isNaN(n)) return t('quoteList.na')
  return String(n)
}

/** 生产日期/DC：字典 ItemCode → 文案；多明细去重后顿号拼接 */
function displayQuoteProductionDateDc(row: Record<string, unknown>): string {
  const opts = materialPdOptions.value
  const mapOne = (code: string) => {
    const label = productionDateDisplayLabel(code, opts)
    return (label && label.trim()) || code
  }
  const items = row.items ?? row.Items
  if (!Array.isArray(items) || items.length === 0) {
    const hdr = row.dateCode ?? row.DateCode
    const s = hdr != null ? String(hdr).trim() : ''
    if (!s) return t('quoteList.na')
    return mapOne(s) || t('quoteList.na')
  }
  const labels = new Set<string>()
  for (const raw of items) {
    const o = raw as Record<string, unknown>
    const dcRaw = o.dateCode ?? o.DateCode
    if (dcRaw == null || String(dcRaw).trim() === '') continue
    const code = String(dcRaw).trim()
    const text = mapOne(code)
    if (text) labels.add(text)
  }
  if (labels.size === 0) return t('quoteList.na')
  return [...labels].join('、')
}

/** 交期：多明细去重后顿号拼接 */
function displayQuoteLeadTime(row: Record<string, unknown>): string {
  const items = row.items ?? row.Items
  if (Array.isArray(items) && items.length > 0) {
    const set = new Set<string>()
    for (const raw of items) {
      const o = raw as Record<string, unknown>
      const lt = o.leadTime ?? o.LeadTime
      if (lt != null && String(lt).trim() !== '') set.add(String(lt).trim())
    }
    if (set.size > 0) return [...set].join('、')
  }
  const hdr = row.leadTime ?? row.LeadTime
  const s = hdr != null ? String(hdr).trim() : ''
  if (s) return s
  return t('quoteList.na')
}

async function handleCopyQuoteSummary(row: Record<string, unknown>) {
  const ok = await copyQuoteSummaryToClipboard(row, {
    naLabel: t('quoteList.na'),
    materialPdOptions: materialPdOptions.value
  })
  if (ok) {
    ElMessage.success(t('quoteList.actions.copySuccess'))
    return
  }
  ElMessage.error(t('quoteList.actions.copyFailed'))
}

// 状态处理
const getStatusType = (status: number) => quoteMainStatusTagType(status)

const getStatusText = (status: number) => t(quoteMainStatusI18nKey(status))

function canEditQuoteRow(row: Record<string, unknown>) {
  return !isQuoteReadOnly(row.status)
}

function canDeleteQuoteRow(row: Record<string, unknown>) {
  return !isQuoteDeleteForbidden(row.status)
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await quoteApi.getList({
      page: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize,
      keyword: searchForm.value.keyword,
      status: searchForm.value.status,
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1],
      rfqItemId: undefined
    })
    quoteListRows.value = (res.data || []) as Record<string, unknown>[]
    pageInfo.value.total = res.total || 0
    const maxPage = Math.max(1, Math.ceil(pageInfo.value.total / pageInfo.value.pageSize) || 1)
    if (pageInfo.value.page > maxPage) {
      pageInfo.value.page = maxPage
      return await loadData()
    }
    const agg = res.aggregates as Record<string, number> | undefined
    stats.value = {
      total: agg?.totalCount ?? res.total ?? 0,
      newCount: agg?.newCount ?? agg?.pendingCount ?? 0,
      wonCount: agg?.wonCount ?? agg?.sentCount ?? 0,
      closedCount: agg?.closedCount ?? agg?.acceptedCount ?? 0
    }
  } catch (error) {
    ElMessage.error(t('quoteList.loadFailed'))
  } finally {
    loading.value = false
  }
  await nextTick()
  await restoreTableSelectionFromBasket()
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
  if (viewMode.value === 'list') loadData()
}

const handleReset = () => {
  searchForm.value = { keyword: '', status: undefined }
  dateRange.value = null
  pageInfo.value.page = 1
  if (viewMode.value === 'list') loadData()
}

const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  pageInfo.value.page = 1
  void loadData()
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
  void loadData()
}

function onQuoteSelectionChange(rows: Record<string, unknown>[]) {
  if (suppressBasketMerge.value) return
  basketStore.mergePageSelection(quoteListRows.value, rows)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  await nextTick()
  table.clearSelection()
  await nextTick()
  for (const row of quoteListRows.value) {
    const id = resolveQuoteId(row)
    if (id && basketStore.has(id)) {
      table.toggleRowSelection(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(id: string) {
  if (!id) return
  basketStore.remove(id)
  suppressBasketMerge.value = true
  const row = quoteListRows.value.find((r) => resolveQuoteId(r) === id)
  if (row) {
    dataTableRef.value?.toggleRowSelection(row, false)
  }
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleClearBasket() {
  if (!basketStore.count) return
  try {
    await ElMessageBox.confirm('确定清空复选篮子中的全部记录？', '清空确认', {
      type: 'warning',
      confirmButtonText: '清空',
      cancelButtonText: '取消'
    })
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success('已清空复选篮子')
}

function resolveQuoteId(row: Record<string, unknown>): string {
  const id = row.id ?? row.Id
  return id != null ? String(id).trim() : ''
}

/** PRD：quoteIds[] + returnTo；跳转前校验同一客户（以复选篮子为准） */
async function handleGenerateSalesOrder() {
  const rows = basketStore.items
  if (!rows.length) {
    ElMessage.warning(t('quoteList.warnings.selectFirst'))
    return
  }
  const ids = [...new Set(rows.map((r) => resolveQuoteId(r)).filter(Boolean))]
  if (!ids.length) {
    ElMessage.warning(t('quoteList.warnings.invalidId'))
    return
  }
  salesOrderPreflightLoading.value = true
  try {
    const check = await assertQuotesSameCustomer(ids)
    if (!check.ok) {
      ElMessage.error(check.message)
      return
    }
    router.push({
      name: 'SalesOrderCreate',
      query: { quoteIds: ids.join(','), returnTo: route.fullPath }
    })
  } finally {
    salesOrderPreflightLoading.value = false
  }
}

// 编辑
const handleEdit = (row: Record<string, unknown>) => {
  if (!canEditQuoteRow(row)) {
    router.push({ name: 'QuoteDetail', params: { id: String(row.id) } })
    return
  }
  router.push({ name: 'QuoteEdit', params: { id: String(row.id) } })
}

// 删除
const handleDelete = async (row: Record<string, unknown>) => {
  if (!canDeleteQuoteRow(row)) {
    ElMessage.warning(t('quoteList.warnings.cannotDeleteWon'))
    return
  }
  try {
    await ElMessageBox.confirm(
      t('quoteList.deleteConfirm', { code: displayQuoteCode(row) }),
      t('quoteList.deleteTitle'),
      { type: 'warning' }
    )
    const rid = resolveQuoteId(row)
    if (!rid) return
    await quoteApi.delete(rid)
    if (rid) basketStore.remove(rid)
    loadData()
  } catch {
    // 取消
  }
}

onMounted(() => {
  void ensureMaterialPdDict()
  if (viewMode.value === 'list') void loadData()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.quote-list-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  max-height: 100%;
  padding: 24px;
  padding-bottom: 12px;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.quote-board-scroll {
  flex: 1 1 0;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
}

.btn-ghost.btn-board-active {
  border-color: rgba(0, 212, 255, 0.45);
  color: #00d4ff;
  background: rgba(0, 212, 255, 0.08);
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .page-title {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
    letter-spacing: 0.5px;
  }

  .count-badge {
    padding: 3px 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    font-size: 12px;
    color: $text-muted;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

  .page-icon {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    color: $cyan-primary;
    font-weight: 700;
    font-size: 15px;
  }
}

.statistics-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 20px;
  text-align: center;

  .stat-value {
    font-size: 22px;
    font-weight: 700;
    color: $text-primary;
    margin-bottom: 5px;
    font-family: 'Noto Sans SC', sans-serif;
  }

  .stat-label {
    font-size: 12px;
    color: $text-muted;
  }

  &.stat-card--pending .stat-value {
    color: $warning-color;
  }

  &.stat-card--sent .stat-value {
    color: $cyan-primary;
  }

  &.stat-card--accepted .stat-value {
    color: $success-color;
  }
}

// ---- 搜索栏（业务列表规范，与 RFQList 对齐）----
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

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--w280 {
  width: 280px;
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

.status-select--quote-status {
  width: 140px;
}

// ---- 表格：全局样式见 crm-unified-list.scss ----
.quote-list-page .table-wrapper {
  :deep(.el-table .cell) {
    line-height: 1.2;
  }

  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row:hover),
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row.hover-row),
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row.current-row),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row:hover),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row.hover-row),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row.current-row) {
    transform: translateY(-1px);
  }
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
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

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.quantum-pagination {
  :deep(.el-pagination__total) {
    color: $text-muted;
  }
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
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

/** 与销售订单明细页「批量申请采购」同款：小号实心主按钮 + 左侧图标 */
.basket-batch-purchase-btn {
  margin-left: 10px;
  letter-spacing: normal;

  &:hover:not(:disabled) {
    transform: none;
    box-shadow: none;
  }
}

.quote-list-toolbar-action-icon {
  font-size: 15px;
}

.quote-list-toolbar-action-icon.is-loading {
  animation: quote-list-icon-spin 0.9s linear infinite;
}

@keyframes quote-list-icon-spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
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

.pagination-wrapper .quantum-pagination {
  margin-left: auto;
  align-self: flex-start;
}

/* 与 .crm-items-table 正文色一致，避免偏青或与链接触觉混淆 */
.quote-code-cell {
  color: $text-primary !important;
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}

// 列表操作列规范（收起/展开）
.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
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
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--danger {
  color: $color-red-brown;
}

</style>

<!-- 抽屉挂载在 body，需单独样式块 -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.quote-list-basket-drawer {
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
</style>
