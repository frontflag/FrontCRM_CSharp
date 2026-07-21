<template>
  <div class="stock-in-batch-panel purchase-order-stock-in-batch-panel">
    <div class="batch-panel-head">
      <h3 class="batch-panel-title">{{ t('purchaseOrderDetail.batchPanel.title') }}</h3>
      <span v-if="activeInnerTab === 'list'" class="batch-panel-count">
        {{ t('purchaseOrderDetail.batchPanel.count', { count: listTotalServer }) }}
      </span>
    </div>

    <div class="batch-panel-tabs">
      <div class="batch-panel-tabs-bar">
        <div class="batch-panel-tabs-nav">
          <button
            type="button"
            class="batch-tab-btn"
            :class="{ 'batch-tab-btn--active': activeInnerTab === 'list' }"
            @click="activeInnerTab = 'list'"
          >
            {{ t('purchaseOrderDetail.batchPanel.tabs.list') }}
          </button>
          <button
            type="button"
            class="batch-tab-btn"
            :class="{ 'batch-tab-btn--active': activeInnerTab === 'logs' }"
            @click="activeInnerTab = 'logs'"
          >
            {{ t('purchaseOrderDetail.batchPanel.tabs.exportLogs') }}
          </button>
        </div>

        <div v-if="activeInnerTab === 'list'" class="batch-panel-toolbar">
          <button type="button" class="btn-export" :disabled="exporting" @click="() => void exportBatches()">
            {{ t('purchaseOrderDetail.batchPanel.actions.export') }}
          </button>
        </div>
      </div>

      <div v-show="activeInnerTab === 'list'" class="batch-panel-body">
        <div class="detail-items-table-wrap">
          <CrmDataTable
            ref="listTableRef"
            column-layout-key="purchase-order-detail-batch-panel-v1"
            :columns="listTableColumns"
            :show-column-settings="false"
            :show-row-density-toggle="false"
            :border="false"
            :data="list"
            v-loading="listLoading"
            embedded
            row-key="rowKey"
            size="small"
            stripe
            class="items-table detail-panel-list-table"
          >
            <template #empty>
              <DetailListPanelEmpty size="low" />
            </template>
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
        </div>

        <div class="batch-panel-pagination">
          <div class="batch-panel-list-footer-left">
            <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
              <el-button
                class="list-settings-btn"
                link
                type="primary"
                :aria-label="t('systemUser.colSetting')"
                @click="listTableRef?.openColumnSettings?.()"
              >
                <el-icon><Setting /></el-icon>
              </el-button>
            </el-tooltip>
          </div>
          <el-pagination
            v-model:current-page="listPage"
            v-model:page-size="listPageSize"
            :total="listTotalServer"
            :page-sizes="[10, 20, 50, 100]"
            layout="total, sizes, prev, pager, next"
            @current-change="() => void fetchList(false)"
            @size-change="onListPageSizeChange"
          />
        </div>
      </div>

      <div v-show="activeInnerTab === 'logs'" class="batch-panel-body">
        <div class="detail-items-table-wrap">
          <CrmDataTable
            column-layout-key="purchase-order-detail-batch-export-logs-v1"
            :columns="logTableColumns"
            :show-column-settings="false"
            :show-row-density-toggle="false"
            :border="false"
            :data="logRows"
            v-loading="logsLoading"
            embedded
            row-key="id"
            size="small"
            stripe
            class="items-table detail-panel-list-table"
          >
            <template #empty>
              <DetailListPanelEmpty size="low" />
            </template>
          <template #col-operationTime="{ row }">
              <template v-for="p in [formatDateTimeParts(row.operationTime)]" :key="row.id + '-t'">
                <span v-if="!p" class="inv-list-dash">—</span>
                <span v-else class="crm-quote-create-time">
                  <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                  <span class="crm-quote-create-time__hm">{{ p.time }}</span>
                </span>
              </template>
            </template>
            <template #col-exportedCount="{ row }">
              <span v-if="row.exportedCount != null && row.exportedCount >= 0">{{ row.exportedCount }}</span>
              <span v-else class="inv-list-dash">—</span>
            </template>
          </CrmDataTable>
        </div>

        <div class="batch-panel-pagination">
          <el-pagination
            v-model:current-page="logsPage"
            v-model:page-size="logsPageSize"
            :total="logsTotal"
            :page-sizes="[10, 20, 50]"
            layout="total, sizes, prev, pager, next"
            @current-change="() => void fetchLogs(false)"
            @size-change="onLogsPageSizeChange"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import { batchReconciliationApi, type BatchReconciliationRow } from '@/api/batchReconciliation'
import { purchaseOrderApi, type PurchaseOrderBatchExportLogRow } from '@/api/purchaseOrder'
import { useBatchReconciliationTableColumns } from '@/composables/useBatchReconciliationTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { withExportTimestamp } from '@/utils/exportFileName'
import { getApiErrorMessage } from '@/utils/apiError'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type ListRow = BatchReconciliationRow & { rowKey: string }

const props = defineProps<{
  purchaseOrderId: string
  purchaseOrderCode: string
}>()

const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { tableColumns } = useBatchReconciliationTableColumns()

const listTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const activeInnerTab = ref<'list' | 'logs'>('list')
const listLoading = ref(false)
const logsLoading = ref(false)
const exporting = ref(false)
const list = ref<ListRow[]>([])
const logRows = ref<PurchaseOrderBatchExportLogRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotalServer = ref(0)
const logsPage = ref(1)
const logsPageSize = ref(20)
const logsTotal = ref(0)

