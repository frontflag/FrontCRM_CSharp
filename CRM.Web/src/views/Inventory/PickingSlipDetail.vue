<template>
  <div
    class="picking-slip-detail-page"
    v-loading="loading"
    element-loading-background="rgba(10,22,40,0.8)"
  >
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('pickingSlip.detail.back') }}
        </button>
        <div v-if="detail" class="picking-caption-title-group">
          <div class="caption-avatar-lg">{{ pickingCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">
                  {{ t('pickingSlip.detail.captionPrefix') }} {{ taskCodeDisplay }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption picking-header-meta-row">
              <el-tag effect="dark" :type="pickingStatusTagType(pickingStatusNum)" size="small">
                {{ statusLabel(detail) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('pickingSlip.detail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('pickingSlip.columns.createTime') }}</span>
                <span class="section-header-meta-item__value">{{ formatTime }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('pickingSlip.columns.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ d('createUserDisplay') }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.warehouse') }}</span>
              <span class="info-value">{{ d('warehouseDisplay') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.materialModel') }}</span>
              <span class="info-value">{{ d('materialModel') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.brand') }}</span>
              <span class="info-value">{{ d('brand') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.customerName') }}</span>
              <span class="info-value">{{ d('customerName') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.salesUserName') }}</span>
              <span class="info-value">{{ d('salesUserName') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.planQtyTotal') }}</span>
              <span class="info-value">{{ d('planQtyTotal') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.lineCount') }}</span>
              <span class="info-value">{{ d('lineCount') }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.columns.stockOutRequestCode') }}</span>
              <span class="info-value">
                <router-link
                  v-if="matchedRequest?.id && stockOutRequestCodeTrim"
                  class="link-text"
                  :to="{ name: 'StockOutNotifyDetail', params: { id: matchedRequest.id } }"
                >
                  {{ stockOutRequestCodeTrim }}
                </router-link>
                <span v-else>{{ stockOutRequestCodeTrim || '—' }}</span>
              </span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div v-if="stockTypesDisplay" class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('pickingSlip.detail.stockTypes') }}</span>
              <span class="info-value">{{ stockTypesDisplay }}</span>
            </div>
          </div>
          <div v-if="d('remark') !== '—'" class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('pickingSlip.detail.remark') }}</span>
              <span class="info-value">{{ d('remark') }}</span>
            </div>
          </div>
        </div>

        <div v-if="packingPanel" class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('pickingSlip.detail.sectionPacking') }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('packingList.columns.packingCode') }}</span>
              <span class="info-value">
                <router-link
                  v-if="packingPanel.packingId"
                  class="link-text"
                  :to="{ name: 'PackingDetail', params: { id: packingPanel.packingId } }"
                >
                  {{ packingPanel.packingCode || packingPanel.packingId }}
                </router-link>
                <span v-else>{{ packingPanel.packingCode || '—' }}</span>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('packingList.columns.shipmentMethod') }}</span>
              <span class="info-value">{{ shipmentMethodDisplay(packingPanel.shipmentMethod) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('pickingSlip.detail.expressCompany') }}</span>
              <span class="info-value">{{ expressCompanyDisplay(packingPanel.expressCompany) }}</span>
            </div>
          </div>
          <StockOutCustomsSummaryPanel
            v-if="packingPanel.customsSummary?.declarationId"
            embedded
            :summary="packingPanel.customsSummary"
          />
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('pickingSlip.detail.sectionLines') }}</span>
              <span v-if="lines.length" class="section-count">{{ lines.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div class="detail-items-table-wrap">
              <el-table
                :data="lines"
                class="detail-panel-list-table"
                size="small"
                empty-text="—"
              >
                <el-table-column :label="t('pickingSlip.detail.itemCode')" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ row.itemCode || '—' }}</template>
                </el-table-column>
                <el-table-column label="物料" min-width="120" prop="materialId" show-overflow-tooltip />
                <el-table-column label="在库明细编号" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ lineStockItemCode(row) }}</template>
                </el-table-column>
                <el-table-column label="入库明细编号" min-width="140" show-overflow-tooltip>
                  <template #default="{ row }">{{ lineStockInItemCode(row) }}</template>
                </el-table-column>
                <el-table-column :label="t('inventoryList.columns.stockType')" width="100" align="center">
                  <template #default="{ row }">{{ stockTypeLabel(row) }}</template>
                </el-table-column>
                <el-table-column label="计划" width="88" align="right">
                  <template #default="{ row }">{{ row.planQty }}</template>
                </el-table-column>
                <el-table-column label="已拣" width="88" align="right">
                  <template #default="{ row }">{{ row.pickedQty }}</template>
                </el-table-column>
                <el-table-column label="来源" width="110" align="center">
                  <template #default="{ row }">
                    <span v-if="lineIsStocking(row)" class="tag-stocking">{{ t('inventoryList.stockTypes.stocking') }}</span>
                    <span v-else class="tag-normal">{{ t('inventoryList.stockTypes.customer') }}</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </div>

        <div v-loading="relatedLoading" class="tabs-section">
          <el-alert
            v-if="relatedLoadError"
            type="warning"
            :closable="false"
            class="related-alert"
            :title="relatedLoadError"
          />
          <div class="tabs-nav">
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': relatedActiveTab === 'sellLine' }"
              @click="relatedActiveTab = 'sellLine'"
            >
              {{ t('pickingSlip.detail.tabs.sellLine') }}
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': relatedActiveTab === 'request' }"
              @click="relatedActiveTab = 'request'"
            >
              {{ t('pickingSlip.detail.tabs.stockOutRequest') }}
            </button>
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': relatedActiveTab === 'stockOut' }"
              @click="relatedActiveTab = 'stockOut'"
            >
              {{ t('pickingSlip.detail.tabs.stockOut') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="relatedActiveTab === 'sellLine'">
              <div v-if="!stockOutRequestCodeTrim" class="panel-hint">{{ t('pickingSlip.detail.relatedEmpty.noRequestCode') }}</div>
              <div v-else-if="!matchedRequest" class="panel-hint">{{ t('pickingSlip.detail.relatedEmpty.noMatchedRequest') }}</div>
              <template v-else>
                <CrmDataTable
                  class="quantum-table-block el-table-host picking-so-item-embed"
                  embedded
                  column-layout-key="sales-order-item-list-v2"
                  :columns="pickingSoItemColumns"
                  :show-column-settings="false"
                  :show-row-density-toggle="false"
                  :data="salesOrderItemRows"
                  row-key="sellOrderItemId"
                  size="small"
                  :empty-text="t('pickingSlip.detail.relatedEmpty.noSalesLine')"
                  @row-dblclick="onPickingSoItemDblclick"
                >
                  <template #col-customerName="{ row }">
                    <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName || '—') }}</span>
                  </template>
                  <template #col-salesUserName="{ row }">
                    <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || '—') }}</span>
                  </template>
                  <template #col-orderStatus="{ row }">
                    <el-tag effect="dark" :type="soItemStatusTagType(row.orderStatus)" size="small">{{ soItemStatusText(row.orderStatus) }}</el-tag>
                  </template>
                  <template #col-purchaseProgressStatus="{ row }">
                    <el-tag effect="dark" :type="soItemExtendTriTagType(row.purchaseProgressStatus)" size="small">
                      {{ soItemExtendTriLabel('purchase', row.purchaseProgressStatus) }}
                    </el-tag>
                  </template>
                  <template #col-stockInProgressStatus="{ row }">
                    <el-tag effect="dark" :type="soItemExtendTriTagType(row.stockInProgressStatus)" size="small">
                      {{ soItemExtendTriLabel('stockIn', row.stockInProgressStatus) }}
                    </el-tag>
                  </template>
                  <template #col-stockOutProgressStatus="{ row }">
                    <el-tag effect="dark" :type="soItemExtendTriTagType(row.stockOutProgressStatus)" size="small">
                      {{ soItemExtendTriLabel('stockOut', row.stockOutProgressStatus) }}
                    </el-tag>
                  </template>
                  <template #col-stockOutNotifyProgressStatus="{ row }">
                    <el-tag effect="dark" :type="soItemExtendTriTagType(row.stockOutNotifyProgressStatus)" size="small">
                      {{ soItemExtendTriLabel('stockOutNotify', row.stockOutNotifyProgressStatus) }}
                    </el-tag>
                  </template>
                  <template #col-receiptProgressStatus="{ row }">
                    <el-tag effect="dark" :type="soItemExtendTriTagType(row.receiptProgressStatus)" size="small">
                      {{ soItemExtendTriLabel('receipt', row.receiptProgressStatus) }}
                    </el-tag>
                  </template>
                  <template #col-invoiceProgressStatus="{ row }">
                    <el-tag effect="dark" :type="soItemExtendTriTagType(row.invoiceProgressStatus)" size="small">
                      {{ soItemExtendTriLabel('invoice', row.invoiceProgressStatus) }}
                    </el-tag>
                  </template>
                  <template #col-currency="{ row }">{{ soItemSettlementCurrencyLabel(row.currency) }}</template>
                  <template #col-price="{ row }">
                    <span class="amount-with-code">
                      <span>{{ formatUnitPriceNumber(row.price) }}</span>
                      <span v-if="formatUnitPriceNumber(row.price) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
                        {{ listAmountCurrencyIso(row.currency) }}
                      </span>
                    </span>
                  </template>
                  <template #col-lineTotal="{ row }">
                    <span class="amount-with-code">
                      <span>{{ formatTotalAmountNumber(row.lineTotal) }}</span>
                      <span v-if="formatTotalAmountNumber(row.lineTotal) !== '—'" :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">
                        {{ listAmountCurrencyIso(row.currency) }}
                      </span>
                    </span>
                  </template>
                  <template #col-usdUnitPrice="{ row }">
                    <span v-if="row.usdUnitPrice != null" class="amount-with-code">
                      <span>{{ Number(row.usdUnitPrice).toFixed(6) }}</span>
                      <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                  <template #col-usdLineTotal="{ row }">
                    <span v-if="row.usdLineTotal != null" class="amount-with-code">
                      <span>{{ Number(row.usdLineTotal).toFixed(2) }}</span>
                      <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                  <template #col-salesProfitExpected="{ row }">
                    <span v-if="row.salesProfitExpected != null" class="amount-with-code">
                      <span>{{ Number(row.salesProfitExpected).toFixed(2) }}</span>
                      <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                  <template #col-profitOutBizUsd="{ row }">
                    <span v-if="row.profitOutBizUsd != null" class="amount-with-code">
                      <span>{{ Number(row.profitOutBizUsd).toFixed(2) }}</span>
                      <span class="dock-tier-ccy dock-tier-ccy--usd">USD</span>
                    </span>
                    <span v-else>—</span>
                  </template>
                  <template #col-profitOutRateBiz="{ row }">{{
                    formatProfitOutRateBizDisplay(row.profitOutBizUsd, row.profitOutRateBiz)
                  }}</template>
                  <template #col-createTime="{ row }">{{ soItemFormatDt(row.createTime || row.orderCreateTime) }}</template>
                  <template #col-createUser="{ row }">{{
                    row.createUserName || row.createdBy || (!maskSaleSensitiveFields ? row.salesUserName : '') || '—'
                  }}</template>
                </CrmDataTable>
              </template>
            </div>
            <div v-show="relatedActiveTab === 'request'">
              <div v-if="!stockOutRequestCodeTrim" class="panel-hint">{{ t('pickingSlip.detail.relatedEmpty.noRequestCode') }}</div>
              <div v-else class="detail-items-table-wrap">
                <el-table
                  :data="matchedRequestTable"
                  class="detail-panel-list-table"
                  size="small"
                  :empty-text="t('pickingSlip.detail.relatedEmpty.noMatchedRequest')"
                >
                  <el-table-column :label="t('stockOutNotifyList.columns.requestCode')" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">
                      <router-link
                        v-if="row.id"
                        class="link-text"
                        :to="{ name: 'StockOutNotifyDetail', params: { id: row.id } }"
                      >
                        {{ row.requestCode || '—' }}
                      </router-link>
                      <span v-else>{{ row.requestCode || '—' }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutNotifyList.columns.salesOrderCode')" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">
                      <router-link
                        v-if="row.salesOrderId && row.salesOrderCode"
                        class="link-text"
                        :to="{ name: 'SalesOrderDetail', params: { id: row.salesOrderId } }"
                      >
                        {{ row.salesOrderCode }}
                      </router-link>
                      <span v-else>{{ row.salesOrderCode || '—' }}</span>
                    </template>
                  </el-table-column>
                  <CrmCopyableTableColumn :label="t('stockOutNotifyList.columns.materialModel')" min-width="120" prop="materialModel" />
                  <CrmCopyableTableColumn :label="t('stockOutNotifyList.columns.brand')" width="100" prop="brand" />
                  <el-table-column :label="t('stockOutNotifyList.columns.outQuantity')" width="96" align="right" prop="outQuantity" />
                  <el-table-column :label="t('stockOutNotifyList.columns.status')" width="100" align="center">
                    <template #default="{ row }">
                      <el-tag effect="dark" :type="stockOutRequestStatusTagType(Number(row.status))" size="small">
                        {{ stockOutRequestStatusLabel(Number(row.status)) }}
                      </el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutNotifyList.columns.requestDate')" min-width="150" show-overflow-tooltip>
                    <template #default="{ row }">{{ formatRelatedDateTime(row.expectedStockOutDate) }}</template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutNotifyList.columns.customer')" min-width="120" show-overflow-tooltip>
                    <template #default="{ row }">{{ maskCustomerCell(row.customerName) }}</template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutNotifyList.columns.salesUserName')" width="110" show-overflow-tooltip>
                    <template #default="{ row }">{{ maskSalesCell(row.salesUserName) }}</template>
                  </el-table-column>
                </el-table>
              </div>
            </div>
            <div v-show="relatedActiveTab === 'stockOut'">
              <div v-if="!stockOutRequestCodeTrim" class="panel-hint">{{ t('pickingSlip.detail.relatedEmpty.noRequestCode') }}</div>
              <div v-else class="detail-items-table-wrap">
                <el-table
                  :data="relatedStockOuts"
                  class="detail-panel-list-table"
                  size="small"
                  :empty-text="t('pickingSlip.detail.relatedEmpty.noStockOuts')"
                >
                  <el-table-column :label="t('stockOutList.columns.stockOutCode')" min-width="140" show-overflow-tooltip>
                    <template #default="{ row }">
                      <router-link
                        v-if="row.id"
                        class="link-text"
                        :to="{ name: 'StockOutDetail', params: { id: row.id } }"
                      >
                        {{ row.stockOutCode || '—' }}
                      </router-link>
                      <span v-else>{{ row.stockOutCode || '—' }}</span>
                    </template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutList.columns.status')" width="100" align="center">
                    <template #default="{ row }">
                      <el-tag effect="dark" :type="stockOutStatusTagType(Number(row.status))" size="small">
                        {{ stockOutStatusLabel(Number(row.status)) }}
                      </el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutList.columns.sourceCode')" min-width="120" show-overflow-tooltip prop="sourceCode" />
                  <el-table-column :label="t('stockOutList.columns.totalQuantity')" width="96" align="right" prop="totalQuantity" />
                  <el-table-column :label="t('stockOutList.columns.stockOutDate')" min-width="150" show-overflow-tooltip>
                    <template #default="{ row }">{{ formatRelatedDateTime(row.stockOutDate) }}</template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutList.columns.customerName')" min-width="120" show-overflow-tooltip>
                    <template #default="{ row }">{{ maskCustomerCell(row.customerName) }}</template>
                  </el-table-column>
                  <el-table-column :label="t('stockOutList.columns.salesUserName')" width="110" show-overflow-tooltip>
                    <template #default="{ row }">{{ maskSalesCell(row.salesUserName) }}</template>
                  </el-table-column>
                </el-table>
              </div>
            </div>
          </div>
        </div>
      </template>
      <el-empty v-else-if="!loading" :description="loadError || '—'" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { formatProfitOutRateBizDisplay } from '@/utils/profitOutRateDisplay'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import { inventoryCenterApi, type PickingTaskDetailView, type PickingTaskLine } from '@/api/inventoryCenter'
import { normalizeStockOutCustomsSummary, stockOutApi, type StockOutDto, type StockOutRequestDto } from '@/api/stockOut'
import StockOutCustomsSummaryPanel from '@/components/Customs/StockOutCustomsSummaryPanel.vue'
import { salesOrderApi } from '@/api/salesOrder'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { getApiErrorMessage } from '@/utils/apiError'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { STOCK_OUT_REQUEST_STATUS } from '@/constants/stockOutRequestStatus'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { ensureLoaded: ensureLogisticsDict, arrivalOptions, expressOptions } = useLogisticsFormDict()
const authStore = useAuthStore()

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const loading = ref(false)
const detail = ref<PickingTaskDetailView | null>(null)
const loadError = ref('')

const relatedActiveTab = ref('sellLine')
const relatedLoading = ref(false)
const relatedLoadError = ref('')
const matchedRequest = ref<StockOutRequestDto | null>(null)
const salesOrderItemRows = ref<SalesOrderItemLineRow[]>([])
const relatedStockOuts = ref<StockOutDto[]>([])
const linkedSalesOrderId = ref('')

const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const listCustomerColumnOk = computed(() => canViewCustomer.value && !maskSaleSensitiveFields.value)
const listShowAmountColumns = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)

const pickingSoItemColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildSalesOrderItemListColumns({
    t,
    listCustomerColumnOk: listCustomerColumnOk.value,
    listShowAmountColumns: listShowAmountColumns.value,
    opColWidth: 0,
    opColMinWidth: 0,
    withSelection: false,
    withActions: false
  })
})

const matchedRequestTable = computed(() => (matchedRequest.value ? [matchedRequest.value] : []))

const stockOutRequestCodeTrim = computed(() => {
  const r = raw.value
  if (!r) return ''
  const v = r.stockOutRequestCode ?? r.StockOutRequestCode
  return String(v ?? '').trim()
})

const raw = computed(() => detail.value as unknown as Record<string, unknown> | null)

const taskCodeDisplay = computed(() => {
  const code = d('taskCode')
  return code === '—' ? '—' : code
})

const pickingStatusNum = computed(() => {
  const r = raw.value
  if (!r) return 0
  return Number(r.status ?? r.Status ?? 0)
})

const pickingCaptionAvatarChar = computed(() => {
  const code = taskCodeDisplay.value.trim()
  return code && code !== '—' ? code[0]! : '拣'
})

const packingPanel = computed(() => {
  const r = raw.value
  if (!r) return null
  const p = (r.packing ?? r.Packing) as Record<string, unknown> | null | undefined
  if (!p || typeof p !== 'object') return null
  const packingId = String(p.packingId ?? p.PackingId ?? '').trim()
  if (!packingId) return null
  return {
    packingId,
    packingCode: (p.packingCode ?? p.PackingCode) as string | null | undefined,
    shipmentMethod: (p.shipmentMethod ?? p.ShipmentMethod) as string | null | undefined,
    expressCompany: (p.expressCompany ?? p.ExpressCompany) as string | null | undefined,
    stockOutType: Number(p.stockOutType ?? p.StockOutType ?? 0) || undefined,
    customsDeclarationId: (p.customsDeclarationId ?? p.CustomsDeclarationId) as string | null | undefined,
    customsDeclarationCode: (p.customsDeclarationCode ?? p.CustomsDeclarationCode) as string | null | undefined,
    customsSummary: normalizeStockOutCustomsSummary(p.customsSummary ?? p.CustomsSummary)
  }
})

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of arrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const expressLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of expressOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

