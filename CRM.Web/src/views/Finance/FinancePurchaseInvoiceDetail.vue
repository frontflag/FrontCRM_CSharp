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
          <el-descriptions-item label="开票日期">{{ detail.makeInvoiceDate?.slice(0, 10) || '-' }}</el-descriptions-item>
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
        <el-table v-else :data="detail.items" border size="small" class="items-table">
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
        </el-table>
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
    color: rgba(200, 220, 240, 0.7);
    &:hover { color: #00d4ff; }
  }
}

.loading-wrap {
  padding: 20px;
  background: #0a1828;
  border-radius: 8px;
}

.info-card, .tab-card {
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

.items-table {
  :deep(.el-table__header-wrapper th) {
    background: #0d1e35;
    color: #5a7a9a;
  }
  :deep(.el-table__row td) {
    background: #0a1828;
    border-color: #1a2d45;
  }
}
</style>
