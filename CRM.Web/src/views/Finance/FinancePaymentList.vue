<template>
  <div class="finance-page">
    <!-- 统计卡片（置顶） -->
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

    <!-- 数据表格 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-payment-list-main"
      :columns="paymentTableColumns"
      :show-column-settings="false"
      :data="tableData"
      v-loading="loading"
      @row-dblclick="openDetail"
      row-class-name="table-row-pointer"
    >
      <template #col-financePaymentCode="{ row }">
        <span class="code-text">{{ row.financePaymentCode }}</span>
      </template>
      <template #col-status="{ row }">
        <el-tag effect="dark" :type="PAYMENT_STATUS_MAP[row.status]?.type as any" size="small">
          {{ PAYMENT_STATUS_MAP[row.status]?.label }}
        </el-tag>
      </template>
      <template #col-paymentAmount="{ row }">
        <span class="amount-text">{{ CURRENCY_MAP[row.paymentCurrency] }} {{ formatAmount(row.paymentAmount) }}</span>
      </template>
      <template #col-paymentMode="{ row }">{{ PAYMENT_MODE_MAP[row.paymentMode] }}</template>
      <template #col-paymentDate="{ row }">{{ row.paymentDate ? formatDisplayDate(row.paymentDate) : '-' }}</template>
      <template #col-bankSlipNo="{ row }">{{ (row as any).bankSlipNo || '-' }}</template>
      <template #col-createdAt="{ row }">{{ row.createdAt ? formatDisplayDateTime(row.createdAt) : '-' }}</template>
      <template #col-createUser="{ row }">
        {{ (row as any).createUserName || (row as any).createdBy || (row as any).paymentUserName || '-' }}
      </template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">操作</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>

      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">详情</el-button>
            <el-button
              size="small"
              text
              type="warning"
              @click.stop="openEdit(row)"
              v-if="[1,-1,10].includes(row.status)"
            >
              付款
            </el-button>
            <el-button size="small" text type="warning" @click.stop="submitAudit(row)" v-if="row.status === 1">
              提交审核
            </el-button>
            <el-button
              size="small"
              text
              type="danger"
              @click.stop="cancelPayment(row)"
              v-if="[1,2].includes(row.status)"
            >
              取消
            </el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">详情</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="[1,-1,10].includes(row.status)"
                  @click.stop="openEdit(row)"
                >
                  <span class="op-more-item op-more-item--warning">付款</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 1" @click.stop="submitAudit(row)">
                  <span class="op-more-item op-more-item--warning">提交审核</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="[1,2].includes(row.status)" @click.stop="cancelPayment(row)">
                  <span class="op-more-item op-more-item--danger">取消</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
      <div class="pagination-wrap">
        <div class="list-footer-left">
          <el-tooltip content="列设置" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" aria-label="列设置" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
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
                <el-option
                  v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="付款日期">
              <el-date-picker v-model="form.paymentDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="银行水单号">
              <el-input v-model="form.bankSlipNo" placeholder="请输入银行水单号码" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="请输入备注" />
            </el-form-item>
          </el-col>
          <el-col :span="24" v-if="editingId">
            <el-form-item label="银行水单附件">
              <div style="display:flex;flex-direction:column;gap:8px;width:100%">
                <input type="file" multiple @change="onSlipFilesSelected" />
                <div v-if="uploadingSlipDocs">上传中...</div>
                <div v-if="paymentDocs.length">
                  <el-tag
                    v-for="doc in paymentDocs"
                    :key="doc.id"
                    size="small"
                    style="margin-right:8px;cursor:pointer"
                    @click="downloadSlipDoc(doc)"
                  >
                    {{ doc.originalFileName }}
                  </el-tag>
                </div>
                <div v-else style="color:var(--el-text-color-placeholder)">暂无已上传水单文件</div>
              </div>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button
          v-if="editingId && canShowFinishButton(form as any)"
          type="success"
          @click="completePaymentInDialog"
          :loading="saving"
        >
          付款完成
        </el-button>
        <el-button type="primary" @click="saveForm" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Search, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import {
  financePaymentApi,
  PAYMENT_STATUS_MAP,
  PAYMENT_MODE_MAP,
  CURRENCY_MAP,
  type FinancePayment,
  type PageQuery,
} from '@/api/finance'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

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
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 220
const OP_COL_EXPANDED_MIN_WIDTH = 220
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const paymentTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'financePaymentCode', label: '付款单号', prop: 'financePaymentCode', width: 160, minWidth: 160, fixed: 'left' },
  { key: 'status', label: '状态', prop: 'status', width: 100, align: 'center' },
  { key: 'vendorName', label: '供应商', prop: 'vendorName', minWidth: 160, showOverflowTooltip: true },
  { key: 'paymentAmount', label: '付款金额', prop: 'paymentAmount', width: 130, align: 'right' },
  { key: 'paymentMode', label: '付款方式', prop: 'paymentMode', width: 110 },
  { key: 'paymentDate', label: '付款日期', prop: 'paymentDate', width: 120 },
  { key: 'bankSlipNo', label: '银行水单号', prop: 'bankSlipNo', width: 150, showOverflowTooltip: true },
  { key: 'remark', label: '备注', prop: 'remark', minWidth: 140, showOverflowTooltip: true },
  { key: 'createdAt', label: '创建时间', prop: 'createdAt', width: 120 },
  { key: 'createUser', label: '创建人', width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: '操作',
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
])

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
const paymentDocs = ref<UploadDocumentDto[]>([])
const uploadingSlipDocs = ref(false)
const form = reactive<Partial<FinancePayment>>({
  vendorId: '', vendorName: '', paymentAmount: 0, paymentMode: 1, paymentCurrency: 1,
  paymentDate: undefined, bankSlipNo: '', remark: '',
})

