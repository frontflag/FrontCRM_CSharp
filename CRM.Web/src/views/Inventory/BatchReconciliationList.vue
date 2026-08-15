<template>
  <!-- 业务列表页：结构对齐《业务列表规范》《列表搜索栏规范》；表格见 CrmDataTable + 全局 crm-unified-list.scss -->
  <div class="batch-reconciliation-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M4 7h16M4 12h10M4 17h6" />
              <circle cx="18" cy="17" r="3" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('batchReconciliation.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('batchReconciliation.count', { count: listTotalServer }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-export btn-sm" :disabled="exporting" @click="() => void exportIn()">
          {{ t('batchReconciliation.actions.exportIn') }}
        </button>
        <button type="button" class="btn-export btn-sm" :disabled="exporting" @click="() => void exportOut()">
          {{ t('batchReconciliation.actions.exportOut') }}
        </button>
      </div>
    </div>

    <!-- 搜索栏：与 StockInList / CustomerList 一致 -->
    <div class="search-bar">
      <div class="search-left">
        <input
          v-model="filters.globalBatchNo"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.globalBatchNoPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.stockInCode"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.stockInCodePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.packingCode"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.packingCodePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.purchaseOrderCode"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.purchaseOrderCodePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.materialModel"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.materialModelPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.lot"
          class="search-input search-input--filter search-input--lot"
          :placeholder="t('batchReconciliation.filters.lotPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.serialNumber"
          class="search-input search-input--filter search-input--sn"
          :placeholder="t('batchReconciliation.filters.serialNumberPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-if="!maskPurchaseSensitiveFields"
          v-model="filters.vendorName"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.vendorNamePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filters.customerName"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.customerNamePlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <input
          v-model="filters.remark"
          class="search-input search-input--filter"
          :placeholder="t('batchReconciliation.filters.remarkPlaceholder')"
          @keyup.enter="() => void fetchList(true)"
        />
        <button type="button" class="btn-primary btn-sm" @click="() => void fetchList(true)">
          {{ t('batchReconciliation.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">
          {{ t('batchReconciliation.filters.reset') }}
        </button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      class="batch-reconciliation-list-crm-table"
      column-layout-key="batch-reconciliation-list-v2"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      highlight-current-row
      row-key="rowKey"
      @row-click="onMainRowClick"
    >
      <template #col-vendorName="{ row }">
        <vendor-name-readonly-text
          :name-zh="row.vendorName"
          :name-en="row.vendorEnglishName"
          :masked="maskPurchaseSensitiveFields"
        />
      </template>
      <template #col-customerName="{ row }">
        <span v-if="maskSaleSensitiveFields">—</span>
        <span v-else>{{ row.customerName || '—' }}</span>
      </template>
      <template #col-stockInDate="{ row }">
        <template v-for="p in [formatDateTimeParts(row.stockInDate)]" :key="'in-' + row.rowKey">
          <span v-if="!p" class="inv-list-dash">—</span>
          <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
          </span>
          <span v-else class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
        </template>
      </template>
      <template #col-stockOutDate="{ row }">
        <template v-for="p in [formatDateTimeParts(row.stockOutDate)]" :key="'out-' + row.rowKey">
          <span v-if="!p" class="inv-list-dash">—</span>
          <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
          </span>
          <span v-else class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
        </template>
      </template>
      <template #col-stockInItemQuantity="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.stockInItemQuantity) }}</span>
      </template>
      <template #col-batchQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.batchQty) }}</span>
      </template>
      <template #col-outQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.outQty) }}</span>
      </template>
      <template #col-totalOutQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.totalOutQty) }}</span>
      </template>
      <template #col-remainingQty="{ row }">
        <span class="inv-list-qty">{{ formatQtyCell(row.remainingQty) }}</span>
      </template>
    </CrmDataTable>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotalServer"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void fetchList(false)"
        @size-change="onPageSizeChange"
      />
    </div>

    <div v-if="selectedGlobalBatchNo" class="consumption-panel">
      <div class="consumption-panel__head">
        <span class="consumption-panel__title">{{ t('batchReconciliation.consumption.title') }}</span>
        <span class="consumption-panel__code">{{ selectedGlobalBatchNo }}</span>
        <button type="button" class="consumption-panel__close" @click="closeConsumptionPanel">
          {{ t('batchReconciliation.consumption.close') }}
        </button>
      </div>
      <CrmDataTable
        column-layout-key="batch-reconciliation-consumption-v1"
        :columns="consumptionColumns"
        :show-column-settings="false"
        :show-row-density-toggle="false"
        :data="consumptionList"
        v-loading="consumptionLoading"
        embedded
      >
        <template #col-customerName="{ row }">
          <span v-if="maskSaleSensitiveFields">—</span>
          <span v-else>{{ row.customerName || '—' }}</span>
        </template>
        <template #col-outQty="{ row }">
          <span class="inv-list-qty">{{ formatQtyCell(row.outQty) }}</span>
        </template>
        <template #col-stockOutDate="{ row }">
          <template v-for="p in [formatDateTimeParts(row.stockOutDate)]" :key="'c-out-' + row.stockOutBatchId">
            <span v-if="!p" class="inv-list-dash">—</span>
            <span v-else-if="isTimeMidnightOnly(p.time)" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            </span>
            <span v-else class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
          </template>
        </template>
      </CrmDataTable>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import {
  batchReconciliationApi,
  type BatchReconciliationConsumptionRow,
  type BatchReconciliationQuery,
  type BatchReconciliationRow
} from '@/api/batchReconciliation'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { withExportTimestamp } from '@/utils/exportFileName'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

type ListRow = BatchReconciliationRow & { rowKey: string }

const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const loading = ref(false)
const exporting = ref(false)
const consumptionLoading = ref(false)
const list = ref<ListRow[]>([])
const consumptionList = ref<BatchReconciliationConsumptionRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotalServer = ref(0)
const selectedGlobalBatchNo = ref('')

watch(listTotalServer, () => {
  const maxP = Math.max(1, Math.ceil(listTotalServer.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

const filters = reactive({
  globalBatchNo: '',
  purchaseOrderCode: '',
  stockInCode: '',
  packingCode: '',
  materialModel: '',
  lot: '',
  serialNumber: '',
  vendorName: '',
  customerName: '',
  remark: ''
})

function formatDateTimeParts(v: string | null | undefined) {
  return formatDisplayDateTime2DigitYearParts(v)
}

function isTimeMidnightOnly(time: string) {
  const s = String(time ?? '').trim()
  return !s || s === '00:00' || s === '00:00:00'
}

/** 《业务列表规范》§3.2：数量千分位、tabular-nums */
function formatQtyCell(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

function buildQuery(): BatchReconciliationQuery {
  return {
    globalBatchNo: filters.globalBatchNo.trim() || undefined,
    purchaseOrderCode: filters.purchaseOrderCode.trim() || undefined,
    stockInCode: filters.stockInCode.trim() || undefined,
    packingCode: filters.packingCode.trim() || undefined,
    materialModel: filters.materialModel.trim() || undefined,
    lot: filters.lot.trim() || undefined,
    serialNumber: filters.serialNumber.trim() || undefined,
    vendorName: maskPurchaseSensitiveFields.value ? undefined : filters.vendorName.trim() || undefined,
    customerName: maskSaleSensitiveFields.value ? undefined : filters.customerName.trim() || undefined,
    remark: filters.remark.trim() || undefined,
    exportSource: 'list',
    exportPageUrl: '/inventory/batch-reconciliation'
  }
}

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'globalBatchNo', label: t('batchReconciliation.columns.globalBatchNo'), prop: 'globalBatchNo', width: 130, showOverflowTooltip: true },
  { key: 'warehouseName', label: t('batchReconciliation.columns.warehouseName'), prop: 'warehouseName', width: 100, showOverflowTooltip: true },
  { key: 'stockInDate', label: t('batchReconciliation.columns.stockInDate'), prop: 'stockInDate', width: 110 },
  { key: 'stockInCode', label: t('batchReconciliation.columns.stockInCode'), prop: 'stockInCode', width: 130, showOverflowTooltip: true },
  { key: 'purchaseOrderCode', label: t('batchReconciliation.columns.purchaseOrderCode'), prop: 'purchaseOrderCode', width: 130, showOverflowTooltip: true },
  { key: 'freightForwarderOrderNo', label: t('batchReconciliation.columns.freightForwarderOrderNo'), prop: 'freightForwarderOrderNo', minWidth: 120, showOverflowTooltip: true },
  { key: 'vendorName', label: t('batchReconciliation.columns.vendorName'), prop: 'vendorName', minWidth: 120, showOverflowTooltip: true },
  { key: 'materialModel', label: t('batchReconciliation.columns.materialModel'), prop: 'materialModel', minWidth: 120, showOverflowTooltip: true },
  { key: 'materialBrand', label: t('batchReconciliation.columns.materialBrand'), prop: 'materialBrand', width: 112, showOverflowTooltip: true },
  { key: 'stockInItemQuantity', label: t('batchReconciliation.columns.stockInItemQuantity'), prop: 'stockInItemQuantity', width: 112, align: 'right' },
  { key: 'batchDimension', label: t('batchReconciliation.columns.batchDimension'), prop: 'batchDimension', width: 112, showOverflowTooltip: true },
  { key: 'batchUnit', label: t('batchReconciliation.columns.batchUnit'), prop: 'batchUnit', width: 148, showOverflowTooltip: true },
  { key: 'unitNo', label: t('batchReconciliation.columns.unitNo'), prop: 'unitNo', width: 112, showOverflowTooltip: true },
  { key: 'batchQty', label: t('batchReconciliation.columns.batchQty'), prop: 'batchQty', width: 112, align: 'right' },
  { key: 'dc', label: t('batchReconciliation.columns.dc'), prop: 'dc', width: 100, showOverflowTooltip: true },
  { key: 'packageOrigin', label: t('batchReconciliation.columns.packageOrigin'), prop: 'packageOrigin', width: 112, showOverflowTooltip: true },
  { key: 'waferOrigin', label: t('batchReconciliation.columns.waferOrigin'), prop: 'waferOrigin', width: 112, showOverflowTooltip: true },
  { key: 'lot', label: t('batchReconciliation.columns.lot'), prop: 'lot', width: 90, showOverflowTooltip: true },
  { key: 'serialNumber', label: t('batchReconciliation.columns.serialNumber'), prop: 'serialNumber', minWidth: 100, showOverflowTooltip: true },
  { key: 'partCode', label: t('batchReconciliation.columns.partCode'), prop: 'partCode', width: 120, minWidth: 112, showOverflowTooltip: true },
  { key: 'packingCode', label: t('batchReconciliation.columns.packingCode'), prop: 'packingCode', width: 130, showOverflowTooltip: true },
  { key: 'customerName', label: t('batchReconciliation.columns.customerName'), prop: 'customerName', minWidth: 120, showOverflowTooltip: true },
  { key: 'stockOutDate', label: t('batchReconciliation.columns.stockOutDate'), prop: 'stockOutDate', width: 110 },
  { key: 'outQty', label: t('batchReconciliation.columns.outQty'), prop: 'outQty', width: 112, minWidth: 108, align: 'right' },
  { key: 'totalOutQty', label: t('batchReconciliation.columns.totalOutQty'), prop: 'totalOutQty', width: 130, minWidth: 120, align: 'right' },
  { key: 'remainingQty', label: t('batchReconciliation.columns.remainingQty'), prop: 'remainingQty', width: 112, minWidth: 108, align: 'right' }
])

const consumptionColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'packingCode', label: t('batchReconciliation.consumption.columns.packingCode'), prop: 'packingCode', minWidth: 140, showOverflowTooltip: true },
  { key: 'outQty', label: t('batchReconciliation.consumption.columns.outQty'), prop: 'outQty', width: 100, align: 'right' },
  { key: 'stockOutDate', label: t('batchReconciliation.consumption.columns.stockOutDate'), prop: 'stockOutDate', width: 120 },
  { key: 'customerName', label: t('batchReconciliation.consumption.columns.customerName'), prop: 'customerName', minWidth: 140, showOverflowTooltip: true }
])

async function fetchList(resetPage = true) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const paged = await batchReconciliationApi.listPaged({
      ...buildQuery(),
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = paged.items.map((row) => ({
      ...row,
      rowKey: `${row.stockInBatchId}:${row.stockOutBatchId ?? ''}`
    }))
    listTotalServer.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('batchReconciliation.messages.loadFailed')))
    list.value = []
    listTotalServer.value = 0
  } finally {
    loading.value = false
  }
}

async function loadConsumption(globalBatchNo: string) {
  consumptionLoading.value = true
  try {
    consumptionList.value = await batchReconciliationApi.getConsumption(globalBatchNo)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('batchReconciliation.messages.consumptionFailed')))
    consumptionList.value = []
  } finally {
    consumptionLoading.value = false
  }
}

