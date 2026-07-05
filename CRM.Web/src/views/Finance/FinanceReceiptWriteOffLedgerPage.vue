<template>
  <div class="finance-page">
    <div class="ledger-page-header">
      <h1 class="finance-list-page-title">{{ t('financeReceiptWriteOffLedger.pageTitle') }}</h1>
      <el-button @click="goBack">{{ t('financeReceiptWriteOffLedger.backToWriteOff') }}</el-button>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeReceiptWriteOffLedger.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financeReceiptWriteOffLedger.filters.search') }}
        </el-button>
      </div>
    </div>

    <CrmDataTable
      column-layout-key="finance-receipt-write-off-ledger-main"
      :columns="tableColumns"
      :show-column-settings="false"
      :data="tableData"
      v-loading="loading"
    >
      <template #col-createTime="{ row }">
        {{ row.createTime ? formatDisplayDateTime(row.createTime) : '—' }}
      </template>
      <template #col-writeOffSource="{ row }">
        {{ writeOffSourceLabel(row.writeOffSource) }}
      </template>
      <template #col-financeReceiptCode="{ row }">
        <router-link
          v-if="row.financeReceiptId && row.financeReceiptCode"
          class="link-text"
          :to="`/finance/receipts/${row.financeReceiptId}`"
        >
          {{ row.financeReceiptCode }}
        </router-link>
        <span v-else>{{ row.financeReceiptCode || '—' }}</span>
      </template>
      <template #col-receivableCode="{ row }">
        <router-link
          v-if="row.financeReceivableId && row.receivableCode"
          class="link-text"
          :to="`/finance/receivables/${row.financeReceivableId}`"
        >
          {{ row.receivableCode }}
        </router-link>
        <span v-else>{{ row.receivableCode || '—' }}</span>
      </template>
      <template #col-stockOutCode="{ row }">
        <router-link
          v-if="row.stockOutId && row.stockOutCode"
          class="link-text"
          :to="`/inventory/stock-out/${row.stockOutId}`"
        >
          {{ row.stockOutCode }}
        </router-link>
        <span v-else>{{ row.stockOutCode || '—' }}</span>
      </template>
      <template #col-sellOrderCode="{ row }">
        <router-link
          v-if="row.sellOrderId && row.sellOrderCode"
          class="link-text"
          :to="`/sales-orders/${row.sellOrderId}`"
        >
          {{ row.sellOrderCode }}
        </router-link>
        <span v-else>{{ row.sellOrderCode || '—' }}</span>
      </template>
      <template #col-customer="{ row }">
        {{ formatCustomerLabel(row) }}
      </template>
      <template #col-amount="{ row }">
        {{ maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(row.amount, row.currency) }}
      </template>
      <template #col-operatorUserName="{ row }">
        {{ row.operatorUserName || '—' }}
      </template>
      <template #col-remark="{ row }">
        {{ row.remark?.trim() || '—' }}
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
        @size-change="onPageSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Search } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { financeReceivableApi, type FinanceReceivableWriteOffLedgerItem } from '@/api/financeReceivable'
import { CURRENCY_MAP } from '@/api/finance'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatCustomerNameReadonlyFromRow } from '@/utils/customerDisplayName'

const { t } = useI18n()
const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const loading = ref(false)
const tableData = ref<FinanceReceivableWriteOffLedgerItem[]>([])
const total = ref(0)
const query = reactive({
  keyword: '',
  page: 1,
  pageSize: 20
})

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'createTime', prop: 'createTime', label: t('financeReceiptWriteOffLedger.columns.createTime'), width: 170, minWidth: 160 },
  { key: 'writeOffSource', prop: 'writeOffSource', label: t('financeReceiptWriteOffLedger.columns.source'), width: 100, minWidth: 92 },
  { key: 'financeReceiptCode', prop: 'financeReceiptCode', label: t('financeReceiptWriteOffLedger.columns.receiptCode'), width: 130, minWidth: 120 },
  { key: 'receivableCode', prop: 'receivableCode', label: t('financeReceiptWriteOffLedger.columns.receivableCode'), width: 130, minWidth: 120 },
  { key: 'stockOutCode', prop: 'stockOutCode', label: t('financeReceiptWriteOffLedger.columns.stockOutCode'), width: 130, minWidth: 120, showOverflowTooltip: true },
  { key: 'sellOrderCode', prop: 'sellOrderCode', label: t('financeReceiptWriteOffLedger.columns.sellOrderCode'), width: 130, minWidth: 120, showOverflowTooltip: true },
  { key: 'customer', prop: 'customer', label: t('financeReceiptWriteOffLedger.columns.customer'), minWidth: 180, showOverflowTooltip: true },
  { key: 'pn', prop: 'pn', label: t('financeReceiptWriteOffLedger.columns.pn'), minWidth: 120, showOverflowTooltip: true },
  { key: 'brand', prop: 'brand', label: t('financeReceiptWriteOffLedger.columns.brand'), width: 100, showOverflowTooltip: true },
  { key: 'amount', prop: 'amount', label: t('financeReceiptWriteOffLedger.columns.amount'), width: 150, minWidth: 140, align: 'right', labelClassName: 'receivable-amount-col', className: 'receivable-amount-col' },
  { key: 'operatorUserName', prop: 'operatorUserName', label: t('financeReceiptWriteOffLedger.columns.operator'), width: 110, showOverflowTooltip: true },
  { key: 'remark', prop: 'remark', label: t('financeReceiptWriteOffLedger.columns.remark'), minWidth: 140, showOverflowTooltip: true }
])

function writeOffSourceLabel(source?: number) {
  if (source === 20) return t('financeReceiptWriteOffLedger.writeOffSource.advancePool')
  return t('financeReceiptWriteOffLedger.writeOffSource.receiptItem')
}

function formatAmount(val?: number) {
  if (val == null) return '—'
  return val.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatAmountWithCurrency(amount?: number, currency?: number) {
  if (amount == null) return '—'
  const label = currency != null ? CURRENCY_MAP[currency] ?? String(currency) : ''
  return label ? `${formatAmount(amount)} ${label}` : formatAmount(amount)
}

function formatCustomerLabel(row: FinanceReceivableWriteOffLedgerItem) {
  return formatCustomerNameReadonlyFromRow({
    customerName: row.customerName,
    customerEnglishName: row.customerEnglishName
  })
}

async function loadData() {
  loading.value = true
  try {
    const res = await financeReceivableApi.getWriteOffLedger({
      keyword: query.keyword.trim() || undefined,
      page: query.page,
      pageSize: query.pageSize
    })
    tableData.value = res.items ?? []
    total.value = res.total ?? 0
  } finally {
    loading.value = false
  }
}

function onPageSizeChange() {
  query.page = 1
  void loadData()
}

function goBack() {
  router.push({ name: 'FinanceReceiptWriteOff' })
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import './finance-common.scss';

.ledger-page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.pagination-wrap {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.link-text {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}
</style>
