<template>
  <div class="pending-approvals-page">
    <div class="page-header">
      <h2 class="page-title">{{ t('pendingApprovals.title') }}</h2>
    </div>

    <div class="stats-row">
      <div class="stat-card stat-card--pending">
        <div class="stat-label">{{ t('pendingApprovals.stats.pending') }}</div>
        <div class="stat-value">{{ pendingCount }}</div>
      </div>
      <div class="stat-card stat-card--approved">
        <div class="stat-label">{{ t('pendingApprovals.stats.approved') }}</div>
        <div class="stat-value">{{ approvedCount }}</div>
      </div>
      <div class="stat-card stat-card--rejected">
        <div class="stat-label">{{ t('pendingApprovals.stats.rejected') }}</div>
        <div class="stat-value">{{ rejectedCount }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="search-label">{{ t('pendingApprovals.filters.bizType') }}</span>
        <el-select
          v-model="searchForm.bizType"
          :placeholder="t('pendingApprovals.filters.all')"
          clearable
          style="width: 200px"
          @change="handleSearch"
        >
          <el-option :label="t('pendingApprovals.bizType.CUSTOMER')" value="CUSTOMER" />
          <el-option :label="t('pendingApprovals.bizType.VENDOR')" value="VENDOR" />
          <el-option :label="t('pendingApprovals.bizType.SALES_ORDER')" value="SALES_ORDER" />
          <el-option :label="t('pendingApprovals.bizType.PURCHASE_ORDER')" value="PURCHASE_ORDER" />
          <el-option :label="t('pendingApprovals.bizType.FINANCE_PAYMENT')" value="FINANCE_PAYMENT" />
          <el-option :label="t('pendingApprovals.bizType.FINANCE_RECEIPT')" value="FINANCE_RECEIPT" />
        </el-select>
      </div>
    </div>

    <div class="segment-row">
      <button class="segment-item" :class="{ 'is-active': activeState === 'pending' }" @click="switchState('pending')">{{ t('pendingApprovals.segment.pending', { count: pendingCount }) }}</button>
      <button class="segment-item" :class="{ 'is-active': activeState === 'approved' }" @click="switchState('approved')">{{ t('pendingApprovals.segment.approved', { count: approvedCount }) }}</button>
      <button class="segment-item" :class="{ 'is-active': activeState === 'rejected' }" @click="switchState('rejected')">{{ t('pendingApprovals.segment.rejected', { count: rejectedCount }) }}</button>
    </div>

    <CrmDataTable :data="approvalList" v-loading="loading" highlight-current-row @row-dblclick="handleView">
      <el-table-column :label="t('pendingApprovals.columns.bizType')" width="100">
        <template #default="{ row }">
          <el-tag effect="dark" :type="getBizTypeTagType(row.bizType)" size="small">
            {{ row.bizTypeName || getBizTypeText(row.bizType) }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="documentCode" :label="t('pendingApprovals.columns.documentCode')" width="160" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="code-link" @click="handleView(row)">{{ row.documentCode }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="counterpartyName" :label="t('pendingApprovals.columns.counterparty')" width="200" min-width="200" show-overflow-tooltip />
      <el-table-column :label="t('pendingApprovals.columns.description')" show-overflow-tooltip>
        <template #default="{ row }">
          <span>{{ buildItemDescription(row) }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="amount" :label="t('pendingApprovals.columns.amount')" width="160" align="right">
        <template #default="{ row }">
          <span class="amount-text" v-if="row.amount != null">
            {{ formatAmount(row.amount, row.currency) }}
          </span>
          <span class="text-muted" v-else>—</span>
        </template>
      </el-table-column>
      <el-table-column prop="createdAt" :label="t('pendingApprovals.columns.submittedAt')" width="160">
        <template #default="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column prop="submitter" :label="t('pendingApprovals.columns.submitter')" width="100" show-overflow-tooltip>
        <template #default="{ row }">
          {{ row.submitter || '—' }}
        </template>
      </el-table-column>
      <el-table-column :label="t('pendingApprovals.columns.actions')" width="180" fixed="right" class-name="op-col" label-class-name="op-col">
        <template #default="{ row }">
          <div @click.stop @dblclick.stop>
            <div class="action-btns">
              <template v-if="activeState === 'pending'">
                <el-button link type="primary" size="small" @click.stop="openAuditDialog(row)">
                  {{ t('pendingApprovals.actions.audit') }}
                </el-button>
              </template>
              <el-button v-else link type="primary" size="small" @click.stop="handleView(row)">{{ t('pendingApprovals.actions.detail') }}</el-button>
            </div>
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

    <el-dialog v-model="auditDialogVisible" :title="t('pendingApprovals.dialog.title')" width="980px" destroy-on-close>
      <div v-if="auditRow" class="audit-dialog">
        <div class="audit-top">
          <div class="audit-top-head">
            <div class="audit-doc">{{ auditRow.documentCode }}</div>
            <el-tag size="small" :type="getBizTypeTagType(auditRow.bizType)">
              {{ auditRow.bizTypeName || getBizTypeText(auditRow.bizType) }}
            </el-tag>
          </div>
          <el-form label-width="88px">
            <el-form-item :label="t('pendingApprovals.dialog.submitRemark')">
              <div class="submit-remark">{{ getSubmitRemark() }}</div>
            </el-form-item>
            <el-form-item :label="t('pendingApprovals.dialog.auditRemark')">
              <el-input
                v-model="auditRemark"
                type="textarea"
                :rows="3"
                :placeholder="t('pendingApprovals.dialog.auditRemarkPlaceholder')"
              />
            </el-form-item>
          </el-form>
          <div class="audit-attachments">
            <div class="attach-header">
              <span>{{ t('pendingApprovals.dialog.attachmentPreview') }}</span>
              <label class="upload-btn">
                <input type="file" multiple @change="onAuditFilesSelected" />
                {{ t('pendingApprovals.dialog.uploadAttachment') }}
              </label>
            </div>
            <div v-if="auditDocsLoading" class="detail-loading">{{ t('pendingApprovals.dialog.docsLoading') }}</div>
            <div v-else-if="auditDocs.length === 0" class="detail-loading">{{ t('pendingApprovals.dialog.noAttachments') }}</div>
            <div v-else class="attach-list">
              <div class="attach-item" v-for="doc in auditDocs" :key="doc.id">
                <span class="name" :title="doc.originalFileName">{{ doc.originalFileName }}</span>
                <span class="ops">
                  <el-button link type="primary" size="small" @click="previewDoc(doc)">{{ t('pendingApprovals.dialog.preview') }}</el-button>
                  <el-button link type="primary" size="small" @click="downloadDoc(doc)" :loading="uploadingAuditDocs">{{ t('pendingApprovals.dialog.download') }}</el-button>
                </span>
              </div>
            </div>
          </div>
          <div class="audit-actions">
            <el-button @click="auditDialogVisible = false">{{ t('common.cancel') }}</el-button>
            <el-button type="danger" :loading="actionLoading" @click="handleRejectInDialog">{{ t('pendingApprovals.dialog.reject') }}</el-button>
            <el-button type="primary" :loading="actionLoading" @click="handleApproveInDialog">{{ t('pendingApprovals.dialog.approve') }}</el-button>
          </div>
        </div>

        <div class="audit-bottom">
          <div class="section-title">{{ t('pendingApprovals.sectionBusiness') }}</div>
          <div v-if="auditDetailLoading" class="detail-loading">{{ t('pendingApprovals.detailLoading') }}</div>
          <div v-else-if="auditDetailError" class="detail-error">{{ auditDetailError }}</div>
          <div class="info-grid">
            <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.bizType') }}</span><span class="v">{{ auditRow.bizTypeName || getBizTypeText(auditRow.bizType) }}</span></div>
            <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.documentCode') }}</span><span class="v">{{ auditRow.documentCode }}</span></div>
            <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.submittedAt') }}</span><span class="v">{{ formatDate(auditRow.createdAt) }}</span></div>
            <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.status') }}</span><span class="v">{{ statusText(auditRow.status) }}</span></div>
            <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.counterparty') }}</span><span class="v">{{ auditRow.counterpartyName || '—' }}</span></div>
            <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.amount') }}</span><span class="v">{{ formatAuditAmount(auditRow, auditDetail) }}</span></div>
          </div>

          <div class="biz-extra">
            <template v-if="auditRow.bizType === 'VENDOR'">
              <div class="extra-title">{{ t('pendingApprovals.vendorSection') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.nameLabel') }}</span>{{ auditDetail?.officialName || auditRow.counterpartyName || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.codeLabel') }}</span>{{ auditRow.documentCode }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.paymentMethod') }}</span>{{ auditDetail?.paymentMethod || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.paymentTerm') }}</span>{{ auditDetail?.payment ?? '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.blacklist') }}</span>{{ auditDetail?.blackList ? t('pendingApprovals.vendor.yes') : t('pendingApprovals.vendor.no') }}</div>
            </template>
            <template v-else-if="auditRow.bizType === 'CUSTOMER'">
              <div class="extra-title">{{ t('pendingApprovals.customerSection') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.nameLabel') }}</span>{{ auditDetail?.customerName || auditDetail?.officialName || auditRow.counterpartyName || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.codeLabel') }}</span>{{ auditRow.documentCode }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.region') }}</span>{{ [auditDetail?.province, auditDetail?.city, auditDetail?.district].filter(Boolean).join(' / ') || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.creditLimit') }}</span>{{ auditDetail?.creditLimit ?? auditDetail?.creditLine ?? '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.salesPerson') }}</span>{{ auditDetail?.salesPersonName || auditDetail?.salesUserId || '—' }}</div>
            </template>
            <template v-else-if="auditRow.bizType === 'SALES_ORDER'">
              <div class="extra-title">{{ t('pendingApprovals.salesOrderSection') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.salesOrder.orderNo') }}</span>{{ auditRow.documentCode }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.salesOrder.customer') }}</span>{{ auditDetail?.customerName || auditRow.counterpartyName || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.salesOrder.orderAmount') }}</span>{{ auditDetail?.total != null ? formatAmount(auditDetail.total, auditDetail.currency) : (auditRow.amount != null ? formatAmount(auditRow.amount, auditRow.currency) : '—') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.salesOrder.deliveryDate') }}</span>{{ auditDetail?.deliveryDate ? formatDate(auditDetail.deliveryDate) : '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.salesOrder.salesUser') }}</span>{{ auditDetail?.salesUserName || auditDetail?.salesUserId || '—' }}</div>
            </template>
            <template v-else-if="auditRow.bizType === 'PURCHASE_ORDER'">
              <div class="extra-title">{{ t('pendingApprovals.purchaseOrderSection') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.purchaseOrder.poNo') }}</span>{{ auditRow.documentCode }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.purchaseOrder.vendor') }}</span>{{ auditDetail?.vendorName || auditRow.counterpartyName || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.purchaseOrder.orderAmount') }}</span>{{ auditDetail?.total != null ? formatAmount(auditDetail.total, auditDetail.currency) : (auditRow.amount != null ? formatAmount(auditRow.amount, auditRow.currency) : '—') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.purchaseOrder.deliveryDate') }}</span>{{ auditDetail?.deliveryDate ? formatDate(auditDetail.deliveryDate) : '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.purchaseOrder.buyer') }}</span>{{ auditDetail?.purchaseUserName || auditDetail?.purchaseUserId || '—' }}</div>
            </template>
            <template v-else-if="auditRow.bizType === 'FINANCE_RECEIPT'">
              <div class="extra-title">{{ t('pendingApprovals.receiptSection') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.receipt.code') }}</span>{{ auditRow.documentCode }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.receipt.customer') }}</span>{{ auditDetail?.customerName || auditRow.counterpartyName || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.receipt.amount') }}</span>{{ auditDetail?.receiptAmount != null ? formatAmount(auditDetail.receiptAmount, auditDetail.receiptCurrency) : (auditRow.amount != null ? formatAmount(auditRow.amount, auditRow.currency) : '—') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.receipt.mode') }}</span>{{ auditDetail?.receiveMode || '—' }}</div>
            </template>
            <template v-else-if="auditRow.bizType === 'FINANCE_PAYMENT'">
              <div class="extra-title">{{ t('pendingApprovals.paymentSection') }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.payment.code') }}</span>{{ auditRow.documentCode }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.payment.vendor') }}</span>{{ auditDetail?.vendorName || auditRow.counterpartyName || '—' }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.payment.amount') }}</span>{{ formatFinancePaymentAuditAmount(auditRow, auditDetail) }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.payment.mode') }}</span>{{ auditDetail?.paymentMode || '—' }}</div>
            </template>
            <template v-else>
              <div class="extra-line"><span>{{ t('pendingApprovals.fallbackApprovalHint') }}</span></div>
            </template>
          </div>

          <div class="detail-jump">
            <el-button type="primary" plain @click="handleView(auditRow)">{{ t('pendingApprovals.viewFullDetail') }}</el-button>
          </div>

          <div class="audit-history">
            <div class="section-title">{{ t('pendingApprovals.sectionHistory') }}</div>
            <div v-if="auditHistoryLoading" class="detail-loading">{{ t('pendingApprovals.historyLoading') }}</div>
            <div v-else-if="auditHistory.length === 0" class="detail-loading">{{ t('pendingApprovals.historyEmpty') }}</div>
            <div v-else class="history-list">
              <div class="history-item" v-for="h in auditHistory" :key="h.id">
                <div class="dot"></div>
                <div class="body">
                  <div class="line-1">
                    <span class="action">{{ historyActionText(h) }}</span>
                    <span class="time">{{ formatDate(h.actionTime) }}</span>
                  </div>
                  <div class="line-2">{{ t('pendingApprovals.history.actor') }}{{ historyActorText(h) }}</div>
                  <div class="line-2" v-if="h.itemDescription">{{ t('pendingApprovals.history.itemDesc') }}{{ h.itemDescription }}</div>
                  <div class="line-2" v-if="h.submitRemark">{{ t('pendingApprovals.history.submitRemark') }}{{ h.submitRemark }}</div>
                  <div class="line-2" v-if="h.auditRemark">{{ t('pendingApprovals.history.auditRemark') }}{{ h.auditRemark }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { approvalsApi, type ApprovalHistoryItem, type BizType, type PendingApprovalItem } from '@/api/approvals'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { vendorApi } from '@/api/vendor'
import { customerApi } from '@/api/customer'
import salesOrderApi from '@/api/salesOrder'
import { financePaymentApi, financeReceiptApi } from '@/api/finance'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { documentApi, type UploadDocumentDto } from '@/api/document'

const router = useRouter()
const { t, te } = useI18n()

const loading = ref(false)
const actionLoading = ref(false)
const auditDialogVisible = ref(false)
const auditRow = ref<PendingApprovalItem | null>(null)
const auditRemark = ref('')
const auditDetailLoading = ref(false)
const auditDetailError = ref('')
const auditDetail = ref<any>(null)
const auditDocsLoading = ref(false)
const auditDocs = ref<UploadDocumentDto[]>([])
const uploadingAuditDocs = ref(false)
const auditHistoryLoading = ref(false)
const auditHistory = ref<ApprovalHistoryItem[]>([])

const searchForm = ref({
  bizType: '' as '' | BizType
})

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0
})

