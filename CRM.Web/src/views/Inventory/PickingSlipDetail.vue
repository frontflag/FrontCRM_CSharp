<template>
  <div class="picking-slip-detail-page stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('pickingSlip.detailTitle') }}</h1>
        </div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-secondary" @click="goBack">{{ t('pickingSlip.detail.back') }}</button>
      </div>
    </div>

    <el-skeleton v-if="loading" :rows="6" animated />
    <template v-else-if="detail">
      <div class="detail-card">
        <h3 class="section-title">{{ t('pickingSlip.detail.sectionHeader') }}</h3>
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="t('pickingSlip.columns.status')">{{ statusLabel(detail) }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.warehouse')">{{ d('warehouseDisplay') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.materialModel')">{{ d('materialModel') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.brand')">{{ d('brand') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.customerName')">{{ d('customerName') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.salesUserName')">{{ d('salesUserName') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.planQtyTotal')">{{ d('planQtyTotal') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.lineCount')">{{ d('lineCount') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.stockOutRequestCode')">{{ d('stockOutRequestCode') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.taskCode')">{{ d('taskCode') }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.createTime')">{{ formatTime }}</el-descriptions-item>
          <el-descriptions-item :label="t('pickingSlip.columns.createUser')">{{ d('createUserDisplay') }}</el-descriptions-item>
          <el-descriptions-item v-if="d('remark') !== '—'" :label="t('pickingSlip.detail.remark')" :span="2">
            {{ d('remark') }}
          </el-descriptions-item>
          <el-descriptions-item
            v-if="stockTypesDisplay"
            :label="t('pickingSlip.detail.stockTypes')"
            :span="2"
          >{{ stockTypesDisplay }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="detail-card">
        <h3 class="section-title">{{ t('pickingSlip.detail.sectionLines') }}</h3>
        <el-table :data="lines" border class="lines-table" size="small" empty-text="—">
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

      <div v-loading="relatedLoading" class="detail-card related-tabs-card">
        <el-alert v-if="relatedLoadError" type="warning" :closable="false" class="related-alert" :title="relatedLoadError" />
        <el-tabs v-model="relatedActiveTab" type="border-card" class="related-tabs">
          <el-tab-pane :label="t('pickingSlip.detail.tabs.sellLine')" name="sellLine">
            <div v-if="!stockOutRequestCodeTrim" class="related-tab-hint">{{ t('pickingSlip.detail.relatedEmpty.noRequestCode') }}</div>
            <div v-else-if="!matchedRequest" class="related-tab-hint">{{ t('pickingSlip.detail.relatedEmpty.noMatchedRequest') }}</div>
            <template v-else>
              <div class="related-tab-toolbar">
                <router-link
                  v-if="linkedSalesOrderId"
                  class="related-toolbar-link"
                  :to="{ name: 'SalesOrderDetail', params: { id: linkedSalesOrderId } }"
                >
                  {{ t('pickingSlip.detail.tabs.viewOrder') }}
                </router-link>
              </div>
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
                <template #col-orderCreateTime="{ row }">{{ soItemFormatDt(row.orderCreateTime) }}</template>
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
                  row.profitOutRateBiz != null ? Number(row.profitOutRateBiz).toFixed(6) : '—'
                }}</template>
                <template #col-createTime="{ row }">{{ soItemFormatDt(row.createTime || row.orderCreateTime) }}</template>
                <template #col-createUser="{ row }">{{
                  row.createUserName || row.createdBy || (!maskSaleSensitiveFields ? row.salesUserName : '') || '—'
                }}</template>
              </CrmDataTable>
            </template>
          </el-tab-pane>
          <el-tab-pane :label="t('pickingSlip.detail.tabs.stockOutRequest')" name="request">
            <div v-if="!stockOutRequestCodeTrim" class="related-tab-hint">{{ t('pickingSlip.detail.relatedEmpty.noRequestCode') }}</div>
            <el-table v-else :data="matchedRequestTable" border size="small" class="lines-table" :empty-text="t('pickingSlip.detail.relatedEmpty.noMatchedRequest')">
              <el-table-column :label="t('stockOutNotifyList.columns.requestCode')" min-width="140" show-overflow-tooltip prop="requestCode" />
              <el-table-column :label="t('stockOutNotifyList.columns.salesOrderCode')" min-width="140" show-overflow-tooltip prop="salesOrderCode" />
              <el-table-column :label="t('stockOutNotifyList.columns.materialModel')" min-width="120" show-overflow-tooltip prop="materialModel" />
              <el-table-column :label="t('stockOutNotifyList.columns.brand')" width="100" show-overflow-tooltip prop="brand" />
              <el-table-column :label="t('stockOutNotifyList.columns.outQuantity')" width="96" align="right" prop="outQuantity" />
              <el-table-column :label="t('stockOutNotifyList.columns.status')" width="100" align="center">
                <template #default="{ row }">{{ stockOutRequestStatusLabel(Number(row.status)) }}</template>
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
          </el-tab-pane>
          <el-tab-pane :label="t('pickingSlip.detail.tabs.stockOut')" name="stockOut">
            <div v-if="!stockOutRequestCodeTrim" class="related-tab-hint">{{ t('pickingSlip.detail.relatedEmpty.noRequestCode') }}</div>
            <el-table v-else :data="relatedStockOuts" border size="small" class="lines-table" :empty-text="t('pickingSlip.detail.relatedEmpty.noStockOuts')">
              <el-table-column :label="t('stockOutList.columns.stockOutCode')" min-width="140" show-overflow-tooltip>
                <template #default="{ row }">
                  <router-link v-if="row.id" class="cell-link" :to="{ name: 'StockOutDetail', params: { id: row.id } }">
                    {{ row.stockOutCode || '—' }}
                  </router-link>
                  <span v-else>{{ row.stockOutCode || '—' }}</span>
                </template>
              </el-table-column>
              <el-table-column :label="t('stockOutList.columns.status')" width="100" align="center">
                <template #default="{ row }">{{ stockOutStatusLabel(Number(row.status)) }}</template>
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
          </el-tab-pane>
        </el-tabs>
      </div>
    </template>
    <el-empty v-else :description="loadError || '—'" />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import { inventoryCenterApi, type PickingTaskDetailView, type PickingTaskLine } from '@/api/inventoryCenter'
