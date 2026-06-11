<template>
  <div class="stock-in-batch-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="3" width="18" height="18" rx="2" />
              <path d="M8 12h8M8 8h8M8 16h5" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockInBatchList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockInBatchList.count', { count: listTotalServer }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.globalBatchNo"
            class="search-input"
            :placeholder="t('stockInBatchList.filters.globalBatchNoPlaceholder')"
            @keyup.enter="() => void fetchList(true)"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.lot"
            class="search-input"
            :placeholder="t('stockInBatchList.filters.lotPlaceholder')"
            @keyup.enter="() => void fetchList(true)"
          />
        </div>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.serialNumber"
            class="search-input"
            :placeholder="t('stockInBatchList.filters.serialNumberPlaceholder')"
            @keyup.enter="() => void fetchList(true)"
          />
        </div>
        <button type="button" class="btn-primary btn-sm" @click="() => void fetchList(true)">{{ t('stockInBatchList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('stockInBatchList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-in-batch-list-v2"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      @row-dblclick="openEdit"
    />

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotalServer"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void fetchList(false)"
        @size-change="onBatchListPageSizeChange"
      />
    </div>

    <el-dialog
      v-model="editVisible"
      :title="t('stockInBatchList.edit.title')"
      width="640px"
      destroy-on-close
      class="stock-in-batch-edit-dialog"
      @closed="onEditClosed"
    >
      <el-form label-width="120px" label-position="right" v-if="editForm">
        <el-form-item :label="t('stockInBatchList.edit.globalBatchNo')">
          <el-input :model-value="editForm.globalBatchNo || '—'" disabled />
        </el-form-item>

        <el-collapse v-model="collapseActive">
          <el-collapse-item :title="t('stockInBatchList.edit.panel1')" name="1">
            <el-form-item :label="t('stockInBatchList.columns.batchDimension')">
              <el-input v-model="editForm.batchDimension" maxlength="32" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.batchUnit')">
              <el-input v-model="editForm.batchUnit" maxlength="32" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.unitNo')">
              <el-input v-model="editForm.unitNo" maxlength="128" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.batchQty')">
              <el-input-number v-model="editForm.batchQty" :min="0" :controls="true" class="w-full-num" />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.dc')">
              <el-input v-model="editForm.dc" maxlength="64" show-word-limit />
            </el-form-item>
          </el-collapse-item>
          <el-collapse-item :title="t('stockInBatchList.edit.panel2')" name="2">
            <el-form-item :label="t('stockInBatchList.columns.packageOrigin')">
              <el-input v-model="editForm.packageOrigin" maxlength="200" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.waferOrigin')">
              <el-input v-model="editForm.waferOrigin" maxlength="200" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.lot')">
              <el-input v-model="editForm.lot" maxlength="128" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.serialNumber')">
              <el-input v-model="editForm.serialNumber" maxlength="200" show-word-limit />
            </el-form-item>
          </el-collapse-item>
          <el-collapse-item :title="t('stockInBatchList.edit.panel3')" name="3">
            <el-form-item :label="t('stockInBatchList.columns.firmwareVersion')">
              <el-input v-model="editForm.firmwareVersion" maxlength="128" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.partCode')">
              <el-input v-model="editForm.partCode" maxlength="128" show-word-limit />
            </el-form-item>
          </el-collapse-item>
          <el-collapse-item :title="t('stockInBatchList.edit.panel4')" name="4">
            <el-form-item :label="t('stockInBatchList.columns.remark')">
              <el-input v-model="editForm.remark" type="textarea" :rows="4" maxlength="1000" show-word-limit />
            </el-form-item>
          </el-collapse-item>
        </el-collapse>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">{{ t('stockInBatchList.edit.cancel') }}</el-button>
        <el-button type="primary" :loading="saving" @click="saveEdit">{{ t('stockInBatchList.edit.save') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { stockInBatchApi, type StockInBatchRow, type StockInBatchUpdatePayload } from '@/api/stockInBatch'
import { getApiErrorMessage } from '@/utils/apiError'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t } = useI18n()
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const loading = ref(false)
const saving = ref(false)
const list = ref<StockInBatchRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotalServer = ref(0)
watch(listTotalServer, () => {
  const maxP = Math.max(1, Math.ceil(listTotalServer.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

const filters = reactive({
  globalBatchNo: '',
  lot: '',
  serialNumber: ''
})

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'globalBatchNo', label: t('stockInBatchList.columns.globalBatchNo'), prop: 'globalBatchNo', width: 140, showOverflowTooltip: true },
  { key: 'batchDimension', label: t('stockInBatchList.columns.batchDimension'), prop: 'batchDimension', width: 100, showOverflowTooltip: true },
  { key: 'batchUnit', label: t('stockInBatchList.columns.batchUnit'), prop: 'batchUnit', width: 110, showOverflowTooltip: true },
  { key: 'unitNo', label: t('stockInBatchList.columns.unitNo'), prop: 'unitNo', minWidth: 100, showOverflowTooltip: true },
  { key: 'batchQty', label: t('stockInBatchList.columns.batchQty'), prop: 'batchQty', width: 100, align: 'right' },
  { key: 'dc', label: t('stockInBatchList.columns.dc'), prop: 'dc', width: 88, showOverflowTooltip: true },
  { key: 'packageOrigin', label: t('stockInBatchList.columns.packageOrigin'), prop: 'packageOrigin', minWidth: 100, showOverflowTooltip: true },
  { key: 'waferOrigin', label: t('stockInBatchList.columns.waferOrigin'), prop: 'waferOrigin', minWidth: 100, showOverflowTooltip: true },
  { key: 'lot', label: t('stockInBatchList.columns.lot'), prop: 'lot', width: 100, showOverflowTooltip: true },
  { key: 'serialNumber', label: t('stockInBatchList.columns.serialNumber'), prop: 'serialNumber', minWidth: 110, showOverflowTooltip: true },
  { key: 'firmwareVersion', label: t('stockInBatchList.columns.firmwareVersion'), prop: 'firmwareVersion', minWidth: 110, showOverflowTooltip: true },
  { key: 'partCode', label: t('stockInBatchList.columns.partCode'), prop: 'partCode', minWidth: 100, showOverflowTooltip: true },
  { key: 'remark', label: t('stockInBatchList.columns.remark'), prop: 'remark', minWidth: 120, showOverflowTooltip: true }
])

type EditForm = {
  id: string
  globalBatchNo: string
  batchDimension: string
  batchUnit: string
  unitNo: string
  batchQty: number
  dc: string
  packageOrigin: string
  waferOrigin: string
  lot: string
  serialNumber: string
  firmwareVersion: string
  partCode: string
  remark: string
}

const editVisible = ref(false)
const editingId = ref<string | null>(null)
const editForm = ref<EditForm | null>(null)
const collapseActive = ref(['1', '2', '3', '4'])

function str(v: string | null | undefined) {
  return v == null ? '' : String(v)
}

function openEdit(row: StockInBatchRow) {
  editingId.value = row.id
  editForm.value = {
    id: row.id,
    globalBatchNo: str(row.globalBatchNo),
    batchDimension: str(row.batchDimension),
    batchUnit: str(row.batchUnit),
    unitNo: str(row.unitNo),
    batchQty: Number(row.batchQty) || 0,
    dc: str(row.dc),
    packageOrigin: str(row.packageOrigin),
    waferOrigin: str(row.waferOrigin),
    lot: str(row.lot),
    serialNumber: str(row.serialNumber),
    firmwareVersion: str(row.firmwareVersion),
    partCode: str(row.partCode),
    remark: str(row.remark)
  }
  editVisible.value = true
}

function onEditClosed() {
  editingId.value = null
  editForm.value = null
}

async function saveEdit() {
  const id = editingId.value
  const f = editForm.value
  if (!id || !f) return
  saving.value = true
  try {
    const body: StockInBatchUpdatePayload = {
      batchDimension: f.batchDimension.trim() || null,
      batchUnit: f.batchUnit.trim() || null,
      unitNo: f.unitNo.trim() || null,
      batchQty: f.batchQty,
      dc: f.dc.trim() || null,
      packageOrigin: f.packageOrigin.trim() || null,
      waferOrigin: f.waferOrigin.trim() || null,
      lot: f.lot.trim() || null,
      serialNumber: f.serialNumber.trim() || null,
      firmwareVersion: f.firmwareVersion.trim() || null,
      partCode: f.partCode.trim() || null,
      remark: f.remark.trim() || null
    }
    const updated = await stockInBatchApi.update(id, body)
    const idx = list.value.findIndex((x) => x.id === id)
    if (idx >= 0) list.value[idx] = { ...list.value[idx], ...updated }
    ElMessage.success(t('stockInBatchList.messages.saveSuccess'))
    editVisible.value = false
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInBatchList.messages.saveFailed')))
  } finally {
    saving.value = false
  }
}

async function fetchList(resetPage = true) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const paged = await stockInBatchApi.listPaged({
      globalBatchNo: filters.globalBatchNo.trim() || undefined,
      lot: filters.lot.trim() || undefined,
      serialNumber: filters.serialNumber.trim() || undefined,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = paged.items
    listTotalServer.value = paged.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInBatchList.messages.loadFailed')))
    list.value = []
    listTotalServer.value = 0
  } finally {
    loading.value = false
  }
}

function onBatchListPageSizeChange() {
  listPage.value = 1
  void fetchList(false)
}

function resetFilters() {
  filters.globalBatchNo = ''
  filters.lot = ''
  filters.serialNumber = ''
  void fetchList(true)
}

onMounted(() => {
  void fetchList(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stock-in-batch-list-page {
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

  .header-left {
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
}

.page-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: rgba(59, 130, 246, 0.12);
  color: #2563eb;
}

.page-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

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

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 32px;
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
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
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
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

.pagination-wrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 16px;
}

.list-footer-density-anchor {
  display: inline-block;
  width: 1px;
  height: 1px;
}

:deep(.w-full-num) {
  width: 100%;
}
</style>
