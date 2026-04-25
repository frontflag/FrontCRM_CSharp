<template>
  <div class="stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 5h18M3 12h18M3 19h18" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutNotifyList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockOutNotifyList.count', { count: filteredList.length }) }}</div>
      </div>
    </div>

    <!-- 搜索栏：与客户列表 CustomerList 同一套结构与样式 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="workflowFilter"
          :placeholder="t('stockOutNotifyList.filters.workflowPlaceholder')"
          clearable
          class="status-select status-select--workflow"
          :teleported="false"
        >
          <el-option :label="t('stockOutNotifyList.filters.workflowAll')" value="all" />
          <el-option :label="t('stockOutNotifyList.filters.workflowPendingPick')" value="pending_pick" />
          <el-option :label="t('stockOutNotifyList.filters.workflowPickedPendingOut')" value="picked_pending_out" />
          <el-option :label="t('stockOutNotifyList.filters.workflowDone')" value="done" />
        </el-select>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keyword"
            class="search-input search-input--wide"
            type="search"
            :placeholder="t('stockOutNotifyList.filters.keywordPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('stockOutNotifyList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('stockOutNotifyList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-notify-list-main"
      :columns="stockOutNotifyColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedFilteredList"
      v-loading="loading"
    >
      <template #col-workflow="{ row }">
        <span class="flow-tag" :class="`flow-tag--${workflowTagKey(row)}`">{{ workflowLabel(row) }}</span>
      </template>
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-outQuantity="{ row }">{{ row.outQuantity }}</template>
      <template #col-regionType="{ row }">{{ regionTypeLabel(row) }}</template>
      <template #col-requestDate="{ row }">{{ formatRequestDateTime(row.requestDate) }}</template>
      <template #col-createTime="{ row }">{{ formatRequestDateTime(row.createTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.requestUserName || '--' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('stockOutNotifyList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" v-show="Number(row.status) !== 1" class="action-btns">
            <button type="button" class="action-btn action-btn--warning" @click.stop="goExecute(row)">
              {{ t('stockOutNotifyList.actions.executeStockOut') }}
            </button>
          </div>
          <span v-else-if="Number(row.status) === 1" class="op-done">{{ t('stockOutNotifyList.actions.alreadyShipped') }}</span>

          <el-dropdown v-else trigger="click" placement="bottom-end" v-if="Number(row.status) !== 1">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goExecute(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('stockOutNotifyList.actions.executeStockOut') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
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
        <div class="list-footer-spacer" aria-hidden="true"></div>
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
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { inventoryCenterApi, type PickingTask } from '@/api/inventoryCenter'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t, locale } = useI18n()
const loading = ref(false)
const keyword = ref('')
const workflowFilter = ref<string>('all')
const list = ref<StockOutRequestDto[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const pickingTasks = ref<PickingTask[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 140
const OP_COL_EXPANDED_MIN_WIDTH = 140
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const stockOutNotifyColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
  { key: 'workflow', label: t('stockOutNotifyList.columns.workflow'), width: 130, align: 'center' },
  { key: 'status', label: t('stockOutNotifyList.columns.status'), prop: 'status', width: 110, align: 'center' },
  { key: 'materialModel', label: t('stockOutNotifyList.columns.materialModel'), prop: 'materialModel', width: 180, showOverflowTooltip: true },
  { key: 'brand', label: t('stockOutNotifyList.columns.brand'), prop: 'brand', width: 140, showOverflowTooltip: true },
  { key: 'outQuantity', label: t('stockOutNotifyList.columns.outQuantity'), prop: 'outQuantity', width: 110, align: 'right' },
  {
    key: 'regionType',
    label: t('stockOutNotifyList.columns.regionType'),
    width: 88,
    align: 'center'
  },
  { key: 'requestDate', label: t('stockOutNotifyList.columns.requestDate'), prop: 'requestDate', width: 170 },
  { key: 'salesUserName', label: t('stockOutNotifyList.columns.salesUserName'), prop: 'salesUserName', width: 130, showOverflowTooltip: true },
  { key: 'customerName', label: t('stockOutNotifyList.columns.customer'), prop: 'customerName', minWidth: 180, showOverflowTooltip: true },
  { key: 'remark', label: t('stockOutNotifyList.columns.remark'), prop: 'remark', minWidth: 180, showOverflowTooltip: true },
  { key: 'requestCode', label: t('stockOutNotifyList.columns.requestCode'), prop: 'requestCode', width: 160, minWidth: 160 },
  { key: 'salesOrderCode', label: t('stockOutNotifyList.columns.salesOrderCode'), prop: 'salesOrderCode', width: 160, minWidth: 160 },
  { key: 'createTime', label: t('stockOutNotifyList.columns.createTime'), prop: 'createTime', width: 170 },
  { key: 'createUser', label: t('stockOutNotifyList.columns.createUser'), width: 140, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('stockOutNotifyList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
  ]
})

const statusLabel = (s: number) => {
  if (s === 0) return t('stockOutNotifyList.status.pendingOut')
  if (s === 1) return t('stockOutNotifyList.status.done')
  if (s === 2) return t('stockOutNotifyList.status.cancelled')
  return t('stockOutNotifyList.status.unknown')
}

function hasPickingCompleted(requestId: string): boolean {
  return pickingTasks.value.some((t) => t.stockOutRequestId === requestId && t.status === 100)
}

/** 用于样式：pending_pick | picked | done */
function workflowTagKey(row: StockOutRequestDto): string {
  if (Number(row.status) === 1) return 'done'
  if (hasPickingCompleted(row.id)) return 'picked'
  return 'pending_pick'
}

function workflowLabel(row: StockOutRequestDto): string {
  if (Number(row.status) === 1) return t('stockOutNotifyList.workflow.done')
  if (Number(row.status) === 2) return t('stockOutNotifyList.workflow.cancelled')
  if (hasPickingCompleted(row.id)) return t('stockOutNotifyList.workflow.pickedPendingOut')
  return t('stockOutNotifyList.workflow.pendingPick')
}

const regionTypeLabel = (row: StockOutRequestDto) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

/** 按本地时区显示年月日 + 时分 */
const formatRequestDateTime = (v?: string | null) => {
  if (v == null || v === '') return '--'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

const filteredList = computed(() => {
  let rows = list.value
  const wf = workflowFilter.value || 'all'
  if (wf !== 'all') {
    rows = rows.filter((x) => {
      const st = Number(x.status)
      if (wf === 'done') return st === 1
      if (wf === 'pending_pick') return st === 0 && !hasPickingCompleted(x.id)
      if (wf === 'picked_pending_out') return st === 0 && hasPickingCompleted(x.id)
      return true
    })
  }
  const k = keyword.value.trim().toLowerCase()
  if (!k) return rows
  return rows.filter(
    (x) =>
      (x.requestCode || '').toLowerCase().includes(k) ||
      (x.salesOrderCode || '').toLowerCase().includes(k) ||
      (x.customerName || '').toLowerCase().includes(k)
  )
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

watch(workflowFilter, () => {
  listPage.value = 1
})

async function runNotifyFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const [requests, tasks] = await Promise.all([
      stockOutApi.getRequestList(),
      inventoryCenterApi.getPickingTasks().catch(() => [] as PickingTask[])
    ])
    list.value = requests
    pickingTasks.value = tasks || []
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutNotifyList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  void runNotifyFetch(true)
}

function handleReset() {
  workflowFilter.value = 'all'
  keyword.value = ''
  void runNotifyFetch(true)
}

const goExecute = (row: StockOutRequestDto) => {
  router.push({ path: '/inventory/stock-out/create', query: { requestId: row.id } })
}

onMounted(() => {
  void runNotifyFetch(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-notify-page {
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
.header-left {
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
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
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

  &--wide {
    width: 280px;
  }
}

.status-select {
  width: 120px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.status-select--workflow {
  width: 168px;
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
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-1 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-2 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  &:hover { text-decoration: underline; }
}
.flow-tag {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &--pending_pick {
    background: rgba(255, 193, 7, 0.12);
    color: #ffc107;
  }
  &--picked {
    background: rgba(0, 212, 255, 0.12);
    color: #00d4ff;
  }
  &--done {
    background: rgba(70, 191, 145, 0.15);
    color: #46bf91;
  }
}
.op-done {
  font-size: 12px;
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
</style>
