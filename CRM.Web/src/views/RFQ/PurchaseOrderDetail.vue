<template>
  <div class="purchase-order-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> 返回列表
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'PurchaseOrderList' }">采购订单</el-breadcrumb-item>
        <el-breadcrumb-item>{{ order?.purchaseOrderCode || '详情' }}</el-breadcrumb-item>
      </el-breadcrumb>
      <div class="header-actions" v-if="order">
        <el-button size="small" @click="handleUpdateStatus">更新状态</el-button>
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
          <el-tag effect="dark" :type="getStatusType(order.status)" size="small" style="margin-left: 12px;">
            {{ getStatusText(order.status) }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item label="订单号">
            <span class="order-code">{{ order.purchaseOrderCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag effect="dark" :type="getStatusType(order.status)">{{ getStatusText(order.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item v-if="canViewVendorInfo" label="供应商">{{ order.vendorName }}</el-descriptions-item>
          <el-descriptions-item label="采购员">{{ order.purchaseUserName }}</el-descriptions-item>
          <el-descriptions-item v-if="canViewPurchaseAmount" label="总金额">
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
          <el-descriptions-item label="内部备注" :span="2">{{ order.innerComment }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- TabBar：订单明细 | 文档 -->
      <div class="tab-card">
        <el-tabs v-model="activeTab" class="detail-tabs">
          <!-- 订单明细 -->
          <el-tab-pane label="订单明细" name="items">
            <el-table :data="order.items" size="small" v-if="order.items?.length" class="items-table">
              <el-table-column type="index" width="50" label="#" />
              <el-table-column prop="pn" label="物料型号" min-width="160" />
              <el-table-column prop="brand" label="品牌" width="120" />
              <el-table-column prop="qty" label="数量" align="right" width="100" />
              <el-table-column v-if="canViewPurchaseAmount" prop="price" label="单价" align="right" width="120">
                <template #default="{ row }">
                  {{ formatCurrency(row.price, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column v-if="canViewPurchaseAmount" label="金额" align="right" width="130">
                <template #default="{ row }">
                  {{ formatCurrency(row.qty * row.price, row.currency) }}
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
          </el-tab-pane>
        </el-tabs>
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
            <el-option label="草稿" :value="0" />
            <el-option label="审批中" :value="1" />
            <el-option label="已审批" :value="2" />
            <el-option label="已确认" :value="3" />
            <el-option label="已收货" :value="4" />
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
import { mockPurchaseOrderApi as purchaseOrderApi } from '@/api/mockPurchaseOrder'
import { tagApi, type TagDefinitionDto } from '@/api/tag'
import { useAuthStore } from '@/stores/auth'
import TagListDisplay from '@/components/Tag/TagListDisplay.vue'
import ApplyTagsDialog from '@/components/Tag/ApplyTagsDialog.vue'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'

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
const newStatus = ref(0)

const orderId = computed(() => route.params.id as string)

onMounted(() => {
  fetchOrder()
})

const fetchOrder = async () => {
  loading.value = true
  try {
    const res = await purchaseOrderApi.getList()
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
    currentTags.value = await tagApi.getEntityTags('PURCHASE_ORDER', order.value.id) || []
  } catch {
    currentTags.value = []
  }
}

const getStatusType = (status: number) => {
  const map: Record<number, string> = { 0: 'info', 1: 'warning', 2: 'success', 3: 'success', 4: 'primary', 6: 'success', [-1]: 'danger' }
  return map[status] ?? 'info'
}
const getStatusText = (status: number) => {
  const map: Record<number, string> = { 0: '草稿', 1: '审批中', 2: '已审批', 3: '已确认', 4: '已收货', 6: '已完成', [-1]: '已取消' }
  return map[status] ?? '未知'
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
.purchase-order-detail {
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
  :deep(.el-descriptions__label) {
    color: #5a7a9a;
    background: #0d1e35;
    width: 100px;
  }
  :deep(.el-descriptions__content) {
    background: #0a1828;
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
