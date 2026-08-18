<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="stock-out-ops-panel"
  >
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('stockOutList.opsPanel.pickRow') }}
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
          <h3 class="ops-card__title">{{ t('stockOutList.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">
            <router-link v-if="stockOutId && stockOutCode !== '—'" :to="detailLink" class="link-text">
              {{ stockOutCode }}
            </router-link>
            <span v-else>{{ stockOutCode }}</span>
          </div>
          <div class="ops-overview-line">
            <el-tag effect="dark" :type="statusTagType" size="small">{{ statusLabel }}</el-tag>
            <StockBizTypeTag
              biz="out"
              :type="stockOutType"
              :customs-declaration-id="customsDeclarationId"
              :customs-declaration-code="customsDeclarationCode"
            />
          </div>
          <div class="ops-overview-line">
            <CustomerNameReadonlyText
              :name-zh="customerNameZh"
              :name-en="customerNameEn"
              :masked="maskSensitive"
            />
          </div>
          <div class="ops-overview-line">{{ salesUserText }}</div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockOutList.columns.totalQuantity') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value ops-kv__value--accent">{{ formatQty(totalQuantity) }}</span>
            </div>
          </div>
          <div v-if="customsDeclarationLink" class="ops-overview-line">
            <router-link :to="customsDeclarationLink" class="link-text">
              {{ customsDeclarationCode || '—' }}
            </router-link>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockOutList.opsPanel.shipmentTitle') }}</h3>
          <el-button
            v-if="canWriteLogistics"
            class="ops-card__head-btn"
            link
            type="primary"
            size="small"
            :disabled="actionLoading"
            @click="emit('edit-header')"
          >
            {{ t('stockOutList.actions.edit') }}
          </el-button>
        </header>
        <div class="ops-card__body">
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockOutList.columns.stockOutDate') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ stockOutDateText }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockOutList.columns.shipmentMethod') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ shipmentMethodText }}</span>
            </div>
          </div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockOutList.columns.expressCompany') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ expressCompanyText }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('stockOutList.columns.courierTrackingNo') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ courierTrackingNoText }}</span>
            </div>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockOutList.opsPanel.markFinishTitle') }}</h3>
          <span v-if="isFinished" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('stockOutList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <button
            v-if="canWriteLogistics && !isFinished"
            type="button"
            class="ops-action-btn ops-action-btn--primary"
            :disabled="actionLoading"
            @click="emit('mark-finish')"
          >
            {{ t('stockOutList.actions.markFinished') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockOutList.opsPanel.itemsTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <p v-if="!itemRows.length" class="ops-status ops-status--info">
            {{ t('stockOutList.opsPanel.noItems') }}
          </p>
          <template v-else>
            <div
              v-for="(item, idx) in visibleItems"
              :key="item.stockOutItemId || idx"
              class="ops-stocking-usage-entry"
              :class="{ 'ops-stocking-usage-entry--divider': idx > 0 }"
            >
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.stockOutItemCode') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ dash(item.stockOutItemCode) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.packingCode') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">
                    <router-link
                      v-if="item.packingId && item.packingCode"
                      :to="{ name: 'PackingDetail', params: { id: item.packingId } }"
                      class="link-text"
                    >
                      {{ item.packingCode }}
                    </router-link>
                    <span v-else>{{ dash(item.packingCode) }}</span>
                  </span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('common.freightForwarderOrderNo') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ dash(item.freightForwarderOrderNo) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.purchasePn') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ dash(item.purchasePn) }}</span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.purchaseBrand') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ dash(item.purchaseBrand) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.outQuantity') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value ops-kv__value--accent">{{ formatQty(item.outQuantity) }}</span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutDetail.itemColumns.outAmount') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">
                    <template v-if="showAmount">
                      <template v-if="lineAmount(item) != null">
                        <span>{{ formatTotalAmountNumber(lineAmount(item)) }}</span>
                        <span
                          v-if="formatTotalAmountNumber(lineAmount(item)) !== '—'"
                          :class="['dock-tier-ccy', listAmountCurrencyDockClass(item.salesCurrency)]"
                        >
                          {{ listAmountCurrencyIso(item.salesCurrency) }}
                        </span>
                      </template>
                      <template v-else>—</template>
                    </template>
                    <template v-else>—</template>
                  </span>
                </div>
              </div>
            </div>
            <p v-if="itemsMoreCount > 0" class="ops-status ops-status--info">
              {{ t('stockOutList.opsPanel.moreCount', { n: itemsMoreCount }) }}
              <router-link :to="detailLink" class="link-text">{{ t('stockOutList.opsPanel.openDetail') }}</router-link>
            </p>
          </template>
        </div>
      </section>

      <section v-if="isSalesStockOut" class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockOutList.opsPanel.sellItemsTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <p v-if="!sellRows.length" class="ops-status ops-status--info">
            {{ t('stockOutList.opsPanel.noSellItems') }}
          </p>
          <template v-else>
            <div
              v-for="(sell, idx) in visibleSellRows"
              :key="sell.sellOrderItemId || idx"
              class="ops-stocking-usage-entry"
              :class="{ 'ops-stocking-usage-entry--divider': idx > 0 }"
            >
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.sellOrderItemCode') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">
                    <router-link
                      v-if="sellOrderLink(sell) && !maskSensitive"
                      :to="sellOrderLink(sell)!"
                      class="link-text"
                    >
                      {{ dash(sell.sellOrderItemCode) }}
                    </router-link>
                    <span v-else>{{ dash(sell.sellOrderItemCode) }}</span>
                  </span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutList.columns.salesUserName') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ maskSensitive ? '—' : dash(sell.salesUserName) }}</span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.purchasePn') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ dash(sell.pn) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutItemList.columns.purchaseBrand') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ dash(sell.brand) }}</span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('stockOutList.opsPanel.qty') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value ops-kv__value--accent">{{ formatQty(sell.qty) }}</span>
                </div>
              </div>
            </div>
            <p v-if="sellMoreCount > 0" class="ops-status ops-status--info">
              {{ t('stockOutList.opsPanel.moreCount', { n: sellMoreCount }) }}
              <router-link :to="detailLink" class="link-text">{{ t('stockOutList.opsPanel.openDetail') }}</router-link>
            </p>
          </template>
        </div>
      </section>

      <section v-if="isSalesStockOut" class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('stockOutList.opsPanel.receivablesTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <p v-if="!receivableRows.length" class="ops-status ops-status--info">{{ receivableEmptyText }}</p>
          <template v-else>
            <div
              v-for="(rec, idx) in visibleReceivables"
              :key="rec.id || idx"
              class="ops-stocking-usage-entry"
              :class="{ 'ops-stocking-usage-entry--divider': idx > 0 }"
            >
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.code') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">
                    <router-link
                      v-if="canOpenReceivable && rec.id && rec.receivableCode"
                      :to="`/finance/receivables/${rec.id}`"
                      class="link-text"
                    >
                      {{ rec.receivableCode }}
                    </router-link>
                    <span v-else>{{ dash(rec.receivableCode) }}</span>
                  </span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.qty') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ formatQty(rec.outboundQty) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.amount') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ amountText(rec.amount, rec.currency) }}</span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.verificationStatus') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">
                    <el-tag :type="receivableVerificationTagType(rec.verificationStatus)" size="small">
                      {{ receivableVerificationLabel(rec.verificationStatus) }}
                    </el-tag>
                  </span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.verifiedDone') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ amountText(rec.verifiedDone, rec.currency) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.verifiedToBe') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ amountText(rec.verifiedToBe, rec.currency) }}</span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.invoiceMatchStatus') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">
                    <el-tag :type="receivableVerificationTagType(rec.invoiceMatchStatus)" size="small">
                      {{ receivableVerificationLabel(rec.invoiceMatchStatus) }}
                    </el-tag>
                  </span>
                </div>
              </div>
              <div class="ops-stock-region-row">
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.invoiceMatchDone') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ amountText(rec.invoiceMatchDone, rec.currency) }}</span>
                </div>
                <div class="ops-stock-region-cell">
                  <span class="ops-kv__label">{{ t('financeReceivableList.columns.invoiceMatchToBe') }}</span>
                  <span class="ops-kv__sep" aria-hidden="true">：</span>
                  <span class="ops-kv__value">{{ amountText(rec.invoiceMatchToBe, rec.currency) }}</span>
                </div>
              </div>
            </div>
            <p v-if="receivableMoreCount > 0" class="ops-status ops-status--info">
              {{ t('stockOutList.opsPanel.moreCount', { n: receivableMoreCount }) }}
              <router-link :to="detailLink" class="link-text">{{ t('stockOutList.opsPanel.openDetail') }}</router-link>
            </p>
          </template>
        </div>
      </section>
    </div>
  </component>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck } from '@element-plus/icons-vue'
