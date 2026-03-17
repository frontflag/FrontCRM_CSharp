<template>
  <div class="demand-detail-page" v-loading="pageLoading">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="page-header-left">
        <el-button :icon="ArrowLeft" text @click="router.back()">返回</el-button>
        <div class="header-title">
          <el-icon v-if="demand?.isImportant" class="important-star"><StarFilled /></el-icon>
          <h1 class="page-title">{{ demand?.demandCode || '需求详情' }}</h1>
          <el-tag :type="getStatusTagType(demand?.status)" size="small" class="status-tag">
            {{ getStatusLabel(demand?.status) }}
          </el-tag>
        </div>
      </div>
      <div class="page-header-right">
        <el-button :icon="Edit" @click="handleEdit">编辑</el-button>
        <el-button type="primary" @click="openAssignDialog">分配采购员</el-button>
        <el-dropdown @command="handleHeaderCommand">
          <el-button>更多操作<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="setImportant">
                {{ demand?.isImportant ? '取消重要标记' : '标记为重要' }}
              </el-dropdown-item>
              <el-dropdown-item command="changeStatus">变更状态</el-dropdown-item>
              <el-dropdown-item command="markRead">标记报价已读</el-dropdown-item>
              <el-dropdown-item command="close" divided>关闭需求</el-dropdown-item>
              <el-dropdown-item command="delete" class="danger-item">删除需求</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <!-- 基本信息卡片 -->
    <el-row :gutter="16" class="info-section">
      <el-col :span="16">
        <el-card class="info-card" shadow="never">
          <template #header><span class="card-title">基本信息</span></template>
          <el-descriptions :column="2" border size="small">
            <el-descriptions-item label="需求编号">{{ demand?.demandCode }}</el-descriptions-item>
            <el-descriptions-item label="客户名称">{{ demand?.customerName || '--' }}</el-descriptions-item>
            <el-descriptions-item label="需求日期">{{ formatDate(demand?.demandDate) }}</el-descriptions-item>
            <el-descriptions-item label="期望交期">{{ formatDate(demand?.expectedDeliveryDate) || '--' }}</el-descriptions-item>
            <el-descriptions-item label="业务员">{{ demand?.salesUserName || '--' }}</el-descriptions-item>
            <el-descriptions-item label="需求来源">{{ getSourceLabel(demand?.source) }}</el-descriptions-item>
            <el-descriptions-item label="货币">{{ demand?.currency || 'CNY' }}</el-descriptions-item>
            <el-descriptions-item label="付款条款">{{ demand?.paymentTerms || '--' }}</el-descriptions-item>
            <el-descriptions-item label="质量要求" :span="2">{{ demand?.qualityRequirements || '--' }}</el-descriptions-item>
            <el-descriptions-item label="认证要求" :span="2">{{ demand?.certificationRequirements || '--' }}</el-descriptions-item>
            <el-descriptions-item label="备注" :span="2">{{ demand?.remark || '--' }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card class="info-card" shadow="never">
          <template #header><span class="card-title">采购员分配</span></template>
          <div v-if="assignments.length === 0" class="empty-assign">
            <el-icon class="empty-icon"><User /></el-icon>
            <p>暂未分配采购员</p>
            <el-button size="small" type="primary" @click="openAssignDialog">立即分配</el-button>
          </div>
          <div v-else class="assign-list">
            <div v-for="a in assignments" :key="a.id" class="assign-item">
              <div class="assign-avatar">{{ a.purchaserName?.charAt(0) }}</div>
              <div class="assign-info">
                <div class="assign-name">{{ a.purchaserName }}</div>
                <div class="assign-meta">
                  <el-tag :type="getHandleStatusType(a.handleStatus)" size="small">
                    {{ getHandleStatusLabel(a.handleStatus) }}
                  </el-tag>
                  <span class="assign-date">{{ formatDate(a.assignedAt) }}</span>
                </div>
              </div>
              <el-button size="small" text type="primary" @click="openReassignDialog(a)">重新分配</el-button>
            </div>
          </div>
        </el-card>

        <el-card class="info-card" shadow="never" style="margin-top: 16px">
          <template #header><span class="card-title">关闭记录</span></template>
          <div v-if="closeRecords.length === 0" class="empty-assign">
            <p style="color: var(--el-text-color-secondary); font-size: 13px;">暂无关闭记录</p>
          </div>
          <div v-else>
            <div v-for="r in closeRecords" :key="r.id" class="close-record-item">
              <div class="close-record-type">
                <el-tag size="small" type="danger">{{ getCloseTypeLabel(r.closeType) }}</el-tag>
                <span class="close-record-date">{{ formatDate(r.closedAt) }}</span>
              </div>
              <div class="close-record-reason">{{ r.closeReason }}</div>
              <div v-if="r.remark" class="close-record-remark">{{ r.remark }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 需求明细 -->
    <el-card class="items-card" shadow="never">
      <template #header>
        <div class="items-header">
          <span class="card-title">需求明细</span>
          <div class="items-header-actions">
            <el-tag type="info" size="small">共 {{ demandItems.length }} 条</el-tag>
            <el-button size="small" type="primary" @click="loadItemsWithBestQuote">刷新最优报价</el-button>
          </div>
        </div>
      </template>
      <el-table :data="demandItems" stripe size="small" v-loading="itemsLoading">
        <el-table-column prop="lineNo" label="行号" width="60" align="center" />
        <el-table-column prop="materialCode" label="物料编码" min-width="120">
          <template #default="{ row }">{{ row.materialCode || '--' }}</template>
        </el-table-column>
        <el-table-column prop="materialName" label="物料名称" min-width="150">
          <template #default="{ row }">{{ row.materialName || '--' }}</template>
        </el-table-column>
        <el-table-column prop="materialModel" label="规格型号" min-width="120">
          <template #default="{ row }">{{ row.materialModel || '--' }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="90" align="right">
          <template #default="{ row }">{{ row.quantity }} {{ row.unit || '' }}</template>
        </el-table-column>
        <el-table-column prop="targetPrice" label="目标单价" width="100" align="right">
          <template #default="{ row }">
            {{ row.targetPrice != null ? `¥${row.targetPrice.toFixed(4)}` : '--' }}
          </template>
        </el-table-column>
        <el-table-column prop="bestQuotePrice" label="最优报价" width="100" align="right">
          <template #default="{ row }">
            <span :class="row.bestQuotePrice != null ? 'best-quote-price' : ''">
              {{ row.bestQuotePrice != null ? `¥${row.bestQuotePrice.toFixed(4)}` : '--' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="bestQuoteSupplier" label="报价供应商" min-width="120">
          <template #default="{ row }">{{ row.bestQuoteSupplier || '--' }}</template>
        </el-table-column>
        <el-table-column prop="deliveryDate" label="交货日期" width="100">
          <template #default="{ row }">{{ formatDate(row.deliveryDate) || '--' }}</template>
        </el-table-column>
        <el-table-column prop="brandRequirement" label="品牌要求" width="100">
          <template #default="{ row }">{{ row.brandRequirement || '--' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getItemStatusType(row.status)" size="small">
              {{ getItemStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="{ row }">
            <el-button size="small" type="danger" text @click="handleDeleteItem(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 分配采购员弹窗 -->
    <el-dialog v-model="assignDialogVisible" title="分配采购员" width="500px" :close-on-click-modal="false">
      <div v-loading="purchasersLoading">
        <div v-if="recommendedPurchasers.length > 0" class="recommended-section">
          <div class="recommended-title">系统推荐</div>
          <div class="recommended-list">
            <div
              v-for="p in recommendedPurchasers"
              :key="p.id"
              class="recommended-item"
              :class="{ selected: assignForm.purchaserId === p.id }"
              @click="assignForm.purchaserId = p.id"
            >
              <div class="rec-avatar">{{ p.name.charAt(0) }}</div>
              <div class="rec-info">
                <div class="rec-name">{{ p.name }}</div>
                <div class="rec-count">处理中: {{ p.handlingCount ?? 0 }}</div>
              </div>
              <el-icon v-if="assignForm.purchaserId === p.id" class="rec-check"><Check /></el-icon>
            </div>
          </div>
        </div>
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
          <el-select v-model="statusForm.status" style="width: 100%">
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
          <el-input v-model="statusForm.remark" type="textarea" :rows="2" />
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
          <el-select v-model="closeForm.closeType" style="width: 100%">
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
          <el-input v-model="closeForm.remark" type="textarea" :rows="2" />
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
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import {
  ArrowLeft, Edit, ArrowDown, StarFilled, User, Check
} from '@element-plus/icons-vue'
import { demandApi } from '@/api/demand'
import type { Demand, DemandItem, DemandAssignment, DemandCloseRecord } from '@/types/demand'

const router = useRouter()
const route = useRoute()
const demandId = route.params.id as string

// ─── 状态 ───
const pageLoading = ref(false)
const demand = ref<Demand | null>(null)
const demandItems = ref<DemandItem[]>([])
const itemsLoading = ref(false)
const assignments = ref<DemandAssignment[]>([])
const closeRecords = ref<DemandCloseRecord[]>([])

// 分配采购员
const assignDialogVisible = ref(false)
const purchasersLoading = ref(false)
const assignLoading = ref(false)
const purchaserList = ref<any[]>([])
const recommendedPurchasers = ref<any[]>([])
const assignForm = reactive({ purchaserId: '', remark: '' })

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

function getStatusLabel(status?: number): string {
  const map: Record<number, string> = {
    0: '草稿', 1: '待处理', 2: '已分配', 3: '处理中',
    4: '已报价', 5: '已接受', 6: '已拒绝', 7: '已关闭', 8: '已取消',
  }
  return status != null ? (map[status] ?? '未知') : '--'
}

function getStatusTagType(status?: number): '' | 'success' | 'warning' | 'danger' | 'info' {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    0: 'info', 1: 'warning', 2: '', 3: '', 4: 'success',
    5: 'success', 6: 'danger', 7: 'info', 8: 'info',
  }
  return status != null ? (map[status] ?? 'info') : 'info'
}

function getSourceLabel(source?: number): string {
  const map: Record<number, string> = { 1: '手动录入', 2: '导入', 3: '邮件', 4: '在线', 5: '电话' }
  return source ? (map[source] ?? '--') : '--'
}

function getItemStatusLabel(status: number): string {
  const map: Record<number, string> = { 0: '待报价', 1: '已报价', 2: '已接受', 3: '已拒绝', 4: '已关闭' }
  return map[status] ?? '未知'
}

function getItemStatusType(status: number): '' | 'success' | 'warning' | 'danger' | 'info' {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    0: 'warning', 1: '', 2: 'success', 3: 'danger', 4: 'info',
  }
  return map[status] ?? 'info'
}

function getHandleStatusLabel(status: number): string {
  const map: Record<number, string> = { 0: '未处理', 1: '处理中', 2: '已完成', 3: '已推迟' }
  return map[status] ?? '未知'
}

function getHandleStatusType(status: number): '' | 'success' | 'warning' | 'danger' | 'info' {
  const map: Record<number, '' | 'success' | 'warning' | 'danger' | 'info'> = {
    0: 'info', 1: 'warning', 2: 'success', 3: '',
  }
  return map[status] ?? 'info'
}

function getCloseTypeLabel(type: number): string {
  const map: Record<number, string> = { 1: '正常关闭', 2: '客户取消', 3: '价格不符', 9: '其他原因' }
  return map[type] ?? '未知'
}

// ─── 数据加载 ───
async function loadDetail() {
  pageLoading.value = true
  try {
    const [d, items, records] = await Promise.allSettled([
      demandApi.getDemandDetail(demandId),
      demandApi.getDemandItemsWithBestQuote(demandId),
      demandApi.getCloseRecords(demandId),
    ])
    if (d.status === 'fulfilled') demand.value = d.value
    if (items.status === 'fulfilled') demandItems.value = items.value
    if (records.status === 'fulfilled') closeRecords.value = records.value
  } catch (err: any) {
    ElNotification.error({ title: '加载失败', message: err?.message || '获取需求详情失败' })
  } finally {
    pageLoading.value = false
  }
}

async function loadItemsWithBestQuote() {
  itemsLoading.value = true
  try {
    demandItems.value = await demandApi.getDemandItemsWithBestQuote(demandId)
    ElNotification.success({ title: '已刷新', message: '最优报价数据已更新' })
  } catch (err: any) {
    ElNotification.error({ title: '刷新失败', message: err?.message || '刷新失败' })
  } finally {
    itemsLoading.value = false
  }
}

// ─── 事件处理 ───
function handleEdit() {
  router.push(`/demands/${demandId}/edit`)
}

async function handleHeaderCommand(cmd: string) {
  switch (cmd) {
    case 'setImportant':
      await toggleImportant()
      break
    case 'changeStatus':
      statusForm.status = demand.value?.status ?? 1
      statusForm.remark = ''
      statusDialogVisible.value = true
      break
    case 'markRead':
      await handleMarkRead()
      break
    case 'close':
      closeForm.closeType = 1
      closeForm.closeReason = ''
      closeForm.remark = ''
      closeDialogVisible.value = true
      break
    case 'delete':
      await handleDelete()
      break
  }
}

async function toggleImportant() {
  if (!demand.value) return
  try {
    await demandApi.setImportant(demandId, !demand.value.isImportant)
    demand.value.isImportant = !demand.value.isImportant
    ElNotification.success({
      title: '操作成功',
      message: demand.value.isImportant ? '已标记为重要需求' : '已取消重要标记',
    })
  } catch (err: any) {
    ElNotification.error({ title: '操作失败', message: err?.message || '操作失败' })
  }
}

async function handleMarkRead() {
  try {
    await demandApi.markQuoteRead(demandId)
    ElNotification.success({ title: '已标记', message: '需求报价已标记为已读' })
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
    const [purchasers, recommended] = await Promise.allSettled([
      demandApi.getPurchasers(),
      demandApi.getRecommendedPurchasers(demandId),
    ])
    purchaserList.value = purchasers.status === 'fulfilled' ? purchasers.value : []
    recommendedPurchasers.value = recommended.status === 'fulfilled' ? recommended.value.slice(0, 3) : []
  } finally {
    purchasersLoading.value = false
  }
}

function openReassignDialog(a: DemandAssignment) {
  assignForm.purchaserId = a.purchaserId
  assignForm.remark = ''
  openAssignDialog()
}

async function handleAssignConfirm() {
  if (!assignForm.purchaserId) {
    ElNotification.warning({ title: '请选择采购员', message: '采购员不能为空' })
    return
  }
  assignLoading.value = true
  try {
    await demandApi.assignPurchaser(demandId, {
      purchaserId: assignForm.purchaserId,
      remark: assignForm.remark,
    })
    ElNotification.success({ title: '分配成功', message: '采购员已成功分配' })
    assignDialogVisible.value = false
    loadDetail()
  } catch (err: any) {
    ElNotification.error({ title: '分配失败', message: err?.message || '分配失败' })
  } finally {
    assignLoading.value = false
  }
}

async function handleStatusConfirm() {
  statusLoading.value = true
  try {
    await demandApi.updateFlowStatus(demandId, {
      status: statusForm.status,
      remark: statusForm.remark,
    })
    if (demand.value) demand.value.status = statusForm.status
    ElNotification.success({ title: '状态已更新', message: `需求状态已变更为「${getStatusLabel(statusForm.status)}」` })
    statusDialogVisible.value = false
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
    await demandApi.addCloseRecord(demandId, closeForm)
    await demandApi.updateFlowStatus(demandId, { status: 7 })
    if (demand.value) demand.value.status = 7
    ElNotification.success({ title: '需求已关闭', message: '需求已成功关闭' })
    closeDialogVisible.value = false
    loadDetail()
  } catch (err: any) {
    ElNotification.error({ title: '关闭失败', message: err?.message || '关闭失败' })
  } finally {
    closeLoading.value = false
  }
}

async function handleDeleteItem(item: DemandItem) {
  try {
    await ElMessageBox.confirm(
      `确定要删除第 ${item.lineNo} 行「${item.materialName || item.materialCode || '未命名'}」吗？`,
      '删除明细',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning', confirmButtonClass: 'el-button--danger' }
    )
    await demandApi.deleteDemandItem(demandId, item.id)
    ElNotification.success({ title: '删除成功', message: '需求明细已删除' })
    demandItems.value = demandItems.value.filter(i => i.id !== item.id)
  } catch (err: any) {
    if (err !== 'cancel') {
      ElNotification.error({ title: '删除失败', message: err?.message || '删除失败' })
    }
  }
}

async function handleDelete() {
  try {
    await ElMessageBox.confirm(
      `确定要删除需求「${demand.value?.demandCode}」吗？此操作不可恢复。`,
      '删除确认',
      { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning', confirmButtonClass: 'el-button--danger' }
    )
    await demandApi.deleteDemand(demandId)
    ElNotification.success({ title: '删除成功', message: '需求已删除' })
    router.push('/demands')
  } catch (err: any) {
    if (err !== 'cancel') {
      ElNotification.error({ title: '删除失败', message: err?.message || '删除失败' })
    }
  }
}

onMounted(() => {
  loadDetail()
})
</script>

<style scoped lang="scss">
.demand-detail-page {
  padding: 20px;
  min-height: 100%;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
  gap: 12px;
}

.page-header-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-title {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-title {
  font-size: 18px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  margin: 0;
}

.important-star {
  color: #f59e0b;
  font-size: 18px;
}

.status-tag { margin-left: 4px; }

.page-header-right {
  display: flex;
  gap: 8px;
}

.info-section { margin-bottom: 16px; }

.info-card {
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.empty-assign {
  text-align: center;
  padding: 20px 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.empty-icon {
  font-size: 32px;
  color: var(--el-text-color-placeholder);
  margin-bottom: 8px;
}

.assign-list { display: flex; flex-direction: column; gap: 10px; }

.assign-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px;
  border-radius: 6px;
  background: var(--el-fill-color-light);
}

.assign-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: var(--el-color-primary);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;
}

.assign-info { flex: 1; min-width: 0; }
.assign-name { font-size: 13px; font-weight: 500; color: var(--el-text-color-primary); }
.assign-meta { display: flex; align-items: center; gap: 8px; margin-top: 2px; }
.assign-date { font-size: 11px; color: var(--el-text-color-secondary); }

.close-record-item {
  padding: 8px;
  border-radius: 6px;
  background: var(--el-fill-color-light);
  margin-bottom: 8px;
}

.close-record-type {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.close-record-date { font-size: 11px; color: var(--el-text-color-secondary); }
.close-record-reason { font-size: 13px; color: var(--el-text-color-primary); }
.close-record-remark { font-size: 12px; color: var(--el-text-color-secondary); margin-top: 2px; }

.items-card {
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
}

.items-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.items-header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.best-quote-price {
  color: #10b981;
  font-weight: 600;
}

.recommended-section {
  margin-bottom: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.recommended-title {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-bottom: 8px;
}

.recommended-list {
  display: flex;
  gap: 8px;
}

.recommended-item {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px;
  border-radius: 6px;
  border: 1px solid var(--el-border-color);
  cursor: pointer;
  transition: all 0.2s;
  position: relative;

  &:hover { border-color: var(--el-color-primary); }
  &.selected { border-color: var(--el-color-primary); background: var(--el-color-primary-light-9); }
}

.rec-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--el-color-primary);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 600;
  flex-shrink: 0;
}

.rec-name { font-size: 13px; font-weight: 500; }
.rec-count { font-size: 11px; color: var(--el-text-color-secondary); }
.rec-check { color: var(--el-color-primary); position: absolute; top: 4px; right: 4px; }

:deep(.danger-item) { color: var(--el-color-danger) !important; }
</style>
