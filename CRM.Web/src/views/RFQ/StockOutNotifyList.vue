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
          <h1 class="page-title">出库通知</h1>
        </div>
        <div class="count-badge">共 {{ filteredList.length }} 条</div>
      </div>
      <div class="header-right">
        <el-select
          v-model="workflowFilter"
          placeholder="流程筛选"
          clearable
          style="width: 168px"
          class="filter-select"
        >
          <el-option label="全部" value="all" />
          <el-option label="待拣货" value="pending_pick" />
          <el-option label="已拣货待出库" value="picked_pending_out" />
          <el-option label="已出库" value="done" />
        </el-select>
        <el-input v-model="keyword" placeholder="通知单号/销售单号/客户" clearable style="width: 280px" @keyup.enter="fetchList" />
        <button class="btn-secondary" @click="fetchList">刷新</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-notify-list-main"
      :columns="stockOutNotifyColumns"
      :show-column-settings="false"
      :data="filteredList"
      v-loading="loading"
    >
      <template #col-workflow="{ row }">
        <span class="flow-tag" :class="`flow-tag--${workflowTagKey(row)}`">{{ workflowLabel(row) }}</span>
      </template>
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-outQuantity="{ row }">{{ row.outQuantity }}</template>
      <template #col-requestDate="{ row }">{{ formatRequestDateTime(row.requestDate) }}</template>
      <template #col-createTime="{ row }">{{ formatRequestDateTime(row.createTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.requestUserName || '--' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">操作</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" v-show="Number(row.status) !== 1" class="action-btns">
            <button type="button" class="action-btn action-btn--warning" @click.stop="goExecute(row)">执行出库</button>
          </div>
          <span v-else-if="Number(row.status) === 1" class="op-done">已出库</span>

          <el-dropdown v-else trigger="click" placement="bottom-end" v-if="Number(row.status) !== 1">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goExecute(row)">
                  <span class="op-more-item op-more-item--warning">执行出库</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip content="列设置" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import { inventoryCenterApi, type PickingTask } from '@/api/inventoryCenter'
import { formatDate as formatDateTimeZh } from '@/utils/date'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const loading = ref(false)
const keyword = ref('')
const workflowFilter = ref<string>('all')
const list = ref<StockOutRequestDto[]>([])
const pickingTasks = ref<PickingTask[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

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

const stockOutNotifyColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'requestCode', label: '通知单号', prop: 'requestCode', width: 160, minWidth: 160 },
  { key: 'workflow', label: '流程', width: 130, align: 'center' },
  { key: 'status', label: '状态', prop: 'status', width: 110, align: 'center' },
  { key: 'salesOrderCode', label: '销售单号', prop: 'salesOrderCode', width: 160, minWidth: 160 },
  { key: 'materialModel', label: '物料型号', prop: 'materialModel', width: 180, showOverflowTooltip: true },
  { key: 'brand', label: '品牌', prop: 'brand', width: 140, showOverflowTooltip: true },
  { key: 'outQuantity', label: '出库数量', prop: 'outQuantity', width: 110, align: 'right' },
  { key: 'requestDate', label: '预计出库日期', prop: 'requestDate', width: 170 },
  { key: 'salesUserName', label: '业务员名称', prop: 'salesUserName', width: 130, showOverflowTooltip: true },
  { key: 'customerName', label: '客户', prop: 'customerName', minWidth: 180, showOverflowTooltip: true },
  { key: 'remark', label: '备注', prop: 'remark', minWidth: 180, showOverflowTooltip: true },
  { key: 'createTime', label: '创建时间', prop: 'createTime', width: 170 },
  { key: 'createUser', label: '创建人', width: 140, showOverflowTooltip: true },
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
    labelClassName: 'op-col'
  }
])

const statusLabel = (s: number) => {
  if (s === 0) return '待出库'
  if (s === 1) return '已出库'
  if (s === 2) return '已取消'
  return '未知'
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
  if (Number(row.status) === 1) return '已出库'
  if (Number(row.status) === 2) return '已取消'
  if (hasPickingCompleted(row.id)) return '已拣货待出库'
  return '待拣货'
}

/** 按本地时区显示年月日 + 时分 */
const formatRequestDateTime = (v?: string | null) => {
  if (v == null || v === '') return '--'
  return formatDateTimeZh(v, 'YYYY-MM-DD HH:mm')
}

const filteredList = computed(() => {
  let rows = list.value
  const wf = workflowFilter.value
  if (wf && wf !== 'all') {
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

const fetchList = async () => {
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
    ElMessage.error('加载出库通知列表失败')
  } finally {
    loading.value = false
  }
}

const goExecute = (row: StockOutRequestDto) => {
  router.push({ path: '/inventory/stock-out/create', query: { requestId: row.id } })
}

onMounted(fetchList)
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
.header-left,
.header-right {
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
.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
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
.filter-select {
  :deep(.el-input__wrapper) {
    background: rgba(255, 255, 255, 0.05);
    box-shadow: 0 0 0 1px $border-panel inset;
  }
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

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>
