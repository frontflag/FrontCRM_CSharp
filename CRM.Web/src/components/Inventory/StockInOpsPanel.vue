<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="stock-in-ops-panel"
  >
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('stockInList.opsPanel.pickRow') }}
    </div>

    <div
      v-else
      v-loading="loading"
      class="so-item-ops-root__content"
      :class="embedded ? 'so-item-ops-root__content--embedded' : 'so-item-ops-panel__body'"
    >
      <p v-if="loadError" class="so-item-ops-root__error">{{ loadError }}</p>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockInList.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">
            <router-link v-if="stockInLink" :to="stockInLink" class="link-text">{{ stockInCode }}</router-link>
            <span v-else>{{ stockInCode }}</span>
          </div>
          <div class="ops-overview-line">
            <VendorNameReadonlyText
              :name-zh="vendorNameZh"
              :name-en="vendorNameEn"
              :masked="maskSensitive"
            />
          </div>
          <div class="ops-overview-line">{{ displayModel }}</div>
          <div class="ops-overview-line">{{ displayBrand }}</div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockInList.opsPanel.stockInDate') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ stockInDateText }}</span>
            </div>
          </div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseUnitPrice') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ overviewUnitPriceText }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockInList.opsPanel.overviewQty') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ formatQty(totalQuantity) }} pcs</span>
            </div>
          </div>
          <div class="ops-overview-line ops-overview-line--type">
            <StockBizTypeTag
              biz="in"
              :type="stockInType"
              :customs-declaration-id="customsDeclarationId"
              :customs-declaration-code="customsDeclarationCode"
            />
            <span v-if="customsBrokerNameText" class="ops-overview-broker">{{ customsBrokerNameText }}</span>
          </div>
        </div>
      </section>

      <section class="ops-card ops-card--status-only">
        <div class="ops-card__body ops-card__body--status">
          <div class="ops-status-tags">
            <div class="ops-status-tags__row">
              <el-tag effect="dark" :type="statusTagType" size="small">{{ statusLabel }}</el-tag>
            </div>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockInList.opsPanel.remarkTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <p class="ops-overview-line ops-remark-text">{{ remarkText }}</p>
          <button
            v-if="canWriteLogistics"
            type="button"
            class="ops-action-btn ops-action-btn--primary"
            :disabled="actionLoading"
            @click="emit('edit-remark')"
          >
            {{ t('stockInList.actions.editRemark') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockInList.opsPanel.purchaseTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="purchase">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseItemCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link v-if="purchaseLink && !maskSensitive" :to="purchaseLink" class="link-text">
                    {{ purchaseItemCode }}
                  </router-link>
                  <span v-else>{{ purchaseItemCode }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseOrderType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <span class="ops-po-type-with-icon">
                    <span>{{ purchaseOrderTypeText }}</span>
                    <el-tooltip
                      v-if="isStockingPurchaseOrder"
                      :content="t('purchaseOrderItemList.filters.stockingTag')"
                      placement="top"
                      :hide-after="0"
                    >
                      <span
                        class="ops-po-type-stocking-hit"
                        role="img"
                        :aria-label="t('purchaseOrderItemList.filters.stockingTag')"
                      >
                        <el-icon class="ops-po-type-stocking-icon" aria-hidden="true">
                          <Box />
                        </el-icon>
                      </span>
                    </el-tooltip>
                  </span>
                </span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ purchaseOrderCreateDateText }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseUser') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ purchaseUserName }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(purchaseQty) }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.purchaseUnitPrice') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ purchaseUnitPriceText }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('stockInList.opsPanel.noPurchase') }}</p>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockInList.opsPanel.arrivalTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="arrivalNotice">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.arrivalNoticeCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link v-if="arrivalNoticeLink" :to="arrivalNoticeLink" class="link-text">
                    {{ arrivalNoticeCode }}
                  </router-link>
                  <span v-else>{{ arrivalNoticeCode }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.arrivalType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <StockBizTypeTag biz="in" :type="arrivalStockInType" />
                </span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.actualArrivalDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ actualArrivalDateText }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.receiveQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(receiveQty) }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('stockInList.opsPanel.qcPassQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ qcPassQtyText }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('stockInList.opsPanel.noArrival') }}</p>
        </div>
      </section>
    </div>
  </component>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Box } from '@element-plus/icons-vue'
