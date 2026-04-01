<template>
  <div class="bom-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">BOM 快速报价</h1>
        <div class="count-badge">共 {{ totalCount }} 条 BOM</div>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="goCreate">
          <el-icon><Plus /></el-icon>新建 BOM
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="16" class="stat-row">
      <el-col :span="6">
        <div class="stat-card">
          <div class="stat-icon icon-total">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="18" height="18" rx="2"/><line x1="3" y1="9" x2="21" y2="9"/><line x1="9" y1="21" x2="9" y2="9"/></svg>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.total }}</div>
            <div class="stat-label">BOM 总数</div>
          </div>
        </div>
      </el-col>
      <el-col :span="6">
        <div class="stat-card">
          <div class="stat-icon icon-pending">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.pending }}</div>
            <div class="stat-label">待报价</div>
          </div>
        </div>
      </el-col>
      <el-col :span="6">
        <div class="stat-card">
          <div class="stat-icon icon-quoting">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.quoting }}</div>
            <div class="stat-label">报价中</div>
          </div>
        </div>
      </el-col>
      <el-col :span="6">
        <div class="stat-card">
          <div class="stat-icon icon-quoted">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="20 6 9 17 4 12"/></svg>
          </div>
          <div class="stat-info">
            <div class="stat-value">{{ stats.quoted }}</div>
            <div class="stat-label">已报价</div>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <el-input
        v-model="searchForm.keyword"
        placeholder="搜索 BOM 单号 / 客户名称..."
        clearable
        class="search-input"
        @keyup.enter="handleSearch"
      >
        <template #prefix><el-icon><Search /></el-icon></template>
      </el-input>
      <el-select v-model="searchForm.status" placeholder="全部状态" clearable class="filter-select">
        <el-option label="草稿" :value="0" />
        <el-option label="待报价" :value="1" />
        <el-option label="报价中" :value="2" />
        <el-option label="已报价" :value="3" />
        <el-option label="已接受" :value="4" />
        <el-option label="已关闭" :value="5" />
        <el-option label="已取消" :value="6" />
      </el-select>
      <el-select v-model="searchForm.bomType" placeholder="全部类型" clearable class="filter-select">
        <el-option label="现货" :value="1" />
        <el-option label="期货" :value="2" />
        <el-option label="样品" :value="3" />
        <el-option label="批量" :value="4" />
      </el-select>
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        value-format="YYYY-MM-DD"
        class="date-picker"
        @change="handleSearch"
      />
      <el-button type="primary" @click="handleSearch">
        <el-icon><Search /></el-icon>搜索
      </el-button>
      <el-button @click="handleReset">重置</el-button>
    </div>

    <!-- 批量操作栏 -->
    <div v-if="selectedIds.length > 0" class="batch-bar">
      <span class="batch-tip">已选 {{ selectedIds.length }} 条</span>
      <el-button type="danger" size="small" @click="handleBatchDelete">
        <el-icon><Delete /></el-icon>批量删除
      </el-button>
      <el-button size="small" @click="selectedIds = []">取消选择</el-button>
    </div>

    <!-- 数据表格 -->
    <div class="table-panel">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="bom-list-main"
        :columns="bomTableColumns"
        :show-column-settings="false"
        v-loading="loading"
        :data="bomList"
        row-key="id"
        class="bom-table"
        @selection-change="handleSelectionChange"
        @row-dblclick="onRowDblclick"
      >
        <template #col-bomCode="{ row }">
          <span class="bom-code" @click="goDetail(row.id)">{{ row.bomCode }}</span>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" size="small" :type="getStatusTagType(row.status)">{{ getStatusText(row.status) }}</el-tag>
        </template>
        <template #col-customerName="{ row }">{{ row.customerName || '—' }}</template>
        <template #col-itemCount="{ row }">{{ row.itemCount ?? 0 }}</template>
        <template #col-quotedCount="{ row }">
          <span :class="row.quotedCount > 0 ? 'text-success' : 'text-muted'">
            {{ row.quotedCount ?? 0 }}
          </span>
        </template>
        <template #col-bomType="{ row }">
          <el-tag effect="dark" size="small" :type="getBOMTypeTagType(row.bomType)">{{ getBOMTypeText(row.bomType) }}</el-tag>
        </template>
        <template #col-createdAt="{ row }">{{ formatDate(row.createdAt) }}</template>
        <template #col-createUser="{ row }">{{ row.salesUserName || row.createdBy || '—' }}</template>
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
              <el-button size="small" type="primary" @click.stop="goDetail(row.id)">详情</el-button>
              <el-button
                v-if="row.status === 1 || row.status === 2"
                size="small"
                type="warning"
                @click.stop="handleAutoQuote(row)"
              >
                一键报价
              </el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goDetail(row.id)">
                    <span class="op-more-item op-more-item--primary">详情</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="row.status === 1 || row.status === 2"
                    @click.stop="handleAutoQuote(row)"
                  >
                    <span class="op-more-item op-more-item--warning">一键报价</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <!-- 分页 -->
      <div class="pagination-bar">
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
          :page-sizes="[20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Plus, Search, Delete, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { bomApi } from '@/api/bom'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { BOM } from '@/types/bom'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()

