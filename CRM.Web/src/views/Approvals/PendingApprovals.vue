<template>
  <div class="pending-approvals-page">
    <div class="page-header">
      <h2 class="page-title">待审批</h2>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="search-label">业务类型</span>
        <el-select
          v-model="searchForm.bizType"
          placeholder="全部"
          clearable
          style="width: 200px"
          @change="handleSearch"
        >
          <el-option label="供应商" value="VENDOR" />
          <el-option label="报价单" value="QUOTE" />
          <el-option label="销售订单" value="SALES_ORDER" />
          <el-option label="收款单" value="FINANCE_RECEIPT" />
          <el-option label="付款单" value="FINANCE_PAYMENT" />
        </el-select>
      </div>
    </div>

    <CrmDataTable :data="approvalList" v-loading="loading" highlight-current-row>
      <el-table-column label="业务类型" width="120">
        <template #default="{ row }">
          <el-tag effect="dark" :type="getBizTypeTagType(row.bizType)" size="small">
            {{ row.bizTypeName || getBizTypeText(row.bizType) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="documentCode" label="单据编号" min-width="180">
        <template #default="{ row }">
          <span class="code-link" @click="handleView(row)">{{ row.documentCode }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="counterpartyName" label="客户/供应商" min-width="200" show-overflow-tooltip />
      <el-table-column prop="amount" label="金额" width="140" align="right">
        <template #default="{ row }">
          <span class="amount-text" v-if="row.amount != null">
            {{ formatAmount(row.amount, row.currency) }}
          </span>
          <span class="text-muted" v-else>—</span>
        </template>
      </el-table-column>
      <el-table-column prop="createdAt" label="提交时间" width="160">
        <template #default="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <div class="action-btns">
            <el-button link type="primary" size="small" :loading="actionLoading" @click="handleApprove(row)">
              审批通过
            </el-button>
            <el-button link type="danger" size="small" :loading="actionLoading" @click="handleReject(row)">
              驳回
            </el-button>
          </div>
        </template>
      </el-table-column>
    </CrmDataTable>

    <div class="pagination-wrapper">
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next"
        @size-change="handleSearch"
        @current-change="handleSearch"
      />
    </div>

    <el-dialog v-model="rejectDialogVisible" title="驳回原因" width="420px">
      <el-form>
        <el-form-item label="驳回原因" label-width="80px">
          <el-input
            v-model="rejectReason"
            type="textarea"
            :rows="3"
            :placeholder="rejectRemarkRequired ? '销售订单驳回须填写原因' : '选填'"
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
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { approvalsApi, type BizType, type PendingApprovalItem } from '@/api/approvals'

const router = useRouter()

const loading = ref(false)
const actionLoading = ref(false)
const rejectDialogVisible = ref(false)
const rejectReason = ref('')
const currentRow = ref<PendingApprovalItem | null>(null)

const searchForm = ref({
  bizType: '' as '' | BizType
})

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

const approvalList = ref<PendingApprovalItem[]>([])

const rejectRemarkRequired = computed(() => currentRow.value?.bizType === 'SALES_ORDER')

const getBizTypeText = (type: string) => {
  const map: Record<string, string> = {
    VENDOR: '供应商',
    QUOTE: '报价单',
    SALES_ORDER: '销售订单',
    FINANCE_RECEIPT: '收款单',
    FINANCE_PAYMENT: '付款单'
  }
  return map[type] || type
}

const getBizTypeTagType = (type: string) => {
  const map: Record<string, string> = {
    VENDOR: 'warning',
    QUOTE: 'primary',
    SALES_ORDER: 'primary',
    FINANCE_RECEIPT: 'success',
    FINANCE_PAYMENT: 'danger'
  }
  return map[type] || ''
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '—'
  const d = new Date(dateStr)
  return (
    d.toLocaleDateString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit' }) +
    ' ' +
    d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  )
}

/** 币别：1=CNY 2=USD 3=EUR */
const formatAmount = (amount: number, currency?: number | null) => {
  const sym = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return sym + Number(amount).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const handleSearch = async () => {
  loading.value = true
  try {
    const res = await approvalsApi.getPendingApprovals({
      bizType: searchForm.value.bizType || undefined,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize
    })
    approvalList.value = res.items ?? []
    pagination.value.total = res.total ?? 0
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : '加载失败')
  } finally {
    loading.value = false
  }
}

const handleView = (row: PendingApprovalItem) => {
  const id = row.businessId
  switch (row.bizType) {
    case 'SALES_ORDER':
      router.push({ name: 'SalesOrderDetail', params: { id } })
      break
    case 'VENDOR':
      router.push({ name: 'VendorDetail', params: { id } })
      break
    case 'QUOTE':
      router.push({ name: 'QuoteDetail', params: { id } })
      break
    case 'FINANCE_RECEIPT':
      router.push({ name: 'FinanceReceiptDetail', params: { id } })
      break
    case 'FINANCE_PAYMENT':
      router.push({ name: 'FinancePaymentDetail', params: { id } })
      break
    default:
      ElMessage.warning('暂不支持从待审批跳转该类型')
  }
}

const handleApprove = async (row: PendingApprovalItem) => {
  try {
    await ElMessageBox.confirm(`确认审批通过单据 ${row.documentCode}？`, '审批确认', {
      confirmButtonText: '确认通过',
      cancelButtonText: '取消',
      type: 'success'
    })
    actionLoading.value = true
    await approvalsApi.decidePendingApproval({
      bizType: row.bizType,
      businessId: row.businessId,
      decision: 'approve'
    })
    ElMessage.success('审批通过')
    await handleSearch()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e instanceof Error ? e.message : '操作失败')
    }
  } finally {
    actionLoading.value = false
  }
}

const handleReject = (row: PendingApprovalItem) => {
  currentRow.value = row
  rejectReason.value = ''
  rejectDialogVisible.value = true
}

const confirmReject = async () => {
  if (!currentRow.value) return
  if (currentRow.value.bizType === 'SALES_ORDER' && !rejectReason.value.trim()) {
    ElMessage.warning('请输入驳回原因')
    return
  }
  actionLoading.value = true
  try {
    await approvalsApi.decidePendingApproval({
      bizType: currentRow.value.bizType,
      businessId: currentRow.value.businessId,
      decision: 'reject',
      remark: rejectReason.value.trim() || undefined
    })
    ElMessage.success('已驳回')
    rejectDialogVisible.value = false
    await handleSearch()
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : '操作失败')
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
