<template>
  <!-- 业务列表页：检索条对齐《业务详情页面规范》§7.4 panel-search；表格见 CrmDataTable -->
  <div class="finance-page customer-accumulated-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="finance-list-page-title">{{ t('customerAccumulated.summaryTitle') }}</h1>
      </div>
      <div class="header-right">
        <div class="header-period-nav">
          <button
            type="button"
            class="period-step-btn"
            :aria-label="t('customerAccumulated.actions.prevMonth')"
            :title="t('customerAccumulated.actions.prevMonth')"
            @click="() => shiftMonth(-1)"
          >
            &lt;
          </button>
          <el-date-picker
            v-model="selectedMonth"
            type="month"
            value-format="YYYY-MM"
            class="header-filter"
            :placeholder="t('customerAccumulated.filters.monthPlaceholder')"
            @change="() => void fetchList(true)"
          />
          <button
            type="button"
            class="period-step-btn"
            :aria-label="t('customerAccumulated.actions.nextMonth')"
            :title="t('customerAccumulated.actions.nextMonth')"
            @click="() => shiftMonth(1)"
          >
            &gt;
          </button>
        </div>
      </div>
    </div>

    <div class="panel-search">
      <el-input
        v-model="queryKeywords"
        class="panel-search__input"
        :placeholder="t('customerAccumulated.filters.customerKeywordsPlaceholder')"
        @keyup.enter="() => void fetchList(true)"
      >
        <template #suffix>
          <button
            v-if="queryKeywords.trim()"
            type="button"
            class="panel-search__clear"
            :aria-label="t('common.clear')"
            :title="t('common.clear')"
            @click="clearCustomerSearch"
          >
            <el-icon><CircleClose /></el-icon>
          </button>
        </template>
      </el-input>
      <el-button type="primary" size="small" class="panel-search__btn" @click="() => void fetchList(true)">
        {{ t('customerAccumulated.actions.search') }}
      </el-button>
      <el-button size="small" class="panel-search__reset" @click="resetFilters">
        {{ t('customerAccumulated.filters.reset') }}
      </el-button>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-customer-accumulated-summary-v1"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="rows"
      v-loading="loading"
      row-key="rowKey"
      row-class-name="table-row-pointer"
      highlight-current-row
      @row-dblclick="onRowDblClick"
    >
      <template #col-customerName="{ row }">
        <router-link
          v-if="canOpenCustomerDetail(row.customerId)"
          class="link-text"
          :to="{ name: 'CustomerDetail', params: { id: row.customerId!.trim() } }"
          @click.stop
          @dblclick.stop
        >
          {{ formatCustomerName(row) }}
        </router-link>
        <span v-else>{{ formatCustomerName(row) }}</span>
      </template>
      <template #col-prvAmountTotal="{ row }">
        <span class="accumulated-amount-neutral">{{ formatAccumulatedUsd(maskAmounts, row.prvAmountTotal) }}</span>
      </template>
      <template #col-currentStockInAmountTotal="{ row }">
        <span class="accumulated-amount-neutral">{{ formatAccumulatedUsd(maskAmounts, row.currentStockInAmountTotal) }}</span>
      </template>
      <template #col-currentStockOutAmountTotal="{ row }">
        <span class="accumulated-amount-neutral">{{ formatAccumulatedUsd(maskAmounts, row.currentStockOutAmountTotal) }}</span>
      </template>
      <template #col-balanceAmountTotal="{ row }">
        <span
          class="amount-text amount-text--balance dock-quote-tier-line"
          :class="{ 'accumulated-balance-negative': isAccumulatedNegative(row.balanceAmountTotal) }"
        >{{ formatAccumulatedUsd(maskAmounts, row.balanceAmountTotal) }}</span>
      </template>
      <template #col-prvStockQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.prvStockQty) }}</span>
      </template>
      <template #col-stockInQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.stockInQty) }}</span>
      </template>
      <template #col-stockOutQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.stockOutQty) }}</span>
      </template>
      <template #col-balanceStockQty="{ row }">
        <span
          class="accumulated-qty-balance"
          :class="{ 'accumulated-balance-negative': isAccumulatedNegative(row.balanceStockQty) }"
        >{{ formatAccumulatedQty(row.balanceStockQty) }}</span>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <el-button link type="primary" size="small" @click.stop="goDetail(row)">
            {{ t('customerAccumulated.actions.detail') }}
          </el-button>
        </div>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
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
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void fetchList()"
        @size-change="onPageSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { CircleClose, Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS,
  buildAccumulatedSummaryMetricColumn,
  formatAccumulatedQty,
  formatAccumulatedUsd,
  isAccumulatedNegative,
  resolveAccumulatedSummaryMetricMinWidth,
  useFinanceAccumulatedTableFooter
} from '@/composables/useFinanceAccumulatedListUi'
import { useFinanceAccumulatedMonthNav, formatFinanceAccumulatedMonth } from '@/composables/useFinanceAccumulatedMonthNav'
import { useFinanceAccumulatedPartyAccess } from '@/composables/useFinanceAccumulatedPartyAccess'
import {
  financeStockAccumulatedApi,
  type FinanceCustomerAccumulatedRow
} from '@/api/financeStockAccumulated'
import { getApiErrorMessage } from '@/utils/apiError'

