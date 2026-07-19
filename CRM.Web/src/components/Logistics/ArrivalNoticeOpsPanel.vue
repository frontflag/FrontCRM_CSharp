<template>
  <component
    :is="embedded ? 'div' : 'aside'"
    class="so-item-ops-root"
    :class="embedded ? 'so-item-ops-root--embedded' : 'so-item-ops-panel'"
    aria-label="arrival-notice-ops-panel"
  >
    <div v-if="!row" class="so-item-ops-root__empty">
      {{ t('arrivalNoticeList.opsPanel.pickRow') }}
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
          <h3 class="ops-card__title">{{ t('arrivalNoticeList.opsPanel.overviewTitle') }}</h3>
        </header>
        <div class="ops-card__body ops-card__body--overview">
          <div class="ops-overview-line ops-overview-line--hero">{{ noticeCode }}</div>
          <div class="ops-overview-line">
            <VendorNameReadonlyText
              :name-zh="vendorNameZh"
              :name-en="vendorNameEn"
              :masked="maskSensitive"
            />
          </div>
          <div class="ops-overview-line">{{ displayPn }}</div>
          <div class="ops-overview-line">{{ displayBrand }}</div>
          <div class="ops-overview-line">{{ expectedArrivalDateText }}</div>
          <div class="ops-overview-line">{{ formatQty(expectQty) }} pcs</div>
          <div class="ops-overview-line">
            <StockBizTypeTag
              biz="in"
              :type="stockInType"
              :customs-declaration-id="customsDeclarationId"
              :customs-declaration-code="customsDeclarationCode"
            />
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
          <h3 class="ops-card__title">{{ t('arrivalNoticeList.opsPanel.confirmArrivedTitle') }}</h3>
          <span v-if="confirmArrivedCompleted" class="ops-card__done">
            <el-icon class="ops-card__done-icon" aria-hidden="true"><CircleCheck /></el-icon>
            {{ t('arrivalNoticeList.opsPanel.completed') }}
          </span>
        </header>
        <div class="ops-card__body">
          <button
            v-if="canWriteLogistics && !confirmArrivedCompleted"
            type="button"
            class="ops-action-btn ops-action-btn--primary"
            :disabled="actionLoading"
            @click="emit('confirm-arrived')"
          >
            {{ t('arrivalNoticeList.actions.confirmArrived') }}
          </button>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('arrivalNoticeList.opsPanel.purchaseTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.purchaseItemCode') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">
                <router-link
                  v-if="purchaseLink && !maskSensitive"
                  :to="purchaseLink"
                  class="link-text"
                >
                  {{ purchaseItemCode }}
                </router-link>
                <span v-else>{{ purchaseItemCode }}</span>
              </span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.purchaseUser') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ purchaseUserName }}</span>
            </div>
          </div>
          <div class="ops-stock-region-row">
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.purchaseDate') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ purchaseOrderCreateDateText }}</span>
            </div>
            <div class="ops-stock-region-cell">
              <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.purchaseQty') }}</span>
              <span class="ops-kv__sep" aria-hidden="true">：</span>
              <span class="ops-kv__value">{{ formatQty(purchaseQty) }}</span>
            </div>
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('arrivalNoticeList.opsPanel.qcTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="qc">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.qcCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link
                    v-if="qcLink"
                    :to="qcLink"
                    class="link-text"
                  >
                    {{ qc.qcCode || '—' }}
                  </router-link>
                  <span v-else>{{ qc.qcCode || '—' }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.qcUser') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ qc.createUserName?.trim() || '—' }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.qcDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ qcCreateDateText }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.qcPassQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(qc.passQty) }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.qcRejectQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(qc.rejectQty) }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('arrivalNoticeList.opsPanel.noQc') }}</p>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('arrivalNoticeList.opsPanel.stockInTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <template v-if="stockIn">
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInCode') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">
                  <router-link
                    v-if="stockInLink"
                    :to="stockInLink"
                    class="link-text"
                  >
                    {{ stockIn.stockInCode || '—' }}
                  </router-link>
                  <span v-else>{{ stockIn.stockInCode || '—' }}</span>
                </span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInUser') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockIn.createUserName?.trim() || '—' }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInDate') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockInDateText }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInStatus') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockInStatusText }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInType') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockInTypeLabel }}</span>
              </div>
            </div>
            <div class="ops-stock-region-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInWarehouse') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ stockIn.warehouseName?.trim() || '—' }}</span>
              </div>
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('arrivalNoticeList.opsPanel.stockInQty') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ formatQty(stockIn.totalQuantity) }}</span>
              </div>
            </div>
          </template>
          <p v-else class="ops-status ops-status--info">{{ t('arrivalNoticeList.opsPanel.noStockIn') }}</p>
        </div>
      </section>
    </div>
  </component>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck } from '@element-plus/icons-vue'
