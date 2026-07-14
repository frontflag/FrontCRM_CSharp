<template>
  <!-- 下钻明细：检索条对齐《业务详情页面规范》§7.4 panel-search；表格见 CrmDataTable -->
  <div class="finance-page vendor-accumulated-items-page">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('vendorAccumulated.actions.backToSummary') }}
        </button>
        <h1 class="finance-list-page-title accumulated-detail-title">
          <span>{{ t('vendorAccumulated.detailTitleBase') }}</span>
          <span class="accumulated-detail-title__sep"> · </span>
          <span>{{ monthLabel }}</span>
          <span class="accumulated-detail-title__sep"> · </span>
          <span class="accumulated-detail-title__vendor">{{ vendorLabel }}</span>
        </h1>
      </div>
    </div>

    <div class="detail-panel-section-body vendor-accumulated-items-panel">
      <div class="panel-search">
        <el-input
          v-model="filters.queryKeywords"
          class="panel-search__field"
          :placeholder="t('vendorAccumulated.filters.keywordsPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <el-input
          v-model="filters.pn"
          class="panel-search__field"
          :placeholder="t('vendorAccumulated.filters.pnPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <el-input
          v-model="filters.stockInCode"
          class="panel-search__field"
          :placeholder="t('vendorAccumulated.filters.stockInCodePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          class="panel-search__date-range"
          :start-placeholder="t('vendorAccumulated.filters.stockInTimeStart')"
          :end-placeholder="t('vendorAccumulated.filters.stockInTimeEnd')"
          clearable
          :teleported="false"
          @change="() => void fetchList(true)"
        />
        <el-button type="primary" size="small" class="panel-search__btn" @click="() => void fetchList(true)">
          {{ t('vendorAccumulated.actions.search') }}
        </el-button>
        <el-button size="small" class="panel-search__reset" @click="resetFilters">
          {{ t('vendorAccumulated.filters.reset') }}
        </el-button>
      </div>

      <div class="detail-items-table-wrap">
        <CrmDataTable
          ref="dataTableRef"
          column-layout-key="finance-vendor-accumulated-items-v2"
          :columns="tableColumns"
          :show-column-settings="false"
          :density-toggle-anchor-el="rowDensityToggleAnchorEl"
          :data="list"
          v-loading="loading"
          row-key="rowKey"
        >
      <template #col-billCode="{ row }">
        <router-link
          v-if="canOpenStockInDetail(row)"
          class="link-text"
          :to="{ name: 'StockInDetail', params: { id: row.stockInId.trim() } }"
        >
          {{ row.billCode }}
        </router-link>
        <span v-else class="code-text">{{ row.billCode || '—' }}</span>
      </template>
      <template #col-pn="{ row }">
        <span>{{ row.pn || '—' }}</span>
      </template>
      <template #col-stockInTime="{ row }">
        <template v-for="p in [formatAccumulatedDateTimeParts(row.stockInTime)]" :key="'in-' + row.rowKey">
          <span v-if="!p" class="inv-list-dash">—</span>
          <span v-else-if="isAccumulatedTimeMidnightOnly(p.time)" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
          </span>
          <span v-else class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
        </template>
      </template>
      <template #col-stockInQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.stockInQty) }}</span>
      </template>
      <template #col-stockOutQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.stockOutQty) }}</span>
      </template>
      <template #col-prvQty="{ row }">
        <span class="inv-list-qty">{{ formatAccumulatedQty(row.prvQty) }}</span>
      </template>
      <template #col-balanceQty="{ row }">
        <span
          class="accumulated-qty-balance"
          :class="{ 'accumulated-balance-negative': isAccumulatedNegative(row.balanceQty) }"
        >{{ formatAccumulatedQty(row.balanceQty) }}</span>
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
        </CrmDataTable>
      </div>

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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  formatAccumulatedDateTimeParts,
  formatAccumulatedQty,
  formatAccumulatedUsd,
  isAccumulatedNegative,
  isAccumulatedTimeMidnightOnly,
  useFinanceAccumulatedTableFooter
} from '@/composables/useFinanceAccumulatedListUi'
import { useFinanceAccumulatedPartyAccess } from '@/composables/useFinanceAccumulatedPartyAccess'
import {
  financeStockAccumulatedApi,
  type FinanceStockAccumulatedItemRow
} from '@/api/financeStockAccumulated'
import { getApiErrorMessage } from '@/utils/apiError'

type ItemRow = FinanceStockAccumulatedItemRow & { rowKey: string }

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { dataTableRef, rowDensityToggleAnchorEl } = useFinanceAccumulatedTableFooter()
const { canOpenStockInDetail } = useFinanceAccumulatedPartyAccess()

