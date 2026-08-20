<template>
  <div class="approval-desktop-queue-panel">
    <div class="approval-desktop-queue-panel__filter">
      <span class="approval-desktop-queue-panel__filter-label">{{ t('approvalDesktop.filters.bizType') }}</span>
      <el-select
        :model-value="bizTypeFilter"
        :placeholder="t('approvalDesktop.filters.all')"
        clearable
        class="approval-desktop-queue-panel__select"
        @update:model-value="onFilterChange"
      >
        <el-option :label="t('pendingApprovals.bizType.CUSTOMER')" value="CUSTOMER" />
        <el-option :label="t('pendingApprovals.bizType.VENDOR')" value="VENDOR" />
        <el-option :label="t('pendingApprovals.bizType.SALES_ORDER')" value="SALES_ORDER" />
        <el-option :label="t('pendingApprovals.bizType.PURCHASE_ORDER')" value="PURCHASE_ORDER" />
        <el-option :label="t('pendingApprovals.bizType.FINANCE_PAYMENT')" value="FINANCE_PAYMENT" />
      </el-select>
    </div>
    <div ref="listEl" class="approval-desktop-queue-panel__list" v-loading="loading">
      <button
        v-for="item in filteredList"
        :key="approvalItemKey(item)"
        type="button"
        class="ad-queue-item"
        :class="{ 'is-selected': selectedKey === approvalItemKey(item) }"
        :data-ad-key="approvalItemKey(item)"
        @click="onSelect(item)"
      >
        <div class="ad-queue-item__head">
          <el-tag effect="dark" :type="getBizTypeTagType(item.bizType)" size="small">
            {{ item.bizTypeName || getBizTypeText(item.bizType) }}
          </el-tag>
          <span class="ad-queue-item__code" :title="item.documentCode">{{ item.documentCode || '—' }}</span>
          <el-tag
            v-if="isStockingPurchaseOrder(item)"
            type="warning"
            effect="plain"
            size="small"
            round
            class="ad-queue-item__stocking"
          >
            {{ t('approvalDesktop.tags.stocking') }}
          </el-tag>
        </div>
        <div class="ad-queue-item__party" :title="displayCounterpartyName(item)">
          {{ displayCounterpartyName(item) }}
        </div>
        <div class="ad-queue-item__time">{{ formatDate(item.createdAt) }}</div>
      </button>
      <div v-if="!loading && filteredList.length === 0" class="approval-desktop-queue-panel__empty">
        {{ t('approvalDesktop.empty.queue') }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import type { BizType, PendingApprovalItem } from '@/api/approvals'
import {
  approvalItemKey,
  useApprovalDesktopQueueStore
} from '@/stores/approvalDesktopQueue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { PO_TYPE_STOCKING } from '@/utils/purchaseOrderItemLinkRules'

const { t, te } = useI18n()
const queueStore = useApprovalDesktopQueueStore()
const { loading, filteredList, selectedKey, bizTypeFilter, scrollToSelectedNonce } =
  storeToRefs(queueStore)
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const listEl = ref<HTMLElement | null>(null)

async function scrollSelectedIntoView() {
  await nextTick()
  const key = selectedKey.value
  if (!key || !listEl.value) return
  const el = listEl.value.querySelector(`[data-ad-key="${CSS.escape(key)}"]`) as HTMLElement | null
  el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

watch(
  () => `${selectedKey.value}:${scrollToSelectedNonce.value}`,
  () => {
    void scrollSelectedIntoView()
  }
)

onMounted(() => {
  // 队列由 ApprovalDesktop 统一 refreshAll，避免与主区并发打满 summary
  void scrollSelectedIntoView()
})

function onFilterChange(v: '' | BizType | null | undefined) {
  queueStore.setBizTypeFilter((v || '') as '' | BizType)
}

function onSelect(item: PendingApprovalItem) {
  queueStore.selectItem(item)
}

function isStockingPurchaseOrder(item: PendingApprovalItem): boolean {
  return (
    item.bizType === 'PURCHASE_ORDER' && Number(item.purchaseOrderType) === PO_TYPE_STOCKING
  )
}

function displayCounterpartyName(row: PendingApprovalItem): string {
  const bt = String(row.bizType || '')
  if (maskPurchaseSensitiveFields.value && (bt === 'VENDOR' || bt === 'PURCHASE_ORDER' || bt === 'FINANCE_PAYMENT')) {
    return '—'
  }
  if (maskSaleSensitiveFields.value && (bt === 'CUSTOMER' || bt === 'SALES_ORDER' || bt === 'FINANCE_RECEIPT')) {
    return '—'
  }
  return row.counterpartyName || '—'
}

const getBizTypeText = (type: string) => {
  const key = `pendingApprovals.bizType.${type}` as const
  return te(key) ? t(key) : type
}

const getBizTypeTagType = (type: string) => {
  const map: Record<string, string> = {
    CUSTOMER: 'success',
    VENDOR: 'warning',
    SALES_ORDER: 'primary',
    PURCHASE_ORDER: 'warning',
    FINANCE_RECEIPT: 'success',
    FINANCE_PAYMENT: 'danger'
  }
  return map[type] || 'info'
}

const formatDate = (dateStr: string) => formatDisplayDateTime(dateStr)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.approval-desktop-queue-panel {
  display: flex;
  flex-direction: column;
  min-width: 0;

  &__filter {
    padding: 0 0 12px;
    border-bottom: 1px solid $border-card;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  &__filter-label {
    font-size: 12px;
    color: $text-muted;
  }

  &__select {
    width: 100%;
  }

  &__list {
    padding: 8px 0 4px;
  }

  &__empty {
    color: $text-muted;
    font-size: 13px;
    line-height: 1.5;
    padding: 24px 12px;
    text-align: center;
  }
}

.ad-queue-item {
  display: block;
  width: 100%;
  text-align: left;
  border: 1px solid transparent;
  border-radius: 8px;
  padding: 10px 10px 8px;
  margin-bottom: 6px;
  background: transparent;
  cursor: pointer;
  color: inherit;
  transition: background 0.15s ease, border-color 0.15s ease;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
  }

  &.is-selected {
    background: rgba(0, 212, 255, 0.1);
    border-color: rgba(0, 212, 255, 0.35);
  }

  &__head {
    display: flex;
    align-items: center;
    gap: 8px;
    min-width: 0;
  }

  &__code {
    flex: 1;
    min-width: 0;
    font-size: 13px;
    font-weight: 600;
    color: $text-primary;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__stocking {
    flex-shrink: 0;
    margin-left: auto;
  }

  &__party {
    margin-top: 6px;
    font-size: 12px;
    color: $text-secondary;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__time {
    margin-top: 4px;
    font-size: 11px;
    color: $text-muted;
  }
}
</style>
