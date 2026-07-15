<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="sales-order-item-ops-panel"
  >
    <header v-if="!embedded" class="so-item-ops-panel__head">
      <h2 class="so-item-ops-panel__title">{{ t('salesOrderItemList.opsPanel.title') }}</h2>
      <button type="button" class="so-item-ops-panel__close" @click="emit('close')">
        {{ t('salesOrderItemList.opsPanel.close') }}
      </button>
    </header>

    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('salesOrderItemList.opsPanel.pickRow') }}
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
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">{{ lineCode }}</div>
          <div class="ops-overview-line">
            <CustomerNameReadonlyText
              :name-zh="customerNameZh"
              :name-en="customerNameEn"
              :masked="maskSensitive"
            />
          </div>
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
                v-for="item in progressRow1"
                :key="item.kind"
                effect="dark"
                :type="extendTriTagType(progressStatus(item.prop))"
                size="small"
              >
                {{ extendTriLabel(t, item.kind, progressStatus(item.prop)) }}
              </el-tag>
            </div>
            <div class="ops-status-tags__row">
              <el-tag
                v-for="item in progressRow2"
                :key="item.kind"
                effect="dark"
                :type="extendTriTagType(progressStatus(item.prop))"
                size="small"
              >
                {{ extendTriLabel(t, item.kind, progressStatus(item.prop)) }}
              </el-tag>
            </div>
          </div>
        </div>
      </section>

      <section v-if="showStockPanel" class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.stockTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockDomestic') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value ops-kv__value--accent">{{ formatQty(stockSummary.domestic) }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockOverseas') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value ops-kv__value--accent">{{ formatQty(stockSummary.overseas) }}</span>
            </div>
          </div>
          <div class="ops-kv ops-kv--divider">
            <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockTotal') }}</span>
            <span class="ops-kv__value ops-kv__value--strong">{{ formatQty(stockSummary.total) }}</span>
          </div>
        </div>
      </section>

      <section v-if="showStockingUsagePanel" class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.stockingUsageTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--stocking-usage">
          <div
            v-for="(entry, idx) in stockingUsageItems"
            :key="entry.purchaseOrderId || idx"
            class="ops-stocking-usage-entry"
            :class="{ 'ops-stocking-usage-entry--divider': idx > 0 }"
          >
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockingUsagePoCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link
                    v-if="entry.purchaseOrderId && canViewPurchaseOrder"
                    :to="{ name: 'PurchaseOrderDetail', params: { id: entry.purchaseOrderId } }"
                    class="cell-link"
                  >
                    {{ entry.purchaseOrderCode || '—' }}
                  </router-link>
                  <span v-else>{{ entry.purchaseOrderCode || '—' }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockingUsagePurchaser') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ entry.purchaseUserName?.trim() || '—' }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockingUsagePoDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatPoCreateDate(entry.purchaseOrderCreateTime) }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('salesOrderItemList.opsPanel.stockingUsageQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value ops-kv__value--accent">{{ formatQty(entry.usedQty) }}</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.purchaseTitle') }}</h3>
          <span v-if="purchaseCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('salesOrderItemList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <div class="ops-metrics">
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('salesOrderItemList.opsPanel.appliedQty') }}</span>
              <span class="ops-metrics__value">{{ formatQty(purchaseAppliedQty) }}</span>
            </div>
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('salesOrderItemList.opsPanel.availableQty') }}</span>
              <span class="ops-metrics__value">{{ formatQty(purchaseAvailableQty) }}</span>
            </div>
          </div>
          <p v-if="purchaseDisabledHint && !purchaseCompleted" class="ops-status ops-status--warn">{{ purchaseDisabledHint.summary }}</p>
          <div class="ops-progress">
            <div class="ops-progress__track">
              <div class="ops-progress__bar ops-progress__bar--purchase" :style="{ width: `${purchaseProgressPct}%` }" />
            </div>
          </div>
          <ul v-if="purchaseDisabledHint?.details.length && !purchaseCompleted" class="ops-hint-list">
            <li v-for="(line, idx) in purchaseDisabledHint.details" :key="`p-${idx}`">{{ line }}</li>
          </ul>
          <p v-if="purchaseDisabledHint && !purchaseCompleted" class="ops-next-step">{{ purchaseDisabledHint.nextStep }}</p>
          <button
            v-if="canPurchaseReq && !purchaseCompleted"
            type="button"
            class="ops-action-btn"
            :class="purchaseBtnDisabled ? 'ops-action-btn--disabled' : 'ops-action-btn--primary'"
            :disabled="purchaseBtnDisabled"
            @click="emit('apply-purchase')"
          >
            {{ t('salesOrderItemList.actions.applyPurchase') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.stockOutTitle') }}</h3>
          <span v-if="stockOutNotifyCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('salesOrderItemList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <div class="ops-metrics">
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('salesOrderItemList.opsPanel.notifiedQty') }}</span>
              <span class="ops-metrics__value">{{ formatQty(stockOutNotifiedQty) }}</span>
            </div>
            <div class="ops-metrics__item">
              <span class="ops-metrics__label">{{ t('salesOrderItemList.opsPanel.notifyAvailableQty') }}</span>
              <span class="ops-metrics__value">{{ formatQty(stockOutAvailableQty) }}</span>
            </div>
          </div>
          <p v-if="stockOutDisabledHint && !stockOutNotifyCompleted" class="ops-status" :class="stockOutStatusClass">{{ stockOutDisabledHint.summary }}</p>
          <div class="ops-progress">
            <div class="ops-progress__track">
              <div class="ops-progress__bar ops-progress__bar--stock-out" :style="{ width: `${stockOutProgressPct}%` }" />
            </div>
          </div>
          <ul v-if="stockOutDisabledHint?.details.length && !stockOutNotifyCompleted" class="ops-hint-list">
            <li v-for="(line, idx) in stockOutDisabledHint.details" :key="`s-${idx}`">{{ line }}</li>
          </ul>
          <p v-if="stockOutDisabledHint && !stockOutNotifyCompleted" class="ops-next-step">{{ stockOutDisabledHint.nextStep }}</p>
          <button
            v-if="canWriteSo && !stockOutNotifyCompleted"
            type="button"
            class="ops-action-btn"
            :class="stockOutBtnDisabled ? 'ops-action-btn--disabled' : 'ops-action-btn--primary'"
            :disabled="stockOutBtnDisabled"
            @click="emit('apply-stock-out')"
          >
            {{ t('salesOrderItemList.actions.applyStockOut') }}
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
import { useAuthStore } from '@/stores/auth'
import type { SalesOrderDetailTabAggregates } from '@/api/salesOrder'
import { salesOrderMainAllowsPurchaseAndStockOut, salesOrderLineApplyStockOutButtonDisabled } from '@/constants/salesOrderStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import CustomerNameReadonlyText from '@/components/Customer/CustomerNameReadonlyText.vue'
import { buildApplyPurchaseDisabledHintContent, applyPurchaseButtonDisabled } from '@/utils/applyPurchaseDisabledHint'
import { buildApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'
import {
  calcProgressPercent,
  extendTriLabel,
  extendTriTagType,
  summarizeStockingByRegion,
  type SellOrderExtendProgressKind
} from '@/utils/sellOrderItemOpsPanel'

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: SalesOrderDetailTabAggregates | null
  loading?: boolean
  loadError?: string
  maskSensitive?: boolean
  canPurchaseReq?: boolean
  canWriteSo?: boolean
  /** 嵌入右侧辅助栏「操作」页签时使用 */
  embedded?: boolean
}>()

