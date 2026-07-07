<template>
  <div
    class="stock-out-notify-detail-page"
    v-loading="loading"
    element-loading-background="rgba(10,22,40,0.8)"
  >
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('stockOutDetail.back') }}
        </button>
        <div v-if="request" class="notify-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1
                  class="page-title"
                  :class="{ 'page-title--muted': Number(request.status) === STOCK_OUT_REQUEST_STATUS.Cancelled }"
                >
                  {{ t('stockOutNotifyList.captionPrefix') }} {{ requestCodeDisplay }}
                </h1>
                <el-tooltip
                  v-if="isCustomsNotify && salesNotifyTooltip"
                  :content="salesNotifyTooltip"
                  placement="top"
                  :hide-after="0"
                >
                  <span class="customs-notify-tag">{{ t('stockOutNotifyList.customsNotifyTag') }}</span>
                </el-tooltip>
              </div>
            </div>
            <div class="title-meta title-meta--caption notify-header-meta-row">
              <el-tag effect="dark" :type="notifyStatusTagType" size="small">
                {{ notifyStatusLabel }}
              </el-tag>
              <StockBizTypeTag
                biz="out"
                :type="request.stockOutType"
                :customs-declaration-id="request.customsDeclarationId"
                :customs-declaration-code="request.customsDeclarationCode"
              />
              <el-tag v-if="customsStatusLabel !== '—'" effect="dark" :type="customsStatusTagType" size="small">
                {{ customsStatusLabel }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="request">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutNotifyList.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('stockOutNotifyList.columns.createTime') }}</span>
                <span class="section-header-meta-item__value">{{ createTimeText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('stockOutNotifyList.columns.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ createUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.materialModel') }}</span>
              <span class="info-value">{{ cellText(request.materialModel) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.brand') }}</span>
              <span class="info-value">{{ cellText(request.brand) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.outQuantity') }}</span>
              <span class="info-value">{{ formatQty(request.outQuantity) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.regionType') }}</span>
              <span class="info-value">{{ regionLabel }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.requestDate') }}</span>
              <span class="info-value info-value--time">{{ requestDateText }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.salesOrderCode') }}</span>
              <span class="info-value">
                <router-link
                  v-if="request.salesOrderId?.trim() && request.salesOrderCode?.trim()"
                  class="link-text"
                  :to="{ name: 'SalesOrderDetail', params: { id: request.salesOrderId.trim() } }"
                >
                  {{ request.salesOrderCode.trim() }}
                </router-link>
                <span v-else>{{ cellText(request.salesOrderCode) }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.customer') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : cellText(request.customerName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.columns.salesUserName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : cellText(request.salesUserName) }}</span>
            </div>
            <div v-if="isCustomsNotify && salesNotifyId" class="info-item">
              <span class="info-label">{{ t('stockOutNotifyList.salesNotifyCodeLink') }}</span>
              <span class="info-value">
                <router-link
                  class="link-text"
                  :to="{ name: 'StockOutNotifyDetail', params: { id: salesNotifyId } }"
                >
                  {{ salesNotifyCode || salesNotifyId }}
                </router-link>
              </span>
            </div>
            <div
              v-if="!(isCustomsNotify && salesNotifyId)"
              class="info-item info-item--basic-spacer"
              aria-hidden="true"
            ></div>
          </div>
          <div v-if="request.remark?.trim()" class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('stockOutNotifyList.columns.remark') }}</span>
              <span class="info-value">{{ request.remark.trim() }}</span>
            </div>
          </div>
        </div>

        <StockOutCustomsSummaryPanel v-if="notifyCustomsSummary" :summary="notifyCustomsSummary" />

        <div class="tabs-section">
          <div class="section-header section-header--tabs">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutNotifyList.detail.sectionRelated') }}</span>
            </div>
          </div>
          <StockOutNotifyDetailTabs :key="request.id" :request="request" />
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="loadError || t('stockOutNotifyList.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import StockOutNotifyDetailTabs from '@/components/Inventory/StockOutNotifyDetailTabs.vue'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import StockOutCustomsSummaryPanel from '@/components/Customs/StockOutCustomsSummaryPanel.vue'
import { stockOutApi, type StockOutCustomsSummaryDto, type StockOutRequestDto } from '@/api/stockOut'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { STOCK_OUT_NOTIFY_CUSTOMS_STATUS } from '@/constants/stockOutNotifyCustomsStatus'
import { StockOutTypeCode } from '@/constants/stockOutType'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const loading = ref(false)
const loadError = ref('')
const request = ref<StockOutRequestDto | null>(null)

const captionAvatarChar = computed(() => {
  const code = request.value?.requestCode?.trim()
  return code ? code.slice(-1).toUpperCase() : '出'
})

const requestCodeDisplay = computed(() => {
  const code = request.value?.requestCode?.trim()
  if (code) return code
  const id = typeof route.params.id === 'string' ? route.params.id.trim() : ''
  return id || '—'
})

const isCustomsNotify = computed(
  () => Number(request.value?.stockOutType ?? StockOutTypeCode.Sales) === StockOutTypeCode.Customs
)

const notifyCustomsSummary = computed((): StockOutCustomsSummaryDto | null => {
  const r = request.value
  const declarationId = String(r?.customsDeclarationId ?? '').trim()
  if (!declarationId) return null
  return {
    declarationId,
    declarationCode: String(r?.customsDeclarationCode ?? '').trim() || declarationId,
    customsBrokerName: String(r?.customsBrokerName ?? '').trim() || null
  }
})

const salesNotifyId = computed(() => String(request.value?.salesStockOutNotifyId ?? '').trim())
const salesNotifyCode = computed(() => String(request.value?.salesStockOutNotifyCode ?? '').trim())

const salesNotifyTooltip = computed(() => {
  const code = salesNotifyCode.value
  if (!code) return ''
  return t('stockOutNotifyList.salesNotifyCodeTooltip', { code })
})

const notifyStatusLabel = computed(() => {
  const s = Number(request.value?.status)
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
})

const notifyStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = Number(request.value?.status)
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return 'warning'
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return 'info'
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return 'warning'
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return 'success'
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return 'info'
  return 'info'
})

const customsStatusLabel = computed(() => {
  const n = Number(request.value?.customsStatus ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.NotRequired) return t('stockOutNotifyList.customsStatus.notRequired')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) return t('stockOutNotifyList.customsStatus.pendingCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms) return t('stockOutNotifyList.customsStatus.inCustoms')
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return t('stockOutNotifyList.customsStatus.completed')
  return '—'
})

const customsStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const n = Number(request.value?.customsStatus ?? 0)
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.Completed) return 'success'
  if (n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.InCustoms || n === STOCK_OUT_NOTIFY_CUSTOMS_STATUS.PendingCustoms) {
    return 'warning'
  }
  return 'info'
})

const regionLabel = computed(() => {
  const n = normalizeRegionType(request.value?.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
})

const requestDateText = computed(() => {
  const v = request.value?.requestDate
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
})

const createTimeText = computed(() => {
  const v = request.value?.createTime
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD')
})

const createUserText = computed(() => {
  const r = request.value as (StockOutRequestDto & { createUserName?: string }) | null
  return r?.createUserName?.trim() || r?.requestUserName?.trim() || '—'
})

function cellText(v?: string | null) {
  const s = (v ?? '').trim()
  return s || '—'
}

function formatQty(n: number | undefined | null) {
  if (n == null || (typeof n === 'number' && Number.isNaN(n))) return '—'
  const v = Number(n)
  if (Number.isNaN(v)) return '—'
  return Number.isInteger(v) ? `${v}` : `${+v.toFixed(4)}`.replace(/\.?0+$/, '')
}

function goBack() {
  router.push({ name: 'InventoryStockOutNotifyList' })
}

async function load() {
  const id = typeof route.params.id === 'string' ? route.params.id.trim() : ''
  if (!id) {
    loadError.value = t('stockOutNotifyList.notFound')
    request.value = null
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    const p = await stockOutApi.getRequestListPaged({ page: 1, pageSize: 2000 })
    request.value =
      p.items.find((x) => x.id === id || x.id?.toLowerCase?.() === id.toLowerCase()) ?? null
    if (!request.value) loadError.value = t('stockOutNotifyList.notFound')
  } catch (e: unknown) {
    request.value = null
    loadError.value = e instanceof Error ? e.message : String(e)
    ElMessage.error(loadError.value)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stock-out-notify-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  min-width: 0;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.04);
  color: $text-secondary;
  font-size: 13px;
  cursor: pointer;
  flex-shrink: 0;

  &:hover {
    color: $text-primary;
    border-color: rgba(0, 212, 255, 0.35);
  }
}

.notify-caption-title-group {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  background: rgba(0, 212, 255, 0.12);
  border: 1px solid rgba(0, 212, 255, 0.28);
  color: $cyan-primary;
  font-size: 16px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;

  &--muted {
    opacity: 0.55;
  }
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.title-meta--caption {
  margin-top: 4px;
}

.notify-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.customs-notify-tag {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  color: #ffc107;
  background: rgba(255, 193, 7, 0.12);
  border: 1px solid rgba(255, 193, 7, 0.35);
}

.detail-content {
  min-height: 200px;
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);

  .section-title {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
  }
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;

  &__label {
    color: $text-muted;
  }

  &__value {
    color: $text-secondary;
  }
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 8px rgba(0, 212, 255, 0.45);
  }
}

.info-grid--inline-labels {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0;
  padding: 0 20px 16px;

  &.info-grid--basic {
    padding-top: 16px;
    padding-bottom: 0;
  }

  .info-item {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 12px 16px 12px 0;
    border-right: 1px solid rgba(255, 255, 255, 0.04);
    min-width: 0;

    &:nth-child(3n) {
      border-right: none;
      padding-right: 0;
    }
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
  padding-bottom: 16px;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;

  &--time {
    font-variant-numeric: tabular-nums;
  }
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
  margin-bottom: 16px;
}

.section-header--tabs {
  margin-bottom: 0;
}

.link-text {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
