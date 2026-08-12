<template>
  <div class="piwo-stock-in-panel">
    <p v-if="!row" class="piwo-stock-in-panel__hint">
      {{ t('purchaseInvoiceWriteOffDesktop.stockIn.empty') }}
    </p>
    <section v-else class="piwo-stock-in-panel__panel">
      <div class="piwo-stock-in-panel__kv">
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.stockInCode') }}</span>
          <span class="v">
            <router-link
              v-if="row.stockInId && row.stockInCode"
              class="piwo-stock-in-panel__link"
              :to="{ name: 'StockInDetail', params: { id: row.stockInId } }"
            >
              {{ row.stockInCode }}
            </router-link>
            <template v-else>{{ row.stockInCode || '—' }}</template>
          </span>
        </div>
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.stockInDate') }}</span>
          <span class="v">{{ formatDate(row.stockInDate) }}</span>
        </div>
        <div v-if="vendorName" class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.vendorName') }}</span>
          <span class="v" :title="vendorName">{{ vendorName }}</span>
        </div>
        <div v-if="vendorEnglishName" class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.vendorEnglishName') }}</span>
          <span class="v" :title="vendorEnglishName">{{ vendorEnglishName }}</span>
        </div>
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.purchaseUser') }}</span>
          <span class="v" :title="purchaseUserName">{{ purchaseUserName || '—' }}</span>
        </div>
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.inboundQty') }}</span>
          <span class="v">{{ formatQty(row.totalQuantity) }}</span>
        </div>
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.amount') }}</span>
          <span class="v">{{ formatAmountWithCurrency(row.totalAmount, row.currency) }}</span>
        </div>
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.invoiceMatchDone') }}</span>
          <span class="v">{{ formatAmountWithCurrency(row.invoiceMatchDone, row.currency) }}</span>
        </div>
        <div class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.invoiceMatchToBe') }}</span>
          <span class="v">{{ formatAmountWithCurrency(row.invoiceMatchToBe, row.currency) }}</span>
        </div>
        <div v-if="row.freightForwarderOrderNo" class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.freightForwarderOrderNo') }}</span>
          <span class="v" :title="row.freightForwarderOrderNo">{{ row.freightForwarderOrderNo }}</span>
        </div>
        <div v-if="row.purchaseOrderCodes" class="piwo-stock-in-panel__row">
          <span class="k">{{ t('purchaseInvoiceWriteOffDesktop.stockIn.purchaseOrderCodes') }}</span>
          <span class="v" :title="row.purchaseOrderCodes">{{ row.purchaseOrderCodes }}</span>
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
import { usePurchaseInvoiceWriteOffDesktopQueueStore } from '@/stores/purchaseInvoiceWriteOffDesktopQueue'

const { t } = useI18n()
const queueStore = usePurchaseInvoiceWriteOffDesktopQueueStore()
const { focusedStockIn, selected } = storeToRefs(queueStore)

const row = computed(() => focusedStockIn.value)

const vendorName = computed(
  () => row.value?.vendorName?.trim() || selected.value?.vendorName?.trim() || ''
)

const vendorEnglishName = computed(
  () =>
    row.value?.vendorEnglishName?.trim() ||
    selected.value?.vendorEnglishName?.trim() ||
    ''
)

const purchaseUserName = computed(() => row.value?.purchaseUserName?.trim() || '')

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

.piwo-stock-in-panel {
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