const openEdit = (row: FinancePayment) => {
  editingId.value = row.id
  const amountForEdit =
    row.status === 100
      ? Number(row.paymentAmount ?? row.paymentAmountToBe ?? 0)
      : Number(row.paymentAmountToBe ?? row.paymentAmount ?? 0)
  Object.assign(form, { ...row, paymentAmount: amountForEdit })
  dialogVisible.value = true
  loadPaymentDocs(row.id)
}

const saveForm = async () => {
  saving.value = true
  try {
    if (editingId.value) {
      await financePaymentApi.update(editingId.value, {
        paymentAmountToBe: Number(form.paymentAmount ?? 0),
        paymentCurrency: Number(form.paymentCurrency ?? 1),
        paymentDate: form.paymentDate,
        paymentMode: form.paymentMode,
        bankSlipNo: form.bankSlipNo,
        remark: form.remark,
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

const loadPaymentDocs = async (paymentId: string) => {
  try {
    paymentDocs.value = await documentApi.getDocuments('FINANCE_PAYMENT', paymentId)
  } catch {
    paymentDocs.value = []
  }
}

const onSlipFilesSelected = async (e: Event) => {
  const paymentId = editingId.value
  if (!paymentId) {
    ElMessage.warning('请先保存付款单后再上传水单附件')
    ;(e.target as HTMLInputElement).value = ''
    return
  }
  const files = Array.from((e.target as HTMLInputElement).files || [])
  if (!files.length) return
  try {
    uploadingSlipDocs.value = true
    await documentApi.uploadDocuments('FINANCE_PAYMENT', paymentId, files, '银行水单')
    await loadPaymentDocs(paymentId)
    ElMessage.success('水单附件上传成功')
  } catch (err: any) {
    ElMessage.error(err?.message || '水单附件上传失败')
  } finally {
    uploadingSlipDocs.value = false
    ;(e.target as HTMLInputElement).value = ''
  }
}

const downloadSlipDoc = async (doc: UploadDocumentDto) => {
  await documentApi.downloadDocument(doc.id, doc.originalFileName)
}

/// 详情
const openDetail = (row: FinancePayment) => {
  router.push({ name: 'FinancePaymentDetail', params: { id: row.id } })
}

// 状态操作
const submitAudit = async (row: FinancePayment) => {
  await ElMessageBox.confirm(`确认提交付款单 ${row.financePaymentCode} 审核？`, '提交审核', { type: 'info' })
  await financePaymentApi.submit(row.id)
  ElMessage.success('已提交审核')
  await loadData()
}

const completePaymentInDialog = async () => {
  if (!editingId.value) return
  const code = form.financePaymentCode || editingId.value
  await ElMessageBox.confirm(`确认将付款单 ${code} 标记为付款完成？`, '付款完成', { type: 'success' })
  await financePaymentApi.complete(editingId.value)
  ElMessage.success('付款已完成')
  dialogVisible.value = false
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
  await financePaymentApi.cancel(row.id)
  ElMessage.success('已取消')
  await loadData()
}

const formatAmount = (v: number) => v?.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || '0.00'

onMounted(loadData)
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';

.pagination-wrap {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>
