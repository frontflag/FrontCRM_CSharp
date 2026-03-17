<template>
  <div class="rfq-detail-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          返回
        </button>
        <div class="rfq-title-group">
          <div class="rfq-avatar-lg">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="12" r="10"/><path d="M12 8v4l3 3"/>
            </svg>
          </div>
          <div>
            <h1 class="page-title">{{ rfq?.rfqCode || 'RFQ 详情' }}</h1>
            <div class="title-meta">
              <span class="rfq-code">{{ rfq?.customerName }}</span>
              <span class="status-badge" :class="`status-${rfq?.status}`">{{ getStatusLabel(rfq?.status) }}</span>
              <span class="source-tag">{{ getSourceLabel(rfq?.source) }}</span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleEdit" v-if="rfq?.status === 0 || rfq?.status === 1">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
          </svg>
          编辑
        </button>
        <button class="btn-secondary" @click="showAssignDialog" v-if="rfq?.status !== 7 && rfq?.status !== 8">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
          </svg>
          分配采购员
        </button>
        <button class="btn-warning" @click="showCloseDialog" v-if="rfq?.status !== 7 && rfq?.status !== 8">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
          </svg>
          关闭需求
        </button>
        <button class="btn-danger" @click="handleDelete">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="3 6 5 6 21 6"/>
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
          </svg>
          删除需求
        </button>
      </div>
    </div>

    <div v-loading="loading" element-loading-background="rgba(10,22,40,0.8)" class="detail-content">
      <template v-if="rfq">
        <!-- 基本信息 -->
        <div class="info-section">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">需求编号</span>
              <span class="info-value info-value--code">{{ rfq.rfqCode || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">客户名称</span>
              <span class="info-value">{{ rfq.customerName || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">业务员</span>
              <span class="info-value">{{ rfq.salesUserName || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">需求日期</span>
              <span class="info-value info-value--time">{{ formatDate(rfq.rfqDate) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">期望交货日期</span>
              <span class="info-value info-value--time">{{ formatDate(rfq.expectedDeliveryDate) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">结算货币</span>
              <span class="info-value">{{ rfq.currency || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">需求来源</span>
              <span class="info-value">{{ getSourceLabel(rfq.source) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">质量要求</span>
              <span class="info-value">{{ rfq.qualityRequirement || '—' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">认证要求</span>
              <span class="info-value">{{ rfq.certificationRequirement || '—' }}</span>
            </div>
            <div class="info-item" style="grid-column: span 3">
              <span class="info-label">备注</span>
              <span class="info-value">{{ rfq.remark || '—' }}</span>
            </div>
          </div>
        </div>

        <!-- 采购员分配信息 -->
        <div class="info-section" v-if="rfq.purchaserName">
          <div class="section-header">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">采购员信息</span>
          </div>
          <div class="info-grid">
            <div class="info-item">
              <span class="info-label">当前采购员</span>
              <span class="info-value">{{ rfq.purchaserName }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">分配时间</span>
              <span class="info-value info-value--time">{{ formatDate(rfq.assignedAt) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">处理状态</span>
              <span class="info-value">{{ getPurchaserStatusLabel(rfq.purchaserStatus) }}</span>
            </div>
          </div>
        </div>

        <!-- 标签页 -->
        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              v-for="tab in tabs"
              :key="tab.key"
              :class="['tab-btn', { 'tab-btn--active': activeTab === tab.key }]"
              @click="activeTab = tab.key"
            >
              {{ tab.label }}
              <span v-if="tab.count !== undefined" class="tab-count">{{ tab.count }}</span>
            </button>
          </div>
          <div class="tabs-body">
            <!-- 需求明细 -->
            <div v-if="activeTab === 'items'">
              <div class="tab-toolbar">
                <span class="cell-muted">共 {{ rfqItems.length }} 条明细</span>
                <button class="btn-add-item" @click="loadItems" style="margin-left: auto;">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-3.51"/>
                  </svg>
                  刷新最优报价
                </button>
              </div>
              <el-table
                :data="rfqItems"
                v-loading="itemsLoading"
                class="quantum-table"
                :header-cell-style="headerCellStyle"
                :cell-style="cellStyle"
              >
                <el-table-column type="index" label="#" width="50" align="center">
                  <template #default="{ $index }"><span class="cell-muted">{{ $index + 1 }}</span></template>
                </el-table-column>
                <el-table-column label="物料编码" width="140">
                  <template #default="{ row }"><span class="cell-code">{{ row.materialCode || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="物料名称" min-width="160">
                  <template #default="{ row }"><span class="cell-primary">{{ row.materialName || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="规格型号" width="140">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.specification || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="数量" width="100" align="right">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.quantity }} {{ row.unit }}</span></template>
                </el-table-column>
                <el-table-column label="目标单价" width="110" align="right">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.targetUnitPrice ? `¥${row.targetUnitPrice}` : '—' }}</span></template>
                </el-table-column>
                <el-table-column label="最优报价" width="110" align="right">
                  <template #default="{ row }">
                    <span :class="row.bestQuotePrice ? 'cell-code' : 'cell-muted'">
                      {{ row.bestQuotePrice ? `¥${row.bestQuotePrice}` : '—' }}
                    </span>
                  </template>
                </el-table-column>
                <el-table-column label="交货日期" width="110">
                  <template #default="{ row }"><span class="cell-muted">{{ formatDate(row.deliveryDate) }}</span></template>
                </el-table-column>
                <el-table-column label="状态" width="90" align="center">
                  <template #default="{ row }">
                    <span :class="['status-badge', `status-${row.status}`]">{{ getStatusLabel(row.status) }}</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- 关闭记录 -->
            <div v-if="activeTab === 'closeRecords'">
              <div v-if="closeRecords.length === 0" class="empty-state">
                <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" opacity="0.3">
                  <path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
                </svg>
                <p>暂无关闭记录</p>
              </div>
              <el-table v-else :data="closeRecords" class="quantum-table" :header-cell-style="headerCellStyle" :cell-style="cellStyle">
                <el-table-column label="关闭类型" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ getCloseTypeLabel(row.closeType) }}</span></template>
                </el-table-column>
                <el-table-column label="关闭原因" min-width="200">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.reason || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="操作人" width="120">
                  <template #default="{ row }"><span class="cell-secondary">{{ row.operatorName || '—' }}</span></template>
                </el-table-column>
                <el-table-column label="关闭时间" width="160">
                  <template #default="{ row }"><span class="cell-muted">{{ formatDate(row.createdAt) }}</span></template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- 分配采购员弹窗 -->
    <el-dialog v-model="assignDialogVisible" title="分配采购员" width="480px" :close-on-click-modal="false">
      <div v-if="recommendedPurchaser" class="recommend-card">
        <div class="recommend-avatar">{{ recommendedPurchaser.name?.charAt(0) }}</div>
        <div>
          <div class="recommend-name">{{ recommendedPurchaser.name }}</div>
          <div class="recommend-meta">系统推荐 · 当前处理中：{{ recommendedPurchaser.handlingCount ?? 0 }} 条</div>
        </div>
        <button class="btn-use-recommend" @click="assignForm.purchaserId = recommendedPurchaser.id">使用推荐</button>
      </div>
      <el-form :model="assignForm" label-width="90px" style="margin-top: 16px;">
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
          <el-input v-model="closeForm.reason" type="textarea" :rows="3" placeholder="请填写关闭原因" />
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
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import { rfqApi } from '@/api/rfq'

const route = useRoute()
const router = useRouter()
const rfqId = route.params.id as string

const loading = ref(false)
const rfq = ref<any>(null)
const rfqItems = ref<any[]>([])
const closeRecords = ref<any[]>([])
const itemsLoading = ref(false)
const activeTab = ref('items')

const tabs = computed(() => [
  { key: 'items', label: '需求明细', count: rfqItems.value.length },
  { key: 'closeRecords', label: '关闭记录', count: closeRecords.value.length }
])

const assignDialogVisible = ref(false)
const assignLoading = ref(false)
const purchaserList = ref<any[]>([])
const recommendedPurchaser = ref<any>(null)
const assignForm = reactive({ purchaserId: '', remark: '' })

const closeDialogVisible = ref(false)
const closeLoading = ref(false)
const closeForm = reactive({ closeType: 1, reason: '' })

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

function getStatusLabel(status?: number) {
  const map: Record<number, string> = {
    0: '草稿', 1: '待处理', 2: '已分配', 3: '处理中',
    4: '已报价', 5: '已接受', 6: '已拒绝', 7: '已关闭', 8: '已取消'
  }
  return status !== undefined ? (map[status] ?? '未知') : '—'
}
function getSourceLabel(source?: number) {
  const map: Record<number, string> = { 1: '手动录入', 2: '导入', 3: '邮件', 4: '在线', 5: '电话' }
  return source !== undefined ? (map[source] ?? '—') : '—'
}
function getPurchaserStatusLabel(status?: number) {
  const map: Record<number, string> = { 0: '待处理', 1: '处理中', 2: '已完成', 3: '已拒绝' }
  return status !== undefined ? (map[status] ?? '—') : '—'
}
function getCloseTypeLabel(type?: number) {
  const map: Record<number, string> = { 1: '正常关闭', 2: '客户取消', 3: '价格不符', 9: '其他原因' }
  return type !== undefined ? (map[type] ?? '—') : '—'
}
function formatDate(val?: string) { if (!val) return '—'; return val.split('T')[0] }
function goBack() { router.push('/rfqs') }
function handleEdit() { router.push(`/rfqs/${rfqId}/edit`) }

async function loadRFQ() {
  loading.value = true
  try { rfq.value = await rfqApi.getRFQDetail(rfqId) }
  catch { ElNotification.error({ title: '加载失败', message: '需求详情加载失败，请检查网络连接' }) }
  finally { loading.value = false }
}

async function loadItems() {
  itemsLoading.value = true
  try { const res = await rfqApi.getRFQItemsWithBestQuote(rfqId); rfqItems.value = res || [] }
  catch { rfqItems.value = [] }
  finally { itemsLoading.value = false }
}

async function loadCloseRecords() {
  try { const res = await rfqApi.getCloseRecords(rfqId); closeRecords.value = res || [] }
  catch { closeRecords.value = [] }
}

async function showAssignDialog() {
  assignForm.purchaserId = ''; assignForm.remark = ''; recommendedPurchaser.value = null
  try {
    const [list, recommended] = await Promise.all([
      rfqApi.getPurchasers(),
      rfqApi.getRecommendedPurchasers(rfqId)
    ])
    purchaserList.value = list || []; recommendedPurchaser.value = recommended
  } catch { purchaserList.value = [] }
  assignDialogVisible.value = true
}

function showCloseDialog() {
  closeForm.closeType = 1; closeForm.reason = ''
  closeDialogVisible.value = true
}

async function handleAssignConfirm() {
  if (!assignForm.purchaserId) {
    ElNotification.warning({ title: '请选择采购员', message: '采购员不能为空' }); return
  }
  assignLoading.value = true
  try {
    await rfqApi.assignPurchaser(rfqId, { purchaserId: assignForm.purchaserId, remark: assignForm.remark })
    ElNotification.success({ title: '分配成功', message: '采购员已成功分配' })
    assignDialogVisible.value = false; loadRFQ()
  } catch { ElNotification.error({ title: '分配失败', message: '采购员分配失败，请重试' }) }
  finally { assignLoading.value = false }
}

async function handleCloseConfirm() {
  if (!closeForm.reason) {
    ElNotification.warning({ title: '请填写关闭原因', message: '关闭原因不能为空' }); return
  }
  closeLoading.value = true
  try {
    await rfqApi.addCloseRecord(rfqId, { closeType: closeForm.closeType, closeReason: closeForm.reason })
    ElNotification.success({ title: '操作成功', message: '需求已关闭' })
    closeDialogVisible.value = false; loadRFQ(); loadCloseRecords()
  } catch { ElNotification.error({ title: '操作失败', message: '关闭需求失败，请重试' }) }
  finally { closeLoading.value = false }
}

async function handleDelete() {
  try {
    await ElMessageBox.confirm(
      `确定删除需求「${rfq.value?.rfqCode}」吗？此操作不可撤销。`,
      '删除确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'error' }
    )
    await rfqApi.deleteRFQ(rfqId)
    ElNotification.success({ title: '删除成功', message: '需求已删除' })
    router.push('/rfqs')
  } catch { /* 取消 */ }
}

onMounted(() => { loadRFQ(); loadItems(); loadCloseRecords() })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-detail-page {
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
  margin-bottom: 24px;
  .header-left { display: flex; align-items: center; gap: 16px; }
  .header-right { display: flex; gap: 10px; }
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255,255,255,0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.07); color: $text-secondary; border-color: rgba(0,212,255,0.2); }
}

.rfq-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.rfq-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
  border: 1px solid rgba(0,212,255,0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0 0 6px 0;
  font-family: 'Space Mono', monospace;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.rfq-code {
  font-size: 13px;
  color: $text-secondary;
}

.source-tag {
  font-size: 11px;
  color: $text-muted;
  background: rgba(255,255,255,0.05);
  border: 1px solid $border-panel;
  border-radius: 4px;
  padding: 1px 6px;
}

.status-badge {
  font-size: 10px;
  padding: 2px 7px;
  border-radius: 3px;
  &.status-0 { background: rgba(107,122,141,0.2); color: #8A9BB0; border: 1px solid rgba(107,122,141,0.3); }
  &.status-1 { background: rgba(201,154,69,0.2); color: $color-amber; border: 1px solid rgba(201,154,69,0.3); }
  &.status-2 { background: rgba(50,149,201,0.2); color: $color-steel-cyan; border: 1px solid rgba(50,149,201,0.3); }
  &.status-3 { background: rgba(0,212,255,0.12); color: $cyan-primary; border: 1px solid rgba(0,212,255,0.25); }
  &.status-4 { background: rgba(70,191,145,0.15); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.3); }
  &.status-5 { background: rgba(70,191,145,0.2); color: $color-mint-green; border: 1px solid rgba(70,191,145,0.4); }
  &.status-6 { background: rgba(201,87,69,0.15); color: $color-red-brown; border: 1px solid rgba(201,87,69,0.3); }
  &.status-7 { background: rgba(107,122,141,0.15); color: #6B7A8D; border: 1px solid rgba(107,122,141,0.25); }
  &.status-8 { background: rgba(107,122,141,0.1); color: #6B7A8D; border: 1px solid rgba(107,122,141,0.2); }
}

.btn-primary {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7));
  border: 1px solid rgba(0,212,255,0.4); border-radius: $border-radius-md;
  color: #fff; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { transform: translateY(-1px); box-shadow: 0 4px 16px rgba(0,212,255,0.25); }
  &:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }
}
.btn-secondary {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: rgba(255,255,255,0.05); border: 1px solid $border-panel; border-radius: $border-radius-md;
  color: $text-secondary; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.08); border-color: rgba(0,212,255,0.25); }
}
.btn-warning {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: rgba(201,154,69,0.15); border: 1px solid rgba(201,154,69,0.4); border-radius: $border-radius-md;
  color: $color-amber; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { background: rgba(201,154,69,0.25); }
}
.btn-danger {
  display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px;
  background: rgba(201,87,69,0.15); border: 1px solid rgba(201,87,69,0.4); border-radius: $border-radius-md;
  color: $color-red-brown; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; cursor: pointer; transition: all 0.2s;
  &:hover { background: rgba(201,87,69,0.25); }
}

// ---- 信息区块 ----
.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}
.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
  background: rgba(0,0,0,0.1);
}
.section-dot {
  width: 8px; height: 8px; border-radius: 50%;
  &--cyan { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
}
.section-title { font-size: 14px; font-weight: 500; color: $text-primary; }

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}
.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
  .info-label { font-size: 11px; color: $text-muted; letter-spacing: 0.5px; text-transform: uppercase; }
  .info-value {
    font-size: 13px; color: $text-secondary;
    &--code { font-family: 'Space Mono', monospace; font-size: 12px; color: $color-ice-blue; }
    &--time { font-size: 12px; color: $text-muted; }
  }
}

// ---- 标签页 ----
.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}
.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  padding: 0 16px;
  background: rgba(0,0,0,0.1);
}
.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
  display: flex;
  align-items: center;
  gap: 6px;
  &:hover { color: $text-secondary; }
  &--active { color: $cyan-primary; border-bottom-color: $cyan-primary; }
}
.tab-count {
  display: inline-block;
  padding: 0 6px;
  background: rgba(0,212,255,0.1);
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 10px;
  font-size: 11px;
  color: $cyan-primary;
  font-family: 'Space Mono', monospace;
}
.tabs-body { padding: 20px; }
.tab-toolbar {
  display: flex;
  align-items: center;
  margin-bottom: 14px;
}
.btn-add-item {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 5px 12px;
  background: rgba(0,212,255,0.08);
  border: 1px solid rgba(0,212,255,0.25);
  border-radius: $border-radius-sm;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(0,212,255,0.14); }
}

// ---- 表格 ----
.quantum-table {
  width: 100%;
  background: transparent !important;
  :deep(.el-table__inner-wrapper) { background: transparent; }
  :deep(tr) { background: transparent !important; &:hover td { background: rgba(0,212,255,0.04) !important; } }
  :deep(.el-table__fixed-right) { background: $layer-2 !important; }
}
.cell-primary   { color: $text-primary; font-size: 13px; }
.cell-secondary { color: $text-secondary; font-size: 13px; }
.cell-muted     { color: $text-muted; font-size: 12px; }
.cell-code      { font-family: 'Space Mono', monospace; font-size: 12px; color: $color-ice-blue; }

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 48px 0;
  color: $text-muted;
  font-size: 13px;
}

// ---- 推荐采购员卡片 ----
.recommend-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: rgba(0,212,255,0.05);
  border: 1px solid rgba(0,212,255,0.15);
  border-radius: $border-radius-md;
  margin-bottom: 8px;
}
.recommend-avatar {
  width: 36px; height: 36px;
  background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
  border: 1px solid rgba(0,212,255,0.2);
  border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-size: 14px; font-weight: 600; color: $cyan-primary; flex-shrink: 0;
}
.recommend-name { font-size: 13px; color: $text-primary; font-weight: 500; }
.recommend-meta { font-size: 11px; color: $text-muted; margin-top: 2px; }
.btn-use-recommend {
  margin-left: auto;
  padding: 5px 10px;
  background: rgba(0,212,255,0.1);
  border: 1px solid rgba(0,212,255,0.25);
  border-radius: $border-radius-md;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;
  &:hover { background: rgba(0,212,255,0.18); }
}
</style>
