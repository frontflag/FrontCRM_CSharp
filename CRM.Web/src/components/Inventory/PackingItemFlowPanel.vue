<template>
  <div
    class="so-item-flow-root so-item-flow-root--embedded"
    aria-label="packing-item-flow-panel"
  >
    <div v-if="!row" class="so-item-flow-root__empty">
      {{ t('packingDetail.flowPanel.noSelection') }}
    </div>

    <div v-else v-loading="loading" class="so-item-flow-root__content">
      <p v-if="loadError" class="so-item-flow-root__error">{{ loadError }}</p>
      <p v-else-if="missingSellLink" class="so-item-flow-root__hint">
        {{ t('packingDetail.flowPanel.missingSellLink') }}
      </p>

      <ol class="so-item-flow-timeline">
        <li
          v-for="station in stations"
          :key="station.key"
          class="so-item-flow-station"
          :class="{ 'is-main': station.key === 'packing' }"
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
                <FlowYouAreHereMark v-if="station.key === 'packing'" />
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
              {{ t('salesOrderItemList.flowPanel.emptyStation') }}
            </div>

            <div v-else class="so-item-flow-cards">
              <article v-for="card in station.cards" :key="card.id" class="so-item-flow-card">
                <div class="so-item-flow-kv">
                  <div class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.docNo') }}：
                    </span>
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
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.status') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ card.statusText || '—' }}</span>
                  </div>
                  <div class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.createdAt') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ formatFlowCardDate(card.createdAt) }}</span>
                  </div>
                  <div class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t(card.personRoleKey) }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.personName || '—' }}</span>
                  </div>
                  <template v-if="card.showCustomer">
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">
                        {{ t('salesOrderItemList.flowPanel.fields.customerName') }}：
                      </span>
                      <span class="so-item-flow-kv__value">{{ card.customerName || '—' }}</span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">
                        {{ t('salesOrderItemList.flowPanel.fields.customerCode') }}：
                      </span>
                      <span class="so-item-flow-kv__value">{{ card.customerCode || '—' }}</span>
                    </div>
                  </template>
                  <div v-if="card.unitPriceText" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ priceLabel(station.key) }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.unitPriceText }}</span>
                  </div>
                  <div v-if="card.qtyText" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.qty') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ card.qtyText }}</span>
                  </div>
                  <div
                    v-if="hasDescription(card.description)"
                    class="so-item-flow-kv__cell so-item-flow-kv__cell--full"
                  >
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.description') }}：
                    </span>
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
import type { SalesOrderDetailTabAggregates } from '@/api/salesOrder'
import FlowYouAreHereMark from '@/components/Common/FlowYouAreHereMark.vue'
import {
  buildPackingItemFlowStations,
  formatFlowCardDate,
  type PackingFlowExtras
} from '@/utils/packingItemFlowPanel'
import type { FlowDocRoute, FlowStationStatus } from '@/utils/sellOrderItemFlowPanel'

const props = withDefaults(
  defineProps<{
    row: Record<string, unknown> | null
    aggregates?: SalesOrderDetailTabAggregates | null
    extras?: PackingFlowExtras | null
    loading?: boolean
    loadError?: string
    missingSellLink?: boolean
    maskSensitive?: boolean
  }>(),
  {
    aggregates: null,
    extras: null,
    loading: false,
    loadError: '',
    missingSellLink: false,
    maskSensitive: false
  }
)

const { t } = useI18n()

const stations = computed(() =>
  buildPackingItemFlowStations(props.row, props.aggregates, t as (key: string, ...args: unknown[]) => string, {
    maskSensitive: props.maskSensitive,
    extras: props.extras
  })
)

function stationStatusLabel(status: FlowStationStatus) {
  if (status === 'done') return t('salesOrderItemList.flowPanel.stationDone')
  if (status === 'active') return t('salesOrderItemList.flowPanel.stationActive')
  return t('salesOrderItemList.flowPanel.stationEmpty')
}

function priceLabel(_key: string) {
  return t('salesOrderItemList.flowPanel.fields.unitPrice')
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

.so-item-flow-root__hint {
  margin: 0 0 10px;
  padding: 0;
  font-size: 12px;
  line-height: 1.5;
  color: var(--el-color-warning);
}
</style>