function shipmentMethodDisplay(code?: string | null): string {
  if (!code?.trim()) return '—'
  const c = code.trim()
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  if (!code?.trim()) return '—'
  const c = code.trim()
  return expressLabelByCode.value.get(c.toLowerCase()) ?? c
}

function d(key: string) {
  if (maskSaleSensitiveFields.value && (key === 'customerName' || key === 'salesUserName')) return '—'
  const r = raw.value
  if (!r) return '—'
  const pascal = key.charAt(0).toUpperCase() + key.slice(1)
  const v = r[key] ?? r[pascal]
  if (v == null || v === '') return '—'
  return String(v)
}

const formatTime = computed(() => {
  const r = raw.value
  if (!r) return '—'
  const v = (r.createTime ?? r.CreateTime) as string | undefined
  if (!v) return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
})

const lines = computed<PickingTaskLine[]>(() => {
  const x = detail.value as unknown as Record<string, unknown> | null
  if (!x) return []
  const rawLines = x.items ?? x.Items
  return Array.isArray(rawLines) ? (rawLines as PickingTaskLine[]) : []
})

const stockTypesDisplay = computed(() => {
  const r = raw.value
  if (!r) return ''
  const arr = (r.distinctStockTypes ?? r.DistinctStockTypes) as unknown
  if (!Array.isArray(arr) || arr.length === 0) return ''
  return (arr as number[])
    .map((c) => stockTypeLabelCode(Number(c)))
    .filter(Boolean)
    .join(locale.value === 'zh-CN' ? '、' : ', ')
})

