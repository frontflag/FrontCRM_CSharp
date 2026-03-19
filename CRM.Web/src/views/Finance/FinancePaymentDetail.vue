<template>
  <div class="finance-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> 返回列表
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'FinancePaymentList' }">付款管理</el-breadcrumb-item>
        <el-breadcrumb-item>{{ detail?.financePaymentCode || '详情' }}</el-breadcrumb-item>
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
          <el-tag :type="PAYMENT_STATUS_MAP[detail.status]?.type as any" size="small" style="margin-left: 12px;">
            {{ PAYMENT_STATUS_MAP[detail.status]?.label }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item label="付款单号">
            <span class="order-code">{{ detail.financePaymentCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="PAYMENT_STATUS_MAP[detail.status]?.type as any">
              {{ PAYMENT_STATUS_MAP[detail.status]?.label }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="供应商">{{ detail.vendorName }}</el-descriptions-item>
          <el-descriptions-item label="付款金额">
            <span class="amount">{{ CURRENCY_MAP[detail.paymentCurrency] }} {{ formatAmount(detail.paymentAmount) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="付款方式">{{ PAYMENT_MODE_MAP[detail.paymentMode] }}</el-descriptions-item>
          <el-descriptions-item label="付款日期">{{ detail.paymentDate?.slice(0, 10) || '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 付款明细 -->
      <div class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>付款明细</span>
        </div>
        <el-empty v-if="!detail.items?.length" description="暂无明细" :image-size="80" />
        <el-table v-else :data="detail.items" border size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="pn" label="型号" min-width="150" />
          <el-table-column prop="brand" label="品牌" width="120" />
          <el-table-column prop="paymentAmount" label="已付金额" width="130" align="right">
            <template #default="{ row }">
              {{ formatAmount(row.paymentAmount) }}
            </template>
          </el-table-column>
          <el-table-column label="核销状态" width="120" align="center">
            <template #default="{ row }">
              <el-tag size="small" :type="row.verificationStatus === 2 ? 'success' : row.verificationStatus === 1 ? 'warning' : 'info'">
                {{ row.verificationStatus === 2 ? '核销完成' : row.verificationStatus === 1 ? '部分核销' : '未核销' }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </template>

    <el-empty v-else description="付款单不存在" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeft } from '@element-plus/icons-vue'
import {
  financePaymentApi,
  PAYMENT_STATUS_MAP,
  PAYMENT_MODE_MAP,
  CURRENCY_MAP,
  type FinancePayment,
} from '@/api/finance'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const detail = ref<FinancePayment | null>(null)

const paymentId = computed(() => route.params.id as string)

onMounted(() => {
  fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    // apiClient 拦截器已解包，直接返回业务数据
    detail.value = await financePaymentApi.getById(paymentId.value)
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