import type {
  StockOutItemListRow,
  StockOutOpsAggregatesDto,
  StockOutOpsSellOrderItemRow
} from '@/api/stockOut'
import { formatDisplayDate } from '@/utils/displayDateTime'
import {
  formatTotalAmountNumber,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  unitPriceDockHasValue
} from '@/utils/moneyFormat'
import { useAuthStore } from '@/stores/auth'
import { StockOutTypeCode } from '@/constants/stockOutType'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import CustomerNameReadonlyText from '@/components/Customer/CustomerNameReadonlyText.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'

const OPS_GROUP_LIMIT = 8

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: StockOutOpsAggregatesDto | null
  loading?: boolean
  loadError?: string
  actionLoading?: boolean
  canWriteLogistics?: boolean
  maskSensitive?: boolean
  embedded?: boolean
}>()

const emit = defineEmits<{
  'edit-header': []
  'mark-finish': []
}>()

const { t } = useI18n()
const authStore = useAuthStore()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()

onMounted(() => {
  void ensureLogisticsDict()
})

const showAmount = computed(
  () => authStore.hasPermission('sales.amount.read') && !props.maskSensitive
)
const canOpenReceivable = computed(() => authStore.hasPermission('finance-receipt.read'))

function rowStr(...keys: string[]): string {
  if (!props.row) return ''
  for (const k of keys) {
    const v = props.row[k]
    if (v != null && String(v).trim()) return String(v).trim()
  }
  return ''
}

