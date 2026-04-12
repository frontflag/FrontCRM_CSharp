<template>
  <div class="finance-page">
    <h1 class="finance-list-page-title">{{ t('financeReceiptList.pageTitle') }}</h1>
    <!-- 统计卡片（置顶：在筛选栏与表格之上） -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">{{ t('financeReceiptList.stats.monthTotal') }}</div>
        <div class="stat-value success">¥ {{ formatAmount(stats.monthTotal) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financeReceiptList.stats.pending') }}</div>
        <div class="stat-value warning">{{ stats.pendingCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financeReceiptList.stats.received') }}</div>
        <div class="stat-value success">{{ stats.receivedCount }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financeReceiptList.stats.draft') }}</div>
        <div class="stat-value">{{ stats.draftCount }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financeReceiptList.filters.keyword')"
          clearable
          class="search-input"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select v-model="query.status" :placeholder="t('financeReceiptList.filters.status')" clearable class="filter-select" @change="loadData">
          <el-option
            v-for="k in receiptStatusSelectKeys"
            :key="k"
            :label="receiptStatusLabel(k)"
            :value="k"
          />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('financeReceiptList.filters.to')"
          :start-placeholder="t('financeReceiptList.filters.startDate')"
          :end-placeholder="t('financeReceiptList.filters.endDate')"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="date-picker"
          @change="loadData"
        />
        <el-button type="primary" @click="loadData">
          <el-icon><Search /></el-icon> {{ t('financeReceiptList.filters.search') }}
        </el-button>
      </div>
      <div class="search-right">
        <el-button type="primary" @click="openCreate">
          <el-icon><Plus /></el-icon> {{ t('financeReceiptList.create') }}
        </el-button>
      </div>
    </div>

    <!-- 数据表格 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-receipt-list-main"
      :columns="receiptTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      @row-dblclick="openDetail"
      row-class-name="table-row-pointer"
    >
      <template #col-financeReceiptCode="{ row }">
        <span class="code-text">{{ row.financeReceiptCode }}</span>
      </template>
      <template #col-status="{ row }">
        <el-tag effect="dark" :type="receiptStatusTag(row.status) as any" size="small">
          {{ receiptStatusLabel(row.status) }}
        </el-tag>
      </template>
      <template #col-receiptAmount="{ row }">
        <span class="amount-text">{{ CURRENCY_MAP[row.receiptCurrency] }} {{ formatAmount(row.receiptAmount) }}</span>
      </template>
      <template #col-receiptMode="{ row }">{{ paymentModeLabel(row.receiptMode) }}</template>
      <template #col-receiptDate="{ row }">{{ row.receiptDate ? formatDisplayDate(row.receiptDate) : '-' }}</template>
      <template #col-bankSlipNo="{ row }">{{ (row as any).bankSlipNo || '-' }}</template>
      <template #col-createdAt="{ row }">
        {{ receiptRowCreatedAt(row) ? formatDisplayDateTime(receiptRowCreatedAt(row)!) : '-' }}
      </template>
      <template #col-createUser="{ row }">
        {{
          row.createUserName ||
          (row as any).createUserName ||
          (row as any).createdBy ||
          (row as any).receiptUserName ||
          '-'
        }}
      </template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('financeReceiptList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">{{ t('financeReceiptList.actions.detail') }}</el-button>
            <el-button size="small" text type="primary" @click.stop="openEdit(row)" v-if="row.status === 0">{{ t('financeReceiptList.actions.edit') }}</el-button>
            <el-button size="small" text type="warning" @click.stop="submitAudit(row)" v-if="row.status === 0">{{ t('financeReceiptList.actions.submitAudit') }}</el-button>
            <el-button size="small" text type="warning" @click.stop="approveReceipt(row)" v-if="row.status === 1">{{ t('financeReceiptList.actions.approve') }}</el-button>
            <el-button size="small" text type="danger" @click.stop="cancelReceipt(row)" v-if="[0,1].includes(row.status)">{{ t('financeReceiptList.actions.cancel') }}</el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeReceiptList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 0" @click.stop="openEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeReceiptList.actions.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 0" @click.stop="submitAudit(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financeReceiptList.actions.submitAudit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 1" @click.stop="approveReceipt(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financeReceiptList.actions.approve') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="[0,1].includes(row.status)" @click.stop="cancelReceipt(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('financeReceiptList.actions.cancel') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
      <div class="pagination-wrap">
        <div class="list-footer-left">
          <el-tooltip :content="t('financeReceiptList.columnSettings')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('financeReceiptList.columnSettings')" @click="dataTableRef?.openColumnSettings?.()">
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
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
      :title="editingId ? t('financeReceiptList.dialogEdit') : t('financeReceiptList.dialogCreate')"
      width="min(96vw, 1020px)"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('financeReceiptList.formCustomer')" required>
              <el-select
                v-model="form.customerId"
                :placeholder="t('financeReceiptList.customerPh')"
                style="width: 100%"
                filterable
                clearable
                :filter-method="onCustomerFilterInput"
                :loading="customerSearchLoading"
                :loading-text="t('financeReceiptList.customerSearchLoading')"
                @change="onCustomerChange"
              >
                <template #empty>
                  <div class="select-hint">{{ t('financeReceiptList.customerEmptyHint') }}</div>
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
            <el-form-item :label="t('financeReceiptList.formAmount')" required>
              <el-input-number v-model="form.receiptAmount" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeReceiptList.formMode')">
              <el-select v-model="form.receiptMode" style="width:100%">
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
            <el-form-item :label="t('financeReceiptList.formCurrency')">
              <el-select v-model="form.receiptCurrency" style="width:100%">
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
            <el-form-item :label="t('financeReceiptList.formDate')">
              <el-date-picker v-model="form.receiptDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeReceiptList.formBankSlip')">
              <el-input v-model="form.bankSlipNo" :placeholder="t('financeReceiptList.formBankSlipPh')" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item :label="t('financeReceiptList.formRemark')">
              <el-input v-model="form.remark" type="textarea" :rows="2" :placeholder="t('financeReceiptList.formRemarkPh')" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item :label="t('financeReceiptList.formSlipAttach')">
              <div class="slip-attach-wrap">
                <div class="slip-upload-row">
                  <el-button size="small" type="primary" plain @click="triggerSlipFilePick">
                    {{ t('financeReceiptList.slipSelectFile') }}
                  </el-button>
                  <span v-if="uploadingSlipDocs" class="slip-upload-hint">{{ t('financeReceiptList.uploadingSlip') }}</span>
                  <span v-else-if="editingId && receiptDocs.length" class="slip-upload-hint slip-upload-hint--ok">
                    {{ t('financeReceiptList.slipHasUploadsHint') }}
                  </span>
                  <span v-else-if="!editingId && pendingSlipFiles.length" class="slip-upload-hint slip-upload-hint--ok">
                    {{ t('financeReceiptList.slipPendingUploadHint') }}
                  </span>
                  <span v-else class="slip-upload-hint slip-upload-hint--muted">
                    {{ editingId ? t('financeReceiptList.slipPickHint') : t('financeReceiptList.slipPickHintCreate') }}
                  </span>
                  <input
                    ref="slipFileInputRef"
                    type="file"
                    multiple
                    class="slip-file-input-hidden"
                    @change="onSlipFilesSelected"
                  />
                </div>
                <div v-if="!editingId && pendingSlipFiles.length" class="slip-doc-tags">
                  <el-tag
                    v-for="(pf, idx) in pendingSlipFiles"
                    :key="`${pf.name}-${pf.size}-${idx}`"
                    size="small"
                    class="slip-doc-tag"
                    closable
                    @close="removePendingSlip(idx)"
                  >
                    {{ pf.name }}
                  </el-tag>
                </div>
                <div v-if="editingId && receiptDocs.length" class="slip-doc-tags">
                  <el-tag
                    v-for="doc in receiptDocs"
                    :key="doc.id"
                    size="small"
                    class="slip-doc-tag"
                    @click="downloadSlipDoc(doc)"
                  >
                    {{ doc.originalFileName }}
                  </el-tag>
                </div>
                <div
                  v-else-if="editingId && !uploadingSlipDocs && !receiptDocs.length"
                  class="slip-no-docs"
                >
                  {{ t('financeReceiptList.noSlipUploaded') }}
                </div>
              </div>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" @click="saveForm" :loading="saving">{{ t('financeReceiptList.btnSave') }}</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { Search, Plus, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import {
  financeReceiptApi,
  RECEIPT_STATUS_MAP,
  PAYMENT_MODE_MAP,
  CURRENCY_MAP,
  type FinanceReceipt,
  type PageQuery,
} from '@/api/finance'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { customerApi } from '@/api/customer'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()
const { receiptStatusLabel, receiptStatusTag, paymentModeLabel } = useFinanceEnumLabels()

const receiptStatusSelectKeys = Object.keys(RECEIPT_STATUS_MAP).map(k => Number(k))
const paymentModeKeys = Object.keys(PAYMENT_MODE_MAP).map(k => Number(k))

/** 后端序列化为 createTime，前端列使用 createdAt */
function receiptRowCreatedAt(row: FinanceReceipt): string | undefined {
  const r = row as FinanceReceipt & { createTime?: string }
  const v = r.createdAt ?? r.createTime
  return v != null && String(v).trim() !== '' ? String(v) : undefined
}

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
        label: c.customerName || (c as { officialName?: string }).officialName || t('financeReceiptList.unknownCustomer'),
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
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinanceReceipt[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 160
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const receiptTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'status', label: t('financeReceiptList.columns.status'), prop: 'status', width: 100, align: 'center' },
  { key: 'customerName', label: t('financeReceiptList.columns.customer'), prop: 'customerName', minWidth: 160, showOverflowTooltip: true },
  { key: 'receiptAmount', label: t('financeReceiptList.columns.amount'), prop: 'receiptAmount', width: 140, align: 'right' },
  { key: 'receiptMode', label: t('financeReceiptList.columns.mode'), prop: 'receiptMode', width: 110 },
  { key: 'receiptDate', label: t('financeReceiptList.columns.date'), prop: 'receiptDate', width: 120 },
  { key: 'bankSlipNo', label: t('financeReceiptList.columns.bankSlip'), prop: 'bankSlipNo', width: 140, showOverflowTooltip: true },
  { key: 'remark', label: t('financeReceiptList.columns.remark'), prop: 'remark', minWidth: 140, showOverflowTooltip: true },
  { key: 'financeReceiptCode', label: t('financeReceiptList.columns.code'), prop: 'financeReceiptCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'createdAt', label: t('financeReceiptList.columns.createdAt'), prop: 'createdAt', width: 120 },
  { key: 'createUser', label: t('financeReceiptList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('financeReceiptList.columns.actions'),
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
    const items = res.items || []
    tableData.value = items.map((row: FinanceReceipt & { createTime?: string }) => ({
      ...row,
      createdAt: row.createdAt ?? row.createTime,
    }))
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

const RECEIPT_DOC_BIZ = 'FINANCE_RECEIPT'

const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const receiptDocs = ref<UploadDocumentDto[]>([])
const pendingSlipFiles = ref<File[]>([])
const uploadingSlipDocs = ref(false)
const slipFileInputRef = ref<HTMLInputElement | null>(null)

function triggerSlipFilePick() {
  slipFileInputRef.value?.click()
}

const form = reactive<Partial<FinanceReceipt>>({
  customerId: '',
  customerName: '',
  receiptAmount: 0,
  receiptMode: 1,
  receiptCurrency: 1,
  receiptDate: undefined,
  bankSlipNo: '',
  remark: '',
})

const openCreate = () => {
  editingId.value = null
  customerOptions.value = []
  receiptDocs.value = []
  pendingSlipFiles.value = []
  Object.assign(form, {
    customerId: '',
    customerName: '',
    receiptAmount: 0,
    receiptMode: 1,
    receiptCurrency: 1,
    receiptDate: undefined,
    bankSlipNo: '',
    remark: '',
  })
  dialogVisible.value = true
}

const openEdit = (row: FinanceReceipt) => {
  editingId.value = row.id
  pendingSlipFiles.value = []
  Object.assign(form, { ...row })
  customerOptions.value = row.customerId
    ? [{ value: row.customerId, label: row.customerName || t('financeReceiptList.customerFallback') }]
    : []
  dialogVisible.value = true
  void loadReceiptDocs(row.id)
}

const loadReceiptDocs = async (receiptId: string) => {
  try {
    receiptDocs.value = await documentApi.getDocuments(RECEIPT_DOC_BIZ, receiptId)
  } catch {
    receiptDocs.value = []
  }
}

const removePendingSlip = (idx: number) => {
  pendingSlipFiles.value.splice(idx, 1)
}

const onSlipFilesSelected = async (e: Event) => {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  if (!files.length) return

  const receiptId = editingId.value
  if (receiptId) {
    try {
      uploadingSlipDocs.value = true
      await documentApi.uploadDocuments(
        RECEIPT_DOC_BIZ,
        receiptId,
        files,
        t('financeReceiptList.slipUploadCategory')
      )
      await loadReceiptDocs(receiptId)
      ElMessage.success(t('financeReceiptList.messages.slipUploadOk'))
    } catch (err: unknown) {
      const msg = err && typeof err === 'object' && 'message' in err ? String((err as { message?: string }).message) : ''
      ElMessage.error(msg || t('financeReceiptList.messages.slipUploadFail'))
    } finally {
      uploadingSlipDocs.value = false
    }
    return
  }

  for (const f of files) {
    const dup = pendingSlipFiles.value.some((x) => x.name === f.name && x.size === f.size)
    if (!dup) pendingSlipFiles.value.push(f)
  }
}

const downloadSlipDoc = async (doc: UploadDocumentDto) => {
  await documentApi.downloadDocument(doc.id, doc.originalFileName)
}

const saveForm = async () => {
  if (!form.customerId?.trim()) {
    ElMessage.warning(t('financeReceiptList.messages.selectCustomer'))
    return
  }
  saving.value = true
  try {
    if (editingId.value) {
      await financeReceiptApi.update(editingId.value, {
        customerId: form.customerId,
        customerName: form.customerName,
        receiptAmount: Number(form.receiptAmount ?? 0),
        receiptCurrency: Number(form.receiptCurrency ?? 1),
        receiptDate: form.receiptDate,
        receiptMode: form.receiptMode,
        bankSlipNo: form.bankSlipNo,
        remark: form.remark,
      })
    } else {
      const created = await financeReceiptApi.create({
        ...form,
        items: [],
      })
      const newId = created?.id?.trim()
      if (newId && pendingSlipFiles.value.length) {
        uploadingSlipDocs.value = true
        try {
          await documentApi.uploadDocuments(
            RECEIPT_DOC_BIZ,
            newId,
            [...pendingSlipFiles.value],
            t('financeReceiptList.slipUploadCategory')
          )
          pendingSlipFiles.value = []
        } catch (err: unknown) {
          const msg = err && typeof err === 'object' && 'message' in err ? String((err as { message?: string }).message) : ''
          ElMessage.error(msg || t('financeReceiptList.messages.slipUploadFail'))
        } finally {
          uploadingSlipDocs.value = false
        }
      }
    }
    ElMessage.success(t('financeReceiptList.messages.saveOk'))
    dialogVisible.value = false
    loadData()
  } catch {
    ElMessage.success(t('financeReceiptList.messages.saveOkDemo'))
    dialogVisible.value = false
  } finally {
    saving.value = false
  }
}

const openDetail = (row: FinanceReceipt) => {
  router.push({ name: 'FinanceReceiptDetail', params: { id: row.id } })
}

const submitAudit = async (row: FinanceReceipt) => {
  await ElMessageBox.confirm(
    t('financeReceiptList.messages.submitMsg', { code: row.financeReceiptCode }),
    t('financeReceiptList.messages.submitTitle'),
    { type: 'info' }
  )
  await financeReceiptApi.submit(row.id)
  ElMessage.success(t('financeReceiptList.messages.submitted'))
  await loadData()
}

const approveReceipt = async (row: FinanceReceipt) => {
  await ElMessageBox.confirm(
    t('financeReceiptList.messages.approveMsg'),
    t('financeReceiptList.messages.approveTitle'),
    { type: 'success' }
  )
  await financeReceiptApi.approve(row.id)
  await financeReceiptApi.confirmReceived(row.id)
  ElMessage.success(t('financeReceiptList.messages.approved'))
  await loadData()
}

const cancelReceipt = async (row: FinanceReceipt) => {
  await ElMessageBox.confirm(
    t('financeReceiptList.messages.cancelMsg', { code: row.financeReceiptCode }),
    t('financeReceiptList.messages.cancelTitle'),
    { type: 'warning' }
  )
  await financeReceiptApi.cancel(row.id)
  ElMessage.success(t('financeReceiptList.messages.cancelled'))
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

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
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
