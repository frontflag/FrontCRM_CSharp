<template>
  <div
    class="packing-detail-page"
    v-loading="loading"
    element-loading-background="rgba(10,22,40,0.8)"
  >
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('packingDetail.back') }}
        </button>
        <div v-if="detail" class="packing-caption-title-group">
          <div class="caption-avatar-lg">{{ packingCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">
                  {{ t('packingDetail.captionPrefix') }} {{ detail.code || '—' }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption packing-header-meta-row">
              <el-tag effect="dark" :type="packingStatusTagType(detail.status)" size="small">
                {{ packingStatusLabel(detail.status) }}
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
      <div v-if="detail && canRefreshPackingStatus" class="header-right">
        <button
          class="btn-secondary"
          type="button"
          :disabled="refreshingStatus"
          @click="handleRefreshStatus"
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0 1 14.13-3.36L23 10M1 14l5.36 4.36A9 9 0 0 0 20.49 15" />
          </svg>
          {{ refreshingStatus ? t('packingDetail.refreshing') : t('packingDetail.refresh') }}
        </button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('packingDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('packingDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ packingBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('packingDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ packingBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('packingList.columns.customerName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : kvValue(detail.customerName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('packingList.columns.salesUserName') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : kvValue(detail.salesUserName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('packingDetail.stockOutType') }}</span>
              <span class="info-value">
                <StockBizTypeTag
                biz="out"
                :type="detail.stockOutType"
                :customs-declaration-id="detail.customsDeclarationId"
                :customs-declaration-code="detail.customsDeclarationCode"
              />
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('packingDetail.materialType') }}</span>
              <span class="info-value">{{ packingMaterialTypeLabel(detail.materialType) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('packingList.columns.itemRows') }}</span>
              <span class="info-value">{{ detail.itemRows }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('packingDetail.scheduleShipDate') }}</span>
              <span class="info-value info-value--time">{{ scheduleShipDateText }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('packingList.columns.shipmentMethod') }}</span>
              <span class="info-value">{{ shipmentMethodDisplay(detail.shipmentMethod) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.detail.expressCompany') }}</span>
              <span class="info-value">{{ expressCompanyDisplay(detail.expressCompany) }}</span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('packingDetail.comment') }}</span>
              <span class="info-value">{{ kvValue(detail.comment) }}</span>
            </div>
          </div>
        </div>

        <StockOutCustomsSummaryPanel v-if="detail.customsSummary?.declarationId" :summary="detail.customsSummary" />

        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': packingExtendTab === 'ship' }"
              @click="packingExtendTab = 'ship'"
            >
              {{ t('packingDetail.tabs.shipAddress') }}
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': packingExtendTab === 'bill' }"
              @click="packingExtendTab = 'bill'"
            >
              {{ t('packingDetail.tabs.billAddress') }}
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': packingExtendTab === 'deliveryReq' }"
              @click="packingExtendTab = 'deliveryReq'"
            >
              {{ t('packingDetail.tabs.deliveryReq') }}
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': packingExtendTab === 'box' }"
              @click="packingExtendTab = 'box'"
            >
              {{ t('packingDetail.tabs.boxParams') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="packingExtendTab === 'ship'">
              <div class="info-grid info-grid--inline-labels info-grid--basic">
                <div class="info-item">
                  <span class="info-label">{{ t('packingDetail.shipCompany') }}</span>
                  <span class="info-value">{{ kvValue(detail.shipCompany) }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">{{ t('packingDetail.shipAttn') }}</span>
                  <span class="info-value">{{ kvValue(detail.shipAttn) }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">{{ t('packingDetail.shipTel') }}</span>
                  <span class="info-value">{{ kvValue(detail.shipTel) }}</span>
                </div>
              </div>
              <div class="info-grid info-grid--inline-labels">
                <div class="info-item info-item--span-all">
                  <span class="info-label">{{ t('packingDetail.shipAddress') }}</span>
                  <span class="info-value">{{ kvValue(detail.shipAddress) }}</span>
                </div>
              </div>
            </div>
            <div v-show="packingExtendTab === 'bill'">
              <div class="info-grid info-grid--inline-labels info-grid--basic">
                <div class="info-item">
                  <span class="info-label">{{ t('packingDetail.billCompany') }}</span>
                  <span class="info-value">{{ kvValue(detail.billCompany) }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">{{ t('packingDetail.billAttn') }}</span>
                  <span class="info-value">{{ kvValue(detail.billAttn) }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">{{ t('packingDetail.billTel') }}</span>
                  <span class="info-value">{{ kvValue(detail.billTel) }}</span>
                </div>
              </div>
              <div class="info-grid info-grid--inline-labels">
                <div class="info-item info-item--span-all">
                  <span class="info-label">{{ t('packingDetail.billAddress') }}</span>
                  <span class="info-value">{{ kvValue(detail.billAddress) }}</span>
                </div>
              </div>
            </div>
            <div v-show="packingExtendTab === 'deliveryReq'">
              <div class="info-grid info-grid--inline-labels">
                <div class="info-item info-item--span-all">
                  <span class="info-label">{{ t('packingDetail.deliveryReq') }}</span>
                  <span class="info-value delivery-req-value">{{ kvValue(detail.deliveryReq) }}</span>
                </div>
              </div>
            </div>
            <div v-show="packingExtendTab === 'box'">
              <div class="info-grid info-grid--inline-labels info-grid--basic info-grid--cols-4">
                <div v-for="row in boxParamsKv" :key="row.key" class="info-item">
                  <span class="info-label">{{ row.label }}</span>
                  <span class="info-value">{{ row.value }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('packingDetail.sectionLines') }}</span>
              <span v-if="detail.items.length" class="tab-count">{{ detail.items.length }}</span>
            </div>
          </div>
          <div class="info-section__body">
            <p class="panel-hint">{{ t('packingDetail.itemExtendHint') }}</p>
            <div class="detail-items-table-wrap">
              <el-empty v-if="!detail.items.length" :description="t('packingDetail.linesEmpty')" :image-size="64" />
              <el-table
                v-else
                :data="detail.items"
                :border="false"
                class="detail-panel-list-table packing-items-table"
                size="small"
                stripe
                :row-class-name="packingItemRowClassName"
                @row-click="onPackingItemRowClick"
              >
                <el-table-column
                  :label="t('packingDetail.itemCode')"
                  prop="itemCode"
                  min-width="148"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">{{ row.itemCode?.trim() || '—' }}</template>
                </el-table-column>
                <CrmCopyableTableColumn :label="t('packingItemList.columns.pn')" prop="pn" min-width="140" />
                <CrmCopyableTableColumn :label="t('packingItemList.columns.brand')" prop="brand" min-width="120" />
                <el-table-column :label="t('packingItemList.columns.qty')" prop="qty" width="88" align="right" />
                <el-table-column :label="t('packingDetail.unit')" prop="unit" width="72" />
                <el-table-column :label="t('packingItemList.columns.sellOrderCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.sellOrderCode || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('packingItemList.columns.sellOrderItemCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.sellOrderItemCode || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.comment')" prop="comment" min-width="120" show-overflow-tooltip />
              </el-table>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('packingDetail.sectionItemExtend') }}</span>
            </div>
          </div>
          <div class="info-section__body">
            <p v-if="selectedPackingItemId" class="panel-hint panel-hint--muted">
              <PackingCascadeItemSummary
                v-if="selectedPackingItemSummary"
                :pn="selectedPackingItemSummary.pn"
                :brand="selectedPackingItemSummary.brand"
                :qty-text="selectedPackingItemSummary.qtyText"
              />
            </p>
            <div class="detail-items-table-wrap">
              <el-empty
                v-if="!selectedItemExtends.length"
                :description="selectedPackingItemId ? t('packingDetail.itemExtendEmpty') : t('packingDetail.itemExtendNoSelection')"
                :image-size="64"
              />
              <el-table
                v-else
                :data="selectedItemExtends"
                :border="false"
                class="detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column :label="t('packingDetail.extendColumns.customerSo')" prop="customerSo" min-width="120" show-overflow-tooltip />
                <el-table-column :label="t('packingDetail.extendColumns.customerPn')" prop="customerPn" min-width="120" show-overflow-tooltip />
                <el-table-column :label="t('packingDetail.extendColumns.customerBrand')" prop="customerBrand" min-width="110" show-overflow-tooltip />
                <el-table-column :label="t('packingDetail.extendColumns.price')" width="110" align="right">
                  <template #default="{ row }">
                    <span v-if="row.price != null">{{ row.price }}</span>
                    <span v-else>—</span>
                  </template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.extendColumns.priceCurrency')" width="80" align="center">
                  <template #default="{ row }">
                    {{ row.priceCurrency != null ? currencyLabel(row.priceCurrency) : '—' }}
                  </template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.extendColumns.priceConvertPrice')" width="110" align="right">
                  <template #default="{ row }">
                    <span v-if="row.priceConvertPrice != null">{{ row.priceConvertPrice }}</span>
                    <span v-else>—</span>
                  </template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.extendColumns.sellOrderCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.sellOrderCode?.trim() || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.extendColumns.sellOrderItemCode')" min-width="150" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.sellOrderItemCode?.trim() || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.extendColumns.customerName')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ displayCustomerName(row.customerName) }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.extendColumns.salesUserName')" min-width="120" show-overflow-tooltip>
                  <template #default="{ row }">{{ displaySalesUserName(row.salesUserName) }}</template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </div>

        <div v-loading="loadingPickPage" class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('packingDetail.sectionPickingLines') }}</span>
            </div>
          </div>
          <div class="info-section__body" :class="{ 'info-section__body--compact': !selectedPickingLines.length }">
            <p v-if="selectedPackingItemId" class="panel-hint panel-hint--muted">
              <PackingCascadeItemSummary
                v-if="selectedPackingItemSummary"
                :pn="selectedPackingItemSummary.pn"
                :brand="selectedPackingItemSummary.brand"
                :qty-text="selectedPackingItemSummary.qtyText"
              />
              <template v-if="pickPage?.pickingTask?.taskCode">
                <span class="panel-hint__sep"> · </span>{{ t('packingDetail.pickingTaskCode', { code: pickPage.pickingTask.taskCode }) }}
              </template>
              <template v-if="!selectedPickingLines.length">
                <span class="panel-hint__sep"> · </span
                ><span class="panel-status-badge panel-status-badge--warning">{{ t('packingDetail.pickingLinesNotPicked') }}</span>
              </template>
            </p>
            <p v-else class="panel-hint">{{ t('packingDetail.pickingLinesHint') }}</p>
            <div v-if="selectedPickingLines.length" class="detail-items-table-wrap">
              <el-table
                :data="selectedPickingLines"
                :border="false"
                class="detail-panel-list-table"
                size="small"
                stripe
                row-key="id"
              >
                <el-table-column :label="t('pickingSlip.detail.itemCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.itemCode?.trim() || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.pickingColumns.stockItemCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ pickingLineStockItemCode(row) }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.pickingColumns.stockInItemCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ pickingLineStockInItemCode(row) }}</template>
                </el-table-column>
                <el-table-column :label="t('inventoryList.columns.stockType')" width="100" align="center">
                  <template #default="{ row }">{{ pickingLineStockTypeLabel(row) }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.pickingColumns.planQty')" width="88" align="right">
                  <template #default="{ row }">{{ row.planQty }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.pickingColumns.pickedQty')" width="88" align="right">
                  <template #default="{ row }">{{ row.pickedQty }}</template>
                </el-table-column>
                <el-table-column :label="t('packingDetail.pickingColumns.source')" width="110" align="center">
                  <template #default="{ row }">
                    <span v-if="pickingLineIsStocking(row)" class="picking-source-stocking">
                      {{ t('inventoryList.stockTypes.stocking') }}
                    </span>
                    <span v-else class="picking-source-normal">{{ t('inventoryList.stockTypes.customer') }}</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('packingDetail.sectionStockOutNotify') }}</span>
            </div>
          </div>
          <div class="info-section__body">
            <p v-if="selectedPackingItemId" class="panel-hint panel-hint--muted">
              <PackingCascadeItemSummary
                v-if="selectedPackingItemSummary"
                :pn="selectedPackingItemSummary.pn"
                :brand="selectedPackingItemSummary.brand"
                :qty-text="selectedPackingItemSummary.qtyText"
              />
            </p>
            <p v-else class="panel-hint">{{ t('packingDetail.stockOutNotifyHint') }}</p>
            <div class="detail-items-table-wrap">
              <el-empty v-if="!selectedStockOutNotifyRows.length" :description="stockOutNotifyEmptyText" :image-size="64" />
              <el-table
                v-else
                :data="selectedStockOutNotifyRows"
                :border="false"
                class="detail-panel-list-table packing-stock-out-notify-table"
                size="small"
                stripe
                row-key="id"
                @row-dblclick="onStockOutNotifyRowDblClick"
              >
                <el-table-column :label="t('stockOutNotifyList.columns.status')" width="110" align="center">
                  <template #default="{ row }">
                    {{ stockOutNotifyStatusLabel(row.status) }}
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('stockOutNotifyList.columns.requestCode')"
                  prop="requestCode"
                  min-width="140"
                  show-overflow-tooltip
                />
                <CrmCopyableTableColumn
                  :label="t('stockOutNotifyList.columns.materialModel')"
                  prop="materialModel"
                  min-width="140"
                />
                <CrmCopyableTableColumn :label="t('stockOutNotifyList.columns.brand')" prop="brand" min-width="120" />
                <el-table-column :label="t('stockOutNotifyList.columns.outQuantity')" prop="outQuantity" width="100" align="right" />
                <el-table-column :label="t('stockOutNotifyList.columns.regionType')" width="100" align="center">
                  <template #default="{ row }">{{ stockOutNotifyRegionLabel(row) }}</template>
                </el-table-column>
                <el-table-column :label="t('stockOutNotifyList.columns.salesOrderCode')" min-width="130" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.salesOrderCode || '—' }}</template>
                </el-table-column>
                <el-table-column :label="t('stockOutNotifyList.columns.customer')" min-width="160" show-overflow-tooltip>
                  <template #default="{ row }">{{ displayCustomerName(row.customerName) }}</template>
                </el-table-column>
                <el-table-column :label="t('stockOutNotifyList.columns.salesUserName')" min-width="110" show-overflow-tooltip>
                  <template #default="{ row }">{{ displaySalesUserName(row.salesUserName) }}</template>
                </el-table-column>
                <el-table-column :label="t('stockOutNotifyList.columns.requestDate')" min-width="150">
                  <template #default="{ row }">{{ formatTime(row.requestDate) }}</template>
                </el-table-column>
                <el-table-column :label="t('stockOutNotifyList.columns.remark')" prop="remark" min-width="140" show-overflow-tooltip />
              </el-table>
            </div>
          </div>
        </div>

        <StockOutBatchPanel
          :packing-id="packingRouteId"
          :packing-code="detail.code || ''"
          :can-write="canWriteLogisticsData"
        />
      </template>
      <el-empty v-else-if="!loading" :description="t('packingDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  packingApi,
  packingMaterialTypeLabel,
  packingStatusLabel,
  packingStatusTagType,
  currencyLabel,
  type PackingDetail,
  type PackingDetailLine,
  type PackingStockOutNotifyRow
} from '@/api/packing'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import {
  inventoryCenterApi,
  type PickPageByPacking,
  type PickingTaskLine
} from '@/api/inventoryCenter'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import StockOutCustomsSummaryPanel from '@/components/Customs/StockOutCustomsSummaryPanel.vue'
import PackingCascadeItemSummary from '@/components/Inventory/PackingCascadeItemSummary.vue'
import { usePackingDetailFlowPanelStore } from '@/stores/packingDetailFlowPanel'

