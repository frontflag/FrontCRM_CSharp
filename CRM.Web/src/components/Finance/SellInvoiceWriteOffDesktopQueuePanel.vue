<template>
  <div class="siwo-desktop-queue-panel">
    <div class="siwo-desktop-queue-panel__search">
      <el-input
        :model-value="keyword"
        size="small"
        clearable
        :placeholder="t('sellInvoiceWriteOffDesktop.queue.searchPh')"
        @update:model-value="onKeyword"
        @keyup.enter="onSearch"
      >
        <template #prefix>
          <el-icon><Search /></el-icon>
        </template>
      </el-input>
      <el-button type="primary" size="small" @click="onSearch">
        {{ t('sellInvoiceWriteOffDesktop.queue.search') }}
      </el-button>
      <el-dropdown trigger="click" @command="onSortCommand">
        <el-button
          size="small"
          class="siwo-desktop-queue-panel__sort-btn"
          :title="sortButtonTip"
          :aria-label="sortButtonTip"
        >
          <el-icon><Sort /></el-icon>
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="earliest" :class="{ 'is-active': sortBy === 'earliest' }">
              {{ t('sellInvoiceWriteOffDesktop.queue.sortEarliest') }}
            </el-dropdown-item>
            <el-dropdown-item command="latest" :class="{ 'is-active': sortBy === 'latest' }">
              {{ t('sellInvoiceWriteOffDesktop.queue.sortLatest') }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <div ref="listEl" class="siwo-desktop-queue-panel__list" v-loading="loading">
      <button
        v-for="item in pagedList"
        :key="sellInvoiceWriteOffCustomerKey(item)"
        type="button"
        class="siwo-queue-card"
        :class="{ 'is-selected': selectedKey === sellInvoiceWriteOffCustomerKey(item) }"
        :data-siwo-key="sellInvoiceWriteOffCustomerKey(item)"
        @click="onSelect(item)"
      >
        <div class="siwo-queue-card__names">
          <span class="siwo-queue-card__zh" :title="nameZh(item)">{{ nameZh(item) }}</span>
          <span
            v-if="hasEnglishName(item)"
            class="siwo-queue-card__en"
            :title="nameEn(item)"
          >{{ nameEn(item) }}</span>
        </div>
        <div class="siwo-queue-card__row">
          <span class="siwo-queue-card__label">{{ t('sellInvoiceWriteOffDesktop.queue.salesUser') }}</span>
          <span :title="item.salesUserName || undefined">{{ item.salesUserName || '—' }}</span>
        </div>
        <div class="siwo-queue-card__row siwo-queue-card__row--amount">
          <span class="siwo-queue-card__label">{{ t('sellInvoiceWriteOffDesktop.queue.pendingTotal') }}</span>
          <span class="siwo-queue-card__amount">{{ formatPendingTotal(item) }}</span>
        </div>
        <div class="siwo-queue-card__meta">
          <span>
            {{ t('sellInvoiceWriteOffDesktop.queue.invoiceCount', { n: item.pendingInvoiceCount ?? 0 }) }}
          </span>
          <span :title="dateFieldLabel">{{ formatDate(displayInvoiceDate(item)) }}</span>
        </div>
      </button>
      <div v-if="!loading && !filteredTotal" class="siwo-queue-empty">
        {{ t('sellInvoiceWriteOffDesktop.empty.queue') }}
      </div>
    </div>

    <div v-if="filteredTotal > 0" class="siwo-desktop-queue-panel__pager">
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
import type { FinanceSellInvoiceWriteOffCustomerSummary } from '@/api/financeSellInvoiceWriteOff'
import {
  sellInvoiceWriteOffCustomerKey,
  useSellInvoiceWriteOffDesktopQueueStore
} from '@/stores/sellInvoiceWriteOffDesktopQueue'
import type { SellInvoiceWriteOffQueueSort } from '@/utils/sellInvoiceWriteOffQueueSort'

const { t } = useI18n()
const queueStore = useSellInvoiceWriteOffDesktopQueueStore()
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
    ? t('sellInvoiceWriteOffDesktop.queue.sortLatest')
    : t('sellInvoiceWriteOffDesktop.queue.sortEarliest')
)

const sortButtonTip = computed(() =>
  t('sellInvoiceWriteOffDesktop.queue.sortTip', { field: dateFieldLabel.value })
)

function nameZh(row: FinanceSellInvoiceWriteOffCustomerSummary) {
  return row.customerName?.trim() || row.customerId || '—'
}

function nameEn(row: FinanceSellInvoiceWriteOffCustomerSummary) {
  return row.customerEnglishName?.trim() || ''
}

function hasEnglishName(row: FinanceSellInvoiceWriteOffCustomerSummary) {
  return !!row.customerEnglishName?.trim()
}

function currencyLabel(currency?: number | null) {
  if (currency == null) return ''
  return CURRENCY_MAP[currency] ?? String(currency)
}

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatPendingTotal(row: FinanceSellInvoiceWriteOffCustomerSummary) {
  const amount = row.pendingWriteOffTotal
  const cur = currencyLabel(row.currency)
  return cur ? `${formatAmount(amount)} ${cur}` : formatAmount(amount)
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return String(v).slice(0, 10)
}

function displayInvoiceDate(row: FinanceSellInvoiceWriteOffCustomerSummary) {
  return sortBy.value === 'latest' ? row.latestInvoiceDate : row.earliestInvoiceDate
}

function onSelect(item: FinanceSellInvoiceWriteOffCustomerSummary) {
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
    queueStore.setSortBy(cmd as SellInvoiceWriteOffQueueSort)
  }
}

function onPageChange(p: number) {
  queueStore.setPage(p)
}

async function scrollSelectedIntoView() {
  await nextTick()
  const key = selectedKey.value
  if (!key || !listEl.value) return
  const el = listEl.value.querySelector(`[data-siwo-key="${CSS.escape(key)}"]`) as HTMLElement | null
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

.siwo-desktop-queue-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  gap: 8px;
  padding: 8px;
  box-sizing: border-box;
}

.siwo-desktop-queue-panel__search {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
  align-items: center;
}

.siwo-desktop-queue-panel__search .el-input {
  flex: 1;
  min-width: 0;
}

.siwo-desktop-queue-panel__sort-btn {
  flex-shrink: 0;
  padding-left: 8px;
  padding-right: 8px;
}

.siwo-desktop-queue-panel__list {
  flex: 1;
  min-height: 0;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.siwo-desktop-queue-panel__pager {
  flex-shrink: 0;
  display: flex;
  justify-content: center;
  padding-top: 4px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.siwo-desktop-queue-panel__pager :deep(.el-pagination) {
  flex-wrap: wrap;
  justify-content: center;
  --el-pagination-button-width: 28px;
}

.siwo-queue-card {
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

.siwo-queue-card:hover {
  background: rgba(0, 212, 255, 0.06);
}

.siwo-queue-card.is-selected {
  border-color: rgba(0, 212, 255, 0.35);
  background: #e5fbff;
}

.siwo-queue-card__names {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.siwo-queue-card__zh {
  font-weight: 600;
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.siwo-queue-card.is-selected .siwo-queue-card__zh,
.siwo-queue-card.is-selected .siwo-queue-card__en {
  color: $color-amber;
}

.siwo-queue-card__en {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.siwo-queue-card__row {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  min-width: 0;
}

.siwo-queue-card__row > span:last-child {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
}

.siwo-queue-card__label {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.siwo-queue-card__amount {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: $cyan-primary;
}

.siwo-queue-card__meta {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}

.siwo-queue-empty {
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