const stockOutId = computed(() => rowStr('id', 'Id'))
const stockOutCode = computed(() => rowStr('stockOutCode', 'StockOutCode') || '—')
const stockOutType = computed(() => Number(props.row?.stockOutType ?? props.row?.StockOutType ?? 0))
const isSalesStockOut = computed(
  () => stockOutType.value === StockOutTypeCode.Sales || stockOutType.value === 1
)
const isFinished = computed(() => Number(props.row?.status ?? props.row?.Status ?? 0) === 4)
const totalQuantity = computed(() => Number(props.row?.totalQuantity ?? props.row?.TotalQuantity ?? 0))
const customerNameZh = computed(() => rowStr('customerName', 'CustomerName'))
const customerNameEn = computed(() => rowStr('customerEnglishName', 'CustomerEnglishName'))
const salesUserText = computed(() => {
  if (props.maskSensitive) return '—'
  const fromRow = rowStr('salesUserName', 'SalesUserName')
  if (fromRow) return fromRow
  const fromSell = (props.aggregates?.sellOrderItems ?? [])
    .map((x) => String(x.salesUserName ?? '').trim())
    .find((x) => x.length > 0)
  return fromSell || '—'
})
const customsDeclarationId = computed(() => rowStr('customsDeclarationId', 'CustomsDeclarationId'))
const customsDeclarationCode = computed(() => rowStr('customsDeclarationCode', 'CustomsDeclarationCode'))
const customsDeclarationLink = computed(() => {
  if (stockOutType.value !== StockOutTypeCode.Customs) return null
  const id = customsDeclarationId.value
  if (!id) return null
  return { name: 'CustomsDeclarationDetail', params: { id } }
})

const detailLink = computed(() => ({ name: 'StockOutDetail', params: { id: stockOutId.value } }))

