<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="purchase-order-item-ops-panel"
  >
    <header v-if="!embedded" class="so-item-ops-panel__head">
      <h2 class="so-item-ops-panel__title">{{ t('purchaseOrderItemList.opsPanel.title') }}</h2>
      <button type="button" class="so-item-ops-panel__close" @click="emit('close')">
        {{ t('purchaseOrderItemList.opsPanel.close') }}
      </button>
    </header>

    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('purchaseOrderItemList.opsPanel.pickRow') }}
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
          <h3 class="ops-card__title">{{ t('purchaseOrderItemList.opsPanel.purchaseOrderTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--po-header">
            <span class="ops-po-header__item">
              <span>{{ t('purchaseOrderItemList.opsPanel.purchaseOrderCode') }}：</span>
              <router-link
                v-if="purchaseOrderLink"
                :to="purchaseOrderLink"
                class="ops-po-code-link"
              >{{ purchaseOrderCode }}</router-link>
              <span v-else>{{ purchaseOrderCode }}</span>
            </span>
            <span class="ops-po-header__item">
              {{ t('purchaseOrderItemList.opsPanel.purchaseOrderStatus') }}：{{ purchaseOrderStatusText }}<template
                v-if="purchaseOrderAwaitingVendorConfirm"
              >，<span class="ops-po-status-hint">{{ t('purchaseOrderItemList.opsPanel.purchaseOrderStatusAwaitingVendorHint') }}</span></template>
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

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('purchaseOrderItemList.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">{{ lineCode }}</div>
          <div class="ops-overview-line">{{ displayPn }}</div>
          <div class="ops-overview-line">{{ displayBrand }}</div>
          <div class="ops-overview-line">{{ displayUnitPriceWithCurrency }}</div>
          <div class="ops-overview-line">{{ formatQty(orderQty) }} pcs</div>
        </div>
      </section>

      <section class="ops-card ops-card--status-only">
        <div class="ops-card__body ops-card__body--status">
          <div class="ops-status-tags">
            <div class="ops-status-tags__row">
              <el-tag
                v-for="item in statusRow1"
                :key="item.kind"
                effect="dark"
                :type="poStatusTagType(item.kind, statusValue(item.prop))"
                size="small"
              >
                {{ poStatusLabel(t, item.kind, statusValue(item.prop)) }}
              </el-tag>
            </div>
            <div class="ops-status-tags__row">
              <el-tag
                v-for="item in statusRow2"
                :key="item.kind"
                effect="dark"
                :type="poStatusTagType(item.kind, statusValue(item.prop))"
                size="small"
              >
                {{ poStatusLabel(t, item.kind, statusValue(item.prop)) }}
              </el-tag>
            </div>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('purchaseOrderItemList.opsPanel.paymentTitle') }}</h3>
          <span v-if="paymentCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('purchaseOrderItemList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <div class="ops-metrics">
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('purchaseOrderItemList.opsPanel.requestedAmount') }}</span>
              <span class="ops-metrics__value ops-metrics__value--amount">
                <span class="ops-amount-stack">
                  <span class="ops-amount-stack__amount">{{ requestedAmountParts.amount }}</span>
                  <span
                    v-if="requestedAmountParts.code"
                    :class="['ops-amount-stack__ccy', 'dock-tier-ccy', listAmountCurrencyDockClass(paymentCurrency)]"
                  >{{ requestedAmountParts.code }}</span>
                </span>
              </span>
            </div>
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('purchaseOrderItemList.opsPanel.availableAmount') }}</span>
              <span class="ops-metrics__value ops-metrics__value--amount">
                <span class="ops-amount-stack">
                  <span class="ops-amount-stack__amount">{{ availableAmountParts.amount }}</span>
                  <span
                    v-if="availableAmountParts.code"
                    :class="['ops-amount-stack__ccy', 'dock-tier-ccy', listAmountCurrencyDockClass(paymentCurrency)]"
                  >{{ availableAmountParts.code }}</span>
                </span>
              </span>
            </div>
          </div>
          <OpsGeneratedDocsRow
            :label="t('purchaseOrderItemList.opsPanel.paymentDocLabel')"
            :docs="linkedPaymentDocLinks"
            :mask-sensitive="maskSensitive"
          />
          <p v-if="paymentDisabledHint && !paymentCompleted" class="ops-status ops-status--warn">{{ paymentDisabledHint.summary }}</p>
          <div v-if="!paymentCompleted" class="ops-progress">
            <div class="ops-progress__track">
              <div class="ops-progress__bar ops-progress__bar--payment" :style="{ width: `${paymentProgressPct}%` }" />
            </div>
          </div>
          <ul v-if="paymentDisabledHint?.details.length && !paymentCompleted" class="ops-hint-list">
            <li v-for="(line, idx) in paymentDisabledHint.details" :key="`p-${idx}`">{{ line }}</li>
          </ul>
          <p v-if="paymentDisabledHint && !paymentCompleted" class="ops-next-step">{{ paymentDisabledHint.nextStep }}</p>
          <button
            v-if="canInitiatePayment && !paymentCompleted"
            type="button"
            class="ops-action-btn"
            :class="paymentBtnDisabled ? 'ops-action-btn--disabled' : 'ops-action-btn--primary'"
            :disabled="paymentBtnDisabled"
            @click="emit('apply-payment')"
          >
            {{ t('purchaseOrderItemList.actions.applyPayment') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('purchaseOrderItemList.opsPanel.arrivalTitle') }}</h3>
          <span v-if="arrivalCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('purchaseOrderItemList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <div class="ops-metrics">
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('purchaseOrderItemList.opsPanel.notifiedQty') }}</span>
              <span class="ops-metrics__value">{{ formatQty(arrivalNotifiedQty) }}</span>
            </div>
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('purchaseOrderItemList.opsPanel.notifyAvailableQty') }}</span>
              <span class="ops-metrics__value">{{ formatQty(arrivalAvailableQty) }}</span>
            </div>
          </div>
          <OpsGeneratedDocsRow
            :label="t('purchaseOrderItemList.opsPanel.arrivalDocLabel')"
            :docs="linkedArrivalDocLinks"
            :mask-sensitive="maskSensitive"
          />
          <p v-if="arrivalDisabledHint && !arrivalCompleted" class="ops-status ops-status--warn">{{ arrivalDisabledHint.summary }}</p>
          <div v-if="!arrivalCompleted" class="ops-progress">
            <div class="ops-progress__track">
              <div class="ops-progress__bar ops-progress__bar--arrival" :style="{ width: `${arrivalProgressPct}%` }" />
            </div>
          </div>
          <ul v-if="arrivalDisabledHint?.details.length && !arrivalCompleted" class="ops-hint-list">
            <li v-for="(line, idx) in arrivalDisabledHint.details" :key="`a-${idx}`">{{ line }}</li>
          </ul>
          <p v-if="arrivalDisabledHint && !arrivalCompleted" class="ops-next-step">{{ arrivalDisabledHint.nextStep }}</p>
          <button
            v-if="canCreateArrivalNotice && !arrivalCompleted"
            type="button"
            class="ops-action-btn"
            :class="arrivalBtnDisabled ? 'ops-action-btn--disabled' : 'ops-action-btn--primary'"
            :disabled="arrivalBtnDisabled"
            @click="emit('apply-arrival')"
          >
            {{ t('purchaseOrderItemList.actions.notifyArrival') }}
          </button>
        </div>
      </section>
    </div>
  </component>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck } from '@element-plus/icons-vue'
