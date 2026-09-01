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
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.salesOrderTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--so-header">
            <span class="ops-so-header__item">
              <span>{{ t('salesOrderItemList.opsPanel.salesOrderCode') }}：</span>
              <router-link
                v-if="salesOrderLink"
                :to="salesOrderLink"
                class="ops-so-code-link"
              >{{ salesOrderCode }}</router-link>
              <span v-else>{{ salesOrderCode }}</span>
            </span>
            <span class="ops-so-header__item">
              {{ t('salesOrderItemList.opsPanel.salesOrderStatus') }}：<span
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

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('salesOrderItemList.opsPanel.overviewTitle') }}</h3>
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

      <section v-if="showStockingUsagePanel" class="ops-card ops-card--stocking-usage">
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
                    class="link-text"
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
          <OpsGeneratedDocsRow
            :label="t('salesOrderItemList.opsPanel.purchaseRequisitionDocLabel')"
            :docs="linkedPurchaseRequisitionDocLinks"
            :mask-sensitive="maskSensitive"
          />
          <p v-if="purchaseDisabledHint && !purchaseCompleted" class="ops-status ops-status--warn">{{ purchaseDisabledHint.summary }}</p>
          <div v-if="!purchaseCompleted" class="ops-progress">
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
          <OpsGeneratedDocsRow
            :label="t('salesOrderItemList.opsPanel.stockOutNotifyDocLabel')"
            :docs="linkedStockOutRequestDocLinks"
            :mask-sensitive="maskSensitive"
          />
          <p v-if="stockOutDisabledHint && !stockOutNotifyCompleted" class="ops-status" :class="stockOutStatusClass">{{ stockOutDisabledHint.summary }}</p>
          <div v-if="!stockOutNotifyCompleted" class="ops-progress">
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
import {
  salesOrderMainAllowsPurchaseAndStockOut,
  salesOrderLineApplyStockOutButtonDisabled,
  translateSalesOrderStatus
} from '@/constants/salesOrderStatus'
import { formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import CustomerNameReadonlyText from '@/components/Customer/CustomerNameReadonlyText.vue'
import OpsGeneratedDocsRow from '@/components/Common/OpsGeneratedDocsRow.vue'
import { buildApplyPurchaseDisabledHintContent, applyPurchaseButtonDisabled } from '@/utils/applyPurchaseDisabledHint'
import { buildApplyStockOutDisabledHintContent } from '@/utils/applyStockOutDisabledHint'
import { listLinkedPurchaseRequisitionDocs, listLinkedStockOutRequestDocs } from '@/utils/opsGeneratedDocs'
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

const salesOrderCode = computed(() => {
  const v = String(props.row?.sellOrderCode ?? props.row?.SellOrderCode ?? '').trim()
  return v || '—'
})
const salesOrderId = computed(() =>
  String(props.row?.sellOrderId ?? props.row?.SellOrderId ?? '').trim()
)
const salesOrderLink = computed(() => {
  const id = salesOrderId.value
  if (!id || salesOrderCode.value === '—') return null
  return { name: 'SalesOrderDetail' as const, params: { id } }
})
const salesOrderStatus = computed(() => Number(props.row?.orderStatus ?? props.row?.OrderStatus))
const salesOrderStatusText = computed(() => {
  const s = salesOrderStatus.value
  if (!Number.isFinite(s)) return t('salesOrderList.status.unknown')
  return translateSalesOrderStatus(s, t)
})
const salesOrderStatusIsAlert = computed(() => salesOrderStatus.value === -1 || salesOrderStatus.value === -2)

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

const linkedPurchaseRequisitionDocLinks = computed(() =>
  listLinkedPurchaseRequisitionDocs(props.aggregates).map((doc) => ({
    ...doc,
    to: { name: 'PurchaseRequisitionDetail', params: { id: doc.id } }
  }))
)

const linkedStockOutRequestDocLinks = computed(() =>
  listLinkedStockOutRequestDocs(props.aggregates).map((doc) => ({
    ...doc,
    to: { name: 'StockOutNotifyDetail', params: { id: doc.id } }
  }))
)

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
@import '@/assets/styles/so-item-ops-panel.scss';

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