const statusLabel = computed(() => {
  const s = Number(props.row?.status ?? props.row?.Status ?? 0)
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return t('rfqDetail.unknown')
  }
})

const statusTagType = computed((): 'success' | 'warning' | 'info' | 'danger' | 'primary' => {
  const s = Number(props.row?.status ?? props.row?.Status ?? 0)
  if (s === 0) return 'info'
  if (s === 1) return 'warning'
  if (s === 2) return 'success'
  if (s === 3) return 'danger'
  if (s === 4) return 'primary'
  return 'info'
})

function dictLabel(options: { label: string; value: string }[], code: string | null | undefined): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  return options.find((o) => String(o.value).trim() === c)?.label ?? c
}

const stockOutDateText = computed(() => {
  const raw = props.row?.stockOutDate ?? props.row?.StockOutDate
  if (!raw) return '—'
  const s = formatDisplayDate(String(raw))
  return s === '--' ? '—' : s
})

const shipmentMethodText = computed(() =>
  dictLabel(
    shipmentArrivalOptions.value,
    (props.row?.shipmentMethod ?? props.row?.ShipmentMethod) as string | null | undefined
  )
)
const expressCompanyText = computed(() =>
  dictLabel(
    expressOptions.value,
    (props.row?.expressCompany ?? props.row?.ExpressCompany) as string | null | undefined
  )
)
const courierTrackingNoText = computed(() => rowStr('courierTrackingNo', 'CourierTrackingNo') || '—')

const itemRows = computed(() => props.aggregates?.items ?? [])
const sellRows = computed(() => props.aggregates?.sellOrderItems ?? [])
const receivableRows = computed(() => props.aggregates?.receivables ?? [])

const visibleItems = computed(() => itemRows.value.slice(0, OPS_GROUP_LIMIT))
const visibleSellRows = computed(() => sellRows.value.slice(0, OPS_GROUP_LIMIT))
const visibleReceivables = computed(() => receivableRows.value.slice(0, OPS_GROUP_LIMIT))

const itemsMoreCount = computed(() => Math.max(0, itemRows.value.length - OPS_GROUP_LIMIT))
const sellMoreCount = computed(() => Math.max(0, sellRows.value.length - OPS_GROUP_LIMIT))
const receivableMoreCount = computed(() => Math.max(0, receivableRows.value.length - OPS_GROUP_LIMIT))

const receivableEmptyText = computed(() =>
  isFinished.value
    ? t('stockOutDetail.receivableEmptyNone')
    : t('stockOutDetail.receivableEmptyNotFinished')
)

function dash(v: string | null | undefined): string {
  const s = String(v ?? '').trim()
  return s || '—'
}

function formatQty(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString()
}

function lineAmount(row: StockOutItemListRow): number | null {
  if (!unitPriceDockHasValue(row.salesPrice)) return null
  const qty = Number(row.outQuantity)
  const price = Number(row.salesPrice)
  if (!Number.isFinite(qty) || !Number.isFinite(price)) return null
  return qty * price
}

function amountText(amount: number, currency: number): string {
  if (!showAmount.value) return '—'
  const num = formatTotalAmountNumber(amount)
  if (num === '—') return '—'
  const iso = listAmountCurrencyIso(currency)
  return iso ? `${num} ${iso}` : num
}

function sellOrderLink(row: StockOutOpsSellOrderItemRow) {
  const oid = String(row.sellOrderId ?? '').trim()
  const iid = String(row.sellOrderItemId ?? '').trim()
  if (!oid) return null
  return {
    name: 'SalesOrderDetail',
    params: { id: oid },
    query: iid ? { sellOrderItemId: iid, salesOrderItemId: iid } : undefined
  }
}

function receivableVerificationLabel(status: number) {
  if (status === 2) return t('financeReceivableList.verification.complete')
  if (status === 1) return t('financeReceivableList.verification.partial')
  return t('financeReceivableList.verification.pending')
}

function receivableVerificationTagType(status: number): 'success' | 'warning' | 'info' {
  if (status === 2) return 'success'
  if (status === 1) return 'warning'
  return 'info'
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/so-item-ops-panel.scss';

.ops-overview-line {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
</style>
