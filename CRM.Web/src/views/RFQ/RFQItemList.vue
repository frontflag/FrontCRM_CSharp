<template>
  <div class="rfq-item-list-page customer-list-theme">
    <div class="rfq-item-main">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">明</div>
          <h1 class="page-title">{{ t('rfqItemList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('rfqItemList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <el-button type="warning" :disabled="!basketCount" @click="handleBatchQuote">
          {{ t('rfqItemList.batchQuote') }}
        </el-button>
      </div>
    </div>

    <!-- 搜索栏：与客户列表 CustomerList 同款布局与控件皮肤 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('rfqItemList.filters.createDate') }}</span>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('rfqItemList.filters.to')"
          :start-placeholder="t('rfqItemList.filters.startDate')"
          :end-placeholder="t('rfqItemList.filters.endDate')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <template v-if="canViewCustomerInRfq">
          <span class="filter-field-label">{{ t('rfqItemList.columns.customer') }}</span>
          <div class="search-input-wrap">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
              <circle cx="11" cy="11" r="8" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
            <input
              v-model="searchForm.customerKeyword"
              class="search-input search-input--w180"
              :placeholder="t('rfqItemList.filters.customerPlaceholder')"
              @keyup.enter="handleSearch"
            />
          </div>
        </template>
        <span class="filter-field-label">{{ t('rfqItemList.columns.materialModel') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="searchForm.materialModel"
            class="search-input search-input--w160"
            :placeholder="t('rfqItemList.filters.materialPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <template v-if="showRfqSalesUserColumn">
          <span class="filter-field-label">{{ t('rfqItemList.columns.salesUser') }}</span>
          <el-select
            v-model="searchForm.salesUserId"
            :placeholder="t('rfqItemList.filters.allSalesUsers')"
            clearable
            filterable
            class="status-select status-select--sales"
            :teleported="false"
          >
            <el-option v-for="u in salesUsers" :key="u.id" :label="salesUserLabel(u)" :value="u.id" />
          </el-select>
        </template>
        <span class="filter-field-label">{{ t('rfqItemList.columns.purchaser') }}</span>
        <el-select
          v-model="searchForm.purchaserUserId"
          :placeholder="t('rfqItemList.filters.allPurchasers')"
          clearable
          filterable
          class="status-select status-select--purchase"
          :teleported="false"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="purchaseUserLabel(u)" :value="u.id" />
        </el-select>
        <el-checkbox
          v-model="searchForm.hasQuotesOnly"
          class="filter-checkbox-has-quotes"
          border
          @change="handleSearch"
        >
          {{ t('rfqItemList.filters.hasQuotes') }}
        </el-checkbox>
        <button class="btn-primary btn-sm" type="button" @click="handleSearch">{{ t('rfqItemList.filters.query') }}</button>
        <button class="btn-ghost btn-sm" type="button" @click="handleReset">{{ t('rfqItemList.filters.reset') }}</button>
      </div>
    </div>

    <div class="rfq-item-table-panel">
      <div class="table-card-scroll rfq-items-main-table" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="rfq-item-list-main"
        :columns="rfqItemMainTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="tableData"
        row-key="id"
        highlight-current-row
        @row-click="onItemRowClick"
        @row-dblclick="goDetail"
        @selection-change="onSelectionChange"
      >
        <template #col-itemStatus="{ row }">
          <el-tag size="small" effect="dark">{{ itemStatusText(effectiveItemLineStatus(row)) }}</el-tag>
        </template>
        <template #col-quoteCount="{ row }">
          <span
            class="rfq-item-quote-count"
            :class="{ 'rfq-item-quote-count--positive': (quoteRecordCountByRfqItemId[row.id] ?? 0) > 0 }"
          >
            {{ quoteRecordCountByRfqItemId[row.id] ?? 0 }}
          </span>
        </template>
        <template #col-materialModel="{ row }">
          {{ row.materialModel || row.mpn || '—' }}
        </template>
        <template #col-brand="{ row }">
          {{ row.brand || '—' }}
        </template>
        <template #col-customerPart="{ row }">
          {{ row.customerMaterialModel || row.customerMpn || '—' }}
        </template>
        <template #col-customerBrand="{ row }">
          {{ row.customerBrand || '—' }}
        </template>
        <template #col-purchasers="{ row }">
          {{ formatAssignedPurchasers(row) }}
        </template>
        <template #col-createTime="{ row }">
          <template
            v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime || row.rfqCreateTime)]"
            :key="`ct-main-${row.id}`"
          >
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
              '—'
          }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('rfqItemList.actions.column') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div v-if="opColExpanded" @click.stop @dblclick.stop>
            <div class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="goDetail(row)">{{ t('rfqItemList.actions.detail') }}</button>
              <button type="button" class="action-btn action-btn--warning" @click.stop="goQuote(row)">{{ t('rfqItemList.actions.quote') }}</button>
            </div>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger" @click.stop @dblclick.stop>
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('rfqItemList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="goQuote(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('rfqItemList.actions.quote') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </template>
      </CrmDataTable>
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

          <div class="list-footer-spacer" aria-hidden="true"></div>

          <div class="basket-footer-left">
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
          </div>
        </div>
        <el-pagination
          class="quantum-pagination"
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      title="复选篮子"
      direction="rtl"
      size="min(560px, 94vw)"
      class="rfq-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">篮子里暂无记录。在列表中勾选行即可加入篮子，翻页后已选记录会保留。</p>
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
            <el-table-column prop="rfqCode" label="需求编号" min-width="140" show-overflow-tooltip />
            <el-table-column v-if="canViewCustomerInRfq" prop="customerName" label="客户" min-width="120" show-overflow-tooltip />
            <el-table-column label="物料型号" min-width="130" show-overflow-tooltip>
              <template #default="{ row }">{{ row.materialModel || row.mpn || '—' }}</template>
            </el-table-column>
            <el-table-column prop="quantity" label="数量" width="72" align="right" />
            <el-table-column label="操作" width="88" fixed="right" align="center" class-name="op-col" label-class-name="op-col">
              <template #default="{ row }">
                <div @click.stop @dblclick.stop>
                  <div class="action-btns">
                    <button type="button" class="action-btn action-btn--danger" @click.stop="removeOneFromBasket(row.id)">移除</button>
                  </div>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </template>
    </el-drawer>

    <!-- 底部：采购报价（当前选中需求明细对应的报价列表） -->
    <div class="supplier-quote-dock" :class="{ collapsed: !supplierPanelExpanded }">
      <div class="dock-header">
        <div class="dock-header-top">
          <div class="dock-header-main">
            <span class="dock-title">{{ t('rfqItemList.dockQuotes.title') }}</span>
            <!-- 与新建报价页提示栏同一套字段与拉数逻辑；与标题同一行 -->
            <div
              v-show="supplierPanelExpanded && selectedRfqItem"
              v-loading="dockSummaryLoading"
              class="dock-link-alert-wrap dock-link-alert-wrap--inline"
            >
              <div v-if="dockLinkAlert" class="dock-link-alert-title-row">
                <span class="la-block-rfq">
                  <span class="la-muted">报价需求</span><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-strong la-rfq-val">{{ dockLinkAlert.linkAlertRfqDisplay }}</span>
                </span>
                <span class="la-pre">{{ linkAlertSep8Ideo }}</span>
                <span class="la-block-detail">
                  <span class="la-muted">物料号</span><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-green">{{ dockLinkAlert.mpn || '—' }}</span
                  ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">品牌</span
                  ><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-green">{{ dockLinkAlert.brand || '—' }}</span
                  ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">数量</span
                  ><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-green">{{ dockLinkAlert.quantityDisplay }}</span
                  ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">目标价</span
                  ><span class="la-pre">{{ linkAlertGap2 }}</span
                  ><span class="la-value-green">{{ dockLinkAlert.targetPriceText }}</span>
                </span>
              </div>
            </div>
          </div>
          <div class="dock-header-actions">
            <el-button
              class="dock-toggle"
              text
              circle
              :title="supplierPanelExpanded ? '收起' : '展开'"
              @click="supplierPanelExpanded = !supplierPanelExpanded"
            >
              <el-icon :size="18">
                <ArrowUp v-if="supplierPanelExpanded" />
                <ArrowDown v-else />
              </el-icon>
            </el-button>
          </div>
        </div>
      </div>
      <div v-show="supplierPanelExpanded" class="dock-body">
        <div v-if="!selectedRfqItem" class="dock-placeholder">{{ t('rfqItemList.dockQuotes.pickRowHint') }}</div>
        <template v-else>
          <div
            v-loading="quotesLoading"
            class="dock-table-wrap"
            :class="{ 'dock-table-wrap--quotes-empty': !quotesLoading && !quotesForItem.length }"
          >
            <div v-if="!quotesLoading && !quotesForItem.length" class="dock-quote-empty-row">{{ t('rfqItemList.dockQuotes.empty') }}</div>
            <CrmDataTable
              v-else-if="quotesForItem.length"
              embedded
              class="dock-quote-table"
              :data="quotesForItem"
              size="small"
              stripe
              max-height="260"
              :row-key="dockQuoteRowKey"
            >
              <el-table-column
                :label="t('rfqItemList.dockQuotes.quoteCode')"
                width="160"
                min-width="160"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ displayQuoteCode(row) }}</template>
              </el-table-column>
              <el-table-column
                prop="mpn"
                :label="t('rfqItemList.dockQuotes.mpn')"
                min-width="120"
                show-overflow-tooltip
              />
              <el-table-column
                :label="t('rfqItemList.dockQuotes.brand')"
                min-width="100"
                width="110"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ dockQuoteBrandDisplay(row as Record<string, unknown>) }}</template>
              </el-table-column>
              <el-table-column
                :label="t('rfqItemList.dockQuotes.vendorName')"
                min-width="140"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ dockQuoteVendorNamesDisplay(row as Record<string, unknown>) }}</template>
              </el-table-column>
              <el-table-column
                :label="t('rfqItemList.dockQuotes.quoteQty')"
                width="100"
                min-width="88"
                align="right"
                class-name="dock-tier-col"
              >
                <template #default="{ row }">
                  <div class="dock-quote-tiers">
                    <template v-if="dockQuoteLineItems(row as Record<string, unknown>).length">
                      <div
                        v-for="(it, idx) in dockQuoteLineItems(row as Record<string, unknown>)"
                        :key="idx"
                        class="dock-quote-tier-line"
                      >
                        {{ formatDockTierQuantity(it.quantity) }}
                      </div>
                    </template>
                    <span v-else class="dock-tier-empty">—</span>
                  </div>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('rfqItemList.dockQuotes.unitPriceTiers')"
                min-width="128"
                align="right"
                class-name="dock-tier-col"
              >
                <template #default="{ row }">
                  <div class="dock-quote-tiers">
                    <template v-if="dockQuoteLineItems(row as Record<string, unknown>).length">
                      <div
                        v-for="(it, idx) in dockQuoteLineItems(row as Record<string, unknown>)"
                        :key="idx"
                        class="dock-quote-tier-line dock-tier-price-line"
                      >
                        <template v-if="!dockTierUnitPriceHasValue(it.unitPrice)">
                          —
                        </template>
                        <template v-else>
                          <template v-for="amt in [splitDockTierAmountParts(it.unitPrice)]" :key="idx + '-amt'">
                            <span class="dock-tier-amt">
                              <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                              ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                            </span>
                          </template>
                          <span class="dock-tier-ccy-gap">&nbsp;</span>
                          <span :class="['dock-tier-ccy', dockTierCurrencyCodeClass(it.currency)]">{{
                            dockTierCurrencyCode(it.currency)
                          }}</span>
                        </template>
                      </div>
                    </template>
                    <span v-else class="dock-tier-empty">—</span>
                  </div>
                </template>
              </el-table-column>
              <el-table-column :label="t('rfqItemList.dockQuotes.status')" width="96" align="center">
                <template #default="{ row }">
                  <el-tag effect="dark" :type="quoteStatusType(row.status)" size="small">
                    {{ quoteStatusText(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('rfqItemList.dockQuotes.purchaser')"
                width="100"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ dockQuotePurchaseUserDisplay(row as Record<string, unknown>) }}</template>
              </el-table-column>
              <el-table-column
                :label="t('rfqItemList.dockQuotes.createTime')"
                width="160"
                show-overflow-tooltip
                resizable
              >
                <template #default="{ row }">
                  <template
                    v-for="p in [formatDisplayDateTime2DigitYearParts((row as Record<string, unknown>).createTime as string)]"
                    :key="String(dockQuoteRowKey(row)) + '-ct'"
                  >
                    <span v-if="p" class="crm-quote-create-time">
                      <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                      <span class="crm-quote-create-time__hm">{{ p.time }}</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('rfqItemList.dockQuotes.actions')"
                :width="opDockColWidth"
                :min-width="opDockColMinWidth"
                align="center"
                fixed="right"
                class-name="op-col"
                label-class-name="op-col"
              >
                <template #header>
                  <div class="op-col-header">
                    <span class="op-col-header-text">{{ t('rfqItemList.dockQuotes.actions') }}</span>
                    <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpDockCol">
                      {{ opDockColExpanded ? '>' : '<' }}
                    </button>
                  </div>
                </template>

                <template #default="{ row }">
                  <div v-if="opDockColExpanded" @click.stop @dblclick.stop>
                    <div class="action-btns">
                      <el-button
                        class="action-btn action-btn--warning"
                        link
                        type="warning"
                        size="small"
                        :loading="dockRowSalesOrderQuoteId === resolveQuoteRowId(row)"
                        @click.stop="handleDockRowGenerateSalesOrder(row)"
                      >
                        {{ t('rfqItemList.dockQuotes.genSalesOrder') }}
                      </el-button>
                    </div>
                  </div>
                  <el-dropdown v-else trigger="click" placement="bottom-end">
                    <div class="op-more-dropdown-trigger" @click.stop @dblclick.stop>
                      <button type="button" class="op-more-trigger">...</button>
                    </div>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item @click.stop="handleDockRowGenerateSalesOrder(row)">
                          <span class="op-more-item op-more-item--warning">{{ t('rfqItemList.dockQuotes.genSalesOrder') }}</span>
                        </el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ArrowUp, ArrowDown } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { storeToRefs } from 'pinia'
