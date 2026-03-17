<template>
  <div class="rfq-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="12" r="10"/>
              <path d="M12 8v4l3 3"/>
            </svg>
          </div>
          <h1 class="page-title">需求管理</h1>
        </div>
        <div class="count-badge">共 {{ totalCount }} 条需求</div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleExport">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
            <polyline points="7 10 12 15 17 10"/>
            <line x1="12" y1="15" x2="12" y2="3"/>
          </svg>
          导出
        </button>
        <button class="btn-primary" @click="handleCreate">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          新增需求
        </button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="statistics-row">
      <div class="stat-card">
        <div class="stat-icon stat-icon--blue">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ stats.total }}</div>
          <div class="stat-label">需求总数</div>
        </div>
        <div class="stat-glow stat-glow--blue"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--amber">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="10"/>
            <path d="M12 8v4l3 3"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ stats.pending }}</div>
          <div class="stat-label">待处理</div>
        </div>
        <div class="stat-glow stat-glow--amber"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--cyan">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ stats.processing }}</div>
          <div class="stat-label">处理中</div>
        </div>
        <div class="stat-glow stat-glow--cyan"></div>
      </div>
      <div class="stat-card">
        <div class="stat-icon stat-icon--green">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <polyline points="20 6 9 17 4 12"/>
          </svg>
        </div>
        <div class="stat-body">
          <div class="stat-value">{{ stats.quoted }}</div>
          <div class="stat-label">已报价</div>
        </div>
        <div class="stat-glow stat-glow--green"></div>
      </div>
    </div>

    <!-- 搜索面板 -->
    <div class="search-panel">
      <div class="search-panel-inner">
        <div class="search-field" style="flex: 1; min-width: 200px;">
          <div class="field-label">搜索</div>
          <div class="input-wrapper">
            <svg class="input-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            <el-input
              v-model="searchForm.keyword"
              placeholder="需求编号/客户名称/物料编码"
              style="width: 100%"
              @keyup.enter="handleSearch"
            />
          </div>
        </div>

        <div class="search-field" style="width: 140px;">
          <div class="field-label">状态</div>
          <el-select v-model="searchForm.status" placeholder="全部状态" clearable class="custom-select" style="width: 100%">
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
        </div>

        <div class="search-field" style="width: 140px;">
          <div class="field-label">来源</div>
          <el-select v-model="searchForm.source" placeholder="全部来源" clearable class="custom-select" style="width: 100%">
            <el-option label="线下" :value="1" />
            <el-option label="线上" :value="2" />
            <el-option label="邮件" :value="3" />
            <el-option label="电话" :value="4" />
            <el-option label="导入" :value="5" />
          </el-select>
        </div>

        <div class="search-field" style="width: 200px;">
          <div class="field-label">开始日期</div>
          <el-date-picker
            v-model="searchForm.startDate"
            type="date"
            placeholder="开始日期"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            style="width: 100%"
            class="custom-date"
          />
        </div>

        <div class="search-field" style="width: 200px;">
          <div class="field-label">结束日期</div>
          <el-date-picker
            v-model="searchForm.endDate"
            type="date"
            placeholder="结束日期"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            style="width: 100%"
            class="custom-date"
          />
        </div>

        <div class="search-actions">
          <button class="btn-ghost" @click="handleReset">仅重置</button>
          <button class="btn-primary btn-sm" @click="handleSearch">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
            搜索
          </button>
        </div>
      </div>
    </div>

    <!-- 表格面板 -->
    <div class="table-panel">
      <el-table
        :data="tableData"
        v-loading="loading"
        class="quantum-table"
        :header-cell-style="headerCellStyle"
        :cell-style="cellStyle"
        row-key="id"
      >
        <el-table-column type="index" label="#" width="50" align="center">
          <template #default="{ $index }">
            <span class="row-index">{{ (currentPage - 1) * pageSize + $index + 1 }}</span>
          </template>
        </el-table-column>

        <el-table-column label="需求编号" width="160">
          <template #default="{ row }">
            <span class="code-link" @click="handleView(row)">{{ row.rfqCode || '—' }}</span>
          </template>
        </el-table-column>

        <el-table-column label="客户名称" min-width="180">
          <template #default="{ row }">
            <div class="name-cell">
              <div class="name-avatar">{{ row.customerName?.charAt(0) || '?' }}</div>
              <span class="name-text">{{ row.customerName || '—' }}</span>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="需求日期" width="120">
          <template #default="{ row }">
            <span class="text-secondary">{{ formatDate(row.rfqDate) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="需求类型" width="100" align="center">
          <template #default="{ row }">
            <span class="text-secondary">{{ getRFQTypeLabel(row.rfqType) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="明细数" width="80" align="center">
          <template #default="{ row }">
            <span class="count-tag">{{ row.itemCount ?? 0 }}</span>
          </template>
        </el-table-column>

        <el-table-column label="业务员" width="100">
          <template #default="{ row }">
            <span class="text-secondary">{{ row.salesUserName || '—' }}</span>
          </template>
        </el-table-column>

        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <span :class="['status-badge', `status-${row.status}`]">{{ getStatusLabel(row.status) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="来源" width="90" align="center">
          <template #default="{ row }">
            <span class="source-tag">{{ getSourceLabel(row.source) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="操作" width="160" fixed="right" align="center">
          <template #default="{ row }">
            <button class="action-btn" @click="handleView(row)">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                <circle cx="12" cy="12" r="3"/>
              </svg>
              查看
            </button>
            <button class="action-btn" @click="handleEdit(row)" v-if="row.status === 0 || row.status === 1">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
              编辑
            </button>
            <el-dropdown trigger="click" @command="(cmd: string) => handleCommand(cmd, row)">
              <button class="action-btn action-btn--more">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/>
                </svg>
              </button>
              <template #dropdown>
                <el-dropdown-menu class="quantum-dropdown">
                  <el-dropdown-item command="assign">分配采购员</el-dropdown-item>
                  <el-dropdown-item command="close" v-if="row.status < 7">关闭需求</el-dropdown-item>
                  <el-dropdown-item command="delete" class="danger-item">删除</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[20, 50, 100]"
          :total="totalCount"
          layout="total, sizes, prev, pager, next, jumper"
          class="quantum-pagination"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>

    <!-- 分配采购员弹窗 -->
    <el-dialog v-model="assignDialogVisible" title="分配采购员" width="480px" :close-on-click-modal="false">
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
      <template #footer>
        <button class="btn-secondary" @click="assignDialogVisible = false">取消</button>
        <button class="btn-primary" :disabled="assignLoading" @click="handleAssignConfirm" style="margin-left: 8px;">
          {{ assignLoading ? '分配中...' : '确认分配' }}
        </button>
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
      </el-form>
      <template #footer>
        <button class="btn-secondary" @click="closeDialogVisible = false">取消</button>
        <button class="btn-primary" :disabled="closeLoading" @click="handleCloseConfirm" style="margin-left: 8px;">
          {{ closeLoading ? '关闭中...' : '确认关闭' }}
        </button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import { rfqApi } from '@/api/rfq'

const router = useRouter()
const loading = ref(false)
const tableData = ref<any[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

const stats = reactive({ total: 0, pending: 0, processing: 0, quoted: 0 })

const searchForm = reactive({
  keyword: '',
  status: undefined as number | undefined,
  source: undefined as number | undefined,
  startDate: '',
  endDate: ''
})

// 分配采购员
const assignDialogVisible = ref(false)
const assignLoading = ref(false)
const purchaserList = ref<any[]>([])
const assignForm = reactive({ rfqId: '', purchaserId: '', remark: '' })

// 关闭需求
const closeDialogVisible = ref(false)
const closeLoading = ref(false)
const closeForm = reactive({ rfqId: '', closeType: 1, closeReason: '' })

const headerCellStyle = {
  background: 'rgba(0,0,0,0.15)',
  color: '#8A9BB0',
  fontSize: '12px',
  fontWeight: '500',
  borderBottom: '1px solid rgba(255,255,255,0.06)',
  padding: '10px 0'
}

const cellStyle = {
  borderBottom: '1px solid rgba(255,255,255,0.04)',
  padding: '10px 0'
}

function getStatusLabel(status: number) {
  const map: Record<number, string> = { 0: '草稿', 1: '待处理', 2: '已分配', 3: '处理中', 4: '已报价', 5: '已接受', 6: '已拒绝', 7: '已关闭', 8: '已取消' }
  return map[status] ?? '未知'
}

function getSourceLabel(source: number) {
  const map: Record<number, string> = { 1: '线下', 2: '线上', 3: '邮件', 4: '电话', 5: '导入' }
  return map[source] ?? '—'
}

function getRFQTypeLabel(type: number) {
  const map: Record<number, string> = { 1: '现货', 2: '期货', 3: '样品', 4: '批量' }
  return map[type] ?? '—'
}

function formatDate(val: string) {
  if (!val) return '—'
  return val.split('T')[0]
}

async function loadData() {
  loading.value = true
  try {
    const res = await rfqApi.searchRFQs({
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
      keyword: searchForm.keyword || undefined,
      status: searchForm.status,
      source: searchForm.source,
      startDate: searchForm.startDate || undefined,
      endDate: searchForm.endDate || undefined
    })
    tableData.value = res.items || []
    totalCount.value = res.totalCount || 0
    stats.total = res.totalCount || 0
    stats.pending = 0
    stats.processing = 0
    stats.quoted = 0
  } catch {
    ElNotification.error({ title: '加载失败', message: '需求列表加载失败，请稍后重试' })
  } finally {
    loading.value = false
  }
}

function handleSearch() { currentPage.value = 1; loadData() }

function handleReset() {
  searchForm.keyword = ''
  searchForm.status = undefined
  searchForm.source = undefined
  searchForm.startDate = ''
  searchForm.endDate = ''
  currentPage.value = 1
  loadData()
}

function handleCreate() { router.push('/rfqs/create') }
function handleView(row: any) { router.push(`/rfqs/${row.id}`) }
function handleEdit(row: any) { router.push(`/rfqs/${row.id}/edit`) }

function handleExport() {
  ElNotification.info({ title: '功能开发中', message: '导出功能正在开发中，敬请期待' })
}

async function handleCommand(cmd: string, row: any) {
  if (cmd === 'assign') {
    assignForm.rfqId = row.id
    assignForm.purchaserId = ''
    assignForm.remark = ''
    try {
      const list = await rfqApi.getPurchasers()
      purchaserList.value = list || []
    } catch { purchaserList.value = [] }
    assignDialogVisible.value = true
  } else if (cmd === 'close') {
    closeForm.rfqId = row.id
    closeForm.closeType = 1
    closeForm.closeReason = ''
    closeDialogVisible.value = true
  } else if (cmd === 'delete') {
    try {
      await ElMessageBox.confirm(`确定删除需求「${row.rfqCode}」吗？此操作不可撤销。`, '删除确认', {
        confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'error'
      })
      await rfqApi.deleteRFQ(row.id)
      ElNotification.success({ title: '删除成功', message: '需求已删除' })
      loadData()
    } catch { /* 取消 */ }
  }
}

async function handleAssignConfirm() {
  if (!assignForm.purchaserId) {
    ElNotification.warning({ title: '请选择采购员', message: '采购员不能为空' })
    return
  }
  assignLoading.value = true
  try {
    await rfqApi.assignPurchaser(assignForm.rfqId, { purchaserId: assignForm.purchaserId, remark: assignForm.remark })
    ElNotification.success({ title: '分配成功', message: '采购员已成功分配' })
    assignDialogVisible.value = false
    loadData()
  } catch {
    ElNotification.error({ title: '分配失败', message: '采购员分配失败，请重试' })
  } finally {
    assignLoading.value = false
  }
}

async function handleCloseConfirm() {
  if (!closeForm.closeReason) {
    ElNotification.warning({ title: '请填写关闭原因', message: '关闭原因不能为空' })
    return
  }
  closeLoading.value = true
  try {
    await rfqApi.addCloseRecord(closeForm.rfqId, { closeType: closeForm.closeType, closeReason: closeForm.closeReason })
    ElNotification.success({ title: '操作成功', message: '需求已关闭' })
    closeDialogVisible.value = false
    loadData()
  } catch {
    ElNotification.error({ title: '操作失败', message: '关闭需求失败，请重试' })
  } finally {
    closeLoading.value = false
  }
}

onMounted(loadData)
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

// ---- 页面头部 ----
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;

  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 10px; }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

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
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 按钮 ----
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

  &:hover { transform: translateY(-1px); box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25); }
  &.btn-sm { padding: 6px 12px; font-size: 12px; }
  &:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(255, 255, 255, 0.08); border-color: rgba(0, 212, 255, 0.25); color: $text-primary; }
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

  &:hover { border-color: rgba(0, 212, 255, 0.3); color: $text-secondary; }
}

// ---- 统计卡片 ----
.statistics-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.stat-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  position: relative;
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover { transform: translateY(-2px); box-shadow: $shadow-md; }
}

.stat-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  &--blue  { background: rgba(50, 149, 201, 0.15); color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.25); }
  &--green { background: rgba(70, 191, 145, 0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.25); }
  &--cyan  { background: rgba(0, 212, 255, 0.12);  color: $cyan-primary;     border: 1px solid rgba(0,212,255,0.25); }
  &--amber { background: rgba(201, 154, 69, 0.15); color: $color-amber;      border: 1px solid rgba(201,154,69,0.25); }
}

.stat-body {
  .stat-value { font-size: 22px; font-weight: 700; color: $text-primary; font-family: 'Space Mono', monospace; line-height: 1.2; }
  .stat-label { font-size: 12px; color: $text-muted; margin-top: 3px; }
}

.stat-glow {
  position: absolute;
  right: -20px;
  top: -20px;
  width: 80px;
  height: 80px;
  border-radius: 50%;
  filter: blur(30px);
  opacity: 0.4;

  &--blue  { background: $color-steel-cyan; }
  &--green { background: $color-mint-green; }
  &--cyan  { background: $cyan-primary; }
  &--amber { background: $color-amber; }
}

// ---- 搜索面板 ----
.search-panel {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 18px 20px;
  margin-bottom: 16px;
}

.search-panel-inner {
  display: flex;
  align-items: flex-end;
  gap: 16px;
  flex-wrap: wrap;
}

.search-field {
  display: flex;
  flex-direction: column;
  gap: 6px;

  .field-label {
    font-size: 11px;
    font-weight: 500;
    color: $text-muted;
    letter-spacing: 0.5px;
    text-transform: uppercase;
  }
}

.search-actions {
  display: flex;
  gap: 8px;
  align-items: center;
  padding-bottom: 1px;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;

  .input-icon {
    position: absolute;
    left: 10px;
    z-index: 2;
    color: rgba(0, 212, 255, 0.45);
    display: flex;
    align-items: center;
    pointer-events: none;
  }

  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    padding-left: 32px !important;
    height: 34px;

    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; box-shadow: 0 0 0 2px rgba(0, 212, 255, 0.08) !important; }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }
}