function onMainRowClick(row: ListRow) {
  const key = row.globalBatchNo?.trim()
  if (!key) return
  selectedGlobalBatchNo.value = key
  void loadConsumption(key)
}

function closeConsumptionPanel() {
  selectedGlobalBatchNo.value = ''
  consumptionList.value = []
}

function onPageSizeChange() {
  listPage.value = 1
  void fetchList(false)
}

function resetFilters() {
  filters.globalBatchNo = ''
  filters.purchaseOrderCode = ''
  filters.stockInCode = ''
  filters.packingCode = ''
  filters.materialModel = ''
  filters.lot = ''
  filters.serialNumber = ''
  filters.vendorName = ''
  filters.customerName = ''
  filters.remark = ''
  closeConsumptionPanel()
  void fetchList(true)
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

async function exportIn() {
  exporting.value = true
  try {
    const blob = await batchReconciliationApi.exportInBatches(buildQuery())
    downloadBlob(blob, withExportTimestamp('stock-in-batches.csv'))
    ElMessage.success(t('batchReconciliation.messages.exportSuccess'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('batchReconciliation.messages.exportFailed')))
  } finally {
    exporting.value = false
  }
}

async function exportOut() {
  exporting.value = true
  try {
    const blob = await batchReconciliationApi.exportOutBatches(buildQuery())
    downloadBlob(blob, withExportTimestamp('stock-out-batches.csv'))
    ElMessage.success(t('batchReconciliation.messages.exportSuccess'))
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('batchReconciliation.messages.exportFailed')))
  } finally {
    exporting.value = false
  }
}