import type { PurchaseOrderDetailTabAggregates } from '@/api/purchaseOrder'
import { formatUnitPriceWithCurrencyCodeSuffix, formatTotalAmountNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { buildApplyArrivalDisabledHintContent, applyArrivalButtonDisabled } from '@/utils/applyArrivalDisabledHint'
import { buildApplyPaymentDisabledHintContent, applyPaymentButtonDisabled, listLinkedFinancePaymentDocs } from '@/utils/applyPaymentDisabledHint'
import { listLinkedArrivalNoticeDocs } from '@/utils/opsGeneratedDocs'
import { calcProgressPercent, getArrivalMetrics, getPaymentMetrics, poStatusLabel, poStatusTagType, type PoItemStatusKind } from '@/utils/purchaseOrderItemOpsPanel'
import { purchaseOrderMainStatusLabel, purchaseOrderMainStatusAwaitingVendorConfirm } from '@/constants/purchaseOrderStatus'
import { DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import OpsGeneratedDocsRow from '@/components/Common/OpsGeneratedDocsRow.vue'

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: PurchaseOrderDetailTabAggregates | null
  loading?: boolean
  loadError?: string
  maskSensitive?: boolean
  canCreateArrivalNotice?: boolean
  canInitiatePayment?: boolean
  /** 嵌入右侧辅助栏「操作」页签时使用 */
  embedded?: boolean
}>()

const emit = defineEmits<{
  close: []
  clear: []
  'apply-arrival': []
  'apply-payment': []
}>()

const { t } = useI18n()

const purchaseOrderCode = computed(() => {
  const v = String(props.row?.purchaseOrderCode ?? props.row?.PurchaseOrderCode ?? '').trim()
  return v || '—'
})
const purchaseOrderId = computed(() =>
  String(props.row?.purchaseOrderId ?? props.row?.PurchaseOrderId ?? '').trim()
)
const purchaseOrderLink = computed(() => {
  const id = purchaseOrderId.value
  if (!id || purchaseOrderCode.value === '—') return null
  return { name: 'PurchaseOrderDetail' as const, params: { id } }
})
const purchaseOrderStatusRaw = computed(() => props.row?.orderStatus ?? props.row?.OrderStatus)
const purchaseOrderStatusText = computed(() =>
  purchaseOrderMainStatusLabel(t, purchaseOrderStatusRaw.value)
)
const purchaseOrderAwaitingVendorConfirm = computed(() =>
  purchaseOrderMainStatusAwaitingVendorConfirm(purchaseOrderStatusRaw.value)
)

const lineCode = computed(() => String(props.row?.purchaseOrderItemCode ?? '—') || '—')
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
const displayPn = computed(() => String(props.row?.pn ?? '—') || '—')
const displayBrand = computed(() => String(props.row?.brand ?? '—') || '—')
const displayUnitPriceWithCurrency = computed(() =>
  formatUnitPriceWithCurrencyCodeSuffix(props.row?.cost, Number(props.row?.currency))
)
const orderQty = computed(() => Math.max(0, Math.trunc(Number(props.row?.qty) || 0)))

const statusRow1: ReadonlyArray<{ kind: PoItemStatusKind; prop: string }> = [
  { kind: 'paymentRequest', prop: 'paymentRequestProgressStatus' },
  { kind: 'payment', prop: 'paymentProgressStatus' }
]

const statusRow2: ReadonlyArray<{ kind: PoItemStatusKind; prop: string }> = [
  { kind: 'purchase', prop: 'purchaseProgressStatus' },
  { kind: 'stockIn', prop: 'stockInProgressStatus' },
  { kind: 'invoice', prop: 'invoiceProgressStatus' }
]

function statusValue(prop: string): number | undefined {
  const raw = props.row?.[prop]
  if (raw === undefined || raw === null || raw === '') return undefined
  const n = Number(raw)
  return Number.isFinite(n) ? n : undefined
}

const arrivalMetrics = computed(() =>
  props.row ? getArrivalMetrics(props.row, props.aggregates) : { orderQty: 0, notifiedQty: 0, applicableQty: 0 }
)
const arrivalNotifiedQty = computed(() => arrivalMetrics.value.notifiedQty)
const arrivalAvailableQty = computed(() => arrivalMetrics.value.applicableQty)
const arrivalProgressPct = computed(() =>
  calcProgressPercent(arrivalNotifiedQty.value, arrivalMetrics.value.orderQty || orderQty.value)
)

const paymentMetrics = computed(() =>
  props.row
    ? getPaymentMetrics(props.row, props.aggregates)
    : { lineTotal: 0, requestedAmount: 0, availableAmount: 0, currency: DEFAULT_SETTLEMENT_CURRENCY_CODE }
)
const paymentCurrency = computed(() => paymentMetrics.value.currency)

function splitPaymentAmountParts(value: number) {
  const amount = formatTotalAmountNumber(value)
  if (amount === '—') return { amount: '—', code: '' }
  return { amount, code: listAmountCurrencyIso(paymentCurrency.value) }
}

const requestedAmountParts = computed(() => splitPaymentAmountParts(paymentMetrics.value.requestedAmount))
const availableAmountParts = computed(() => splitPaymentAmountParts(paymentMetrics.value.availableAmount))
const paymentProgressPct = computed(() =>
  calcProgressPercent(paymentMetrics.value.requestedAmount, paymentMetrics.value.lineTotal)
)

const arrivalCompleted = computed(() => {
  if (!props.row) return false
  const total = arrivalMetrics.value.orderQty || orderQty.value
  if (total <= 0) return false
  return arrivalAvailableQty.value <= 0 && arrivalNotifiedQty.value >= total
})

const paymentCompleted = computed(() => {
  if (!props.row) return false
  const { lineTotal, requestedAmount, availableAmount } = paymentMetrics.value
  if (lineTotal <= 0) return false
  return availableAmount <= 0 && requestedAmount >= lineTotal
})

const arrivalDisabledHint = computed(() => (props.row ? buildApplyArrivalDisabledHintContent(props.row, t) : null))
const paymentDisabledHint = computed(() =>
  props.row
    ? buildApplyPaymentDisabledHintContent(props.row, t, {
        canInitiatePayment: props.canInitiatePayment,
        aggregates: props.aggregates
      })
    : null
)

const linkedPaymentDocLinks = computed(() =>
  (listLinkedFinancePaymentDocs(props.aggregates) ?? []).map((doc) => ({
    ...doc,
    to: { name: 'FinancePaymentDetail', params: { id: doc.id } }
  }))
)

const linkedArrivalDocLinks = computed(() =>
  listLinkedArrivalNoticeDocs(props.aggregates).map((doc) => ({
    ...doc,
    to: { name: 'ArrivalNoticeList', query: { noticeId: doc.id } }
  }))
)

const arrivalBtnDisabled = computed(() => !props.row || applyArrivalButtonDisabled(props.row))
const paymentBtnDisabled = computed(
  () =>
    !props.row ||
    applyPaymentButtonDisabled(props.row, {
      canInitiatePayment: props.canInitiatePayment,
      aggregates: props.aggregates
    })
)

function formatQty(v: number) {
  if (!Number.isFinite(v)) return '—'
  return v.toLocaleString()
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-ops-root--embedded {
  flex: 1;
  width: 100%;
  min-width: 0;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow: visible;
}

.so-item-ops-root--embedded .so-item-ops-root__empty,
.so-item-ops-root--embedded .so-item-ops-root__error {
  color: var(--crm-aux-placeholder);
}

.so-item-ops-root__content--embedded {
  width: 100%;
  min-width: 0;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0;
  overflow: visible;
}

.so-item-ops-root--embedded .ops-card {
  width: 100%;
  min-width: 0;
  max-width: 100%;
  box-sizing: border-box;
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: 12px;
  box-shadow: none;
}

.so-item-ops-root--embedded .ops-card__head,
.so-item-ops-root--embedded .ops-card__body {
  min-width: 0;
}

.so-item-ops-root--embedded .ops-card__title,
.so-item-ops-root--embedded .ops-kv__value,
.so-item-ops-root--embedded .ops-metrics__value {
  color: var(--crm-aux-body-text);
}

.so-item-ops-root--embedded .ops-kv__label,
.so-item-ops-root--embedded .ops-metrics__label,
.so-item-ops-root--embedded .ops-hint-list,
.so-item-ops-root--embedded .ops-status--ok {
  color: var(--crm-aux-hint);
}

.so-item-ops-root--embedded .ops-metrics {
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
}

.so-item-ops-root--embedded .ops-metrics__value--amount {
  font-size: inherit;
  line-height: inherit;
}

.ops-amount-stack {
  display: flex;
  flex-direction: column;
  gap: 2px;
  line-height: 1.25;
}

.ops-amount-stack__amount {
  font-size: 20px;
  font-weight: 700;
  color: inherit;
}

.ops-amount-stack__ccy {
  font-size: 12px;
  font-weight: 500;
}

.ops-metrics__value--amount {
  font-size: inherit;
  line-height: inherit;
}

.so-item-ops-root--embedded .ops-progress {
  flex-direction: column;
  align-items: stretch;
  gap: 6px;
}

.so-item-ops-root--embedded .ops-progress__track {
  background: var(--crm-aux-tab-bg);
}

.so-item-ops-root--embedded .ops-next-step,
.so-item-ops-root--embedded .ops-hint-list,
.so-item-ops-root--embedded .ops-status {
  min-width: 0;
  word-break: break-word;
}

.so-item-ops-root--embedded .ops-next-step {
  background: var(--crm-accent-005);
  color: var(--crm-aux-body-text);
}

.so-item-ops-root--embedded .ops-action-btn {
  max-width: 100%;
  box-sizing: border-box;
}

.so-item-ops-panel {
  flex: 0 0 min(400px, 36vw);
  width: min(400px, 36vw);
  min-width: 320px;
  max-height: calc(100vh - 180px);
  display: flex;
  flex-direction: column;
  background: #f3f5f8;
  border: 1px solid $border-panel;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08);
}

.so-item-ops-panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  background: #fff;
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
}

