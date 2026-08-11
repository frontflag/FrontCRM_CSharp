<template>
  <div class="rwo-desktop-queue-panel">
    <div class="rwo-desktop-queue-panel__search">
      <el-input
        :model-value="keyword"
        size="small"
        clearable
        :placeholder="t('receiptWriteOffDesktop.queue.searchPh')"
        @update:model-value="onKeyword"
        @keyup.enter="onSearch"
      >
        <template #prefix>
          <el-icon><Search /></el-icon>
        </template>
      </el-input>
      <el-button type="primary" size="small" @click="onSearch">
        {{ t('receiptWriteOffDesktop.queue.search') }}
      </el-button>
      <el-dropdown trigger="click" @command="onSortCommand">
        <el-button
          size="small"
          class="rwo-desktop-queue-panel__sort-btn"
          :title="sortButtonTip"
          :aria-label="sortButtonTip"
        >
          <el-icon><Sort /></el-icon>
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="earliest" :class="{ 'is-active': sortBy === 'earliest' }">
              {{ t('receiptWriteOffDesktop.queue.sortEarliest') }}
            </el-dropdown-item>
            <el-dropdown-item command="latest" :class="{ 'is-active': sortBy === 'latest' }">
              {{ t('receiptWriteOffDesktop.queue.sortLatest') }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <div ref="listEl" class="rwo-desktop-queue-panel__list" v-loading="loading">
      <button
        v-for="item in pagedList"
        :key="receiptWriteOffCustomerKey(item)"
        type="button"
        class="rwo-queue-card"
        :class="{ 'is-selected': selectedKey === receiptWriteOffCustomerKey(item) }"
        :data-rwo-key="receiptWriteOffCustomerKey(item)"
        @click="onSelect(item)"
      >
        <div class="rwo-queue-card__names">
          <span class="rwo-queue-card__zh" :title="nameZh(item)">{{ nameZh(item) }}</span>
          <span
            v-if="hasEnglishName(item)"
            class="rwo-queue-card__en"
            :title="nameEn(item)"
          >{{ nameEn(item) }}</span>
        </div>
        <div class="rwo-queue-card__row">
          <span class="rwo-queue-card__label">{{ t('receiptWriteOffDesktop.queue.salesUser') }}</span>
          <span :title="item.salesUserName || undefined">{{ item.salesUserName || '—' }}</span>
        </div>
        <div class="rwo-queue-card__row rwo-queue-card__row--amount">
          <span class="rwo-queue-card__label">{{ t('receiptWriteOffDesktop.queue.pendingTotal') }}</span>
          <span class="rwo-queue-card__amount">{{ formatPendingTotal(item) }}</span>
        </div>
        <div class="rwo-queue-card__meta">
          <span>
            {{ t('receiptWriteOffDesktop.queue.receiptCount', { n: item.pendingReceiptItemCount ?? 0 }) }}
          </span>
        </div>
        <div class="rwo-queue-card__row">
          <span class="rwo-queue-card__label">{{ dateFieldLabel }}</span>
          <span>{{ formatDate(displayReceiptDate(item)) }}</span>
        </div>
      </button>
      <div v-if="!loading && !filteredTotal" class="rwo-queue-empty">
        {{ t('receiptWriteOffDesktop.empty.queue') }}
      </div>
    </div>

    <div v-if="filteredTotal > 0" class="rwo-desktop-queue-panel__pager">
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
import type { FinanceWriteOffCustomerSummary } from '@/api/financeReceivable'
import {
  receiptWriteOffCustomerKey,
  useReceiptWriteOffDesktopQueueStore
} from '@/stores/receiptWriteOffDesktopQueue'
import type { ReceiptWriteOffQueueSort } from '@/utils/receiptWriteOffQueueSort'

const { t } = useI18n()
const queueStore = useReceiptWriteOffDesktopQueueStore()
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
    ? t('receiptWriteOffDesktop.queue.sortLatest')
    : t('receiptWriteOffDesktop.queue.sortEarliest')
)

const sortButtonTip = computed(() =>
  t('receiptWriteOffDesktop.queue.sortTip', { field: dateFieldLabel.value })
)

function nameZh(row: FinanceWriteOffCustomerSummary) {
  return row.customerName?.trim() || row.customerId || '—'
}

function nameEn(row: FinanceWriteOffCustomerSummary) {
  return row.customerEnglishName?.trim() || ''
}

function hasEnglishName(row: FinanceWriteOffCustomerSummary) {
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

function formatPendingTotal(row: FinanceWriteOffCustomerSummary) {
  const amount = row.pendingWriteOffTotal ?? row.currencyTotals?.[0]?.amount
  const currency = row.currency ?? row.currencyTotals?.[0]?.currency
  const cur = currencyLabel(currency)
  return cur ? `${formatAmount(amount)} ${cur}` : formatAmount(amount)
}

function formatDate(v?: string | null) {
  if (!v) return '—'
  return String(v).slice(0, 10)
}

function displayReceiptDate(row: FinanceWriteOffCustomerSummary) {
  return sortBy.value === 'latest' ? row.latestReceiptDate : row.earliestReceiptDate
}

function onSelect(item: FinanceWriteOffCustomerSummary) {
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
    queueStore.setSortBy(cmd as ReceiptWriteOffQueueSort)
  }
}

function onPageChange(p: number) {
  queueStore.setPage(p)
}

async function scrollSelectedIntoView() {
  await nextTick()
  const key = selectedKey.value
  if (!key || !listEl.value) return
  const el = listEl.value.querySelector(`[data-rwo-key="${CSS.escape(key)}"]`) as HTMLElement | null
  el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

watch(scrollToSelectedNonce, () => {
  void scrollSelectedIntoView()
})

watch(selectedKey, () => {
  void scrollSelectedIntoView()
})
</script>

<style scoped>
.rwo-desktop-queue-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  gap: 8px;
  padding: 8px;
  box-sizing: border-box;
}

.rwo-desktop-queue-panel__search {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
  align-items: center;
}

.rwo-desktop-queue-panel__search .el-input {
  flex: 1;
  min-width: 0;
}

.rwo-desktop-queue-panel__sort-btn {
  flex-shrink: 0;
  padding-left: 8px;
  padding-right: 8px;
}

.rwo-desktop-queue-panel__list {
  flex: 1;
  min-height: 0;
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.rwo-desktop-queue-panel__pager {
  flex-shrink: 0;
  display: flex;
  justify-content: center;
  padding-top: 4px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.rwo-desktop-queue-panel__pager :deep(.el-pagination) {
  flex-wrap: wrap;
  justify-content: center;
  --el-pagination-button-width: 28px;
}

.rwo-queue-card {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 10px 10px 8px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-bg-color);
  text-align: left;
  cursor: pointer;
  color: inherit;
  font: inherit;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.rwo-queue-card:hover {
  border-color: var(--el-color-primary-light-5);
}

.rwo-queue-card.is-selected {
  border-color: var(--el-color-primary);
  box-shadow: inset 3px 0 0 var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}

.rwo-queue-card__names {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.rwo-queue-card__zh {
  font-weight: 600;
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rwo-queue-card__en {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rwo-queue-card__row {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 12px;
  min-width: 0;
}

.rwo-queue-card__row > span:last-child {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  text-align: right;
}

.rwo-queue-card__label {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.rwo-queue-card__amount {
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--el-color-primary);
}

.rwo-queue-card__meta {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-size: 11px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}

.rwo-queue-empty {
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
