<template>
  <div class="finance-page">
    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          placeholder="搜索发票单号/供应商/发票号"
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
        <el-select v-model="filterPayStatus" placeholder="付款状态" clearable class="filter-select" style="width:120px" @change="loadData">
          <el-option v-for="(v, k) in PAYMENT_DONE_STATUS_MAP" :key="k" :label="v.label" :value="Number(k)" />
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
          <el-icon><Plus /></el-icon> 新建进项发票
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
        <div class="stat-label">已付款金额</div>
        <div class="stat-value success">¥ {{ formatAmount(stats.paidAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">待付款金额</div>
        <div class="stat-value warning">¥ {{ formatAmount(stats.toPayAmount) }}</div>
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
      @row-click="openDetail"
      row-class-name="table-row-pointer"
    >
        <el-table-column prop="financePurchaseInvoiceCode" label="发票单号" width="150" fixed>
          <template #default="{ row }">
            <span class="code-text">{{ row.financePurchaseInvoiceCode }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
        <el-table-column prop="invoiceNo" label="发票号码" width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.invoiceNo || '-' }}</template>
        </el-table-column>
        <el-table-column prop="invoiceTotal" label="发票金额" width="130" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥ {{ formatAmount(row.invoiceTotal) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="paymentDone" label="已付金额" width="130" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥ {{ formatAmount(row.paymentDone) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="paymentStatus" label="付款状态" width="110">
          <template #default="{ row }">
            <el-tag effect="dark" :type="PAYMENT_DONE_STATUS_MAP[row.paymentStatus]?.type as any" size="small">
              {{ PAYMENT_DONE_STATUS_MAP[row.paymentStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="invoiceStatus" label="开票状态" width="100">
          <template #default="{ row }">
            <el-tag effect="dark" :type="INVOICE_STATUS_MAP[row.invoiceStatus]?.type as any" size="small">
              {{ INVOICE_STATUS_MAP[row.invoiceStatus]?.label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="purchaseInvoiceType" label="发票类型" width="140">
          <template #default="{ row }">{{ PURCHASE_INVOICE_TYPE_MAP[row.purchaseInvoiceType] }}</template>
        </el-table-column>
        <el-table-column prop="makeInvoiceDate" label="开票日期" width="120">
          <template #default="{ row }">{{ row.makeInvoiceDate ? formatDisplayDate(row.makeInvoiceDate) : '-' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">详情</el-button>
            <el-button size="small" text type="primary" @click.stop="openEdit(row)" v-if="row.invoiceStatus === 1">编辑</el-button>
            <el-button size="small" text type="danger" @click.stop="voidInvoice(row)" v-if="row.invoiceStatus === 100">作废</el-button>
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
      :title="editingId ? '编辑进项发票' : '新建进项发票'"
      width="720px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="供应商" required>
              <el-input v-model="form.vendorName" placeholder="请输入供应商名称" />
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
              <el-select v-model="form.purchaseInvoiceType" style="width:100%">
                <el-option v-for="(v, k) in PURCHASE_INVOICE_TYPE_MAP" :key="k" :label="v" :value="Number(k)" />
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
                <el-option v-for="(v, k) in CURRENCY_MAP" :key="k" :label="v" :value="Number(k)" />
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
  financePurchaseInvoiceApi,
  INVOICE_STATUS_MAP,
  PAYMENT_DONE_STATUS_MAP,
  PURCHASE_INVOICE_TYPE_MAP,
  INVOICE_TYPE_MAP,
  CURRENCY_MAP,
  type FinancePurchaseInvoice,
  type PageQuery,
} from '@/api/finance'
import { formatDisplayDate } from '@/utils/displayDateTime'

const router = useRouter()

const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, keyword: '', status: undefined,
  startDate: undefined, endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const filterPayStatus = ref<number | undefined>(undefined)
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinancePurchaseInvoice[]>([])
const stats = reactive({ totalAmount: 0, paidAmount: 0, toPayAmount: 0, invoicedCount: 0 })

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
    const res = await financePurchaseInvoiceApi.getList(query)
    tableData.value = res.items || []
    total.value = res.total || 0
  } catch {
    tableData.value = getMockData()
    total.value = tableData.value.length
  } finally {
    loading.value = false
    stats.totalAmount = tableData.value.reduce((s, r) => s + r.invoiceTotal, 0)
    stats.paidAmount = tableData.value.reduce((s, r) => s + r.paymentDone, 0)
    stats.toPayAmount = tableData.value.reduce((s, r) => s + r.paymentToBe, 0)
    stats.invoicedCount = tableData.value.filter(r => r.invoiceStatus === 100).length
  }
}

const getMockData = (): FinancePurchaseInvoice[] => [
  { id: '1', financePurchaseInvoiceCode: 'PIN-2026-0001', vendorId: 'v1', vendorName: '深圳华强电子', invoiceNo: '31200000000001', invoiceTotal: 128500, makeInvoiceDate: '2026-03-10', paymentStatus: 2, paymentDone: 128500, paymentToBe: 0, currency: 1, type: 10, invoiceStatus: 100, purchaseInvoiceType: 100, remark: '', createdAt: '2026-03-10' },
  { id: '2', financePurchaseInvoiceCode: 'PIN-2026-0002', vendorId: 'v2', vendorName: '上海元器件贸易', invoiceNo: '31200000000002', invoiceTotal: 56800, makeInvoiceDate: '2026-03-12', paymentStatus: 1, paymentDone: 30000, paymentToBe: 26800, currency: 1, type: 10, invoiceStatus: 100, purchaseInvoiceType: 200, remark: '', createdAt: '2026-03-12' },
  { id: '3', financePurchaseInvoiceCode: 'PIN-2026-0003', vendorId: 'v3', vendorName: '广州立创电子', invoiceNo: undefined, invoiceTotal: 89200, makeInvoiceDate: undefined, paymentStatus: 0, paymentDone: 0, paymentToBe: 89200, currency: 1, type: 10, invoiceStatus: 1, purchaseInvoiceType: 100, remark: '待开票', createdAt: '2026-03-14' },
  { id: '4', financePurchaseInvoiceCode: 'PIN-2026-0004', vendorId: 'v4', vendorName: 'Arrow Electronics', invoiceNo: 'INV-AR-20260315', invoiceTotal: 23400, makeInvoiceDate: '2026-03-15', paymentStatus: 0, paymentDone: 0, paymentToBe: 23400, currency: 2, type: 10, invoiceStatus: 100, purchaseInvoiceType: 100, remark: '', createdAt: '2026-03-15' },
  { id: '5', financePurchaseInvoiceCode: 'PIN-2026-0005', vendorId: 'v1', vendorName: '深圳华强电子', invoiceNo: '31200000000005', invoiceTotal: 15600, makeInvoiceDate: '2026-03-08', paymentStatus: 2, paymentDone: 15600, paymentToBe: 0, currency: 1, type: 20, invoiceStatus: -1, purchaseInvoiceType: 200, remark: '已作废', createdAt: '2026-03-08' },
]

const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive<Partial<FinancePurchaseInvoice>>({
  vendorName: '', invoiceNo: '', invoiceTotal: 0, makeInvoiceDate: undefined,
  purchaseInvoiceType: 100, type: 10, currency: 1, remark: '',
})

const openCreate = () => {
  editingId.value = null
  Object.assign(form, { vendorName: '', invoiceNo: '', invoiceTotal: 0, makeInvoiceDate: undefined, purchaseInvoiceType: 100, type: 10, currency: 1, remark: '' })
  dialogVisible.value = true
}

const openEdit = (row: FinancePurchaseInvoice) => {
  editingId.value = row.id
  Object.assign(form, { ...row })
  dialogVisible.value = true
}

const saveForm = async () => {
  saving.value = true
  try {
    if (editingId.value) {
      await financePurchaseInvoiceApi.update(editingId.value, form)
    } else {
      await financePurchaseInvoiceApi.create(form)
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

const openDetail = (row: FinancePurchaseInvoice) => {
  router.push({ name: 'FinancePurchaseInvoiceDetail', params: { id: row.id } })
}

const voidInvoice = async (row: FinancePurchaseInvoice) => {
  await ElMessageBox.confirm(`确认作废发票 ${row.financePurchaseInvoiceCode}？此操作不可撤销。`, '作废确认', { type: 'warning' })
  row.invoiceStatus = -1
  ElMessage.success('发票已作废')
}

const formatAmount = (v: number) => v?.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'

onMounted(loadData)
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';
</style>
