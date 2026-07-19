<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="qc-ops-panel"
  >
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('qcList.opsPanel.pickRow') }}
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
          <h3 class="ops-card__title">{{ t('qcList.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">
            <router-link v-if="qcLink" :to="qcLink" class="link-text">{{ qcCode }}</router-link>
            <span v-else>{{ qcCode }}</span>
          </div>
          <div class="ops-overview-line">
            <el-tag effect="dark" :type="qcStatusTagType" size="small">{{ qcStatusLabel }}</el-tag>
          </div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('qcList.opsPanel.passQty') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ formatQty(passQty) }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('qcList.opsPanel.rejectQty') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ formatQty(rejectQty) }}</span>
            </div>
          </div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInEligibleQty') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ formatQty(stockInEligibleQty) }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('qcList.opsPanel.qcImages') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ qcImageCountText }}</span>
            </div>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('qcList.opsPanel.createStockInTitle') }}</h3>
          <span v-if="createStockInCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('qcList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <p
            v-if="createStockInDisabledHint && !createStockInCompleted"
            class="ops-status ops-status--warn"
          >
            {{ createStockInDisabledHint.summary }}
          </p>
          <ul
            v-if="createStockInDisabledHint?.details.length && !createStockInCompleted"
            class="ops-hint-list"
          >
            <li v-for="(line, idx) in createStockInDisabledHint.details" :key="`csi-${idx}`">{{ line }}</li>
          </ul>
          <p v-if="createStockInDisabledHint && !createStockInCompleted" class="ops-next-step">
            {{ createStockInDisabledHint.nextStep }}
          </p>
          <button
            v-if="canWriteLogistics && !createStockInCompleted"
            type="button"
            class="ops-action-btn"
            :class="createStockInBtnDisabled ? 'ops-action-btn--disabled' : 'ops-action-btn--primary'"
            :disabled="createStockInBtnDisabled || actionLoading"
            @click="emit('create-stock-in')"
          >
            {{ t('qcList.actions.createStockIn') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('qcList.opsPanel.purchaseTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="purchase">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.purchaseItemCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link v-if="purchaseLink && !maskSensitive" :to="purchaseLink" class="link-text">
                    {{ purchaseItemCode }}
                  </router-link>
                  <span v-else>{{ purchaseItemCode }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.purchaseUser') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ purchaseUserName }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.purchaseDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ purchaseOrderCreateDateText }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.purchaseQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(purchaseQty) }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('qcList.opsPanel.noPurchase') }}</p>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('qcList.opsPanel.arrivalTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="arrivalNotice">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.arrivalNoticeCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link v-if="arrivalNoticeLink" :to="arrivalNoticeLink" class="link-text">
                    {{ arrivalNoticeCode }}
                  </router-link>
                  <span v-else>{{ arrivalNoticeCode }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.arrivalType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <StockBizTypeTag
                    biz="in"
                    :type="arrivalStockInType"
                    :customs-declaration-id="customsDeclarationId"
                    :customs-declaration-code="customsDeclarationCode"
                  />
                </span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.arrivalDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ arrivalDateText }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.arrivalQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(arrivalQty) }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('qcList.opsPanel.noArrival') }}</p>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('qcList.opsPanel.stockInTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="stockIn">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link v-if="stockInLink" :to="stockInLink" class="link-text">
                    {{ stockIn.stockInCode || '—' }}
                  </router-link>
                  <span v-else>{{ stockIn.stockInCode || '—' }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInUser') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockIn.createUserName?.trim() || '—' }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockInDateText }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInStatus') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockInStatusText }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockInTypeLabel }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInWarehouse') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockIn.warehouseName?.trim() || '—' }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.stockInQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(stockIn.totalQuantity) }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('qcList.opsPanel.noStockIn') }}</p>
        </div>
      </section>
    </div>
  </component>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck } from '@element-plus/icons-vue'
import type { QcOpsAggregatesDto } from '@/api/logistics'
import { formatDisplayDate } from '@/utils/displayDateTime'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { StockInTypeCode } from '@/constants/stockInType'
import {
  buildQcCreateStockInDisabledHintContent,
  qcCreateStockInButtonDisabled,
  qcCreateStockInCompleted
} from '@/utils/qcCreateStockInDisabledHint'

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: QcOpsAggregatesDto | null
  loading?: boolean
  loadError?: string
  actionLoading?: boolean
  canWriteLogistics?: boolean
  maskSensitive?: boolean
  qcImageCount?: number
  embedded?: boolean
}>()

