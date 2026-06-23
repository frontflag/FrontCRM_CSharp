<template>
  <div class="finance-page">
    <h1 class="finance-list-page-title">{{ t('financeReceivableList.pageTitle') }}</h1>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeReceivableList.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select
          v-model="query.verificationStatus"
          :placeholder="t('financeReceivableList.filters.verificationStatus')"
          clearable
          class="filter-select"
          @change="loadData"
        >
          <el-option :label="t('financeReceivableList.verification.pending')" :value="0" />
          <el-option :label="t('financeReceivableList.verification.partial')" :value="1" />
          <el-option :label="t('financeReceivableList.verification.complete')" :value="2" />
        </el-select>
        <el-checkbox v-model="query.onlyOpen" @change="loadData">
          {{ t('financeReceivableList.filters.onlyOpen') }}
        </el-checkbox>
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financeReceivableList.filters.search') }}
        </el-button>
      </div>
      <div class="search-right">
        <el-button v-if="canWriteFinanceReceipt" type="primary" @click="goWriteOff">
          {{ t('financeReceivableList.goWriteOff') }}
        </el-button>
      </div>
    </div>

    <CrmDataTable
      column-layout-key="finance-receivable-list-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :data="tableData"
      v-loading="loading"
    >
      <template #col-receivableCode="{ row }">
        <span class="code-text">{{ row.receivableCode || '—' }}</span>
      </template>
      <template #col-stockOutCode="{ row }">
        <router-link class="link-text" :to="`/inventory/stock-out/${row.stockOutId}`">
          {{ row.stockOutCode }}
        </router-link>
      </template>
      <template #col-verificationStatus="{ row }">
        <el-tag :type="verificationTagType(row.verificationStatus)" size="small">
          {{ verificationLabel(row.verificationStatus) }}
        </el-tag>
      </template>
      <template #col-amount="{ row }">{{ formatAmount(row.amount) }}</template>
      <template #col-verifiedDone="{ row }">{{ formatAmount(row.verifiedDone) }}</template>
      <template #col-verifiedToBe="{ row }">{{ formatAmount(row.verifiedToBe) }}</template>
      <template #col-stockOutDate="{ row }">{{ formatDate(row.stockOutDate) }}</template>
    </CrmDataTable>

    <div class="pagination-wrap">
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @current-change="loadData"
        @size-change="loadData"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Search } from '@element-plus/icons-vue'
import { financeReceivableApi, type FinanceReceivable } from '@/api/financeReceivable'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const { canWriteFinanceReceipt } = useFinanceWriteGate()

const loading = ref(false)
const tableData = ref<FinanceReceivable[]>([])
const total = ref(0)

const query = reactive({
  keyword: '',
  verificationStatus: undefined as number | undefined,
  onlyOpen: true,
  page: 1,
  pageSize: 20
})

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'receivableCode', prop: 'receivableCode', label: t('financeReceivableList.columns.code'), minWidth: 120 },
  { key: 'stockOutCode', prop: 'stockOutCode', label: t('financeReceivableList.columns.stockOutCode'), minWidth: 130 },
  { key: 'customerName', prop: 'customerName', label: t('financeReceivableList.columns.customer'), minWidth: 140 },
  { key: 'pn', prop: 'pn', label: t('financeReceivableList.columns.pn'), minWidth: 120 },
  { key: 'brand', prop: 'brand', label: t('financeReceivableList.columns.brand'), minWidth: 100 },
  { key: 'outboundQty', prop: 'outboundQty', label: t('financeReceivableList.columns.qty'), width: 90, align: 'right' },
  { key: 'amount', prop: 'amount', label: t('financeReceivableList.columns.amount'), width: 110, align: 'right' },
  { key: 'verifiedDone', prop: 'verifiedDone', label: t('financeReceivableList.columns.verifiedDone'), width: 110, align: 'right' },
  { key: 'verifiedToBe', prop: 'verifiedToBe', label: t('financeReceivableList.columns.verifiedToBe'), width: 110, align: 'right' },
  { key: 'verificationStatus', prop: 'verificationStatus', label: t('financeReceivableList.columns.verificationStatus'), width: 100 },
  { key: 'stockOutDate', prop: 'stockOutDate', label: t('financeReceivableList.columns.stockOutDate'), width: 120 }
])

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(v?: string) {
  if (!v) return '—'
  return v.slice(0, 10)
}

function verificationLabel(status: number) {
  if (status === 2) return t('financeReceivableList.verification.complete')
  if (status === 1) return t('financeReceivableList.verification.partial')
  return t('financeReceivableList.verification.pending')
}

function verificationTagType(status: number): 'success' | 'warning' | 'info' {
  if (status === 2) return 'success'
  if (status === 1) return 'warning'
  return 'info'
}

async function loadData() {
  loading.value = true
  try {
    const res = await financeReceivableApi.getPaged({
      keyword: query.keyword || undefined,
      verificationStatus: query.verificationStatus,
      onlyOpen: query.onlyOpen,
      page: query.page,
      pageSize: query.pageSize
    })
    tableData.value = res.items ?? []
    total.value = res.total ?? 0
  } finally {
    loading.value = false
  }
}

function syncQueryFromRoute() {
  if (route.name !== 'FinanceReceivableList') return
  const q = route.query
  if (q.onlyOpen === '0' || q.onlyOpen === 'false') {
    query.onlyOpen = false
  } else if (q.onlyOpen === '1' || q.onlyOpen === 'true') {
    query.onlyOpen = true
  }
}

watch(
  () => [route.name, route.query] as const,
  async () => {
    syncQueryFromRoute()
    if (route.name === 'FinanceReceivableList') await loadData()
  },
  { deep: true, immediate: true }
)

function goWriteOff() {
  router.push({ name: 'FinanceReceiptWriteOff' })
}
</script>

<style scoped lang="scss">
@import './finance-common.scss';

.pagination-wrap {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.link-text {
  color: var(--el-color-primary);
  text-decoration: none;
  &:hover { text-decoration: underline; }
}
</style>