function stockTypeLabelCode(code: number) {
  const m: Record<number, string> = {
    1: t('inventoryList.stockTypes.customer'),
    2: t('inventoryList.stockTypes.stocking'),
    3: t('inventoryList.stockTypes.sample')
  }
  return m[code] ?? ''
}

function statusLabel(row: PickingTaskDetailView) {
  const r = row as unknown as Record<string, unknown>
  const s = Number(r.status ?? r.Status ?? 0)
  if (s === 1) return t('pickingSlip.status.pending')
  if (s === 2) return t('pickingSlip.status.inProgress')
  if (s === 100) return t('pickingSlip.status.done')
  if (s === -1) return t('pickingSlip.status.cancelled')
  return t('pickingSlip.status.unknown')
}

function pickingStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === 1) return 'info'
  if (s === 2) return 'warning'
  if (s === 100) return 'success'
  if (s === -1) return 'info'
  return 'info'
}

function lineRecord(line: PickingTaskLine) {
  return line as unknown as Record<string, unknown>
}

function stockTypeLabel(line: PickingTaskLine) {
  const x = lineRecord(line)
  const n = Number(x.stockType ?? x.StockType ?? '')
  if (!Number.isFinite(n)) return '—'
  return stockTypeLabelCode(n) || '—'
}

