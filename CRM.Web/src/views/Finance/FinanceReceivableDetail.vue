<template>
  <div class="finance-receivable-detail-page" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('financeReceivableDetail.back') }}
        </button>
        <div v-if="detail" class="receivable-caption-title-group">
          <div class="caption-avatar-lg">{{ receivableCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': detail.verificationStatus === 2 }">
                  {{ t('financeReceivableDetail.captionPrefix') }} {{ detail.receivableCode || '—' }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption receivable-header-meta-row">
              <el-tag :type="verificationTagType(detail.verificationStatus)" size="small" effect="dark">
                {{ verificationLabel(detail.verificationStatus) }}
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
              <span class="section-title">{{ t('financeReceivableDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financeReceivableDetail.labels.stockOutDate') }}</span>
                <span class="section-header-meta-item__value">{{ formatDate(detail.stockOutDate) }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financeReceivableDetail.labels.createTime') }}</span>
                <span class="section-header-meta-item__value">{{ formatDateTime(detail.createTime) }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.customer') }}</span>
              <span class="info-value">{{ formatReceivableCustomerLabel(detail) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.stockOutCode') }}</span>
              <span class="info-value">
                <router-link class="cell-link" :to="`/inventory/stock-out/${detail.stockOutId}`">
                  {{ detail.stockOutCode }}
                </router-link>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.sellOrderCode') }}</span>
              <span class="info-value">
                <router-link
                  v-if="detail.sellOrderId && detail.sellOrderCode"
                  class="cell-link"
                  :to="`/sales-orders/${detail.sellOrderId}`"
                >
                  {{ detail.sellOrderCode }}
                </router-link>
                <template v-else>{{ detail.sellOrderCode || '—' }}</template>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.pn') }}</span>
              <span class="info-value">{{ reportCellText(detail.pn) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.brand') }}</span>
              <span class="info-value">{{ reportCellText(detail.brand) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.qty') }}</span>
              <span class="info-value">{{ formatQty(detail.outboundQty) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.amount') }}</span>
              <span class="info-value info-value--amount">{{
                maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(detail.amount, detail.currency)
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.verifiedDone') }}</span>
              <span class="info-value info-value--received">{{
                maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(detail.verifiedDone, detail.currency)
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceivableDetail.labels.verifiedToBe') }}</span>
              <span class="info-value info-value--pending">{{
                maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(detail.verifiedToBe, detail.currency)
              }}</span>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceivableDetail.writeOffSection') }}</span>
              <span v-if="writeOffRecords.length" class="section-count">{{ writeOffRecords.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div class="detail-items-table-wrap">
            <CrmDataTable
              v-if="writeOffRecords.length"
              :data="writeOffRecords"
              embedded
              :border="false"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              class="items-table detail-panel-list-table"
              size="small"
              stripe
            >
              <el-table-column type="index" width="50" label="#" />
              <el-table-column :label="t('financeReceivableDetail.writeOffLabels.createTime')" width="170">
                <template #default="{ row }">{{ formatDateTime(row.createTime) }}</template>
              </el-table-column>
              <el-table-column :label="t('financeReceivableDetail.writeOffLabels.source')" width="100">
                <template #default="{ row }">{{ writeOffSourceLabel(row.writeOffSource) }}</template>
              </el-table-column>
              <el-table-column
                :label="t('financeReceivableDetail.writeOffLabels.receiptCode')"
                width="140"
                show-overflow-tooltip
              >
                <template #default="{ row }">
                  <router-link
                    v-if="row.financeReceiptId && row.financeReceiptCode"
                    class="cell-link"
                    :to="`/finance/receipts/${row.financeReceiptId}`"
                  >
                    {{ row.financeReceiptCode }}
                  </router-link>
                  <span v-else>{{ row.financeReceiptCode || '—' }}</span>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('financeReceivableDetail.writeOffLabels.amount')"
                width="150"
                align="right"
                header-align="right"
              >
                <template #default="{ row }">
                  {{ maskSaleSensitiveFields ? '—' : formatAmountWithCurrency(row.amount, detail.currency) }}
                </template>
              </el-table-column>
              <el-table-column
                prop="operatorUserName"
                :label="t('financeReceivableDetail.writeOffLabels.operator')"
                width="110"
                show-overflow-tooltip
              />
              <el-table-column
                prop="remark"
                :label="t('financeReceivableDetail.writeOffLabels.remark')"
                min-width="120"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.remark?.trim() || '—' }}</template>
              </el-table-column>
            </CrmDataTable>
            <DetailListPanelEmpty v-else size="low" :description="t('financeReceivableDetail.noWriteOffs')" />
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceivableDetail.stockOutSection') }}</span>
              <span v-if="stockOutItems.length" class="section-count">{{ stockOutItems.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div v-loading="stockOutItemsLoading" class="detail-items-table-wrap">
            <CrmDataTable
              v-if="stockOutItems.length"
              :data="stockOutItems"
              embedded
              :border="false"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              class="items-table detail-panel-list-table receivable-stock-out-item-embed"
              size="small"
              stripe
              row-key="stockOutItemId"
            >
              <el-table-column :label="t('stockOutItemList.columns.status')" width="100" align="center">
                <template #default="{ row }">
                  <span :class="['status-badge', `status-${row.status}`]">{{ stockOutItemStatusLabel(row.status) }}</span>
                </template>
              </el-table-column>
              <el-table-column
                prop="stockOutCode"
                :label="t('stockOutItemList.columns.stockOutCode')"
                width="150"
                show-overflow-tooltip
              />
              <el-table-column
                prop="stockInCode"
                :label="t('stockOutItemList.columns.stockInCode')"
                width="140"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.stockInCode || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column
                prop="packingCode"
                :label="t('stockOutItemList.columns.packingCode')"
                width="150"
                show-overflow-tooltip
              >
                <template #default="{ row }">
                  <router-link
                    v-if="row.packingId?.trim() && row.packingCode?.trim()"
                    class="cell-link mono-cell"
                    :to="`/inventory/packing/${row.packingId.trim()}`"
                    @click.stop
                  >
                    {{ row.packingCode.trim() }}
                  </router-link>
                  <span v-else-if="row.packingCode?.trim()" class="mono-cell">{{ row.packingCode.trim() }}</span>
                  <span v-else>{{ stockOutItemNa }}</span>
                </template>
              </el-table-column>
              <el-table-column
                prop="freightForwarderOrderNo"
                :label="t('common.freightForwarderOrderNo')"
                width="160"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.freightForwarderOrderNo?.trim() || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column :label="t('stockOutItemList.columns.stockOutDate')" width="118">
                <template #default="{ row }">{{ formatStockOutDateOnly(row.stockOutDate) }}</template>
              </el-table-column>
              <el-table-column
                prop="customerName"
                :label="t('stockOutItemList.columns.customerName')"
                min-width="120"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ maskSaleSensitiveFields ? '—' : row.customerName || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column
                prop="salesUserName"
                :label="t('stockOutItemList.columns.salesUserName')"
                width="100"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ maskSaleSensitiveFields ? '—' : row.salesUserName || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column
                prop="purchasePn"
                :label="t('stockOutItemList.columns.purchasePn')"
                min-width="130"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.purchasePn || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column
                prop="purchaseBrand"
                :label="t('stockOutItemList.columns.purchaseBrand')"
                min-width="100"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.purchaseBrand || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column
                prop="outQuantity"
                :label="t('stockOutItemList.columns.outQuantity')"
                min-width="120"
                align="right"
                show-overflow-tooltip
              />
              <el-table-column
                :label="t('financeReceivableDetail.stockOutItemLabels.salesUnitPrice')"
                min-width="132"
                align="right"
                header-align="right"
                class-name="stock-item-unit-price-col"
              >
                <template #default="{ row }">
                  <span v-if="!listShowAmountColumns">—</span>
                  <template v-else-if="row.salesPrice != null && unitPriceDockHasValue(row.salesPrice)">
                    <div class="dock-tier-price-line">
                      <template v-for="amt in [splitUnitPriceDockParts(row.salesPrice)]" :key="`sup-${row.stockOutItemId}`">
                        <span class="dock-tier-amt">
                          <span class="dock-tier-amt-int">{{ amt.intPart }}</span
                          ><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
                        </span>
                      </template>
                      <span class="dock-tier-ccy-gap">&nbsp;</span>
                      <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.salesCurrency)]">{{
                        listAmountCurrencyIso(row.salesCurrency)
                      }}</span>
                    </div>
                  </template>
                  <span v-else>{{ stockOutItemNa }}</span>
                </template>
              </el-table-column>
              <el-table-column
                :label="t('financeReceivableDetail.stockOutItemLabels.outAmount')"
                min-width="128"
                align="right"
                header-align="right"
              >
                <template #default="{ row }">
                  <span v-if="!listShowAmountColumns">—</span>
                  <template v-else-if="stockOutLineAmount(row) != null">
                    <div class="dock-tier-price-line">
                      <span class="dock-tier-amt">{{ formatTotalAmountNumber(stockOutLineAmount(row)) }}</span>
                      <span class="dock-tier-ccy-gap">&nbsp;</span>
                      <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.salesCurrency)]">{{
                        listAmountCurrencyIso(row.salesCurrency)
                      }}</span>
                    </div>
                  </template>
                  <span v-else>{{ stockOutItemNa }}</span>
                </template>
              </el-table-column>
              <el-table-column
                prop="shipmentMethod"
                :label="t('stockOutItemList.columns.shipmentMethod')"
                width="110"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ stockOutItemShipmentMethodDisplay(row.shipmentMethod) }}</template>
              </el-table-column>
              <el-table-column
                prop="courierTrackingNo"
                :label="t('stockOutItemList.columns.courierTrackingNo')"
                width="130"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.courierTrackingNo || stockOutItemNa }}</template>
              </el-table-column>
              <el-table-column
                prop="sellOrderItemCode"
                :label="t('stockOutItemList.columns.sellOrderItemCode')"
                width="130"
                show-overflow-tooltip
              >
                <template #default="{ row }">{{ row.sellOrderItemCode || stockOutItemNa }}</template>
              </el-table-column>
            </CrmDataTable>
            <DetailListPanelEmpty v-else-if="!stockOutItemsLoading" size="low" :description="t('financeReceivableDetail.noStockOutItems')" />
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceivableDetail.sellOrderItemSection') }}</span>
              <span v-if="sellOrderItemRows.length" class="section-count">{{ sellOrderItemRows.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div v-loading="sellOrderItemLoading" class="detail-items-table-wrap">
            <CrmDataTable
              v-if="sellOrderItemRows.length"
              :data="sellOrderItemRows"
              embedded
              column-layout-key="finance-receivable-detail-so-item"
              :columns="sellOrderItemColumns"
              :border="false"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              class="items-table detail-panel-list-table receivable-so-item-embed"
              size="small"
              stripe
              row-key="sellOrderItemId"
            >
              <template #col-customerName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName || '—') }}</span>
              </template>
              <template #col-customerSo="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.customerSo || '—') }}</span>
              </template>
              <template #col-customerPn="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.customerPn || '—') }}</span>
              </template>
              <template #col-salesUserName="{ row }">
                <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || '—') }}</span>
              </template>
              <template #col-orderStatus="{ row }">
                <el-tag effect="dark" :type="soItemStatusTagType(row.orderStatus)" size="small">
                  {{ soItemStatusText(row.orderStatus) }}
                </el-tag>
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
                  <span
                    v-if="formatUnitPriceNumber(row.price) !== '—'"
                    :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                  >
                    {{ listAmountCurrencyIso(row.currency) }}
                  </span>
                </span>
              </template>
              <template #col-lineTotal="{ row }">
                <span class="amount-with-code">
                  <span>{{ formatTotalAmountNumber(row.lineTotal) }}</span>
                  <span
                    v-if="formatTotalAmountNumber(row.lineTotal) !== '—'"
                    :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]"
                  >
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
                row.profitOutRateBiz != null ? Number(row.profitOutRateBiz).toFixed(6) : '—'
              }}</template>
              <template #col-createTime="{ row }">{{ soItemFormatDt(row.createTime || row.orderCreateTime) }}</template>
              <template #col-createUser="{ row }">{{
                row.createUserName || row.createdBy || (!maskSaleSensitiveFields ? row.salesUserName : '') || '—'
              }}</template>
            </CrmDataTable>
            <DetailListPanelEmpty v-else-if="!sellOrderItemLoading" size="low" :description="t('financeReceivableDetail.noSellOrderItem')" />
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="t('financeReceivableDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  financeReceivableApi,
  type FinanceReceivable,
  type FinanceReceivableWriteOffDetailItem
} from '@/api/financeReceivable'
import { CURRENCY_MAP } from '@/api/finance'
import { stockOutApi, type StockOutItemListRow } from '@/api/stockOut'
import salesOrderApi from '@/api/salesOrder'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatCustomerNameReadonlyFromRow } from '@/utils/customerDisplayName'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso, splitUnitPriceDockParts, unitPriceDockHasValue } from '@/utils/moneyFormat'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useAuthStore } from '@/stores/auth'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const authStore = useAuthStore()
const { ensureLoaded: ensureLogisticsDict, arrivalOptions } = useLogisticsFormDict()

