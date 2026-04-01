<template>
  <div class="finance-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> 返回列表
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'FinancePurchaseInvoiceList' }">进项发票</el-breadcrumb-item>
        <el-breadcrumb-item>{{ detail?.financePurchaseInvoiceCode || '详情' }}</el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="detail">
      <!-- 基本信息卡片 -->
      <div class="info-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>基本信息</span>
          <el-tag effect="dark" :type="INVOICE_STATUS_MAP[detail.invoiceStatus]?.type as any" size="small" style="margin-left: 12px;">
            {{ INVOICE_STATUS_MAP[detail.invoiceStatus]?.label }}
          </el-tag>
          <el-tag effect="dark" :type="PAYMENT_DONE_STATUS_MAP[detail.paymentStatus]?.type as any" size="small" style="margin-left: 4px;">
            {{ PAYMENT_DONE_STATUS_MAP[detail.paymentStatus]?.label }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item label="发票单号">
            <span class="order-code">{{ detail.financePurchaseInvoiceCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="发票号码">{{ detail.invoiceNo || '-' }}</el-descriptions-item>
          <el-descriptions-item label="供应商">{{ detail.vendorName }}</el-descriptions-item>
          <el-descriptions-item label="发票金额">
            <span class="amount">¥ {{ formatAmount(detail.invoiceTotal) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="已付金额">
            <span class="amount">¥ {{ formatAmount(detail.paymentDone) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="待付金额">
            <span style="color:#E8A838; font-weight: 600;">¥ {{ formatAmount(detail.paymentToBe) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="开票日期">{{ detail.makeInvoiceDate ? formatDisplayDate(detail.makeInvoiceDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item label="发票类型">{{ PURCHASE_INVOICE_TYPE_MAP[detail.purchaseInvoiceType] }}</el-descriptions-item>
          <el-descriptions-item label="蓝/红字">{{ INVOICE_TYPE_MAP[detail.type] }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 发票明细 -->
      <div class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>发票明细</span>
        </div>
        <el-empty v-if="!detail.items?.length" description="暂无明细" :image-size="80" />
        <CrmDataTable v-else :data="detail.items" size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="stockInCode" label="入库单号" min-width="140" />
          <el-table-column prop="purchaseOrderCode" label="采购单号" min-width="140" />
          <el-table-column prop="billQty" label="数量" width="80" align="right" />
          <el-table-column prop="billAmount" label="含税金额" width="130" align="right">
            <template #default="{ row }">
              ¥ {{ formatAmount(row.billAmount) }}
            </template>
          </el-table-column>
          <el-table-column prop="taxRate" label="税率" width="80" align="center">
            <template #default="{ row }">
              {{ (row.taxRate * 100).toFixed(0) }}%
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>
    </template>

    <el-empty v-else description="进项发票不存在" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import {
  financePurchaseInvoiceApi,
  INVOICE_STATUS_MAP,
  PAYMENT_DONE_STATUS_MAP,
  PURCHASE_INVOICE_TYPE_MAP,
  INVOICE_TYPE_MAP,
  type FinancePurchaseInvoice,
} from '@/api/finance'
import { formatDisplayDate } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const detail = ref<FinancePurchaseInvoice | null>(null)

const invoiceId = computed(() => route.params.id as string)

onMounted(() => {
  fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    detail.value = await financePurchaseInvoiceApi.getById(invoiceId.value)
  } catch {
    detail.value = null
  } finally {
    loading.value = false
  }
}

const formatAmount = (val: number) => {
  if (val == null) return '-'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.finance-detail {
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
}

.loading-wrap {
  padding: 20px;
  background: $layer-2;
  border-radius: 8px;
}

.info-card, .tab-card {
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

.amount {
  color: $cyan-primary;
  font-weight: 600;
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
</style>
