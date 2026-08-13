<template>
  <div class="finance-page">
    <h1 class="finance-list-page-title">{{ t('financeCustomerAdvanceList.pageTitle') }}</h1>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeCustomerAdvanceList.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select
          v-model="query.currency"
          :placeholder="t('financeCustomerAdvanceList.filters.currency')"
          clearable
          class="filter-select"
          @change="loadData"
        >
          <el-option
            v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-checkbox v-model="query.onlyPositiveBalance" @change="loadData">
          {{ t('financeCustomerAdvanceList.filters.onlyPositive') }}
        </el-checkbox>
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financeCustomerAdvanceList.filters.search') }}
        </el-button>
      </div>
      <div class="search-right">
        <button type="button" class="btn-export" :disabled="exporting" @click="() => void handleExport()">
          {{ t('financeCustomerAdvanceList.filters.export') }}
        </button>
        <el-button @click="goWriteOff">{{ t('financeCustomerAdvanceList.goWriteOff') }}</el-button>
      </div>
    </div>

    <CrmDataTable
      column-layout-key="finance-customer-advance-list-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :data="tableData"
      v-loading="loading"
    >
      <template #col-customerName="{ row }">
        <span>{{ row.customerName || row.customerId || '—' }}</span>
      </template>
      <template #col-currency="{ row }">{{ CURRENCY_MAP[row.currency] || row.currency }}</template>
      <template #col-balance="{ row }">{{ formatAmount(row.balance) }}</template>
      <template #col-totalIn="{ row }">{{ formatAmount(row.totalIn) }}</template>
      <template #col-totalApplied="{ row }">{{ formatAmount(row.totalApplied) }}</template>
      <template #col-actions="{ row }">
        <el-button link type="primary" size="small" @click="openLedger(row)">
          {{ t('financeCustomerAdvanceList.viewLedger') }}
        </el-button>
      </template>
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

    <el-drawer
      v-model="ledgerVisible"
      :title="t('financeCustomerAdvanceList.ledgerTitle')"
      size="min(96vw, 720px)"
      destroy-on-close
    >
      <el-table :data="ledgerRows" size="small" v-loading="ledgerLoading">
        <el-table-column :label="t('financeCustomerAdvanceList.ledgerType')" width="100">
          <template #default="{ row }">{{ ledgerTypeLabel(row.ledgerType) }}</template>
        </el-table-column>
        <el-table-column :label="t('financeCustomerAdvanceList.ledgerAmount')" width="120" align="right">
          <template #default="{ row }">{{ formatAmount(row.amount) }}</template>
        </el-table-column>
        <el-table-column :label="t('financeCustomerAdvanceList.ledgerBalanceAfter')" width="120" align="right">
          <template #default="{ row }">{{ formatAmount(row.balanceAfter) }}</template>
        </el-table-column>
        <el-table-column prop="remark" :label="t('financeCustomerAdvanceList.ledgerRemark')" min-width="160" show-overflow-tooltip />
        <el-table-column :label="t('financeCustomerAdvanceList.ledgerTime')" width="160">
          <template #default="{ row }">{{ formatDateTime(row.createTime) }}</template>
        </el-table-column>
      </el-table>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Search } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  financeCustomerAdvanceApi,
  type FinanceCustomerAdvance,
  type FinanceCustomerAdvanceLedger
} from '@/api/financeCustomerAdvance'
import { CURRENCY_MAP } from '@/api/finance'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { downloadCsvBlob } from '@/utils/exportFileName'

const { t } = useI18n()
const router = useRouter()

const loading = ref(false)
const exporting = ref(false)
const tableData = ref<FinanceCustomerAdvance[]>([])
const total = ref(0)

const query = reactive({
  keyword: '',
  currency: undefined as number | undefined,
  onlyPositiveBalance: true,
  page: 1,
  pageSize: 20
})

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'customerName', label: t('financeCustomerAdvanceList.columns.customer'), prop: 'customerName', minWidth: 160 },
  { key: 'currency', label: t('financeCustomerAdvanceList.columns.currency'), prop: 'currency', width: 100 },
  { key: 'balance', label: t('financeCustomerAdvanceList.columns.balance'), prop: 'balance', width: 130, align: 'right' },
  { key: 'totalIn', label: t('financeCustomerAdvanceList.columns.totalIn'), prop: 'totalIn', width: 130, align: 'right' },
  { key: 'totalApplied', label: t('financeCustomerAdvanceList.columns.totalApplied'), prop: 'totalApplied', width: 130, align: 'right' },
  { key: 'actions', label: t('financeCustomerAdvanceList.columns.actions'), prop: 'actions', width: 100, fixed: 'right' }
])

const ledgerVisible = ref(false)
const ledgerLoading = ref(false)
const ledgerRows = ref<FinanceCustomerAdvanceLedger[]>([])
const ledgerCustomerId = ref('')
const ledgerCurrency = ref<number | undefined>(undefined)

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDateTime(v?: string) {
  if (!v) return '—'
  return new Date(v).toLocaleString()
}

function ledgerTypeLabel(type: number) {
  const map: Record<number, string> = {
    10: t('financeCustomerAdvanceList.ledgerTypes.in'),
    20: t('financeCustomerAdvanceList.ledgerTypes.apply'),
    30: t('financeCustomerAdvanceList.ledgerTypes.autoIn')
  }
  return map[type] ?? String(type)
}

async function loadData() {
  loading.value = true
  try {
    const res = await financeCustomerAdvanceApi.getPaged({
      keyword: query.keyword || undefined,
      currency: query.currency,
      onlyPositiveBalance: query.onlyPositiveBalance,
      page: query.page,
      pageSize: query.pageSize
    })
    tableData.value = res.items ?? []
    total.value = res.total ?? 0
  } finally {
    loading.value = false
  }
}

async function handleExport() {
  try {
    await ElMessageBox.confirm(
      t('financeCustomerAdvanceList.messages.exportConfirmMessage'),
      t('financeCustomerAdvanceList.messages.exportConfirmTitle'),
      { type: 'warning', confirmButtonText: t('common.confirm'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }
  exporting.value = true
  try {
    const blob = await financeCustomerAdvanceApi.exportList({
      keyword: query.keyword || undefined,
      currency: query.currency,
      onlyPositiveBalance: query.onlyPositiveBalance
    })
    downloadCsvBlob(blob, '预收款.csv')
    ElMessage.success(t('financeCustomerAdvanceList.messages.exportSuccess'))
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('financeCustomerAdvanceList.messages.exportFailed'))
  } finally {
    exporting.value = false
  }
}

async function openLedger(row: FinanceCustomerAdvance) {
  ledgerCustomerId.value = row.customerId
  ledgerCurrency.value = row.currency
  ledgerVisible.value = true
  ledgerLoading.value = true
  try {
    const res = await financeCustomerAdvanceApi.getLedger({
      customerId: row.customerId,
      currency: row.currency,
      page: 1,
      pageSize: 100
    })
    ledgerRows.value = res.items ?? []
  } finally {
    ledgerLoading.value = false
  }
}

function goWriteOff() {
  router.push({ name: 'FinanceReceiptWriteOff' })
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import './finance-common.scss';
</style>