const emit = defineEmits<{
  close: []
  clear: []
  'apply-purchase': []
  'apply-stock-out': []
}>()

const { t } = useI18n()
const authStore = useAuthStore()

const canViewPurchaseOrder = computed(() => authStore.hasPermission('purchase-order.read'))

const lineCode = computed(() => String(props.row?.sellOrderItemCode ?? '—') || '—')
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
const displayPn = computed(() => String(props.row?.pn ?? '—') || '—')
const displayBrand = computed(() => String(props.row?.brand ?? '—') || '—')
const displayUnitPriceWithCurrency = computed(() =>
  formatUnitPriceWithCurrencyCodeSuffix(props.row?.price, Number(props.row?.currency))
)
const orderQty = computed(() => Math.max(0, Math.trunc(Number(props.row?.qty) || 0)))

const progressRow1: ReadonlyArray<{ kind: SellOrderExtendProgressKind; prop: string }> = [
  { kind: 'purchase', prop: 'purchaseProgressStatus' },
  { kind: 'stockIn', prop: 'stockInProgressStatus' },
  { kind: 'stockOutNotify', prop: 'stockOutNotifyProgressStatus' }
]

const progressRow2: ReadonlyArray<{ kind: SellOrderExtendProgressKind; prop: string }> = [
  { kind: 'stockOut', prop: 'stockOutProgressStatus' },
  { kind: 'receipt', prop: 'receiptProgressStatus' },
  { kind: 'invoice', prop: 'invoiceProgressStatus' }
]

