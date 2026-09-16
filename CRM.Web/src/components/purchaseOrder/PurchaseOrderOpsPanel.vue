<template>
  <div class="so-item-ops-root so-item-ops-root--embedded" aria-label="purchase-order-ops-panel">
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('purchaseOrderList.opsPanel.pickRow') }}
    </div>

    <div v-else class="so-item-ops-root__content so-item-ops-root__content--embedded">
      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('purchaseOrderList.opsPanel.purchaseOrderTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--po-header">
            <span class="ops-po-header__item">
              <span>{{ t('purchaseOrderList.opsPanel.purchaseOrderCode') }}：</span>
              <router-link
                v-if="purchaseOrderLink"
                :to="purchaseOrderLink"
                class="ops-po-code-link"
              >{{ purchaseOrderCode }}</router-link>
              <span v-else>{{ purchaseOrderCode }}</span>
            </span>
            <span class="ops-po-header__item">
              {{ t('purchaseOrderList.opsPanel.purchaseOrderStatus') }}：{{ purchaseOrderStatusText }}<template
                v-if="purchaseOrderAwaitingVendorConfirm"
              >，<span class="ops-po-status-hint">{{ t('purchaseOrderList.opsPanel.purchaseOrderStatusAwaitingVendorHint') }}</span></template>
            </span>
          </div>
          <div class="ops-overview-line ops-overview-line--vendor">
            <VendorNameReadonlyText
              :name-zh="vendorNameZh"
              :name-en="vendorNameEn"
              :masked="maskSensitive"
            />
          </div>
        </div>
      </section>

      <SalesOrderOpsDocumentsCard
        v-if="!maskSensitive && purchaseOrderId"
        biz-type="PURCHASE_ORDER"
        :biz-id="purchaseOrderId"
        i18n-prefix="purchaseOrderList.opsPanel"
        :storage-key="PO_OPS_DOCS_EXPANDED_STORAGE_KEY"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  purchaseOrderMainStatusLabel,
  purchaseOrderMainStatusAwaitingVendorConfirm
} from '@/constants/purchaseOrderStatus'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import SalesOrderOpsDocumentsCard from '@/components/RFQ/SalesOrderOpsDocumentsCard.vue'
import { PO_OPS_DOCS_EXPANDED_STORAGE_KEY } from '@/utils/salesOrderOpsDocuments'

const props = withDefaults(
  defineProps<{
    row: Record<string, unknown> | null
    maskSensitive?: boolean
  }>(),
  {
    maskSensitive: false
  }
)

const { t } = useI18n()

const purchaseOrderCode = computed(() => {
  const v = String(props.row?.purchaseOrderCode ?? props.row?.PurchaseOrderCode ?? '').trim()
  return v || '—'
})
const purchaseOrderId = computed(() => String(props.row?.id ?? props.row?.Id ?? '').trim())
const purchaseOrderLink = computed(() => {
  const id = purchaseOrderId.value
  if (!id || purchaseOrderCode.value === '—') return null
  return { name: 'PurchaseOrderDetail' as const, params: { id } }
})
const purchaseOrderStatusRaw = computed(() => props.row?.status ?? props.row?.Status)
const purchaseOrderStatusText = computed(() =>
  purchaseOrderMainStatusLabel(t, purchaseOrderStatusRaw.value)
)
const purchaseOrderAwaitingVendorConfirm = computed(() =>
  purchaseOrderMainStatusAwaitingVendorConfirm(purchaseOrderStatusRaw.value)
)

const vendorNameZh = computed(() => {
  const r = props.row
  if (!r) return ''
  return String(r.vendorName ?? r.VendorName ?? '').trim()
})
const vendorNameEn = computed(() => {
  const r = props.row
  if (!r) return ''
  return String(r.vendorEnglishName ?? r.VendorEnglishName ?? '').trim()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';
@import '@/assets/styles/variables.scss';

.ops-overview-line--po-header {
  display: flex;
  flex-wrap: nowrap;
  align-items: baseline;
}

.ops-po-header__item {
  flex: 0 0 50%;
  width: 50%;
  min-width: 0;
  box-sizing: border-box;
}

.ops-po-code-link {
  color: inherit;
  text-decoration: none;

  &:hover,
  &:focus,
  &:visited {
    color: inherit;
    text-decoration: none;
  }

  &:hover,
  &:focus-visible {
    color: var(--el-color-primary);
  }
}

.ops-overview-line--vendor {
  font-weight: 700;
}

.ops-po-status-hint {
  font-style: italic;
  color: $text-muted;
}
</style>
