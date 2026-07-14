<template>
  <!-- 业务列表页：检索条对齐《业务详情页面规范》§7.4 panel-search；表格见 CrmDataTable -->
  <div class="finance-page vendor-accumulated-page">
    <div class="page-header">
      <div class="header-left">
        <h1 class="finance-list-page-title">{{ t('vendorAccumulated.summaryTitle') }}</h1>
      </div>
      <div class="header-right">
        <div class="header-period-nav">
          <button
            type="button"
            class="period-step-btn"
            :aria-label="t('vendorAccumulated.actions.prevMonth')"
            :title="t('vendorAccumulated.actions.prevMonth')"
            @click="() => shiftMonth(-1)"
          >
            &lt;
          </button>
          <el-date-picker
            v-model="selectedMonth"
            type="month"
            value-format="YYYY-MM"
            class="header-filter"
            :placeholder="t('vendorAccumulated.filters.monthPlaceholder')"
            @change="() => void fetchList(true)"
          />
          <button
            type="button"
            class="period-step-btn"
            :aria-label="t('vendorAccumulated.actions.nextMonth')"
            :title="t('vendorAccumulated.actions.nextMonth')"
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
        :placeholder="t('vendorAccumulated.filters.vendorKeywordsPlaceholder')"
        @keyup.enter="() => void fetchList(true)"
      >
        <template #suffix>
          <button
            v-if="queryKeywords.trim()"
            type="button"
            class="panel-search__clear"
            :aria-label="t('common.clear')"
            :title="t('common.clear')"
            @click="clearVendorSearch"
          >
            <el-icon><CircleClose /></el-icon>
          </button>
        </template>
      </el-input>
      <el-button type="primary" size="small" class="panel-search__btn" @click="() => void fetchList(true)">
        {{ t('vendorAccumulated.actions.search') }}
      </el-button>
      <el-button size="small" class="panel-search__reset" @click="resetFilters">
        {{ t('vendorAccumulated.filters.reset') }}
      </el-button>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-vendor-accumulated-summary-v5"
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
      <template #col-vendorName="{ row }">
        <router-link
          v-if="canOpenVendorDetail(row.vendorId)"
          class="link-text"
          :to="{ name: 'VendorDetail', params: { id: row.vendorId!.trim() } }"
          @click.stop
          @dblclick.stop
        >
          {{ formatVendorName(row) }}
        </router-link>
        <span v-else>{{ formatVendorName(row) }}</span>
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
            {{ t('vendorAccumulated.actions.detail') }}
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
import { useFinanceAccumulatedPartyAccess } from '@/composables/useFinanceAccumulatedPartyAccess'
import {
  financeStockAccumulatedApi,
  type FinanceVendorAccumulatedRow
} from '@/api/financeStockAccumulated'
import { getApiErrorMessage } from '@/utils/apiError'

type VendorRow = FinanceVendorAccumulatedRow & { rowKey: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { dataTableRef, rowDensityToggleAnchorEl } = useFinanceAccumulatedTableFooter()
const { canOpenVendorDetail } = useFinanceAccumulatedPartyAccess()

const loading = ref(false)
const selectedMonth = ref(formatCurrentMonth())
const queryKeywords = ref('')
const rows = ref<VendorRow[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const maskAmounts = ref(false)

watch(total, () => {
  const maxP = Math.max(1, Math.ceil(total.value / pageSize.value) || 1)
  if (page.value > maxP) page.value = maxP
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const metricMinWidth = resolveAccumulatedSummaryMetricMinWidth((key) => t(`vendorAccumulated.columns.${key}`))
  return [
    { key: 'vendorName', label: t('vendorAccumulated.columns.vendorName'), prop: 'vendorName', minWidth: 180, showOverflowTooltip: true },
    ...ACCUMULATED_SUMMARY_METRIC_COLUMN_KEYS.map((key) =>
      buildAccumulatedSummaryMetricColumn(key, t(`vendorAccumulated.columns.${key}`), key, metricMinWidth)
    ),
    { key: 'actions', label: t('vendorAccumulated.columns.actions'), prop: 'actions', width: 90, minWidth: 90, fixed: 'right', className: 'op-col' }
  ]
})

function formatCurrentMonth(): string {
  const d = new Date()
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  return `${y}-${m}`
}

function shiftMonth(delta: number) {
  const [yRaw, mRaw] = selectedMonth.value.split('-')
  let y = Number(yRaw)
  let m = Number(mRaw)
  if (!Number.isFinite(y) || !Number.isFinite(m)) return
  m += delta
  while (m < 1) {
    m += 12
    y -= 1
  }
  while (m > 12) {
    m -= 12
    y += 1
  }
  selectedMonth.value = `${y}-${String(m).padStart(2, '0')}`
  void fetchList(true)
}

function formatVendorName(row: FinanceVendorAccumulatedRow): string {
  if (!row.vendorId) return t('vendorAccumulated.unspecified')
  return row.vendorName?.trim() || t('vendorAccumulated.unspecified')
}

function vendorRowKey(row: FinanceVendorAccumulatedRow): string {
  return row.vendorId?.trim() || '__unspecified__'
}

function goDetail(row: FinanceVendorAccumulatedRow) {
  router.push({
    name: 'FinanceVendorAccumulatedItemList',
    query: {
      month: selectedMonth.value,
      vendorId: row.vendorId ?? '',
      vendorName: row.vendorName?.trim() || ''
    }
  })
}

function onRowDblClick(row: VendorRow) {
  goDetail(row)
}

function clearVendorSearch() {
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
    ElMessage.warning(t('vendorAccumulated.messages.monthRequired'))
    return
  }
  if (resetPage) page.value = 1
  loading.value = true
  try {
    const data = await financeStockAccumulatedApi.getVendors({
      month: selectedMonth.value,
      queryKeywords: queryKeywords.value.trim() || undefined,
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = (data.items ?? []).map((row) => ({
      ...row,
      rowKey: vendorRowKey(row)
    }))
    total.value = data.totalCount ?? 0
    maskAmounts.value = data.maskAmounts === true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('vendorAccumulated.messages.loadFailed')))
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
