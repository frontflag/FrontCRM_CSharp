<template>
  <div class="purchase-order-list-page">
    <div class="page-header">
      <h2>采购订单</h2>
      <button type="button" class="btn-success" @click="handleCreate">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        新建采购订单
      </button>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ statTotal }}</div>
          <div class="stat-label">订单总数</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ statPending }}</div>
          <div class="stat-label">待确认</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ statInProgress }}</div>
          <div class="stat-label">进行中</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-info">
          <div class="stat-value">{{ canViewPurchaseAmount ? `¥${statAmount.toLocaleString()}` : '--' }}</div>
          <div class="stat-label">采购总额</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索栏（与客户列表页 search-bar 一致） -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">订单号</span>
        <div class="search-input-wrap">
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            class="search-icon"
            aria-hidden="true"
          >
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.code"
            class="search-input"
            placeholder="请输入订单号"
            @keyup.enter="handleSearch"
          />
        </div>
        <template v-if="canViewVendorInfo">
          <span class="filter-field-label">供应商</span>
          <input
            v-model="filterForm.vendor"
            class="search-input search-input--plain"
            placeholder="请输入供应商"
            @keyup.enter="handleSearch"
          />
        </template>
        <span class="filter-field-label">状态</span>
        <el-select
          v-model="filterForm.status"
          placeholder="全部状态"
          clearable
          class="status-select status-select--po"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option label="草稿" :value="0" />
          <el-option label="新建" :value="1" />
          <el-option label="待审核" :value="2" />
          <el-option label="审核通过" :value="10" />
          <el-option label="待确认" :value="20" />
          <el-option label="已确认" :value="30" />
          <el-option label="进行中" :value="50" />
          <el-option label="采购完成" :value="100" />
          <el-option label="审核失败" :value="-1" />
          <el-option label="取消" :value="-2" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">搜索</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">重置</button>
      </div>
    </div>

    <!-- 数据表格 -->
    <el-card class="table-card">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="purchase-order-list-main"
        :columns="purchaseOrderTableColumns"
        :show-column-settings="false"
        :data="filteredList"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <template #col-purchaseOrderCode="{ row }">
          <el-link type="primary" @click="handleView(row)">{{ row.purchaseOrderCode }}</el-link>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(poListMainStatus(row))" size="small">
            {{ getStatusText(poListMainStatus(row)) }}
          </el-tag>
        </template>
        <template #col-total="{ row }">
          <span class="amount">{{ formatCurrency(row.total, row.currency) }}</span>
        </template>
        <template #col-stockStatus="{ row }">
          <el-tag effect="dark" :type="getStockStatusType(row.stockStatus)" size="small">
            {{ getStockStatusText(row.stockStatus) }}
          </el-tag>
        </template>
        <template #col-deliveryDate="{ row }">
          {{ formatDisplayDate(row.deliveryDate) }}
        </template>
        <template #col-createTime="{ row }">
          {{ formatDisplayDateTime(row.createTime) }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || row.purchaseUserName || '—' }}
        </template>
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
            <div v-if="opColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">详情</button>
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">编辑</button>
              <button
                v-if="(poListMainStatus(row) >= 1 && poListMainStatus(row) < 10) || poListMainStatus(row) === -1"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="submitAudit(row)"
              >
                提交审核
              </button>
              <button
                v-if="poListMainStatus(row) >= 10 && poListMainStatus(row) < 30"
                type="button"
                class="action-btn action-btn--warning"
                @click.stop="confirmBySupplier(row)"
              >
                供应商确认
              </button>
              <button
                v-if="purchaseOrderReportAllowed(poListMainStatus(row))"
                type="button"
                class="action-btn action-btn--primary"
                @click.stop="handlePrintOrder(row)"
              >
                采购单
              </button>
              <button
                v-if="poListMainStatus(row) === 30"
                type="button"
                class="action-btn action-btn--danger"
                @click.stop="cancelSupplierConfirm(row)"
              >
                取消确认
              </button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">详情</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">编辑</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="(poListMainStatus(row) >= 1 && poListMainStatus(row) < 10) || poListMainStatus(row) === -1"
                    @click.stop="submitAudit(row)"
                  >
                    <span class="op-more-item op-more-item--warning">提交审核</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="poListMainStatus(row) >= 10 && poListMainStatus(row) < 30"
                    @click.stop="confirmBySupplier(row)"
                  >
                    <span class="op-more-item op-more-item--warning">供应商确认</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="purchaseOrderReportAllowed(poListMainStatus(row))"
                    @click.stop="handlePrintOrder(row)"
                  >
                    <span class="op-more-item op-more-item--primary">采购单</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="poListMainStatus(row) === 30" @click.stop="cancelSupplierConfirm(row)">
                    <span class="op-more-item op-more-item--danger">取消确认</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <!-- 底栏：列设置（图标+Tip+Spacer） + 分页（顶对齐） -->
      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip content="列设置" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
        <el-pagination
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  purchaseOrderReportAllowed,
  normalizePurchaseOrderMainStatus
} from '@/constants/purchaseOrderStatus'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CrmDataTable from '@/components/CrmDataTable.vue'

