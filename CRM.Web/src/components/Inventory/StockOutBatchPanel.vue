<template>
  <div class="stock-out-batch-panel">
    <div class="batch-panel-head">
      <h3 class="batch-panel-title">{{ t('packingDetail.batchPanel.title') }}</h3>
      <span v-if="activeInnerTab === 'list'" class="batch-panel-count">
        {{ t('packingDetail.batchPanel.count', { count: listTotalServer }) }}
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
            {{ t('packingDetail.batchPanel.tabs.list') }}
          </button>
          <button
            type="button"
            class="batch-tab-btn"
            :class="{ 'batch-tab-btn--active': activeInnerTab === 'logs' }"
            @click="activeInnerTab = 'logs'"
          >
            {{ t('packingDetail.batchPanel.tabs.logs') }}
          </button>
        </div>

        <div v-if="canWrite && activeInnerTab === 'list'" class="batch-panel-toolbar">
          <button type="button" class="btn-export" @click="importVisible = true">
            {{ t('packingDetail.batchPanel.actions.import') }}
          </button>
          <button type="button" class="btn-export" :disabled="exporting" @click="() => void exportBatches()">
            {{ t('packingDetail.batchPanel.actions.export') }}
          </button>
          <button type="button" class="btn-secondary btn-secondary--danger" @click="() => void openBulkDeleteFlow()">
            {{ t('packingDetail.batchPanel.actions.bulkDelete') }}
          </button>
        </div>
      </div>

      <div v-show="activeInnerTab === 'list'" class="batch-panel-body">
        <div class="detail-items-table-wrap">
          <CrmDataTable
            ref="listTableRef"
            column-layout-key="packing-detail-batch-panel-v1"
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
            @row-dblclick="onBatchRowDblClick"
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
            <template v-if="canWrite" #col-actions="{ row }">
              <div class="batch-panel-row-actions" @click.stop>
                <button type="button" class="action-btn action-btn--ghost" @click="openEdit(row)">
                  {{ t('packingDetail.batchPanel.actions.edit') }}
                </button>
                <button type="button" class="action-btn action-btn--danger" @click="() => void confirmDelete(row)">
                  {{ t('packingDetail.batchPanel.actions.delete') }}
                </button>
              </div>
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
            column-layout-key="packing-detail-batch-logs-v1"
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
            <template #col-operationTime="{ row }">
              <template v-for="p in [formatDateTimeParts(row.operationTime)]" :key="row.id + '-t'">
                <span v-if="!p" class="inv-list-dash">—</span>
                <span v-else class="crm-quote-create-time">
                  <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
                  <span class="crm-quote-create-time__hm">{{ p.time }}</span>
                </span>
              </template>
            </template>
            <template #col-actionType="{ row }">
              {{ actionTypeLabel(row.actionType) }}
            </template>
            <template #col-affectedSummary="{ row }">
              <span v-if="row.affectedCount != null && row.affectedCount > 0">
                {{ t('packingDetail.batchPanel.logs.affectedSummary', { count: row.affectedCount }) }}
                <span v-if="row.batchNosSummary" class="batch-log-nos">{{ row.batchNosSummary }}</span>
              </span>
              <span v-else class="inv-list-dash">—</span>
            </template>
            <template #col-skippedSummary="{ row }">
              <span v-if="row.skippedCount != null && row.skippedCount > 0">
                {{ t('packingDetail.batchPanel.logs.skippedSummary', { count: row.skippedCount }) }}
                <span v-if="row.skippedBatchNosSummary" class="batch-log-nos">{{ row.skippedBatchNosSummary }}</span>
              </span>
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

    <StockOutBatchImportDialog
      v-model="importVisible"
      :packing-id="packingId"
      :packing-code="packingCode"
      @success="onImportSuccess"
    />

    <StockOutBatchEditDialog v-model="editVisible" :batch-id="editBatchId" @saved="onEditSaved" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import StockOutBatchImportDialog from '@/components/Inventory/StockOutBatchImportDialog.vue'
import StockOutBatchEditDialog from '@/components/Inventory/StockOutBatchEditDialog.vue'
import { batchReconciliationApi, type BatchReconciliationRow } from '@/api/batchReconciliation'
import { stockOutBatchApi, type StockOutBatchOperationLogRow } from '@/api/stockOutBatch'
import { useBatchReconciliationTableColumns } from '@/composables/useBatchReconciliationTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { getApiErrorMessage } from '@/utils/apiError'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { withExportTimestamp } from '@/utils/exportFileName'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type ListRow = BatchReconciliationRow & { rowKey: string }