function lineIsStocking(line: PickingTaskLine) {
  const x = lineRecord(line)
  return Boolean(x.isStockingSupplement ?? x.IsStockingSupplement)
}

function lineStockItemCode(line: PickingTaskLine) {
  const x = lineRecord(line)
  const code = String(x.stockItemCode ?? x.StockItemCode ?? '').trim()
  if (code) return code
  const id = String(x.stockItemId ?? x.StockItemId ?? '').trim()
  if (!id) return '—'
  return id.length <= 12 ? id : `${id.slice(0, 6)}…${id.slice(-4)}`
}

function lineStockInItemCode(line: PickingTaskLine) {
  const x = lineRecord(line)
  const v = String(x.stockInItemCode ?? x.StockInItemCode ?? '').trim()
  return v || '—'
}

const goBack = () => {
  router.push({ name: 'PickingSlipList' })
}

function clearRelated() {
  relatedLoadError.value = ''
  matchedRequest.value = null
  salesOrderItemRows.value = []
  relatedStockOuts.value = []
  linkedSalesOrderId.value = ''
}

function normCode(s: string) {
  return s.trim().toLowerCase()
}

function stockOutRequestStatusLabel(s: number) {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return t('stockOutNotifyList.status.pendingCustoms')
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return t('stockOutNotifyList.status.pendingPacking')
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return t('stockOutNotifyList.status.packed')
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return t('stockOutNotifyList.status.stockedOut')
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function stockOutRequestStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === STOCK_OUT_REQUEST_STATUS.PendingCustoms) return 'warning'
  if (s === STOCK_OUT_REQUEST_STATUS.PendingPacking) return 'info'
  if (s === STOCK_OUT_REQUEST_STATUS.Packed) return 'warning'
  if (s === STOCK_OUT_REQUEST_STATUS.StockedOut) return 'success'
  if (s === STOCK_OUT_REQUEST_STATUS.Cancelled) return 'info'
  return 'info'
}