const router = useRouter()

const loading = ref(false)
const orderList = ref<any[]>([])
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const authStore = useAuthStore()
const canViewVendorInfo = computed(() => authStore.hasPermission('vendor.info.read'))
const canViewPurchaseAmount = computed(() => authStore.hasPermission('purchase.amount.read'))

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 300
const OP_COL_EXPANDED_MIN_WIDTH = 292
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const poListMainStatus = normalizePurchaseOrderMainStatus

/** 采购订单列表主表可配置列（localStorage：crm-table-columns:v1:purchase-order-list-main） */
const purchaseOrderTableColumns = computed((): CrmTableColumnDef[] => [
  {
    key: 'purchaseOrderCode',
    label: '订单号',
    prop: 'purchaseOrderCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    sortable: true
  },
  { key: 'status', label: '状态', prop: 'status', width: 160, align: 'center' as const },
  ...(canViewVendorInfo.value
    ? [{ key: 'vendorName', label: '供应商', prop: 'vendorName', minWidth: 200, showOverflowTooltip: true }]
    : []),
  { key: 'purchaseUserName', label: '采购员', prop: 'purchaseUserName', width: 100 },
  ...(canViewPurchaseAmount.value
    ? [{ key: 'total', label: '总金额', prop: 'total', width: 160, align: 'right' as const }]
    : []),
  { key: 'itemRows', label: '行项目', prop: 'itemRows', width: 80, align: 'center' as const },
  { key: 'stockStatus', label: '入库状态', prop: 'stockStatus', width: 160, align: 'center' as const },
  { key: 'deliveryDate', label: '交货日期', prop: 'deliveryDate', width: 160 },
  { key: 'createTime', label: '创建时间', prop: 'createTime', width: 160 },
  { key: 'createUser', label: '创建人', width: 120, showOverflowTooltip: true },
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

// 筛选表单
const filterForm = ref({
  code: '',
  vendor: '',
  status: undefined as number | undefined
})

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 10,
  total: 0
})

// 对话框控制
// 计算属性：筛选后的列表
const filteredList = computed(() => {
  let result = orderList.value
  if (filterForm.value.code) {
    result = result.filter(o => o.purchaseOrderCode.toLowerCase().includes(filterForm.value.code.toLowerCase()))
  }
  if (filterForm.value.vendor) {
    result = result.filter(o => o.vendorName?.toLowerCase().includes(filterForm.value.vendor.toLowerCase()))
  }
  if (filterForm.value.status !== undefined) {
    result = result.filter(o => poListMainStatus(o) === filterForm.value.status)
  }
  pageInfo.value.total = result.length
  const start = (pageInfo.value.page - 1) * pageInfo.value.pageSize
  return result.slice(start, start + pageInfo.value.pageSize)
})

// 统计
const statTotal = computed(() => orderList.value.length)
const statPending = computed(() => orderList.value.filter(o => poListMainStatus(o) === 20).length)
const statInProgress = computed(() => orderList.value.filter(o => poListMainStatus(o) === 50).length)
const statAmount = computed(() => orderList.value.reduce((sum, o) => sum + (o.total || 0), 0))

// 格式化货币
const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 状态处理
const getStatusType = (status: number) => {
  if (!Number.isFinite(status)) return 'info'
  const map: Record<number, string> = {
    0: 'info',
    1: 'info',
    2: 'warning',
    10: 'success',
    20: 'warning',
    30: 'primary',
    50: 'primary',
    100: 'success',
    '-1': 'danger',
    '-2': 'info'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  if (!Number.isFinite(status)) return '—'
  const map: Record<number, string> = {
    0: '草稿',
    1: '新建',
    2: '待审核',
    10: '审核通过',
    20: '待确认',
    30: '已确认',
    50: '进行中',
    100: '采购完成',
    '-1': '审核失败',
    '-2': '取消'
  }
  return map[status] || '未知'
}

const getStockStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'warning', 2: 'success' }
  return map[status] || 'info'
}

const getStockStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '未入库', 1: '部分入库', 2: '全部入库' }
  return map[status] || '未知'
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await purchaseOrderApi.getList({ page: 1, pageSize: 2000 })
    orderList.value = (res as { items?: unknown[] }).items || []
    pageInfo.value.total = orderList.value.length
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
}

