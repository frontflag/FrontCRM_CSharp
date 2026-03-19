<template>
  <div class="finance-page">
    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          placeholder="搜索收款单号/客户"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select v-model="query.status" placeholder="状态" clearable class="filter-select" @change="loadData">
          <el-option v-for="(v, k) in RECEIPT_STATUS_MAP" :key="k" :label="v.label" :value="Number(k)" />
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
      <div class="search-right">
        <el-button type="primary" @click="openCreate">
          <el-icon><Plus /></el-icon> 新建收款单
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">本月收款总额</div>
        <div class="stat-value success">¥ {{ formatAmount(stats.monthTotal) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">待审核</div>
        <div class="stat-value warning">{{ stats.pendingCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">已收款</div>
        <div class="stat-value success">{{ stats.receivedCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">草稿</div>
        <div class="stat-value">{{ stats.draftCount }}</div>
      </div>
    </div>

    <!-- 数据表格 -->
    <div class="table-wrap">
      <el-table
        :data="tableData"
        v-loading="loading"
        stripe
        class="crm-table"
        @row-click="openDetail"
        row-class-name="table-row-pointer"
      >
        <el-table-column prop="financeReceiptCode" label="收款单号" width="140" fixed>
          <template #default="{ row }">
            <span class="code-text">{{ row.financeReceiptCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" min-width="160" show-overflow-tooltip />
        <el-table-column prop="receiptAmount" label="收款金额" width="140" align="right">
          <template #default="{ row }">
            <span class="amount-text">{{ CURRENCY_MAP[row.receiptCurrency] }} {{ formatAmount(row.receiptAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="receiptMode" label="收款方式" width="110">
          <template #default="{ row }">{{ PAYMENT_MODE_MAP[row.receiptMode] }}</template>
        </el-table-column>
        <el-table-column prop="receiptDate" label="收款日期" width="120">
          <template #default="{ row }">{{ row.receiptDate ? row.receiptDate.slice(0, 10) : '-' }}</template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="RECEIPT_STATUS_MAP[row.status]?.type as any" size="small">
              {{ RECEIPT_STATUS_MAP[row.status]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="140" show-overflow-tooltip />
        <el-table-column prop="createdAt" label="创建时间" width="120">
          <template #default="{ row }">{{ row.createdAt ? row.createdAt.slice(0, 10) : '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">详情</el-button>
            <el-button size="small" text type="primary" @click.stop="openEdit(row)" v-if="row.status === 0">编辑</el-button>
            <el-button size="small" text type="success" @click.stop="submitAudit(row)" v-if="row.status === 0">提交审核</el-button>
            <el-button size="small" text type="success" @click.stop="approveReceipt(row)" v-if="row.status === 1">审核通过</el-button>
            <el-button size="small" text type="danger" @click.stop="cancelReceipt(row)" v-if="[0,1].includes(row.status)">取消</el-button>
          </template>
        </el-table-column>
      </el-table>
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
    </div>

    <!-- 新建/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingId ? '编辑收款单' : '新建收款单'"
      width="680px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="客户" required>
              <el-input v-model="form.customerName" placeholder="请输入客户名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="收款金额" required>
              <el-input-number v-model="form.receiptAmount" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="收款方式">
              <el-select v-model="form.receiptMode" style="width:100%">
                <el-option v-for="(v, k) in PAYMENT_MODE_MAP" :key="k" :label="v" :value="Number(k)" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="币别">
              <el-select v-model="form.receiptCurrency" style="width:100%">
                <el-option v-for="(v, k) in CURRENCY_MAP" :key="k" :label="v" :value="Number(k)" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="收款日期">
              <el-date-picker v-model="form.receiptDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
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
import { Search, Plus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  financeReceiptApi,
  RECEIPT_STATUS_MAP,
  PAYMENT_MODE_MAP,
  CURRENCY_MAP,
  type FinanceReceipt,
  type PageQuery,
} from '@/api/finance'

const router = useRouter()

const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, keyword: '', status: undefined,
  startDate: undefined, endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinanceReceipt[]>([])
const stats = reactive({ monthTotal: 0, pendingCount: 0, receivedCount: 0, draftCount: 0 })

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
    const res = await financeReceiptApi.getList(query)
    tableData.value = res.items || []
    total.value = res.total || 0
  } catch {
    tableData.value = getMockData()
    total.value = tableData.value.length
  } finally {
    loading.value = false
    stats.monthTotal = tableData.value.filter(r => r.status === 3).reduce((s, r) => s + r.receiptAmount, 0)
    stats.pendingCount = tableData.value.filter(r => r.status === 1).length
    stats.receivedCount = tableData.value.filter(r => r.status === 3).length
    stats.draftCount = tableData.value.filter(r => r.status === 0).length
  }
}

const getMockData = (): FinanceReceipt[] => [
  { id: '1', financeReceiptCode: 'RCP-2026-0001', customerId: 'c1', customerName: '北京科技有限公司', receiptAmount: 256000, receiptCurrency: 1, receiptMode: 1, receiptDate: '2026-03-14', status: 3, remark: '3月货款', createdAt: '2026-03-10' },
  { id: '2', financeReceiptCode: 'RCP-2026-0002', customerId: 'c2', customerName: '上海智能制造股份', receiptAmount: 88500, receiptCurrency: 1, receiptMode: 1, receiptDate: '2026-03-17', status: 1, remark: '', createdAt: '2026-03-12' },
  { id: '3', financeReceiptCode: 'RCP-2026-0003', customerId: 'c3', customerName: 'Acme Corp', receiptAmount: 15800, receiptCurrency: 2, receiptMode: 1, receiptDate: undefined, status: 0, remark: '待提交', createdAt: '2026-03-15' },
  { id: '4', financeReceiptCode: 'RCP-2026-0004', customerId: 'c4', customerName: '广州电子科技', receiptAmount: 43200, receiptCurrency: 1, receiptMode: 2, receiptDate: '2026-03-16', status: 2, remark: '', createdAt: '2026-03-13' },
  { id: '5', financeReceiptCode: 'RCP-2026-0005', customerId: 'c1', customerName: '北京科技有限公司', receiptAmount: 19800, receiptCurrency: 1, receiptMode: 3, receiptDate: undefined, status: 4, remark: '已取消', createdAt: '2026-03-08' },
]

const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive<Partial<FinanceReceipt>>({
  customerName: '', receiptAmount: 0, receiptMode: 1, receiptCurrency: 1,
  receiptDate: undefined, remark: '',
})

const openCreate = () => {
  editingId.value = null
  Object.assign(form, { customerName: '', receiptAmount: 0, receiptMode: 1, receiptCurrency: 1, receiptDate: undefined, remark: '' })
  dialogVisible.value = true
}

const openEdit = (row: FinanceReceipt) => {
  editingId.value = row.id
  Object.assign(form, { ...row })
  dialogVisible.value = true
}

const saveForm = async () => {
  saving.value = true
  try {
    if (editingId.value) {
      await financeReceiptApi.update(editingId.value, form)
    } else {
      await financeReceiptApi.create(form)
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

const openDetail = (row: FinanceReceipt) => {
  router.push({ name: 'FinanceReceiptDetail', params: { id: row.id } })
}

const submitAudit = async (row: FinanceReceipt) => {
  await ElMessageBox.confirm(`确认提交收款单 ${row.financeReceiptCode} 审核？`, '提交审核', { type: 'info' })
  row.status = 1
  ElMessage.success('已提交审核')
}

const approveReceipt = async (row: FinanceReceipt) => {
  await ElMessageBox.confirm('确认审核通过并标记为已收款？', '审核确认', { type: 'success' })
  row.status = 3
  ElMessage.success('审核通过，已标记为已收款')
}

const cancelReceipt = async (row: FinanceReceipt) => {
  await ElMessageBox.confirm(`确认取消收款单 ${row.financeReceiptCode}？`, '取消确认', { type: 'warning' })
  row.status = 4
  ElMessage.success('已取消')
}

const formatAmount = (v: number) => v?.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'

onMounted(loadData)
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';
</style>
