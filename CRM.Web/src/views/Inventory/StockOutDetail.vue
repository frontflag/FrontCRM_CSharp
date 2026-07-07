<template>
  <div class="stockout-detail-page" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('stockOutDetail.back') }}
        </button>
        <div v-if="detail" class="stockout-caption-title-group">
          <div class="caption-avatar-lg">{{ stockOutCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': detail.status === 3 }">
                  {{ t('stockOutDetail.captionPrefix') }} {{ detail.stockOutCode || '—' }}
                </h1>
                <el-tooltip
                  v-if="isCustomsStockOut && salesNotifyTooltip"
                  :content="salesNotifyTooltip"
                  placement="top"
                  :hide-after="0"
                >
                  <span class="customs-notify-tag">{{ t('stockOutList.customsNotifyTag') }}</span>
                </el-tooltip>
              </div>
            </div>
            <div class="title-meta title-meta--caption stockout-header-meta-row">
              <el-tag effect="dark" :type="stockOutStatusTagType" size="small">
                {{ statusLabel(detail.status) }}
              </el-tag>
              <StockBizTypeTag
                biz="out"
                :type="detail.stockOutType"
                :customs-declaration-id="detail.customsDeclarationId"
                :customs-declaration-code="detail.customsDeclarationCode"
              />
            </div>
          </div>
        </div>
      </div>
      <div v-if="detail" class="header-right">
        <button type="button" class="btn-secondary" @click="goInvoiceReport">
          {{ t('stockOutDetail.printInvoice') }}
        </button>
        <button type="button" class="btn-primary" :disabled="saving" @click="saveHeader">
          {{ saving ? t('stockOutDetail.saving') : t('stockOutDetail.save') }}
        </button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('stockOutDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ stockOutBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('stockOutDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ stockOutBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.sourceCode') }}</span>
              <span class="info-value">{{ reportCellText(detail.sourceCode) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.warehouseName') }}</span>
              <span class="info-value">{{ detailWarehouseNameText }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.totalQuantity') }}</span>
              <span class="info-value">{{ formatNum(detail.totalQuantity) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.customerName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : reportCellText(detail.customerName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.salesUserName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : reportCellText(detail.salesUserName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.sellOrderItemCode') }}</span>
              <span class="info-value">{{ reportCellText(detail.sellOrderItemCode) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div v-if="detailSalesNotifyId && detailSalesNotifyCode" class="info-item">
              <span class="info-label">{{ t('stockOutList.salesNotifyCodeLink') }}</span>
              <span class="info-value">
                <router-link :to="{ name: 'StockOutNotifyDetail', params: { id: detailSalesNotifyId } }" class="cell-link">
                  {{ detailSalesNotifyCode }}
                </router-link>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.expectedStockOutDate') }}</span>
              <span class="info-value info-value--time">{{ expectedStockOutDateText }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.packingCodes') }}</span>
              <span class="info-value">{{ reportCellText(detail.packingCodes) }}</span>
            </div>
            <div
              v-if="!(detailSalesNotifyId && detailSalesNotifyCode)"
              class="info-item info-item--basic-spacer"
              aria-hidden="true"
            ></div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('stockOutList.columns.remark') }}</span>
              <span class="info-value">{{ reportCellText(detail.remark) }}</span>
            </div>
          </div>
        </div>

        <StockOutCustomsSummaryPanel v-if="detail.customsSummary?.declarationId" :summary="detail.customsSummary" />

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('stockOutDetail.sectionEditable') }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic info-grid--editable">
            <div class="info-item">
              <span class="info-label">{{ t('stockOutList.columns.stockOutDate') }}</span>
              <span class="info-value info-value--control">
                <el-date-picker
                  v-model="editForm.stockOutDate"
                  type="date"
                  value-format="YYYY-MM-DD"
                  :placeholder="t('stockOutDetail.pickDate')"
                  style="width: 100%"
                />
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.shipmentMethod') }}</span>
              <span class="info-value info-value--control">
                <el-select
                  v-model="editForm.shipmentMethod"
                  clearable
                  filterable
                  :placeholder="t('stockOutDetail.shipmentPlaceholder')"
                  style="width: 100%"
                >
                  <el-option v-for="o in shipmentMethodOptions" :key="o.value" :label="o.label" :value="o.value" />
                </el-select>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('stockOutDetail.courierTrackingNo') }}</span>
              <span class="info-value info-value--control">
                <el-input
                  v-model="editForm.courierTrackingNo"
                  clearable
                  :placeholder="t('stockOutDetail.trackingPlaceholder')"
                />
              </span>
            </div>
          </div>
        </div>

        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'items' }"
              @click="detailActiveTab = 'items'"
            >
              {{ t('stockOutDetail.tabs.items') }}
              <span v-if="stockOutItems.length" class="tab-count">{{ stockOutItems.length }}</span>
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'documents' }"
              @click="detailActiveTab = 'documents'"
            >
              {{ t('stockOutDetail.tabs.documents') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="detailActiveTab === 'items'" v-loading="itemsLoading" class="detail-items-table-wrap">
              <el-empty v-if="!itemsLoading && !stockOutItems.length" :description="t('stockOutDetail.noItems')" :image-size="80" />
              <CrmDataTable
                v-else-if="stockOutItems.length"
                :data="stockOutItems"
                class="items-table detail-panel-list-table"
                size="small"
                stripe
                row-key="stockOutItemId"
              >
                <el-table-column type="index" width="50" align="center" />
                <el-table-column
                  prop="purchasePn"
                  :label="t('stockOutItemList.columns.purchasePn')"
                  min-width="140"
                  show-overflow-tooltip
                />
                <el-table-column
                  prop="purchaseBrand"
                  :label="t('stockOutItemList.columns.purchaseBrand')"
                  width="100"
                  show-overflow-tooltip
                />
                <el-table-column
                  :label="t('stockOutItemList.columns.outQuantity')"
                  width="100"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">{{ formatNum(row.outQuantity) }}</template>
                </el-table-column>
                <el-table-column
                  prop="stockInCode"
                  :label="t('stockOutItemList.columns.stockInCode')"
                  min-width="130"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.stockInCode || '—' }}</template>
                </el-table-column>
                <el-table-column
                  prop="packingCode"
                  :label="t('stockOutItemList.columns.packingCode')"
                  min-width="130"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.packingCode || '—' }}</template>
                </el-table-column>
                <el-table-column
                  prop="sellOrderItemCode"
                  :label="t('stockOutItemList.columns.sellOrderItemCode')"
                  min-width="150"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.sellOrderItemCode || '—' }}</template>
                </el-table-column>
              </CrmDataTable>
            </div>
            <div v-show="detailActiveTab === 'documents'">
              <p class="doc-hint">{{ t('stockOutDetail.docHint') }}</p>
              <DocumentUploadPanel
                :biz-type="DOC_BIZ"
                :biz-id="detail.id"
                :max-files="20"
                :max-size-mb="100"
                @uploaded="docListRef?.refresh()"
              />
              <DocumentListPanel
                ref="docListRef"
                :biz-type="DOC_BIZ"
                :biz-id="detail.id"
                view-mode="list"
                style="margin-top: 16px"
              />
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="loadError || t('stockOutDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { stockOutApi, type StockOutDetailDto, type StockOutItemListRow } from '@/api/stockOut'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { StockOutTypeCode } from '@/constants/stockOutType'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import StockOutCustomsSummaryPanel from '@/components/Customs/StockOutCustomsSummaryPanel.vue'
import { formatDisplayDate } from '@/utils/displayDateTime'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const DOC_BIZ = 'STOCK_OUT'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions: shipmentMethodOptions } = useLogisticsFormDict()

const loading = ref(false)
const saving = ref(false)
const itemsLoading = ref(false)
const loadError = ref('')
const detail = ref<StockOutDetailDto | null>(null)
const stockOutItems = ref<StockOutItemListRow[]>([])
const detailActiveTab = ref<'items' | 'documents'>('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

const stockOutId = computed(() => {
  const raw = route.params.id
  if (Array.isArray(raw)) return String(raw[0] ?? '').trim()
  return String(raw ?? '').trim()
})

const editForm = ref({
  stockOutDate: '' as string,
  shipmentMethod: '' as string,
  courierTrackingNo: '' as string
})

const stockOutCaptionAvatarChar = computed(() => {
  const c = detail.value?.stockOutCode?.trim()
  return c ? c[0]! : '出'
})

const stockOutBasicCreateDateText = computed(() => {
  const raw = detail.value?.createTime
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

const stockOutBasicCreateUserText = computed(() => detail.value?.createUserName?.trim() || '—')

const detailWarehouseNameText = computed(() => {
  const name = detail.value?.warehouseName?.trim()
  if (name) return name
  const code = detail.value?.warehouseCode?.trim()
  return code || '—'
})

const stockOutStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' | 'primary' => {
  const s = detail.value?.status
  if (s === 0) return 'info'
  if (s === 1) return 'warning'
  if (s === 2) return 'success'
  if (s === 3) return 'danger'
  if (s === 4) return 'primary'
  return 'info'
})

const expectedStockOutDateText = computed(() => {
  const raw = detail.value?.expectedStockOutDate
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

function toDateOnly(iso?: string) {
  if (!iso) return ''
  const s = String(iso)
  return s.length >= 10 ? s.slice(0, 10) : s
}

function syncEditFromDetail(d: StockOutDetailDto) {
  editForm.value = {
    stockOutDate: toDateOnly(d.stockOutDate),
    shipmentMethod: d.shipmentMethod ?? '',
    courierTrackingNo: d.courierTrackingNo ?? ''
  }
}

watch(detail, (d) => {
  if (d) syncEditFromDetail(d)
})

function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

const formatNum = (v: number) => (v == null ? '—' : Number(v).toLocaleString())

const isCustomsStockOut = computed(() => Number(detail.value?.stockOutType) === StockOutTypeCode.Customs)

const detailSalesNotifyId = computed(() => String(detail.value?.salesStockOutNotifyId ?? '').trim())
const detailSalesNotifyCode = computed(() => String(detail.value?.salesStockOutNotifyCode ?? '').trim())

const salesNotifyTooltip = computed(() => {
  const code = detailSalesNotifyCode.value
  if (!code) return ''
  return t('stockOutList.salesNotifyCodeTooltip', { code })
})

const statusLabel = (s: number) => {
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
      return String(s)
  }
}

async function loadItems() {
  const code = detail.value?.stockOutCode?.trim()
  if (!code) {
    stockOutItems.value = []
    return
  }
  itemsLoading.value = true
  try {
    stockOutItems.value = await stockOutApi.searchItems({ stockOutCode: code })
  } catch {
    stockOutItems.value = []
  } finally {
    itemsLoading.value = false
  }
}

async function load() {
  const id = stockOutId.value
  if (!id) {
    loadError.value = t('stockOutDetail.notFound')
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    await ensureLogisticsDict()
    const d = await stockOutApi.getById(id)
    if (!d) {
      detail.value = null
      loadError.value = t('stockOutDetail.notFound')
      return
    }
    detail.value = d
    syncEditFromDetail(d)
    await loadItems()
  } catch {
    detail.value = null
    loadError.value = t('stockOutDetail.loadFailed')
  } finally {
    loading.value = false
  }
}

async function saveHeader() {
  const id = stockOutId.value
  const d = detail.value
  if (!id || !d) return
  if (!editForm.value.stockOutDate) {
    ElMessage.warning(t('stockOutDetail.needDate'))
    return
  }
  saving.value = true
  try {
    const dateIso = `${editForm.value.stockOutDate}T00:00:00.000Z`
    await stockOutApi.updateHeader(id, {
      stockOutDate: dateIso,
      shipmentMethod: editForm.value.shipmentMethod?.trim() || null,
      courierTrackingNo: editForm.value.courierTrackingNo?.trim() || null
    })
    ElMessage.success(t('stockOutDetail.saveOk'))
    await load()
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || t('stockOutDetail.saveFail'))
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push({ name: 'StockOutList' })
}

function goInvoiceReport() {
  const id = stockOutId.value
  if (!id) return
  router.push({ name: 'StockOutInvoiceReport', params: { id } })
}

onMounted(() => void load())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.stockout-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
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

.stockout-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-left {
  min-width: 0;
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

.info-section__body {
  padding: 16px 20px 20px;
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

    &::after {
      content: '：';
    }
  }

  &__value {
    color: $text-secondary;
  }
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
    font-size: 12px;

    &::after {
      content: '：';
    }
  }

  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
}

.info-grid--basic {
  .info-item {
    &:nth-child(3n) {
      border-right: none;
    }
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;

  &--time {
    font-size: 12px;
    color: $text-muted;
  }

  &--control {
    :deep(.el-input),
    :deep(.el-select),
    :deep(.el-date-editor) {
      width: 100%;
    }
  }
}

.info-grid--editable .info-item {
  align-items: center;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: var(--crm-detail-section-header-bg);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
  display: inline-flex;
  align-items: center;
  gap: 6px;

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tab-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
}

.btn-secondary,
.btn-primary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
}

.btn-secondary {
  border: 1px solid $border-panel;
  color: $text-secondary;
  background: rgba(255, 255, 255, 0.05);
}

.btn-primary {
  border: none;
  background: linear-gradient(135deg, #00a8cc, #0066cc);
  color: #fff;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.doc-hint {
  font-size: 12px;
  color: $text-muted;
  margin: 0 0 12px;
}

.customs-notify-tag {
  flex: 0 0 auto;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.4;
  color: #ffb84d;
  background: rgba(255, 184, 77, 0.14);
  border: 1px solid rgba(255, 184, 77, 0.45);
  cursor: default;
  user-select: none;
}

.cell-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
