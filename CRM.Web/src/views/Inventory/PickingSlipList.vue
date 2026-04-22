<template>
  <div class="picking-slip-page stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('pickingSlip.title') }}</h1>
        </div>
        <div class="picking-count-badge">{{ t('pickingSlip.count', { count: filteredList.length }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('pickingSlip.filters.keyword') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keyword"
            class="search-input search-input--picking"
            :placeholder="t('pickingSlip.filters.keywordPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('pickingSlip.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('pickingSlip.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="picking-slip-list-main"
      :columns="columns"
      :show-column-settings="true"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedFilteredList"
      :row-key="rowKey"
      v-loading="loading"
      @row-dblclick="onRowDblClick"
    >
      <template #col-status="{ row }">
        <span class="status-badge">{{ statusLabel(row) }}</span>
      </template>
      <template #col-warehouseDisplay="{ row }">
        <span>{{ displayCell(row, 'warehouseDisplay') }}</span>
      </template>
      <template #col-materialModel="{ row }">
        <span>{{ displayCell(row, 'materialModel') }}</span>
      </template>
      <template #col-brand="{ row }">
        <span>{{ displayCell(row, 'brand') }}</span>
      </template>
      <template #col-customerName="{ row }">
        <span>{{ displayCell(row, 'customerName') }}</span>
      </template>
      <template #col-salesUserName="{ row }">
        <span>{{ displayCell(row, 'salesUserName') }}</span>
      </template>
      <template #col-planQtyTotal="{ row }">
        <span class="qty-cell">{{ Number(displayCell(row, 'planQtyTotal')) || 0 }}</span>
      </template>
      <template #col-lineCount="{ row }">
        <span class="qty-cell">{{ Number(displayCell(row, 'lineCount')) || 0 }}</span>
      </template>
      <template #col-stockOutRequestCode="{ row }">
        <span>{{ displayCell(row, 'stockOutRequestCode') }}</span>
      </template>
      <template #col-taskCode="{ row }">
        <span class="mono-cell">{{ displayCell(row, 'taskCode') }}</span>
      </template>
      <template #col-createTime="{ row }">
        <span>{{ formatCellTime(row) }}</span>
      </template>
      <template #col-createUserDisplay="{ row }">
        <span>{{ displayCell(row, 'createUserDisplay') }}</span>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('stockOutNotifyList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('stockOutNotifyList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="filteredListTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="listPage = 1"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { inventoryCenterApi, type PickingTaskListRow } from '@/api/inventoryCenter'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { getApiErrorMessage } from '@/utils/apiError'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t, locale } = useI18n()
const loading = ref(false)
const keyword = ref('')
const list = ref<PickingTaskListRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

function rowKey(row: PickingTaskListRow) {
  const r = row as unknown as Record<string, unknown>
  return String(r.id ?? r.Id ?? '')
}

function rowRecord(row: PickingTaskListRow) {
  return row as unknown as Record<string, unknown>
}

function displayCell(row: PickingTaskListRow, camel: string) {
  const r = rowRecord(row)
  const pascal = camel.charAt(0).toUpperCase() + camel.slice(1)
  const v = r[camel] ?? r[pascal]
  if (v == null || v === '') return '—'
  return String(v)
}

function formatCellTime(row: PickingTaskListRow) {
  const r = rowRecord(row)
  const raw = (r.createTime ?? r.CreateTime) as string | undefined
  if (!raw) return '—'
  return formatDateTimeZh(raw, 'YYYY-MM-DD HH:mm')
}

const columns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'status', label: t('pickingSlip.columns.status'), width: 100, align: 'center' },
    { key: 'warehouseDisplay', label: t('pickingSlip.columns.warehouse'), minWidth: 160, showOverflowTooltip: true },
    { key: 'materialModel', label: t('pickingSlip.columns.materialModel'), width: 160, minWidth: 140, showOverflowTooltip: true },
    { key: 'brand', label: t('pickingSlip.columns.brand'), width: 120, minWidth: 100, showOverflowTooltip: true },
    { key: 'customerName', label: t('pickingSlip.columns.customerName'), minWidth: 160, showOverflowTooltip: true },
    { key: 'salesUserName', label: t('pickingSlip.columns.salesUserName'), width: 120, showOverflowTooltip: true },
    { key: 'planQtyTotal', label: t('pickingSlip.columns.planQtyTotal'), width: 100, align: 'right' },
    { key: 'lineCount', label: t('pickingSlip.columns.lineCount'), width: 110, align: 'right' },
    { key: 'stockOutRequestCode', label: t('pickingSlip.columns.stockOutRequestCode'), width: 160, minWidth: 140, showOverflowTooltip: true },
    { key: 'taskCode', label: t('pickingSlip.columns.taskCode'), width: 150, minWidth: 130, showOverflowTooltip: true },
    { key: 'createTime', label: t('pickingSlip.columns.createTime'), width: 170 },
    { key: 'createUserDisplay', label: t('pickingSlip.columns.createUser'), width: 120, showOverflowTooltip: true }
  ]
})