const props = defineProps<{
  packingId: string
  packingCode: string
  canWrite?: boolean
}>()

const { t } = useI18n()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { tableColumns } = useBatchReconciliationTableColumns()

const canWrite = computed(() => props.canWrite !== false)

const listTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const activeInnerTab = ref<'list' | 'logs'>('list')
const listLoading = ref(false)
const logsLoading = ref(false)
const exporting = ref(false)
const list = ref<ListRow[]>([])
const logRows = ref<StockOutBatchOperationLogRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotalServer = ref(0)
const logsPage = ref(1)
const logsPageSize = ref(20)
const logsTotal = ref(0)

const importVisible = ref(false)
const editVisible = ref(false)
const editBatchId = ref<string | null>(null)

const listTableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols = [...tableColumns.value]
  if (canWrite.value) {
    cols.push({
      key: 'actions',
      label: t('packingDetail.batchPanel.columns.actions'),
      prop: 'actions',
      width: 132,
      fixed: 'right'
    })
  }
  return cols
})

const logTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'operationTime', label: t('packingDetail.batchPanel.logs.columns.operationTime'), prop: 'operationTime', width: 130 },
  { key: 'actionType', label: t('packingDetail.batchPanel.logs.columns.actionType'), prop: 'actionType', width: 100 },
  { key: 'packingCode', label: t('packingDetail.batchPanel.logs.columns.packingCode'), prop: 'packingCode', width: 130, showOverflowTooltip: true },
  { key: 'affectedSummary', label: t('packingDetail.batchPanel.logs.columns.affectedSummary'), prop: 'affectedSummary', minWidth: 160, showOverflowTooltip: true },
  { key: 'skippedSummary', label: t('packingDetail.batchPanel.logs.columns.skippedSummary'), prop: 'skippedSummary', minWidth: 140, showOverflowTooltip: true },
  { key: 'reason', label: t('packingDetail.batchPanel.logs.columns.reason'), prop: 'reason', minWidth: 120, showOverflowTooltip: true },
  { key: 'operatorUserName', label: t('packingDetail.batchPanel.logs.columns.operator'), prop: 'operatorUserName', width: 100, showOverflowTooltip: true },
  { key: 'operationDesc', label: t('packingDetail.batchPanel.logs.columns.operationDesc'), prop: 'operationDesc', minWidth: 180, showOverflowTooltip: true },
  { key: 'filterSummary', label: t('packingDetail.batchPanel.logs.columns.filterSummary'), prop: 'filterSummary', minWidth: 200, showOverflowTooltip: true }
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
  return n.toLocaleString('zh-CN')
}

function actionTypeLabel(actionType: string) {
  const key = String(actionType ?? '').trim()
  const map: Record<string, string> = {
    StockOutBatchImport: t('packingDetail.batchPanel.logs.actionTypes.import'),
    StockOutBatchDelete: t('packingDetail.batchPanel.logs.actionTypes.delete'),
    StockOutBatchBulkDelete: t('packingDetail.batchPanel.logs.actionTypes.bulkDelete'),
    StockOutBatchUpdate: t('packingDetail.batchPanel.logs.actionTypes.update'),
    StockOutBatchExport: t('packingDetail.batchPanel.logs.actionTypes.export')
  }
  return map[key] ?? (key || '—')
}