import { stockOutApi, type StockOutDto, type StockOutRequestDto } from '@/api/stockOut'
import { salesOrderApi } from '@/api/salesOrder'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { getApiErrorMessage } from '@/utils/apiError'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
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
  if (s === 0) return t('stockOutNotifyList.status.pendingOut')
  if (s === 1) return t('stockOutNotifyList.status.done')
  if (s === 2) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
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

function formatRelatedDateTime(v?: string | null) {
  if (v == null || v === '') return '—'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

function toNum(v: unknown): number {
  const n = Number(v)
  return Number.isFinite(n) ? n : 0
}

/** 将订单详情中的明细行展平为与 `/sales-order-items` 列表 API 相近的字段，供 CrmDataTable 使用 */
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
    const [requests, outs] = await Promise.all([stockOutApi.getRequestList(), stockOutApi.getAll()])
    const c = normCode(code)
    const req = requests.find((x) => normCode(x.requestCode || '') === c) ?? null
    matchedRequest.value = req
    relatedStockOuts.value = outs.filter((x) => normCode(String(x.sourceCode || '')) === c)

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
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.picking-slip-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
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
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}
.page-icon {
  width: 36px;
  height: 36px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
}
.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}
.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid $border-panel;
  background: rgba(255, 255, 255, 0.05);
  color: $text-secondary;
}
.detail-card {
  background: $layer-2;
  border-radius: 8px;
  padding: 16px 18px;
  margin-bottom: 16px;
  border: 1px solid rgba(255, 255, 255, 0.06);
}
.section-title {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}
.lines-table {
  width: 100%;
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
.related-tabs-card {
  position: relative;
}
.related-alert {
  margin-bottom: 12px;
}
.related-tabs {
  --el-tabs-header-height: 40px;
}
.related-tabs :deep(.el-tabs__content) {
  padding: 12px 14px 14px;
}
.related-tab-hint {
  font-size: 13px;
  color: $text-secondary;
  padding: 8px 0 4px;
}
.related-tab-toolbar {
  margin-bottom: 10px;
}
.related-toolbar-link {
  font-size: 13px;
  color: $cyan-primary;
  text-decoration: none;
}
.related-toolbar-link:hover {
  text-decoration: underline;
}
.cell-link {
  color: $cyan-primary;
  text-decoration: none;
}
.cell-link:hover {
  text-decoration: underline;
}
</style>
