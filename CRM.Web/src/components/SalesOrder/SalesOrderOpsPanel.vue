<template>
  <div class="so-item-ops-root so-item-ops-root--embedded" aria-label="sales-order-ops-panel">
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('salesOrderList.opsPanel.pickRow') }}
    </div>

    <div v-else class="so-item-ops-root__content so-item-ops-root__content--embedded">
      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('salesOrderList.opsPanel.salesOrderTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--so-header">
            <span class="ops-so-header__item">
              <span>{{ t('salesOrderList.opsPanel.salesOrderCode') }}：</span>
              <router-link
                v-if="salesOrderLink"
                :to="salesOrderLink"
                class="ops-so-code-link"
              >{{ salesOrderCode }}</router-link>
              <span v-else>{{ salesOrderCode }}</span>
            </span>
            <span class="ops-so-header__item">
              {{ t('salesOrderList.opsPanel.salesOrderStatus') }}：<span
                :class="{ 'ops-so-status--alert': salesOrderStatusIsAlert }"
              >{{ salesOrderStatusText }}</span>
            </span>
          </div>
          <div class="ops-overview-line ops-overview-line--customer">
            <CustomerNameReadonlyText
              :name-zh="customerNameZh"
              :name-en="customerNameEn"
              :masked="maskSensitive"
            />
          </div>
        </div>
      </section>

      <SalesOrderOpsDocumentsCard
        v-if="!maskSensitive && salesOrderId"
        biz-type="SALES_ORDER"
        :biz-id="salesOrderId"
        i18n-prefix="salesOrderList.opsPanel"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { translateSalesOrderStatus } from '@/constants/salesOrderStatus'
import CustomerNameReadonlyText from '@/components/Customer/CustomerNameReadonlyText.vue'
import SalesOrderOpsDocumentsCard from '@/components/RFQ/SalesOrderOpsDocumentsCard.vue'

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

const salesOrderCode = computed(() => {
  const v = String(props.row?.sellOrderCode ?? props.row?.SellOrderCode ?? '').trim()
  return v || '—'
})
const salesOrderId = computed(() => String(props.row?.id ?? props.row?.Id ?? '').trim())
const salesOrderLink = computed(() => {
  const id = salesOrderId.value
  if (!id || salesOrderCode.value === '—') return null
  return { name: 'SalesOrderDetail' as const, params: { id } }
})
const salesOrderStatus = computed(() => Number(props.row?.status ?? props.row?.Status))
const salesOrderStatusText = computed(() => {
  const s = salesOrderStatus.value
  if (!Number.isFinite(s)) return t('salesOrderList.status.unknown')
  return translateSalesOrderStatus(s, t)
})
const salesOrderStatusIsAlert = computed(() => salesOrderStatus.value === -1 || salesOrderStatus.value === -2)

const customerNameZh = computed(() => {
  const r = props.row
  if (!r) return ''
  return String(r.customerName ?? r.CustomerName ?? '').trim()
})
const customerNameEn = computed(() => {
  const r = props.row
  if (!r) return ''
  return String(r.customerEnglishName ?? r.CustomerEnglishName ?? '').trim()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';
@import '@/assets/styles/variables.scss';

.ops-overview-line--so-header {
  display: flex;
  flex-wrap: nowrap;
  align-items: baseline;
}

.ops-so-header__item {
  flex: 0 0 50%;
  width: 50%;
  min-width: 0;
  box-sizing: border-box;
}

.ops-so-code-link {
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

.ops-so-status--alert {
  color: $danger-color;
}

.ops-overview-line--customer {
  font-weight: 700;
}
</style>