import { rfqApi } from '@/api/rfq'
import { quoteApi } from '@/api/quote'
import { buildLinkAlertFieldsFromItem, fetchLinkedRfqItemRecord } from '@/utils/rfqLinkedItemSummary'
import { assertQuotesSameCustomer } from '@/utils/quoteSalesOrderPrefill'
import type { RFQItem } from '@/types/rfq'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useRfqItemListBasketStore } from '@/stores/rfqItemListBasket'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { Setting } from '@element-plus/icons-vue'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
/** 与后端 RFQ 脱敏一致：采购等角色可有 customer.read 但不应见需求侧客户名/客户料号筛选（需 customer.info.read）；§5.2.1 时强制不可见 */
const canViewCustomerInRfq = computed(
  () => authStore.hasPermission('customer.info.read') && !maskSaleSensitiveFields.value
)
const showRfqSalesUserColumn = computed(() => !maskSaleSensitiveFields.value)

/** 需求明细列表：按当前筛选与分页自动刷新间隔 */
const RFQ_ITEM_LIST_AUTO_REFRESH_MS = 5 * 60 * 1000
/** 浏览器 setInterval 句柄；显式用 number 避免与 Node Timeout 类型冲突 */
let rfqItemListAutoRefreshTimer: number | null = null

const basketStore = useRfqItemListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)

