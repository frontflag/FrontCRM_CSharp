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
          </div>
        </div>
      </section>

      <section class="ops-card">
        <header class="ops-card__head">
          <h3 class="ops-card__title">{{ t('qcList.opsPanel.qcImagesTitle') }}</h3>
        </header>
        <div class="ops-card__body">
          <p v-if="qcImageCountNum <= 0" class="ops-status ops-status--info">
            {{ t('qcList.opsPanel.qcImagesEmpty') }}
          </p>
          <template v-else>
            <div class="ops-stock-region-row ops-qc-images-row">
              <div class="ops-stock-region-cell">
                <span class="ops-kv__label">{{ t('qcList.opsPanel.qcImages') }}</span>
                <span class="ops-kv__sep" aria-hidden="true">：</span>
                <span class="ops-kv__value">{{ qcImageCountText }}</span>
              </div>
              <el-button
                link
                type="primary"
                class="ops-qc-images-preview-btn"
                :disabled="previewLoading"
                @click="onTogglePreview"
              >
                {{
                  previewExpanded
                    ? t('qcList.opsPanel.qcImagesCollapse')
                    : t('qcList.opsPanel.qcImagesPreview')
                }}
              </el-button>
            </div>
            <p v-if="previewError" class="ops-status ops-status--warn">{{ previewError }}</p>
            <div v-if="previewExpanded" v-loading="previewLoading" class="ops-qc-images-gallery">
              <QcImagesReadonlyGallery
                v-if="!previewLoading"
                :images="previewImages"
                :empty-text="t('qcList.opsPanel.qcImagesEmpty')"
                :browser-title="t('qcList.opsPanel.qcImagesTitle')"
              />
            </div>
          </template>
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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CircleCheck } from '@element-plus/icons-vue'
import type { QcOpsAggregatesDto } from '@/api/logistics'
import {
  documentApi,
  DOCUMENT_BIZ_TYPE_QC,
  type QcImageReadonlyRow
} from '@/api/document'
import { formatDisplayDate } from '@/utils/displayDateTime'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import QcImagesReadonlyGallery from '@/components/Logistics/QcImagesReadonlyGallery.vue'
import { resolveStockInTypeLabelKey } from '@/constants/stockInType'
import { filterQcImageDocuments, resolveUploadDocumentId } from '@/utils/qcImageDocument'
import { getApiErrorMessage } from '@/utils/apiError'
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

const previewExpanded = ref(false)
const previewLoading = ref(false)
const previewError = ref('')
const previewImages = ref<QcImageReadonlyRow[]>([])
let previewSeq = 0

function resetPreview() {
  previewSeq += 1
  previewExpanded.value = false
  previewLoading.value = false
  previewError.value = ''
  previewImages.value = []
}

watch(
  () => String(props.row?.id ?? props.row?.Id ?? ''),
  () => {
    resetPreview()
  }
)

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

const qcImageCountNum = computed(() => {
  const fromProp = Number(props.qcImageCount)
  if (Number.isFinite(fromProp) && fromProp >= 0) return Math.floor(fromProp)
  const fromRow = Number(props.row?.qcImageCount ?? props.row?.QcImageCount ?? 0)
  if (Number.isFinite(fromRow) && fromRow >= 0) return Math.floor(fromRow)
  return 0
})

const qcImageCountText = computed(() =>
  t('qcList.opsPanel.qcImageCount', { count: qcImageCountNum.value })
)

async function onTogglePreview() {
  if (qcImageCountNum.value <= 0) return
  if (previewExpanded.value) {
    previewExpanded.value = false
    return
  }

  const id = String(props.row?.id ?? props.row?.Id ?? '').trim()
  if (!id) return

  const seq = ++previewSeq
  previewExpanded.value = true
  previewLoading.value = true
  previewError.value = ''
  previewImages.value = []

  try {
    const docs = await documentApi.getDocuments(DOCUMENT_BIZ_TYPE_QC, id)
    if (seq !== previewSeq) return
    const images = filterQcImageDocuments(Array.isArray(docs) ? docs : [])
    const qcCodeVal = String(props.row?.qcCode ?? props.row?.QcCode ?? '').trim()
    const notifyCode = String(props.row?.stockInNotifyCode ?? props.row?.StockInNotifyCode ?? '').trim()
    previewImages.value = images.map((d) => ({
      documentId: resolveUploadDocumentId(d),
      qcId: id,
      qcCode: qcCodeVal || null,
      stockInNotifyCode: notifyCode || null,
      originalFileName: d.originalFileName ?? null,
      mimeType: d.mimeType ?? null,
      fileExtension: d.fileExtension ?? null,
      createTime: d.createTime ?? ''
    }))
  } catch (e: unknown) {
    if (seq !== previewSeq) return
    previewError.value = getApiErrorMessage(e, t('qcList.opsPanel.qcImagesLoadFailed'))
    previewImages.value = []
  } finally {
    if (seq === previewSeq) previewLoading.value = false
  }
}

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
const arrivalStockInType = computed(() => {
  const raw = arrivalNotice.value?.stockInType
  if (raw == null) return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
})
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
  return t(`stockInList.stockInTypeLabels.${resolveStockInTypeLabelKey(type)}`)
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

.ops-qc-images-row {
  align-items: center;
}

.ops-qc-images-row .ops-stock-region-cell {
  flex: 1 1 auto;
  min-width: 0;
}

.ops-qc-images-preview-btn {
  flex: 0 0 auto;
  margin-left: auto;
  padding: 0;
  height: auto;
  font-size: 13px;
}

.ops-qc-images-gallery {
  margin-top: 10px;
  min-height: 48px;
}

.ops-qc-images-gallery :deep(.qc-images-readonly) {
  padding: 4px 0 0;
}

.ops-qc-images-gallery :deep(.qc-images-readonly__group-head) {
  display: none;
}

.ops-qc-images-gallery :deep(.qc-images-readonly__thumb) {
  width: 72px;
  height: 72px;
}
</style>
