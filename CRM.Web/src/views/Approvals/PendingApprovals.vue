<template>
  <div class="pending-approvals-page">
    <!-- 页面标题 -->
    <div class="page-header">
      <h2 class="page-title">待审批</h2>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="search-label">业务类型</span>
        <el-select v-model="searchForm.bizType" placeholder="全部" clearable style="width: 160px" @change="handleSearch">
          <el-option label="销售订单" value="SALES_ORDER" />
          <el-option label="采购订单" value="PURCHASE_ORDER" />
          <el-option label="收款单" value="RECEIPT" />
          <el-option label="付款单" value="PAYMENT" />
          <el-option label="销售发票" value="SELL_INVOICE" />
          <el-option label="采购发票" value="PURCHASE_INVOICE" />
        </el-select>
      </div>
    </div>

    <!-- 数据表格 -->
    <div class="table-wrapper">
      <el-table
        :data="approvalList"
        v-loading="loading"
        highlight-current-row
      >
        <el-table-column label="业务类型" width="120">
          <template #default="{ row }">
            <el-tag effect="dark" :type="getBizTypeTagType(row.bizType)" size="small">
              {{ getBizTypeText(row.bizType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="bizCode" label="单据编号" min-width="180">
          <template #default="{ row }">
            <span class="code-link" @click="handleView(row)">{{ row.bizCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="relatedName" label="客户/供应商" min-width="200" show-overflow-tooltip />
        <el-table-column prop="amount" label="金额" width="140" align="right">
          <template #default="{ row }">
            <span class="amount-text" v-if="row.amount != null">
              ¥{{ Number(row.amount).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
            </span>
            <span class="text-muted" v-else>—</span>
          </template>
        </el-table-column>
        <el-table-column prop="submittedAt" label="提交时间" width="160">
          <template #default="{ row }">
            {{ formatDate(row.submittedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <div class="action-btns">
              <el-button link type="primary" size="small" @click="handleApprove(row)">审批通过</el-button>
              <el-button link type="danger" size="small" @click="handleReject(row)">驳回</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          v-model:current-page="pagination.pageNumber"
          v-model:page-size="pagination.pageSize"
          :page-sizes="[20, 50, 100]"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next"
          @size-change="handleSearch"
          @current-change="handleSearch"
        />
      </div>
    </div>

    <!-- 驳回原因弹窗 -->
    <el-dialog v-model="rejectDialogVisible" title="驳回原因" width="420px">
      <el-form>
        <el-form-item label="驳回原因" label-width="80px">
          <el-input
            v-model="rejectReason"
            type="textarea"
            :rows="3"
            placeholder="请输入驳回原因"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rejectDialogVisible = false">取消</el-button>
        <el-button type="danger" :loading="actionLoading" @click="confirmReject">确认驳回</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'

const router = useRouter()

const loading = ref(false)
const actionLoading = ref(false)
const rejectDialogVisible = ref(false)
const rejectReason = ref('')
const currentRow = ref<any>(null)

const searchForm = ref({
  bizType: ''
})

const pagination = ref({
  pageNumber: 1,
  pageSize: 20,
  total: 0
})

const approvalList = ref<any[]>([])

const getBizTypeText = (type: string) => {
  const map: Record<string, string> = {
    SALES_ORDER: '销售订单',
    PURCHASE_ORDER: '采购订单',
    RECEIPT: '收款单',
    PAYMENT: '付款单',
    SELL_INVOICE: '销售发票',
    PURCHASE_INVOICE: '采购发票'
  }
  return map[type] || type
}

const getBizTypeTagType = (type: string) => {
  const map: Record<string, string> = {
    SALES_ORDER: 'primary',
    PURCHASE_ORDER: 'warning',
    RECEIPT: 'success',
    PAYMENT: 'danger',
    SELL_INVOICE: 'info',
    PURCHASE_INVOICE: ''
  }
  return map[type] || ''
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '—'
  const d = new Date(dateStr)
  return d.toLocaleDateString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit' })
    + ' ' + d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}

const handleSearch = async () => {
  loading.value = true
  try {
    // TODO: 接入真实 API
    await new Promise(resolve => setTimeout(resolve, 300))
    approvalList.value = []
    pagination.value.total = 0
  } catch (e) {
    ElMessage.error('加载失败')
  } finally {
    loading.value = false
  }
}

const handleView = (row: any) => {
  const routeMap: Record<string, string> = {
    SALES_ORDER: `/sales-orders/${row.bizId}`,
    PURCHASE_ORDER: `/purchase-orders/${row.bizId}`,
    RECEIPT: `/finance/receipts/${row.bizId}`,
    PAYMENT: `/finance/payments/${row.bizId}`,
    SELL_INVOICE: `/finance/sell-invoices/${row.bizId}`,
    PURCHASE_INVOICE: `/finance/purchase-invoices/${row.bizId}`
  }
  const path = routeMap[row.bizType]
  if (path) router.push(path)
}

const handleApprove = async (row: any) => {
  await ElMessageBox.confirm(`确认审批通过单据 ${row.bizCode}？`, '审批确认', {
    confirmButtonText: '确认通过',
    cancelButtonText: '取消',
    type: 'success'
  })
  ElMessage.success('审批通过')
  handleSearch()
}

const handleReject = (row: any) => {
  currentRow.value = row
  rejectReason.value = ''
  rejectDialogVisible.value = true
}

const confirmReject = async () => {
  if (!rejectReason.value.trim()) {
    ElMessage.warning('请输入驳回原因')
    return
  }
  actionLoading.value = true
  try {
    await new Promise(resolve => setTimeout(resolve, 300))
    ElMessage.success('已驳回')
    rejectDialogVisible.value = false
    handleSearch()
  } finally {
    actionLoading.value = false
  }
}

onMounted(() => {
  handleSearch()
})
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.pending-approvals-page {
  padding: 20px 24px;
  min-height: 100%;
}

.page-header {
  margin-bottom: 18px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  letter-spacing: 0.3px;
}

// 搜索栏
.search-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 14px 18px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
}

.search-label {
  font-size: 13px;
  color: $text-secondary;
  white-space: nowrap;
  margin-right: 8px;
}

// 表格容器
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

// 编号链接
.code-link {
  color: $cyan-primary;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Space Mono', monospace;
  transition: color 0.15s;

  &:hover {
    color: lighten(#00d4ff, 10%);
    text-decoration: underline;
  }
}

.amount-text {
  color: $color-mint-green;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Space Mono', monospace;
}

.text-muted {
  color: $text-muted;
}

.action-btns {
  display: flex;
  gap: 4px;
  white-space: nowrap;
}

// 分页
.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  padding: 14px 18px;
  border-top: 1px solid rgba(255, 255, 255, 0.04);

  :deep(.el-pagination) {
    --el-pagination-bg-color: transparent;
    --el-pagination-button-bg-color: transparent;
    --el-pagination-hover-color: #{$cyan-primary};
    color: $text-secondary;

    .el-pagination__total,
    .el-pagination__sizes {
      color: $text-muted;
    }

    .el-pager li {
      background: transparent;
      color: $text-secondary;

      &.is-active {
        color: $cyan-primary;
        font-weight: 600;
      }

      &:hover {
        color: $cyan-primary;
      }
    }

    button {
      background: transparent;
      color: $text-secondary;

      &:hover {
        color: $cyan-primary;
      }

      &:disabled {
        color: $text-muted;
      }
    }
  }
}
</style>
