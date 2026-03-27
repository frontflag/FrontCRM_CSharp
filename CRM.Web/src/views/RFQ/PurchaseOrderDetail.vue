<template>
  <div class="purchase-order-detail">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="router.back()">
          <el-icon><ArrowLeft /></el-icon>
          返回列表
        </button>
        <div class="title-group" v-if="order">
          <div class="title-avatar">采</div>
          <div>
            <h1 class="page-title">采购订单详情</h1>
            <div class="title-meta">
              <span class="order-code">{{ order.purchaseOrderCode }}</span>
              <el-tag effect="dark" :type="getStatusType(order.status)" size="small">
                {{ getStatusText(order.status) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right" v-if="order">
        <button class="btn-secondary" @click="handleUpdateStatus">更新状态</button>
        <button class="btn-primary" @click="handleEdit">编辑</button>
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
            <span class="info-value info-value--code">{{ order.purchaseOrderCode }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">状态</span>
            <span class="info-value">{{ getStatusText(order.status) }}</span>
          </div>
          <div class="info-item" v-if="canViewVendorInfo">
            <span class="info-label">供应商</span>
            <span class="info-value">{{ order.vendorName || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">采购员</span>
            <span class="info-value">{{ order.purchaseUserName || '--' }}</span>
          </div>
          <div class="info-item" v-if="canViewPurchaseAmount">
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
          <div class="info-item info-item--span-3">
            <span class="info-label">内部备注</span>
            <span class="info-value">{{ order.innerComment || '--' }}</span>
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
              <el-table-column v-if="canViewPurchaseAmount" prop="cost" label="单价" align="right" width="120">
                <template #default="{ row }">
                  {{ formatCurrency(row.cost, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column v-if="canViewPurchaseAmount" label="金额" align="right" width="130">
                <template #default="{ row }">
                  {{ formatCurrency(row.qty * row.cost, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column prop="comment" label="备注" min-width="120" />
              <el-table-column prop="innerComment" label="内部备注" min-width="160" />
            </CrmDataTable>
            <el-empty v-else description="暂无明细" :image-size="80" />
          </div>
          <div v-show="activeTab === 'documents'" class="doc-tab-content">
            <DocumentUploadPanel
              biz-type="PURCHASE_ORDER"
              :biz-id="String(order.id)"
              :max-files="20"
              :max-size-mb="100"
              @uploaded="docListRef?.refresh()"
            />
            <DocumentListPanel
              ref="docListRef"
              biz-type="PURCHASE_ORDER"
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
      entity-type="PURCHASE_ORDER"
      :entity-ids="order ? [order.id] : []"
      title="为采购订单添加标签"
      @success="refreshTags"
    />

    <!-- 更新状态弹窗 -->
    <el-dialog v-model="statusDialogVisible" title="更新状态" width="400px">
      <el-form label-width="100px">
        <el-form-item label="新状态">
          <el-select v-model="newStatus" style="width: 100%">
            <el-option label="新建" :value="1" />
            <el-option label="待审核" :value="2" />
            <el-option label="审核通过" :value="10" />
            <el-option label="待确认" :value="20" />
            <el-option label="已确认" :value="30" />
            <el-option label="进行中" :value="50" />
            <el-option label="采购完成" :value="100" />
            <el-option label="审核失败" :value="-1" />
            <el-option label="取消" :value="-2" />
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
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const canViewVendorInfo = computed(() => authStore.hasPermission('vendor.info.read'))
const canViewPurchaseAmount = computed(() => authStore.hasPermission('purchase.amount.read'))

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
const newStatus = ref(1)

const orderId = computed(() => route.params.id as string)

onMounted(() => {
  fetchOrder()
})

const fetchOrder = async () => {
  loading.value = true
  try {
    const data = await purchaseOrderApi.getById(orderId.value)
    order.value = data ?? null
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
    currentTags.value = await tagApi.getEntityTags('PURCHASE_ORDER', order.value.id) || []
  } catch {
    currentTags.value = []
  }
}

const getStatusType = (status: number) => {
  const map: Record<number, string> = { 1: 'info', 2: 'warning', 10: 'success', 20: 'warning', 30: 'primary', 50: 'primary', 100: 'success', [-1]: 'danger', [-2]: 'info' }
  return map[status] ?? 'info'
}
const getStatusText = (status: number) => {
  const map: Record<number, string> = { 1: '新建', 2: '待审核', 10: '审核通过', 20: '待确认', 30: '已确认', 50: '进行中', 100: '采购完成', [-1]: '审核失败', [-2]: '取消' }
  return map[status] ?? '未知'
}
const formatCurrency = (amount: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return `${symbol}${(amount || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}
const formatDateTime = (v?: string) => (v ? formatDisplayDateTime(v) : '--')

const handleEdit = () => {
  if (!order.value?.id) return
  router.push({ name: 'PurchaseOrderEdit', params: { id: order.value.id } })
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
    await purchaseOrderApi.updateStatus(order.value.id, newStatus.value)
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

.purchase-order-detail {
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