const loading = ref(false)
const list = ref<ItemRow[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const maskAmounts = ref(false)
const dateRange = ref<[string, string] | null>(null)
const vendorDisplayName = ref('')

const filters = reactive({
  queryKeywords: '',
  pn: '',
  stockInCode: ''
})

const month = computed(() => String(route.query.month ?? ''))
const vendorId = computed(() => String(route.query.vendorId ?? ''))
const monthLabel = computed(() => month.value || '—')

const vendorLabel = computed(() => {
  const qName = String(route.query.vendorName ?? '').trim()
  if (qName) return qName
  if (vendorDisplayName.value) return vendorDisplayName.value
  if (!vendorId.value) return t('vendorAccumulated.unspecified')
  return vendorId.value
})

watch(total, () => {
  const maxP = Math.max(1, Math.ceil(total.value / pageSize.value) || 1)
  if (page.value > maxP) page.value = maxP
})

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'billCode', label: t('vendorAccumulated.columns.billCode'), prop: 'billCode', minWidth: 140, showOverflowTooltip: true },
  { key: 'pn', label: t('vendorAccumulated.columns.pn'), prop: 'pn', minWidth: 160, showOverflowTooltip: true },
  { key: 'stockInTime', label: t('vendorAccumulated.columns.stockInTime'), prop: 'stockInTime', width: 120, minWidth: 110 },
  { key: 'stockInQty', label: t('vendorAccumulated.columns.stockInQty'), prop: 'stockInQty', width: 120, minWidth: 120, align: 'right' },
  { key: 'stockOutQty', label: t('vendorAccumulated.columns.stockOutQty'), prop: 'stockOutQty', width: 120, minWidth: 120, align: 'right' },
  { key: 'prvQty', label: t('vendorAccumulated.columns.prvQty'), prop: 'prvQty', width: 120, minWidth: 120, align: 'right' },
  { key: 'balanceQty', label: t('vendorAccumulated.columns.balanceQty'), prop: 'balanceQty', width: 120, minWidth: 120, align: 'right' },
  { key: 'prvAmountTotal', label: t('vendorAccumulated.columns.prvAmountTotal'), prop: 'prvAmountTotal', minWidth: 120, align: 'right' },
  { key: 'currentStockInAmountTotal', label: t('vendorAccumulated.columns.currentStockInAmountTotal'), prop: 'currentStockInAmountTotal', minWidth: 120, align: 'right' },
  { key: 'currentStockOutAmountTotal', label: t('vendorAccumulated.columns.currentStockOutAmountTotal'), prop: 'currentStockOutAmountTotal', minWidth: 120, align: 'right' },
  { key: 'balanceAmountTotal', label: t('vendorAccumulated.columns.balanceAmountTotal'), prop: 'balanceAmountTotal', minWidth: 120, align: 'right' }
])

function goBack() {
  router.push({ name: 'FinanceVendorAccumulatedList', query: month.value ? { month: month.value } : undefined })
}

function buildQuery() {
  return {
    month: month.value,
    vendorId: vendorId.value,
    queryKeywords: filters.queryKeywords.trim() || undefined,
    pn: filters.pn.trim() || undefined,
    stockInCode: filters.stockInCode.trim() || undefined,
    stockInTimeStart: dateRange.value?.[0],
    stockInTimeEnd: dateRange.value?.[1],
    page: page.value,
    pageSize: pageSize.value
  }
}

async function fetchList(resetPage = false) {
  if (!month.value) {
    ElMessage.warning(t('vendorAccumulated.messages.monthRequired'))
    return
  }
  if (resetPage) page.value = 1
  loading.value = true
  try {
    const data = await financeStockAccumulatedApi.getVendorItems(buildQuery())
    list.value = (data.items ?? []).map((row, idx) => ({
      ...row,
      rowKey: `${row.billCode ?? ''}:${row.pn ?? ''}:${row.stockInTime ?? ''}:${idx}`
    }))
    total.value = data.total ?? 0
    maskAmounts.value = data.maskAmounts === true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('vendorAccumulated.messages.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onPageSizeChange() {
  page.value = 1
  void fetchList()
}

function resetFilters() {
  filters.queryKeywords = ''
  filters.pn = ''
  filters.stockInCode = ''
  dateRange.value = null
  void fetchList(true)
}

async function resolveVendorLabel() {
  const qName = String(route.query.vendorName ?? '').trim()
  if (qName) {
    vendorDisplayName.value = qName
    return
  }
  if (!vendorId.value) {
    vendorDisplayName.value = t('vendorAccumulated.unspecified')
    return
  }
  if (!month.value) return
  try {
    const data = await financeStockAccumulatedApi.getVendors({
      month: month.value,
      page: 1,
      pageSize: 100
    })
    const hit = (data.items ?? []).find((x) => x.vendorId === vendorId.value)
    vendorDisplayName.value = hit?.vendorName?.trim() || vendorId.value
  } catch {
    vendorDisplayName.value = vendorId.value
  }
}

watch(
  () => [route.query.month, route.query.vendorId],
  () => {
    void resolveVendorLabel()
    void fetchList(true)
  }
)

onMounted(() => {
  if (!month.value) {
    ElMessage.warning(t('vendorAccumulated.messages.monthRequired'))
    goBack()
    return
  }
  void resolveVendorLabel()
  void fetchList(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import './finance-accumulated-list-common.scss';

.vendor-accumulated-items-panel {
  padding: 16px 20px 20px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
}

.vendor-accumulated-items-panel .pagination-wrap {
  margin-top: 12px;
  padding-top: 0;
  border-top: none;
}
</style>
