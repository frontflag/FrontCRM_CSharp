<template>
  <div class="finance-page">
    <h1 class="finance-list-page-title">{{ t('financePaymentList.pageTitle') }}</h1>
    <!-- 统计卡片（置顶） -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.monthTotal') }}</div>
        <div class="stat-value">¥ {{ formatAmount(stats.monthTotal) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.pending') }}</div>
        <div class="stat-value warning">{{ stats.pendingCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.paid') }}</div>
        <div class="stat-value success">{{ stats.paidCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePaymentList.stats.draft') }}</div>
        <div class="stat-value">{{ stats.draftCount }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financePaymentList.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        <el-select v-model="query.status" :placeholder="t('financePaymentList.filters.status')" clearable class="filter-select" @change="loadData">
          <el-option
            v-for="k in paymentStatusSelectKeys"
            :key="k"
            :label="paymentStatusLabel(k)"
            :value="k"
          />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('financePaymentList.filters.to')"
          :start-placeholder="t('financePaymentList.filters.startDate')"
          :end-placeholder="t('financePaymentList.filters.endDate')"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="date-picker"
          @change="loadData"
        />
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financePaymentList.filters.search') }}
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
        <el-tag effect="dark" :type="paymentStatusTag(row.status) as any" size="small">
          {{ paymentStatusLabel(row.status) }}
        </el-tag>
      </template>
      <template #col-paymentAmount="{ row }">
        <span class="amount-text">{{ CURRENCY_MAP[row.paymentCurrency] }} {{ formatAmount(row.paymentAmount) }}</span>
      </template>
      <template #col-paymentMode="{ row }">{{ paymentModeLabel(row.paymentMode) }}</template>
      <template #col-paymentDate="{ row }">{{ row.paymentDate ? formatDisplayDate(row.paymentDate) : '-' }}</template>
      <template #col-bankSlipNo="{ row }">{{ (row as any).bankSlipNo || '-' }}</template>
      <template #col-createdAt="{ row }">
        {{ paymentRowCreateTime(row) ? formatDisplayDateTime(paymentRowCreateTime(row)!) : '-' }}
      </template>
      <template #col-createUser="{ row }">
        {{ (row as any).createUserName || (row as any).createdBy || (row as any).paymentUserName || '-' }}
      </template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('financePaymentList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>

      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">{{ t('financePaymentList.actions.detail') }}</el-button>
            <el-button
              size="small"
              text
              type="warning"
              @click.stop="openEdit(row)"
              v-if="[1,-1,10].includes(row.status)"
            >
              {{ t('financePaymentList.actions.pay') }}
            </el-button>
            <el-button size="small" text type="warning" @click.stop="submitAudit(row)" v-if="row.status === 1">
              {{ t('financePaymentList.actions.submitAudit') }}
            </el-button>
            <el-button
              size="small"
              text
              type="danger"
              @click.stop="cancelPayment(row)"
              v-if="[1,2].includes(row.status)"
            >
              {{ t('financePaymentList.actions.cancel') }}
            </el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financePaymentList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="[1,-1,10].includes(row.status)"
                  @click.stop="openEdit(row)"
                >
                  <span class="op-more-item op-more-item--warning">{{ t('financePaymentList.actions.pay') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 1" @click.stop="submitAudit(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financePaymentList.actions.submitAudit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="[1,2].includes(row.status)" @click.stop="cancelPayment(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('financePaymentList.actions.cancel') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
      <div class="pagination-wrap">
        <div class="list-footer-left">
          <el-tooltip :content="t('financePaymentList.columnSettings')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('financePaymentList.columnSettings')" @click="dataTableRef?.openColumnSettings?.()">
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
      :title="editingId ? t('financePaymentList.dialogEdit') : t('financePaymentList.dialogCreate')"
      width="680px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formVendorId')" :required="!editingId">
              <el-input
                v-if="!editingId"
                v-model="form.vendorId"
                :placeholder="t('financePaymentList.formVendorIdPh')"
              />
              <el-input v-else :model-value="editVendorCodeDisplay" readonly />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formVendorName')" required>
              <el-input v-model="form.vendorName" :placeholder="t('financePaymentList.formVendorNamePh')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formAmount')" required>
              <el-input-number v-model="form.paymentAmount" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formMode')">
              <el-select v-model="form.paymentMode" style="width:100%">
                <el-option
                  v-for="k in paymentModeKeys"
                  :key="k"
                  :label="paymentModeLabel(k)"
                  :value="k"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formCurrency')">
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
            <el-form-item :label="t('financePaymentList.formDate')">
              <el-date-picker v-model="form.paymentDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formBankSlip')">
              <el-input v-model="form.bankSlipNo" :placeholder="t('financePaymentList.formBankSlipPh')" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item :label="t('financePaymentList.formRemark')">
              <el-input v-model="form.remark" type="textarea" :rows="2" :placeholder="t('financePaymentList.formRemarkPh')" />
            </el-form-item>
          </el-col>
          <el-col :span="24" v-if="editingId">
            <el-form-item :label="t('financePaymentList.formSlipAttach')">
              <div class="slip-attach-wrap">
                <div class="slip-upload-row">
                  <el-button size="small" type="primary" plain @click="triggerSlipFilePick">
                    {{ t('financePaymentList.slipSelectFile') }}
                  </el-button>
                  <span v-if="uploadingSlipDocs" class="slip-upload-hint">{{ t('financePaymentList.uploadingSlip') }}</span>
                  <span v-else-if="paymentDocs.length" class="slip-upload-hint slip-upload-hint--ok">
                    {{ t('financePaymentList.slipHasUploadsHint') }}
                  </span>
                  <span v-else class="slip-upload-hint slip-upload-hint--muted">
                    {{ t('financePaymentList.slipPickHint') }}
                  </span>
                  <!-- 隐藏原生 input，避免上传后清空 value 仍显示「未选择任何文件」 -->
                  <input
                    ref="slipFileInputRef"
                    type="file"
                    multiple
                    class="slip-file-input-hidden"
                    @change="onSlipFilesSelected"
                  />
                </div>
                <div v-if="paymentDocs.length" class="slip-doc-tags">
                  <el-tag
                    v-for="doc in paymentDocs"
                    :key="doc.id"
                    size="small"
                    class="slip-doc-tag"
                    @click="downloadSlipDoc(doc)"
                  >
                    {{ doc.originalFileName }}
                  </el-tag>
                </div>
                <div v-else-if="!uploadingSlipDocs" class="slip-no-docs">{{ t('financePaymentList.noSlipUploaded') }}</div>
              </div>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button
          v-if="editingId && canShowFinishButton(form as any)"
          type="success"
          @click="completePaymentInDialog"
          :loading="saving"
        >
          {{ t('financePaymentList.btnPaymentDone') }}
        </el-button>
        <el-button type="primary" @click="saveForm" :loading="saving">{{ t('financePaymentList.btnSave') }}</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
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
import { vendorApi } from '@/api/vendor'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()
const { paymentStatusLabel, paymentStatusTag, paymentModeLabel } = useFinanceEnumLabels()

const paymentStatusSelectKeys = Object.keys(PAYMENT_STATUS_MAP).map(k => Number(k))
const paymentModeKeys = Object.keys(PAYMENT_MODE_MAP).map(k => Number(k))

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

/** 后端序列化为 createTime；旧 mock/别名可能用 createdAt；PascalCase 需经 unknown 再读 */
function paymentRowCreateTime(row: FinancePayment): string | undefined {
  const ext = row as unknown as Record<string, unknown>
  const raw = row.createTime || row.createdAt || ext.CreateTime
  const s = raw != null ? String(raw).trim() : ''
  return s || undefined
}

const paymentTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'financePaymentCode', label: t('financePaymentList.columns.code'), prop: 'financePaymentCode', width: 160, minWidth: 160, fixed: 'left' },
  { key: 'status', label: t('financePaymentList.columns.status'), prop: 'status', width: 100, align: 'center' },
  { key: 'vendorName', label: t('financePaymentList.columns.vendor'), prop: 'vendorName', minWidth: 160, showOverflowTooltip: true },
  { key: 'paymentAmount', label: t('financePaymentList.columns.amount'), prop: 'paymentAmount', width: 200, minWidth: 180, align: 'right' },
  { key: 'paymentMode', label: t('financePaymentList.columns.mode'), prop: 'paymentMode', width: 110 },
  { key: 'paymentDate', label: t('financePaymentList.columns.date'), prop: 'paymentDate', width: 120 },
  { key: 'bankSlipNo', label: t('financePaymentList.columns.bankSlip'), prop: 'bankSlipNo', width: 150, showOverflowTooltip: true },
  { key: 'remark', label: t('financePaymentList.columns.remark'), prop: 'remark', minWidth: 140, showOverflowTooltip: true },
  { key: 'createdAt', label: t('financePaymentList.columns.createdAt'), prop: 'createdAt', width: 120 },
  { key: 'createUser', label: t('financePaymentList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('financePaymentList.columns.actions'),
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
  { id: '1', financePaymentCode: 'PAY-2026-0001', vendorId: 'v1', vendorCode: 'DEMO01', vendorName: '深圳华强电子有限公司', paymentAmount: 128500, paymentCurrency: 1, paymentMode: 1, paymentDate: '2026-03-15', status: 100, remark: '3月采购款', createTime: '2026-03-10T08:00:00Z', createUserName: '演示用户' },
  { id: '2', financePaymentCode: 'PAY-2026-0002', vendorId: 'v2', vendorCode: 'DEMO02', vendorName: '上海元器件贸易公司', paymentAmount: 56800, paymentCurrency: 1, paymentMode: 1, paymentDate: undefined, status: 2, remark: '', createTime: '2026-03-12T08:00:00Z', createUserName: '演示用户' },
  { id: '3', financePaymentCode: 'PAY-2026-0003', vendorId: 'v3', vendorCode: 'DEMO03', vendorName: 'Arrow Electronics', paymentAmount: 23400, paymentCurrency: 2, paymentMode: 1, paymentDate: undefined, status: 1, remark: '待提交', createTime: '2026-03-14T08:00:00Z', createUserName: '演示用户' },
  { id: '4', financePaymentCode: 'PAY-2026-0004', vendorId: 'v4', vendorCode: 'DEMO04', vendorName: '广州立创电子科技', paymentAmount: 89200, paymentCurrency: 1, paymentMode: 2, paymentDate: undefined, status: 10, remark: '', createTime: '2026-03-13T08:00:00Z', createUserName: '演示用户' },
  { id: '5', financePaymentCode: 'PAY-2026-0005', vendorId: 'v1', vendorCode: 'DEMO01', vendorName: '深圳华强电子有限公司', paymentAmount: 34600, paymentCurrency: 1, paymentMode: 3, paymentDate: undefined, status: -1, remark: '审核驳回', createTime: '2026-03-08T08:00:00Z', createUserName: '演示用户' },
]

// 弹窗
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const paymentDocs = ref<UploadDocumentDto[]>([])
const uploadingSlipDocs = ref(false)
const slipFileInputRef = ref<HTMLInputElement | null>(null)

function triggerSlipFilePick() {
  slipFileInputRef.value?.click()
}
const form = reactive<Partial<FinancePayment>>({
  vendorId: '', vendorCode: '', vendorName: '', paymentAmount: 0, paymentMode: 1, paymentCurrency: 1,
  paymentDate: undefined, bankSlipNo: '', remark: '',
})

/** 编辑弹窗「供应商编号」仅展示业务编码，不展示内部 vendorId */
const editVendorCodeDisplay = computed(() => {
  const c = (form.vendorCode || '').trim()
  return c || '—'
})

const openEdit = async (row: FinancePayment) => {
  editingId.value = row.id
  const amountForEdit =
    row.status === 100
      ? Number(row.paymentAmount ?? row.paymentAmountToBe ?? 0)
      : Number(row.paymentAmountToBe ?? row.paymentAmount ?? 0)
  Object.assign(form, { ...row, paymentAmount: amountForEdit })
  const ext = row as unknown as Record<string, unknown>
  let code = String(row.vendorCode ?? ext.VendorCode ?? '').trim()
  if (!code && row.vendorId) {
    try {
      const v = await vendorApi.getVendorById(row.vendorId)
      code = (v.code || '').trim()
    } catch {
      /* 列表未带编码时尽力补全 */
    }
  }
  form.vendorCode = code
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
    ElMessage.success(t('financePaymentList.messages.saveOk'))
    dialogVisible.value = false
    loadData()
  } catch {
    ElMessage.success(t('financePaymentList.messages.saveOkDemo'))
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
    ElMessage.warning(t('financePaymentList.messages.saveSlipFirst'))
    ;(e.target as HTMLInputElement).value = ''
    return
  }
  const files = Array.from((e.target as HTMLInputElement).files || [])
  if (!files.length) return
  try {
    uploadingSlipDocs.value = true
    await documentApi.uploadDocuments('FINANCE_PAYMENT', paymentId, files, t('financePaymentList.slipUploadCategory'))
    await loadPaymentDocs(paymentId)
    ElMessage.success(t('financePaymentList.messages.slipUploadOk'))
  } catch (err: any) {
    ElMessage.error(err?.message || t('financePaymentList.messages.slipUploadFail'))
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
  await ElMessageBox.confirm(
    t('financePaymentList.messages.submitAuditMsg', { code: row.financePaymentCode }),
    t('financePaymentList.messages.submitAuditTitle'),
    { type: 'info' }
  )
  await financePaymentApi.submit(row.id)
  ElMessage.success(t('financePaymentList.messages.submitted'))
  await loadData()
}

const completePaymentInDialog = async () => {
  if (!editingId.value) return
  const code = form.financePaymentCode || editingId.value
  await ElMessageBox.confirm(
    t('financePaymentList.messages.completeMsg', { code: String(code) }),
    t('financePaymentList.messages.completeTitle'),
    { type: 'success' }
  )
  await financePaymentApi.complete(editingId.value)
  ElMessage.success(t('financePaymentList.messages.completed'))
  dialogVisible.value = false
  await loadData()
}

const canShowFinishButton = (row: FinancePayment | Record<string, any>) => {
  return Number((row as any)?.status) === 10
}

const cancelPayment = async (row: FinancePayment) => {
  await ElMessageBox.confirm(
    t('financePaymentList.messages.cancelMsg', { code: row.financePaymentCode }),
    t('financePaymentList.messages.cancelTitle'),
    { type: 'warning' }
  )
  await financePaymentApi.cancel(row.id)
  ElMessage.success(t('financePaymentList.messages.cancelled'))
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

.slip-attach-wrap {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
}

.slip-upload-row {
  position: relative;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.slip-upload-hint {
  font-size: 13px;
  line-height: 1.4;
}

.slip-upload-hint--ok {
  color: var(--el-color-success);
}

.slip-upload-hint--muted {
  color: var(--el-text-color-secondary);
}

.slip-file-input-hidden {
  position: absolute;
  left: 0;
  top: 0;
  width: 0;
  height: 0;
  opacity: 0;
  overflow: hidden;
}

.slip-doc-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.slip-doc-tag {
  cursor: pointer;
}

.slip-no-docs {
  font-size: 13px;
  color: var(--el-text-color-placeholder);
}
</style>
