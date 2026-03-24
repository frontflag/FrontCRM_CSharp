<template>
  <div class="sales-order-detail">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="router.back()">
          <el-icon><ArrowLeft /></el-icon>
          返回列表
        </button>
        <div class="title-group" v-if="order">
          <div class="title-avatar">销</div>
          <div>
            <h1 class="page-title">销售订单详情</h1>
            <div class="title-meta">
              <span class="order-code">{{ order.sellOrderCode }}</span>
              <el-tag :type="getStatusType(order.status)" size="small" effect="dark">
                {{ getStatusText(order.status) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right" v-if="order">
        <button class="btn-warning" @click="handleOpenApplyStockOut">申请出库</button>
        <button class="btn-secondary" @click="handleUpdateStatus">更新状态</button>
        <button v-if="canWriteSo" class="btn-primary" @click="handleEdit">编辑</button>
      </div>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="order">
      <!-- 基本信息卡片 -->
      <div class="info-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基本信息</span>
        </div>
        <div class="info-grid">
          <div class="info-item">
            <span class="info-label">订单号</span>
            <span class="info-value info-value--code">{{ order.sellOrderCode }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">状态</span>
            <span class="info-value">{{ getStatusText(order.status) }}</span>
          </div>
          <div class="info-item" v-if="canViewCustomerInfo">
            <span class="info-label">客户</span>
            <span class="info-value">{{ order.customerName || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">业务员</span>
            <span class="info-value">{{ order.salesUserName || '--' }}</span>
          </div>
          <div class="info-item" v-if="canViewSalesAmount">
            <span class="info-label">总金额</span>
            <span class="info-value info-value--amount">{{ formatCurrency(order.total, order.currency) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">行项目数</span>
            <span class="info-value">{{ order.itemRows ?? 0 }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">交货日期</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.deliveryDate) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">创建时间</span>
            <span class="info-value info-value--time">{{ formatDateTime(order.createTime) }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">标签</span>
            <div class="tags-row">
              <TagListDisplay :tags="currentTags" />
              <button class="btn-add-tag" @click="tagDialogVisible = true">添加标签</button>
            </div>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">送货地址</span>
            <span class="info-value">{{ order.deliveryAddress || '--' }}</span>
          </div>
          <div class="info-item info-item--span-3">
            <span class="info-label">备注</span>
            <span class="info-value">{{ order.comment || '--' }}</span>
          </div>
          <div v-if="order.auditRemark || order.status === -1" class="info-item info-item--span-3">
            <span class="info-label">审核拒绝原因</span>
            <span class="info-value info-value--warn">{{ order.auditRemark || '--' }}</span>
          </div>
        </div>
      </div>

      <!-- TabBar：订单明细 | 文档 -->
      <div class="tabs-section">
        <div class="tabs-nav">
          <button class="tab-btn" :class="{ 'tab-btn--active': activeTab === 'items' }" @click="activeTab = 'items'">订单明细</button>
          <button class="tab-btn" :class="{ 'tab-btn--active': activeTab === 'documents' }" @click="activeTab = 'documents'">文档</button>
        </div>
        <div class="tabs-body">
          <div v-show="activeTab === 'items'">
            <CrmDataTable :data="order.items" size="small" v-if="order.items?.length" class="items-table">
              <el-table-column type="index" width="50" label="#" />
              <el-table-column prop="pn" label="物料型号" min-width="160" />
              <el-table-column prop="brand" label="品牌" width="120" />
              <el-table-column prop="qty" label="数量" align="right" width="100" />
              <el-table-column v-if="canViewSalesAmount" prop="price" label="单价" align="right" width="120">
                <template #default="{ row }">
                  {{ formatCurrency(row.price, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column v-if="canViewSalesAmount" label="金额" align="right" width="130">
                <template #default="{ row }">
                  {{ formatCurrency(row.qty * row.price, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column label="审核状态" width="90" align="center">
                <template #default="{ row }">
                  <el-tag :type="getItemAuditStatusType(row.itemAuditStatus)" size="small" effect="dark">
                    {{ getItemAuditStatusText(row.itemAuditStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="货运状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getShippingStatusType(row.shippingStatus)" size="small" effect="dark">
                    {{ getShippingStatusText(row.shippingStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="款项状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getPaymentStatusType(row.paymentStatus)" size="small" effect="dark">
                    {{ getPaymentStatusText(row.paymentStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="票据状态" width="100" align="center">
                <template #default="{ row }">
                  <el-tag :type="getInvoiceStatusType(row.invoiceStatus)" size="small" effect="dark">
                    {{ getInvoiceStatusText(row.invoiceStatus) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="comment" label="备注" min-width="120" />
            </CrmDataTable>
            <el-empty v-else description="暂无明细" :image-size="80" />
          </div>
          <div v-show="activeTab === 'documents'" class="doc-tab-content">
            <DocumentUploadPanel
              biz-type="SALES_ORDER"
              :biz-id="String(order.id)"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="docListRef?.refresh()"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="SALES_ORDER"
              :biz-id="String(order.id)"
              view-mode="list"
              style="margin-top: 16px;"
            />
          </div>
        </div>
      </div>
    </template>

    <el-empty v-else description="订单不存在" />

    <!-- 标签弹窗 -->
    <ApplyTagsDialog
      v-model="tagDialogVisible"
      entity-type="SALES_ORDER"
      :entity-ids="order ? [order.id] : []"
      title="为销售订单添加标签"
      @success="refreshTags"
    />

    <el-dialog v-model="applyDialogVisible" title="新建出货通知" width="900px" destroy-on-close>
      <el-form :model="applyForm" label-width="96px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="通知单号" required>
              <el-input v-model="applyForm.requestCode" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="预计出货日期" required>
              <el-date-picker v-model="applyForm.requestDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="客户">
              <el-input :model-value="order?.customerName || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="销售单号">
              <el-input :model-value="order?.sellOrderCode || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="applyForm.remark" type="textarea" :rows="2" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <CrmDataTable :data="applyForm.items" class="items-table">
        <el-table-column type="index" width="50" />
        <el-table-column prop="materialCode" label="物料型号" min-width="150" />
        <el-table-column prop="materialName" label="品牌" width="140" />
        <el-table-column label="订单数量" width="120" align="right">
          <template #default="{ row }">{{ row.maxQty }}</template>
        </el-table-column>
        <el-table-column label="出库通知数量" width="160">
          <template #default="{ row }">
            <el-input-number v-model="row.quantity" :min="0" :max="row.maxQty" :precision="0" />
          </template>
        </el-table-column>
      </CrmDataTable>
      <template #footer>
        <el-button @click="applyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="applySubmitting" @click="submitApplyStockOut">确定</el-button>
      </template>
    </el-dialog>

    <!-- 更新状态弹窗 -->
    <el-dialog v-model="editDialogVisible" title="编辑销售订单" width="560px" destroy-on-close @closed="onEditDialogClosed">
      <el-form v-if="order" label-width="100px">
        <el-form-item v-if="canViewCustomerInfo" label="客户名称">
          <el-input v-model="editForm.customerName" />
        </el-form-item>
        <el-form-item label="业务员">
          <el-input v-model="editForm.salesUserName" />
        </el-form-item>
        <el-form-item label="订单类型">
          <el-select v-model="editForm.type" style="width: 100%">
            <el-option label="普通订单" :value="1" />
            <el-option label="紧急订单" :value="2" />
            <el-option label="样品订单" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="币别">
          <el-select v-model="editForm.currency" style="width: 100%">
            <el-option label="CNY" :value="1" />
            <el-option label="USD" :value="2" />
            <el-option label="EUR" :value="3" />
          </el-select>
        </el-form-item>
        <el-form-item label="交货日期">
          <el-date-picker v-model="editForm.deliveryDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item label="送货地址">
          <el-input v-model="editForm.deliveryAddress" type="textarea" :rows="2" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="editForm.comment" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="editSaving" @click="saveEdit">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="statusDialogVisible" title="更新状态" width="400px">
      <el-form label-width="100px">
        <el-form-item label="新状态">
          <el-select v-model="newStatus" style="width: 100%">
            <el-option v-for="opt in statusDialogOptions" :key="String(opt.value)" :label="opt.label" :value="opt.value" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="statusDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="statusLoading" @click="confirmUpdateStatus">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import salesOrderApi from '@/api/salesOrder'
import { salesOrderStatusText, salesOrderStatusTagType } from '@/constants/salesOrderStatus'
import { stockOutApi } from '@/api/stockOut'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const canViewCustomerInfo = computed(() => authStore.hasPermission('customer.info.read'))
const canViewSalesAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const canWriteSo = computed(() => authStore.hasPermission('sales-order.write'))

const loading = ref(false)
const order = ref<any>(null)
const activeTab = ref('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

// 标签
const currentTags = ref<TagDefinitionDto[]>([])
const tagDialogVisible = ref(false)

// 状态
const statusDialogVisible = ref(false)
const statusLoading = ref(false)
const newStatus = ref(0)

const editDialogVisible = ref(false)
const editSaving = ref(false)
const applyDialogVisible = ref(false)
const applySubmitting = ref(false)
const applyForm = ref({
  requestCode: '',
  requestDate: '',
  remark: '',
  items: [] as Array<{ lineNo: number; materialCode: string; materialName: string; quantity: number; maxQty: number }>
})
const editForm = ref({
  customerName: '',
  salesUserName: '',
  type: 1,
  currency: 1,
  deliveryDate: '' as string | undefined,
  deliveryAddress: '',
  comment: ''
})

const orderId = computed(() => route.params.id as string)

const mainStatusOptions = [
  { label: '新建', value: 1 },
  { label: '待审核', value: 2 },
  { label: '审核通过', value: 10 },
  { label: '进行中', value: 20 },
  { label: '完成', value: 100 },
  { label: '审核失败', value: -1 },
  { label: '取消', value: -2 }
] as const

/** 手工更新状态不允许直接改为审核通过/审核失败 */
const statusDialogOptions = computed(() =>
  mainStatusOptions.filter(o => o.value !== 10 && o.value !== -1)
)

onMounted(() => {
  fetchOrder()
})

watch(
  () => [route.query.edit, order.value?.id] as const,
  () => {
    if (route.query.edit === '1' && order.value && canWriteSo.value) {
      openEditDialog()
    }
  }
)

watch(
  () => [route.query.applyStockOut, order.value?.id] as const,
  () => {
    if (route.query.applyStockOut === '1' && order.value && canWriteSo.value) {
      handleOpenApplyStockOut()
      router.replace({ path: route.path, query: { ...route.query, applyStockOut: undefined } })
    }
  }
)

const fetchOrder = async () => {
  loading.value = true
  try {
    const id = orderId.value
    order.value = await salesOrderApi.getById(id)
    if (order.value) {
      refreshTags()
    }
  } catch {
    order.value = null
  } finally {
    loading.value = false
  }
}

function openEditDialog() {
  if (!order.value) return
  const o = order.value
  editForm.value = {
    customerName: o.customerName || '',
    salesUserName: o.salesUserName || '',
    type: o.type ?? 1,
    currency: o.currency ?? 1,
    deliveryDate: o.deliveryDate ? String(o.deliveryDate).slice(0, 10) : undefined,
    deliveryAddress: o.deliveryAddress || '',
    comment: o.comment || ''
  }
  editDialogVisible.value = true
}

function onEditDialogClosed() {
  if (route.query.edit === '1') {
    router.replace({ path: route.path, query: {} })
  }
}

const saveEdit = async () => {
  if (!order.value) return
  editSaving.value = true
  try {
    await salesOrderApi.update(order.value.id, {
      customerName: editForm.value.customerName || undefined,
      salesUserName: editForm.value.salesUserName || undefined,
      type: editForm.value.type,
      currency: editForm.value.currency,
      deliveryDate: editForm.value.deliveryDate || undefined,
      deliveryAddress: editForm.value.deliveryAddress || undefined,
      comment: editForm.value.comment || undefined
    })
    ElMessage.success('已保存')
    editDialogVisible.value = false
    await fetchOrder()
  } catch (e: any) {
    ElMessage.error(e?.message || '保存失败')
  } finally {
    editSaving.value = false
  }
}

const refreshTags = async () => {
  if (!order.value) return
  try {
    currentTags.value = await tagApi.getEntityTags('SALES_ORDER', order.value.id) || []
  } catch {
    currentTags.value = []
  }
}

const getStatusType = (status: number) => salesOrderStatusTagType(status)
const getStatusText = (status: number) => salesOrderStatusText(status)
// ===== 明细状态辅助函数 =====
// 审核状态
const getItemAuditStatusText = (v?: number) => {
  const map: Record<number, string> = { 0: '新建', 1: '待审核', 2: '已审核' }
  return v !== undefined ? (map[v] ?? '-') : '-'
}
const getItemAuditStatusType = (v?: number): '' | 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = { 0: 'info', 1: 'warning', 2: 'success' }
  return v !== undefined ? (map[v] ?? 'info') : 'info'
}
// 货运状态
const getShippingStatusText = (v?: number) => {
  const map: Record<number, string> = { 0: '待发货', 1: '在途', 2: '部分送达', 3: '货运完成' }
  return v !== undefined ? (map[v] ?? '-') : '-'
}
const getShippingStatusType = (v?: number): '' | 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = { 0: 'info', 1: 'warning', 2: '', 3: 'success' }
  return v !== undefined ? (map[v] ?? 'info') : 'info'
}
// 款项状态
const getPaymentStatusText = (v?: number) => {
  const map: Record<number, string> = { 0: '部分付款', 1: '付款完成' }
  return v !== undefined ? (map[v] ?? '-') : '-'
}
const getPaymentStatusType = (v?: number): '' | 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = { 0: 'warning', 1: 'success' }
  return v !== undefined ? (map[v] ?? 'info') : 'info'
}
// 票据状态
const getInvoiceStatusText = (v?: number) => {
  const map: Record<number, string> = { 0: '待开票', 1: '部分开票', 2: '开票完成' }
  return v !== undefined ? (map[v] ?? '-') : '-'
}
const getInvoiceStatusType = (v?: number): '' | 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = { 0: 'info', 1: 'warning', 2: 'success' }
  return v !== undefined ? (map[v] ?? 'info') : 'info'
}

const formatCurrency = (amount: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return `${symbol}${(amount || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}
const formatDateTime = (v?: string) => (v ? new Date(v).toLocaleString('zh-CN') : '--')
const makeRequestCode = () => `SON${new Date().toISOString().replace(/[-:TZ.]/g, '').slice(0, 14)}`

const handleEdit = () => {
  if (!canWriteSo.value) {
    ElMessage.warning('无编辑权限')
    return
  }
  openEditDialog()
}

const handleOpenApplyStockOut = () => {
  if (!order.value) return
  applyForm.value = {
    requestCode: makeRequestCode(),
    requestDate: new Date().toISOString().slice(0, 10),
    remark: '',
    items: (order.value.items || []).map((it: any, idx: number) => ({
      lineNo: idx + 1,
      materialCode: it.pn || '',
      materialName: it.brand || '',
      quantity: Number(it.qty || 0),
      maxQty: Number(it.qty || 0)
    }))
  }
  applyDialogVisible.value = true
}

const submitApplyStockOut = async () => {
  if (!order.value) return
  const items = applyForm.value.items.filter(x => Number(x.quantity) > 0)
  if (!items.length) {
    ElMessage.warning('请至少填写一条出库通知数量')
    return
  }
  applySubmitting.value = true
  try {
    await stockOutApi.createRequest({
      requestCode: applyForm.value.requestCode.trim(),
      salesOrderId: order.value.id,
      customerId: order.value.customerId || '',
      requestUserId: (authStore.user as any)?.id || '',
      requestDate: `${applyForm.value.requestDate}T00:00:00Z`,
      remark: applyForm.value.remark || undefined,
      items: items.map(x => ({
        lineNo: x.lineNo,
        materialCode: x.materialCode,
        materialName: x.materialName,
        quantity: x.quantity
      }))
    })
    applyDialogVisible.value = false
    ElMessage.success('申请出库成功')
    router.push('/stock-out-notifies')
  } catch (e: any) {
    ElMessage.error(e?.message || '申请出库失败')
  } finally {
    applySubmitting.value = false
  }
}

const handleUpdateStatus = () => {
  if (!order.value) return
  newStatus.value = order.value.status
  statusDialogVisible.value = true
}

const confirmUpdateStatus = async () => {
  if (!order.value) return
  statusLoading.value = true
  try {
    await salesOrderApi.updateStatus(order.value.id, newStatus.value)
    order.value.status = newStatus.value
    statusDialogVisible.value = false
    ElMessage.success('状态已更新')
  } catch {
    ElMessage.error('更新失败')
  } finally {
    statusLoading.value = false
  }
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.sales-order-detail {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-right {
  display: flex;
  gap: 10px;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
  &:hover { background: rgba(255,255,255,0.07); color: $text-secondary; border-color: rgba(0,212,255,0.2); }
}

.title-group {
  display: flex;
  align-items: center;
  gap: 14px;
}

.title-avatar {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  border: 1px solid rgba(0, 212, 255, 0.25);
  background: linear-gradient(135deg, rgba(0,102,255,0.3), rgba(0,212,255,0.2));
}

.page-title {
  margin: 0 0 6px;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-primary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid rgba(0,212,255,0.4);
  color: #fff;
  font-size: 13px;
  background: linear-gradient(135deg, rgba(0,102,255,0.8), rgba(0,212,255,0.7));
  cursor: pointer;
}

.btn-warning {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid rgba(201,154,69,0.4);
  color: $color-amber;
  font-size: 13px;
  background: rgba(201,154,69,0.15);
  cursor: pointer;
}

.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  background: rgba(255,255,255,0.05);
  cursor: pointer;
}

.loading-wrap {
  padding: 20px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
}

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
  width: 8px;
  height: 8px;
  border-radius: 50%;
  &--cyan { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.order-code {
  font-family: 'Space Mono', monospace;
  font-size: 11px;
  color: $text-muted;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 16px 20px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  border-right: 1px solid rgba(255,255,255,0.04);
  &:nth-child(3n) { border-right: none; }
}

.info-item--span-3 {
  grid-column: 1 / span 3;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
}

.info-value--code {
  font-family: 'Space Mono', monospace;
  color: $color-ice-blue;
}

.info-value--amount {
  font-family: 'Space Mono', monospace;
  color: $text-primary;
  font-weight: 500;
}

.info-value--time {
  font-size: 12px;
  color: $text-muted;
}

.info-value--warn {
  color: #f89898;
  white-space: pre-wrap;
}

.tags-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}

.btn-add-tag {
  padding: 3px 8px;
  border-radius: 999px;
  border: 1px dashed rgba(0, 212, 255, 0.35);
  background: transparent;
  color: rgba(200, 216, 232, 0.85);
  font-size: 11px;
  cursor: pointer;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 0 20px 20px;
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
  cursor: pointer;
  margin-bottom: -1px;
}

.tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.items-table {
  // 无外边框，行间细线分隔，对标客户管理列表风格
  --el-table-border-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: rgba(0, 212, 255, 0.04) !important;
      border-bottom: 1px solid rgba(0, 212, 255, 0.1) !important;
      border-right: none !important;
      color: rgba(200, 216, 232, 0.55);
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
  }
  :deep(.el-table__row) {
    background: transparent !important;
    td.el-table__cell {
      background: transparent !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04) !important;
      border-right: none !important;
      color: rgba(224, 244, 255, 0.85);
      font-size: 13px;
    }
    &:last-child td.el-table__cell { border-bottom: none !important; }
    &:hover td.el-table__cell { background: rgba(0, 212, 255, 0.04) !important; }
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
}

.doc-tab-content {
  padding-top: 4px;
}
</style>