const StockOutBatchPanel = defineAsyncComponent(
  () => import('@/components/Inventory/StockOutBatchPanel.vue')
)

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const packingFlowStore = usePackingDetailFlowPanelStore()

const canRefreshPackingStatus = computed(
  () => authStore.user?.isSysAdmin === true || authStore.user?.isSysManager === true
)
const refreshingStatus = ref(false)

function shipmentMethodDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = shipmentArrivalOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  const hit = expressOptions.value.find((o) => String(o.value) === c)
  return hit?.label ?? c
}

function displayCustomerName(name?: string | null): string {
  if (maskSaleSensitiveFields.value) return '—'
  return name?.trim() || '—'
}

function displaySalesUserName(name?: string | null): string {
  if (maskSaleSensitiveFields.value) return '—'
  return name?.trim() || '—'
}

const loading = ref(false)
const loadingPickPage = ref(false)
const detail = ref<PackingDetail | null>(null)
const pickPage = ref<PickPageByPacking | null>(null)
const selectedPackingItemId = ref<string | null>(null)
const packingExtendTab = ref<'ship' | 'bill' | 'deliveryReq' | 'box'>('ship')

const packingRouteId = computed(() => String(route.params.id || '').trim())

const packingCaptionAvatarChar = computed(() => {
  const c = detail.value?.code?.trim()
  return c ? c[0]! : '箱'
})