async function fetchList(resetPage = true) {
  const code = (props.packingCode ?? '').trim()
  if (!code) {
    list.value = []
    listTotalServer.value = 0
    return
  }
  if (resetPage) listPage.value = 1
  listLoading.value = true
  try {
    const paged = await batchReconciliationApi.listPaged({
      packingId: (props.packingId ?? '').trim() || undefined,
      packingCode: code,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = paged.items.map((row) => ({
        ...row,
        rowKey: `${row.stockInBatchId}:${row.stockOutBatchId ?? ''}`
      }))
    listTotalServer.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingDetail.batchPanel.messages.loadListFailed')))
    list.value = []
    listTotalServer.value = 0
  } finally {
    listLoading.value = false
  }
}

async function fetchLogs(resetPage = true) {
  const id = (props.packingId ?? '').trim()
  if (!id) {
    logRows.value = []
    logsTotal.value = 0
    return
  }
  if (resetPage) logsPage.value = 1
  logsLoading.value = true
  try {
    const paged = await stockOutBatchApi.getBatchOperationLogs(id, {
      page: logsPage.value,
      pageSize: logsPageSize.value
    })
    logRows.value = paged.items.map((row) => ({
      ...row,
      packingCode: row.packingCode ?? row.recordCode ?? null
    }))
    logsTotal.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingDetail.batchPanel.messages.loadLogsFailed')))
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

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

async function exportBatches() {
  const code = (props.packingCode ?? '').trim()
  const pid = (props.packingId ?? '').trim()
  if (!code || !pid) return
  try {
    await ElMessageBox.confirm(
      t('packingDetail.batchPanel.messages.exportConfirmMessage', {
        code,
        count: listTotalServer.value
      }),
      t('packingDetail.batchPanel.messages.exportConfirmTitle'),
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
    const blob = await batchReconciliationApi.exportOutBatches({
      packingId: pid,
      packingCode: code,
      exportSource: 'packing'
    })
    downloadBlob(blob, withExportTimestamp(`stock-out-batches-${code}.csv`))
    ElMessage.success(t('packingDetail.batchPanel.messages.exportSuccess'))
    void fetchLogs(false)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingDetail.batchPanel.messages.exportFailed')))
  } finally {
    exporting.value = false
  }
}

async function promptDeleteReason(title: string): Promise<string | null> {
  try {
    const ret = await ElMessageBox.prompt(
      t('packingDetail.batchPanel.prompts.reasonHint'),
      title,
      {
        confirmButtonText: t('packingDetail.batchPanel.prompts.confirm'),
        cancelButtonText: t('packingDetail.batchPanel.prompts.cancel'),
        inputPlaceholder: t('packingDetail.batchPanel.prompts.reasonPlaceholder'),
        inputValidator: (v) => {
          if (!String(v ?? '').trim()) return t('packingDetail.batchPanel.prompts.reasonRequired')
          return true
        }
      }
    )
    return String(ret.value ?? '').trim()
  } catch {
    return null
  }
}

async function confirmDelete(row: ListRow) {
  const batchId = (row.stockOutBatchId ?? '').trim()
  const batchNo = (row.globalBatchNo ?? '').trim()
  if (!batchId) return
  const reason = await promptDeleteReason(t('packingDetail.batchPanel.prompts.deleteTitle', { batchNo: batchNo || batchId }))
  if (!reason) return
  try {
    await stockOutBatchApi.softDelete(batchId, reason)
    ElMessage.success(t('packingDetail.batchPanel.messages.deleteSuccess'))
    refreshAll()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingDetail.batchPanel.messages.deleteFailed')))
  }
}

async function openBulkDeleteFlow() {
  const pid = (props.packingId ?? '').trim()
  const code = (props.packingCode ?? '').trim() || pid
  if (!pid) return
  const reason = await promptDeleteReason(t('packingDetail.batchPanel.prompts.bulkDeleteTitle', { packingCode: code }))
  if (!reason) return
  try {
    const result = await stockOutBatchApi.bulkDeleteByPacking(pid, reason)
    ElMessage.success(t('packingDetail.batchPanel.messages.bulkDeleteSuccess', { count: result.deletedCount }))
    refreshAll()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingDetail.batchPanel.messages.bulkDeleteFailed')))
  }
}

function openEdit(row: ListRow) {
  const id = (row.stockOutBatchId ?? '').trim()
  if (!id) return
  editBatchId.value = id
  editVisible.value = true
}

function onBatchRowDblClick(row: ListRow, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: props.canWrite,
    onEdit: openEdit,
  })
}

function onImportSuccess() {
  refreshAll()
}

function onEditSaved() {
  refreshAll()
}

watch(
  () => props.packingCode,
  () => {
    void fetchList(true)
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

.stock-out-batch-panel {
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

  :deep(.el-table) {
    color: var(--crm-table-text);
  }

  :deep(.el-table__inner-wrapper) {
    background: transparent;

    &::before,
    &::after {
      display: none !important;
    }
  }

  :deep(.el-table__border-left-patch) {
    display: none !important;
  }

  :deep(.el-table__cell) {
    .cell {
      white-space: nowrap;
    }
  }
}

.batch-panel-row-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  justify-content: center;
}

.action-btn {
  padding: 2px 8px;
  font-size: 12px;
  border-radius: 4px;
  border: 1px solid transparent;
  cursor: pointer;
  background: transparent;
  &--ghost {
    color: $cyan-primary;
    border-color: rgba(0, 212, 255, 0.35);
  }
  &--danger {
    color: #f56c6c;
    border-color: rgba(245, 108, 108, 0.35);
  }
}

.batch-log-nos {
  display: block;
  font-size: 11px;
  color: $text-muted;
  margin-top: 2px;
}

.inv-list-dash {
  color: $text-muted;
}

.inv-list-qty {
  font-variant-numeric: tabular-nums;
}
</style>
