<template>
  <div class="sales-order-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> 返回列表
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'SalesOrderList' }">销售订单</el-breadcrumb-item>
        <el-breadcrumb-item>{{ order?.sellOrderCode || '详情' }}</el-breadcrumb-item>
      </el-breadcrumb>
      <div class="header-actions" v-if="order">
        <el-button size="small" plain @click="handleUpdateStatus">更新状态</el-button>
        <el-button size="small" type="primary" @click="handleEdit">编辑</el-button>
      </div>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="order">
      <!-- 基本信息卡片 -->
      <div class="info-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>基本信息</span>
          <el-tag :type="getStatusType(order.status)" size="small" effect="dark" style="margin-left: 12px;">
            {{ getStatusText(order.status) }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item label="订单号">
            <span class="order-code">{{ order.sellOrderCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(order.status)" effect="dark">{{ getStatusText(order.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item v-if="canViewCustomerInfo" label="客户">{{ order.customerName }}</el-descriptions-item>
          <el-descriptions-item label="业务员">{{ order.salesUserName }}</el-descriptions-item>
          <el-descriptions-item v-if="canViewSalesAmount" label="总金额">
            <span class="amount">{{ formatCurrency(order.total, order.currency) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="行项目数">{{ order.itemRows }}</el-descriptions-item>
          <el-descriptions-item label="交货日期">{{ order.deliveryDate }}</el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ order.createTime }}</el-descriptions-item>
          <el-descriptions-item label="标签" :span="2">
            <div class="tags-row">
              <TagListDisplay :tags="currentTags" />
              <el-button size="small" type="primary" link @click="tagDialogVisible = true">
                + 添加标签
              </el-button>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="送货地址" :span="2">{{ order.deliveryAddress }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ order.comment }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- TabBar：订单明细 | 文档 -->
      <div class="tab-card">
        <el-tabs v-model="activeTab" class="detail-tabs">
          <!-- 订单明细 -->
          <el-tab-pane label="订单明细" name="items">
            <el-table :data="order.items" border size="small" v-if="order.items?.length" class="items-table">
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
            </el-table>
            <el-empty v-else description="暂无明细" :image-size="80" />
          </el-tab-pane>

          <!-- 文档 -->
          <el-tab-pane label="文档" name="documents">
            <div class="doc-tab-content">
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
          </el-tab-pane>
        </el-tabs>
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

    <!-- 更新状态弹窗 -->
    <el-dialog v-model="statusDialogVisible" title="更新状态" width="400px">
      <el-form label-width="100px">
        <el-form-item label="新状态">
          <el-select v-model="newStatus" style="width: 100%">
            <el-option label="草稿" :value="0" />
            <el-option label="审批中" :value="1" />
            <el-option label="已审批" :value="2" />
            <el-option label="已确认" :value="3" />
            <el-option label="已发货" :value="4" />
            <el-option label="已完成" :value="6" />
            <el-option label="已取消" :value="-1" />
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
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { mockSalesOrderApi as salesOrderApi } from '@/api/mockSalesOrder'
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

const orderId = computed(() => route.params.id as string)

onMounted(() => {
  fetchOrder()
})

const fetchOrder = async () => {
  loading.value = true
  try {
    const res = await salesOrderApi.getList()
    const list: any[] = res.data || []
    const id = orderId.value
    order.value = list.find((o: any) => String(o.id) === String(id)) || null
    if (order.value) {
      refreshTags()
    }
  } catch {
    order.value = null
  } finally {
    loading.value = false
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

const getStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'warning', 2: 'success', 3: 'success', 4: 'primary', 6: 'success', [-1]: 'danger' }
  return map[status] ?? 'info'
}
const getStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '草稿', 1: '审批中', 2: '已审批', 3: '已确认', 4: '已发货', 6: '已完成', [-1]: '已取消' }
  return map[status] ?? '未知'
}
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

const handleEdit = () => {
  ElMessage.info('编辑功能开发中')
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
.sales-order-detail {
  padding: 20px;
  min-height: 100%;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
  .back-btn {
    color: rgba(200, 220, 240, 0.7);
    &:hover { color: #00d4ff; }
  }
  .header-actions {
    margin-left: auto;
    display: flex;
    gap: 8px;
  }
}

.loading-wrap {
  padding: 20px;
  background: #0a1828;
  border-radius: 8px;
}

.info-card {
  background: #0a1828;
  border: 1px solid #1a2d45;
  border-radius: 8px;
  padding: 16px 20px;
  margin-bottom: 16px;
}

.card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: #e0f0ff;
  margin-bottom: 14px;
  .title-bar {
    width: 4px;
    height: 16px;
    background: #00c8ff;
    border-radius: 2px;
  }
}

.order-desc {
  :deep(.el-descriptions__body) {
    background: transparent;
  }
  :deep(.el-descriptions__label) {
    color: #5a7a9a;
    background: #0d1e35;
    width: 100px;
    border-color: #1a2d45 !important;
  }
  :deep(.el-descriptions__content) {
    background: #0a1828;
    border-color: #1a2d45 !important;
  }
  :deep(.el-descriptions__cell) {
    border-color: #1a2d45 !important;
  }
  :deep(table) {
    border-color: #1a2d45 !important;
  }
  :deep(td), :deep(th) {
    border-color: #1a2d45 !important;
  }
}

.order-code {
  font-family: 'Courier New', monospace;
  color: #7ecfff;
  font-weight: 600;
}

.amount {
  color: #00c8ff;
  font-weight: 600;
}

.tags-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
}

.tab-card {
  background: #0a1828;
  border: 1px solid #1a2d45;
  border-radius: 8px;
  padding: 0 20px 20px;
}

.detail-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 16px;
    border-bottom: 1px solid #1a2d45;
    background: transparent;
  }
  :deep(.el-tabs__nav-wrap::after) {
    display: none;
  }
  :deep(.el-tabs__active-bar) {
    display: none;
  }
  :deep(.el-tabs__item) {
    position: relative;
    height: 30px;
    line-height: 30px;
    padding: 0 14px;
    margin-right: 4px;
    border-radius: 6px 6px 0 0;
    border: 1px solid transparent;
    border-bottom: none;
    background: rgba(255, 255, 255, 0.03);
    color: rgba(180, 210, 230, 0.6);
    font-size: 12px;
    font-family: 'Noto Sans SC', sans-serif;
    transition: all 0.15s;
    &:hover {
      background: rgba(0, 212, 255, 0.06);
      border-color: rgba(0, 212, 255, 0.1);
      color: rgba(180, 210, 230, 0.9);
    }
    &.is-active {
      background: linear-gradient(180deg, rgba(0, 212, 255, 0.12) 0%, rgba(0, 212, 255, 0.05) 100%);
      border-color: rgba(0, 212, 255, 0.25);
      color: #00D4FF;
      font-weight: 600;
      text-shadow: 0 0 8px rgba(0, 212, 255, 0.4);
      box-shadow: 0 0 14px rgba(0, 212, 255, 0.15);
      transform: translateY(-1px);
    }
  }
  :deep(.el-tabs__content) {
    padding: 0;
  }
}

.items-table {
  :deep(.el-table) {
    --el-table-border-color: #1a2d45;
    --el-table-header-bg-color: #0d1e35;
    --el-table-row-hover-bg-color: #0f2035;
    --el-table-bg-color: #0a1828;
    --el-table-tr-bg-color: #0a1828;
    background: #0a1828;
    color: #c8dff0;
  }
  :deep(.el-table__header-wrapper th) {
    background: #0d1e35;
    color: #5a7a9a;
    border-color: #1a2d45 !important;
  }
  :deep(.el-table__row td) {
    background: #0a1828;
    border-color: #1a2d45 !important;
  }
  :deep(.el-table__border-left-patch),
  :deep(.el-table__inner-wrapper::before),
  :deep(.el-table__inner-wrapper::after) {
    background: #1a2d45;
  }
}

.doc-tab-content {
  padding-top: 4px;
}
</style>
