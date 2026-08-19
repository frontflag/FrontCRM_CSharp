<template>
  <div
    class="so-item-flow-root so-item-flow-root--embedded"
    aria-label="purchase-order-item-flow-panel"
  >
    <div v-if="!row" class="so-item-flow-root__empty">
      {{ t('purchaseOrderItemList.flowPanel.pickRow') }}
    </div>

    <div v-else v-loading="loading" class="so-item-flow-root__content">
      <p v-if="loadError" class="so-item-flow-root__error">{{ loadError }}</p>

      <ol class="so-item-flow-timeline">
        <li
          v-for="station in stations"
          :key="station.key"
          class="so-item-flow-station"
          :class="{ 'is-main': station.key === 'purchaseOrderItem' }"
        >
          <div class="so-item-flow-station__rail">
            <span
              class="so-item-flow-station__dot"
              :class="`so-item-flow-station__dot--${station.stationStatus}`"
            />
          </div>
          <div class="so-item-flow-station__body">
            <div class="so-item-flow-station__head">
              <h3 class="so-item-flow-station__title">
                {{ t(station.titleKey) }}
                <FlowYouAreHereMark v-if="station.key === 'purchaseOrderItem'" />
              </h3>
              <span
                v-if="station.stationStatus !== 'empty'"
                class="so-item-flow-station__badge"
                :class="`so-item-flow-station__badge--${station.stationStatus}`"
              >
                {{ stationStatusLabel(station.stationStatus) }}
              </span>
            </div>

            <div v-if="station.cards.length === 0" class="so-item-flow-station__empty-hint">
              {{ t('purchaseOrderItemList.flowPanel.emptyStation') }}
            </div>

            <div v-else class="so-item-flow-cards">
              <article v-for="card in station.cards" :key="card.id" class="so-item-flow-card">
                <div class="so-item-flow-kv">
                  <template v-if="station.key === 'stockIn'">
                    <div class="so-item-flow-kv__cell so-item-flow-kv__cell--full">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.docNo') }}：</span>
                      <span class="so-item-flow-kv__value">
                        <router-link
                          v-if="card.docRoute && !maskSensitive"
                          class="link-text"
                          :to="toRouteLocation(card.docRoute)"
                        >
                          {{ card.docNo }}
                        </router-link>
                        <template v-else>{{ card.docNo }}</template>
                      </span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.stockInType') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.bizTypeText || '—' }}</span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.status') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.statusText || '—' }}</span>
                    </div>
                  </template>
                  <template v-else>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.docNo') }}：</span>
                      <span class="so-item-flow-kv__value">
                        <router-link
                          v-if="card.docRoute && !maskSensitive"
                          class="link-text"
                          :to="toRouteLocation(card.docRoute)"
                        >
                          {{ card.docNo }}
                        </router-link>
                        <template v-else>{{ card.docNo }}</template>
                      </span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.status') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.statusText || '—' }}</span>
                    </div>
                  </template>
                  <div class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.createdAt') }}：</span>
                    <span class="so-item-flow-kv__value">{{ formatPoFlowCardDate(card.createdAt) }}</span>
                  </div>
                  <div class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t(card.personRoleKey) }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.personName || '—' }}</span>
                  </div>
                  <template v-if="card.showVendor">
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.vendorName') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.vendorName || '—' }}</span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.vendorCode') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.vendorCode || '—' }}</span>
                    </div>
                  </template>
                  <div v-if="card.unitPriceText" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ priceLabel(station.key) }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.unitPriceText }}</span>
                  </div>
                  <div v-if="card.qtyText" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.qty') }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.qtyText }}</span>
                  </div>
                  <div
                    v-if="hasDescription(card.description)"
                    class="so-item-flow-kv__cell so-item-flow-kv__cell--full"
                  >
                    <span class="so-item-flow-kv__label">{{ t('purchaseOrderItemList.flowPanel.fields.description') }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.description }}</span>
                  </div>
                </div>
              </article>
            </div>
          </div>
        </li>
      </ol>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PurchaseOrderDetailTabAggregates } from '@/api/purchaseOrder'
import FlowYouAreHereMark from '@/components/Common/FlowYouAreHereMark.vue'
import {
  buildPurchaseOrderItemFlowStations,
  formatPoFlowCardDate,
  type FlowDocRoute,
  type PoFlowStationKey,
  type FlowStationStatus
} from '@/utils/purchaseOrderItemFlowPanel'

const props = withDefaults(
  defineProps<{
    row: Record<string, unknown> | null
    aggregates?: PurchaseOrderDetailTabAggregates | null
    loading?: boolean
    loadError?: string
    maskSensitive?: boolean
  }>(),
  {
    aggregates: null,
    loading: false,
    loadError: '',
    maskSensitive: false
  }
)

const { t } = useI18n()

const stations = computed(() =>
  buildPurchaseOrderItemFlowStations(props.row, props.aggregates, t as (key: string, ...args: unknown[]) => string, {
    maskSensitive: props.maskSensitive
  })
)

function stationStatusLabel(status: FlowStationStatus) {
  if (status === 'done') return t('purchaseOrderItemList.flowPanel.stationDone')
  if (status === 'active') return t('purchaseOrderItemList.flowPanel.stationActive')
  return t('purchaseOrderItemList.flowPanel.stationEmpty')
}

function priceLabel(key: PoFlowStationKey) {
  if (key === 'paymentRequest' || key === 'payment' || key === 'purchaseInvoice') {
    return t('purchaseOrderItemList.flowPanel.fields.amount')
  }
  return t('purchaseOrderItemList.flowPanel.fields.unitPrice')
}

function toRouteLocation(route: FlowDocRoute) {
  return {
    name: route.name,
    params: route.params,
    query: route.query
  }
}

function hasDescription(v?: string | null) {
  const s = String(v ?? '').trim()
  return s.length > 0 && s !== '—'
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-flow-panel.scss';
</style>
