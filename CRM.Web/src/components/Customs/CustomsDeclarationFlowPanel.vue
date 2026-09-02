<template>
  <div class="so-item-flow-root so-item-flow-root--embedded" aria-label="customs-declaration-flow-panel">
    <div v-if="!row" class="so-item-flow-root__empty">
      {{ t('customsPages.declarations.flowPanel.pickRow') }}
    </div>

    <div v-else v-loading="loading" class="so-item-flow-root__content">
      <p v-if="loadError" class="so-item-flow-root__error">{{ loadError }}</p>

      <ol class="so-item-flow-timeline">
        <li
          v-for="station in stations"
          :key="station.key"
          class="so-item-flow-station"
          :class="{ 'is-main': station.key === 'customsDeclaration' }"
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
                <FlowYouAreHereMark v-if="station.key === 'customsDeclaration'" />
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
                  <div v-if="showsOutType(station.key) && station.key === 'stockOut'" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('packingDetail.flowPanel.fields.stockOutType') }}：
                    </span>
                    <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                      <StockBizTypeTag
                        v-if="card.stockOutType != null"
                        biz="out"
                        :type="card.stockOutType"
                        :customs-declaration-id="card.customsDeclarationId"
                        :customs-declaration-code="card.customsDeclarationCode"
                      />
                      <template v-else>—</template>
                    </span>
                  </div>
                  <div v-if="station.key === 'customsStockIn'" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('customsPages.declarations.flowPanel.fields.stockInType') }}：
                    </span>
                    <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                      <StockBizTypeTag
                        v-if="card.stockInType != null"
                        biz="in"
                        :type="card.stockInType"
                        :customs-declaration-id="card.customsDeclarationId"
                        :customs-declaration-code="card.customsDeclarationCode"
                      />
                      <template v-else>—</template>
                    </span>
                  </div>
                  <div class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.createdAt') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ formatFlowCardDate(card.createdAt) }}</span>
                  </div>
                  <div
                    v-if="station.key === 'customsStockOutNotify' || station.key === 'packing'"
                    class="so-item-flow-kv__cell"
                  >
                    <span class="so-item-flow-kv__label">
                      {{ t('packingDetail.flowPanel.fields.stockOutType') }}：
                    </span>
                    <span class="so-item-flow-kv__value so-item-flow-kv__value--with-icon">
                      <StockBizTypeTag
                        v-if="card.stockOutType != null"
                        biz="out"
                        :type="card.stockOutType"
                        :customs-declaration-id="card.customsDeclarationId"
                        :customs-declaration-code="card.customsDeclarationCode"
                      />
                      <template v-else>—</template>
                    </span>
                  </div>
                  <div v-else class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">{{ t(card.personRoleKey) }}：</span>
                    <span class="so-item-flow-kv__value">{{ card.personName || '—' }}</span>
                  </div>
                  <template v-if="card.showCustomer">
                    <div
                      v-if="station.key === 'sellOrderItem'"
                      class="so-item-flow-kv__cell so-item-flow-kv__cell--full"
                    >
                      <span class="so-item-flow-kv__label">
                        {{ t('salesOrderItemList.flowPanel.fields.customerName') }}：
                      </span>
                      <FlowPartyLink
                        :text="formatFlowCustomerNameWithCode(card.customerName, card.customerCode)"
                        :to="customerTo(card.customerId, maskSensitive)"
                      />
                    </div>
                    <template v-else>
                      <div class="so-item-flow-kv__cell">
                        <span class="so-item-flow-kv__label">
                          {{ t('salesOrderItemList.flowPanel.fields.customerName') }}：
                        </span>
                        <FlowPartyLink
                          :text="card.customerName || '—'"
                          :to="customerTo(card.customerId, maskSensitive)"
                        />
                      </div>
                      <div class="so-item-flow-kv__cell">
                        <span class="so-item-flow-kv__label">
                          {{ t('salesOrderItemList.flowPanel.fields.customerCode') }}：
                        </span>
                        <span class="so-item-flow-kv__value">{{ card.customerCode || '—' }}</span>
                      </div>
                    </template>
                  </template>
                  <div
                    v-if="station.key === 'customsDeclaration' && card.brokerName"
                    class="so-item-flow-kv__cell so-item-flow-kv__cell--full"
                  >
                    <span class="so-item-flow-kv__label">
                      {{ t('customsPages.declarations.colBroker') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ card.brokerName }}</span>
                  </div>
                  <div v-if="card.unitPriceText" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t('salesOrderItemList.flowPanel.fields.unitPrice') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ card.unitPriceText }}</span>
                  </div>
                  <div v-if="card.qtyText" class="so-item-flow-kv__cell">
                    <span class="so-item-flow-kv__label">
                      {{ t(card.qtyLabelKey || 'salesOrderItemList.flowPanel.fields.qty') }}：
                    </span>
                    <span class="so-item-flow-kv__value">{{ card.qtyText }}</span>
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
import type { CustomsDeclarationFlowAggregatesDto } from '@/api/customs'
import FlowPartyLink from '@/components/Common/FlowPartyLink.vue'
import FlowYouAreHereMark from '@/components/Common/FlowYouAreHereMark.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { useFlowPartyLinks } from '@/composables/useFlowPartyLinks'
import { buildCustomsDeclarationFlowStations } from '@/utils/customsDeclarationFlowPanel'
import {
  formatFlowCardDate,
  formatFlowCustomerNameWithCode,
  type FlowDocRoute,
  type FlowStationKey,
  type FlowStationStatus
} from '@/utils/sellOrderItemFlowPanel'

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: CustomsDeclarationFlowAggregatesDto | null
  loading?: boolean
  loadError?: string
  maskSensitive?: boolean
}>()

const { t } = useI18n()
const { customerTo } = useFlowPartyLinks()

const stations = computed(() =>
  buildCustomsDeclarationFlowStations(props.aggregates, t as (key: string, ...args: unknown[]) => string, {
    maskSensitive: props.maskSensitive
  })
)

function showsOutType(key: FlowStationKey) {
  return key === 'customsStockOutNotify' || key === 'packing' || key === 'stockOut'
}

function stationStatusLabel(status: FlowStationStatus) {
  if (status === 'active') return t('salesOrderItemList.flowPanel.stationActive')
  if (status === 'done') return t('salesOrderItemList.flowPanel.stationDone')
  return t('salesOrderItemList.flowPanel.stationEmpty')
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
