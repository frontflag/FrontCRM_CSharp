<template>
  <div class="finance-page">
    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          placeholder="搜索发票单号/客户/发票号"
          clearable
          class="search-input"
          style="width:240px"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select v-model="query.status" placeholder="开票状态" clearable class="filter-select" style="width:130px" @change="loadData">
          <el-option v-for="(v, k) in INVOICE_STATUS_MAP" :key="k" :label="v.label" :value="Number(k)" />
        </el-select>
        <el-select v-model="filterReceiveStatus" placeholder="收款状态" clearable class="filter-select" style="width:120px" @change="loadData">
          <el-option v-for="(v, k) in RECEIVE_STATUS_MAP" :key="k" :label="v.label" :value="Number(k)" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开票开始"
          end-placeholder="开票结束"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="date-picker"
          @change="loadData"
        />
        <el-button type="primary" @click="loadData"><el-icon><Search /></el-icon> 查询</el-button>
      </div>
      <div class="search-right">
        <el-button type="primary" @click="openCreate">
          <el-icon><Plus /></el-icon> 新建销项发票
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">发票总金额</div>
        <div class="stat-value">¥ {{ formatAmount(stats.totalAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">已收款金额</div>
        <div class="stat-value success">¥ {{ formatAmount(stats.receivedAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">待收款金额</div>
        <div class="stat-value warning">¥ {{ formatAmount(stats.toReceiveAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">已开票数量</div>
        <div class="stat-value">{{ stats.invoicedCount }}</div>
      </div>
    </div>

    <!-- 数据表格 -->
    <CrmDataTable
      :data="tableData"
      v-loading="loading"
      @row-dblclick="openDetail"
      row-class-name="table-row-pointer"
    >
        <el-table-column prop="invoiceCode" label="发票单号" width="160" min-width="160" fixed>
          <template #default="{ row }">
            <span class="code-text">{{ row.invoiceCode || '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="invoiceStatus" label="开票状态" width="100">
          <template #default="{ row }">
            <el-tag effect="dark" :type="INVOICE_STATUS_MAP[row.invoiceStatus]?.type as any" size="small">
              {{ INVOICE_STATUS_MAP[row.invoiceStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="customerName" label="客户" min-width="160" show-overflow-tooltip />
        <el-table-column prop="invoiceNo" label="发票号码" width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.invoiceNo || '-' }}</template>
        </el-table-column>
        <el-table-column prop="invoiceTotal" label="发票金额" width="130" align="right">
          <template #default="{ row }">
            <span class="amount-text">{{ CURRENCY_MAP[row.currency] }} {{ formatAmount(row.invoiceTotal) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="receiveDone" label="已收金额" width="130" align="right">
          <template #default="{ row }">
            <span class="amount-text">{{ CURRENCY_MAP[row.currency] }} {{ formatAmount(row.receiveDone) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="receiveStatus" label="收款状态" width="110">
          <template #default="{ row }">
            <el-tag effect="dark" :type="RECEIVE_STATUS_MAP[row.receiveStatus]?.type as any" size="small">
              {{ RECEIVE_STATUS_MAP[row.receiveStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sellInvoiceType" label="发票类型" width="140">
          <template #default="{ row }">{{ SELL_INVOICE_TYPE_MAP[row.sellInvoiceType] }}</template>
        </el-table-column>
        <el-table-column prop="makeInvoiceDate" label="开票日期" width="120">
          <template #default="{ row }">{{ row.makeInvoiceDate ? formatDisplayDate(row.makeInvoiceDate) : '-' }}</template>
        </el-table-column>
        <el-table-column label="创建时间" width="120">
          <template #default="{ row }">{{ row.createdAt ? formatDisplayDateTime(row.createdAt) : '-' }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right" class-name="op-col" label-class-name="op-col">
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div class="action-btns">
                <el-button size="small" text type="primary" @click.stop="openDetail(row)">详情</el-button>
                <el-button size="small" text type="primary" @click.stop="openEdit(row)" v-if="row.invoiceStatus === 1">编辑</el-button>
                <el-button size="small" text type="warning" @click.stop="applyInvoice(row)" v-if="row.invoiceStatus === 1">申请开票</el-button>
                <el-button size="small" text type="danger" @click.stop="voidInvoice(row)" v-if="row.invoiceStatus === 100">作废</el-button>
              </div>
            </div>
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
      :title="editingId ? '编辑销项发票' : '新建销项发票'"
      width="720px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="客户" required>
              <el-select
                v-model="form.customerId"
                placeholder="输入关键字搜索客户"
                style="width: 100%"
                filterable
                clearable
                :filter-method="onCustomerFilterInput"
                :loading="customerSearchLoading"
                loading-text="搜索中..."
                @change="onCustomerChange"
              >
                <template #empty>
                  <div class="select-hint">请输入关键字搜索客户</div>
                </template>
                <el-option
                  v-for="c in customerOptions"
                  :key="c.value"
                  :label="c.label"
                  :value="c.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="发票号码">
              <el-input v-model="form.invoiceNo" placeholder="请输入纸质发票号码" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="发票金额" required>
              <el-input-number v-model="form.invoiceTotal" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="开票日期">
              <el-date-picker v-model="form.makeInvoiceDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="发票类型">
              <el-select v-model="form.sellInvoiceType" style="width:100%">
                <el-option v-for="(v, k) in SELL_INVOICE_TYPE_MAP" :key="k" :label="v" :value="Number(k)" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="蓝/红字">
              <el-select v-model="form.type" style="width:100%">
                <el-option v-for="(v, k) in INVOICE_TYPE_MAP" :key="k" :label="v" :value="Number(k)" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="币别">
              <el-select v-model="form.currency" style="width:100%">
                <el-option
                  v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
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
  financeSellInvoiceApi,
  INVOICE_STATUS_MAP,
  RECEIVE_STATUS_MAP,
  SELL_INVOICE_TYPE_MAP,
  INVOICE_TYPE_MAP,
  CURRENCY_MAP,
  type FinanceSellInvoice,
  type PageQuery,
} from '@/api/finance'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { customerApi } from '@/api/customer'

const router = useRouter()

type CustomerOption = { value: string; label: string }

const customerOptions = ref<CustomerOption[]>([])
const customerSearchLoading = ref(false)
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

async function onCustomerFilterInput(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) return
  customerSearchTimer = setTimeout(async () => {
    customerSearchLoading.value = true
    try {
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 30,
        searchTerm: query.trim(),
      })
      customerOptions.value = (res.items || []).map((c) => ({
        value: c.id,
        label: c.customerName || (c as { officialName?: string }).officialName || '未知客户',
      }))
    } catch {
      customerOptions.value = []
    } finally {
      customerSearchLoading.value = false
    }
  }, 300)
}

function onCustomerChange(val: string | undefined) {
  const id = val?.trim() || ''
  if (!id) {
    form.customerName = ''
    return
  }
  const found = customerOptions.value.find((c) => c.value === id)
  if (found) form.customerName = found.label
}

const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, keyword: '', status: undefined,
  startDate: undefined, endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const filterReceiveStatus = ref<number | undefined>(undefined)
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinanceSellInvoice[]>([])
const stats = reactive({ totalAmount: 0, receivedAmount: 0, toReceiveAmount: 0, invoicedCount: 0 })

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
    const res = await financeSellInvoiceApi.getList(query)
    tableData.value = res.items || []
    total.value = res.total || 0
  } catch {
    tableData.value = getMockData()
    total.value = tableData.value.length
  } finally {
    loading.value = false
    stats.totalAmount = tableData.value.reduce((s, r) => s + r.invoiceTotal, 0)
    stats.receivedAmount = tableData.value.reduce((s, r) => s + r.receiveDone, 0)
    stats.toReceiveAmount = tableData.value.reduce((s, r) => s + r.receiveToBe, 0)
    stats.invoicedCount = tableData.value.filter(r => r.invoiceStatus === 100).length
  }
}

const getMockData = (): FinanceSellInvoice[] => [
  { id: '1', invoiceCode: 'SIN-2026-0001', customerId: 'c1', customerName: '北京科技有限公司', invoiceNo: '41200000000001', invoiceTotal: 256000, makeInvoiceDate: '2026-03-12', receiveStatus: 2, receiveDone: 256000, receiveToBe: 0, currency: 1, type: 10, invoiceStatus: 100, sellInvoiceType: 100, remark: '', createdAt: '2026-03-12' },
  { id: '2', invoiceCode: 'SIN-2026-0002', customerId: 'c2', customerName: '上海智能制造股份', invoiceNo: '41200000000002', invoiceTotal: 88500, makeInvoiceDate: '2026-03-14', receiveStatus: 1, receiveDone: 50000, receiveToBe: 38500, currency: 1, type: 10, invoiceStatus: 100, sellInvoiceType: 200, remark: '', createdAt: '2026-03-14' },
  { id: '3', invoiceCode: 'SIN-2026-0003', customerId: 'c3', customerName: 'Acme Corp', invoiceNo: undefined, invoiceTotal: 15800, makeInvoiceDate: undefined, receiveStatus: 0, receiveDone: 0, receiveToBe: 15800, currency: 2, type: 10, invoiceStatus: 1, sellInvoiceType: 100, remark: '待申请开票', createdAt: '2026-03-16' },
  { id: '4', invoiceCode: 'SIN-2026-0004', customerId: 'c4', customerName: '广州电子科技', invoiceNo: '41200000000004', invoiceTotal: 43200, makeInvoiceDate: '2026-03-15', receiveStatus: 0, receiveDone: 0, receiveToBe: 43200, currency: 1, type: 10, invoiceStatus: 100, sellInvoiceType: 100, remark: '', createdAt: '2026-03-15' },
  { id: '5', invoiceCode: 'SIN-2026-0005', customerId: 'c1', customerName: '北京科技有限公司', invoiceNo: '41200000000005', invoiceTotal: 19800, makeInvoiceDate: '2026-03-09', receiveStatus: 2, receiveDone: 19800, receiveToBe: 0, currency: 1, type: 20, invoiceStatus: -1, sellInvoiceType: 200, remark: '已作废', createdAt: '2026-03-09' },
]

const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive<Partial<FinanceSellInvoice>>({
  customerId: '',
  customerName: '',
  invoiceNo: '',
  invoiceTotal: 0,
  makeInvoiceDate: undefined,
  sellInvoiceType: 100,
  type: 10,
  currency: 1,
  remark: '',
})

const openCreate = () => {
  editingId.value = null
  customerOptions.value = []
  Object.assign(form, {
    customerId: '',
    customerName: '',
    invoiceNo: '',
    invoiceTotal: 0,
    makeInvoiceDate: undefined,
    sellInvoiceType: 100,
    type: 10,
    currency: 1,
    remark: '',
  })
  dialogVisible.value = true
}

const openEdit = (row: FinanceSellInvoice) => {
  editingId.value = row.id
  Object.assign(form, { ...row })
  customerOptions.value = row.customerId
    ? [{ value: row.customerId, label: row.customerName || '客户' }]
    : []
  dialogVisible.value = true
}

const saveForm = async () => {
  if (!form.customerId?.trim()) {
    ElMessage.warning('请选择客户')
    return
  }
  saving.value = true
  try {
    if (editingId.value) {
      await financeSellInvoiceApi.update(editingId.value, form)
    } else {
      await financeSellInvoiceApi.create(form)
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

const openDetail = (row: FinanceSellInvoice) => {
  router.push({ name: 'FinanceSellInvoiceDetail', params: { id: row.id } })
}

const applyInvoice = async (row: FinanceSellInvoice) => {
  await ElMessageBox.confirm(`确认申请开票 ${row.invoiceCode}？`, '申请开票', { type: 'info' })
  await financeSellInvoiceApi.submitApplication(row.id)
  ElMessage.success('已提交开票申请')
  await loadData()
}

const voidInvoice = async (row: FinanceSellInvoice) => {
  await ElMessageBox.confirm(`确认作废发票 ${row.invoiceCode}？此操作不可撤销。`, '作废确认', { type: 'warning' })
  await financeSellInvoiceApi.void(row.id)
  ElMessage.success('发票已作废')
  await loadData()
}

const formatAmount = (v: number) => v?.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'

onMounted(loadData)
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';

.select-hint {
  padding: 8px 12px;
  color: rgba(80, 187, 227, 0.55);
  font-size: 12px;
}
</style>
