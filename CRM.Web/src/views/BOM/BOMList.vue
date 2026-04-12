<template>
  <div class="bom-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('bomList.title') }}</h1>
        <div class="count-badge">{{ t('bomList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <el-button type="primary" @click="goCreate">
          <el-icon><Plus /></el-icon>{{ t('bomList.create') }}
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
            <div class="stat-label">{{ t('bomList.stats.total') }}</div>
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
            <div class="stat-label">{{ t('bomList.stats.pending') }}</div>
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
            <div class="stat-label">{{ t('bomList.stats.quoting') }}</div>
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
            <div class="stat-label">{{ t('bomList.stats.quoted') }}</div>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <el-input
        v-model="searchForm.keyword"
        :placeholder="t('bomList.filters.searchPlaceholder')"
        clearable
        class="search-input"
        @keyup.enter="handleSearch"
      >
        <template #prefix><el-icon><Search /></el-icon></template>
      </el-input>
      <el-select v-model="searchForm.status" :placeholder="t('bomList.filters.allStatus')" clearable class="filter-select">
        <el-option :label="t('bomList.status.draft')" :value="0" />
        <el-option :label="t('bomList.status.pending')" :value="1" />
        <el-option :label="t('bomList.status.quoting')" :value="2" />
        <el-option :label="t('bomList.status.quoted')" :value="3" />
        <el-option :label="t('bomList.status.accepted')" :value="4" />
        <el-option :label="t('bomList.status.closed')" :value="5" />
        <el-option :label="t('bomList.status.cancelled')" :value="6" />
      </el-select>
      <el-select v-model="searchForm.bomType" :placeholder="t('bomList.filters.allType')" clearable class="filter-select">
        <el-option :label="t('bomList.type.spot')" :value="1" />
        <el-option :label="t('bomList.type.future')" :value="2" />
        <el-option :label="t('bomList.type.sample')" :value="3" />
        <el-option :label="t('bomList.type.bulk')" :value="4" />
      </el-select>
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        :range-separator="t('bomList.filters.to')"
        :start-placeholder="t('bomList.filters.startDate')"
        :end-placeholder="t('bomList.filters.endDate')"
        value-format="YYYY-MM-DD"
        class="date-picker"
        @change="handleSearch"
      />
      <el-button type="primary" @click="handleSearch">
        <el-icon><Search /></el-icon>{{ t('bomList.filters.search') }}
      </el-button>
      <el-button @click="handleReset">{{ t('bomList.filters.reset') }}</el-button>
    </div>

    <!-- 批量操作栏 -->
    <div v-if="selectedIds.length > 0" class="batch-bar">
      <span class="batch-tip">{{ t('bomList.batch.selected', { count: selectedIds.length }) }}</span>
      <el-button type="danger" size="small" @click="handleBatchDelete">
        <el-icon><Delete /></el-icon>{{ t('bomList.batch.delete') }}
      </el-button>
      <el-button size="small" @click="selectedIds = []">{{ t('bomList.batch.clearSelection') }}</el-button>
    </div>

    <!-- 数据表格 -->
    <div class="table-panel">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="bom-list-main"
        :columns="bomTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
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
            <span class="op-col-header-text">{{ t('bomList.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button size="small" type="primary" @click.stop="goDetail(row.id)">{{ t('bomList.actions.detail') }}</el-button>
              <el-button
                v-if="row.status === 1 || row.status === 2"
                size="small"
                type="warning"
                @click.stop="handleAutoQuote(row)"
              >
                {{ t('bomList.actions.autoQuote') }}
              </el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="goDetail(row.id)">
                    <span class="op-more-item op-more-item--primary">{{ t('bomList.actions.detail') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="row.status === 1 || row.status === 2"
                    @click.stop="handleAutoQuote(row)"
                  >
                    <span class="op-more-item op-more-item--warning">{{ t('bomList.actions.autoQuote') }}</span>
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
          <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
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
import { useI18n } from 'vue-i18n'
import { Plus, Search, Delete, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { bomApi } from '@/api/bom'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { BOM } from '@/types/bom'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const bomList = ref<BOM[]>([])
const selectedIds = ref<string[]>([])
const dateRange = ref<[string, string] | null>(null)
const dataTableRef = ref<any>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

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
  { key: 'status', label: t('bomList.columns.status'), width: 90, align: 'center' },
  { key: 'customerName', label: t('bomList.columns.customer'), minWidth: 160, showOverflowTooltip: true },
  { key: 'itemCount', label: t('bomList.columns.itemCount'), width: 80, align: 'center' },
  { key: 'quotedCount', label: t('bomList.columns.quotedCount'), width: 80, align: 'center' },
  { key: 'bomType', label: t('bomList.columns.type'), width: 80, align: 'center' },
  { key: 'bomCode', label: t('bomList.columns.bomCode'), width: 160, minWidth: 160 },
  { key: 'createdAt', label: t('bomList.columns.createdAt'), width: 150 },
  { key: 'createUser', label: t('bomList.columns.createUser'), width: 90, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('bomList.columns.actions'),
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
  const map: Record<number, string> = {
    0: t('bomList.status.draft'),
    1: t('bomList.status.pending'),
    2: t('bomList.status.quoting'),
    3: t('bomList.status.quoted'),
    4: t('bomList.status.accepted'),
    5: t('bomList.status.closed'),
    6: t('bomList.status.cancelled')
  }
  return map[s] ?? t('rfqDetail.unknown')
}
const getStatusTagType = (s: number): '' | 'success' | 'warning' | 'danger' | 'info' => {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    0: 'info', 1: 'warning', 2: '', 3: 'success', 4: 'success', 5: 'info', 6: 'danger'
  }
  return map[s] ?? 'info'
}
const getBOMTypeText = (type: number) => {
  const map: Record<number, string> = {
    1: t('bomList.type.spot'),
    2: t('bomList.type.future'),
    3: t('bomList.type.sample'),
    4: t('bomList.type.bulk')
  }
  return map[type] ?? t('quoteList.na')
}
const getBOMTypeTagType = (type: number): '' | 'success' | 'warning' | 'danger' | 'info' => {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    1: '', 2: 'warning', 3: 'success', 4: 'info'
  }
  return map[type] ?? 'info'
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
  await ElMessageBox.confirm(t('bomList.batch.deleteConfirm', { count: selectedIds.value.length }), t('bomList.batch.deleteTitle'), {
    confirmButtonText: t('bomList.batch.deleteConfirmButton'), cancelButtonText: t('common.cancel'), type: 'warning'
  })
  try {
    await bomApi.deleteBOMs({ ids: selectedIds.value })
    ElMessage.success(t('bomList.batch.deleteSuccess'))
    selectedIds.value = []
    loadData()
  } catch {
    ElMessage.error(t('bomList.batch.deleteFailed'))
  }
}

// ── 一键报价（列表快捷入口） ──
const handleAutoQuote = async (row: BOM) => {
  try {
    const res = await bomApi.autoQuote({ bomId: row.id })
    ElMessage.success(t('bomList.actions.autoQuoteDone', { quoted: res.quotedItems, noStock: res.noStockItems }))
    loadData()
  } catch {
    ElMessage.error(t('bomList.actions.autoQuoteFailed'))
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