function stockOutStatusLabel(s: number) {
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
      return t('rfqDetail.unknown')
  }
}

function stockOutStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === 2 || s === 4) return 'success'
  if (s === 1) return 'warning'
  if (s === 3) return 'info'
  return 'info'
}

function formatRelatedDateTime(v?: string | null) {
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

function toNum(v: unknown): number {
  const n = Number(v)
  return Number.isFinite(n) ? n : 0
}

function toSalesOrderItemListRow(order: Record<string, unknown>, item: Record<string, unknown>): SalesOrderItemLineRow {
  const sellOrderItemId = String(item.id ?? item.sellOrderItemId ?? item.SellOrderItemId ?? '').trim()
  const qty = toNum(item.qty ?? item.Qty)
  const priceRaw = item.price ?? item.Price
  const lineTotalRaw = item.lineTotal ?? item.LineTotal
  let lineTotal: number | undefined
  if (lineTotalRaw != null && lineTotalRaw !== '') lineTotal = toNum(lineTotalRaw)
  else if (priceRaw != null && priceRaw !== '' && Number.isFinite(Number(priceRaw))) lineTotal = qty * Number(priceRaw)

  return {
    ...item,
    sellOrderItemId,
    sellOrderId: String(order.id ?? order.Id ?? ''),
    sellOrderCode: String(order.sellOrderCode ?? order.SellOrderCode ?? ''),
    orderStatus: order.status ?? order.Status,
    orderCreateTime: order.createTime ?? order.CreateTime,
    customerName: order.customerName ?? order.CustomerName,
    salesUserName: (item.salesUserName ?? item.SalesUserName ?? order.salesUserName ?? order.SalesUserName ?? '') as string,
    sellOrderItemCode: item.sellOrderItemCode ?? item.SellOrderItemCode,
    pn: item.pn ?? item.Pn,
    brand: item.brand ?? item.Brand,
    qty,
    currency: item.currency ?? item.Currency,
    price: item.price ?? item.Price,
    lineTotal,
    usdUnitPrice: item.usdUnitPrice ?? item.UsdUnitPrice,
    usdLineTotal: item.usdLineTotal ?? item.UsdLineTotal,
    salesProfitExpected: item.salesProfitExpected,
    profitOutBizUsd: item.profitOutBizUsd,
    profitOutRateBiz: item.profitOutRateBiz,
    purchaseProgressStatus: item.purchaseProgressStatus,
    stockInProgressStatus: item.stockInProgressStatus,
    stockOutNotifyProgressStatus: item.stockOutNotifyProgressStatus,
    stockOutProgressStatus: item.stockOutProgressStatus,
    receiptProgressStatus: item.receiptProgressStatus,
    invoiceProgressStatus: item.invoiceProgressStatus,
    createTime: item.createTime ?? item.CreateTime,
    createUserName: item.createUserName,
    createdBy: item.createdBy
  } as SalesOrderItemLineRow
}

function soItemSettlementCurrencyLabel(code: unknown): string {
  const c = Number(code)
  if (!Number.isFinite(c)) return '—'
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? '—'
}

function soItemStatusText(s: number) {
  return translateSalesOrderStatus(s, t)
}

function soItemStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  return salesOrderStatusTagType(s) as '' | 'success' | 'warning' | 'info' | 'danger'
}

