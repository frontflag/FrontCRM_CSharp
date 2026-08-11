<template>
  <div class="rwo-stock-out-panel">
    <p v-if="!row" class="rwo-stock-out-panel__hint">
      {{ t('receiptWriteOffDesktop.stockOut.empty') }}
    </p>
    <section v-else class="rwo-stock-out-panel__panel">
      <div class="rwo-stock-out-panel__kv">
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.stockOutCode') }}</span>
          <span class="v">
            <router-link
              v-if="row.stockOutId && row.stockOutCode"
              class="rwo-stock-out-panel__link"
              :to="{ name: 'StockOutDetail', params: { id: row.stockOutId } }"
            >
              {{ row.stockOutCode }}
            </router-link>
            <template v-else>{{ row.stockOutCode || '—' }}</template>
          </span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.stockOutDate') }}</span>
          <span class="v">{{ formatDate(row.stockOutDate) }}</span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.customerName') }}</span>
          <span class="v" :title="customerName">{{ customerName || '—' }}</span>
        </div>
        <div v-if="customerEnglishName" class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.customerEnglishName') }}</span>
          <span class="v" :title="customerEnglishName">{{ customerEnglishName }}</span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.salesUser') }}</span>
          <span class="v" :title="salesUserName">{{ salesUserName || '—' }}</span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.outboundQty') }}</span>
          <span class="v">{{ formatQty(row.outboundQty) }}</span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.amount') }}</span>
          <span class="v">{{ formatAmountWithCurrency(row.amount, row.currency) }}</span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.verifiedDone') }}</span>
          <span class="v">{{ formatAmountWithCurrency(row.verifiedDone, row.currency) }}</span>
        </div>
        <div class="rwo-stock-out-panel__row">
          <span class="k">{{ t('receiptWriteOffDesktop.stockOut.verifiedToBe') }}</span>
          <span class="v">{{ formatAmountWithCurrency(row.verifiedToBe, row.currency) }}</span>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { CURRENCY_MAP } from '@/api/finance'
import { useReceiptWriteOffDesktopQueueStore } from '@/stores/receiptWriteOffDesktopQueue'

const { t } = useI18n()
const queueStore = useReceiptWriteOffDesktopQueueStore()
const { focusedReceivable, selected } = storeToRefs(queueStore)

const row = computed(() => focusedReceivable.value)

const customerName = computed(
  () => row.value?.customerName?.trim() || selected.value?.customerName?.trim() || ''
)

const customerEnglishName = computed(
  () =>
    row.value?.customerEnglishName?.trim() ||
    selected.value?.customerEnglishName?.trim() ||
    ''
)

const salesUserName = computed(
  () => row.value?.salesUserName?.trim() || selected.value?.salesUserName?.trim() || ''
)

function formatDate(v?: string | null) {
  if (!v) return '—'
  return String(v).slice(0, 10)
}

function formatQty(v?: number) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return Number(v).toLocaleString(undefined, { maximumFractionDigits: 4 })
}

function formatAmount(v?: number) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatAmountWithCurrency(amount?: number, currency?: number) {
  if (amount == null || Number.isNaN(Number(amount))) return '—'
  if (currency == null) return formatAmount(amount)
  const label = CURRENCY_MAP[currency] ?? String(currency)
  return `${formatAmount(amount)} ${label}`
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.rwo-stock-out-panel {
  min-height: 120px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;

  &__hint {
    margin: 8px 4px;
    color: $text-secondary;
    font-size: 13px;
    line-height: 1.5;
  }

  &__panel {
    border: 1px solid rgba(0, 212, 255, 0.16);
    border-radius: 10px;
    padding: 10px 12px;
    background: rgba(0, 212, 255, 0.03);
  }

  &__kv {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  &__row {
    display: flex;
    gap: 10px;
    font-size: 12px;
    line-height: 1.5;

    .k {
      color: $text-muted;
      flex-shrink: 0;
      width: 7em;
    }

    .v {
      text-align: left;
      color: $text-primary;
      min-width: 0;
      word-break: break-word;
    }
  }

  &__link {
    color: var(--el-color-primary);
    text-decoration: none;

    &:hover {
      text-decoration: underline;
    }
  }
}
</style>