const loading = ref(false)
const bomList = ref<BOM[]>([])
const selectedIds = ref<string[]>([])
const dateRange = ref<[string, string] | null>(null)
const dataTableRef = ref<any>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 168
const OP_COL_EXPANDED_MIN_WIDTH = 168
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const bomTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'selection', type: 'selection', width: 44, fixed: 'left', hideable: false, reorderable: false },
  { key: 'bomCode', label: 'BOM 单号', width: 160, minWidth: 160 },
  { key: 'status', label: '状态', width: 90, align: 'center' },
  { key: 'customerName', label: '客户', minWidth: 160, showOverflowTooltip: true },
  { key: 'itemCount', label: '明细数', width: 80, align: 'center' },
  { key: 'quotedCount', label: '已报价', width: 80, align: 'center' },
  { key: 'bomType', label: '类型', width: 80, align: 'center' },
  { key: 'createdAt', label: '创建时间', width: 150 },
  { key: 'createUser', label: '创建人', width: 90, showOverflowTooltip: true },
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

const stats = ref({ total: 0, pending: 0, quoting: 0, quoted: 0 })

const searchForm = ref({
  keyword: '',
  status: '' as number | '',
  bomType: '' as number | '',
})

const pageInfo = ref({ page: 1, pageSize: 20, total: 0 })

const totalCount = computed(() => pageInfo.value.total)

// ── 状态/类型文本 ──
const getStatusText = (s: number) => {
  const map: Record<number, string> = { 0: '草稿', 1: '待报价', 2: '报价中', 3: '已报价', 4: '已接受', 5: '已关闭', 6: '已取消' }
  return map[s] ?? '未知'
}
const getStatusTagType = (s: number): '' | 'success' | 'warning' | 'danger' | 'info' => {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    0: 'info', 1: 'warning', 2: '', 3: 'success', 4: 'success', 5: 'info', 6: 'danger'
  }
  return map[s] ?? 'info'
}
const getBOMTypeText = (t: number) => {
  const map: Record<number, string> = { 1: '现货', 2: '期货', 3: '样品', 4: '批量' }
  return map[t] ?? '—'
}
const getBOMTypeTagType = (t: number): '' | 'success' | 'warning' | 'danger' | 'info' => {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    1: '', 2: 'warning', 3: 'success', 4: 'info'
  }
  return map[t] ?? 'info'
}
const formatDate = (d?: string) => {
  if (!d) return '—'
  const s = formatDisplayDateTime(d)
  return s === '--' ? '—' : s
}