import type { StockInOpsAggregatesDto } from '@/api/stockIn'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import {
  resolveStockInCustomsBrokerName,
  resolveStockInOverviewUnitPrice,
  resolveStockInPurchaseOrderTypeKey
} from '@/utils/stockInOpsOverview'

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: StockInOpsAggregatesDto | null
  loading?: boolean
  loadError?: string
  actionLoading?: boolean
  canWriteLogistics?: boolean
  maskSensitive?: boolean
  embedded?: boolean
}>()

const emit = defineEmits<{
  'edit-remark': []
}>()

const { t } = useI18n()

const stockInCode = computed(() => String(props.row?.stockInCode ?? props.row?.StockInCode ?? '—') || '—')
const stockInId = computed(() => String(props.row?.id ?? props.row?.Id ?? '').trim())
const stockInLink = computed(() => {
  const id = stockInId.value
  if (!id) return null
  return { name: 'StockInDetail', params: { id } }
})

const vendorNameZh = computed(() => String(props.row?.vendorName ?? props.row?.VendorName ?? '').trim())
const vendorNameEn = computed(() =>
  String(props.row?.vendorEnglishName ?? props.row?.VendorEnglishName ?? '').trim()
)
const displayModel = computed(() => {
  const v = props.row?.materialModelSummary ?? props.row?.MaterialModelSummary
  return v != null && String(v).trim() ? String(v).trim() : '—'
})
const displayBrand = computed(() => {
  const v = props.row?.materialBrandSummary ?? props.row?.MaterialBrandSummary
  return v != null && String(v).trim() ? String(v).trim() : '—'
})
const totalQuantity = computed(() => Number(props.row?.totalQuantity ?? props.row?.TotalQuantity ?? 0))
const stockInType = computed(() => {
  const raw = props.row?.stockInType ?? props.row?.StockInType
  if (raw === null || raw === undefined || raw === '') return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
})
const customsDeclarationId = computed(() =>
  (props.row?.customsDeclarationId ?? props.row?.CustomsDeclarationId) as string | null | undefined
)
const customsDeclarationCode = computed(() =>
  (props.row?.customsDeclarationCode ?? props.row?.CustomsDeclarationCode) as string | null | undefined
)
const customsBrokerNameText = computed(() =>
  resolveStockInCustomsBrokerName(props.row, stockInType.value)
)
const stockInDateText = computed(() => {
  const raw = props.row?.stockInDate ?? props.row?.StockInDate
  return raw ? formatDisplayDate(String(raw)) : '—'
})

const purchase = computed(() => props.aggregates?.purchase ?? null)
const overviewUnitPriceText = computed(() =>
  resolveStockInOverviewUnitPrice({
    maskSensitive: props.maskSensitive,
    aggregateUnitPrice: purchase.value?.unitPrice,
    aggregateCurrency: purchase.value?.currency,
    listSummary: (props.row?.unitPriceSummary ?? props.row?.UnitPriceSummary) as string | null | undefined,
    listCurrency: (props.row?.unitPriceCurrencyCode ??
      props.row?.UnitPriceCurrencyCode ??
      props.row?.currencyCode ??
      props.row?.CurrencyCode) as number | null | undefined
  })
)

const noticeStatus = computed(() => Number(props.row?.status ?? props.row?.Status ?? 0))
const statusLabel = computed(() => {
  const keyMap: Record<number, 'draft' | 'pending' | 'done' | 'cancelled'> = {
    0: 'draft',
    1: 'pending',
    2: 'done',
    3: 'cancelled'
  }
  const k = keyMap[noticeStatus.value]
  return k ? t(`stockInList.status.${k}`) : t('rfqDetail.unknown')
})
const statusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = noticeStatus.value
  if (s === 2) return 'success'
  if (s === 1) return 'warning'
  if (s === 3) return 'danger'
  return 'info'
})