function progressStatus(prop: string): number | undefined {
  const raw = props.row?.[prop]
  if (raw === undefined || raw === null || raw === '') return undefined
  const n = Number(raw)
  return Number.isFinite(n) ? n : undefined
}

const stockSummary = computed(() => summarizeStockingByRegion(props.aggregates?.stockItems ?? []))
const showStockPanel = computed(() => stockSummary.value.total > 0)

const stockingUsageItems = computed(() => props.aggregates?.stockingUsage?.items ?? [])
const showStockingUsagePanel = computed(() => stockingUsageItems.value.length > 0)

function formatPoCreateDate(raw?: string | null): string {
  if (!raw) return '—'
  return String(raw).slice(0, 10)
}

const purchaseAppliedQty = computed(() => {
  const fromOverview = props.aggregates?.lineOverview?.purchaseRequisition?.done
  if (fromOverview !== undefined && fromOverview !== null) return Math.max(0, Math.trunc(Number(fromOverview) || 0))
  const remaining = props.row?.purchaseRemainingQty
  if (remaining === undefined || remaining === null) return 0
  const rem = Math.max(0, Math.trunc(Number(remaining) || 0))
  return Math.max(0, orderQty.value - rem)
})

const purchaseAvailableQty = computed(() => {
  const raw = props.row?.purchaseRemainingQty
  if (raw === undefined || raw === null) return orderQty.value
  return Math.max(0, Math.trunc(Number(raw) || 0))
})

const stockOutNotifiedQty = computed(() => {
  const fromOverview = props.aggregates?.lineOverview?.stockOutNotify?.done
  if (fromOverview !== undefined && fromOverview !== null) return Math.max(0, Math.trunc(Number(fromOverview) || 0))
  return 0
})

const stockOutAvailableQty = computed(() => {
  const fromOverview = props.aggregates?.lineOverview?.stockOutNotify?.pending
  if (fromOverview !== undefined && fromOverview !== null) return Math.max(0, Math.trunc(Number(fromOverview) || 0))
  return Math.max(0, orderQty.value - stockOutNotifiedQty.value)
})

const purchaseProgressPct = computed(() => calcProgressPercent(purchaseAppliedQty.value, orderQty.value))
const stockOutProgressPct = computed(() => calcProgressPercent(stockOutNotifiedQty.value, orderQty.value))

const purchaseCompleted = computed(() => {
  if (!props.row || orderQty.value <= 0) return false
  if (!salesOrderMainAllowsPurchaseAndStockOut(Number(props.row.orderStatus))) return false
  if (Number(props.row.purchaseProgressStatus) === 2) return true
  return purchaseAvailableQty.value <= 0 && purchaseAppliedQty.value >= orderQty.value
})

const stockOutNotifyCompleted = computed(() => {
  if (!props.row || orderQty.value <= 0) return false
  if (!salesOrderMainAllowsPurchaseAndStockOut(Number(props.row.orderStatus))) return false
  if (Number(props.row.stockOutNotifyProgressStatus) === 2) return true
  return stockOutAvailableQty.value <= 0 && stockOutNotifiedQty.value >= orderQty.value
})

const purchaseDisabledHint = computed(() => (props.row ? buildApplyPurchaseDisabledHintContent(props.row, t) : null))

const stockOutDisabledHint = computed(() => {
  if (!props.row) return null
  if (!salesOrderMainAllowsPurchaseAndStockOut(Number(props.row.orderStatus))) {
    return {
      summary: t('salesOrderItemList.messages.applyStockOutNeedAudit'),
      details: [],
      nextStep: t('salesOrderItemList.opsPanel.stockOutNextAudit')
    }
  }
  return buildApplyStockOutDisabledHintContent(props.row, t)
})

