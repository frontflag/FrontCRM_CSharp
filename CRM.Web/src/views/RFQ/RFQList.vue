<template>
  <div class="rfq-list-page customer-list-theme">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">R</div>
          <h1 class="page-title">需求管理 (RFQ)</h1>
        </div>
        <div class="count-badge">共 {{ totalCount }} 条需求</div>
      </div>
      <div class="header-right">
        <el-button class="btn-ghost btn-sm" @click="importDialogVisible = true">
          <el-icon><Upload /></el-icon>导入 Excel 创建
        </el-button>
        <button class="btn-success" type="button" @click="router.push({ name: 'RFQCreate' })">
          <el-icon class="btn-success__icon"><Plus /></el-icon>
          新增需求
        </button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="statistics-row">
      <div class="stat-card">
        <div class="stat-value">{{ stats.total }}</div>
        <div class="stat-label">需求总数</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.pending }}</div>
        <div class="stat-label">待分配</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.processing }}</div>
        <div class="stat-label">报价中</div>
      </div>
      <div class="stat-card">
        <div class="stat-value">{{ stats.quoted }}</div>
        <div class="stat-label">已报价</div>
      </div>
    </div>

    <!-- 搜索面板 -->
    <el-card class="filter-card search-bar">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="搜索">
          <el-input 
            v-model="searchForm.keyword" 
            placeholder="需求编号/客户名称/产品"
            clearable
            @keyup.enter="handleSearch"
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 140px">
            <el-option label="待分配" :value="0" />
            <el-option label="已分配" :value="1" />
            <el-option label="报价中" :value="2" />
            <el-option label="已报价" :value="3" />
            <el-option label="已选价" :value="4" />
            <el-option label="已转订单" :value="5" />
            <el-option label="已关闭" :value="6" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button class="btn-primary btn-sm" type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>查询
          </el-button>
          <el-button class="btn-ghost btn-sm" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <!-- 勿在 el-card 上再加 table-wrapper：会与 CrmDataTable 内层 .table-wrapper 叠套，overflow 影响固定列叠层 -->
    <el-card class="table-card rfq-list-table-card">
      <CrmDataTable
        :data="rfqList"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <el-table-column prop="rfqCode" label="需求编号" width="160" min-width="160" show-overflow-tooltip sortable>
          <template #default="{ row }">
            <el-link type="primary" @click="handleView(row)">{{ row.rfqCode }}</el-link>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="160" align="center">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" min-width="200" show-overflow-tooltip />
        <el-table-column prop="product" label="产品" min-width="150" show-overflow-tooltip />
        <el-table-column prop="industry" label="行业" width="100" />
        <el-table-column prop="itemCount" label="明细数" width="80" align="center" />
        <el-table-column prop="importance" label="重要度" width="90" align="center">
          <template #default="{ row }">
            <el-rate v-model="row.importance" disabled :max="10" />
          </template>
        </el-table-column>
        <el-table-column prop="rfqType" label="类型" width="90">
          <template #default="{ row }">
            {{ getTypeText(row.rfqType) }}
          </template>
        </el-table-column>
        <el-table-column prop="salesUserName" label="业务员" width="100" />
        <el-table-column label="创建时间" width="160">
          <template #default="{ row }">
            {{ row.createTime ? formatDisplayDateTime(row.createTime) : '--' }}
          </template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.createUserName || row.createdBy || row.salesUserName || '—' }}
          </template>
        </el-table-column>
        <el-table-column
          label="操作"
          :width="opColWidth"
          :min-width="opColMinWidth"
          fixed="right"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="op-col-header">
              <span class="op-col-header-text">操作</span>
              <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
                {{ opColExpanded ? '>' : '<' }}
              </button>
            </div>
          </template>

          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="opColExpanded" class="action-btns">
                <button type="button" class="action-btn action-btn--primary" @click.stop="handleView(row)">查看</button>
                <button type="button" class="action-btn action-btn--primary" @click.stop="handleEdit(row)">编辑</button>
                <button type="button" class="action-btn action-btn--warning" @click.stop="handleGenerateQuote(row)">
                  生成报价
                </button>
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
                    <el-dropdown-item @click.stop="handleEdit(row)">
                      <span class="op-more-item op-more-item--primary">编辑</span>
                    </el-dropdown-item>
                    <el-dropdown-item @click.stop="handleGenerateQuote(row)">
                      <span class="op-more-item op-more-item--warning">生成报价</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </CrmDataTable>

      <!-- 分页 -->
      <div class="pagination-wrapper">
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
import { Plus, Search, Upload } from '@element-plus/icons-vue'
import ImportRFQDialog from './components/ImportRFQDialog.vue'
import { ElMessage } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatRfqTypeLabel } from '@/constants/rfqFormEnums'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
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
const OP_COL_EXPANDED_WIDTH = 236
const OP_COL_EXPANDED_MIN_WIDTH = 228
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

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
    0: '待分配', 1: '已分配', 2: '报价中', 3: '已报价',
    4: '已选价', 5: '已转订单', 6: '已关闭'
  }
  return map[status] || '未知'
}

const getTypeText = (type: number) => {
  const s = formatRfqTypeLabel(type)
  return s === '—' ? '未知' : s
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
    ElMessage.error('加载数据失败')
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
  router.push({ name: 'RFQEdit', params: { id: row.id } })
}

// 查看
const handleView = (row: any) => {
  router.push({ name: 'RFQDetail', params: { id: row.id } })
}

const handleGenerateQuote = (_row: any) => {
  ElMessage.success('已生成报价单')
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
  background: $layer-2;
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
  justify-content: flex-end;
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