type CustomerRow = FinanceCustomerAccumulatedRow & { rowKey: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { dataTableRef, rowDensityToggleAnchorEl } = useFinanceAccumulatedTableFooter()
const { canOpenCustomerDetail } = useFinanceAccumulatedPartyAccess()

const loading = ref(false)
const selectedMonth = ref(formatFinanceAccumulatedMonth())
const queryKeywords = ref('')
const rows = ref<CustomerRow[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const maskAmounts = ref(false)

const { shiftMonth } = useFinanceAccumulatedMonthNav(selectedMonth, () => {
  void fetchList(true)
})

watch(total, () => {
  const maxP = Math.max(1, Math.ceil(total.value / pageSize.value) || 1)
  if (page.value > maxP) page.value = maxP
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const metricMinWidth = resolveAccumulatedSummaryMetricMinWidth((key) => t(`customerAccumulated.columns.${key}`))
  return [
    { key: 'customerName', label: t('customerAccumulated.columns.customerName'), prop: 'customerName', minWidth: 180, showOverflowTooltip: true },
    ...ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS.map((key) =>
      buildAccumulatedSummaryMetricColumn(key, t(`customerAccumulated.columns.${key}`), key, metricMinWidth)
    ),
    { key: 'actions', label: t('customerAccumulated.columns.actions'), prop: 'actions', width: 90, minWidth: 90, fixed: 'right', className: 'op-col' }
  ]
})

function formatCustomerName(row: FinanceCustomerAccumulatedRow): string {
  if (!row.customerId) return t('customerAccumulated.unspecified')
  return row.customerName?.trim() || t('customerAccumulated.unspecified')
}

function customerRowKey(row: FinanceCustomerAccumulatedRow): string {
  return row.customerId?.trim() || '__unspecified__'
}

function goDetail(row: FinanceCustomerAccumulatedRow) {
  router.push({
    name: 'FinanceCustomerAccumulatedItemList',
    query: {
      month: selectedMonth.value,
      customerId: row.customerId ?? '',
      customerName: row.customerName?.trim() || ''
    }
  })
}

function onRowDblClick(row: CustomerRow) {
  goDetail(row)
}

function clearCustomerSearch() {
  queryKeywords.value = ''
  void fetchList(true)
}

function resetFilters() {
  queryKeywords.value = ''
  void fetchList(true)
}

function onPageSizeChange() {
  page.value = 1
  void fetchList()
}

async function fetchList(resetPage = false) {
  if (!selectedMonth.value) {
    ElMessage.warning(t('customerAccumulated.messages.monthRequired'))
    return
  }
  if (resetPage) page.value = 1
  loading.value = true
  try {
    const data = await financeStockAccumulatedApi.getCustomers({
      month: selectedMonth.value,
      queryKeywords: queryKeywords.value.trim() || undefined,
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = (data.items ?? []).map((row) => ({
      ...row,
      rowKey: customerRowKey(row)
    }))
    total.value = data.totalCount ?? 0
    maskAmounts.value = data.maskAmounts === true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerAccumulated.messages.loadFailed')))
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  const qMonth = String(route.query.month ?? '').trim()
  if (qMonth) selectedMonth.value = qMonth
  void fetchList(true)
})
</script>

<style scoped lang="scss">
@import './finance-accumulated-list-common.scss';

.header-filter {
  width: 160px;
}
</style>
