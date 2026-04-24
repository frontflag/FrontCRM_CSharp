<template>
  <div class="finance-page">
    <!-- 统计卡片（置顶） -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">{{ t('financePurchaseInvoiceList.stats.totalAmount') }}</div>
        <div class="stat-value">{{ maskPurchaseSensitiveFields ? '—' : ('¥ ' + formatAmount(stats.totalAmount)) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePurchaseInvoiceList.stats.paidAmount') }}</div>
        <div class="stat-value success">{{ maskPurchaseSensitiveFields ? '—' : ('¥ ' + formatAmount(stats.paidAmount)) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePurchaseInvoiceList.stats.toPayAmount') }}</div>
        <div class="stat-value warning">{{ maskPurchaseSensitiveFields ? '—' : ('¥ ' + formatAmount(stats.toPayAmount)) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financePurchaseInvoiceList.stats.invoicedCount') }}</div>
        <div class="stat-value">{{ stats.invoicedCount }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <el-input
          v-model="query.keyword"
          :placeholder="t('financePurchaseInvoiceList.filters.keyword')"
          clearable
          class="search-input"
          style="width:240px"
          @keyup.enter="loadData"
          @clear="loadData"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>
        <el-select v-model="query.status" :placeholder="t('financePurchaseInvoiceList.filters.invoiceStatus')" clearable class="filter-select" style="width:130px" @change="loadData">
          <el-option v-for="k in invoiceStatusSelectKeys" :key="k" :label="invoiceStatusLabel(k)" :value="k" />
        </el-select>
        <el-select v-model="filterPayStatus" :placeholder="t('financePurchaseInvoiceList.filters.payStatus')" clearable class="filter-select" style="width:120px" @change="loadData">
          <el-option v-for="k in paymentDoneSelectKeys" :key="k" :label="paymentDoneStatusLabel(k)" :value="k" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('financePurchaseInvoiceList.filters.to')"
          :start-placeholder="t('financePurchaseInvoiceList.filters.start')"
          :end-placeholder="t('financePurchaseInvoiceList.filters.end')"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          class="date-picker"
          @change="loadData"
        />
        <el-button type="primary" @click="loadData"><el-icon><Search /></el-icon> {{ t('financePurchaseInvoiceList.filters.search') }}</el-button>
      </div>
      <div class="search-right">
        <el-button type="primary" @click="openCreate">
          <el-icon><Plus /></el-icon> {{ t('financePurchaseInvoiceList.create') }}
        </el-button>
      </div>
    </div>

    <!-- 数据表格 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-purchase-invoice-list-main"
      :columns="purchaseInvoiceTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      @row-dblclick="openDetail"
      row-class-name="table-row-pointer"
    >
      <template #col-financePurchaseInvoiceCode="{ row }">
        <span class="code-text">{{ row.financePurchaseInvoiceCode }}</span>
      </template>
      <template #col-invoiceStatus="{ row }">
        <el-tag effect="dark" :type="invoiceStatusTag(row.invoiceStatus) as any" size="small">
          {{ invoiceStatusLabel(row.invoiceStatus) }}
        </el-tag>
      </template>
      <template #col-invoiceNo="{ row }">{{ row.invoiceNo || '-' }}</template>
      <template #col-vendorName="{ row }">
        <span>{{ maskPurchaseSensitiveFields ? '—' : (row.vendorName?.trim() || '—') }}</span>
      </template>
      <template #col-invoiceTotal="{ row }">
        <span v-if="maskPurchaseSensitiveFields">—</span>
        <span v-else class="amount-text">¥ {{ formatAmount(row.invoiceTotal) }}</span>
      </template>
      <template #col-paymentDone="{ row }">
        <span v-if="maskPurchaseSensitiveFields">—</span>
        <span v-else class="amount-text">¥ {{ formatAmount(row.paymentDone) }}</span>
      </template>
      <template #col-paymentStatus="{ row }">
        <el-tag effect="dark" :type="paymentDoneStatusTag(row.paymentStatus) as any" size="small">
          {{ paymentDoneStatusLabel(row.paymentStatus) }}
        </el-tag>
      </template>
      <template #col-purchaseInvoiceType="{ row }">{{ purchaseInvoiceTypeLabel(row.purchaseInvoiceType) }}</template>
      <template #col-makeInvoiceDate="{ row }">{{ row.makeInvoiceDate ? formatDisplayDate(row.makeInvoiceDate) : '-' }}</template>
      <template #col-createTime="{ row }">{{ row.createdAt ? formatDisplayDateTime(row.createdAt) : '-' }}</template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '-' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('financePurchaseInvoiceList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>

      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">{{ t('financePurchaseInvoiceList.actions.detail') }}</el-button>
            <el-button
              size="small"
              text
              type="primary"
              @click.stop="openEdit(row)"
              v-if="row.invoiceStatus === 1"
            >
              {{ t('financePurchaseInvoiceList.actions.edit') }}
            </el-button>
            <el-button
              size="small"
              text
              type="danger"
              @click.stop="voidInvoice(row)"
              v-if="row.invoiceStatus === 100"
            >
              {{ t('financePurchaseInvoiceList.actions.void') }}
            </el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financePurchaseInvoiceList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.invoiceStatus === 1" @click.stop="openEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financePurchaseInvoiceList.actions.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.invoiceStatus === 100" @click.stop="voidInvoice(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('financePurchaseInvoiceList.actions.void') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
      <div class="pagination-wrap">
        <div class="list-footer-left">
          <el-tooltip :content="t('financePurchaseInvoiceList.columnSettings')" placement="top" :hide-after="0">
            <el-button class="list-settings-btn" link type="primary" :aria-label="t('financePurchaseInvoiceList.columnSettings')" @click="dataTableRef?.openColumnSettings?.()">
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
      :title="editingId ? t('financePurchaseInvoiceList.dialogEdit') : t('financePurchaseInvoiceList.dialogCreate')"
      width="720px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formVendor')" required>
              <el-select
                v-model="form.vendorId"
                class="fpi-vendor-select"
                :placeholder="t('financePurchaseInvoiceList.formVendorPh')"
                style="width: 100%"
                filterable
                clearable
                :disabled="!!editingId"
                :filter-method="onVendorFilterInput"
                :loading="vendorSearchLoading"
                :loading-text="t('financePurchaseInvoiceList.vendorSearchLoading')"
                @change="onVendorChange"
              >
                <template #empty>
                  <div class="vendor-search-hint">{{ t('financePurchaseInvoiceList.vendorSearchHint') }}</div>
                </template>
                <el-option v-for="v in vendorOptions" :key="v.value" :label="v.label" :value="v.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formInvoiceNo')">
              <el-input v-model="form.invoiceNo" :placeholder="t('financePurchaseInvoiceList.formInvoiceNoPh')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formAmount')" required>
              <el-input-number v-model="form.invoiceTotal" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formMakeDate')">
              <el-date-picker v-model="form.makeInvoiceDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formInvoiceType')">
              <el-select v-model="form.purchaseInvoiceType" style="width:100%">
                <el-option v-for="k in purchaseInvoiceTypeKeys" :key="k" :label="purchaseInvoiceTypeLabel(k)" :value="k" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formBlueRed')">
              <el-select v-model="form.type" style="width:100%">
                <el-option v-for="k in invoiceTypeKeys" :key="k" :label="invoiceTypeLabel(k)" :value="k" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePurchaseInvoiceList.formCurrency')">
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
            <el-form-item :label="t('financePurchaseInvoiceList.formRemark')">
              <el-input v-model="form.remark" type="textarea" :rows="2" :placeholder="t('financePurchaseInvoiceList.formRemarkPh')" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" @click="saveForm" :loading="saving">{{ t('financePurchaseInvoiceList.btnSave') }}</el-button>
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
import {
  financePurchaseInvoiceApi,
  normalizeFinancePurchaseInvoice,
  INVOICE_STATUS_MAP,
  PAYMENT_DONE_STATUS_MAP,
  PURCHASE_INVOICE_TYPE_MAP,
  INVOICE_TYPE_MAP,
  type FinancePurchaseInvoice,
  type PageQuery,
} from '@/api/finance'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { vendorApi } from '@/api/vendor'
import type { Vendor } from '@/types/vendor'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const router = useRouter()
const { t } = useI18n()
const {
  invoiceStatusLabel,
  invoiceStatusTag,
  paymentDoneStatusLabel,
  paymentDoneStatusTag,
  purchaseInvoiceTypeLabel,
  invoiceTypeLabel,
} = useFinanceEnumLabels()

const invoiceStatusSelectKeys = Object.keys(INVOICE_STATUS_MAP).map(k => Number(k))
const paymentDoneSelectKeys = Object.keys(PAYMENT_DONE_STATUS_MAP).map(k => Number(k))
const purchaseInvoiceTypeKeys = Object.keys(PURCHASE_INVOICE_TYPE_MAP).map(k => Number(k))
const invoiceTypeKeys = Object.keys(INVOICE_TYPE_MAP).map(k => Number(k))

const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, keyword: '', status: undefined,
  startDate: undefined, endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const filterPayStatus = ref<number | undefined>(undefined)
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinancePurchaseInvoice[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 120
const OP_COL_EXPANDED_MIN_WIDTH = 120
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}
const purchaseInvoiceTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'invoiceStatus', label: t('financePurchaseInvoiceList.columns.invoiceStatus'), prop: 'invoiceStatus', width: 100, align: 'center' },
  { key: 'vendorName', label: t('financePurchaseInvoiceList.columns.vendor'), prop: 'vendorName', minWidth: 160, showOverflowTooltip: true },
  { key: 'invoiceNo', label: t('financePurchaseInvoiceList.columns.invoiceNo'), prop: 'invoiceNo', width: 140, showOverflowTooltip: true },
  { key: 'invoiceTotal', label: t('financePurchaseInvoiceList.columns.amount'), prop: 'invoiceTotal', width: 130, align: 'right' },
  { key: 'paymentDone', label: t('financePurchaseInvoiceList.columns.paid'), prop: 'paymentDone', width: 130, align: 'right' },
  { key: 'paymentStatus', label: t('financePurchaseInvoiceList.columns.payStatus'), prop: 'paymentStatus', width: 110, align: 'center' },
  { key: 'purchaseInvoiceType', label: t('financePurchaseInvoiceList.columns.invoiceType'), prop: 'purchaseInvoiceType', width: 140 },
  { key: 'makeInvoiceDate', label: t('financePurchaseInvoiceList.columns.makeDate'), prop: 'makeInvoiceDate', width: 120 },
  { key: 'financePurchaseInvoiceCode', label: t('financePurchaseInvoiceList.columns.code'), prop: 'financePurchaseInvoiceCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'createTime', label: t('financePurchaseInvoiceList.columns.createdAt'), width: 120 },
  { key: 'createUser', label: t('financePurchaseInvoiceList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('financePurchaseInvoiceList.columns.actions'),
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
    tableData.value = (res.items || []).map(normalizeFinancePurchaseInvoice)
    total.value = res.total || 0
  } catch {
    tableData.value = getMockData()
    total.value = tableData.value.length
  } finally {
    loading.value = false
    const safeNum = (v: number) => (Number.isFinite(v) ? v : 0)
    stats.totalAmount = tableData.value.reduce((s, r) => s + safeNum(r.invoiceTotal), 0)
    stats.paidAmount = tableData.value.reduce((s, r) => s + safeNum(r.paymentDone), 0)
    stats.toPayAmount = tableData.value.reduce((s, r) => s + safeNum(r.paymentToBe), 0)
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
  vendorId: '',
  vendorName: '',
  invoiceNo: '',
  invoiceTotal: 0,
  makeInvoiceDate: undefined,
  purchaseInvoiceType: 100,
  type: 10,
  currency: 1,
  remark: ''
})

const vendorOptions = ref<{ value: string; label: string }[]>([])
const vendorSearchLoading = ref(false)
let vendorSearchTimer: ReturnType<typeof setTimeout> | null = null

function onVendorFilterInput(query: string) {
  if (vendorSearchTimer) clearTimeout(vendorSearchTimer)
  if (!query || query.trim().length < 1) {
    if (form.vendorId && form.vendorName) {
      vendorOptions.value = [{ value: String(form.vendorId), label: String(form.vendorName) }]
    } else {
      vendorOptions.value = []
    }
    return
  }
  vendorSearchTimer = setTimeout(async () => {
    vendorSearchLoading.value = true
    try {
      const res = await vendorApi.searchVendors({
        pageNumber: 1,
        pageSize: 30,
        keyword: query.trim()
      })
      vendorOptions.value = (res.items || []).map((v: Vendor) => ({
        value: v.id,
        label: v.officialName || v.nickName || v.code || '—'
      }))
    } catch {
      vendorOptions.value = []
    } finally {
      vendorSearchLoading.value = false
    }
  }, 300)
}

function onVendorChange(val: string | null | undefined) {
  if (!val) {
    form.vendorName = ''
    return
  }
  const found = vendorOptions.value.find((x) => x.value === val)
  if (found) form.vendorName = found.label
}

const openCreate = () => {
  editingId.value = null
  vendorOptions.value = []
  Object.assign(form, {
    vendorId: '',
    vendorName: '',
    invoiceNo: '',
    invoiceTotal: 0,
    makeInvoiceDate: undefined,
    purchaseInvoiceType: 100,
    type: 10,
    currency: 1,
    remark: ''
  })
  dialogVisible.value = true
}

const openEdit = (row: FinancePurchaseInvoice) => {
  editingId.value = row.id
  Object.assign(form, { ...row })
  const vid = String(row.vendorId ?? '').trim()
  const vname = String(row.vendorName ?? '').trim()
  if (vid) {
    vendorOptions.value = [{ value: vid, label: vname || vid }]
  } else {
    vendorOptions.value = []
  }
  dialogVisible.value = true
}

const saveForm = async () => {
  if (!editingId.value && !String(form.vendorId ?? '').trim()) {
    ElMessage.warning(t('financePurchaseInvoiceList.validation.vendorRequired'))
    return
  }
  saving.value = true
  try {
    if (editingId.value) {
      await financePurchaseInvoiceApi.update(editingId.value, {
        invoiceNo: form.invoiceNo,
        invoiceAmount: form.invoiceTotal,
        invoiceDate: form.makeInvoiceDate,
        remark: form.remark
      } as Partial<FinancePurchaseInvoice>)
    } else {
      const amt = Number(form.invoiceTotal) || 0
      await financePurchaseInvoiceApi.create({
        vendorId: String(form.vendorId).trim(),
        vendorName: form.vendorName || undefined,
        invoiceNo: form.invoiceNo || undefined,
        invoiceAmount: amt,
        billAmount: amt,
        taxAmount: 0,
        excludTaxAmount: amt,
        invoiceDate: form.makeInvoiceDate || undefined,
        remark: form.remark || undefined,
        items: []
      } as unknown as Partial<FinancePurchaseInvoice>)
    }
    ElMessage.success(t('financePurchaseInvoiceList.messages.saveOk'))
    dialogVisible.value = false
    loadData()
  } catch {
    ElMessage.success(t('financePurchaseInvoiceList.messages.saveOkDemo'))
    dialogVisible.value = false
  } finally {
    saving.value = false
  }
}

const openDetail = (row: FinancePurchaseInvoice) => {
  router.push({ name: 'FinancePurchaseInvoiceDetail', params: { id: row.id } })
}

const voidInvoice = async (row: FinancePurchaseInvoice) => {
  await ElMessageBox.confirm(
    t('financePurchaseInvoiceList.messages.voidMsg', { code: row.financePurchaseInvoiceCode }),
    t('financePurchaseInvoiceList.messages.voidTitle'),
    { type: 'warning' }
  )
  await financePurchaseInvoiceApi.redInvoice(row.id)
  ElMessage.success(t('financePurchaseInvoiceList.messages.voided'))
  await loadData()
}

const formatAmount = (v: number | unknown) => {
  const n = Number(v)
  if (!Number.isFinite(n)) return '0.00'
  return n.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

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

.vendor-search-hint {
  padding: 8px 12px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  text-align: center;
}
</style>