const listTableColumns = computed(() => tableColumns.value)

const logTableColumns = computed<CrmTableColumnDef[]>(() => [
  {
    key: 'operationTime',
    label: t('purchaseOrderDetail.batchPanel.exportLogs.columns.operationTime'),
    prop: 'operationTime',
    width: 130
  },
  {
    key: 'operatorUserName',
    label: t('purchaseOrderDetail.batchPanel.exportLogs.columns.operator'),
    prop: 'operatorUserName',
    width: 100,
    showOverflowTooltip: true
  },
  {
    key: 'exportedCount',
    label: t('purchaseOrderDetail.batchPanel.exportLogs.columns.exportedCount'),
    prop: 'exportedCount',
    width: 100,
    align: 'right'
  },
  {
    key: 'filterSummary',
    label: t('purchaseOrderDetail.batchPanel.exportLogs.columns.filterSummary'),
    prop: 'filterSummary',
    minWidth: 200,
    showOverflowTooltip: true
  },
  {
    key: 'operationDesc',
    label: t('purchaseOrderDetail.batchPanel.exportLogs.columns.operationDesc'),
    prop: 'operationDesc',
    minWidth: 220,
    showOverflowTooltip: true
  }
])

function formatDateTimeParts(v: string | null | undefined) {
  return formatDisplayDateTime2DigitYearParts(v)
}

function isTimeMidnightOnly(time: string) {
  const s = String(time ?? '').trim()
  return !s || s === '00:00' || s === '00:00:00'
}

function formatQtyCell(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return String(n)
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

async function fetchList(resetPage = true) {
  const id = (props.purchaseOrderId ?? '').trim()
  if (!id) {
    list.value = []
    listTotalServer.value = 0
    return
  }
  if (resetPage) listPage.value = 1
  listLoading.value = true
  try {
    const paged = await batchReconciliationApi.listPaged({
      purchaseOrderId: id,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = paged.items.map((row) => ({
      ...row,
      rowKey: `${row.stockInBatchId}:${row.stockOutBatchId ?? ''}`
    }))
    listTotalServer.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('purchaseOrderDetail.batchPanel.messages.loadListFailed')))
    list.value = []
    listTotalServer.value = 0
  } finally {
    listLoading.value = false
  }
}

async function fetchLogs(resetPage = true) {
  const id = (props.purchaseOrderId ?? '').trim()
  if (!id) {
    logRows.value = []
    logsTotal.value = 0
    return
  }
  if (resetPage) logsPage.value = 1
  logsLoading.value = true
  try {
    const paged = await purchaseOrderApi.getBatchExportLogs(id, {
      page: logsPage.value,
      pageSize: logsPageSize.value
    })
    logRows.value = paged.items
    logsTotal.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('purchaseOrderDetail.batchPanel.messages.loadExportLogsFailed')))
    logRows.value = []
    logsTotal.value = 0
  } finally {
    logsLoading.value = false
  }
}

function refreshAll() {
  void fetchList(false)
  void fetchLogs(false)
}

defineExpose({ refresh: refreshAll })

function onListPageSizeChange() {
  listPage.value = 1
  void fetchList(false)
}

function onLogsPageSizeChange() {
  logsPage.value = 1
  void fetchLogs(false)
}

async function exportBatches() {
  const id = (props.purchaseOrderId ?? '').trim()
  const code = (props.purchaseOrderCode ?? '').trim()
  if (!id) return
  try {
    await ElMessageBox.confirm(
      t('purchaseOrderDetail.batchPanel.messages.exportConfirmMessage', {
        code: code || '—',
        count: listTotalServer.value
      }),
      t('purchaseOrderDetail.batchPanel.messages.exportConfirmTitle'),
      {
        type: 'warning',
        confirmButtonText: t('common.confirm'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }
  exporting.value = true
  try {
    const blob = await batchReconciliationApi.exportInBatches({ purchaseOrderId: id, exportSource: 'purchaseOrder' })
    const filename = withExportTimestamp(code ? `${code}-in-batches.csv` : 'purchase-order-in-batches.csv')
    downloadBlob(blob, filename)
    ElMessage.success(t('purchaseOrderDetail.batchPanel.messages.exportSuccess'))
    void fetchLogs(false)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('purchaseOrderDetail.batchPanel.messages.exportFailed')))
  } finally {
    exporting.value = false
  }
}

watch(
  () => props.purchaseOrderId,
  () => {
    void fetchList(true)
    if (activeInnerTab.value === 'logs') void fetchLogs(true)
  }
)

watch(activeInnerTab, (tab) => {
  if (tab === 'logs' && logRows.value.length === 0 && !logsLoading.value) {
    void fetchLogs(true)
  }
})

onMounted(() => {
  void fetchList(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/batch-panel-toolbar.scss';

.stock-in-batch-panel {
  margin-top: 20px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.batch-panel-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px 20px 0;
}

.batch-panel-title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  color: $text-primary;
}

.batch-panel-count {
  font-size: 12px;
  color: $text-muted;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.04);
}

.batch-panel-tabs-bar {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  padding: 0 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.batch-panel-tabs-nav {
  display: flex;
  gap: 4px;
  padding: 12px 0 0;
  flex: 1;
  min-width: 0;
}

.batch-tab-btn {
  padding: 10px 14px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  margin-bottom: -1px;
  &:hover {
    color: $text-secondary;
  }
  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.batch-panel-body {
  padding: 12px 20px 20px;
}

.detail-items-table-wrap {
  margin-top: 4px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  border-radius: 0;
}
</style>