const approvalList = ref<PendingApprovalItem[]>([])
const activeState = ref<'pending' | 'approved' | 'rejected'>('pending')
const pendingCount = ref(0)
const approvedCount = ref(0)
const rejectedCount = ref(0)

const getBizTypeText = (type: string) => {
  const key = `pendingApprovals.bizType.${type}` as const
  return te(key) ? t(key) : type
}

const getBizTypeTagType = (type: string) => {
  const map: Record<string, string> = {
    VENDOR: 'warning',
    QUOTE: 'primary',
    SALES_ORDER: 'primary',
    PURCHASE_ORDER: 'info',
    FINANCE_RECEIPT: 'success',
    FINANCE_PAYMENT: 'danger'
  }
  return map[type] || ''
}

const formatDate = (dateStr: string) => formatDisplayDateTime(dateStr)

/** 币别：1=RMB 2=USD 3=EUR */
const formatAmount = (amount: number, currency?: number | null) => {
  const sym = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return sym + Number(amount).toLocaleString('zh-CN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
}

const pickDefinedNumber = (v: unknown): number | undefined => {
  if (v == null || v === '') return undefined
  const n = Number(v)
  return Number.isFinite(n) ? n : undefined
}

const auditMoneyCurrency = (row: PendingApprovalItem, detail: any) =>
  pickDefinedNumber(row.currency) ??
  pickDefinedNumber(detail?.paymentCurrency) ??
  pickDefinedNumber(detail?.PaymentCurrency) ??
  1

/** 付款请款：主数据为 PaymentAmountToBe，待审核阶段 PaymentAmount 多为 0 */
const formatFinancePaymentAuditAmount = (row: PendingApprovalItem, detail: any) => {
  const cur = auditMoneyCurrency(row, detail)
  const d = detail || {}
  const toBe = pickDefinedNumber(d.paymentAmountToBe ?? d.PaymentAmountToBe)
  if (toBe !== undefined) return formatAmount(toBe, cur)
  const paid = pickDefinedNumber(d.paymentAmount ?? d.PaymentAmount)
  if (paid !== undefined) return formatAmount(paid, cur)
  const fromRow = pickDefinedNumber(row.amount)
  if (fromRow !== undefined) return formatAmount(fromRow, cur)
  return '—'
}

const formatAuditAmount = (row: PendingApprovalItem, detail: any) => {
  if (row.bizType === 'FINANCE_PAYMENT') return formatFinancePaymentAuditAmount(row, detail)
  return row.amount != null ? formatAmount(row.amount, row.currency) : '—'
}

const buildItemDescription = (row: PendingApprovalItem) => {
  const titlePart = (row.title || '').trim()
  const cp = (row.counterpartyName || '').trim()
  const join = t('pendingApprovals.descJoin')
  if (titlePart && cp && titlePart !== cp) return `${titlePart}${join}${cp}`
  if (titlePart) return titlePart
  if (cp) return cp
  return row.documentCode || '—'
}

const is404Error = (e: unknown) => {
  const msg = e instanceof Error ? e.message : String(e ?? '')
  return /404/.test(msg) || /not\s*found/i.test(msg)
}

const loadApprovalItemsCompat = async () => {
  try {
    return await approvalsApi.getApprovalItems({
      bizType: searchForm.value.bizType || undefined,
      state: activeState.value,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize
    })
  } catch (e) {
    // 兼容旧后端：仅提供 /pending 接口
    if (!is404Error(e)) throw e
    if (activeState.value !== 'pending') {
      return {
        items: [] as PendingApprovalItem[],
        total: 0,
        page: pagination.value.page,
        pageSize: pagination.value.pageSize
      }
    }
    return await approvalsApi.getPendingApprovals({
      bizType: searchForm.value.bizType || undefined,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize
    })
  }
}

const loadApprovalSummaryCompat = async () => {
  try {
    return await approvalsApi.getApprovalSummary({
      bizType: searchForm.value.bizType || undefined
    })
  } catch (e) {
    if (!is404Error(e)) throw e
    // 旧后端没有 summary：降级展示
    return {
      pendingCount: activeState.value === 'pending' ? Number(pagination.value.total || 0) : 0,
      approvedCount: 0,
      rejectedCount: 0
    }
  }
}

const handleSearch = async () => {
  loading.value = true
  try {
    const [res, summary] = await Promise.all([
      loadApprovalItemsCompat(),
      loadApprovalSummaryCompat()
    ])
    approvalList.value = res.items ?? []
    pagination.value.total = res.total ?? 0
    // 兼容模式下，pendingCount 可用当前分页总数兜底
    const fallbackPending = activeState.value === 'pending' ? Number(pagination.value.total || 0) : 0
    pendingCount.value = Number(summary.pendingCount ?? 0)
    approvedCount.value = Number(summary.approvedCount ?? 0)
    rejectedCount.value = Number(summary.rejectedCount ?? 0)
    if (!pendingCount.value && fallbackPending > 0) pendingCount.value = fallbackPending
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('pendingApprovals.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

const switchState = (state: 'pending' | 'approved' | 'rejected') => {
  if (activeState.value === state) return
  activeState.value = state
  pagination.value.page = 1
  handleSearch()
}

const refreshSummaryOnly = async () => {
  try {
    const summary = await loadApprovalSummaryCompat()
    pendingCount.value = Number(summary.pendingCount ?? 0)
    approvedCount.value = Number(summary.approvedCount ?? 0)
    rejectedCount.value = Number(summary.rejectedCount ?? 0)
  } catch {
    // 忽略汇总错误，不影响主列表
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
    case 'CUSTOMER':
      router.push({ name: 'CustomerDetail', params: { id } })
      break
    case 'FINANCE_RECEIPT':
      router.push({ name: 'FinanceReceiptDetail', params: { id } })
      break
    case 'FINANCE_PAYMENT':
      router.push({ name: 'FinancePaymentDetail', params: { id } })
      break
    case 'PURCHASE_ORDER':
      router.push({ name: 'PurchaseOrderDetail', params: { id } })
      break
    default:
      ElMessage.warning(t('pendingApprovals.messages.jumpNotSupported'))
  }
}

const openAuditDialog = (row: PendingApprovalItem) => {
  auditRow.value = row
  auditRemark.value = ''
  auditDialogVisible.value = true
  loadAuditDetail(row)
  loadAuditDocs(row)
  loadAuditHistory(row)
}

const normalizeApiData = <T = any>(res: any): T => (res?.data ?? res) as T
const currentBizKey = () => ({ bizType: auditRow.value?.bizType || '', bizId: auditRow.value?.businessId || '' })

const loadAuditDetail = async (row: PendingApprovalItem) => {
  auditDetailLoading.value = true
  auditDetailError.value = ''
  auditDetail.value = null
  try {
    if (row.bizType === 'VENDOR') {
      auditDetail.value = normalizeApiData(await vendorApi.getVendorById(row.businessId))
    } else if (row.bizType === 'CUSTOMER') {
      auditDetail.value = normalizeApiData(await customerApi.getCustomerById(row.businessId))
    } else if (row.bizType === 'SALES_ORDER') {
      auditDetail.value = normalizeApiData(await salesOrderApi.getById(row.businessId))
    } else if (row.bizType === 'FINANCE_RECEIPT') {
      auditDetail.value = normalizeApiData(await financeReceiptApi.getById(row.businessId))
    } else if (row.bizType === 'FINANCE_PAYMENT') {
      auditDetail.value = normalizeApiData(await financePaymentApi.getById(row.businessId))
    } else if (row.bizType === 'PURCHASE_ORDER') {
      auditDetail.value = normalizeApiData(await purchaseOrderApi.getById(row.businessId))
    }
  } catch (e: any) {
    auditDetailError.value = e?.message || t('pendingApprovals.messages.detailLoadFailed')
  } finally {
    auditDetailLoading.value = false
  }
}

const loadAuditDocs = async (row: PendingApprovalItem) => {
  auditDocsLoading.value = true
  try {
    auditDocs.value = await documentApi.getDocuments(row.bizType, row.businessId)
  } catch {
    auditDocs.value = []
  } finally {
    auditDocsLoading.value = false
  }
}

const getSubmitRemark = () => {
  const d = auditDetail.value || {}
  return d.submitRemark || d.remark || d.remarks || d.companyInfo || '—'
}

const previewDoc = (doc: UploadDocumentDto) => {
  window.open(documentApi.getPreviewPath(doc.id), '_blank')
}

const downloadDoc = async (doc: UploadDocumentDto) => {
  await documentApi.downloadDocument(doc.id, doc.originalFileName)
}

const onAuditFilesSelected = async (e: Event) => {
  const files = Array.from((e.target as HTMLInputElement).files || [])
  if (!files.length) return
  const key = currentBizKey()
  if (!key.bizType || !key.bizId) return
  try {
    uploadingAuditDocs.value = true
    await documentApi.uploadDocuments(key.bizType, key.bizId, files, t('pendingApprovals.messages.auditDocCategory'))
    if (auditRow.value) await loadAuditDocs(auditRow.value)
    ElMessage.success(t('pendingApprovals.messages.uploadSuccess'))
  } catch (err: any) {
    ElMessage.error(err?.message || t('pendingApprovals.messages.uploadFailed'))
  } finally {
    uploadingAuditDocs.value = false
    ;(e.target as HTMLInputElement).value = ''
  }
}

const loadAuditHistory = async (row: PendingApprovalItem) => {
  auditHistoryLoading.value = true
  try {
    const list = await approvalsApi.getApprovalHistory({ bizType: row.bizType, businessId: row.businessId })
    auditHistory.value = Array.isArray(list) ? list : ((list as any)?.data ?? [])
  } catch {
    auditHistory.value = []
  } finally {
    auditHistoryLoading.value = false
  }
}

const historyActionText = (item: ApprovalHistoryItem) => {
  if (item.actionType === 'submit') return t('pendingApprovals.historyAction.submit')
  if (item.actionType === 'approve') return t('pendingApprovals.historyAction.approve')
  if (item.actionType === 'reject') return t('pendingApprovals.historyAction.reject')
  return item.actionType
}

const historyActorText = (item: ApprovalHistoryItem) => {
  const sys = t('pendingApprovals.system')
  if (item.actionType === 'submit') return item.submitterUserName || item.submitterUserId || sys
  return item.approverUserName || item.approverUserId || sys
}

const handleApproveInDialog = async () => {
  if (!auditRow.value) return
  try {
    actionLoading.value = true
    await approvalsApi.decidePendingApproval({
      bizType: auditRow.value.bizType,
      businessId: auditRow.value.businessId,
      decision: 'approve',
      remark: auditRemark.value.trim() || undefined
    })
    ElMessage.success(t('pendingApprovals.messages.approveSuccess'))
    auditDialogVisible.value = false
    await refreshSummaryOnly()
    await handleSearch()
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('pendingApprovals.messages.operationFailed'))
  } finally {
    actionLoading.value = false
  }
}

const handleRejectInDialog = async () => {
  if (!auditRow.value) return
  const needReason = ['SALES_ORDER', 'PURCHASE_ORDER', 'CUSTOMER', 'VENDOR', 'FINANCE_PAYMENT'].includes(
    auditRow.value.bizType
  )
  if (needReason && !auditRemark.value.trim()) {
    ElMessage.warning(t('pendingApprovals.messages.rejectReasonRequired'))
    return
  }
  try {
    actionLoading.value = true
    await approvalsApi.decidePendingApproval({
      bizType: auditRow.value.bizType,
      businessId: auditRow.value.businessId,
      decision: 'reject',
      remark: auditRemark.value.trim() || undefined
    })
    ElMessage.success(t('pendingApprovals.messages.rejectSuccess'))
    auditDialogVisible.value = false
    await refreshSummaryOnly()
    await handleSearch()
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('pendingApprovals.messages.operationFailed'))
  } finally {
    actionLoading.value = false
  }
}

const statusText = (status: number) => {
  if (status === 2 || status === 1) return t('pendingApprovals.rowStatus.pending')
  if (status === 10 || status === 20 || status === 3) return t('pendingApprovals.rowStatus.passed')
  if (status < 0 || status === 4 || status === 5) return t('pendingApprovals.rowStatus.rejected')
  return String(status)
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
  margin-bottom: 14px;
}

.page-title {
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  letter-spacing: 0.3px;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
  margin-bottom: 14px;
}

.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: 10px;
  padding: 16px 18px;
}

.stat-label {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 30px;
  line-height: 1;
  font-weight: 700;
  font-family: 'Space Mono', monospace;
}

.stat-card--pending .stat-value {
  color: $warning-color;
}
.stat-card--approved .stat-value {
  color: $success-color;
}
.stat-card--rejected .stat-value {
  color: $danger-color;
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

.segment-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

.segment-item {
  border: 1px solid rgba(0, 212, 255, 0.2);
  background: rgba(0, 212, 255, 0.08);
  color: $text-secondary;
  border-radius: 999px;
  padding: 3px 10px;
  font-size: 12px;
  cursor: pointer;
}

.segment-item.is-active {
  color: $cyan-primary;
  border-color: rgba(0, 212, 255, 0.45);
}

.segment-item:disabled {
  opacity: 0.65;
  cursor: not-allowed;
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
  color: #f0f6ff;
  font-size: 15px;
  font-weight: 600;
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

.audit-dialog {
  .audit-top {
    border: 1px solid rgba(0, 212, 255, 0.15);
    border-radius: 10px;
    padding: 12px 14px;
    margin-bottom: 12px;
    background: rgba(0, 212, 255, 0.03);
  }

  .audit-top-head {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 10px;
  }

  .audit-doc {
    font-family: 'Space Mono', monospace;
    color: $text-primary;
    font-weight: 600;
  }

  .audit-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
  }

  .submit-remark {
    color: $text-secondary;
    white-space: pre-wrap;
    line-height: 1.5;
  }

  .audit-attachments {
    margin-top: 10px;
    border-top: 1px dashed rgba(255, 255, 255, 0.12);
    padding-top: 10px;
  }

  .attach-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    color: $text-primary;
    margin-bottom: 8px;
  }

  .upload-btn {
    position: relative;
    display: inline-block;
    font-size: 12px;
    color: $cyan-primary;
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 6px;
    padding: 2px 8px;
    cursor: pointer;
  }

  .upload-btn input {
    position: absolute;
    inset: 0;
    opacity: 0;
    cursor: pointer;
  }

  .attach-list {
    display: grid;
    gap: 6px;
  }

  .attach-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 6px;
    padding: 6px 8px;
    background: rgba(255, 255, 255, 0.02);
  }

  .attach-item .name {
    color: $text-secondary;
    max-width: 70%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .audit-bottom {
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 10px;
    padding: 12px 14px;
    background: rgba(255, 255, 255, 0.01);
  }

  .section-title {
    color: $text-primary;
    font-weight: 600;
    margin-bottom: 10px;
  }

  .info-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px 14px;
  }

  .info-item {
    display: flex;
    gap: 8px;
    min-width: 0;
  }

  .k {
    color: $text-muted;
    width: 70px;
    flex-shrink: 0;
  }

  .v {
    color: $text-primary;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .biz-extra {
    margin-top: 12px;
    padding-top: 10px;
    border-top: 1px dashed rgba(255, 255, 255, 0.1);
  }

  .detail-loading {
    color: $text-muted;
    margin-bottom: 8px;
  }

  .detail-error {
    color: #ef6a73;
    margin-bottom: 8px;
  }

  .extra-title {
    color: $text-primary;
    font-weight: 600;
    margin-bottom: 8px;
  }

  .extra-line {
    color: $text-secondary;
    margin-bottom: 6px;
    span { color: $text-primary; }
  }

  .detail-jump {
    margin-top: 10px;
    display: flex;
    justify-content: flex-end;
  }

  .audit-history {
    margin-top: 12px;
    border-top: 1px dashed rgba(255, 255, 255, 0.1);
    padding-top: 10px;
  }

  .history-list {
    display: grid;
    gap: 8px;
  }

  .history-item {
    display: flex;
    gap: 8px;
    border: 1px solid rgba(255,255,255,0.08);
    border-radius: 8px;
    padding: 8px;
    background: rgba(255,255,255,0.02);
  }

  .history-item .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: $cyan-primary;
    margin-top: 6px;
    flex-shrink: 0;
  }

  .history-item .line-1 {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
  }

  .history-item .action {
    color: $text-primary;
    font-weight: 600;
  }

  .history-item .time,
  .history-item .line-2 {
    color: $text-muted;
    font-size: 12px;
  }
}
</style>
