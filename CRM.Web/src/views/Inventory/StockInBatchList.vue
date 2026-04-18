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
        <div class="count-badge">{{ t('stockInBatchList.count', { count: listTotal }) }}</div>
      </div>
      <div class="header-right">
        <button type="button" class="btn-header-blue" @click="writeOffVisible = true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" />
            <line x1="5" y1="12" x2="19" y2="12" />
          </svg>
          {{ t('stockInBatchList.writeOff.button') }}
        </button>
      </div>
    </div>

    <StockInBatchWriteOffDialog v-model="writeOffVisible" @success="fetchList" />

    <!-- 搜索栏：与客户列表 /customerlist 同一套结构与样式 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('stockInBatchList.columns.stockInItemCode') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.stockInItemCode"
            class="search-input"
            :placeholder="t('stockInBatchList.filters.stockInItemCodePlaceholder')"
            @keyup.enter="fetchList"
          />
        </div>
        <span class="filter-field-label">{{ t('stockInBatchList.columns.lot') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.lot"
            class="search-input"
            :placeholder="t('stockInBatchList.filters.lotPlaceholder')"
            @keyup.enter="fetchList"
          />
        </div>
        <span class="filter-field-label">{{ t('stockInBatchList.columns.serialNumber') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.serialNumber"
            class="search-input"
            :placeholder="t('stockInBatchList.filters.serialNumberPlaceholder')"
            @keyup.enter="fetchList"
          />
        </div>
        <button type="button" class="btn-primary btn-sm" @click="fetchList">{{ t('stockInBatchList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('stockInBatchList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-in-batch-list"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedList"
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
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="listPage = 1"
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
        <el-form-item :label="t('stockInBatchList.edit.stockInItemCode')">
          <el-input :model-value="editForm.stockInItemCode || '—'" disabled />
        </el-form-item>

        <el-collapse v-model="collapseActive">
          <el-collapse-item :title="t('stockInBatchList.edit.panel1')" name="1">
            <el-form-item :label="t('stockInBatchList.columns.materialModel')">
              <el-input v-model="editForm.materialModel" maxlength="200" show-word-limit />
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
            <el-form-item :label="t('stockInBatchList.columns.lotQtyIn')">
              <el-input-number v-model="editForm.lotQtyIn" :min="0" :controls="true" class="w-full-num" />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.lotQtyOut')">
              <el-input-number v-model="editForm.lotQtyOut" :min="0" :controls="true" class="w-full-num" />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.origin')">
              <el-input v-model="editForm.origin" maxlength="200" show-word-limit />
            </el-form-item>
          </el-collapse-item>
          <el-collapse-item :title="t('stockInBatchList.edit.panel3')" name="3">
            <el-form-item :label="t('stockInBatchList.columns.serialNumber')">
              <el-input v-model="editForm.serialNumber" maxlength="200" show-word-limit />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.snQtyIn')">
              <el-input-number v-model="editForm.snQtyIn" :min="0" :controls="true" class="w-full-num" />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.snQtyOut')">
              <el-input-number v-model="editForm.snQtyOut" :min="0" :controls="true" class="w-full-num" />
            </el-form-item>
            <el-form-item :label="t('stockInBatchList.columns.firmwareVersion')">
              <el-input v-model="editForm.firmwareVersion" maxlength="128" show-word-limit />
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
const writeOffVisible = ref(false)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const loading = ref(false)
const saving = ref(false)
const list = ref<StockInBatchRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const listTotal = computed(() => list.value.length)
const pagedList = computed(() => {
  const start = (listPage.value - 1) * listPageSize.value
  return list.value.slice(start, start + listPageSize.value)
})
watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

const filters = reactive({
  stockInItemCode: '',
  lot: '',
  serialNumber: ''
})

const tableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'stockInItemCode', label: t('stockInBatchList.columns.stockInItemCode'), prop: 'stockInItemCode', width: 160, showOverflowTooltip: true },
  { key: 'materialModel', label: t('stockInBatchList.columns.materialModel'), prop: 'materialModel', minWidth: 120, showOverflowTooltip: true },
  { key: 'dc', label: t('stockInBatchList.columns.dc'), prop: 'dc', width: 88, showOverflowTooltip: true },
  { key: 'packageOrigin', label: t('stockInBatchList.columns.packageOrigin'), prop: 'packageOrigin', minWidth: 100, showOverflowTooltip: true },
  { key: 'waferOrigin', label: t('stockInBatchList.columns.waferOrigin'), prop: 'waferOrigin', minWidth: 100, showOverflowTooltip: true },
  { key: 'lot', label: t('stockInBatchList.columns.lot'), prop: 'lot', width: 100, showOverflowTooltip: true },
  { key: 'lotQtyIn', label: t('stockInBatchList.columns.lotQtyIn'), prop: 'lotQtyIn', width: 100, align: 'right' },
  { key: 'lotQtyOut', label: t('stockInBatchList.columns.lotQtyOut'), prop: 'lotQtyOut', width: 100, align: 'right' },
  { key: 'origin', label: t('stockInBatchList.columns.origin'), prop: 'origin', minWidth: 90, showOverflowTooltip: true },
  { key: 'serialNumber', label: t('stockInBatchList.columns.serialNumber'), prop: 'serialNumber', minWidth: 110, showOverflowTooltip: true },
  { key: 'snQtyIn', label: t('stockInBatchList.columns.snQtyIn'), prop: 'snQtyIn', width: 110, align: 'right' },
  { key: 'snQtyOut', label: t('stockInBatchList.columns.snQtyOut'), prop: 'snQtyOut', width: 110, align: 'right' },
  { key: 'firmwareVersion', label: t('stockInBatchList.columns.firmwareVersion'), prop: 'firmwareVersion', minWidth: 110, showOverflowTooltip: true },
  { key: 'remark', label: t('stockInBatchList.columns.remark'), prop: 'remark', minWidth: 120, showOverflowTooltip: true }
])

type EditForm = {
  id: string
  stockInItemCode: string
  materialModel: string
  dc: string
  packageOrigin: string
  waferOrigin: string
  lot: string
  lotQtyIn: number
  lotQtyOut: number
  origin: string
  serialNumber: string
  snQtyIn: number
  snQtyOut: number
  firmwareVersion: string
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
    stockInItemCode: str(row.stockInItemCode),
    materialModel: str(row.materialModel),
    dc: str(row.dc),
    packageOrigin: str(row.packageOrigin),
    waferOrigin: str(row.waferOrigin),
    lot: str(row.lot),
    lotQtyIn: Number(row.lotQtyIn) || 0,
    lotQtyOut: Number(row.lotQtyOut) || 0,
    origin: str(row.origin),
    serialNumber: str(row.serialNumber),
    snQtyIn: Number(row.snQtyIn) || 0,
    snQtyOut: Number(row.snQtyOut) || 0,
    firmwareVersion: str(row.firmwareVersion),
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
      materialModel: f.materialModel.trim() || null,
      dc: f.dc.trim() || null,
      packageOrigin: f.packageOrigin.trim() || null,
      waferOrigin: f.waferOrigin.trim() || null,
      lot: f.lot.trim() || null,
      lotQtyIn: f.lotQtyIn,
      lotQtyOut: f.lotQtyOut,
      origin: f.origin.trim() || null,
      serialNumber: f.serialNumber.trim() || null,
      snQtyIn: f.snQtyIn,
      snQtyOut: f.snQtyOut,
      firmwareVersion: f.firmwareVersion.trim() || null,
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

async function fetchList() {
  loading.value = true
  try {
    list.value = await stockInBatchApi.list({
      stockInItemCode: filters.stockInItemCode.trim() || undefined,
      lot: filters.lot.trim() || undefined,
      serialNumber: filters.serialNumber.trim() || undefined
    })
    listPage.value = 1
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('stockInBatchList.messages.loadFailed')))
    list.value = []
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  filters.stockInItemCode = ''
  filters.lot = ''
  filters.serialNumber = ''
  void fetchList()
}

onMounted(() => {
  void fetchList()
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

  .header-right {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
    flex-wrap: wrap;
  }
}

/* 与采购订单列表「新建采购订单」同结构（PurchaseOrderList .btn-success），主色改为蓝（同页 .btn-primary 渐变） */
.btn-header-blue {
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

// ---- 搜索栏（与 CustomerList.vue 一致）----
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

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
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
