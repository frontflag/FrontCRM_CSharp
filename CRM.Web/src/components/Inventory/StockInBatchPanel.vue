<template>
  <div class="stock-in-batch-panel">
    <div class="batch-panel-head">
      <h3 class="batch-panel-title">{{ t('stockInDetail.batchPanel.title') }}</h3>
      <span v-if="activeInnerTab === 'list'" class="batch-panel-count">
        {{ t('stockInDetail.batchPanel.count', { count: listTotalServer }) }}
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
            {{ t('stockInDetail.batchPanel.tabs.list') }}
          </button>
          <button
            type="button"
            class="batch-tab-btn"
            :class="{ 'batch-tab-btn--active': activeInnerTab === 'logs' }"
            @click="activeInnerTab = 'logs'"
          >
            {{ t('stockInDetail.batchPanel.tabs.logs') }}
          </button>
        </div>

        <div v-if="canWrite && activeInnerTab === 'list'" class="batch-panel-toolbar">
          <button type="button" class="btn-export" @click="() => void openImportFlow()">
            {{ t('stockInDetail.batchPanel.actions.import') }}
          </button>
          <button type="button" class="btn-export" :disabled="exporting" @click="() => void exportBatches()">
            {{ t('stockInDetail.batchPanel.actions.export') }}
          </button>
          <button type="button" class="btn-secondary btn-secondary--danger" @click="() => void openBulkDeleteFlow()">
            {{ t('stockInDetail.batchPanel.actions.bulkDeleteByItem') }}
          </button>
        </div>
      </div>

      <div v-show="activeInnerTab === 'list'" class="batch-panel-body">
        <div class="detail-items-table-wrap">
          <CrmDataTable
            ref="listTableRef"
            column-layout-key="stock-in-detail-batch-panel-v3"
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
                {{ t('stockInDetail.batchPanel.actions.edit') }}
              </button>
              <button type="button" class="action-btn action-btn--danger" @click="() => void confirmDelete(row)">
                {{ t('stockInDetail.batchPanel.actions.delete') }}
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
            column-layout-key="stock-in-detail-batch-logs-v1"
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
              {{ t('stockInDetail.batchPanel.logs.affectedSummary', { count: row.affectedCount }) }}
              <span v-if="row.batchNosSummary" class="batch-log-nos">{{ row.batchNosSummary }}</span>
            </span>
            <span v-else class="inv-list-dash">—</span>
          </template>
          <template #col-skippedSummary="{ row }">
            <span v-if="row.skippedCount != null && row.skippedCount > 0">
              {{ t('stockInDetail.batchPanel.logs.skippedSummary', { count: row.skippedCount }) }}
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

    <el-dialog
      v-model="itemPickerVisible"
      :title="itemPickerTitle"
      width="480px"
      destroy-on-close
      class="stock-in-batch-item-picker-dialog"
    >
      <p v-if="itemPickerHint" class="item-picker-hint">{{ itemPickerHint }}</p>
      <el-select v-model="pickedItemId" filterable class="item-picker-select" :placeholder="t('stockInDetail.batchPanel.itemPicker.placeholder')">
        <el-option v-for="it in selectableItems" :key="it.itemId" :label="formatItemLabel(it)" :value="it.itemId!" />
      </el-select>
      <template #footer>
        <el-button @click="itemPickerVisible = false">{{ t('stockInDetail.batchPanel.itemPicker.cancel') }}</el-button>
        <el-button type="primary" :disabled="!pickedItemId" @click="confirmItemPicker">
          {{ t('stockInDetail.batchPanel.itemPicker.confirm') }}
        </el-button>
      </template>
    </el-dialog>

    <StockInBatchImportDialog
      v-model="importVisible"
      :stock-in-id="stockInId"
      :stock-in-item-id="importItemId"
      :stock-in-item-code="importItemCode"
      @success="onImportSuccess"
    />

    <StockInBatchEditDialog v-model="editVisible" :batch-id="editBatchId" @saved="onEditSaved" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import VendorNameReadonlyText from '@/components/Vendor/VendorNameReadonlyText.vue'
import StockInBatchImportDialog from '@/components/Inventory/StockInBatchImportDialog.vue'
import StockInBatchEditDialog from '@/components/Inventory/StockInBatchEditDialog.vue'
import {
  batchReconciliationApi,
  type BatchReconciliationRow
} from '@/api/batchReconciliation'
import { stockInBatchApi, type StockInBatchOperationLogRow } from '@/api/stockInBatch'
import type { StockInItemDto } from '@/api/stockIn'
import { useBatchReconciliationTableColumns } from '@/composables/useBatchReconciliationTableColumns'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { withExportTimestamp } from '@/utils/exportFileName'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

type ListRow = BatchReconciliationRow & { rowKey: string }
type ItemPickerMode = 'import' | 'bulkDelete'