import type { ArrivalNoticeOpsAggregatesDto } from '@/api/logistics'
import { formatDisplayDate } from '@/utils/displayDateTime'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import { StockInTypeCode } from '@/constants/stockInType'

const props = defineProps<{
  row: Record<string, unknown> | null
  aggregates: ArrivalNoticeOpsAggregatesDto | null
  loading?: boolean
  loadError?: string
  actionLoading?: boolean
  canWriteLogistics?: boolean
  maskSensitive?: boolean
  embedded?: boolean
}>()

const emit = defineEmits<{
  'confirm-arrived': []
}>()

const { t } = useI18n()

const noticeCode = computed(() => String(props.row?.noticeCode ?? props.row?.NoticeCode ?? '—') || '—')
const vendorNameZh = computed(() => String(props.row?.vendorName ?? props.row?.VendorName ?? '').trim())
const vendorNameEn = computed(() => String(props.row?.vendorEnglishName ?? props.row?.VendorEnglishName ?? '').trim())
const displayPn = computed(() => {
  const pn = props.row?.pn ?? props.row?.Pn
  return pn != null && String(pn).trim() ? String(pn).trim() : '—'
})
const displayBrand = computed(() => {
  const brand = props.row?.brand ?? props.row?.Brand
  return brand != null && String(brand).trim() ? String(brand).trim() : '—'
})
const expectQty = computed(() => Number(props.row?.expectQty ?? props.row?.ExpectQty ?? 0))
const stockInType = computed(() =>
  Number(props.row?.stockInType ?? props.row?.StockInType ?? StockInTypeCode.Purchase)
)
const customsDeclarationId = computed(() =>
  (props.row?.customsDeclarationId ?? props.row?.CustomsDeclarationId) as string | null | undefined
)
const customsDeclarationCode = computed(() =>
  (props.row?.customsDeclarationCode ?? props.row?.CustomsDeclarationCode) as string | null | undefined
)

const expectedArrivalDateText = computed(() => {
  const raw = props.row?.expectedArrivalDate ?? props.row?.ExpectedArrivalDate
  return raw ? formatDisplayDate(String(raw)) : '—'
})

const noticeStatus = computed(() => Number(props.row?.status ?? props.row?.Status ?? 0))

const displayNoticeStatus = computed(() => {
  if (stockIn.value?.status === 2 && noticeStatus.value < 100) return 100
  return noticeStatus.value
})

const statusLabel = computed(() => {
  const keyMap: Record<number, 'new' | 'notArrived' | 'pendingQc' | 'qcDone' | 'stocked'> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[displayNoticeStatus.value]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
})

const statusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = displayNoticeStatus.value
  if (s === 100 || s === 30) return 'success'
  if (s === 20) return 'info'
  if (s === 10) return 'warning'
  return 'info'
})

const confirmArrivedCompleted = computed(() => noticeStatus.value > 10)

const purchase = computed(() => props.aggregates?.purchase ?? null)
const qc = computed(() => props.aggregates?.qc ?? null)
const stockIn = computed(() => props.aggregates?.stockIn ?? null)

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

const stockInLink = computed(() => {
  const id = stockIn.value?.id?.trim()
  if (!id) return null
  return { name: 'StockInDetail', params: { id } }
})

const qcLink = computed(() => {
  const noticeId = String(props.row?.id ?? props.row?.Id ?? '').trim()
  const qcId = qc.value?.id?.trim()
  if (!noticeId || !qcId) return null
  return {
    name: 'QcCreate',
    query: { noticeId, qcId }
  }
})

const qcCreateDateText = computed(() => {
  const raw = qc.value?.createTime
  return raw ? formatDisplayDate(String(raw)) : '—'
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
  const type = stockIn.value?.stockInType ?? stockInType.value
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
