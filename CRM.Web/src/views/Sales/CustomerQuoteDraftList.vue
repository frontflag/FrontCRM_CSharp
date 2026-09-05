<template>
  <div class="customer-quote-draft-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">草</div>
          <h1 class="page-title">{{ t('customerQuoteDraftList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('customerQuoteDraftList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-ghost btn-sm" @click="basketDrawerVisible = true">
          {{ t('customerQuoteDraftList.basket') }}
          <span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </button>
        <button
          v-if="canWrite"
          type="button"
          class="btn-primary btn-sm"
          :disabled="!basketCount || generating"
          @click="handleGenerate"
        >
          {{ t('customerQuoteDraftList.generate') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="customer-quote-draft-list-v1"
        :columns="tableColumns"
        :show-column-settings="false"
        :data="rows"
        row-key="id"
        @selection-change="onSelectionChange"
      >
        <template #col-status>
          <el-tag size="small" effect="plain">{{ t('customerQuoteDraftList.statusDraft') }}</el-tag>
        </template>
        <template #col-createTime="{ row }">
          {{ formatDate(row.createTime) }}
        </template>
        <template #col-purchasePrice="{ row }">
          <span class="price-cell">
            {{ formatPrice(row.purchasePrice) }}
            <span class="ccy">{{ currencyLabel(row.purchaseCurrency) }}</span>
          </span>
        </template>
        <template #col-sourceQuoteDate="{ row }">
          {{ formatDate(row.sourceQuoteDate) }}
        </template>
        <template #col-actions="{ row }">
          <el-button
            v-if="canWrite"
            link
            type="danger"
            size="small"
            @click.stop="handleDelete(row)"
          >
            {{ t('customerQuoteDraftList.delete') }}
          </el-button>
        </template>
      </CrmDataTable>
    </div>

    <div class="pagination-bar">
      <el-pagination
        v-model:current-page="pageInfo.page"
        v-model:page-size="pageInfo.pageSize"
        :total="pageInfo.total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>

    <el-drawer v-model="basketDrawerVisible" :title="t('customerQuoteDraftList.basketTitle')" size="420px">
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('customerQuoteDraftList.basketEmpty') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('customerQuoteDraftList.basketSummary', { count: basketCount }) }}
        </p>
        <ul class="basket-list">
          <li v-for="item in basketItems" :key="item.id" class="basket-list-item">
            <div class="basket-list-main">
              <strong>{{ item.mpn || '—' }}</strong>
              <span>{{ item.customerName || '—' }}</span>
            </div>
            <button type="button" class="btn-ghost btn-xs" @click="removeFromBasket(item.id)">
              {{ t('customerQuoteDraftList.removeFromBasket') }}
            </button>
          </li>
        </ul>
        <button
          v-if="canWrite"
          type="button"
          class="btn-primary"
          :disabled="generating"
          @click="handleGenerate"
        >
          {{ t('customerQuoteDraftList.generate') }}
        </button>
      </template>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { storeToRefs } from 'pinia'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { customerQuoteApi, type CustomerQuoteDraftRow } from '@/api/customerQuote'
import { useAuthStore } from '@/stores/auth'
import { useCustomerQuoteDraftBasketStore } from '@/stores/customerQuoteDraftBasket'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { listAmountCurrencyIso } from '@/utils/moneyFormat'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const canWrite = computed(() => authStore.hasPermission('customer-quote.write'))

const basketStore = useCustomerQuoteDraftBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)

const loading = ref(false)
const generating = ref(false)
const rows = ref<CustomerQuoteDraftRow[]>([])
const totalCount = computed(() => pageInfo.value.total)
const pageInfo = ref({ page: 1, pageSize: 20, total: 0 })
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const suppressBasketMerge = ref(false)
const basketDrawerVisible = ref(false)

const tableColumns = computed((): CrmTableColumnDef[] => {
  const cols: CrmTableColumnDef[] = [
    { key: 'selection', type: 'selection', width: 48, fixed: 'left' },
    { key: 'status', label: t('customerQuoteDraftList.colStatus'), width: 88 },
    { key: 'createTime', label: t('customerQuoteDraftList.colDraftDate'), width: 120 },
    { key: 'customerName', label: t('customerQuoteDraftList.colCustomer'), minWidth: 140, prop: 'customerName' },
    { key: 'salesUserName', label: t('customerQuoteDraftList.colSales'), width: 100, prop: 'salesUserName' },
    { key: 'mpn', label: t('customerQuoteDraftList.colMpn'), minWidth: 140, prop: 'mpn' },
    { key: 'brand', label: t('customerQuoteDraftList.colBrand'), minWidth: 100, prop: 'brand' },
    { key: 'quantity', label: t('customerQuoteDraftList.colQty'), width: 90, prop: 'quantity', align: 'right' },
    { key: 'purchasePrice', label: t('customerQuoteDraftList.colPurchasePrice'), minWidth: 130 },
    { key: 'customerMpn', label: t('customerQuoteDraftList.colCustomerMpn'), minWidth: 120, prop: 'customerMpn' },
    { key: 'customerBrand', label: t('customerQuoteDraftList.colCustomerBrand'), minWidth: 100, prop: 'customerBrand' },
    { key: 'sourceQuoteCode', label: t('customerQuoteDraftList.colSourceQuote'), width: 120, prop: 'sourceQuoteCode' },
    { key: 'sourceQuoteDate', label: t('customerQuoteDraftList.colQuoteDate'), width: 120 },
    { key: 'purchaseUserName', label: t('customerQuoteDraftList.colPurchaser'), width: 100, prop: 'purchaseUserName' }
  ]
  if (canWrite.value) {
    cols.push({ key: 'actions', label: t('customerQuoteDraftList.colActions'), width: 80, fixed: 'right' })
  }
  return cols
})

function formatDate(v?: string | null) {
  if (!v) return '—'
  const p = formatDisplayDateTime2DigitYearParts(v)
  return p ? p.date : '—'
}

function formatPrice(v?: number | null) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function currencyLabel(c?: number) {
  return listAmountCurrencyIso(c ?? 1)
}

async function loadData() {
  loading.value = true
  try {
    const res = await customerQuoteApi.getDrafts({
      page: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize
    })
    rows.value = res.items || []
    pageInfo.value.total = res.total || 0
    const maxPage = Math.max(1, Math.ceil(pageInfo.value.total / pageInfo.value.pageSize) || 1)
    if (pageInfo.value.page > maxPage) {
      pageInfo.value.page = maxPage
      return loadData()
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteDraftList.loadFailed')))
  } finally {
    loading.value = false
  }
  await nextTick()
  await restoreTableSelectionFromBasket()
}

function onSelectionChange(selected: CustomerQuoteDraftRow[]) {
  if (suppressBasketMerge.value) return
  basketStore.mergePageSelection(rows.value, selected)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  await nextTick()
  table.clearSelection()
  await nextTick()
  for (const row of rows.value) {
    if (row.id && basketStore.has(row.id)) {
      table.toggleRowSelection(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeFromBasket(id: string) {
  basketStore.remove(id)
  suppressBasketMerge.value = true
  const row = rows.value.find((r) => r.id === id)
  if (row) dataTableRef.value?.toggleRowSelection(row, false)
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleDelete(row: CustomerQuoteDraftRow) {
  try {
    await ElMessageBox.confirm(t('customerQuoteDraftList.deleteConfirm'), t('common.confirm'), {
      type: 'warning'
    })
    await customerQuoteApi.deleteDraft(row.id)
    basketStore.remove(row.id)
    ElMessage.success(t('customerQuoteDraftList.deleteSuccess'))
    await loadData()
  } catch (e) {
    if (e === 'cancel') return
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteDraftList.deleteFailed')))
  }
}

async function handleGenerate() {
  if (!basketCount.value) {
    ElMessage.warning(t('customerQuoteDraftList.generateEmpty'))
    return
  }
  const customerIds = new Set(basketItems.value.map((x) => x.customerId || '').filter(Boolean))
  if (customerIds.size > 1) {
    ElMessage.error(t('customerQuoteDraftList.generateDifferentCustomer'))
    return
  }
  generating.value = true
  try {
    const quote = await customerQuoteApi.generateQuote(basketItems.value.map((x) => x.id))
    basketStore.clear()
    basketDrawerVisible.value = false
    ElMessage.success(t('customerQuoteDraftList.generateSuccess'))
    await router.push({ name: 'CustomerQuoteEdit', params: { id: quote.id } })
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteDraftList.generateFailed')))
  } finally {
    generating.value = false
  }
}

function handleSizeChange(val: number) {
  pageInfo.value.pageSize = val
  pageInfo.value.page = 1
  void loadData()
}

function handlePageChange(val: number) {
  pageInfo.value.page = val
  void loadData()
}

onMounted(() => {
  void loadData()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-quote-draft-list-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  padding: 24px;
  box-sizing: border-box;
  background: $layer-1;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: #e8f4ff;
  color: #1677ff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
}

.table-wrapper {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  padding-top: 12px;
}

.price-cell .ccy {
  margin-left: 4px;
  color: $text-secondary;
  font-size: 12px;
}

.basket-count-label {
  color: #1677ff;
}

.basket-drawer-hint,
.basket-drawer-summary {
  color: $text-secondary;
  margin-bottom: 12px;
}

.basket-list {
  list-style: none;
  padding: 0;
  margin: 0 0 16px;
}

.basket-list-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid $border-panel;
}

.basket-list-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
</style>