const loading = ref(false)
const tableData = ref<RFQItem[]>([])
const totalCount = ref(0)

/** CrmDataTable 暴露的 el-table 方法 */
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const suppressBasketMerge = ref(false)
const basketDrawerVisible = ref(false)
const dateRange = ref<[string, string] | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 220
const OP_COL_EXPANDED_MIN_WIDTH = 220
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

/** 主表可配置列（列设置 / 顺序 / localStorage：crm-table-columns:v1:rfq-item-list-main） */
const rfqItemMainTableColumns = computed<CrmTableColumnDef[]>(() => {
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
  {
    key: 'itemStatus',
    label: t('rfqItemList.columns.itemStatus'),
    width: 160,
    minWidth: 160,
    align: 'center',
    resizable: true
  },
  {
    key: 'quoteCount',
    label: t('rfqItemList.columns.quoteCount'),
    width: 120,
    minWidth: 112,
    align: 'center',
    resizable: true
  },
  ]
  if (canViewCustomerInRfq.value) {
    cols.push({
      key: 'customerName',
      label: t('rfqItemList.columns.customer'),
      prop: 'customerName',
      minWidth: 200,
      showOverflowTooltip: true,
      resizable: true
    })
  }
  cols.push(
  {
    key: 'customerPart',
    label: t('rfqItemList.columns.customerPart'),
    width: 120,
    minWidth: 88,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'customerBrand',
    label: t('rfqItemList.columns.customerBrand'),
    minWidth: 100,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'materialModel',
    label: t('rfqItemList.columns.materialModel'),
    minWidth: 120,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'brand',
    label: t('rfqItemList.columns.brand'),
    minWidth: 100,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'quantity',
    label: t('rfqItemList.columns.quantity'),
    prop: 'quantity',
    width: 90,
    minWidth: 72,
    align: 'right',
    resizable: true
  }
  )
  if (showRfqSalesUserColumn.value) {
    cols.push({
      key: 'salesUserName',
      label: t('rfqItemList.columns.salesUser'),
      prop: 'salesUserName',
      width: 100,
      minWidth: 80,
      showOverflowTooltip: true,
      resizable: true
    })
  }
  cols.push(
  {
    key: 'purchasers',
    label: t('rfqItemList.columns.purchaser'),
    minWidth: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'rfqCode',
    label: t('rfqItemList.columns.rfqCode'),
    prop: 'rfqCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'createTime',
    label: t('rfqItemList.columns.createTime'),
    width: 160,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'createUser',
    label: t('rfqItemList.columns.createUser'),
    width: 120,
    showOverflowTooltip: true,
    resizable: true
  },
  {
    key: 'actions',
    label: t('rfqItemList.actions.column'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col',
    resizable: true
  }
  )
  if (!canViewCustomerInRfq.value) {
    return cols.filter(c => c.key !== 'customerPart' && c.key !== 'customerBrand')
  }
  return cols
})

