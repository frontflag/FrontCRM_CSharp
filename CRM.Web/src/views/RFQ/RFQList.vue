<template>
  <div class="rfq-list-page customer-list-theme">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">R</div>
          <h1 class="page-title">{{ t('rfqList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('rfqList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <el-button v-if="canCreateNewRfq" class="btn-ghost btn-sm" @click="importDialogVisible = true">
          <el-icon><Upload /></el-icon>{{ t('rfqList.importExcel') }}
        </el-button>
        <button v-if="canCreateNewRfq" class="btn-success" type="button" @click="goCreateRfq">
          <el-icon class="btn-success__icon"><Plus /></el-icon>
          {{ t('rfqList.create') }}
        </button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="statistics-row">
      <div class="stat-card">
        <div class="stat-value">{{ stats.total }}</div>
        <div class="stat-label">{{ t('rfqList.stats.total') }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.pending }}</div>
        <div class="stat-label">{{ t('rfqList.stats.pending') }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.processing }}</div>
        <div class="stat-label">{{ t('rfqList.stats.processing') }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.quoted }}</div>
        <div class="stat-label">{{ t('rfqList.stats.quoted') }}</div>
      </div>
    </div>

    <!-- 搜索面板 -->
    <el-card class="filter-card search-bar">
      <el-form :inline="true" :model="searchForm">
        <el-form-item :label="t('rfqList.filters.search')">
          <el-input 
            v-model="searchForm.keyword" 
            :placeholder="t('rfqList.filters.searchPlaceholder')"
            clearable
            @keyup.enter="handleSearch"
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item :label="t('rfqList.filters.status')">
          <el-select v-model="searchForm.status" :placeholder="t('rfqList.filters.allStatus')" clearable style="width: 140px">
            <el-option :label="t('rfqList.status.pending')" :value="0" />
            <el-option :label="t('rfqList.status.assigned')" :value="1" />
            <el-option :label="t('rfqList.status.processing')" :value="2" />
            <el-option :label="t('rfqList.status.quoted')" :value="3" />
            <el-option :label="t('rfqList.status.selected')" :value="4" />
            <el-option :label="t('rfqList.status.converted')" :value="5" />
            <el-option :label="t('rfqList.status.closed')" :value="6" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button class="btn-primary btn-sm" type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>{{ t('rfqList.filters.query') }}
          </el-button>
          <el-button class="btn-ghost btn-sm" @click="handleReset">{{ t('rfqList.filters.reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <!-- 勿在 el-card 上再加 table-wrapper：会与 CrmDataTable 内层 .table-wrapper 叠套，overflow 影响固定列叠层 -->
    <el-card class="table-card rfq-list-table-card">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="rfq-list-main"
        :columns="rfqTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rfqList"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <template #col-rfqCode="{ row }">
          <el-link type="primary" @click="handleView(row)">{{ row.rfqCode }}</el-link>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-importance="{ row }">
          <el-rate v-model="row.importance" disabled :max="10" />
        </template>
        <template #col-rfqType="{ row }">
          {{ getTypeText(row.rfqType) }}
        </template>
        <template #col-createTime="{ row }">
          {{ row.createTime ? formatDisplayDateTime(row.createTime) : '--' }}
        </template>
        <template #col-createUser="{ row }">
          {{ row.createUserName || row.createdBy || row.salesUserName || '—' }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('rfqList.actions.column') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">{{ t('rfqList.actions.view') }}</button>
              <button v-if="canEditRfq" type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">{{ t('rfqList.actions.edit') }}</button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('rfqList.actions.view') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="canEditRfq" @click.stop="handleEdit(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('rfqList.actions.edit') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <div class="pagination-wrapper">
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
          class="quantum-pagination"
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

    <!-- 导入 Excel 创建 RFQ 对话框 -->
    <ImportRFQDialog
      v-model="importDialogVisible"
      @created="handleImportCreated"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { Plus, Search, Setting, Upload } from '@element-plus/icons-vue'
import ImportRFQDialog from './components/ImportRFQDialog.vue'
import { ElMessage } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatRfqTypeLabel } from '@/constants/rfqFormEnums'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()

/** 新建需求 / Excel 导入（调用创建 API） */
const canCreateNewRfq = computed(() => authStore.hasPermission('rfq.create'))
/** 编辑需求头表（分配等维护仍用 rfq.write） */
const canEditRfq = computed(() => authStore.hasPermission('rfq.write'))
/** 与后端 RFQ 脱敏一致：采购等角色可有 customer.read 但不应见需求侧客户名（需 customer.info.read） */
const canViewCustomerInRfq = computed(() => authStore.hasPermission('customer.info.read'))

function goCreateRfq() {
  if (authStore.isIdentityBlockedForPermission('rfq.create')) {
    ElMessage.warning(t('rfqHome.createBlockedByIdentity'))
    return
  }
  if (!authStore.hasPermission('rfq.create')) {
    ElMessage.warning(t('rfqHome.createNeedRfqCreate'))
    return
  }
  router.push({ name: 'RFQCreate' })
}

const loading = ref(false)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const rfqList = ref<any[]>([])
const stats = ref({ total: 0, pending: 0, processing: 0, quoted: 0 })

// 搜索表单
const searchForm = ref({
  keyword: '',
  status: undefined as number | undefined
})

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 10,
  total: 0
})

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 168
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

/** 需求列表主表可配置列（localStorage：crm-table-columns:v1:rfq-list-main） */
const rfqTableColumns = computed((): CrmTableColumnDef[] => {
  const cols: CrmTableColumnDef[] = [
  { key: 'status', label: t('rfqList.columns.status'), prop: 'status', width: 160, align: 'center' as const },
  ]
  if (canViewCustomerInRfq.value) {
    cols.push({ key: 'customerName', label: t('rfqList.columns.customer'), prop: 'customerName', minWidth: 200, showOverflowTooltip: true })
  }
  cols.push(
  { key: 'product', label: t('rfqList.columns.product'), prop: 'product', minWidth: 150, showOverflowTooltip: true },
  { key: 'industry', label: t('rfqList.columns.industry'), prop: 'industry', width: 100 },
  { key: 'itemCount', label: t('rfqList.columns.itemCount'), prop: 'itemCount', width: 80, align: 'center' as const },
  { key: 'importance', label: t('rfqList.columns.importance'), prop: 'importance', width: 90, align: 'center' as const },
  { key: 'rfqType', label: t('rfqList.columns.type'), prop: 'rfqType', width: 90 },
  { key: 'salesUserName', label: t('rfqList.columns.salesUser'), prop: 'salesUserName', width: 100 },
  {
    key: 'rfqCode',
    label: t('rfqList.columns.rfqCode'),
    prop: 'rfqCode',
    width: 160,
    minWidth: 160,
    showOverflowTooltip: true,
    sortable: true
  },
  { key: 'createTime', label: t('rfqList.columns.createTime'), width: 160 },
  { key: 'createUser', label: t('rfqList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('rfqList.actions.column'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
  )
  return cols
})

const importDialogVisible = ref(false)

const totalCount = computed(() => rfqList.value.length)

// 状态处理
const getStatusType = (status: number) => {
  const map: Record<number, string> = { 
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success', 
    4: 'success', 5: 'success', 6: 'danger'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: t('rfqList.status.pending'), 1: t('rfqList.status.assigned'), 2: t('rfqList.status.processing'), 3: t('rfqList.status.quoted'),
    4: t('rfqList.status.selected'), 5: t('rfqList.status.converted'), 6: t('rfqList.status.closed')
  }
  return map[status] || t('rfqList.status.unknown')
}

const getTypeText = (type: number) => {
  const s = formatRfqTypeLabel(type)
  return s === '—' ? t('rfqList.status.unknown') : s
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await rfqApi.searchRFQs({
      keyword: searchForm.value.keyword,
      status: searchForm.value.status,
      pageNumber: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize
    })
    rfqList.value = res.items || []
    pageInfo.value.total = res.totalCount || 0
    
    // 计算统计数据
    stats.value = {
      total: res.totalCount || 0,
      pending: rfqList.value.filter((r: any) => r.status === 0).length,
      processing: rfqList.value.filter((r: any) => r.status === 1 || r.status === 2).length,
      quoted: rfqList.value.filter((r: any) => r.status >= 3).length
    }
  } catch (error) {
    ElMessage.error(t('rfqList.loadFailed'))
  } finally {
    loading.value = false
  }
}

// 与左侧「检索」面板共用 URL 查询参数（keyword、status）
const handleSearch = () => {
  pageInfo.value.page = 1
  const q: Record<string, string> = {}
  const kw = searchForm.value.keyword.trim()
  if (kw) q.keyword = kw
  if (searchForm.value.status !== undefined && searchForm.value.status !== null) {
    q.status = String(searchForm.value.status)
  }
  router.replace({ name: 'RFQList', query: q })
}

const handleReset = () => {
  router.replace({ name: 'RFQList', query: {} })
}

watch(
  () => [route.name, route.query] as const,
  () => {
    if (route.name !== 'RFQList') return
    const kw = typeof route.query.keyword === 'string' ? route.query.keyword : ''
    let st: number | undefined = undefined
    const qs = route.query.status
    if (qs !== undefined && qs !== null && qs !== '') {
      const raw = Array.isArray(qs) ? qs[0] : qs
      const n = Number(raw)
      if (!Number.isNaN(n)) st = n
    }
    searchForm.value = { keyword: kw, status: st }
    pageInfo.value.page = 1
    loadData()
  },
  { deep: true, immediate: true }
)

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  loadData()
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
  loadData()
}

// 新建
// 导入 Excel 创建 RFQ 成功后刷新列表
const handleImportCreated = (_rfqId: string) => {
  loadData()
}

// 编辑：与「新建需求」共用 RFQCreate 页面（路由 rfqs/:id/edit）
const handleEdit = (row: any) => {
  if (!authStore.hasPermission('rfq.write')) {
    ElMessage.warning(t('rfqList.editNeedRfqWrite'))
    return
  }
  router.push({ name: 'RFQEdit', params: { id: row.id } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'RFQDetail', params: { id: row.id } })
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-list-page {
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
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 10px; }
  .page-title { margin: 0; color: $text-primary; font-size: 20px; }
  .count-badge {
    padding: 3px 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    font-size: 12px;
    color: $text-muted;
  }
}

.page-title-group {
  display: flex; align-items: center; gap: 10px;
  .page-icon {
    width: 36px; height: 36px; border-radius: 10px; display: flex; align-items: center; justify-content: center;
    background: rgba(0, 212, 255, 0.1); border: 1px solid rgba(0, 212, 255, 0.25); color: $cyan-primary; font-weight: 700;
  }
}

.statistics-row { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 20px; }
.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 20px;
  text-align: center;
  .stat-value {
    font-size: 22px;
    font-weight: 700;
    color: $text-primary;
    margin-bottom: 5px;
    font-family: 'Space Mono', monospace;
  }
  .stat-label { font-size: 12px; color: $text-muted; }
}

.filter-card {
  margin-bottom: 20px;
  background: $layer-2;
  border: 1px solid $border-panel;
}

.table-card {
  background: $layer-2;
  border: 1px solid $border-panel;
  // 避免卡片 body 形成裁剪/叠层，导致固定操作列无法盖住横向滚动区
  &.rfq-list-table-card :deep(.el-card__body) {
    overflow: visible;
  }
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(255, 255, 255, 0.03);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: $border-panel;
    color: $text-primary;

    .el-table__cell .cell { white-space: nowrap; }
  }
}

.pagination-wrapper {
  margin-top: 16px;
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

.btn-primary {
  border-radius: $border-radius-md;
}

// 新建/新增/创建（列表操作按钮颜色规范 PRD：success 绿）
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  .btn-success__icon {
    font-size: 14px;
  }

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
  }
}

// 操作列 op-col 底色与固定列叠层：main.scss 全局 .el-table 规则；按钮：crm-unified-list.scss .crm-data-table

.quantum-pagination {
  :deep(.el-pagination__total) { color: $text-muted; }
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
