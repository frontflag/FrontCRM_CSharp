<template>
  <div
    class="customs-declaration-detail-page"
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
        <div v-if="detail" class="customs-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">
                  {{ t('customsPages.declarations.captionPrefix') }} {{ detail.declarationCode || '—' }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption customs-header-meta-row">
              <el-tag effect="dark" :type="internalStatusTagType" size="small">
                {{ internalLabel(detail.internalStatus) }}
              </el-tag>
              <el-tag effect="dark" :type="clearanceStatusTagType" size="small">
                {{ clearanceLabel(detail.customsClearanceStatus) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div v-if="detail" class="header-right">
        <el-button
          v-if="canWriteLogistics && detail.canCreateArrivalNotifies"
          type="primary"
          :loading="creatingArrival"
          @click="handleCreateArrivalNotifies"
        >
          {{ t('customsPages.declarations.createArrivalNotifies') }}
        </el-button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <el-alert
          v-if="detail.arrivalNotifyBlockReason && !detail.canCreateArrivalNotifies && detail.existingArrivalNotifyCount === 0"
          type="info"
          :closable="false"
          show-icon
          class="arrival-hint"
          :title="detail.arrivalNotifyBlockReason"
        />
        <el-alert
          v-else-if="detail.existingArrivalNotifyCodes?.length"
          type="success"
          :closable="false"
          show-icon
          class="arrival-hint"
          :title="t('customsPages.declarations.existingArrivalNotifies', { codes: detail.existingArrivalNotifyCodes.join('、') })"
        />

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('customsPages.declarations.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('customsPages.declarations.colCreateTime') }}</span>
                <span class="section-header-meta-item__value">{{ createDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('customsPages.declarations.colCreator') }}</span>
                <span class="section-header-meta-item__value">{{ createUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colDecCode') }}</span>
              <span class="info-value">{{ detail.declarationCode || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colBroker') }}</span>
              <span class="info-value">{{ brokerDisplay }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colDeclareDate') }}</span>
              <span class="info-value">{{ formatDate(detail.declareDate) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colExchangeRate') }}</span>
              <span class="info-value">{{ moneyText(detail.exchangeRate) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colTotal') }}</span>
              <span class="info-value">{{ moneyText(detail.totalTaxAmount) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colWarehouseRoute') }}</span>
              <span class="info-value">{{ warehouseRoute }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colCustomsPacking') }}</span>
              <span class="info-value">
                <router-link
                  v-if="detail.packingId"
                  :to="{ name: 'PackingDetail', params: { id: detail.packingId } }"
                  class="link-text"
                >
                  {{ detail.packingCode || detail.packingId }}
                </router-link>
                <span v-else>—</span>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('customsPages.declarations.colSor') }}</span>
              <span class="info-value">
                <router-link
                  v-if="detail.stockOutRequestId"
                  :to="{ name: 'StockOutNotifyDetail', params: { id: detail.stockOutRequestId } }"
                  class="link-text"
                >
                  {{ detail.stockOutRequestCode || detail.stockOutRequestId }}
                </router-link>
                <span v-else>—</span>
              </span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div v-if="detail.remark" class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('customsPages.declarations.colRemark') }}</span>
              <span class="info-value">{{ detail.remark }}</span>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('customsPages.declarations.sectionItems') }}</span>
              <span v-if="detail.items?.length" class="section-count">{{ detail.items.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div v-if="detail.items?.length" class="detail-items-table-wrap">
              <el-table :data="detail.items" size="small" border class="detail-panel-list-table">
                <el-table-column prop="lineNo" label="#" width="56" align="center" />
                <el-table-column prop="purchasePn" :label="t('customsPages.items.colPn')" min-width="120" show-overflow-tooltip />
                <el-table-column prop="purchaseBrand" :label="t('customsPages.items.colBrand')" width="96" show-overflow-tooltip />
                <el-table-column prop="hsCode" :label="t('customsPages.items.colHs')" width="100" show-overflow-tooltip />
                <el-table-column prop="declareQty" :label="t('customsPages.items.colQty')" width="90" align="right" />
                <el-table-column :label="t('customsPages.items.colCustomer')" min-width="120" show-overflow-tooltip>
                  <template #default="{ row }">{{ maskSale ? '—' : row.customerName || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('stockInDetail.sellOrderItemCode')" min-width="120" show-overflow-tooltip>
                  <template #default="{ row }">{{ maskSale ? '—' : row.sellOrderItemCode || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('stockInDetail.vendor')" min-width="120" show-overflow-tooltip>
                  <template #default="{ row }">{{ formatVendorNameReadonly(row.vendorName, row.vendorEnglishName, { masked: maskPurchase }) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colUnitPrice')" width="100" align="right">
                  <template #default="{ row }">{{ moneyText(row.declareUnitPrice) }}</template>
                </el-table-column>
                <el-table-column :label="t('stockInDetail.originalPrice')" width="100" align="right">
                  <template #default="{ row }">{{ unitPriceText(row.originalPurchasePrice) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colTaxUnit')" width="100" align="right">
                  <template #default="{ row }">{{ unitPriceText(row.taxIncludedUnitPrice) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colDuty')" width="90" align="right">
                  <template #default="{ row }">{{ moneyText(row.dutyAmount) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colVat')" width="90" align="right">
                  <template #default="{ row }">{{ moneyText(row.vatAmount) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colGoods')" width="100" align="right">
                  <template #default="{ row }">{{ moneyText(row.customsPaymentGoods) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colAgency')" width="100" align="right">
                  <template #default="{ row }">{{ moneyText(row.customsAgencyFee) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colOther')" width="80" align="right">
                  <template #default="{ row }">{{ moneyText(row.otherFee) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colInspection')" width="90" align="right">
                  <template #default="{ row }">{{ moneyText(row.inspectionFee) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.items.colTotalTax')" width="100" align="right">
                  <template #default="{ row }">{{ moneyText(row.totalValueTax) }}</template>
                </el-table-column>
                <el-table-column :label="t('customsPages.declarations.colArrivalNotify')" min-width="120" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.arrivalNotifyCode || '—' }}</template>
                </el-table-column>
              </el-table>
            </div>
            <DetailListPanelEmpty v-else size="low" />
          </div>
        </div>

        <div class="tabs-section">
          <div class="section-header section-header--tabs">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('customsPages.declarations.sectionBusinessRecords') }}</span>
            </div>
          </div>
          <CustomsDeclarationBusinessRecordsPanel :key="detail.id" :declaration-id="detail.id" />
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="loadError || t('stockOutDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import CustomsDeclarationBusinessRecordsPanel from '@/components/Customs/CustomsDeclarationBusinessRecordsPanel.vue'
import {
  createCustomsArrivalNotifies,
  fetchCustomsDeclarationById,
  type CustomsDeclarationDetailDto
} from '@/api/customs'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { canWriteLogisticsData: canWriteLogistics } = useDepartmentDataReadOnly()
const { maskPurchaseSensitiveFields: maskPurchase } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields: maskSale } = useSaleSensitiveFieldMask()
const loading = ref(false)
const creatingArrival = ref(false)
const loadError = ref('')
const detail = ref<CustomsDeclarationDetailDto | null>(null)

const captionAvatarChar = computed(() => {
  const code = detail.value?.declarationCode?.trim()
  return code ? code.slice(-1).toUpperCase() : '报'
})

const brokerDisplay = computed(() => {
  const d = detail.value
  if (!d) return '—'
  return d.customsBrokerName || d.customsBrokerCode || d.customsBrokerId || '—'
})

const warehouseRoute = computed(() => {
  const d = detail.value
  if (!d) return '—'
  const from = (d.fromWarehouseCode ?? d.fromWarehouseId ?? '').trim()
  const to = (d.toWarehouseCode ?? d.toWarehouseId ?? '').trim()
  if (from && to) return `${from} → ${to}`
  return from || to || '—'
})

const createDateText = computed(() => {
  const iso = detail.value?.createTime
  if (!iso) return '—'
  return formatDateTimeZh(iso, 'YYYY-MM-DD')
})

const createUserText = computed(() => detail.value?.createUserDisplay?.trim() || '—')

const internalStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = detail.value?.internalStatus
  if (s === 3) return 'success'
  if (s === 2) return 'warning'
  if (s === -1) return 'danger'
  return 'info'
})

const clearanceStatusTagType = computed((): '' | 'success' | 'warning' | 'info' | 'danger' => {
  const s = detail.value?.customsClearanceStatus
  if (s === 100) return 'success'
  if (s === 10) return 'warning'
  return 'info'
})

function internalLabel(v: number) {
  if (v === -1) return t('customsPages.declarations.internalVoid')
  const m: Record<number, string> = {
    1: t('customsPages.declarations.internalPending'),
    2: t('customsPages.declarations.internalProcessing'),
    3: t('customsPages.declarations.internalDone')
  }
  return m[v] ?? String(v)
}

function clearanceLabel(v: number) {
  const m: Record<number, string> = {
    0: t('customsPages.declarations.clearanceNone'),
    10: t('customsPages.declarations.clearanceReleased'),
    100: t('customsPages.declarations.clearanceCleared')
  }
  return m[v] ?? String(v)
}

function formatDate(iso: string | undefined) {
  if (!iso) return '—'
  return iso.includes('T') ? iso.slice(0, 10) : iso.slice(0, 10)
}

function moneyText(n: number | null | undefined): string {
  if (maskPurchase.value) return '—'
  const x = Number(n)
  if (!Number.isFinite(x) || x <= 0) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function unitPriceText(n: number | null | undefined): string {
  if (maskPurchase.value) return '—'
  const x = Number(n)
  if (!Number.isFinite(x) || x <= 0) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

function goBack() {
  router.push({ name: 'CustomsDeclarationList' })
}

async function handleCreateArrivalNotifies() {
  const id = detail.value?.id
  if (!id) return
  try {
    await ElMessageBox.confirm(
      t('customsPages.declarations.createArrivalConfirm'),
      t('customsPages.declarations.createArrivalNotifies'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  creatingArrival.value = true
  try {
    const result = await createCustomsArrivalNotifies(id)
    const codes = result.created?.map((c) => c.noticeCode).filter(Boolean).join('、')
    ElMessage.success(
      codes
        ? t('customsPages.declarations.createArrivalOkWithCodes', { codes })
        : t('customsPages.declarations.createArrivalOk', { count: result.createdCount })
    )
    await load()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    creatingArrival.value = false
  }
}

async function load() {
  const id = typeof route.params.id === 'string' ? route.params.id.trim() : ''
  if (!id) {
    loadError.value = t('stockOutDetail.notFound')
    return
  }
  loading.value = true
  loadError.value = ''
  try {
    detail.value = await fetchCustomsDeclarationById(id)
  } catch (e: unknown) {
    detail.value = null
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

.customs-declaration-detail-page {
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

.header-right {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
  flex-wrap: wrap;
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

.customs-caption-title-group {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  font-weight: 700;
  color: $cyan-primary;
  background: rgba(0, 212, 255, 0.12);
  border: 1px solid rgba(0, 212, 255, 0.25);
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

.customs-header-meta-row {
  min-height: 28px;
}

.detail-content {
  min-height: 200px;
}

.arrival-hint {
  margin-bottom: 12px;
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

  &--tabs {
    border-bottom: none;
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

.section-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
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
}

.detail-panel-section-body {
  padding: 0;
}

.detail-items-table-wrap {
  margin-top: 0;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
  margin-bottom: 16px;
}

.link-text {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
