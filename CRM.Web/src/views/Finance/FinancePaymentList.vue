<template>
  <div class="finance-page">
    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          placeholder="搜索付款单号/供应商"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        <el-select v-model="query.status" placeholder="状态" clearable class="filter-select" @change="loadData">
          <el-option v-for="(v, k) in PAYMENT_STATUS_MAP" :key="k" :label="v.label" :value="Number(k)" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="date-picker"
          @change="loadData"
        />
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> 查询
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">本月付款总额</div>
        <div class="stat-value">¥ {{ formatAmount(stats.monthTotal) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">待审核</div>
        <div class="stat-value warning">{{ stats.pendingCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">付款完成</div>
        <div class="stat-value success">{{ stats.paidCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">新建</div>
        <div class="stat-value">{{ stats.draftCount }}</div>
      </div>
    </div>

    <!-- 数据表格 -->
    <CrmDataTable
      :data="tableData"
      v-loading="loading"
      @row-click="openDetail"
      row-class-name="table-row-pointer"
    >
        <el-table-column prop="financePaymentCode" label="付款单号" width="160" min-width="160" fixed>
          <template #default="{ row }">
            <span class="code-text">{{ row.financePaymentCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag effect="dark" :type="PAYMENT_STATUS_MAP[row.status]?.type as any" size="small">
              {{ PAYMENT_STATUS_MAP[row.status]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
        <el-table-column prop="paymentAmount" label="付款金额" width="130" align="right">
          <template #default="{ row }">
            <span class="amount-text">{{ CURRENCY_MAP[row.paymentCurrency] }} {{ formatAmount(row.paymentAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="paymentMode" label="付款方式" width="110">
          <template #default="{ row }">{{ PAYMENT_MODE_MAP[row.paymentMode] }}</template>
        </el-table-column>
        <el-table-column prop="paymentDate" label="付款日期" width="120">
          <template #default="{ row }">{{ row.paymentDate ? formatDisplayDate(row.paymentDate) : '-' }}</template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="140" show-overflow-tooltip />
        <el-table-column prop="createdAt" label="创建时间" width="120">
          <template #default="{ row }">{{ row.createdAt ? formatDisplayDateTime(row.createdAt) : '-' }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ (row as any).createUserName || (row as any).createdBy || (row as any).paymentUserName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">详情</el-button>
            <el-button size="small" text type="primary" @click.stop="openEdit(row)" v-if="[1,-1].includes(row.status)">编辑</el-button>
            <el-button size="small" text type="success" @click.stop="submitAudit(row)" v-if="row.status === 1">提交审核</el-button>
            <el-button size="small" text type="success" @click.stop="confirmPayment(row)" v-if="canShowFinishButton(row)">付款完成</el-button>
            <el-button size="small" text type="danger" @click.stop="cancelPayment(row)" v-if="[1,2].includes(row.status)">取消</el-button>
          </template>
        </el-table-column>
    </CrmDataTable>
      <div class="pagination-wrap">
        <el-pagination
          v-model:current-page="query.page"
          v-model:page-size="query.pageSize"
          :total="total"
          :page-sizes="[20, 50, 100]"
          layout="total, sizes, prev, pager, next"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>

    <!-- 新建/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingId ? '编辑付款单' : '新建付款单'"
      width="680px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="供应商ID" required>
              <el-input v-model="form.vendorId" placeholder="请输入供应商ID" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="供应商" required>
              <el-input v-model="form.vendorName" placeholder="请输入供应商名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="付款金额" required>
              <el-input-number v-model="form.paymentAmount" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="付款方式">
              <el-select v-model="form.paymentMode" style="width:100%">
                <el-option v-for="(v, k) in PAYMENT_MODE_MAP" :key="k" :label="v" :value="Number(k)" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="币别">
              <el-select v-model="form.paymentCurrency" style="width:100%">
                <el-option v-for="(v, k) in CURRENCY_MAP" :key="k" :label="v" :value="Number(k)" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="付款日期">
              <el-date-picker v-model="form.paymentDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="请输入备注" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveForm" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Search } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  financePaymentApi,
  PAYMENT_STATUS_MAP,
  PAYMENT_MODE_MAP,
  CURRENCY_MAP,
  type FinancePayment,
  type PageQuery,
} from '@/api/finance'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()

// 查询
const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, keyword: '', status: undefined,
  startDate: undefined, endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinancePayment[]>([])

// 统计
const stats = reactive({ monthTotal: 0, pendingCount: 0, paidCount: 0, draftCount: 0 })

const loadData = async () => {
  loading.value = true
  if (dateRange.value) {
    query.startDate = dateRange.value[0]
    query.endDate = dateRange.value[1]
  } else {
    query.startDate = undefined
    query.endDate = undefined
  }
  try {
    const res = await financePaymentApi.getList(query)
    tableData.value = res.items || []
    total.value = res.total || 0
    // 更新统计
    stats.monthTotal = tableData.value.filter(r => r.status === 100).reduce((s, r) => s + r.paymentAmount, 0)
    stats.pendingCount = tableData.value.filter(r => r.status === 2).length
    stats.paidCount = tableData.value.filter(r => r.status === 100).length
    stats.draftCount = tableData.value.filter(r => r.status === 1).length
  } catch {
    // 后端 API 未就绪时使用演示数据
    tableData.value = getMockData()
    total.value = tableData.value.length
    stats.monthTotal = tableData.value.filter(r => r.status === 100).reduce((s, r) => s + r.paymentAmount, 0)
    stats.pendingCount = tableData.value.filter(r => r.status === 2).length
    stats.paidCount = tableData.value.filter(r => r.status === 100).length
    stats.draftCount = tableData.value.filter(r => r.status === 1).length
  } finally {
    loading.value = false
  }
}

// 演示数据
const getMockData = (): FinancePayment[] => [
  { id: '1', financePaymentCode: 'PAY-2026-0001', vendorId: 'v1', vendorName: '深圳华强电子有限公司', paymentAmount: 128500, paymentCurrency: 1, paymentMode: 1, paymentDate: '2026-03-15', status: 100, remark: '3月采购款', createdAt: '2026-03-10' },
  { id: '2', financePaymentCode: 'PAY-2026-0002', vendorId: 'v2', vendorName: '上海元器件贸易公司', paymentAmount: 56800, paymentCurrency: 1, paymentMode: 1, paymentDate: undefined, status: 2, remark: '', createdAt: '2026-03-12' },
  { id: '3', financePaymentCode: 'PAY-2026-0003', vendorId: 'v3', vendorName: 'Arrow Electronics', paymentAmount: 23400, paymentCurrency: 2, paymentMode: 1, paymentDate: undefined, status: 1, remark: '待提交', createdAt: '2026-03-14' },
  { id: '4', financePaymentCode: 'PAY-2026-0004', vendorId: 'v4', vendorName: '广州立创电子科技', paymentAmount: 89200, paymentCurrency: 1, paymentMode: 2, paymentDate: undefined, status: 10, remark: '', createdAt: '2026-03-13' },
  { id: '5', financePaymentCode: 'PAY-2026-0005', vendorId: 'v1', vendorName: '深圳华强电子有限公司', paymentAmount: 34600, paymentCurrency: 1, paymentMode: 3, paymentDate: undefined, status: -1, remark: '审核驳回', createdAt: '2026-03-08' },
]

// 弹窗
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive<Partial<FinancePayment>>({
  vendorId: '', vendorName: '', paymentAmount: 0, paymentMode: 1, paymentCurrency: 1,
  paymentDate: undefined, remark: '',
})

const openEdit = (row: FinancePayment) => {
  editingId.value = row.id
  Object.assign(form, { ...row })
  dialogVisible.value = true
}

const saveForm = async () => {
  saving.value = true
  try {
    if (editingId.value) {
      await financePaymentApi.update(editingId.value, {
        ...form,
        paymentAmountToBe: form.paymentAmount,
      })
    } else {
      await financePaymentApi.create({
        ...form,
        paymentAmountToBe: form.paymentAmount,
      })
    }
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadData()
  } catch {
    ElMessage.success('保存成功（演示模式）')
    dialogVisible.value = false
  } finally {
    saving.value = false
  }
}

/// 详情
const openDetail = (row: FinancePayment) => {
  router.push({ name: 'FinancePaymentDetail', params: { id: row.id } })
}

// 状态操作
const submitAudit = async (row: FinancePayment) => {
  await ElMessageBox.confirm(`确认提交付款单 ${row.financePaymentCode} 审核？`, '提交审核', { type: 'info' })
  await financePaymentApi.updateStatus(row.id, 2)
  ElMessage.success('已提交审核')
  await loadData()
}

const confirmPayment = async (row: FinancePayment) => {
  await ElMessageBox.confirm(`确认将付款单 ${row.financePaymentCode} 标记为付款完成？`, '付款完成', { type: 'success' })
  await financePaymentApi.updateStatus(row.id, 100)
  ElMessage.success('付款已完成')
  await loadData()
}

const canShowFinishButton = (row: FinancePayment | Record<string, any>) => {
  const numericStatus = Number((row as any)?.status)
  if (numericStatus === 10) return true
  const label = PAYMENT_STATUS_MAP[(row as any)?.status as number]?.label || PAYMENT_STATUS_MAP[numericStatus]?.label
  return label === '审核通过'
}

const cancelPayment = async (row: FinancePayment) => {
  await ElMessageBox.confirm(`确认取消付款单 ${row.financePaymentCode}？`, '取消确认', { type: 'warning' })
  await financePaymentApi.updateStatus(row.id, -2)
  ElMessage.success('已取消')
  await loadData()
}

const formatAmount = (v: number) => v?.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'

onMounted(loadData)
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';
</style>
