<template>
  <div
    class="so-item-flow-root so-item-flow-root--embedded"
    aria-label="stock-item-flow-panel"
  >
    <div v-if="!row" class="so-item-flow-root__empty">
      {{ t('inventoryStockItemList.flowPanel.pickRow') }}
    </div>

    <div v-else v-loading="loading" class="so-item-flow-root__content">
      <p v-if="loadError" class="so-item-flow-root__error">{{ loadError }}</p>

      <ol class="so-item-flow-timeline">
        <li
          v-for="station in stations"
          :key="station.key"
          class="so-item-flow-station"
          :class="{ 'is-main': station.key === 'stockItem' }"
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
                <FlowYouAreHereMark v-if="station.key === 'stockItem'" />
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
              {{ t('inventoryStockItemList.flowPanel.emptyStation') }}
            </div>

            <div v-else class="so-item-flow-cards">
              <article v-for="card in station.cards" :key="card.id" class="so-item-flow-card">
                <div class="so-item-flow-kv">
                  <template v-if="station.key === 'stockIn'">
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.docNo') }}：</span>
                      <span class="so-item-flow-kv__value">
                        <router-link
                          v-if="card.docRoute && !maskPurchase"
                          class="link-text"
                          :to="toRouteLocation(card.docRoute)"
                        >
                          {{ card.docNo }}
                        </router-link>
                        <template v-else>{{ card.docNo }}</template>
                      </span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.stockInType') }}：</span>
                      <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                        <StockBizTypeTag
                          biz="in"
                          :type="card.stockInType"
                          :customs-declaration-id="card.customsDeclarationId"
                          :customs-declaration-code="card.customsDeclarationCode"
                        />
                      </span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.status') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.statusText || '—' }}</span>
                    </div>
                  </template>
                  <template v-else>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.docNo') }}：</span>
                      <span class="so-item-flow-kv__value">
                        <router-link
                          v-if="card.docRoute"
                          class="link-text"
                          :to="toRouteLocation(card.docRoute)"
                        >
                          {{ card.docNo }}
                        </router-link>
                        <template v-else>{{ card.docNo }}</template>
                      </span>
                    </div>
                    <div class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.status') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.statusText || '—' }}</span>
                    </div>
                  </template>
                  <div
                    class="so-item-flow-kv__cell"
                    :class="{ 'so-item-flow-kv__cell--full': station.key === 'stockItem' }"
                  >
                    <span class="so-item-flow-kv__label">{{ t(card.createdAtLabelKey) }}：</span>
                    <span class="so-item-flow-kv__value">{{ formatStockItemFlowCardDate(card.createdAt) }}</span>
                  </div>
                  <div
                    v-if="station.key === 'stockItem'"
                    class="so-item-flow-kv__cell so-item-flow-kv__cell--full"
                  >
                    <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.stockInType') }}：</span>
                    <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                      <StockBizTypeTag
                        biz="in"
                        :type="card.stockInType"
                        :customs-declaration-id="card.customsDeclarationId"
                        :customs-declaration-code="card.customsDeclarationCode"
                      />
                    </span>
                  </div>
                  <div v-else-if="card.bizTypeLabelKey" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t(card.bizTypeLabelKey) }}：</span>
                    <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                      <StockBizTypeTag
                        biz="out"
                        :type="card.stockOutType"
                        :customs-declaration-id="card.customsDeclarationId"
                        :customs-declaration-code="card.customsDeclarationCode"
                      />
                    </span>
                  </div>
                  <div v-if="!card.bizTypeLabelKey && card.showPerson" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t(card.personRoleKey) }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.personName || '—' }}</span>
                  </div>
                  <template v-if="station.key === 'stockItem'">
                    <div v-if="card.showVendor" class="so-item-flow-kv__cell so-item-flow-kv__cell--full">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.vendorName') }}：</span>
                      <FlowPartyLink
                        :text="card.vendorName || '—'"
                        :to="vendorTo(card.vendorId, maskPurchase)"
                      />
                    </div>
                    <div v-if="card.unitPriceText" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.unitPrice') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.unitPriceText }}</span>
                    </div>
                    <div v-if="card.qtyText" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t(card.qtyLabelKey) }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.qtyText }}</span>
                    </div>
                    <div class="so-item-flow-kv__cell so-item-flow-kv__cell--full">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.stockOutType') }}：</span>
                      <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                        <StockBizTypeTag
                          v-if="card.stockOutType != null"
                          biz="out"
                          :type="card.stockOutType"
                          :customs-declaration-id="card.stockOutCustomsDeclarationId"
                          :customs-declaration-code="card.stockOutCustomsDeclarationCode"
                        />
                        <template v-else>—</template>
                      </span>
                    </div>
                    <div v-if="card.showCustomer" class="so-item-flow-kv__cell so-item-flow-kv__cell--full">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.customerName') }}：</span>
                      <FlowPartyLink
                        :text="card.customerName || '—'"
                        :to="customerTo(card.customerId, maskSale)"
                      />
                    </div>
                    <div v-if="card.salesPriceText" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.salesPrice') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.salesPriceText }}</span>
                    </div>
                    <div v-if="card.qty2Text" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t(card.qty2LabelKey || card.qtyLabelKey) }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.qty2Text }}</span>
                    </div>
                  </template>
                  <template v-else>
                    <div v-if="card.showVendor" class="so-item-flow-kv__cell so-item-flow-kv__cell--full">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.vendorName') }}：</span>
                      <FlowPartyLink
                        :text="card.vendorName || '—'"
                        :to="vendorTo(card.vendorId, maskPurchase)"
                      />
                    </div>
                    <div v-if="card.showCustomer" class="so-item-flow-kv__cell so-item-flow-kv__cell--full">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.customerName') }}：</span>
                      <FlowPartyLink
                        :text="card.customerName || '—'"
                        :to="customerTo(card.customerId, maskSale)"
                      />
                    </div>
                    <div v-if="card.unitPriceText" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.unitPrice') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.unitPriceText }}</span>
                    </div>
                    <div v-if="card.salesPriceText" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t('inventoryStockItemList.flowPanel.fields.salesPrice') }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.salesPriceText }}</span>
                    </div>
                    <div v-if="card.qtyText" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t(card.qtyLabelKey) }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.qtyText }}</span>
                    </div>
                    <div v-if="card.qty2Text" class="so-item-flow-kv__cell">
                      <span class="so-item-flow-kv__label">{{ t(card.qty2LabelKey || card.qtyLabelKey) }}：</span>
                      <span class="so-item-flow-kv__value">{{ card.qty2Text }}</span>
                    </div>
                  </template>
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
import type { StockItemFlowAggregates } from '@/api/inventoryCenter'
import FlowPartyLink from '@/components/Common/FlowPartyLink.vue'
import FlowYouAreHereMark from '@/components/Common/FlowYouAreHereMark.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { useFlowPartyLinks } from '@/composables/useFlowPartyLinks'
import {
  buildStockItemFlowStations,
  formatStockItemFlowCardDate,
  type FlowDocRoute,
  type FlowStationStatus
} from '@/utils/stockItemFlowPanel'

const props = withDefaults(
  defineProps<{
    row: Record<string, unknown> | null
    aggregates?: StockItemFlowAggregates | null
    loading?: boolean
    loadError?: string
    maskPurchase?: boolean
    maskSale?: boolean
  }>(),
  {
    aggregates: null,
    loading: false,
    loadError: '',
    maskPurchase: false,
    maskSale: false
  }
)

const { t } = useI18n()
const { customerTo, vendorTo } = useFlowPartyLinks()

const stations = computed(() =>
  buildStockItemFlowStations(props.row, props.aggregates, t as (key: string, ...args: unknown[]) => string, {
    maskPurchase: props.maskPurchase,
    maskSale: props.maskSale
  })
)

function stationStatusLabel(status: FlowStationStatus) {
  if (status === 'done') return t('inventoryStockItemList.flowPanel.stationDone')
  if (status === 'active') return t('inventoryStockItemList.flowPanel.stationActive')
  return t('inventoryStockItemList.flowPanel.stationEmpty')
}

function toRouteLocation(route: FlowDocRoute) {
  return {
    name: route.name,
    params: route.params,
    query: route.query
  }
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-flow-panel.scss';
</style>