const packingBasicCreateDateText = computed(() => {
  const raw = detail.value?.createTime
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

const packingBasicCreateUserText = computed(() => detail.value?.createUserName?.trim() || '—')

const scheduleShipDateText = computed(() => {
  const raw = detail.value?.scheduleShipDate
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

type KvRow = { key: string; label: string; value: string }

function kvValue(v?: string | null): string {
  const s = v?.trim()
  return s || '—'
}

function kvNumber(v?: number | null): string {
  return v != null ? String(v) : '—'
}

const boxParamsKv = computed<KvRow[]>(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'nw', label: t('packingDetail.boxNw'), value: kvNumber(d.boxNw) },
    { key: 'gw', label: t('packingDetail.boxGw'), value: kvNumber(d.boxGw) },
    { key: 'dim', label: t('packingDetail.boxDim'), value: kvValue(d.boxDim) },
    { key: 'ctns', label: t('packingDetail.boxCtns'), value: kvNumber(d.boxCtns) }
  ]
})

const selectedItemExtends = computed(() => {
  const d = detail.value
  const itemId = selectedPackingItemId.value
  if (!d || !itemId) return []
  return d.itemExtends.filter((e) => e.packingItemId === itemId)
})

const selectedStockOutNotifyRows = computed((): PackingStockOutNotifyRow[] => {
  const d = detail.value
  const itemId = selectedPackingItemId.value
  if (!d || !itemId) return []

  const line = d.items.find((x) => x.id === itemId)
  if (!line) return []

  const all = d.stockOutNotifies ?? []
  const notifyId = line.stockOutNotifyId?.trim()
  if (notifyId) {
    const byId = all.find((n) => n.id === notifyId)
    return byId ? [byId] : []
  }

  const sellItemId = line.sellOrderItemId?.trim()
  if (sellItemId) {
    const bySoItem = all.find((n) => (n.salesOrderItemId?.trim() || '') === sellItemId)
    return bySoItem ? [bySoItem] : []
  }

  return []
})

const stockOutNotifyEmptyText = computed(() => {
  if (!selectedPackingItemId.value) return t('packingDetail.stockOutNotifyNoSelection')
  return t('packingDetail.stockOutNotifyEmpty')
})

const selectedPickingLines = computed((): PickingTaskLine[] => {
  const itemId = selectedPackingItemId.value?.trim()
  if (!itemId || !pickPage.value?.lines?.length) return []
  const pl = pickPage.value.lines.find((l) => l.packingItemId === itemId)
  return pl?.pickingItems ?? []
})

function pickingLineStockItemCode(line: PickingTaskLine) {
  const v = line.stockItemCode ?? (line as unknown as Record<string, unknown>).StockItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingLineStockInItemCode(line: PickingTaskLine) {
  const v = line.stockInItemCode ?? (line as unknown as Record<string, unknown>).StockInItemCode
  const s = String(v ?? '').trim()
  return s || '—'
}

function pickingLineStockTypeLabel(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  const n = line.stockType ?? x.StockType
  if (n == null || n === '') return t('inventoryList.stockTypes.unknown')
  const num = Number(n)
  const m: Record<number, string> = {
    1: t('inventoryList.stockTypes.customer'),
    2: t('inventoryList.stockTypes.stocking'),
    3: t('inventoryList.stockTypes.sample')
  }
  return Number.isFinite(num) ? (m[num] ?? t('inventoryList.stockTypes.unknown')) : t('inventoryList.stockTypes.unknown')
}

function pickingLineIsStocking(line: PickingTaskLine) {
  const x = line as unknown as Record<string, unknown>
  return Boolean(line.isStockingSupplement ?? x.IsStockingSupplement)
}

const selectedPackingItemSummary = computed(() => {
  const d = detail.value
  const itemId = selectedPackingItemId.value
  if (!d || !itemId) return null
  const line = d.items.find((x) => x.id === itemId)
  if (!line) {
    return { pn: itemId, brand: '—', qtyText: '—' }
  }
  const pn = line.pn?.trim() || '—'
  const brand = line.brand?.trim() || '—'
  const qtyText = `${line.qty}${line.unit ? ` ${line.unit}` : ''}`
  return { pn, brand, qtyText }
})

function onPackingItemRowClick(row: PackingDetailLine) {
  const id = String(row?.id || '').trim()
  if (!id) return
  selectedPackingItemId.value = id
}

function packingItemRowClassName({ row }: { row: PackingDetailLine }) {
  return row.id === selectedPackingItemId.value ? 'packing-item-row--active' : ''
}

function stockOutNotifyStatusLabel(s: number) {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function stockOutNotifyRegionLabel(row: PackingStockOutNotifyRow) {
  const n = normalizeRegionType(row.regionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

function onStockOutNotifyRowDblClick(row: PackingStockOutNotifyRow) {
  const id = String(row?.id || '').trim()
  if (!id) return
  router.push({ name: 'StockOutNotifyDetail', params: { id } })
}

function syncDefaultSelectedItem() {
  const items = detail.value?.items ?? []
  if (!items.length) {
    selectedPackingItemId.value = null
    return
  }
  const current = selectedPackingItemId.value
  if (!current || !items.some((x) => x.id === current)) {
    selectedPackingItemId.value = items[0].id
  }
}

function formatTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function goBack() {
  router.push({ name: 'PackingList' })
}

async function loadPickPage(packingId: string) {
  loadingPickPage.value = true
  try {
    pickPage.value = await inventoryCenterApi.getPickPageByPacking(packingId)
  } catch (e) {
    console.error(e)
    pickPage.value = null
  } finally {
    loadingPickPage.value = false
  }
}

async function loadDetail() {
  const id = String(route.params.id || '').trim()
  if (!id) {
    detail.value = null
    pickPage.value = null
    return
  }
  loading.value = true
  pickPage.value = null
  try {
    detail.value = await packingApi.getById(id)
    syncDefaultSelectedItem()
    await loadPickPage(id)
  } catch (e) {
    console.error(e)
    detail.value = null
    pickPage.value = null
    ElMessage.error(e instanceof Error ? e.message : t('packingDetail.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function handleRefreshStatus() {
  if (!detail.value?.id || refreshingStatus.value || !canRefreshPackingStatus.value) return
  const code = detail.value.code || detail.value.id
  try {
    await ElMessageBox.confirm(
      t('packingDetail.refreshConfirm', { code }),
      t('packingDetail.refreshConfirmTitle'),
      { type: 'warning', confirmButtonText: t('packingDetail.refresh'), cancelButtonText: t('common.cancel') }
    )
  } catch {
    return
  }

  refreshingStatus.value = true
  try {
    const result = await packingApi.refreshStatus(detail.value.id)
    await loadDetail()
    if (!result.changed) {
      await ElMessageBox.alert(t('packingDetail.refreshNoChange'), t('packingDetail.refreshResultTitle'), {
        confirmButtonText: t('common.confirm')
      })
      return
    }
    await ElMessageBox.alert(
      t('packingDetail.refreshChanged', {
        from: packingStatusLabel(result.previousStatus),
        to: packingStatusLabel(result.currentStatus)
      }),
      t('packingDetail.refreshResultTitle'),
      { confirmButtonText: t('common.confirm') }
    )
  } catch (e) {
    await ElMessageBox.alert(
      getApiErrorMessage(e, t('packingDetail.refreshFailed')),
      t('packingDetail.refreshFailedTitle'),
      { confirmButtonText: t('common.confirm') }
    )
  } finally {
    refreshingStatus.value = false
  }
}

watch(
  () => route.params.id,
  () => {
    void loadDetail()
  }
)

watch(
  [detail, selectedPackingItemId, pickPage],
  () => {
    const d = detail.value
    const itemId = selectedPackingItemId.value?.trim()
    if (!d || !itemId) {
      packingFlowStore.clear()
      return
    }
    const line = d.items.find((x) => x.id === itemId)
    if (!line) {
      packingFlowStore.clear()
      return
    }
    void packingFlowStore.selectPackingItem(d, line, pickPage.value)
  },
  { immediate: true }
)

onMounted(() => {
  void ensureLogisticsDict()
  void loadDetail()
})

onUnmounted(() => {
  packingFlowStore.clear()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-detail-page {
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

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.07);
    color: $text-primary;
    border-color: rgba(0, 212, 255, 0.2);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.packing-caption-title-group {
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

.packing-header-meta-row {
  min-height: 28px;
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

  &--compact {
    padding-bottom: 14px;

    .panel-hint {
      margin-bottom: 0;
    }
  }
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

.info-grid--cols-4 {
  grid-template-columns: repeat(4, minmax(0, 1fr));

  .info-item {
    &:nth-child(3n) {
      border-right: 1px solid rgba(255, 255, 255, 0.04);
    }

    &:nth-child(4n) {
      border-right: none;
    }
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
}

.delivery-req-value {
  white-space: pre-wrap;
  line-height: 1.65;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
  margin-bottom: 16px;
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

.panel-hint {
  margin: 0 0 10px;
  font-size: 12px;
  color: $text-muted;
  line-height: 1.5;

  &--muted {
    // 级联从属面板当前行摘要 — 见《业务详情页面规范》§7.4.6；字色由 __value / __sep 分担
  }
}

.panel-hint__sep {
  color: $text-muted;
}

.panel-status-badge {
  display: inline-block;
  padding: 1px 8px;
  border-radius: 4px;
  font-size: 12px;
  line-height: 1.5;
  vertical-align: baseline;

  &--warning {
    background: rgba(255, 214, 102, 0.55);
    color: #4a5568;
  }
}

.detail-items-table-wrap {
  margin-top: 4px;
}

// §7.4 表头/表体基线见 detail-panel-list-table.scss；此处仅页内裸 el-table 扩展
.detail-items-table-wrap :deep(.detail-panel-list-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  border-radius: 0;

  .el-table__inner-wrapper {
    background: transparent;

    &::before,
    &::after {
      display: none !important;
    }
  }

  .el-table__border-left-patch {
    display: none !important;
  }

  .el-table__cell {
    .cell {
      white-space: nowrap;
    }
  }
}

.packing-items-table {
  cursor: pointer;
}

.packing-stock-out-notify-table {
  cursor: default;
}

.picking-source-stocking {
  color: #e6a23c;
  font-size: 12px;
}

.picking-source-normal {
  display: inline-flex;
  align-items: center;
  font-size: 12px;
  font-weight: 600;
  color: $cyan-primary;
}
</style>