const props = defineProps<{
  stockInId: string
  stockInCode: string
  items: StockInItemDto[]
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
const logRows = ref<StockInBatchOperationLogRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotalServer = ref(0)
const logsPage = ref(1)
const logsPageSize = ref(20)
const logsTotal = ref(0)

const itemPickerVisible = ref(false)
const itemPickerMode = ref<ItemPickerMode>('import')
const pickedItemId = ref('')
const importVisible = ref(false)
const importItemId = ref('')
const importItemCode = ref('')
const editVisible = ref(false)
const editBatchId = ref<string | null>(null)

const selectableItems = computed(() =>
  (props.items ?? [])
    .map((it) => ({
      ...it,
      itemId: (it.itemId ?? '').trim()
    }))
    .filter((it) => it.itemId)
)

const itemPickerTitle = computed(() =>
  itemPickerMode.value === 'bulkDelete'
    ? t('stockInDetail.batchPanel.itemPicker.titleBulkDelete')
    : t('stockInDetail.batchPanel.itemPicker.titleImport')
)

const itemPickerHint = computed(() =>
  itemPickerMode.value === 'bulkDelete'
    ? t('stockInDetail.batchPanel.itemPicker.hintBulkDelete')
    : t('stockInDetail.batchPanel.itemPicker.hintImport')
)

const listTableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols = [...tableColumns.value]
  if (canWrite.value) {
    cols.push({
      key: 'actions',
      label: t('stockInDetail.batchPanel.columns.actions'),
      prop: 'actions',
      width: 132,
      fixed: 'right'
    })
  }
  return cols
})

const logTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'operationTime', label: t('stockInDetail.batchPanel.logs.columns.operationTime'), prop: 'operationTime', width: 130 },
  { key: 'actionType', label: t('stockInDetail.batchPanel.logs.columns.actionType'), prop: 'actionType', width: 100 },
  { key: 'stockInItemCode', label: t('stockInDetail.batchPanel.logs.columns.stockInItemCode'), prop: 'stockInItemCode', width: 140, showOverflowTooltip: true },
  { key: 'affectedSummary', label: t('stockInDetail.batchPanel.logs.columns.affectedSummary'), prop: 'affectedSummary', minWidth: 160, showOverflowTooltip: true },
  { key: 'skippedSummary', label: t('stockInDetail.batchPanel.logs.columns.skippedSummary'), prop: 'skippedSummary', minWidth: 140, showOverflowTooltip: true },
  { key: 'reason', label: t('stockInDetail.batchPanel.logs.columns.reason'), prop: 'reason', minWidth: 120, showOverflowTooltip: true },
  { key: 'operatorUserName', label: t('stockInDetail.batchPanel.logs.columns.operator'), prop: 'operatorUserName', width: 100, showOverflowTooltip: true },
  { key: 'operationDesc', label: t('stockInDetail.batchPanel.logs.columns.operationDesc'), prop: 'operationDesc', minWidth: 180, showOverflowTooltip: true },
  { key: 'filterSummary', label: t('stockInDetail.batchPanel.logs.columns.filterSummary'), prop: 'filterSummary', minWidth: 200, showOverflowTooltip: true }
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

function formatItemLabel(it: StockInItemDto) {
  const code = (it.stockInItemCode ?? '').trim() || '—'
  const model = (it.materialName ?? '').trim()
  return model ? `${code} · ${model}` : code
}

function actionTypeLabel(actionType: string) {
  const key = String(actionType ?? '').trim()
  const map: Record<string, string> = {
    StockInBatchImport: t('stockInDetail.batchPanel.logs.actionTypes.import'),
    StockInBatchDelete: t('stockInDetail.batchPanel.logs.actionTypes.delete'),
    StockInBatchBulkDelete: t('stockInDetail.batchPanel.logs.actionTypes.bulkDelete'),
    StockInBatchUpdate: t('stockInDetail.batchPanel.logs.actionTypes.update'),
    StockInBatchExport: t('stockInDetail.batchPanel.logs.actionTypes.export')
  }
  return map[key] ?? (key || '—')
}