// 底部抽屉内子表格操作列：默认收起（Collapsed）
const opDockColExpanded = ref(false)
const OP_DOCK_COL_COLLAPSED_WIDTH = 110
const OP_DOCK_COL_EXPANDED_WIDTH = 128
const OP_DOCK_COL_EXPANDED_MIN_WIDTH = 128
const opDockColWidth = computed(() => (opDockColExpanded.value ? OP_DOCK_COL_EXPANDED_WIDTH : OP_DOCK_COL_COLLAPSED_WIDTH))
const opDockColMinWidth = computed(() =>
  opDockColExpanded.value ? OP_DOCK_COL_EXPANDED_MIN_WIDTH : OP_DOCK_COL_COLLAPSED_WIDTH
)
function toggleOpDockCol() {
  opDockColExpanded.value = !opDockColExpanded.value
}

const searchForm = reactive({
  customerKeyword: '',
  materialModel: '',
  salesUserId: undefined as string | undefined,
  purchaserUserId: undefined as string | undefined,
  hasQuotesOnly: false
})

const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])

function salesUserLabel(u: SalesUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
}

function purchaseUserLabel(u: PurchaseUserSelectOption) {
  const name = u.realName || u.label || u.userName
  return u.userName && name !== u.userName ? `${name}（${u.userName}）` : name
}

const pageInfo = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

/** 当前点击选中的需求明细（用于底部采购报价面板） */
const selectedRfqItem = ref<RFQItem | null>(null)
const supplierPanelExpanded = ref(true)
const quotesForItem = ref<Record<string, unknown>[]>([])
const quotesLoading = ref(false)
/** 正在预检并跳转生成销售订单的报价行 id（行内按钮 loading） */
const dockRowSalesOrderQuoteId = ref<string | null>(null)

/** 每条需求明细对应的报价单数量（报价头 rfqItemId 与明细 id 一致） */
const quoteRecordCountByRfqItemId = ref<Record<string, number>>({})

/** 底部面板提示条（与 QuoteCreate 提示栏字段一致） */
const dockSummaryLoading = ref(false)
const dockLinkAlert = ref<{
  linkAlertRfqDisplay: string
  mpn: string
  brand: string
  quantityDisplay: string
  targetPriceText: string
} | null>(null)

/** 与 QuoteCreate 提示栏排版一致 */
const linkAlertGap2 = '  '
const linkAlertSep8Ideo = '\u3000'.repeat(8)
const linkAlertSep4Ideo = '\u3000'.repeat(4)

/** 轮询分配的两名询价采购员展示 */
function formatAssignedPurchasers(row: RFQItem) {
  const n1 = row.assignedPurchaserName1?.trim()
  const n2 = row.assignedPurchaserName2?.trim()
  const parts = [n1, n2].filter((x): x is string => !!x)
  return parts.length ? parts.join('、') : '—'
}

function itemStatusText(s?: number | string) {
  const n = s === undefined || s === null || s === '' ? NaN : Number(s)
  const map: Record<number, string> = {
    0: t('rfqItemList.status.pending'),
    1: t('rfqItemList.status.quoted'),
    2: t('rfqItemList.status.accepted'),
    3: t('rfqItemList.status.rejected'),
    4: t('rfqItemList.status.closed')
  }
  return Number.isFinite(n) ? map[n] ?? t('quoteList.na') : t('quoteList.na')
}

/** 库内 status 未回写或接口未部署旧版时，与「报价条目」列一致：有条数则不应仍显示待报价 */
function effectiveItemLineStatus(row: RFQItem): number | undefined {
  const raw = row.status
  const n = raw === undefined || raw === null || (raw as unknown) === '' ? NaN : Number(raw)
  const qc = quoteRecordCountByRfqItemId.value[row.id] ?? 0
  if (Number.isFinite(n) && n === 0 && qc > 0) return 1
  if (Number.isFinite(n)) return n
  return undefined
}

function quoteStatusText(status: number) {
  const map: Record<number, string> = {
    0: '草稿',
    1: '待审核',
    2: '已审核',
    3: '已发送',
    4: '已接受',
    5: '已拒绝',
    6: '已过期',
    7: '已关闭'
  }
  return map[status] ?? '—'
}

function quoteStatusType(status: number) {
  const map: Record<number, string> = {
    0: 'info',
    1: 'warning',
    2: 'primary',
    3: 'success',
    4: 'success',
    5: 'danger',
    6: 'info',
    7: 'info'
  }
  return map[status] || 'info'
}

function displayQuoteCode(row: Record<string, unknown>) {
  const v = row.quoteCode ?? row.quoteNumber ?? row.QuoteCode
  if (v != null && String(v).trim() !== '') return String(v)
  return '—'
}