const remarkText = computed(() => {
  const v = String(props.row?.remark ?? props.row?.Remark ?? '').trim()
  return v || '—'
})

const purchaseItemCode = computed(() => {
  if (props.maskSensitive) return '—'
  const code = purchase.value?.purchaseOrderItemCode?.trim()
  if (code) return code
  const fallback = purchase.value?.purchaseOrderItemId?.trim()
  return fallback || '—'
})
const purchaseUserName = computed(() => {
  if (props.maskSensitive) return '—'
  return purchase.value?.purchaseUserName?.trim() || '—'
})
const purchaseQty = computed(() => Number(purchase.value?.qty ?? 0))
const purchaseOrderCreateDateText = computed(() => {
  const raw = purchase.value?.purchaseOrderCreateTime
  return raw ? formatDisplayDate(String(raw)) : '—'
})
const purchaseOrderTypeKey = computed(() =>
  resolveStockInPurchaseOrderTypeKey(purchase.value?.purchaseOrderType)
)
const isStockingPurchaseOrder = computed(() => purchaseOrderTypeKey.value === 'stocking')
const purchaseOrderTypeText = computed(() => {
  const key = purchaseOrderTypeKey.value
  if (key === 'unknown') return '—'
  return t(`stockInList.opsPanel.orderTypes.${key}`)
})
const purchaseUnitPriceText = computed(() => {
  if (props.maskSensitive) return '—'
  const price = purchase.value?.unitPrice
  if (price == null || !Number.isFinite(Number(price)) || Number(price) === 0) return '—'
  return formatUnitPriceWithCurrencyCodeSuffix(price, purchase.value?.currency)
})
const purchaseLink = computed(() => {
  const purchaseOrderId = purchase.value?.purchaseOrderId?.trim()
  const purchaseOrderItemId = purchase.value?.purchaseOrderItemId?.trim()
  if (!purchaseOrderId || !purchaseOrderItemId) return null
  return {
    name: 'PurchaseOrderDetail',
    params: { id: purchaseOrderId },
    query: { purchaseOrderItemId }
  }
})

const arrivalNotice = computed(() => props.aggregates?.arrivalNotice ?? null)
const arrivalNoticeCode = computed(() => arrivalNotice.value?.noticeCode?.trim() || '—')
const arrivalStockInType = computed(() => arrivalNotice.value?.stockInType ?? null)
const actualArrivalDateText = computed(() => {
  const raw = arrivalNotice.value?.actualArrivalDate
  return raw ? formatDisplayDate(String(raw)) : '—'
})
const receiveQty = computed(() => Number(arrivalNotice.value?.receiveQty ?? 0))
const qcPassQtyText = computed(() => {
  const v = arrivalNotice.value?.passQty
  if (v == null || !Number.isFinite(Number(v))) return '—'
  return formatQty(v)
})
const arrivalNoticeLink = computed(() => {
  const id = arrivalNotice.value?.id?.trim()
  if (!id) return null
  return { name: 'ArrivalNoticeList', query: { noticeId: id } }
})

function formatQty(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';

.ops-overview-line--type {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.ops-overview-broker {
  min-width: 0;
  font-size: 13px;
  font-weight: 500;
  line-height: 1.5;
}

.ops-remark-text {
  white-space: pre-wrap;
  margin: 0 0 10px;
}

.ops-po-type-with-icon {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.ops-po-type-stocking-hit {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  cursor: default;
  line-height: 1;
}

.ops-po-type-stocking-icon {
  font-size: 16px;
  color: #e6a23c;
}

html[data-theme='dark'] .ops-po-type-stocking-icon {
  color: #ebb563;
}
</style>