async function fetchList(resetPage = true) {
  const code = (props.stockInCode ?? '').trim()
  if (!code) {
    list.value = []
    listTotalServer.value = 0
    return
  }
  if (resetPage) listPage.value = 1
  listLoading.value = true
  try {
    const paged = await batchReconciliationApi.listPaged({
      stockInCode: code,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = paged.items.map((row) => ({
      ...row,
      rowKey: `${row.stockInBatchId}:${row.stockOutBatchId ?? ''}`
    }))
    listTotalServer.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInDetail.batchPanel.messages.loadListFailed')))
    list.value = []
    listTotalServer.value = 0
  } finally {
    listLoading.value = false
  }
}

async function fetchLogs(resetPage = true) {
  const id = (props.stockInId ?? '').trim()
  if (!id) {
    logRows.value = []
    logsTotal.value = 0
    return
  }
  if (resetPage) logsPage.value = 1
  logsLoading.value = true
  try {
    const paged = await stockInBatchApi.getBatchOperationLogs(id, {
      page: logsPage.value,
      pageSize: logsPageSize.value
    })
    logRows.value = paged.items
    logsTotal.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInDetail.batchPanel.messages.loadLogsFailed')))
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
  const code = (props.stockInCode ?? '').trim()
  if (!code) return
  try {
    await ElMessageBox.confirm(
      t('stockInDetail.batchPanel.messages.exportConfirmMessage', {
        code,
        count: listTotalServer.value
      }),
      t('stockInDetail.batchPanel.messages.exportConfirmTitle'),
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
    const stockInId = (props.stockInId ?? '').trim()
    const blob = await batchReconciliationApi.exportInBatches({
      stockInCode: code,
      exportSource: 'stockIn',
      ...(stockInId ? { exportPageUrl: `/inventory/stock-in/${stockInId}` } : {})
    })
    downloadBlob(blob, withExportTimestamp(`stock-in-batches-${code}.csv`))
    ElMessage.success(t('stockInDetail.batchPanel.messages.exportSuccess'))
    void fetchLogs(false)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInDetail.batchPanel.messages.exportFailed')))
  } finally {
    exporting.value = false
  }
}

function resolveSingleItem(): StockInItemDto | null {
  if (selectableItems.value.length === 1) return selectableItems.value[0]!
  return null
}

function openImportFlow() {
  const only = resolveSingleItem()
  if (only) {
    startImport(only)
    return
  }
  if (!selectableItems.value.length) {
    ElMessage.warning(t('stockInDetail.batchPanel.messages.noItems'))
    return
  }
  itemPickerMode.value = 'import'
  pickedItemId.value = ''
  itemPickerVisible.value = true
}

function startImport(item: StockInItemDto) {
  importItemId.value = (item.itemId ?? '').trim()
  importItemCode.value = (item.stockInItemCode ?? '').trim()
  importVisible.value = true
}

function openBulkDeleteFlow() {
  if (!selectableItems.value.length) {
    ElMessage.warning(t('stockInDetail.batchPanel.messages.noItems'))
    return
  }
  itemPickerMode.value = 'bulkDelete'
  pickedItemId.value = ''
  itemPickerVisible.value = true
}

function confirmItemPicker() {
  const id = pickedItemId.value.trim()
  const item = selectableItems.value.find((it) => it.itemId === id)
  if (!item) return
  itemPickerVisible.value = false
  if (itemPickerMode.value === 'import') {
    startImport(item)
  } else {
    void runBulkDelete(item)
  }
}

async function promptDeleteReason(title: string): Promise<string | null> {
  try {
    const ret = await ElMessageBox.prompt(
      t('stockInDetail.batchPanel.prompts.reasonHint'),
      title,
      {
        confirmButtonText: t('stockInDetail.batchPanel.prompts.confirm'),
        cancelButtonText: t('stockInDetail.batchPanel.prompts.cancel'),
        inputPlaceholder: t('stockInDetail.batchPanel.prompts.reasonPlaceholder'),
        inputValidator: (v) => {
          if (!String(v ?? '').trim()) return t('stockInDetail.batchPanel.prompts.reasonRequired')
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
  const batchId = (row.stockInBatchId ?? '').trim()
  const batchNo = (row.globalBatchNo ?? '').trim()
  if (!batchId) return
  const reason = await promptDeleteReason(t('stockInDetail.batchPanel.prompts.deleteTitle', { batchNo: batchNo || batchId }))
  if (!reason) return
  try {
    await stockInBatchApi.softDelete(batchId, reason)
    ElMessage.success(t('stockInDetail.batchPanel.messages.deleteSuccess'))
    refreshAll()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInDetail.batchPanel.messages.deleteFailed')))
  }
}

async function runBulkDelete(item: StockInItemDto) {
  const itemId = (item.itemId ?? '').trim()
  const itemCode = (item.stockInItemCode ?? '').trim() || itemId
  const reason = await promptDeleteReason(
    t('stockInDetail.batchPanel.prompts.bulkDeleteTitle', { itemCode })
  )
  if (!reason) return
  try {
    const result = await stockInBatchApi.bulkDeleteByItem(itemId, reason)
    ElMessage.success(
      t('stockInDetail.batchPanel.messages.bulkDeleteSuccess', { count: result.deletedCount })
    )
    refreshAll()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInDetail.batchPanel.messages.bulkDeleteFailed')))
  }
}

function openEdit(row: ListRow) {
  const id = (row.stockInBatchId ?? '').trim()
  if (!id) return
  editBatchId.value = id
  editVisible.value = true
}

function onImportSuccess() {
  refreshAll()
}

function onEditSaved() {
  refreshAll()
}

watch(
  () => props.stockInCode,
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

// §7.4 表头/表体基线见 detail-panel-list-table.scss；此处仅面板内 CrmDataTable 扩展
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

.item-picker-hint {
  margin: 0 0 12px;
  font-size: 13px;
  color: $text-secondary;
  line-height: 1.5;
}

.item-picker-select {
  width: 100%;
}

.inv-list-dash {
  color: $text-muted;
}

.inv-list-qty {
  font-variant-numeric: tabular-nums;
}
</style>