const emit = defineEmits<{
  'create-stock-in': []
}>()

const { t } = useI18n()

const qcCode = computed(() => String(props.row?.qcCode ?? props.row?.QcCode ?? '—') || '—')
const qcStatus = computed(() => Number(props.row?.status ?? props.row?.Status ?? 0))
const passQty = computed(() => Number(props.row?.passQty ?? props.row?.PassQty ?? 0))
const rejectQty = computed(() => Number(props.row?.rejectQty ?? props.row?.RejectQty ?? 0))
const customsDeclarationId = computed(() =>
  (props.row?.customsDeclarationId ?? props.row?.CustomsDeclarationId) as string | null | undefined
)
const customsDeclarationCode = computed(() =>
  (props.row?.customsDeclarationCode ?? props.row?.CustomsDeclarationCode) as string | null | undefined
)

const qcStatusLabel = computed(() => {
  const keyMap: Record<number, 'failed' | 'partial' | 'passed'> = {
    [-1]: 'failed',
    10: 'partial',
    100: 'passed'
  }
  const k = keyMap[qcStatus.value]
  return k ? t(`qcList.qcStatus.${k}`) : t('qcList.qcStatus.unknown')
})

const qcStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = qcStatus.value
  if (s === 100) return 'success'
  if (s === 10) return 'warning'
  if (s === -1) return 'danger'
  return 'info'
})

const purchase = computed(() => props.aggregates?.purchase ?? null)
const arrivalNotice = computed(() => props.aggregates?.arrivalNotice ?? null)
const stockIn = computed(() => props.aggregates?.stockIn ?? null)

const createStockInCompleted = computed(() => {
  if (!props.row) return false
  return qcCreateStockInCompleted(props.row, !!stockIn.value)
})

const createStockInDisabledHint = computed(() => {
  if (!props.row) return null
  return buildQcCreateStockInDisabledHintContent(
    props.row,
    props.canWriteLogistics === true,
    !!stockIn.value,
    t
  )
})

const createStockInBtnDisabled = computed(() => {
  if (!props.row) return true
  return qcCreateStockInButtonDisabled(props.row)
})

const stockInEligibleQty = computed(() => passQty.value)

const qcImageCountText = computed(() =>
  t('qcList.opsPanel.qcImageCount', { count: Math.max(0, Number(props.qcImageCount ?? 0)) })
)

const qcLink = computed(() => {
  const id = String(props.row?.id ?? props.row?.Id ?? '').trim()
  if (!id) return null
  return { name: 'QcCreate', query: { qcId: id } }
})

const purchaseItemCode = computed(() => {
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

const arrivalNoticeCode = computed(() => arrivalNotice.value?.noticeCode?.trim() || '—')
const arrivalStockInType = computed(() =>
  Number(arrivalNotice.value?.stockInType ?? StockInTypeCode.Purchase)
)
const arrivalQty = computed(() => Number(arrivalNotice.value?.expectQty ?? 0))

const arrivalDateText = computed(() => {
  const actual = arrivalNotice.value?.actualArrivalDate
  if (actual) return formatDisplayDate(String(actual))
  const expected = arrivalNotice.value?.expectedArrivalDate
  return expected ? formatDisplayDate(String(expected)) : '—'
})

const arrivalNoticeLink = computed(() => {
  const id = arrivalNotice.value?.id?.trim()
  if (!id) return null
  return { name: 'ArrivalNoticeList', query: { noticeId: id } }
})

const stockInLink = computed(() => {
  const id = stockIn.value?.id?.trim()
  if (!id) return null
  return { name: 'StockInDetail', params: { id } }
})

const stockInDateText = computed(() => {
  const raw = stockIn.value?.stockInDate
  return raw ? formatDisplayDate(String(raw)) : '—'
})

const stockInStatusText = computed(() => {
  const s = stockIn.value?.status
  if (s === 0) return t('stockInList.status.draft')
  if (s === 1) return t('stockInList.status.pending')
  if (s === 2) return t('stockInList.status.done')
  if (s === 3) return t('stockInList.status.cancelled')
  return '—'
})

const stockInTypeLabel = computed(() => {
  const type = stockIn.value?.stockInType ?? arrivalStockInType.value
  if (type === StockInTypeCode.Customs) return t('stockInList.stockInTypeLabels.customs')
  if (type === StockInTypeCode.Return) return t('stockInList.stockInTypeLabels.return')
  if (type === StockInTypeCode.Scrap) return t('stockInList.stockInTypeLabels.scrap')
  return t('stockInList.stockInTypeLabels.purchase')
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
</style>
