<template>
  <div class="purchase-requisition-list-page">
    <div class="page-header">
      <h2>采购申请列表</h2>
      <el-button type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>新建采购申请
      </el-button>
    </div>

    <!-- 搜索栏：对齐客户列表 CustomerList -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">采购申请号</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.billCode"
            class="search-input"
            placeholder="请输入采购申请号"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">销售订单</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.sellOrderId"
            class="search-input search-input--narrow"
            placeholder="销售订单 ID"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="filterForm.status"
          placeholder="全部状态"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option :value="0" label="新建" />
          <el-option :value="1" label="部分完成" />
          <el-option :value="2" label="全部完成" />
          <el-option :value="3" label="已取消" />
        </el-select>
        <button class="btn-primary btn-sm" type="button" :disabled="loading" @click="handleSearch">搜索</button>
        <button class="btn-ghost btn-sm" type="button" :disabled="loading" @click="handleReset">重置</button>
      </div>
    </div>

    <el-card class="table-card">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="purchase-requisition-list-main"
        :columns="purchaseReqColumns"
        :show-column-settings="false"
        :data="list"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusTagType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-expectedPurchaseTime="{ row }">{{ formatDisplayDateTime(row.expectedPurchaseTime) }}</template>
        <template #col-type="{ row }">{{ getTypeText(row.type) }}</template>
        <template #col-createTime="{ row }">{{ row.createTime ? formatDisplayDateTime(row.createTime) : '--' }}</template>
        <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || row.purchaseUserId || '--' }}</template>
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
              <el-button link type="primary" @click.stop="handleView(row)">查看</el-button>
              <el-button link type="warning" size="small" @click.stop="handleGeneratePurchaseOrder(row)">
                生成采购订单
              </el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">查看</span>
                  </el-dropdown-item>
                  <el-dropdown-item @click.stop="handleGeneratePurchaseOrder(row)">
                    <span class="op-more-item op-more-item--warning">生成采购订单</span>
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
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="loadList"
          @size-change="loadList"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Setting } from '@element-plus/icons-vue'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()

const loading = ref(false)
const list = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 200
const OP_COL_EXPANDED_MIN_WIDTH = 200
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const purchaseReqColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'billCode', label: '采购申请号', prop: 'billCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'status', label: '状态', prop: 'status', width: 160, align: 'center' },
  { key: 'sellOrderCode', label: '销售订单', prop: 'sellOrderCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'pn', label: '物料型号', prop: 'pn', minWidth: 140, showOverflowTooltip: true },
  { key: 'brand', label: '品牌', prop: 'brand', minWidth: 120, showOverflowTooltip: true },
  { key: 'qty', label: '申请数量', prop: 'qty', width: 120, align: 'right' },
  { key: 'expectedPurchaseTime', label: '预计采购日期', prop: 'expectedPurchaseTime', width: 160 },
  { key: 'type', label: '类型', prop: 'type', width: 110, align: 'center' },
  { key: 'purchaseUserId', label: '采购员ID', prop: 'purchaseUserId', width: 140, showOverflowTooltip: true },
  { key: 'remark', label: '备注', prop: 'remark', minWidth: 180, showOverflowTooltip: true },
  { key: 'createTime', label: '创建时间', width: 160 },
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

const filterForm = reactive({
  billCode: '',
  sellOrderId: '',
  status: undefined as number | undefined
})

function getStatusText(s: number) {
  const m: Record<number, string> = {
    0: '新建',
    1: '部分完成',
    2: '全部完成',
    3: '已取消'
  }
  return m[s] ?? String(s)
}

function getStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === 0) return 'info'
  if (s === 1 || s === 2) return 'success'
  if (s === 3) return 'danger'
  return ''
}

function getTypeText(t: number) {
  const m: Record<number, string> = {
    0: '专属',
    1: '公开备货'
  }
  return m[t] ?? String(t)
}

async function loadList() {
  loading.value = true
  try {
    const data = await purchaseRequisitionApi.getList({
      keyword: filterForm.billCode.trim() || undefined,
      sellOrderId: filterForm.sellOrderId.trim() || undefined,
      status: filterForm.status,
      page: page.value,
      pageSize: pageSize.value
    })
    list.value = data?.items ?? []
    total.value = data?.total ?? 0
  } catch (e: any) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  page.value = 1
  loadList()
}

function handleReset() {
  filterForm.billCode = ''
  filterForm.sellOrderId = ''
  filterForm.status = undefined
  page.value = 1
  loadList()
}

function handleCreate() {
  router.push('/purchase-requisitions/new')
}

function handleView(row: any) {
  router.push(`/purchase-requisitions/${row.id}`)
}

function handleGeneratePurchaseOrder(row: any) {
  router.push({ name: 'PurchaseOrderCreate', query: { requisitionId: row?.id } })
}

onMounted(loadList)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.purchase-requisition-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0;
  color: $text-primary;
  font-size: 20px;
  font-weight: 600;
}

// ---- 搜索栏（与 CustomerList 一致）----
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

  &--narrow {
    width: 180px;
  }

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.status-select {
  width: 140px;
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

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
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

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.table-card {
  margin-top: 0;
  background: #0a1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #e8f4ff;

    .el-table__cell .el-button {
      white-space: nowrap !important;
    }
  }
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
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