function dockQuoteItemsRaw(quoteRow: Record<string, unknown>): Record<string, unknown>[] {
  const rawItems = (quoteRow.items ?? quoteRow.Items) as unknown[] | undefined
  if (!rawItems?.length) return []
  return rawItems.map((it) => it as Record<string, unknown>)
}

/** 采购报价表：品牌（多行明细去重后用顿号拼接；无则看报价头 brand） */
function dockQuoteBrandDisplay(quoteRow: Record<string, unknown>): string {
  const items = dockQuoteItemsRaw(quoteRow)
  const set = new Set<string>()
  for (const o of items) {
    const b = o.brand ?? o.Brand
    if (b != null && String(b).trim() !== '') set.add(String(b).trim())
  }
  if (set.size > 0) return [...set].join('、')
  const hb = quoteRow.brand ?? quoteRow.Brand
  if (hb != null && String(hb).trim() !== '') return String(hb).trim()
  return '—'
}

/** 采购报价表：供应商名称（多供应商去重后顿号拼接） */
function dockQuoteVendorNamesDisplay(quoteRow: Record<string, unknown>): string {
  if (maskPurchaseSensitiveFields.value) return '—'
  const items = dockQuoteItemsRaw(quoteRow)
  const set = new Set<string>()
  for (const o of items) {
    const n = o.vendorName ?? o.VendorName
    if (n != null && String(n).trim() !== '') set.add(String(n).trim())
  }
  if (set.size > 0) return [...set].join('、')
  return '—'
}

/** 报价单行：与后端 quoteitem / 前端阶梯行一致 */
interface DockQuoteTierLine {
  quantity: number
  unitPrice: number
  currency: number
}

function dockQuoteLineItems(quoteRow: Record<string, unknown>): DockQuoteTierLine[] {
  const rawItems = (quoteRow.items ?? quoteRow.Items) as unknown[] | undefined
  if (rawItems && rawItems.length > 0) {
    const lines: DockQuoteTierLine[] = []
    const headerCurrency = Number(quoteRow.currency ?? quoteRow.Currency ?? 1)
    for (const it of rawItems) {
      const o = it as Record<string, unknown>
      const quantity = Number(o.quantity ?? o.Quantity ?? 0)
      const unitPrice = Number(o.unitPrice ?? o.UnitPrice ?? 0)
      const currency = Number(o.currency ?? o.Currency ?? headerCurrency) || 1
      lines.push({ quantity, unitPrice, currency })
    }
    return lines
  }
  const q = Number(quoteRow.quantity ?? quoteRow.quoteLineQuantity ?? 0)
  const p = Number(quoteRow.unitPrice ?? quoteRow.UnitPrice ?? 0)
  const c = Number(quoteRow.currency ?? quoteRow.Currency ?? 1) || 1
  if ((Number.isFinite(q) && q !== 0) || (Number.isFinite(p) && p !== 0)) {
    return [{ quantity: q, unitPrice: p, currency: c }]
  }
  return []
}

function formatDockTierQuantity(q: number) {
  if (!Number.isFinite(q)) return '—'
  if (Math.abs(q - Math.round(q)) < 1e-9) return String(Math.round(q))
  return q.toLocaleString('zh-CN', { maximumFractionDigits: 4 })
}

/** 与报价阶梯币别枚举一致：1=RMB，2=USD，3=EUR，4=HKD，5=JPY，6=GBP */
function dockTierCurrencyCode(currency?: number): string {
  const n = Number(currency)
  if (n === 2) return 'USD'
  if (n === 3) return 'EUR'
  if (n === 4) return 'HKD'
  if (n === 5) return 'JPY'
  if (n === 6) return 'GBP'
  return 'RMB'
}

function dockTierCurrencyCodeClass(currency?: number): string {
  const n = Number(currency)
  if (n === 1 || !Number.isFinite(n) || n === 0) return 'dock-tier-ccy--rmb'
  if (n === 2) return 'dock-tier-ccy--usd'
  if (n === 3) return 'dock-tier-ccy--eur'
  if (n === 4) return 'dock-tier-ccy--hkd'
  return 'dock-tier-ccy--purple'
}

function dockTierUnitPriceHasValue(unitPrice: number): boolean {
  return Number.isFinite(unitPrice) && unitPrice !== 0
}

function formatDockTierAmountNum(unitPrice: number): string {
  if (!dockTierUnitPriceHasValue(unitPrice)) return '—'
  return unitPrice.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

/** 与 `formatDockTierAmountNum` 同规则；用 `formatToParts` 避免环境小数点字符非 `.` 时拆分失败 */
function splitDockTierAmountParts(unitPrice: number): { intPart: string; fracPart: string } {
  if (!dockTierUnitPriceHasValue(unitPrice)) return { intPart: '—', fracPart: '' }
  const n = Number(unitPrice)
  const parts = new Intl.NumberFormat('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6
  }).formatToParts(n)
  let intPart = ''
  let fracPart = ''
  for (const p of parts) {
    if (p.type === 'integer' || p.type === 'group') intPart += p.value
    else if (p.type === 'decimal' || p.type === 'fraction') fracPart += p.value
  }
  if (!fracPart) return { intPart: intPart || formatDockTierAmountNum(n), fracPart: '' }
  return { intPart, fracPart }
}

function mapRow(row: any): RFQItem {
  return {
    ...row,
    rfqCreateTime: row.rfqCreateTime,
    materialModel: row.mpn ?? row.materialModel,
    customerMaterialModel: row.customerMpn ?? row.customerMaterialModel,
    customerBrand: row.customerBrand ?? row.CustomerBrand
  }
}