function soItemExtendTriTagType(v?: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}

function soItemExtendTriLabel(
  kind: 'purchase' | 'stockIn' | 'stockOut' | 'stockOutNotify' | 'receipt' | 'invoice',
  v?: number
): string {
  const slot = v === 2 ? 'complete' : v === 1 ? 'partial' : 'pending'
  return t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
}

function soItemFormatDt(v: unknown) {
  if (v == null || v === '') return '—'
  const s = formatDisplayDateTime(String(v))
  return s === '--' ? '—' : s
}

function onPickingSoItemDblclick(row: SalesOrderItemLineRow) {
  const id = String(row.sellOrderId ?? '').trim()
  if (id) router.push({ name: 'SalesOrderDetail', params: { id } })
}

function maskCustomerCell(v?: string | null) {
  if (maskSaleSensitiveFields.value) return '—'
  const s = (v ?? '').trim()
  return s || '—'
}

function maskSalesCell(v?: string | null) {
  if (maskSaleSensitiveFields.value) return '—'
  const s = (v ?? '').trim()
  return s || '—'
}

async function loadRelated(d: PickingTaskDetailView) {
  clearRelated()
  const r = d as unknown as Record<string, unknown>
  const code = String(r.stockOutRequestCode ?? r.StockOutRequestCode ?? '').trim()
  if (!code) return

  relatedLoading.value = true
  try {
    const c = normCode(code)
    const [reqPage, outPage] = await Promise.all([
      stockOutApi.getRequestListPaged({ keyword: code.trim() || undefined, page: 1, pageSize: 100 }),
      stockOutApi.getListPaged({ sourceCode: code.trim() || undefined, page: 1, pageSize: 200 })
    ])
    const req =
      reqPage.items.find((x) => normCode(x.requestCode || '') === c) ??
      reqPage.items[0] ??
      null
    matchedRequest.value = req
    relatedStockOuts.value = outPage.items.filter((x) => normCode(String(x.sourceCode || '')) === c)

    if (req) {
      const soId = String(req.salesOrderId || '').trim()
      const itemId = String(req.salesOrderItemId ?? '').trim()
      linkedSalesOrderId.value = soId
      if (soId && itemId) {
        try {
          const order = (await salesOrderApi.getById(soId)) as unknown
          const o = order && typeof order === 'object' ? (order as Record<string, unknown>) : null
          const itemsRaw = o?.items ?? o?.Items
          const items = Array.isArray(itemsRaw) ? (itemsRaw as Record<string, unknown>[]) : []
          const row = items.find((it) => {
            const id = String(it.id ?? it.sellOrderItemId ?? it.SellOrderItemId ?? '').trim()
            return id === itemId
          })
          salesOrderItemRows.value = row && o ? [toSalesOrderItemListRow(o, row)] : []
        } catch (e) {
          console.error(e)
          relatedLoadError.value = getApiErrorMessage(e, t('pickingSlip.messages.loadRelatedFailed'))
        }
      }
    }
  } catch (e) {
    console.error(e)
    relatedLoadError.value = getApiErrorMessage(e, t('pickingSlip.messages.loadRelatedFailed'))
    ElMessage.error(relatedLoadError.value)
  } finally {
    relatedLoading.value = false
  }
}

