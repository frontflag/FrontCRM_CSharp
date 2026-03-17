<template>
  <div class="demand-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <h1 class="page-title">需求管理</h1>
        <span class="page-subtitle">共 {{ totalCount }} 条需求</span>
      </div>
      <div class="page-header-right">
        <el-button type="primary" :icon="Plus" @click="handleCreate">新增需求</el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-row">
      <div class="stat-card">
        <div class="stat-icon stat-icon--total"><el-icon><Document /></el-icon></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.total }}</div>
          <div class="stat-label">需求总数</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--pending"><el-icon><Clock /></el-icon></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.pending }}</div>
          <div class="stat-label">待处理</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--processing"><el-icon><Loading /></el-icon></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.processing }}</div>
          <div class="stat-label">处理中</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--quoted"><el-icon><Tickets /></el-icon></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.quoted }}</div>
          <div class="stat-label">已报价</div>
        </div>
      </div>
    </div>

    <!-- 搜索筛选栏 -->
    <div class="search-bar">
      <el-input
        v-model="searchForm.searchTerm"
        placeholder="需求编号/客户名称/物料编码"
        clearable
        class="search-input"
        @keyup.enter="handleSearch"
      >
        <template #prefix><el-icon><Search /></el-icon></template>
      </el-input>
      <el-select v-model="searchForm.status" placeholder="全部状态" clearable class="filter-select">
        <el-option label="草稿" :value="0" />
        <el-option label="待处理" :value="1" />
        <el-option label="已分配" :value="2" />
        <el-option label="处理中" :value="3" />
        <el-option label="已报价" :value="4" />
        <el-option label="已接受" :value="5" />
        <el-option label="已拒绝" :value="6" />
        <el-option label="已关闭" :value="7" />
        <el-option label="已取消" :value="8" />
      </el-select>
      <el-select v-model="searchForm.source" placeholder="全部来源" clearable class="filter-select">
        <el-option label="手动录入" :value="1" />
        <el-option label="导入" :value="2" />
        <el-option label="邮件" :value="3" />
        <el-option label="在线" :value="4" />
        <el-option label="电话" :value="5" />
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
      <el-checkbox v-model="searchForm.isImportant" label="仅重要" class="important-check" @change="handleSearch" />
      <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
      <el-button :icon="Refresh" @click="handleReset">重置</el-button>
    </div>

    <!-- 数据表格 -->
    <div class="table-container">
      <el-table
        v-loading="loading"
        :data="demandList"
        stripe
        row-key="id"
        @sort-change="handleSortChange"
      >
        <el-table-column type="index" width="50" label="#" />
        <el-table-column prop="demandCode" label="需求编号" min-width="140" sortable="custom">
          <template #default="{ row }">
            <div class="demand-code-cell">
              <el-icon v-if="row.isImportant" class="important-icon"><StarFilled /></el-icon>
              <span class="demand-code" @click="handleView(row)">{{ row.demandCode }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户名称" min-width="160">
          <template #default="{ row }">
            <span class="text-ellipsis">{{ row.customerName || '--' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="demandDate" label="需求日期" width="110" sortable="custom">
          <template #default="{ row }">{{ formatDate(row.demandDate) }}</template>
        </el-table-column>
        <el-table-column prop="expectedDeliveryDate" label="期望交期" width="110">
          <template #default="{ row }">{{ formatDate(row.expectedDeliveryDate) || '--' }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="明细数" width="80" align="center">
          <template #default="{ row }">
            <el-tag size="small" type="info">{{ row.itemCount ?? 0 }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="salesUserName" label="业务员" width="90">
          <template #default="{ row }">{{ row.salesUserName || '--' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="source" label="来源" width="90" align="center">
          <template #default="{ row }">
            <span class="source-text">{{ getSourceLabel(row.source) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="primary" text @click="handleView(row)">查看</el-button>
            <el-button size="small" type="warning" text @click="handleEdit(row)">编辑</el-button>
            <el-dropdown size="small" @command="(cmd: string) => handleCommand(cmd, row)">
              <el-button size="small" text>更多<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="setImportant">
                    {{ row.isImportant ? '取消重要' : '标记重要' }}
                  </el-dropdown-item>
                  <el-dropdown-item command="assign">分配采购员</el-dropdown-item>
                  <el-dropdown-item command="changeStatus">变更状态</el-dropdown-item>
                  <el-dropdown-item command="close" divided>关闭需求</el-dropdown-item>
                  <el-dropdown-item command="delete" class="danger-item">删除需求</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :total="totalCount"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSearch"
          @current-change="handlePageChange"
        />
      </div>
    </div>

    <!-- 分配采购员弹窗 -->
    <el-dialog v-model="assignDialogVisible" title="分配采购员" width="480px" :close-on-click-modal="false">
      <div v-loading="purchasersLoading">
        <el-form :model="assignForm" label-width="90px">
          <el-form-item label="采购员" required>
            <el-select v-model="assignForm.purchaserId" placeholder="请选择采购员" style="width: 100%">
              <el-option
                v-for="p in purchaserList"
                :key="p.id"
                :label="`${p.name}（处理中: ${p.handlingCount ?? 0}）`"
                :value="p.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="assignForm.remark" type="textarea" :rows="2" placeholder="分配备注（可选）" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="assignDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="assignLoading" @click="handleAssignConfirm">确认分配</el-button>
      </template>
    </el-dialog>

    <!-- 变更状态弹窗 -->
    <el-dialog v-model="statusDialogVisible" title="变更需求状态" width="420px" :close-on-click-modal="false">
      <el-form :model="statusForm" label-width="90px">
        <el-form-item label="新状态" required>
          <el-select v-model="statusForm.status" placeholder="请选择状态" style="width: 100%">
            <el-option label="草稿" :value="0" />
            <el-option label="待处理" :value="1" />
            <el-option label="已分配" :value="2" />
            <el-option label="处理中" :value="3" />
            <el-option label="已报价" :value="4" />
            <el-option label="已接受" :value="5" />
            <el-option label="已拒绝" :value="6" />
            <el-option label="已关闭" :value="7" />
            <el-option label="已取消" :value="8" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="statusForm.remark" type="textarea" :rows="2" placeholder="变更原因（可选）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="statusDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="statusLoading" @click="handleStatusConfirm">确认变更</el-button>
      </template>
    </el-dialog>

    <!-- 关闭需求弹窗 -->
    <el-dialog v-model="closeDialogVisible" title="关闭需求" width="420px" :close-on-click-modal="false">
      <el-form :model="closeForm" label-width="90px">
        <el-form-item label="关闭类型" required>
          <el-select v-model="closeForm.closeType" placeholder="请选择" style="width: 100%">
            <el-option label="正常关闭" :value="1" />
            <el-option label="客户取消" :value="2" />
            <el-option label="价格不符" :value="3" />
            <el-option label="其他原因" :value="9" />
          </el-select>
        </el-form-item>
        <el-form-item label="关闭原因" required>
          <el-input v-model="closeForm.closeReason" type="textarea" :rows="3" placeholder="请填写关闭原因" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="closeForm.remark" type="textarea" :rows="2" placeholder="备注（可选）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="closeDialogVisible = false">取消</el-button>
        <el-button type="danger" :loading="closeLoading" @click="handleCloseConfirm">确认关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import {
  Plus, Search, Refresh, Document, Clock, Loading, Tickets,
  StarFilled, ArrowDown
} from '@element-plus/icons-vue'
import { demandApi } from '@/api/demand'
import type { Demand, DemandSearchRequest } from '@/types/demand'

const router = useRouter()

// ─── 状态 ───
const loading = ref(false)
const demandList = ref<Demand[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

const searchForm = reactive<DemandSearchRequest>({
  searchTerm: '',
  status: '',
  source: '',
  isImportant: undefined,
})
const dateRange = ref<[string, string] | null>(null)

const stats = reactive({ total: 0, pending: 0, processing: 0, quoted: 0 })

// 分配采购员
const assignDialogVisible = ref(false)
const purchasersLoading = ref(false)
const assignLoading = ref(false)
const purchaserList = ref<any[]>([])
const assignForm = reactive({ purchaserId: '', remark: '' })
const currentDemandId = ref('')

// 变更状态
const statusDialogVisible = ref(false)
const statusLoading = ref(false)
const statusForm = reactive({ status: 1 as number, remark: '' })

// 关闭需求
const closeDialogVisible = ref(false)
const closeLoading = ref(false)
const closeForm = reactive({ closeType: 1, closeReason: '', remark: '' })

// ─── 工具函数 ───
function formatDate(d?: string): string {
  if (!d) return ''
  return d.slice(0, 10)
}

function getStatusLabel(status: number): string {
  const map: Record<number, string> = {
    0: '草稿', 1: '待处理', 2: '已分配', 3: '处理中',
    4: '已报价', 5: '已接受', 6: '已拒绝', 7: '已关闭', 8: '已取消',
  }
  return map[status] ?? '未知'
}

function getStatusTagType(status: number): '' | 'success' | 'warning' | 'danger' | 'info' {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    0: 'info', 1: 'warning', 2: '', 3: '', 4: 'success',
    5: 'success', 6: 'danger', 7: 'info', 8: 'info',
  }
  return map[status] ?? 'info'
}

function getSourceLabel(source?: number): string {
  const map: Record<number, string> = {
    1: '手动', 2: '导入', 3: '邮件', 4: '在线', 5: '电话',
  }
  return source ? (map[source] ?? '--') : '--'
}

// ─── 数据加载 ───
async function loadDemands() {
  loading.value = true
  try {
    const params: DemandSearchRequest = {
      ...searchForm,
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
    }
    if (dateRange.value) {
      params.startDate = dateRange.value[0]
      params.endDate = dateRange.value[1]
    }
    const res = await demandApi.searchDemands(params)
    demandList.value = res.items ?? []
    totalCount.value = res.totalCount ?? 0
    // 更新统计
    stats.total = res.totalCount ?? 0
    stats.pending = res.items?.filter(d => d.status === 1).length ?? 0
    stats.processing = res.items?.filter(d => d.status === 3).length ?? 0
    stats.quoted = res.items?.filter(d => d.status === 4).length ?? 0
  } catch (err: any) {
    ElNotification.error({ title: '加载失败', message: err?.message || '获取需求列表失败' })
    demandList.value = []
  } finally {
    loading.value = false
  }
}

// ─── 事件处理 ───
function handleSearch() {
  currentPage.value = 1
  loadDemands()
}

function handleReset() {
  searchForm.searchTerm = ''
  searchForm.status = ''
  searchForm.source = ''
  searchForm.isImportant = undefined
  dateRange.value = null
  handleSearch()
}

function handlePageChange(page: number) {
  currentPage.value = page
  loadDemands()
}

function handleSortChange({ prop, order }: { prop: string; order: string | null }) {
  searchForm.sortBy = prop
  searchForm.sortDescending = order === 'descending'
  loadDemands()
}

function handleCreate() {
  router.push('/demands/create')
}

function handleView(row: Demand) {
  router.push(`/demands/${row.id}`)
}

function handleEdit(row: Demand) {
  router.push(`/demands/${row.id}/edit`)
}

async function handleCommand(cmd: string, row: Demand) {
  currentDemandId.value = row.id
  switch (cmd) {
    case 'setImportant':
      await toggleImportant(row)
      break
    case 'assign':
      await openAssignDialog()
      break
    case 'changeStatus':
      statusForm.status = row.status
      statusForm.remark = ''
      statusDialogVisible.value = true
      break
    case 'close':
      closeForm.closeType = 1
      closeForm.closeReason = ''
      closeForm.remark = ''
      closeDialogVisible.value = true
      break
    case 'delete':
      await handleDelete(row)
      break
  }
}

async function toggleImportant(row: Demand) {
  try {
    await demandApi.setImportant(row.id, !row.isImportant)
    ElNotification.success({ title: '操作成功', message: row.isImportant ? '已取消重要标记' : '已标记为重要需求' })
    loadDemands()
  } catch (err: any) {
    ElNotification.error({ title: '操作失败', message: err?.message || '操作失败' })
  }
}

async function openAssignDialog() {
  assignForm.purchaserId = ''
  assignForm.remark = ''
  assignDialogVisible.value = true
  purchasersLoading.value = true
  try {
    purchaserList.value = await demandApi.getPurchasers()
  } catch {
    purchaserList.value = []
  } finally {
    purchasersLoading.value = false
  }
}

async function handleAssignConfirm() {
  if (!assignForm.purchaserId) {
    ElNotification.warning({ title: '请选择采购员', message: '采购员不能为空' })
    return
  }
  assignLoading.value = true
  try {
    await demandApi.assignPurchaser(currentDemandId.value, {
      purchaserId: assignForm.purchaserId,
      remark: assignForm.remark,
    })
    ElNotification.success({ title: '分配成功', message: '采购员已成功分配' })
    assignDialogVisible.value = false
    loadDemands()
  } catch (err: any) {
    ElNotification.error({ title: '分配失败', message: err?.message || '分配失败' })
  } finally {
    assignLoading.value = false
  }
}

async function handleStatusConfirm() {
  statusLoading.value = true
  try {
    await demandApi.updateFlowStatus(currentDemandId.value, {
      status: statusForm.status,
      remark: statusForm.remark,
    })
    ElNotification.success({ title: '状态已更新', message: `需求状态已变更为「${getStatusLabel(statusForm.status)}」` })
    statusDialogVisible.value = false
    loadDemands()
  } catch (err: any) {
    ElNotification.error({ title: '变更失败', message: err?.message || '状态变更失败' })
  } finally {
    statusLoading.value = false
  }
}

async function handleCloseConfirm() {
  if (!closeForm.closeReason.trim()) {
    ElNotification.warning({ title: '请填写关闭原因', message: '关闭原因不能为空' })
    return
  }
  closeLoading.value = true
  try {
    await demandApi.addCloseRecord(currentDemandId.value, closeForm)
    await demandApi.updateFlowStatus(currentDemandId.value, { status: 7 })
    ElNotification.success({ title: '需求已关闭', message: '需求已成功关闭并记录关闭信息' })
    closeDialogVisible.value = false
    loadDemands()
  } catch (err: any) {
    ElNotification.error({ title: '关闭失败', message: err?.message || '关闭失败' })
  } finally {
    closeLoading.value = false
  }
}

async function handleDelete(row: Demand) {
  try {
    await ElMessageBox.confirm(
      `确定要删除需求「${row.demandCode}」吗？此操作不可恢复。`,
      '删除确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning', confirmButtonClass: 'el-button--danger' }
    )
    await demandApi.deleteDemand(row.id)
    ElNotification.success({ title: '删除成功', message: `需求「${row.demandCode}」已删除` })
    loadDemands()
  } catch (err: any) {
    if (err !== 'cancel') {
      ElNotification.error({ title: '删除失败', message: err?.message || '删除失败' })
    }
  }
}

onMounted(() => {
  loadDemands()
})
</script>

<style scoped lang="scss">
.demand-list-page {
  padding: 20px;
  background: var(--bg-main, #0f172a);
  min-height: 100%;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin: 0;
}

.page-subtitle {
  font-size: 13px;
  color: var(--el-text-color-secondary);
  margin-left: 10px;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  padding: 16px 20px;
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;

  &--total { background: rgba(59, 130, 246, 0.15); color: #3b82f6; }
  &--pending { background: rgba(245, 158, 11, 0.15); color: #f59e0b; }
  &--processing { background: rgba(139, 92, 246, 0.15); color: #8b5cf6; }
  &--quoted { background: rgba(16, 185, 129, 0.15); color: #10b981; }
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--el-text-color-primary);
  line-height: 1;
}

.stat-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}

.search-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  padding: 14px 16px;
  margin-bottom: 16px;
}

.search-input { width: 240px; }
.filter-select { width: 130px; }
.date-picker { width: 240px; }
.important-check { margin: 0 4px; }

.table-container {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  overflow: hidden;
}

.demand-code-cell {
  display: flex;
  align-items: center;
  gap: 4px;
}

.important-icon {
  color: #f59e0b;
  font-size: 14px;
}

.demand-code {
  color: var(--el-color-primary);
  cursor: pointer;
  font-weight: 500;
  &:hover { text-decoration: underline; }
}

.text-ellipsis {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: block;
}

.source-text {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.pagination-wrap {
  display: flex;
  justify-content: flex-end;
  padding: 12px 16px;
  border-top: 1px solid var(--el-border-color-lighter);
}

:deep(.danger-item) {
  color: var(--el-color-danger) !important;
}
</style>