/** 仅按 quote.rfqItemId 计数（与后端一致；同需求同 PN 多行不能用 rfqId+mpn 推断） */
function aggregateQuoteCountByRfqItemId(quotes: unknown[]): Record<string, number> {
  const counts: Record<string, number> = {}
  for (const q of quotes) {
    const row = q as Record<string, unknown>
    const rid = row.rfqItemId ?? row.RfqItemId
    if (rid == null || String(rid).trim() === '') continue
    const k = String(rid).trim()
    counts[k] = (counts[k] || 0) + 1
  }
  return counts
}

function applyRouteQueryToFilters() {
  const q = route.query
  const s = q.startDate
  const e = q.endDate
  if (typeof s === 'string' && typeof e === 'string' && s && e) {
    dateRange.value = [s, e]
  } else {
    dateRange.value = null
  }
  searchForm.customerKeyword = typeof q.customerKeyword === 'string' ? q.customerKeyword : ''
  searchForm.materialModel = typeof q.materialModel === 'string' ? q.materialModel : ''
  const sid = q.salesUserId
  const sidRaw = Array.isArray(sid) ? sid[0] : sid
  searchForm.salesUserId =
    typeof sidRaw === 'string' && sidRaw !== '' ? sidRaw : undefined
  const pid = q.purchaserUserId
  const pidRaw = Array.isArray(pid) ? pid[0] : pid
  searchForm.purchaserUserId =
    typeof pidRaw === 'string' && pidRaw !== '' ? pidRaw : undefined
  const hq = q.hasQuotesOnly
  const hqRaw = Array.isArray(hq) ? hq[0] : hq
  const hqStr = hqRaw != null && typeof hqRaw !== 'object' ? String(hqRaw).trim().toLowerCase() : ''
  searchForm.hasQuotesOnly = hqStr === '1' || hqStr === 'true' || hqStr === 'yes'
}

async function loadData() {
  loading.value = true
  try {
    const [res, quoteRes] = await Promise.all([
      rfqApi.searchRFQItems({
        pageNumber: pageInfo.page,
        pageSize: pageInfo.pageSize,
        startDate: dateRange.value?.[0],
        endDate: dateRange.value?.[1],
        customerKeyword: searchForm.customerKeyword.trim() || undefined,
        materialModel: searchForm.materialModel.trim() || undefined,
        salesUserId: searchForm.salesUserId || undefined,
        purchaserUserId: searchForm.purchaserUserId || undefined,
        ...(searchForm.hasQuotesOnly ? { hasQuotesOnly: true } : {})
      }),
      quoteApi.getList({}).catch(() => ({ data: [] as unknown[] }))
    ])
    quoteRecordCountByRfqItemId.value = aggregateQuoteCountByRfqItemId(quoteRes.data || [])
    tableData.value = (res.items || []).map(mapRow)
    totalCount.value = res.totalCount ?? 0
    pageInfo.total = res.totalCount ?? 0

    const selId = selectedRfqItem.value?.id
    if (selId) {
      const found = tableData.value.find((r) => r.id === selId)
      if (found) {
        selectedRfqItem.value = found
        await loadQuotesForRfqItem(found)
        await refreshDockLinkAlert(found)
      } else {
        selectedRfqItem.value = null
        quotesForItem.value = []
        dockLinkAlert.value = null
      }
    }
  } catch (e: unknown) {
    tableData.value = []
    totalCount.value = 0
    pageInfo.total = 0
    quoteRecordCountByRfqItemId.value = {}
    selectedRfqItem.value = null
    quotesForItem.value = []
    dockLinkAlert.value = null
    const msg = e instanceof Error ? e.message : t('rfqItemList.loadFailed')
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
  await nextTick()
  await restoreTableSelectionFromBasket()
}

function handleSearch() {
  pageInfo.page = 1
  const query: Record<string, string> = {}
  if (dateRange.value?.[0] && dateRange.value[1]) {
    query.startDate = dateRange.value[0]
    query.endDate = dateRange.value[1]
  }
  const ck = searchForm.customerKeyword.trim()
  if (ck) query.customerKeyword = ck
  const mm = searchForm.materialModel.trim()
  if (mm) query.materialModel = mm
  if (searchForm.salesUserId) query.salesUserId = searchForm.salesUserId
  if (searchForm.purchaserUserId) query.purchaserUserId = searchForm.purchaserUserId
  if (searchForm.hasQuotesOnly) query.hasQuotesOnly = '1'
  router.replace({ name: 'RFQItemList', query })
}

function handleReset() {
  router.replace({ name: 'RFQItemList', query: {} })
}

watch(
  () => [route.name, route.query] as const,
  () => {
    if (route.name !== 'RFQItemList') return
    applyRouteQueryToFilters()
    pageInfo.page = 1
    loadData()
  },
  { deep: true, immediate: true }
)

function onSelectionChange(rows: RFQItem[]) {
  if (suppressBasketMerge.value) return
  basketStore.mergePageSelection(tableData.value, rows)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  await nextTick()
  table.clearSelection()
  await nextTick()
  for (const row of tableData.value) {
    if (basketStore.has(row.id)) {
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
  const row = tableData.value.find((r) => r.id === id)
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

function dockQuoteRowKey(row: Record<string, unknown>) {
  return resolveQuoteRowId(row) ?? ''
}

/** 与报价列表「采购员」语义一致：优先采购员姓名（接口填充 purchaseUserName），缺省再回退创建人 / 业务员 */
function dockQuotePurchaseUserDisplay(row: Record<string, unknown>) {
  const v =
    row.purchaseUserName ??
    row.PurchaseUserName ??
    row.createUserName ??
    row.CreateUserName ??
    row.createdBy ??
    row.salesUserName ??
    row.SalesUserName
  const s = v != null && v !== '' ? String(v).trim() : ''
  return s || '—'
}

async function loadQuotesForRfqItem(item: RFQItem | null) {
  if (!item?.id) {
    quotesForItem.value = []
    return
  }
  quotesLoading.value = true
  try {
    const res = await quoteApi.getList({ rfqItemId: item.id })
    quotesForItem.value = (res.data || []) as Record<string, unknown>[]
  } catch {
    quotesForItem.value = []
  } finally {
    quotesLoading.value = false
  }
}

async function refreshDockLinkAlert(row: RFQItem) {
  dockSummaryLoading.value = true
  dockLinkAlert.value = null
  try {
    const loaded = await fetchLinkedRfqItemRecord(row.rfqId || '', row.id)
    const raw = loaded?.item ?? (row as unknown as Record<string, unknown>)
    dockLinkAlert.value = buildLinkAlertFieldsFromItem(raw, {
      rfqCode: row.rfqCode,
      rfqId: row.rfqId,
      rfqHeader: loaded?.rfqHeader ?? undefined
    })
  } finally {
    dockSummaryLoading.value = false
  }
}

function onItemRowClick(row: RFQItem) {
  selectedRfqItem.value = row
  void loadQuotesForRfqItem(row)
  void refreshDockLinkAlert(row)
}

function goDetail(row: RFQItem) {
  if (!row.rfqId) return
  router.push({ name: 'RFQDetail', params: { id: row.rfqId } })
}

function goQuote(row: RFQItem) {
  if (!row.rfqId || !row.id) {
    ElMessage.warning(t('rfqItemList.warnings.missingIds'))
    return
  }
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId: row.rfqId,
      rfqItemId: row.id,
      ...(row.rfqCode ? { rfqCode: row.rfqCode } : {}),
      returnTo: route.fullPath
    }
  })
}

function resolveQuoteRowId(row: Record<string, unknown>): string | undefined {
  const id = row.id ?? row.Id
  if (id != null && String(id).trim() !== '') return String(id)
  return undefined
}

async function handleDockRowGenerateSalesOrder(row: Record<string, unknown>) {
  const id = resolveQuoteRowId(row)
  if (!id) {
    ElMessage.warning('无法识别报价主键')
    return
  }
  dockRowSalesOrderQuoteId.value = id
  try {
    const check = await assertQuotesSameCustomer([id])
    if (!check.ok) {
      ElMessage.error(check.message)
      return
    }
    router.push({
      name: 'SalesOrderCreate',
      query: { quoteIds: id, returnTo: route.fullPath }
    })
  } finally {
    dockRowSalesOrderQuoteId.value = null
  }
}

function handleBatchQuote() {
  const rows = basketStore.items
  if (!rows.length) {
    ElMessage.warning(t('rfqItemList.warnings.selectFromBasket'))
    return
  }
  const rfqIds = new Set(rows.map((r) => r.rfqId).filter(Boolean))
  if (rfqIds.size !== 1) {
    ElMessage.warning(t('rfqItemList.warnings.sameRfqOnly'))
    return
  }
  const rfqId = [...rfqIds][0]!
  const ids = rows.map((r) => r.id).filter(Boolean)
  const code = rows[0]?.rfqCode
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId,
      rfqItemIds: ids.join(','),
      ...(code ? { rfqCode: code } : {}),
      returnTo: route.fullPath
    }
  })
}

