<template>
  <div class="piwo-desktop-queue-panel">
    <div class="piwo-desktop-queue-panel__search">
      <el-input
        :model-value="keyword"
        size="small"
        clearable
        :placeholder="t('purchaseInvoiceWriteOffDesktop.queue.searchPh')"
        @update:model-value="onKeyword"
        @keyup.enter="onSearch"
      >
        <template #prefix>
          <el-icon><Search /></el-icon>
        </template>
      </el-input>
      <el-button type="primary" size="small" @click="onSearch">
        {{ t('purchaseInvoiceWriteOffDesktop.queue.search') }}
      </el-button>
      <el-dropdown trigger="click" @command="onSortCommand">
        <el-button
          size="small"
          class="piwo-desktop-queue-panel__sort-btn"
          :title="sortButtonTip"
          :aria-label="sortButtonTip"
        >
          <el-icon><Sort /></el-icon>
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="earliest" :class="{ 'is-active': sortBy === 'earliest' }">
              {{ t('purchaseInvoiceWriteOffDesktop.queue.sortEarliest') }}
            </el-dropdown-item>
            <el-dropdown-item command="latest" :class="{ 'is-active': sortBy === 'latest' }">
              {{ t('purchaseInvoiceWriteOffDesktop.queue.sortLatest') }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <div ref="listEl" class="piwo-desktop-queue-panel__list" v-loading="loading">
      <button
        v-for="item in pagedList"
        :key="purchaseInvoiceWriteOffVendorKey(item)"
        type="button"
        class="piwo-queue-card"
        :class="{ 'is-selected': selectedKey === purchaseInvoiceWriteOffVendorKey(item) }"
        :data-piwo-key="purchaseInvoiceWriteOffVendorKey(item)"
        @click="onSelect(item)"
      >
        <div class="piwo-queue-card__names">
          <span class="piwo-queue-card__zh" :title="nameZh(item)">{{ nameZh(item) }}</span>
          <span
            v-if="hasEnglishName(item)"
            class="piwo-queue-card__en"
            :title="nameEn(item)"
          >{{ nameEn(item) }}</span>
        </div>
        <div class="piwo-queue-card__row piwo-queue-card__row--amount">
          <span class="piwo-queue-card__label">{{ t('purchaseInvoiceWriteOffDesktop.queue.pendingTotal') }}</span>
          <span class="piwo-queue-card__amount">{{ formatPendingTotal(item) }}</span>
        </div>
        <div class="piwo-queue-card__meta">
          <span>
            {{ t('purchaseInvoiceWriteOffDesktop.queue.invoiceCount', { n: item.pendingInvoiceCount ?? 0 }) }}
          </span>
          <span :title="dateFieldLabel">{{ formatDate(displayInvoiceDate(item)) }}</span>
        </div>
      </button>
      <div v-if="!loading && !filteredTotal" class="piwo-queue-empty">
        {{ t('purchaseInvoiceWriteOffDesktop.empty.queue') }}
      </div>
    </div>

    <div v-if="filteredTotal > 0" class="piwo-desktop-queue-panel__pager">
      <el-pagination
        small
        layout="prev, pager, next"
        :current-page="page"
        :page-size="pageSize"
        :total="filteredTotal"
        @current-change="onPageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { Search, Sort } from '@element-plus/icons-vue'
import { CURRENCY_MAP } from '@/api/finance'
import type { FinancePurchaseInvoiceWriteOffVendorSummary } from '@/api/financePurchaseInvoiceWriteOff'
import {
  purchaseInvoiceWriteOffVendorKey,
  usePurchaseInvoiceWriteOffDesktopQueueStore
} from '@/stores/purchaseInvoiceWriteOffDesktopQueue'
import type { PurchaseInvoiceWriteOffQueueSort } from '@/utils/purchaseInvoiceWriteOffQueueSort'

const { t } = useI18n()
const queueStore = usePurchaseInvoiceWriteOffDesktopQueueStore()
const {
  loading,
  keyword,
  sortBy,
  page,
  pageSize,
  pagedList,
  filteredTotal,
  selectedKey,
  scrollToSelectedNonce
} = storeToRefs(queueStore)
const listEl = ref<HTMLElement | null>(null)

const dateFieldLabel = computed(() =>
  sortBy.value === 'latest'
    ? t('purchaseInvoiceWriteOffDesktop.queue.sortLatest')
    : t('purchaseInvoiceWriteOffDesktop.queue.sortEarliest')
)

const sortButtonTip = computed(() =>
  t('purchaseInvoiceWriteOffDesktop.queue.sortTip', { field: dateFieldLabel.value })
)

function nameZh(row: FinancePurchaseInvoiceWriteOffVendorSummary) {
  return row.vendorName?.trim() || row.vendorId || '—'
}

function nameEn(row: FinancePurchaseInvoiceWriteOffVendorSummary) {
  return row.vendorEnglishName?.trim() || ''
}

function hasEnglishName(row: FinancePurchaseInvoiceWriteOffVendorSummary) {
  return !!row.vendorEnglishName?.trim()
}

function currencyLabel(currency?: number | null) {
  if (currency == null) return ''
  return CURRENCY_MAP[currency] ?? String(currency)
}

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatPendingTotal(row: FinancePurchaseInvoiceWriteOffVendorSummary) {
  const amount = row.pendingWriteOffTotal
  const cur = currencyLabel(row.currency)
  return cur ? `${formatAmount(amount)} ${cur}` : formatAmount(amount)
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return String(v).slice(0, 10)
}

function displayInvoiceDate(row: FinancePurchaseInvoiceWriteOffVendorSummary) {
  return sortBy.value === 'latest' ? row.latestInvoiceDate : row.earliestInvoiceDate
}

function onSelect(item: FinancePurchaseInvoiceWriteOffVendorSummary) {
  queueStore.selectItem(item)
}

function onKeyword(v: string) {
  queueStore.setKeyword(v ?? '')
}

function onSearch() {
  queueStore.setKeyword(keyword.value)
}

function onSortCommand(cmd: string | number) {
  if (cmd === 'earliest' || cmd === 'latest') {
    queueStore.setSortBy(cmd as PurchaseInvoiceWriteOffQueueSort)
  }
}

function onPageChange(p: number) {
  queueStore.setPage(p)
}

async function scrollSelectedIntoView() {
  await nextTick()
  const key = selectedKey.value
  if (!key || !listEl.value) return
  const el = listEl.value.querySelector(`[data-piwo-key="${CSS.escape(key)}"]`) as HTMLElement | null
  el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

watch(scrollToSelectedNonce, () => {
  void scrollSelectedIntoView()
})

watch(selectedKey, () => {
  void scrollSelectedIntoView()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.piwo-desktop-queue-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  gap: 8px;
  padding: 8px;
  box-sizing: border-box;
}

.piwo-desktop-queue-panel__search {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
  align-items: center;
}

.piwo-desktop-queue-panel__search .el-input {
  flex: 1;
  min-width: 0;
}

.piwo-desktop-queue-panel__sort-btn {
  flex-shrink: 0;
  padding-left: 8px;
  padding-right: 8px;
}

.piwo-desktop-queue-panel__list {
  flex: 1;
  min-height: 0;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.piwo-desktop-queue-panel__pager {
  flex-shrink: 0;
  display: flex;
  justify-content: center;
  padding-top: 4px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.piwo-desktop-queue-panel__pager :deep(.el-pagination) {
  flex-wrap: wrap;
  justify-content: center;
  --el-pagination-button-width: 28px;
}

.piwo-queue-card {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 10px 10px 8px;
  border: 1px solid transparent;
  border-radius: 8px;
  background: transparent;
  text-align: left;
  cursor: pointer;
  color: inherit;
  font: inherit;
  transition: background 0.15s ease, border-color 0.15s ease;
}

.piwo-queue-card:hover {
  background: rgba(0, 212, 255, 0.06);
}

.piwo-queue-card.is-selected {
  border-color: rgba(0, 212, 255, 0.35);
  background: #e5fbff;
}

.piwo-queue-card__names {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.piwo-queue-card__zh {
  font-weight: 600;
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.piwo-queue-card.is-selected .piwo-queue-card__zh,
.piwo-queue-card.is-selected .piwo-queue-card__en {
  color: $color-amber;
}

.piwo-queue-card__en {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.piwo-queue-card__row {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  min-width: 0;
}

.piwo-queue-card__row > span:last-child {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
}

.piwo-queue-card__label {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.piwo-queue-card__amount {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: $cyan-primary;
}

.piwo-queue-card__meta {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}

.piwo-queue-empty {
  padding: 24px 8px;
  text-align: center;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

:deep(.el-dropdown-menu__item.is-active) {
  color: var(--el-color-primary);
  font-weight: 600;
}
</style>
