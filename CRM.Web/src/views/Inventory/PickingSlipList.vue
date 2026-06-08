<template>
  <div class="picking-slip-page">
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
        <div class="count-badge">{{ t('pickingSlip.count', { count: list.length }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filterForm.status"
          :placeholder="t('pickingSlip.filters.status')"
          clearable
          class="filter-select filter-select--status"
          :teleported="false"
        >
          <el-option
            v-for="opt in statusFilterOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select
          v-model="filterForm.warehouseId"
          :placeholder="t('pickingSlip.filters.warehouse')"
          clearable
          filterable
          class="filter-select filter-select--warehouse"
          :teleported="false"
        >
          <el-option
            v-for="wh in warehouseOptions"
            :key="wh.id"
            :label="warehouseOptionLabel(wh)"
            :value="wh.id"
          />
        </el-select>
        <input
          v-model="filterForm.taskCode"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('pickingSlip.filters.taskCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.packingCode"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('pickingSlip.filters.packingCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.stockOutRequestCode"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('pickingSlip.filters.stockOutRequestCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.freightForwarderOrderNo"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.materialModel"
          class="search-input search-input--pn"
          type="search"
          :placeholder="t('pickingSlip.filters.materialModel')"
          @keyup.enter="handleSearch"
        />
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filterForm.customerName"
          class="search-input search-input--customer"
          type="search"
          :placeholder="t('pickingSlip.filters.customerName')"
          @keyup.enter="handleSearch"
        />
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filterForm.salesUserName"
          class="search-input search-input--sales"
          type="search"
          :placeholder="t('pickingSlip.filters.salesUserName')"
          @keyup.enter="handleSearch"
        />
        <el-date-picker
          v-model="filterForm.createDateRange"
          type="daterange"
          :range-separator="t('pickingSlip.filters.dateSep')"
          :start-placeholder="t('pickingSlip.filters.dateFrom')"
          :end-placeholder="t('pickingSlip.filters.dateTo')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('pickingSlip.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('pickingSlip.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="picking-slip-list-main"
      :columns="columns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedList"
      :row-key="rowKey"
      v-loading="loading"
      @row-dblclick="onRowDblClick"
    >
      <template #col-actions-header>
        <div class="list-op-col-header--icon-only">
          <button
            type="button"
            class="op-col-toggle-btn list-op-col-toggle"
            :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
            @click.stop="toggleOpCol"
          >
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
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
      <template #col-packingCode="{ row }">
        <router-link
          v-if="packingLinkId(row)"
          class="picking-link mono-cell"
          :to="`/inventory/packing/${packingLinkId(row)}`"
        >
          {{ displayCell(row, 'packingCode') }}
        </router-link>
        <span v-else>{{ displayCell(row, 'packingCode') }}</span>
      </template>
      <template #col-freightForwarderOrderNo="{ row }">
        <span>{{ displayCell(row, 'freightForwarderOrderNo') }}</span>
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
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <button type="button" class="action-btn action-btn--primary" @click.stop="goDetail(row)">详情</button>
            <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
            <button v-if="isSysAdmin" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteRow(row)">强制删除</button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">详情</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" divided @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="isSysAdmin" @click.stop="handleForceDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
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
        <div class="list-footer-spacer" aria-hidden="true" />
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import {
  inventoryCenterApi,
  type PickingTaskListQuery,
  type PickingTaskListRow,
  type WarehouseInfo
} from '@/api/inventoryCenter'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import { getApiErrorMessage } from '@/utils/apiError'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const loading = ref(false)
const list = ref<PickingTaskListRow[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const warehouseOptions = ref<WarehouseInfo[]>([])

const filterForm = reactive({
  status: undefined as number | undefined,
  warehouseId: '',
  taskCode: '',
  packingCode: '',
  stockOutRequestCode: '',
  freightForwarderOrderNo: '',
  materialModel: '',
  customerName: '',
  salesUserName: '',
  createDateRange: null as [string, string] | null
})

const statusFilterOptions = computed(() => [
  { value: 1, label: t('pickingSlip.status.pending') },
  { value: 2, label: t('pickingSlip.status.inProgress') },
  { value: 100, label: t('pickingSlip.status.done') },
  { value: -1, label: t('pickingSlip.status.cancelled') }
])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))

function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

function rowKey(row: PickingTaskListRow) {
  const r = row as unknown as Record<string, unknown>
  return String(r.id ?? r.Id ?? '')
}

function rowRecord(row: PickingTaskListRow) {
  return row as unknown as Record<string, unknown>
}

function displayCell(row: PickingTaskListRow, camel: string) {
  if (maskSaleSensitiveFields.value && (camel === 'customerName' || camel === 'salesUserName')) return '—'
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
    { key: 'packingCode', label: t('pickingSlip.columns.packingCode'), width: 150, minWidth: 130, showOverflowTooltip: true },
    {
      key: 'freightForwarderOrderNo',
      label: t('common.freightForwarderOrderNo'),
      prop: 'freightForwarderOrderNo',
      width: 160,
      minWidth: 140,
      showOverflowTooltip: true
    },
    { key: 'taskCode', label: t('pickingSlip.columns.taskCode'), width: 150, minWidth: 130, showOverflowTooltip: true },
    { key: 'createTime', label: t('pickingSlip.columns.createTime'), width: 170 },
    { key: 'createUserDisplay', label: t('pickingSlip.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: '操作',
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col',
    resizable: false
    }
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

function warehouseOptionLabel(wh: WarehouseInfo) {
  const name = (wh.warehouseName || '').trim()
  const code = (wh.warehouseCode || '').trim()
  if (name && code) return `${name}（${code}）`
  return name || code || wh.id
}

function packingLinkId(row: PickingTaskListRow) {
  const r = rowRecord(row)
  return String(r.packingId ?? r.PackingId ?? '').trim() || ''
}

function buildListQuery(): PickingTaskListQuery {
  const q: PickingTaskListQuery = {}
  if (filterForm.status != null && !Number.isNaN(Number(filterForm.status))) q.status = Number(filterForm.status)
  const wh = filterForm.warehouseId?.trim()
  if (wh) q.warehouseId = wh
  const tc = filterForm.taskCode?.trim()
  if (tc) q.taskCode = tc
  const pc = filterForm.packingCode?.trim()
  if (pc) q.packingCode = pc
  const nc = filterForm.stockOutRequestCode?.trim()
  if (nc) q.stockOutRequestCode = nc
  const ff = filterForm.freightForwarderOrderNo?.trim()
  if (ff) q.freightForwarderOrderNo = ff
  const pn = filterForm.materialModel?.trim()
  if (pn) q.materialModel = pn
  if (!maskSaleSensitiveFields.value) {
    const cn = filterForm.customerName?.trim()
    if (cn) q.customerName = cn
    const su = filterForm.salesUserName?.trim()
    if (su) q.salesUserName = su
  }
  if (filterForm.createDateRange?.length === 2) {
    q.createTimeFrom = filterForm.createDateRange[0]
    q.createTimeTo = filterForm.createDateRange[1]
  }
  return q
}

function resetFilterForm() {
  filterForm.status = undefined
  filterForm.warehouseId = ''
  filterForm.taskCode = ''
  filterForm.packingCode = ''
  filterForm.stockOutRequestCode = ''
  filterForm.freightForwarderOrderNo = ''
  filterForm.materialModel = ''
  filterForm.customerName = ''
  filterForm.salesUserName = ''
  filterForm.createDateRange = null
}

const listTotal = computed(() => list.value.length)
const pagedList = computed(() => {
  const rows = list.value
  const start = (listPage.value - 1) * listPageSize.value
  return rows.slice(start, start + listPageSize.value)
})

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

const onRowDblClick = (row: PickingTaskListRow) => {
  goDetail(row)
}

const goDetail = (row: PickingTaskListRow) => {
  const r = rowRecord(row)
  const id = String(r.id ?? r.Id ?? '').trim()
  if (!id) return
  router.push({ name: 'PickingSlipDetail', params: { id } })
}

const handleDeleteRow = async (row: PickingTaskListRow) => {
  const r = rowRecord(row)
  const id = String(r.id ?? r.Id ?? '').trim()
  const code = String(r.taskCode ?? r.TaskCode ?? '').trim()
  if (!id) return
  try {
    await ElMessageBox.confirm(`确认删除拣货单 ${code || id} 吗？`, '删除确认', { type: 'warning' })
  } catch {
    return
  }
  try {
    await inventoryCenterApi.deletePickingSlip(id)
    ElMessage.success('删除成功')
    fetchList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '删除失败'))
  }
}

const handleForceDeleteRow = async (row: PickingTaskListRow) => {
  const r = rowRecord(row)
  const id = String(r.id ?? r.Id ?? '').trim()
  const code = String(r.taskCode ?? r.TaskCode ?? '').trim()
  if (!id || !code) return
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt('请输入拣货单号以确认强制删除', '强制删除确认', {
      inputPlaceholder: code
    })
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered !== code) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await inventoryCenterApi.forceDeletePickingSlip(id, entered)
    ElMessage.success('强制删除成功')
    fetchList()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '强制删除失败'))
  }
}