onMounted(async () => {
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
  rfqItemListAutoRefreshTimer = window.setInterval(() => {
    if (route.name !== 'RFQItemList' || loading.value) return
    void loadData()
  }, RFQ_ITEM_LIST_AUTO_REFRESH_MS)
})

onUnmounted(() => {
  if (rfqItemListAutoRefreshTimer != null) {
    clearInterval(rfqItemListAutoRefreshTimer)
    rfqItemListAutoRefreshTimer = null
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-item-list-page {
  display: flex;
  flex-direction: column;
  /* 填满 AppLayout.content-wrapper 可视高度，避免整页被父级滚动条拉长导致底部面板不可见 */
  height: 100%;
  min-height: 0;
  max-height: 100%;
  padding: 24px;
  padding-bottom: 12px;
  box-sizing: border-box;
  overflow: hidden;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.rfq-item-main {
  flex: 1 1 0;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding-bottom: 8px;
}

.supplier-quote-dock {
  flex: 0 0 auto;
  flex-shrink: 0;
  margin-top: 0;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  overflow: hidden;
}

.dock-header {
  padding: 10px 14px;
  background: var(--crm-table-header-bg);
  border-bottom: 1px solid $border-panel;
}

.dock-header-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  min-height: 32px;
}

.dock-header-main {
  display: flex;
  align-items: center;
  gap: 14px;
  flex: 1 1 auto;
  min-width: 0;
}

.dock-header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.dock-title {
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
  flex-shrink: 0;
  line-height: 1.4;
}

.dock-toggle {
  color: $cyan-primary !important;
}

.dock-link-alert-wrap {
  margin-top: 10px;
  min-height: 28px;
}

.dock-link-alert-wrap--inline {
  margin-top: 0;
  min-height: 0;
  flex: 1 1 auto;
  min-width: 0;
  align-self: center;
}

.dock-link-alert-title-row {
  display: flex;
  flex-wrap: nowrap;
  align-items: baseline;
  gap: 0;
  line-height: 1.55;
  font-size: 14px;
  font-weight: 400;
  color: $text-primary;
  white-space: nowrap;
  overflow-x: auto;
  overflow-y: hidden;
  min-width: 0;
  scrollbar-width: thin;
}

.dock-link-alert-title-row .la-pre {
  white-space: pre;
  font-size: inherit;
}

.dock-link-alert-title-row .la-muted {
  color: $text-muted;
  font-weight: 400;
}

.dock-link-alert-title-row .la-strong {
  color: $text-primary;
  font-weight: 600;
}

.dock-link-alert-title-row .la-value-green {
  color: #5fd89a;
  font-weight: 600;
}

.supplier-quote-dock.collapsed .dock-header {
  border-bottom: none;
}

.supplier-quote-dock.collapsed .dock-link-alert-wrap--inline {
  display: none;
}

.dock-body {
  padding: 12px 14px 14px;
  min-height: 0;
}

.dock-placeholder {
  padding: 24px 12px;
  text-align: center;
  font-size: 13px;
  color: $text-muted;
}

.dock-table-wrap {
  min-height: 120px;
}

.dock-table-wrap--quotes-empty {
  min-height: 0;
}

/* 采购报价阶梯表（数量 / 金额 / 币别）样式见 assets/styles/crm-quote-tier-dock.scss */

.page-header {
  flex-shrink: 0;
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

  .basket-count-label {
    color: $cyan-primary;
    font-weight: 600;
    margin-left: 2px;
  }

  .page-title {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
    letter-spacing: 0.5px;
  }

  .count-badge {
    font-size: 12px;
    color: $text-muted;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    padding: 3px 10px;
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
  }
}

// ---- 搜索栏（与客户列表 CustomerList 对齐）----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  flex-shrink: 0;
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

  &.search-input--w180 {
    width: 180px;
  }

  &.search-input--w160 {
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

.status-select--sales,
.status-select--purchase {
  width: 180px;
}

.filter-checkbox-has-quotes {
  flex-shrink: 0;

  :deep(.el-checkbox__label) {
    color: $text-primary;
    font-size: 12px;
  }
}

.rfq-item-quote-count {
  font-variant-numeric: tabular-nums;
}

.rfq-item-quote-count--positive {
  color: $warning-color;
  font-weight: 600;
}

.filter-date-range {
  width: 218px;
  flex-shrink: 0;

  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    padding-inline: 6px;
  }

  :deep(.el-range-input) {
    color: $text-primary !important;
    font-size: 12px !important;
    width: 82px !important;
    min-width: 82px !important;
    max-width: 82px !important;
    flex: 0 0 82px !important;
  }

  :deep(.el-range-separator) {
    color: $text-muted !important;
    flex-shrink: 0;
    padding: 0 2px;
    font-size: 12px;
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

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
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

// 表格区：无 el-card，由 CrmDataTable 根节点 .table-wrapper 承接 crm-unified-list 皮肤
.rfq-item-table-panel {
  flex: 1 1 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.table-card-scroll {
  flex: 1 1 0;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;

  :deep(.table-wrapper) {
    flex: 1 1 0;
    min-height: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  :deep(.el-table) {
    flex: 1 1 0;
    height: 100% !important;
  }

  :deep(.el-table__inner-wrapper) {
    height: 100%;
  }

  /*
   * 勿在 .el-table__body-wrapper 上设 padding-bottom：会在横向滚动条下方留出一条「缝」，
   * 仍能扫到表格底色/内容。改为给内部 table 加 margin-bottom，滚动条贴在列表区域最底，
   * 同时竖向滚到底时最后一行仍能在横条之上完整露出。
   */
  :deep(.el-table__body-wrapper .el-table__body) {
    margin-bottom: 12px;
  }

  :deep(.el-table__fixed-body-wrapper .el-table__body) {
    margin-bottom: 12px;
  }

  /* 竖向条与菜单同宽 $scrollbar-vertical-width；横向条高度 $scrollbar-table-horizontal-height */
  :deep(.el-table__body-wrapper::-webkit-scrollbar),
  :deep(.el-table__header-wrapper::-webkit-scrollbar),
  :deep(.el-table__fixed-body-wrapper::-webkit-scrollbar) {
    width: $scrollbar-vertical-width;
    height: $scrollbar-table-horizontal-height;
  }

  :deep(.el-table__body-wrapper::-webkit-scrollbar-thumb),
  :deep(.el-table__header-wrapper::-webkit-scrollbar-thumb),
  :deep(.el-table__fixed-body-wrapper::-webkit-scrollbar-thumb) {
    border-radius: $scrollbar-table-horizontal-height * 0.5;
  }

  /* 与 RFQList 列表区一致的表头变量（操作列表头单独 #0A1D30 见下） */
  :deep(.el-table) {
    --el-table-header-bg-color: rgba(255, 255, 255, 0.03);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: #{$border-panel};
  }
}

// 主列表操作列 op-col：main.scss 全局；按钮：crm-unified-list.scss

/* 底栏：与《业务列表规范》及 CustomerList 一致（列设置齿轮 → 行高密度锚点 → Spacer → 复选篮子） */
.pagination-wrapper {
  flex-shrink: 0;
  margin-top: 6px;
  padding-top: 0;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-wrap: nowrap;
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

.basket-footer-left {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  flex-wrap: nowrap;
  flex-shrink: 0;
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

.pagination-wrapper .quantum-pagination {
  margin-left: auto;
  align-self: flex-start;
}

// 主列表操作列 .action-btns / .action-btn / op-col：crm-unified-list.scss 中 .crm-data-table 统一提供

.quantum-pagination {
  :deep(.el-pagination__total) {
    color: $text-muted;
  }

  :deep(.el-pagination__sizes .el-select__wrapper) {
    background: $layer-2 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }

  :deep(.el-pager li) {
    background: $layer-2;
    border: 1px solid $border-panel;
    color: $text-secondary;
    border-radius: 6px;
    margin: 0 2px;
  }

  :deep(.el-pager li.is-active) {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.4);
    color: $cyan-primary;
  }

  :deep(.btn-prev),
  :deep(.btn-next) {
    background: $layer-2 !important;
    border: 1px solid $border-panel !important;
    color: $text-secondary !important;
    border-radius: 6px !important;
  }
}
</style>

<!-- 抽屉挂载在 body，需单独样式块 -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.rfq-basket-drawer {
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

  /* 与列表页底部「清空篮子」同款 label 链式按钮，嵌入说明句内 */
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
