<template>
  <div class="finance-page stock-accumulated-items-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link type="primary" @click="goBack">{{ t('stockAccumulated.actions.backToSummary') }}</el-button>
        <h1 class="finance-list-page-title">{{ t('stockAccumulated.detailTitle', { month: monthLabel }) }}</h1>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="filters.queryKeywords"
          class="search-input"
          clearable
          :placeholder="t('stockAccumulated.filters.keywordsPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <el-input
          v-model="filters.pn"
          class="search-input"
          clearable
          :placeholder="t('stockAccumulated.filters.pnPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <el-input
          v-model="filters.stockInCode"
          class="search-input"
          clearable
          :placeholder="t('stockAccumulated.filters.stockInCodePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('stockAccumulated.filters.stockInTimeStart')"
          :end-placeholder="t('stockAccumulated.filters.stockInTimeEnd')"
          @change="() => void fetchList(true)"
        />
        <el-button type="primary" @click="() => void fetchList(true)">{{ t('stockAccumulated.actions.search') }}</el-button>
      </div>
    </div>

    <el-table v-loading="loading" :data="list" stripe>
      <el-table-column prop="billCode" :label="t('stockAccumulated.columns.billCode')" min-width="140" />
      <el-table-column prop="pn" :label="t('stockAccumulated.columns.pn')" min-width="160" />
      <el-table-column :label="t('stockAccumulated.columns.stockInTime')" min-width="120">
        <template #default="{ row }">{{ formatDate(row.stockInTime) }}</template>
      </el-table-column>
      <el-table-column prop="stockInQty" :label="t('stockAccumulated.columns.stockInQty')" min-width="100" align="right" />
      <el-table-column prop="stockOutQty" :label="t('stockAccumulated.columns.stockOutQty')" min-width="100" align="right" />
      <el-table-column prop="prvQty" :label="t('stockAccumulated.columns.prvQty')" min-width="100" align="right" />
      <el-table-column prop="balanceQty" :label="t('stockAccumulated.columns.balanceQty')" min-width="100" align="right" />
      <el-table-column :label="t('stockAccumulated.columns.prvAmountTotal')" min-width="120" align="right">
        <template #default="{ row }">{{ formatUsd(row.prvAmountTotal) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockAccumulated.columns.currentStockInAmountTotal')" min-width="120" align="right">
        <template #default="{ row }">{{ formatUsd(row.currentStockInAmountTotal) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockAccumulated.columns.currentStockOutAmountTotal')" min-width="120" align="right">
        <template #default="{ row }">{{ formatUsd(row.currentStockOutAmountTotal) }}</template>
      </el-table-column>
      <el-table-column :label="t('stockAccumulated.columns.balanceAmountTotal')" min-width="120" align="right">
        <template #default="{ row }">{{ formatUsd(row.balanceAmountTotal) }}</template>
      </el-table-column>
    </el-table>

    <div class="pagination-wrap">
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        layout="total, prev, pager, next, sizes"
        :total="total"
        :page-sizes="[20, 50, 100]"
        @current-change="() => void fetchList()"
        @size-change="() => void fetchList(true)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  financeStockAccumulatedApi,
  type FinanceStockAccumulatedItemRow
} from '@/api/financeStockAccumulated'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const list = ref<FinanceStockAccumulatedItemRow[]>([])
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const maskAmounts = ref(false)
const dateRange = ref<[string, string] | null>(null)

const filters = reactive({
  queryKeywords: '',
  pn: '',
  stockInCode: ''
})

const month = computed(() => String(route.query.month ?? ''))
const monthLabel = computed(() => month.value || '—')

function formatUsd(value: number | null | undefined): string {
  if (maskAmounts.value || value == null) return '—'
  return `$ ${value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function formatDate(value: string): string {
  const parts = formatDisplayDateTime2DigitYearParts(value)
  return parts?.date ?? value
}

function goBack() {
  router.push({ name: 'FinanceStockAccumulatedList' })
}

function buildQuery() {
  return {
    month: month.value,
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
    ElMessage.warning(t('stockAccumulated.messages.monthRequired'))
    return
  }
  if (resetPage) page.value = 1
  loading.value = true
  try {
    const data = await financeStockAccumulatedApi.getStockItems(buildQuery())
    list.value = data.items ?? []
    total.value = data.total ?? 0
    maskAmounts.value = data.maskAmounts === true
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockAccumulated.messages.loadFailed')))
  } finally {
    loading.value = false
  }
}

watch(
  () => route.query.month,
  () => {
    void fetchList(true)
  }
)

onMounted(() => {
  if (!month.value) {
    ElMessage.warning(t('stockAccumulated.messages.monthRequired'))
    goBack()
    return
  }
  void fetchList(true)
})
</script>

<style scoped>
.stock-accumulated-items-page .page-header {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 16px;
}

.search-bar {
  margin-bottom: 16px;
}

.search-left {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.search-input {
  width: 180px;
}

.pagination-wrap {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