const purchaseBtnDisabled = computed(() => !props.row || applyPurchaseButtonDisabled(props.row))
const stockOutBtnDisabled = computed(() => {
  if (!props.row) return true
  if (!salesOrderMainAllowsPurchaseAndStockOut(Number(props.row.orderStatus))) return true
  return salesOrderLineApplyStockOutButtonDisabled(props.row)
})

const stockOutStatusClass = computed(() => {
  if (!stockOutDisabledHint.value) return 'ops-status--ok'
  const progress = Number(props.row?.stockOutNotifyProgressStatus)
  return progress === 1 ? 'ops-status--info' : 'ops-status--warn'
})

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

.so-item-ops-root--embedded .ops-kv__label,
.so-item-ops-root--embedded .ops-kv__value {
  min-width: 0;
  word-break: break-word;
}

.so-item-ops-root--embedded .ops-kv__value {
  flex: 0 1 46%;
  max-width: 46%;
}

.so-item-ops-root--embedded .ops-stock-region-cell .ops-kv__value {
  flex: 0 0 auto;
  max-width: none;
  text-align: left;
}

.so-item-ops-root--embedded .ops-kv--divider {
  border-top-color: $border-panel;
}

.so-item-ops-root--embedded .ops-metrics {
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
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

.so-item-ops-root--embedded .so-item-ops-root__empty,
.so-item-ops-root--embedded .so-item-ops-root__error {
  color: var(--crm-aux-placeholder);
}

.so-item-ops-root__error {
  color: $danger-color;
}

.so-item-ops-panel__empty,
.so-item-ops-panel__error {
  padding: 16px;
  font-size: 13px;
  color: $text-secondary;
}

.so-item-ops-panel__error {
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
  padding: 10px 14px 14px;
}

.ops-card__body--overview {
  display: flex;
  flex-direction: column;
  gap: 6px;
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

.ops-overview-line {
  width: 100%;
  text-align: left;
  font-size: 13px;
  font-weight: 500;
  color: $text-primary;
  line-height: 1.5;
  word-break: break-word;
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

.ops-kv {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 6px 0;
  font-size: 13px;
}

.ops-kv--hero {
  padding-top: 4px;
  padding-bottom: 10px;
}

.ops-kv--divider {
  margin-top: 4px;
  padding-top: 10px;
  border-top: 1px solid rgba(15, 23, 42, 0.08);
}

.ops-stock-region-row {
  display: flex;
  align-items: stretch;
  gap: 8px;
  padding: 6px 0;
}

.ops-stock-region-cell {
  flex: 1 1 50%;
  min-width: 0;
  display: flex;
  align-items: baseline;
  justify-content: flex-start;
  gap: 0;
  font-size: 13px;
  text-align: left;
}

.ops-stock-region-cell .ops-kv__label {
  flex: 0 0 auto;
}

.ops-stock-region-cell .ops-kv__sep {
  flex: 0 0 auto;
  color: $text-secondary;
}

.ops-stock-region-cell .ops-kv__value {
  flex: 0 0 auto;
  text-align: left;
  max-width: none;
  min-width: 0;
}

.ops-kv__label {
  color: $text-secondary;
  flex: 1;
}

.ops-kv__value {
  color: $text-primary;
  text-align: right;
  font-weight: 500;
}

.ops-kv__value--strong {
  font-size: 18px;
  font-weight: 700;
}

.ops-kv__value--accent {
  color: $cyan-primary;
  font-weight: 700;
}

.ops-stocking-usage-entry--divider {
  margin-top: 8px;
  padding-top: 10px;
  border-top: 1px solid rgba(15, 23, 42, 0.08);
}

.ops-metrics {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 10px;
  margin-bottom: 8px;
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

.ops-status--info {
  color: $cyan-primary;
}

.ops-status--ok {
  color: $text-secondary;
}

.ops-progress {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin: 10px 0;
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

.ops-progress__bar--purchase {
  background: linear-gradient(90deg, #f87171, #ef4444);
}

.ops-progress__bar--stock-out {
  background: linear-gradient(90deg, #38bdf8, #0284c7);
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
