<template>
  <div v-loading="loading" class="detail-card related-tabs-card">
    <el-alert v-if="loadError" type="warning" :closable="false" class="related-alert" :title="loadError" />
    <el-tabs v-model="activeTab" type="border-card" class="related-tabs">
      <el-tab-pane :label="t('stockOutNotifyList.detail.tabs.sellLine')" name="sellLine">
        <div v-if="!request" class="related-tab-hint">{{ t('stockOutNotifyList.detail.relatedEmpty.noRequest') }}</div>
        <template v-else>
          <div class="related-tab-toolbar">
            <router-link
              v-if="linkedSalesOrderId"
              class="related-toolbar-link"
              :to="{ name: 'SalesOrderDetail', params: { id: linkedSalesOrderId } }"
            >
              {{ t('stockOutNotifyList.detail.viewOrder') }}
            </router-link>
          </div>
          <CrmDataTable
            class="quantum-table-block el-table-host picking-so-item-embed"
            embedded
            column-layout-key="stock-out-notify-detail-so-item"
            :columns="soItemColumns"
            :show-column-settings="false"
            :show-row-density-toggle="false"
            :data="salesOrderItemRows"
            row-key="sellOrderItemId"
            size="small"
            :empty-text="t('stockOutNotifyList.detail.relatedEmpty.noSalesLine')"
            @row-dblclick="onSoItemDblclick"
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

      <el-tab-pane :label="t('stockOutNotifyList.detail.tabs.packing')" name="packing">
        <div class="related-tab-hint">{{ t('stockOutNotifyList.detail.relatedEmpty.noPacking') }}</div>
        <el-table :data="packingRows" border size="small" class="lines-table" :empty-text="t('stockOutNotifyList.detail.relatedEmpty.noPacking')">
          <el-table-column :label="t('stockOutNotifyList.detail.packingColumns.code')" min-width="140" show-overflow-tooltip prop="packingCode" />
          <el-table-column :label="t('stockOutNotifyList.detail.packingColumns.status')" width="100" align="center" prop="status" />
          <el-table-column :label="t('stockOutNotifyList.detail.packingColumns.createTime')" min-width="150" show-overflow-tooltip prop="createTime" />
        </el-table>
      </el-tab-pane>

      <el-tab-pane :label="t('stockOutNotifyList.detail.tabs.stockOut')" name="stockOut">
        <div v-if="!requestCodeTrim" class="related-tab-hint">{{ t('stockOutNotifyList.detail.relatedEmpty.noRequestCode') }}</div>
        <el-table
          v-else
          :data="relatedStockOuts"
          border
          size="small"
          class="lines-table"
          :empty-text="t('stockOutNotifyList.detail.relatedEmpty.noStockOuts')"
        >
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

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { buildSalesOrderItemListColumns } from '@/composables/buildSalesOrderItemListColumns'
import { translateSalesOrderStatus, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import { stockOutApi, type StockOutDto, type StockOutRequestDto } from '@/api/stockOut'
import { salesOrderApi } from '@/api/salesOrder'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { getApiErrorMessage } from '@/utils/apiError'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  request: StockOutRequestDto | null
}>()

const router = useRouter()
const { t, locale } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const authStore = useAuthStore()

const activeTab = ref('sellLine')
const loading = ref(false)
const loadError = ref('')
const salesOrderItemRows = ref<SalesOrderItemLineRow[]>([])
const relatedStockOuts = ref<StockOutDto[]>([])
const linkedSalesOrderId = ref('')
const packingRows = ref<{ packingCode: string; status: string; createTime: string }[]>([])

const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const listCustomerColumnOk = computed(() => canViewCustomer.value && !maskSaleSensitiveFields.value)
const listShowAmountColumns = computed(() => canViewAmount.value && !maskSaleSensitiveFields.value)

const soItemColumns = computed<CrmTableColumnDef[]>(() => {
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

const requestCodeTrim = computed(() => String(props.request?.requestCode || '').trim())

function normCode(s: string) {
  return s.trim().toLowerCase()
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

function onSoItemDblclick(row: SalesOrderItemLineRow) {
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

function clearRelated() {
  loadError.value = ''
  salesOrderItemRows.value = []
  relatedStockOuts.value = []
  linkedSalesOrderId.value = ''
  packingRows.value = []
}

async function loadRelated(req: StockOutRequestDto) {
  clearRelated()
  const code = String(req.requestCode || '').trim()
  if (!code) return

  loading.value = true
  try {
    const c = normCode(code)
    const outPage = await stockOutApi.getListPaged({ sourceCode: code, page: 1, pageSize: 200 })
    relatedStockOuts.value = outPage.items.filter((x) => normCode(String(x.sourceCode || '')) === c)

    const soId = String(req.salesOrderId || '').trim()
    const itemId = String(req.salesOrderItemId ?? '').trim()
    linkedSalesOrderId.value = soId
    if (soId && itemId) {
      const order = (await salesOrderApi.getById(soId)) as unknown
      const o = order && typeof order === 'object' ? (order as Record<string, unknown>) : null
      const itemsRaw = o?.items ?? o?.Items
      const items = Array.isArray(itemsRaw) ? (itemsRaw as Record<string, unknown>[]) : []
      const row = items.find((it) => {
        const id = String(it.id ?? it.sellOrderItemId ?? it.SellOrderItemId ?? '').trim()
        return id === itemId
      })
      salesOrderItemRows.value = row && o ? [toSalesOrderItemListRow(o, row)] : []
    }
  } catch (e) {
    console.error(e)
    loadError.value = getApiErrorMessage(e, t('stockOutNotifyList.detail.loadRelatedFailed'))
  } finally {
    loading.value = false
  }
}

watch(
  () => props.request,
  (req) => {
    if (req) void loadRelated(req)
    else clearRelated()
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.detail-card {
  background: $layer-2;
  border-radius: 8px;
  padding: 16px 18px;
  margin-bottom: 16px;
  border: 1px solid rgba(255, 255, 255, 0.06);
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
.lines-table {
  width: 100%;
}
.cell-link {
  color: $cyan-primary;
  text-decoration: none;
}
.cell-link:hover {
  text-decoration: underline;
}
</style>