.custom-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    height: 34px;
    color: $text-primary !important;

    &.is-focused { border-color: rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-select__placeholder) { color: $text-placeholder !important; }
}

.custom-date {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    height: 34px;

    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }

  :deep(.el-input__inner) {
    color: $text-primary !important;
    &::placeholder { color: $text-placeholder !important; }
  }
}

// ---- 表格面板 ----
.table-panel {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.quantum-table {
  width: 100%;
  background: transparent !important;

  :deep(.el-table__inner-wrapper) { background: transparent; }
  :deep(.el-table__header-wrapper) { background: $layer-2; }
  :deep(.el-table__body-wrapper) { background: transparent; }

  :deep(tr) {
    background: transparent !important;
    transition: background 0.15s;

    &:hover td { background: rgba(0, 212, 255, 0.04) !important; }
  }

  :deep(.el-table__fixed-right) {
    background: $layer-2 !important;
    .el-table__fixed-right-patch { background: $layer-2; }
  }

  :deep(.el-loading-mask) { background: rgba(10, 22, 40, 0.7); }
}

.row-index { font-size: 12px; color: $text-muted; font-family: 'Space Mono', monospace; }

.code-link {
  color: $color-ice-blue;
  cursor: pointer;
  font-family: 'Space Mono', monospace;
  font-size: 12px;
  transition: color 0.2s;

  &:hover { color: $cyan-primary; text-decoration: underline; }
}

.name-cell {
  display: flex;
  align-items: center;
  gap: 10px;

  .name-avatar {
    width: 32px;
    height: 32px;
    background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
    border: 1px solid rgba(0, 212, 255, 0.2);
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 13px;
    font-weight: 600;
    color: $cyan-primary;
    flex-shrink: 0;
  }

  .name-text { font-size: 13px; color: $text-primary; font-weight: 500; }
}

.count-tag {
  display: inline-block;
  padding: 1px 8px;
  background: rgba(0, 212, 255, 0.1);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 10px;
  font-size: 12px;
  color: $cyan-primary;
  font-family: 'Space Mono', monospace;
}

.status-badge {
  display: inline-block;
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 500;

  &.status-0 { background: rgba(107,122,141,0.2); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &.status-1 { background: rgba(201,154,69,0.2);  color: $color-amber; border: 1px solid rgba(201,154,69,0.3); }
  &.status-2 { background: rgba(50,149,201,0.2);  color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.3); }
  &.status-3 { background: rgba(0,212,255,0.12);  color: $cyan-primary; border: 1px solid rgba(0,212,255,0.25); }
  &.status-4 { background: rgba(70,191,145,0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.3); }
  &.status-5 { background: rgba(70,191,145,0.2);  color: $color-mint-green; border: 1px solid rgba(70,191,145,0.4); }
  &.status-6 { background: rgba(201,87,69,0.15);  color: $color-red-brown; border: 1px solid rgba(201,87,69,0.3); }
  &.status-7 { background: rgba(107,122,141,0.15); color: #6B7A8D; border: 1px solid rgba(107,122,141,0.25); }
  &.status-8 { background: rgba(107,122,141,0.1);  color: #6B7A8D; border: 1px solid rgba(107,122,141,0.2); }
}

.source-tag { font-size: 11px; color: $text-muted; }
.text-secondary { color: $text-secondary; font-size: 13px; }

// 操作按钮
.action-btn {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 3px 8px;
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 4px;
  color: $color-ice-blue;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  margin-right: 4px;

  &:hover { background: rgba(0, 212, 255, 0.08); border-color: rgba(0, 212, 255, 0.4); color: $cyan-primary; }

  &--more { color: $text-muted; border-color: $border-panel;
    &:hover { color: $text-secondary; border-color: rgba(0, 212, 255, 0.2); }
  }
}

// 分页
.pagination-wrapper {
  padding: 16px 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.05);
  display: flex;
  justify-content: flex-end;
}

.quantum-pagination {
  :deep(.el-pagination__total),
  :deep(.el-pagination__sizes),
  :deep(.el-pagination__jump) { color: $text-muted !important; font-size: 12px; }

  :deep(.el-pager li) {
    background: transparent !important;
    color: $text-muted !important;
    border: 1px solid transparent;
    border-radius: 6px;
    min-width: 28px;
    height: 28px;
    line-height: 28px;

    &.is-active { background: rgba(0, 212, 255, 0.15) !important; color: $cyan-primary !important; border-color: rgba(0, 212, 255, 0.3); }
    &:hover:not(.is-active) { background: rgba(255, 255, 255, 0.05) !important; color: $text-secondary !important; }
  }

  :deep(.btn-prev), :deep(.btn-next) {
    background: transparent !important;
    color: $text-muted !important;
    border: 1px solid $border-panel;
    border-radius: 6px;

    &:hover { border-color: rgba(0, 212, 255, 0.3); color: $text-secondary !important; }
  }
}

:deep(.quantum-dropdown) {
  background: $layer-2 !important;
  border: 1px solid $border-card !important;
  border-radius: $border-radius-md !important;

  .el-dropdown-menu__item { color: $text-secondary !important; font-size: 13px;
    &:hover { background: rgba(0, 212, 255, 0.06) !important; color: $text-primary !important; }
  }

  .danger-item { color: $color-red-brown !important; }
}
</style>
