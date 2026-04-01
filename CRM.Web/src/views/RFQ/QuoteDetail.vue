<template>
  <div class="quote-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> 返回列表
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'QuoteList' }">报价单</el-breadcrumb-item>
        <el-breadcrumb-item>{{ quote?.quoteCode || '详情' }}</el-breadcrumb-item>
      </el-breadcrumb>
      <div class="header-actions" v-if="quote">
        <el-button size="small" @click="handleUpdateStatus">更新状态</el-button>
      </div>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="quote">
      <!-- 基本信息卡片 -->
      <div class="info-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>基本信息</span>
          <el-tag effect="dark" :type="getStatusType(quote.status)" size="small" style="margin-left: 12px;">
            {{ getStatusText(quote.status) }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item label="报价编号">
            <span class="order-code">{{ quote.quoteCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag effect="dark" :type="getStatusType(quote.status)">{{ getStatusText(quote.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="物料型号">{{ quote.mpn }}</el-descriptions-item>
          <el-descriptions-item label="报价日期">{{ quote.quoteDate }}</el-descriptions-item>
          <el-descriptions-item label="业务员">{{ quote.salesUserName }}</el-descriptions-item>
          <el-descriptions-item label="采购员">{{ quote.purchaseUserName }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ quote.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- TabBar：供应商报价明细 | 文档 -->
      <div class="tab-card">
        <el-tabs v-model="activeTab" class="detail-tabs">
          <!-- 供应商报价明细 -->
          <el-tab-pane label="供应商报价明细" name="items">
            <CrmDataTable :data="quote.items" size="small" v-if="quote.items?.length" class="items-table">
              <el-table-column type="index" width="50" label="#" />
              <el-table-column prop="vendorName" label="供应商" min-width="140" />
              <el-table-column prop="contactName" label="联系人" width="100" />
              <el-table-column prop="brand" label="品牌" width="100" />
              <el-table-column prop="quantity" label="数量" width="80" align="right" />
              <el-table-column prop="unitPrice" label="单价" width="110" align="right">
                <template #default="{ row }">
                  {{ formatCurrency(row.unitPrice, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column label="金额" width="110" align="right">
                <template #default="{ row }">
                  {{ formatCurrency(row.quantity * row.unitPrice, row.currency) }}
                </template>
              </el-table-column>
              <el-table-column prop="leadTime" label="交期" width="100" />
              <el-table-column prop="stockQty" label="库存" width="80" align="right" />
            </CrmDataTable>
            <el-empty v-else description="暂无报价明细" :image-size="80" />
          </el-tab-pane>

          <!-- 文档 -->
          <el-tab-pane label="文档" name="documents">
            <div class="doc-tab-content">
              <DocumentUploadPanel
                biz-type="QUOTE"
                :biz-id="String(quote.id)"
                :max-files="20"
                :max-size-mb="100"
                @uploaded="docListRef?.refresh()"
              />
              <DocumentListPanel
                ref="docListRef"
                biz-type="QUOTE"
                :biz-id="String(quote.id)"
                view-mode="list"
                style="margin-top: 16px;"
              />
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>
    </template>

    <el-empty v-else description="报价单不存在" />

    <!-- 更新状态弹窗 -->
    <el-dialog v-model="statusDialogVisible" title="更新状态" width="400px">
      <el-form label-width="100px">
        <el-form-item label="新状态">
          <el-select v-model="newStatus" style="width: 100%">
            <el-option label="草稿" :value="0" />
            <el-option label="待审核" :value="1" />
            <el-option label="已审核" :value="2" />
            <el-option label="已发送" :value="3" />
            <el-option label="已接受" :value="4" />
            <el-option label="已拒绝" :value="5" />
            <el-option label="已过期" :value="6" />
            <el-option label="已关闭" :value="7" />
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
import { quoteApi } from '@/api/quote'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const quote = ref<any>(null)
const activeTab = ref('items')
const docListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)

const statusDialogVisible = ref(false)
const statusLoading = ref(false)
const newStatus = ref(0)

const quoteId = computed(() => route.params.id as string)

onMounted(() => {
  fetchQuote()
})

const fetchQuote = async () => {
  loading.value = true
  try {
    const res = await quoteApi.getById(quoteId.value)
    quote.value = res.data || null
  } catch {
    quote.value = null
  } finally {
    loading.value = false
  }
}

const getStatusType = (status: number) => {
  const map: Record<number, string> = {
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success',
    4: 'success', 5: 'danger', 6: 'info', 7: 'info'
  }
  return map[status] ?? 'info'
}
const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: '草稿', 1: '待审核', 2: '已审核', 3: '已发送',
    4: '已接受', 5: '已拒绝', 6: '已过期', 7: '已关闭'
  }
  return map[status] ?? '未知'
}
const formatCurrency = (value: number, currency?: number) => {
  if (!value) return '-'
  const symbol = currency === 1 ? '$' : '¥'
  return symbol + value.toLocaleString('zh-CN', { minimumFractionDigits: 4, maximumFractionDigits: 4 })
}

const handleUpdateStatus = () => {
  if (!quote.value) return
  newStatus.value = quote.value.status
  statusDialogVisible.value = true
}

const confirmUpdateStatus = async () => {
  if (!quote.value) return
  statusLoading.value = true
  try {
    await quoteApi.updateStatus(quote.value.id, newStatus.value)
    quote.value.status = newStatus.value
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

.quote-detail {
  padding: 20px;
  min-height: 100%;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
  .back-btn {
    color: $text-secondary;
    &:hover { color: $cyan-primary; }
  }
  .header-actions {
    margin-left: auto;
    display: flex;
    gap: 8px;
  }
}

.loading-wrap {
  padding: 20px;
  background: $layer-2;
  border-radius: 8px;
}

.info-card {
  background: $layer-2;
  border: 1px solid $border-card;
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
  color: $text-primary;
  margin-bottom: 14px;
  .title-bar {
    width: 4px;
    height: 16px;
    background: $cyan-primary;
    border-radius: 2px;
  }
}

.order-desc {
  :deep(.el-descriptions__label) {
    color: $text-muted;
    background: $layer-3;
    width: 100px;
  }
  :deep(.el-descriptions__content) {
    background: $layer-2;
  }
}

.order-code {
  font-family: 'Courier New', monospace;
  color: $text-secondary;
  font-weight: 600;
}

.tab-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 0 20px 20px;
}

.detail-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 16px;
    border-bottom: 1px solid $border-panel;
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
    color: $text-muted;
    font-size: 12px;
    font-family: 'Noto Sans SC', sans-serif;
    transition: all 0.15s;
    &:hover {
      background: rgba(0, 212, 255, 0.06);
      border-color: rgba(0, 212, 255, 0.1);
      color: rgba(180, 210, 230, 0.9);
      color: $text-secondary;
    }
    &.is-active {
      background: linear-gradient(180deg, rgba(0, 212, 255, 0.12) 0%, rgba(0, 212, 255, 0.05) 100%);
      border-color: rgba(0, 212, 255, 0.25);
      color: $cyan-primary;
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