onMounted(() => {
  void fetchList(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.batch-reconciliation-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 12px;
  flex-wrap: wrap;

  .header-left,
  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

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
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 搜索栏（与 StockInList / CustomerList 一致）----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.search-input {
  width: 160px;
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }

  &--filter {
    width: 160px;
  }

  &--lot,
  &--sn {
    width: 120px;
  }
}

.btn-primary,
.btn-ghost {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  border-radius: $border-radius-md;
  font-size: 12px;
  cursor: pointer;
  font-family: 'Noto Sans SC', sans-serif;
  transition: all 0.2s;
}

.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  color: #fff;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.btn-ghost {
  background: transparent;
  border: 1px solid $border-panel;
  color: $text-muted;

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.btn-export {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  border: 1px solid rgba(230, 162, 60, 0.35);
  background: rgba(230, 162, 60, 0.16);
  color: #c9902e;
  font-weight: 500;
  letter-spacing: 0.3px;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: rgba(230, 162, 60, 0.24);
    border-color: rgba(230, 162, 60, 0.5);
    color: #b8821f;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

/** 《业务列表规范》§3.2：数量字重与字色 */
.inv-list-qty {
  font-weight: 700;
  color: #27292c;
  font-variant-numeric: tabular-nums;
}

html[data-theme='dark'] .inv-list-qty {
  color: $text-primary;
}

.inv-list-dash {
  color: $text-muted;
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.consumption-panel {
  margin-top: 16px;
  padding: 12px 16px 16px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: $layer-2;
}

.consumption-panel__head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  flex-wrap: wrap;
}

.consumption-panel__title {
  font-weight: 600;
  color: $text-primary;
}

.consumption-panel__code {
  font-family: ui-monospace, monospace;
  color: $color-amber;
  font-size: 13px;
}

.consumption-panel__close {
  margin-left: auto;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  padding: 4px 10px;
  font-size: 12px;
  cursor: pointer;
  font-family: 'Noto Sans SC', sans-serif;

  &:hover {
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.3);
  }
}
</style>
