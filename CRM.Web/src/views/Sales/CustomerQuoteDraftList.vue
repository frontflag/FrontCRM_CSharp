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
        <router-link to="/customer-quotes" class="btn-ghost btn-sm">
          {{ t('customerQuoteDraftList.backToQuotes') }}
        </router-link>
      </div>
    </div>

    <div class="table-wrapper customer-quote-draft-list-table-scroll" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="customer-quote-draft-list-v2"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-key="id"
        highlight-current-row
        @selection-change="onSelectionChange"
      >
        <template #col-status>
          <el-tag size="small" effect="plain">{{ t('customerQuoteDraftList.statusDraft') }}</el-tag>
        </template>
        <template #col-createTime="{ row }">
          <template v-for="p in [formatDateTimeParts(row.createTime)]" :key="`ct-${row.id}`">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-purchasePrice="{ row }">
          <span class="amount-with-code">
            <span class="dock-tier-amt">{{ formatPrice(row.purchasePrice) }}</span>
            <span :class="['dock-tier-ccy', purchaseCurrencyClass(row.purchaseCurrency)]">
              {{ currencyLabel(row.purchaseCurrency) }}
            </span>
          </span>
        </template>
        <template #col-sourceQuoteDate="{ row }">
          {{ formatDateOnly(row.sourceQuoteDate) }}
        </template>
        <template #col-actions="{ row }">
          <div class="action-btns" @click.stop @dblclick.stop>
            <el-button
              v-if="canWrite"
              class="action-btn action-btn--danger"
              link
              type="danger"
              size="small"
              @click.stop="handleDelete(row)"
            >
              {{ t('customerQuoteDraftList.delete') }}
            </el-button>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div v-if="pageInfo.total > 0" class="pagination-wrapper">
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

        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          {{ t('customerQuoteDraftList.basket') }}<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          {{ t('customerQuoteDraftList.clearBasket') }}
        </el-button>
        <button
          v-if="canWrite"
          type="button"
          class="btn-primary btn-sm basket-batch-purchase-btn"
          :disabled="!basketCount || generating"
          @click="handleGenerate"
        >
          <el-icon v-if="generating" class="toolbar-action-icon is-loading"><Loading /></el-icon>
          <el-icon v-else class="toolbar-action-icon"><Document /></el-icon>
          {{ generating ? '…' : t('customerQuoteDraftList.generate') }}
        </button>
      </div>
      <el-pagination
        v-model:current-page="pageInfo.page"
        v-model:page-size="pageInfo.pageSize"
        class="quantum-pagination"
        :total="pageInfo.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      :title="t('customerQuoteDraftList.basketTitle')"
      direction="rtl"
      size="min(560px, 94vw)"
      class="customer-quote-draft-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('customerQuoteDraftList.basketEmpty') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('customerQuoteDraftList.basketSummary', { count: basketCount }) }}
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            {{ t('customerQuoteDraftList.clearBasket') }}
          </el-button>
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column prop="mpn" :label="t('customerQuoteDraftList.colMpn')" min-width="140" show-overflow-tooltip />
            <el-table-column prop="customerName" :label="t('customerQuoteDraftList.colCustomer')" min-width="120" show-overflow-tooltip />
            <el-table-column :label="t('customerQuoteDraftList.colActions')" width="88" align="center" fixed="right">
              <template #default="{ row }">
                <el-button link type="danger" size="small" @click="removeFromBasket(row.id)">
                  {{ t('customerQuoteDraftList.removeFromBasket') }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <button
          v-if="canWrite"
          type="button"
          class="btn-primary btn-sm basket-drawer-generate-btn"
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
import { Document, Loading, Setting } from '@element-plus/icons-vue'
import { storeToRefs } from 'pinia'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { customerQuoteApi, type CustomerQuoteDraftRow } from '@/api/customerQuote'
import { useAuthStore } from '@/stores/auth'
import { useCustomerQuoteDraftBasketStore } from '@/stores/customerQuoteDraftBasket'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
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
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const suppressBasketMerge = ref(false)
const basketDrawerVisible = ref(false)

const tableColumns = computed((): CrmTableColumnDef[] => {
  const cols: CrmTableColumnDef[] = [
    { key: 'selection', type: 'selection', width: 48, fixed: 'left' },
    { key: 'status', label: t('customerQuoteDraftList.colStatus'), width: 88 },
    { key: 'createTime', label: t('customerQuoteDraftList.colDraftDate'), width: 140 },
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
    cols.push({
      key: 'actions',
      label: t('customerQuoteDraftList.colActions'),
      width: 80,
      fixed: 'right',
      className: 'op-col',
      labelClassName: 'op-col'
    })
  }
  return cols
})

function formatDateTimeParts(v?: string | null) {
  if (!v) return null
  return formatDisplayDateTime2DigitYearParts(v)
}

function formatDateOnly(v?: string | null) {
  const p = formatDateTimeParts(v)
  return p ? p.date : '—'
}

function formatPrice(v?: number | null) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function currencyLabel(c?: number) {
  return listAmountCurrencyIso(c ?? 1)
}

function purchaseCurrencyClass(c?: number) {
  return listAmountCurrencyDockClass(c ?? 1)
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

function handleClearBasket() {
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
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
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  gap: 12px;
  flex-wrap: wrap;

  .header-left,
  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
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
  text-decoration: none;
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

.table-wrapper {
  flex: 1;
  min-height: 120px;
  min-height: 0;
  overflow: auto;
}

.pagination-wrapper {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
  margin-top: 12px;
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

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.basket-batch-purchase-btn {
  margin-left: 10px;
  letter-spacing: normal;

  &:hover:not(:disabled) {
    transform: none;
    box-shadow: none;
  }
}

.toolbar-action-icon {
  font-size: 15px;

  &.is-loading {
    animation: cq-draft-icon-spin 0.9s linear infinite;
  }
}

@keyframes cq-draft-icon-spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.pagination-wrapper .quantum-pagination {
  margin-left: auto;
  align-self: flex-start;

  :deep(.el-pagination__total) {
    color: $text-muted;
  }
}

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 6px;
}

.basket-drawer-hint,
.basket-drawer-summary {
  color: $text-secondary;
  margin-bottom: 12px;
}

.basket-drawer-generate-btn {
  margin-top: 16px;
}
</style>