const handleReset = () => {
  filterForm.value = { code: '', vendor: '', status: undefined }
  pageInfo.value.page = 1
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
}

// 新建（手工新建入口暂关，以销定采等其它入口仍可用）
const handleCreate = () => {
  ElMessage.info('功能暂未开放')
}

// 编辑
const handleEdit = (row: any) => {
  router.push({ name: 'PurchaseOrderCreate', query: { id: row.id, edit: '1' } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.id } })
}

const handlePrintOrder = (row: any) => {
  if (!purchaseOrderReportAllowed(poListMainStatus(row))) {
    ElMessage.warning('仅供应商已确认后的采购订单可生成采购单报表')
    return
  }
  router.push({ name: 'PurchaseOrderReport', params: { id: row.id } })
}

/** 供应商确认：待确认(20) -> 已确认(30) */
const confirmBySupplier = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 标记为“已确认”吗？`,
      '供应商确认',
      { type: 'info', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    // 允许从“审核通过(10)”推进到“待确认(20)”再到“已确认(30)”
    if (poListMainStatus(row) === 10) {
      await purchaseOrderApi.updateStatus(row.id, 20)
    }
    await purchaseOrderApi.updateStatus(row.id, 30)
    ElMessage.success('供应商确认成功')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 取消确认：仅「已确认(30)」时显示 */
const cancelSupplierConfirm = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 取消确认吗？`,
      '取消确认',
      { type: 'warning', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    await purchaseOrderApi.updateStatus(row.id, -2)
    ElMessage.success('已取消确认')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

/** 提交审核 */
const submitAudit = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      `确认将采购订单 ${row.purchaseOrderCode} 提交审核吗？`,
      '提交审核',
      { type: 'info', confirmButtonText: '确认', cancelButtonText: '取消' }
    )
    // 审核失败(-1)先回到新建(1)，再提交审核(2)
    if (poListMainStatus(row) === -1) {
      await purchaseOrderApi.updateStatus(row.id, 1)
    }
    await purchaseOrderApi.updateStatus(row.id, 2)
    ElMessage.success('提交审核成功')
    await loadData()
  } catch {
    // 取消或失败已由全局拦截器提示
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.purchase-order-list-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  h2 {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
  }
}

.stat-row {
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
  background: $layer-3;
  border: 1px solid $border-card;
  :deep(.el-card__body) {
    padding: 15px;
  }
  .stat-value {
    font-size: 24px;
    font-weight: bold;
    color: $cyan-primary;
    margin-bottom: 5px;
  }
  .stat-label {
    font-size: 14px;
    color: $text-muted;
  }
  &.stat-warning .stat-value {
    color: $warning-color;
  }
  &.stat-success .stat-value {
    color: $success-color;
  }
  &.stat-info .stat-value {
    color: $info-color;
  }
}

// ---- 搜索栏（与客户列表一致）----
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
  outline: none;
  transition: border-color 0.2s;
  box-sizing: border-box;

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--plain {
  padding: 7px 12px;
  width: 200px;
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

.status-select--po {
  width: 150px;
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

/* 列表页「新建/新增」：UI 规范 success 绿（见 列表操作按钮颜色规范PRD） */
.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.85), rgba(70, 191, 145, 0.75));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
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
  cursor: pointer;
  transition: all 0.2s;

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
}

.table-card {
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #E8F4FF;

    // 操作列：列宽随内容收缩（.action-btns / .action-btn 视觉见 crm-unified-list.scss）
    .el-table__cell.op-col .cell {
      display: inline-block;
      width: max-content;
      max-width: 100%;
    }
    .el-table__cell .cell {
      white-space: nowrap;
    }
  }
}

.amount {
  color: #00D4FF;
  font-weight: 500;
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.items-section {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);
  .items-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    h4 {
      margin: 0;
      color: #E8F4FF;
    }
  }
  .total-amount {
    margin-top: 10px;
    text-align: right;
    font-size: 16px;
    color: #E8F4FF;
    .amount {
      font-size: 20px;
      font-weight: bold;
      color: #00D4FF;
    }
  }
}

.tags-row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.doc-tab-content {
  padding: 8px 0;
}
.detail-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 16px;
  }
  :deep(.el-tabs__content) {
    min-height: 200px;
  }
}

// 列表操作列规范（收起/展开）
.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--warning {
  color: $color-amber;
}

.op-more-item--danger {
  color: $color-red-brown;
}

.op-more-item--success {
  color: $color-mint-green;
}

.op-more-item--info {
  color: rgba(200, 216, 232, 0.85);
}
</style>