async function runPickingFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    list.value = await inventoryCenterApi.getPickingListRows(buildListQuery())
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
  void runPickingFetch(true)
}

const handleReset = () => {
  resetFilterForm()
  void runPickingFetch(true)
}

onMounted(async () => {
  try {
    warehouseOptions.value = await inventoryCenterApi.getWarehouses()
  } catch {
    warehouseOptions.value = []
  }
  void fetchList()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.picking-slip-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
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

.count-badge {
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

  &.search-input--code {
    width: 140px;
  }

  &.search-input--pn {
    width: 140px;
  }

  &.search-input--customer {
    width: 140px;
  }

  &.search-input--sales {
    width: 120px;
  }
}

.filter-select {
  width: 130px;

  &--warehouse {
    width: 180px;
  }
}

.filter-date-range {
  width: 240px;
}

.picking-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
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
.status-badge {
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.06);
  color: $text-primary;
}

.op-col-header {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.op-col-header-text {
  font-size: 12px;
}

.op-col-toggle-btn {
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
  padding: 0 2px;
  line-height: 1;
}

.op-more-dropdown-trigger {
  display: inline-flex;
}

.op-more-trigger {
  background: transparent;
  border: none;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  padding: 2px 6px;
}

.op-more-item {
  font-size: 13px;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--danger {
  color: #f56c6c;
}
</style>