const load = async () => {
  const id = String(route.params.id || '').trim()
  if (!id) {
    loadError.value = '—'
    detail.value = null
    clearRelated()
    return
  }
  loading.value = true
  loadError.value = ''
  clearRelated()
  try {
    detail.value = await inventoryCenterApi.getPickingListDetail(id)
  } catch (e) {
    console.error(e)
    detail.value = null
    loadError.value = getApiErrorMessage(e, t('pickingSlip.messages.loadDetailFailed'))
    ElMessage.error(loadError.value)
  } finally {
    loading.value = false
  }
}

watch(
  () => route.params.id,
  () => {
    void load()
  },
  { immediate: true }
)

watch(
  () => detail.value,
  (d) => {
    if (d) void loadRelated(d)
    else clearRelated()
  }
)

onMounted(() => {
  void ensureLogisticsDict()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/business-detail-info-grid.scss';

.picking-slip-detail-page {
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
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.picking-caption-title-group {
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

.picking-header-meta-row {
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

.tag-stocking {
  color: #ffc107;
  font-weight: 600;
  font-size: 12px;
}

.tag-normal {
  font-size: 12px;
  color: rgba(200, 216, 232, 0.75);
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
  margin-bottom: 16px;
}

.related-alert {
  margin: 12px 16px 0;
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

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tabs-body {
  padding: 20px;
}

.panel-hint {
  margin: 0;
  font-size: 13px;
  color: $text-secondary;
  line-height: 1.5;
}

.link-text {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}
</style>