const statusLabel = (row: PickingTaskListRow) => {
  const r = rowRecord(row)
  const s = Number(r.status ?? r.Status ?? 0)
  if (s === 1) return t('pickingSlip.status.pending')
  if (s === 2) return t('pickingSlip.status.inProgress')
  if (s === 100) return t('pickingSlip.status.done')
  if (s === -1) return t('pickingSlip.status.cancelled')
  return t('pickingSlip.status.unknown')
}

const filteredList = computed(() => {
  const k = keyword.value.trim().toLowerCase()
  if (!k) return list.value
  return list.value.filter((x) => {
    const r = rowRecord(x)
    const taskCode = String(r.taskCode ?? r.TaskCode ?? '').toLowerCase()
    const notice = String(r.stockOutRequestCode ?? r.StockOutRequestCode ?? '').toLowerCase()
    const cust = String(r.customerName ?? r.CustomerName ?? '').toLowerCase()
    return taskCode.includes(k) || notice.includes(k) || cust.includes(k)
  })
})

const filteredListTotal = computed(() => filteredList.value.length)
const pagedFilteredList = computed(() => {
  const rows = filteredList.value
  const start = (listPage.value - 1) * listPageSize.value
  return rows.slice(start, start + listPageSize.value)
})

watch(filteredListTotal, () => {
  const maxP = Math.max(1, Math.ceil(filteredListTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

watch(keyword, () => {
  listPage.value = 1
})

const onRowDblClick = (row: PickingTaskListRow) => {
  const r = rowRecord(row)
  const id = String(r.id ?? r.Id ?? '').trim()
  if (!id) return
  router.push({ name: 'PickingSlipDetail', params: { id } })
}

async function runPickingFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    list.value = await inventoryCenterApi.getPickingListRows()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('pickingSlip.messages.loadFailed')))
    list.value = []
  } finally {
    loading.value = false
  }
}

const fetchList = () => void runPickingFetch(true)

const handleSearch = () => {
  listPage.value = 1
  void runPickingFetch(false)
}

const handleReset = () => {
  keyword.value = ''
  listPage.value = 1
  void runPickingFetch(false)
}

onMounted(() => void fetchList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.picking-slip-page {
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

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
  }
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
  letter-spacing: 0.5px;
}

.picking-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 搜索栏（与客户列表 search-bar 一致）----
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

  &.search-input--picking {
    width: 280px;
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
.qty-cell {
  font-variant-numeric: tabular-nums;
}
.mono-cell {
  font-family: ui-monospace, monospace;
  font-size: 12px;
}
.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
}
.list-footer-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.list-footer-density-anchor {
  display: inline-block;
  width: 1px;
  height: 1px;
}
.list-footer-spacer {
  flex: 1;
}
.status-badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.06);
  color: $text-primary;
}
</style>