// ── 数据加载 ──
const loadData = async () => {
  loading.value = true
  try {
    const res = await bomApi.searchBOMs({
      pageNumber: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize,
      keyword: searchForm.value.keyword || undefined,
      status: searchForm.value.status !== '' ? searchForm.value.status : undefined,
      bomType: searchForm.value.bomType !== '' ? searchForm.value.bomType : undefined,
      startDate: dateRange.value?.[0] || undefined,
      endDate: dateRange.value?.[1] || undefined,
    })
    bomList.value = res.items || []
    pageInfo.value.total = res.totalCount || 0
    // 统计
    stats.value.total = res.totalCount || 0
    stats.value.pending = bomList.value.filter(b => b.status === 1).length
    stats.value.quoting = bomList.value.filter(b => b.status === 2).length
    stats.value.quoted = bomList.value.filter(b => b.status === 3).length
  } catch {
    // API 未实现时静默降级
    bomList.value = []
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pageInfo.value.page = 1
  loadData()
}
const handleReset = () => {
  searchForm.value = { keyword: '', status: '', bomType: '' }
  dateRange.value = null
  handleSearch()
}

// ── 选择 ──
const handleSelectionChange = (rows: BOM[]) => {
  selectedIds.value = rows.map(r => r.id)
}

// ── 路由跳转 ──
const goCreate = () => router.push({ name: 'BOMCreate' })
const goDetail = (id: string) => router.push({ name: 'BOMDetail', params: { id } })
const onRowDblclick = (row: BOM) => goDetail(row.id)

const handleBatchDelete = async () => {
  await ElMessageBox.confirm(`确认删除选中的 ${selectedIds.value.length} 条 BOM？`, '批量删除确认', {
    confirmButtonText: '确认删除', cancelButtonText: '取消', type: 'warning'
  })
  try {
    await bomApi.deleteBOMs({ ids: selectedIds.value })
    ElMessage.success('批量删除成功')
    selectedIds.value = []
    loadData()
  } catch {
    ElMessage.error('批量删除失败，请稍后重试')
  }
}

// ── 一键报价（列表快捷入口） ──
const handleAutoQuote = async (row: BOM) => {
  try {
    const res = await bomApi.autoQuote({ bomId: row.id })
    ElMessage.success(`报价完成：${res.quotedItems} 条已报价，${res.noStockItems} 条无货`)
    loadData()
  } catch {
    ElMessage.error('一键报价失败，请稍后重试')
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

/* ── 与全站主题 token 一致 ── */
.bom-list-page {
  padding: 20px;
  min-height: 100%;
  background: transparent;
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
  .page-title {
    font-size: 18px;
    font-weight: 700;
    color: $text-primary;
    margin: 0;
  }
  .count-badge {
    font-size: 12px;
    color: rgba(0, 212, 255, 0.7);
    background: rgba(0, 212, 255, 0.08);
    border: 1px solid rgba(0, 212, 255, 0.2);
    padding: 2px 10px;
    border-radius: 10px;
  }
  .header-right {
    display: flex;
    gap: 10px;
    align-items: center;
  }
}

/* ── 统计卡片 ── */
.stat-row {
  margin-bottom: 20px;
}
.stat-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 18px;
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: 8px;
  .stat-icon {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    svg { width: 22px; height: 22px; }
    &.icon-total {
      background: var(--crm-accent-008);
      color: $cyan-primary;
    }
    &.icon-pending {
      background: var(--crm-accent-008);
      color: $warning-color;
    }
    &.icon-quoting {
      background: var(--crm-accent-008);
      color: $info-color;
    }
    &.icon-quoted {
      background: var(--crm-accent-008);
      color: $success-color;
    }
  }
  .stat-value {
    font-size: 24px;
    font-weight: 700;
    color: $text-primary;
    line-height: 1;
  }
  .stat-label {
    font-size: 12px;
    color: $text-muted;
    margin-top: 4px;
  }
}

/* ── 搜索栏 ── */
.search-bar {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
  margin-bottom: 14px;
  padding: 14px 16px;
  background: rgba(0, 20, 45, 0.6);
  border: 1px solid rgba(0, 212, 255, 0.1);
  border-radius: 8px;
  .search-input { width: 240px; }
  .filter-select { width: 130px; }
  .date-picker { width: 260px; }
}

/* ── 批量操作栏 ── */
.batch-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 14px;
  background: rgba(0, 212, 255, 0.05);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 6px;
  margin-bottom: 10px;
  .batch-tip { font-size: 13px; color: #00d4ff; }
}

/* ── 表格 ── */
.table-panel {
  background: rgba(0, 20, 45, 0.6);
  border: 1px solid rgba(0, 212, 255, 0.1);
  border-radius: 8px;
  overflow: hidden;
}
.bom-code {
  color: #00d4ff;
  cursor: pointer;
  font-weight: 600;
  font-family: 'Courier New', monospace;
  font-size: 13px;
  &:hover { text-decoration: underline; }
}
.text-success { color: #27ae60; font-weight: 600; }
.text-muted { color: #556; }

.action-btns {
  display: flex;
  gap: 6px;
  flex-wrap: nowrap;
  .el-button { white-space: nowrap; }
}

.pagination-bar {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 12px 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.05);
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

/* ── el-table 深色覆盖 ── */
:deep(.el-table) {
  background: transparent;
  color: #c8d8e8;
  --el-table-border-color: rgba(0, 212, 255, 0.08);
  --el-table-header-bg-color: rgba(0, 212, 255, 0.06);
  --el-table-tr-bg-color: transparent;
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  .el-table__header th { color: rgba(0, 212, 255, 0.7); font-size: 12px; font-weight: 600; }
  .el-table__cell .el-button { white-space: nowrap; }
  .cell { white-space: nowrap; }
}
</style>