const stockOutItemNa = computed(() => t('quoteList.na'))

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of arrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const listCustomerColumnOk = computed(() => canViewCustomer.value && !maskSaleSensitiveFields.value)
const listShowAmountColumns = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)

const sellOrderItemColumns = computed<CrmTableColumnDef[]>(() => {
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

const loading = ref(false)
const writeOffLoading = ref(false)
const stockOutItemsLoading = ref(false)
const sellOrderItemLoading = ref(false)
const detail = ref<FinanceReceivable | null>(null)
const writeOffRecords = ref<FinanceReceivableWriteOffDetailItem[]>([])
const stockOutItems = ref<StockOutItemListRow[]>([])
const sellOrderItemRows = ref<SalesOrderItemLineRow[]>([])

const receivableId = computed(() => route.params.id as string)

const receivableCaptionAvatarChar = computed(() => {
  const c = detail.value?.receivableCode?.trim()
  return c ? c[0]! : '应'
})

onMounted(() => {
  void ensureLogisticsDict().catch(() => undefined)
  fetchDetail()
})

function stockOutItemShipmentMethodDisplay(code?: string | number | null) {
  if (code === null || code === undefined || code === '') return stockOutItemNa.value
  const c = String(code).trim()
  if (!c) return stockOutItemNa.value
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function stockOutLineAmount(row: StockOutItemListRow): number | null {
  if (!unitPriceDockHasValue(row.salesPrice)) return null
  const qty = Number(row.outQuantity)
  const price = Number(row.salesPrice)
  if (!Number.isFinite(qty) || !Number.isFinite(price)) return null
  return qty * price
}

function stockOutItemStatusLabel(s: number) {
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

function formatStockOutDateOnly(v?: string | null) {
  if (!v) return stockOutItemNa.value
  return formatDisplayDateTime(v).split(/\s+/)[0] || stockOutItemNa.value
}

async function fetchDetail() {
  loading.value = true
  try {
    detail.value = await financeReceivableApi.getById(receivableId.value)
    if (detail.value) {
      await Promise.all([fetchWriteOffs(), loadStockOutItems(), loadSellOrderItem()])
    } else {
      writeOffRecords.value = []
      stockOutItems.value = []
      sellOrderItemRows.value = []
    }
  } catch {
    detail.value = null
    writeOffRecords.value = []
    stockOutItems.value = []
    sellOrderItemRows.value = []
  } finally {
    loading.value = false
  }
}

async function fetchWriteOffs() {
  writeOffLoading.value = true
  try {
    writeOffRecords.value = await financeReceivableApi.getWriteOffs(receivableId.value)
  } catch {
    writeOffRecords.value = []
  } finally {
    writeOffLoading.value = false
  }
}

async function loadStockOutItems() {
  const code = detail.value?.stockOutCode?.trim()
  if (!code) {
    stockOutItems.value = []
    return
  }
  stockOutItemsLoading.value = true
  try {
    stockOutItems.value = await stockOutApi.searchItems({ stockOutCode: code })
  } catch {
    stockOutItems.value = []
  } finally {
    stockOutItemsLoading.value = false
  }
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

async function loadSellOrderItem() {
  const orderCode = detail.value?.sellOrderCode?.trim()
  const itemId = detail.value?.sellOrderItemId?.trim()
  if (!orderCode || !itemId) {
    sellOrderItemRows.value = []
    return
  }
  sellOrderItemLoading.value = true
  try {
    const data = (await salesOrderApi.getItemLines({
      sellOrderCode: orderCode,
      page: 1,
      pageSize: 100
    })) as { items?: SalesOrderItemLineRow[] }
    const items = data.items ?? []
    const matched = items.find((row) => {
      const rid = String(row.sellOrderItemId ?? row.id ?? '').trim()
      return rid === itemId
    })
    sellOrderItemRows.value = matched ? [matched] : []
  } catch {
    sellOrderItemRows.value = []
  } finally {
    sellOrderItemLoading.value = false
  }
}

function formatReceivableCustomerLabel(row: FinanceReceivable | null | undefined) {
  return formatCustomerNameReadonlyFromRow(row, { masked: maskSaleSensitiveFields.value })
}

function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s || '—'
}

function formatAmount(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function currencyLabel(currency?: number) {
  if (currency == null) return ''
  return CURRENCY_MAP[currency] || String(currency)
}

function formatAmountWithCurrency(amount?: number, currency?: number) {
  if (amount == null) return '—'
  if (currency == null) return formatAmount(amount)
  return `${formatAmount(amount)} ${currencyLabel(currency)}`
}

function formatQty(v?: number) {
  if (v == null) return '—'
  return Number(v).toLocaleString(undefined, { maximumFractionDigits: 4 })
}

function formatDate(v?: string) {
  if (!v) return '—'
  return v.slice(0, 10)
}

function formatDateTime(v?: string) {
  if (!v) return '—'
  return formatDisplayDateTime(v)
}

function verificationLabel(status: number) {
  if (status === 2) return t('financeReceivableList.verification.complete')
  if (status === 1) return t('financeReceivableList.verification.partial')
  return t('financeReceivableList.verification.pending')
}

function verificationTagType(status: number): 'success' | 'warning' | 'info' {
  if (status === 2) return 'success'
  if (status === 1) return 'warning'
  return 'info'
}

function writeOffSourceLabel(source?: number) {
  if (source === 20) return t('financeReceivableDetail.writeOffSource.advancePool')
  return t('financeReceivableDetail.writeOffSource.receiptItem')
}

function goBack() {
  router.push({ name: 'FinanceReceivableList' })
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/crm-quote-tier-dock.scss';

.amount-with-code {
  display: inline-flex;
  align-items: baseline;
  gap: 4px;
}

.mono-cell {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;

  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }

  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }

  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }

  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }

  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}

.finance-receivable-detail-page {
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

.receivable-caption-title-group {
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

.receivable-header-meta-row {
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

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
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
  flex-shrink: 0;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.45);
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

    &:nth-last-child(-n + 3) {
      border-bottom: none;
    }
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;

  &--amount {
    font-family: 'Noto Sans SC', sans-serif;
    font-variant-numeric: tabular-nums;
    color: $cyan-primary;
    font-weight: 700;
  }

  &--received {
    font-family: 'Noto Sans SC', sans-serif;
    font-variant-numeric: tabular-nums;
    color: $success-color;
    font-weight: 700;
  }

  &--pending {
    font-family: 'Noto Sans SC', sans-serif;
    font-variant-numeric: tabular-nums;
    color: #e8a838;
    font-weight: 700;
  }
}

.section-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.cell-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.detail-panel-section-body {
  padding: 16px 20px 20px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
}
</style>