.so-item-ops-panel__title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.so-item-ops-panel__close {
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 13px;
}

.so-item-ops-root--embedded :deep(.el-loading-parent--relative) {
  width: 100%;
  min-width: 0;
  max-width: 100%;
  overflow-x: hidden;
}

.so-item-ops-root__empty,
.so-item-ops-root__error {
  padding: 0;
  font-size: 13px;
}

.so-item-ops-root__error {
  color: $danger-color;
}

.so-item-ops-panel__body {
  flex: 1;
  overflow: auto;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.ops-card {
  width: 100%;
  min-width: 0;
  box-sizing: border-box;
  background: #fff;
  border-radius: 12px;
  border: 1px solid rgba(15, 23, 42, 0.06);
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.04);
}

.ops-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 12px 14px 0;
}

.ops-card__done {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
  font-size: 12px;
  font-weight: 600;
  color: #67c23a;
}

.ops-card__done-icon {
  font-size: 14px;
}

.ops-card__title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.ops-card__body {
  padding: 10px 14px 12px;
}

.ops-card__body--overview {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.ops-overview-line {
  width: 100%;
  text-align: left;
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  line-height: 1.5;
  word-break: break-word;
}

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

.ops-overview-line--hero {
  font-size: 18px;
  font-weight: 700;
  color: $color-amber;
}

.so-item-ops-root--embedded .ops-overview-line {
  color: var(--crm-aux-body-text);
}

.so-item-ops-root--embedded .ops-overview-line--hero {
  color: $color-amber;
}

.ops-card--status-only .ops-card__body--status {
  padding-top: 10px;
}

.ops-card__body--status {
  padding-top: 8px;
}

.ops-status-tags {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.ops-status-tags__row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  column-gap: 8px;
  align-items: center;
}

.ops-status-tags__row :deep(.el-tag) {
  width: 100%;
  justify-content: center;
  box-sizing: border-box;
}

.ops-metrics {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 10px;
  margin-bottom: 8px;
}

.ops-metrics:last-child {
  margin-bottom: 0;
}

.ops-metrics__item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.ops-metrics__label {
  font-size: 12px;
  color: $text-secondary;
}

.ops-metrics__value {
  font-size: 20px;
  font-weight: 700;
  color: $text-primary;
}

.ops-status {
  margin: 8px 0 0;
  font-size: 12px;
  line-height: 1.5;
}

.ops-status--warn {
  color: $danger-color;
}

.ops-progress {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin: 10px 0;
}

.ops-progress:last-child {
  margin-bottom: 0;
}

.ops-progress__track {
  flex: 1 1 100%;
  min-width: 0;
  width: 100%;
  height: 10px;
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.08);
  overflow: hidden;
}

.ops-progress__bar {
  height: 100%;
  border-radius: inherit;
  transition: width 0.2s ease;
}

.ops-progress__bar--arrival {
  background: linear-gradient(90deg, #34d399, #059669);
}

.ops-progress__bar--payment {
  background: linear-gradient(90deg, #fbbf24, #d97706);
}

.ops-hint-list {
  margin: 0 0 8px;
  padding-left: 18px;
  font-size: 12px;
  color: $text-secondary;
  line-height: 1.5;
}

.ops-next-step {
  margin: 0 0 12px;
  padding: 8px 10px;
  border-radius: 8px;
  background: rgba(56, 189, 248, 0.08);
  color: $text-primary;
  font-size: 12px;
  line-height: 1.55;
}

.ops-action-btn {
  width: 100%;
  border: none;
  border-radius: 10px;
  padding: 11px 14px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.ops-action-btn--primary {
  background: #0f4c81;
  color: #fff;
}

.ops-action-btn--disabled,
.ops-action-btn:disabled {
  cursor: not-allowed;
  background: #e5e7eb;
  color: #9ca3af;
  opacity: 1;
}
</style>
