<template>
  <div v-if="row" class="audit-dialog" :class="{ 'audit-dialog--embedded': embedded }">
    <div class="audit-business">
      <div class="section-title">{{ t('pendingApprovals.sectionBusiness') }}</div>
      <div v-if="auditDetailLoading" class="detail-loading">{{ t('pendingApprovals.detailLoading') }}</div>
      <div v-else-if="auditDetailError" class="detail-error">{{ auditDetailError }}</div>
      <div class="info-grid">
        <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.bizType') }}</span><span class="v">{{ row.bizTypeName || getBizTypeText(row.bizType) }}</span></div>
        <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.documentCode') }}</span><span class="v">{{ row.documentCode }}</span></div>
        <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.submittedAt') }}</span><span class="v">{{ formatDate(row.createdAt) }}</span></div>
        <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.status') }}</span><span class="v">{{ statusText(row.status) }}</span></div>
        <div class="info-item"><span class="k">{{ t('pendingApprovals.infoLabels.counterparty') }}</span><span class="v">{{ displayCounterpartyName(row) }}</span></div>
        <div class="info-item">
          <span class="k">{{ t('pendingApprovals.infoLabels.amount') }}</span>
          <span class="v">
            <template v-if="auditHeaderAmountText === '—'">—</template>
            <span v-else class="amount-with-code">
              <span>{{ auditHeaderAmountText }}</span>
              <span
                v-if="auditHeaderCurrencyIso"
                :class="['dock-tier-ccy', listAmountCurrencyDockClass(auditHeaderCurrency)]"
              >{{ auditHeaderCurrencyIso }}</span>
            </span>
          </span>
        </div>
      </div>

      <div
        class="biz-extra"
        :class="{ 'biz-extra--stacked': row.bizType === 'SALES_ORDER' || row.bizType === 'PURCHASE_ORDER' }"
      >
        <template v-if="row.bizType === 'VENDOR'">
          <div class="extra-title">{{ t('pendingApprovals.vendorSection') }}</div>
          <div class="extra-grid">
            <div class="extra-line extra-line--span">
              <span>{{ t('pendingApprovals.vendor.nameLabel') }}</span>{{ auditVendorNameLabel(auditDetail, row.counterpartyName ?? undefined) }}
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.codeLabel') }}</span>{{ row.documentCode }}</div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.tradeCurrency') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorCurrencyLabel }}</div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.identity') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorIdentityLabel }}</div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.paymentMethod') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorPaymentMethodLabel }}</div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.vendor.paymentTermType') }}</span>
              <em
                v-if="!maskPurchaseSensitiveFields"
                class="extra-value"
                :class="{ 'extra-value--warn': auditVendorIsCreditTerm }"
              >{{ auditVendorPaymentTermTypeLabel }}</em>
              <template v-else>—</template>
            </div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.vendor.paymentTerm') }}</span>
              <em
                v-if="!maskPurchaseSensitiveFields"
                class="extra-value"
                :class="{ 'extra-value--warn': auditVendorPaymentHighlight }"
              >{{ auditVendorPaymentLabel }}</em>
              <template v-else>—</template>
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.purchaser') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorPurchaserLabel }}</div>
          </div>
        </template>
        <template v-else-if="row.bizType === 'CUSTOMER'">
          <div class="extra-title">{{ t('pendingApprovals.customerSection') }}</div>
          <div class="extra-grid">
            <div class="extra-line extra-line--span">
              <span>{{ t('pendingApprovals.customer.nameLabel') }}</span>{{ auditCustomerNameLabel(auditDetail, row.counterpartyName ?? undefined) }}
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.customer.codeLabel') }}</span>{{ maskSaleSensitiveFields ? '—' : row.documentCode }}</div>
            <div class="extra-line"><span>{{ t('pendingApprovals.customer.customerType') }}</span>{{ maskSaleSensitiveFields ? '—' : auditCustomerTypeLabel }}</div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.customer.paymentTermType') }}</span>
              <em
                v-if="!maskSaleSensitiveFields"
                class="extra-value"
                :class="{ 'extra-value--warn': auditCustomerIsCreditTerm }"
              >{{ auditCustomerPaymentTermTypeLabel }}</em>
              <template v-else>—</template>
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.customer.settlementCurrency') }}</span>{{ maskSaleSensitiveFields ? '—' : auditCustomerCurrencyLabel }}</div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.customer.paymentDays') }}</span>
              <em
                v-if="!maskSaleSensitiveFields"
                class="extra-value"
                :class="{ 'extra-value--warn': auditCustomerPaymentDaysHighlight }"
              >{{ auditCustomerPaymentDaysLabel }}</em>
              <template v-else>—</template>
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.customer.creditLimit') }}</span>{{ maskSaleSensitiveFields ? '—' : (auditDetail?.creditLimit ?? auditDetail?.creditLine ?? '—') }}</div>
            <div class="extra-line"><span>{{ t('pendingApprovals.customer.salesPerson') }}</span>{{ maskSaleSensitiveFields ? '—' : auditCustomerSalesPersonLabel }}</div>
          </div>
        </template>
        <template v-else-if="row.bizType === 'SALES_ORDER'">
          <div class="extra-panel extra-panel--customer">
            <div class="extra-title">{{ t('pendingApprovals.salesOrderCustomerSection') }}</div>
            <div class="extra-grid">
              <div class="extra-line extra-line--span">
                <span>{{ t('pendingApprovals.salesOrder.customer') }}</span>{{ auditSalesOrderCustomerNameLabel }}
              </div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.customerType') }}</span>{{ maskSaleSensitiveFields ? '—' : auditCustomerTypeLabel }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.settlementCurrency') }}</span>{{ maskSaleSensitiveFields ? '—' : auditCustomerCurrencyLabel }}</div>
              <div class="extra-line">
                <span>{{ t('pendingApprovals.customer.paymentTermType') }}</span>
                <em
                  v-if="!maskSaleSensitiveFields"
                  class="extra-value"
                  :class="{ 'extra-value--warn': auditCustomerIsCreditTerm }"
                >{{ auditCustomerPaymentTermTypeLabel }}</em>
                <template v-else>—</template>
              </div>
              <div class="extra-line">
                <span>{{ t('pendingApprovals.customer.paymentDays') }}</span>
                <em
                  v-if="!maskSaleSensitiveFields"
                  class="extra-value"
                  :class="{ 'extra-value--warn': auditCustomerPaymentDaysHighlight }"
                >{{ auditCustomerPaymentDaysLabel }}</em>
                <template v-else>—</template>
              </div>
              <div class="extra-line"><span>{{ t('pendingApprovals.customer.creditLimit') }}</span>{{ maskSaleSensitiveFields ? '—' : auditCustomerCreditLimitLabel }}</div>
            </div>
          </div>
          <ApprovalOrderLineCards
            class="audit-sales-order-ref"
            mode="sales"
            :columns="2"
            :order-id="String(row.businessId || '')"
            :items="auditSalesOrderItems"
          />
        </template>
        <template v-else-if="row.bizType === 'PURCHASE_ORDER'">
          <div class="extra-panel extra-panel--customer">
            <div class="extra-title">{{ t('pendingApprovals.purchaseOrderVendorSection') }}</div>
            <div class="extra-grid">
              <div class="extra-line extra-line--span">
                <span>{{ t('pendingApprovals.purchaseOrder.vendor') }}</span>{{ auditPurchaseOrderVendorNameLabel }}
              </div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.identity') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorIdentityLabel }}</div>
              <div class="extra-line"><span>{{ t('pendingApprovals.vendor.paymentMethod') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorPaymentMethodLabel }}</div>
              <div class="extra-line">
                <span>{{ t('pendingApprovals.vendor.paymentTermType') }}</span>
                <em
                  v-if="!maskPurchaseSensitiveFields"
                  class="extra-value"
                  :class="{ 'extra-value--warn': auditVendorIsCreditTerm }"
                >{{ auditVendorPaymentTermTypeLabel }}</em>
                <template v-else>—</template>
              </div>
              <div class="extra-line">
                <span>{{ t('pendingApprovals.customer.paymentDays') }}</span>
                <em
                  v-if="!maskPurchaseSensitiveFields"
                  class="extra-value"
                  :class="{ 'extra-value--warn': auditVendorPaymentHighlight }"
                >{{ auditVendorPaymentLabel }}</em>
                <template v-else>—</template>
              </div>
            </div>
          </div>
          <ApprovalOrderLineCards
            class="audit-sales-order-ref"
            mode="purchase"
            :columns="2"
            :order-id="String(row.businessId || '')"
            :items="auditPurchaseOrderItems"
          />
        </template>
        <template v-else-if="row.bizType === 'FINANCE_RECEIPT'">
          <div class="extra-title">{{ t('pendingApprovals.receiptSection') }}</div>
          <div class="extra-line"><span>{{ t('pendingApprovals.receipt.code') }}</span>{{ row.documentCode }}</div>
          <div class="extra-line"><span>{{ t('pendingApprovals.receipt.customer') }}</span>{{ maskSaleSensitiveFields ? '—' : (auditDetail?.customerName || row.counterpartyName || '—') }}</div>
          <div class="extra-line"><span>{{ t('pendingApprovals.receipt.amount') }}</span>{{ maskSaleSensitiveFields ? '—' : (auditDetail?.receiptAmount != null ? formatAmount(auditDetail.receiptAmount, auditDetail.receiptCurrency) : (row.amount != null ? formatAmount(row.amount, row.currency) : '—')) }}</div>
          <div class="extra-line"><span>{{ t('pendingApprovals.receipt.mode') }}</span>{{ auditDetail?.receiveMode || '—' }}</div>
        </template>
        <template v-else-if="row.bizType === 'FINANCE_PAYMENT'">
          <div class="extra-title">{{ t('pendingApprovals.paymentSection') }}</div>
          <div class="extra-grid">
            <div class="extra-line extra-line--span">
              <span>{{ t('pendingApprovals.payment.vendor') }}</span>{{ formatVendorNameReadonly(
                auditDetail?.vendorName || row.counterpartyName,
                auditDetail?.vendorEnglishName || auditDetail?.englishOfficialName,
                { masked: maskPurchaseSensitiveFields }
              ) }}
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.identity') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorIdentityLabel }}</div>
            <div class="extra-line"><span>{{ t('pendingApprovals.vendor.paymentMethod') }}</span>{{ maskPurchaseSensitiveFields ? '—' : auditVendorPaymentMethodLabel }}</div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.vendor.paymentTermType') }}</span>
              <em
                v-if="!maskPurchaseSensitiveFields"
                class="extra-value"
                :class="{ 'extra-value--warn': auditVendorIsCreditTerm }"
              >{{ auditVendorPaymentTermTypeLabel }}</em>
              <template v-else>—</template>
            </div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.vendor.paymentTerm') }}</span>
              <em
                v-if="!maskPurchaseSensitiveFields"
                class="extra-value"
                :class="{ 'extra-value--warn': auditVendorPaymentHighlight }"
              >{{ auditVendorPaymentLabel }}</em>
              <template v-else>—</template>
            </div>
            <div class="extra-divider" role="separator" />
            <div class="extra-line"><span>{{ t('pendingApprovals.payment.code') }}</span>{{ row.documentCode }}</div>
            <div class="extra-line">
              <span>{{ t('pendingApprovals.payment.amount') }}</span>
              <template v-if="auditFinancePaymentAmountText === '—'">—</template>
              <span v-else class="amount-with-code">
                <span class="amount-with-code__num">{{ auditFinancePaymentAmountText }}</span>
                <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(auditFinancePaymentCurrency)]">
                  {{ listAmountCurrencyIso(auditFinancePaymentCurrency) }}
                </span>
              </span>
            </div>
            <div class="extra-line"><span>{{ t('pendingApprovals.payment.mode') }}</span>{{ auditPaymentModeLabel }}</div>
          </div>
        </template>
        <template v-else>
          <div class="extra-line"><span>{{ t('pendingApprovals.fallbackApprovalHint') }}</span></div>
        </template>
      </div>

      <div
        v-if="row.bizType === 'SALES_ORDER' || row.bizType === 'PURCHASE_ORDER'"
        class="order-files-panel"
      >
        <div class="extra-title">{{ t('pendingApprovals.orderFilesSection') }}</div>
        <div v-if="isAuditAttachmentsRestricted(row)" class="order-files-panel__restricted">
          {{ t('pendingApprovals.dialog.attachmentsRestrictedByRbac') }}
        </div>
        <DocumentListPanel
          v-else-if="auditOrderFilesBizType && auditOrderFilesBizId"
          :biz-type="auditOrderFilesBizType"
          :biz-id="auditOrderFilesBizId"
          view-mode="list"
          readonly
          hide-toolbar
          :empty-text="t('pendingApprovals.orderFilesEmpty')"
        />
        <div v-else class="order-files-panel__restricted">
          {{ t('pendingApprovals.orderFilesEmpty') }}
        </div>
      </div>

      <div class="detail-jump">
        <el-button type="primary" plain @click="handleViewInNewTab(row)">{{ t('pendingApprovals.viewFullDetail') }}</el-button>
      </div>
    </div>

    <div class="audit-top">
      <el-form label-width="88px">
        <el-form-item :label="t('pendingApprovals.dialog.submitRemark')">
          <div class="submit-remark">{{ getSubmitRemark() }}</div>
        </el-form-item>
        <el-form-item v-if="!readOnly" :label="t('pendingApprovals.dialog.auditRemark')">
          <el-input
            v-model="auditRemark"
            type="textarea"
            :rows="3"
            :placeholder="t('pendingApprovals.dialog.auditRemarkPlaceholder')"
          />
        </el-form-item>
      </el-form>
      <div
        v-if="row.bizType !== 'SALES_ORDER' && row.bizType !== 'PURCHASE_ORDER'"
        class="audit-attachments"
      >
        <div class="attach-header">
          <span>{{ t('pendingApprovals.dialog.attachmentPreview') }}</span>
        </div>
        <div v-if="isAuditAttachmentsRestricted(row)" class="detail-loading">
          {{ t('pendingApprovals.dialog.attachmentsRestrictedByRbac') }}
        </div>
        <template v-else>
          <div v-if="auditDocsLoading" class="detail-loading">{{ t('pendingApprovals.dialog.docsLoading') }}</div>
          <div v-else-if="auditDocs.length === 0" class="detail-loading">{{ t('pendingApprovals.dialog.noAttachments') }}</div>
          <div v-else class="attach-list">
            <div class="attach-item" v-for="doc in auditDocs" :key="doc.id">
              <span class="name" :title="doc.originalFileName">{{ doc.originalFileName }}</span>
              <span class="ops">
                <el-button link type="primary" size="small" @click="previewDoc(doc)">{{ t('pendingApprovals.dialog.preview') }}</el-button>
                <el-button link type="primary" size="small" @click="downloadDoc(doc)">{{ t('pendingApprovals.dialog.download') }}</el-button>
              </span>
            </div>
          </div>
        </template>
      </div>
      <div v-if="!readOnly" class="audit-actions">
        <el-button v-if="!embedded" @click="emit('close')">{{ t('common.cancel') }}</el-button>
        <el-button type="danger" :loading="actionLoading" @click="handleReject">{{ t('pendingApprovals.dialog.reject') }}</el-button>
        <el-button type="primary" :loading="actionLoading" @click="handleApprove">{{ t('pendingApprovals.dialog.approve') }}</el-button>
      </div>
      <div v-else-if="!embedded" class="audit-actions">
        <el-button type="primary" @click="emit('close')">{{ t('pendingApprovals.dialog.close') }}</el-button>
      </div>
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
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  approvalsApi,
  type ApprovalHistoryItem,
  type BizType,
  type PendingApprovalItem
} from '@/api/approvals'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { vendorApi } from '@/api/vendor'
import { customerApi } from '@/api/customer'
import salesOrderApi from '@/api/salesOrder'
import { financePaymentApi, financeReceiptApi } from '@/api/finance'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import ApprovalOrderLineCards from '@/components/Approvals/ApprovalOrderLineCards.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'
import { formatCustomerNameReadonly } from '@/utils/customerDisplayName'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useCustomerDictStore } from '@/stores/customerDict'
import { useVendorDictStore } from '@/stores/vendorDict'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import {
  formatTotalAmountNumber,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso
} from '@/utils/moneyFormat'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'

const props = withDefaults(
  defineProps<{
    row: PendingApprovalItem | null
    readOnly?: boolean
    embedded?: boolean
  }>(),
  {
    readOnly: false,
    embedded: false
  }
)

export type ApprovalAuditPartyContext = {
  bizType: BizType
  businessId: string
  customerId?: string | null
  customerName?: string | null
  vendorId?: string | null
  vendorName?: string | null
  /** SO/PO 详情明细（供右侧行卡片） */
  orderItems?: any[]
}

const emit = defineEmits<{
  decided: [payload: { decision: 'approve' | 'reject' }]
  close: []
  context: [payload: ApprovalAuditPartyContext | null]
}>()

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { paymentModeLabel } = useFinanceEnumLabels()
const router = useRouter()
const { t, te } = useI18n()

const row = computed(() => props.row)
const readOnly = computed(() => !!props.readOnly)
const embedded = computed(() => !!props.embedded)

const actionLoading = ref(false)
const auditRemark = ref('')
const auditDetailLoading = ref(false)
const auditDetailError = ref('')
const auditDetail = ref<any>(null)
/** 付款单审核时关联的供应商主档（身份/账期等） */
const auditRelatedVendor = ref<any>(null)
/** 销售订单审核时关联的客户主档（类型/账期/额度等） */
const auditRelatedCustomer = ref<any>(null)
const auditDocsLoading = ref(false)
const auditDocs = ref<UploadDocumentDto[]>([])
const auditHistoryLoading = ref(false)
const auditHistory = ref<ApprovalHistoryItem[]>([])
const customerDict = useCustomerDictStore()
const vendorDict = useVendorDictStore()

function isAuditAttachmentsRestricted(item: PendingApprovalItem) {
  const bt = item.bizType
  if (maskSaleSensitiveFields.value && (bt === 'SALES_ORDER' || bt === 'FINANCE_RECEIPT')) return true
  if (maskPurchaseSensitiveFields.value && (bt === 'PURCHASE_ORDER' || bt === 'FINANCE_PAYMENT')) return true
  return false
}

/** 审核窗左侧「订单文件」：与业务详情「文档」页签同源（SO / PO 均用自身 bizType + businessId） */
const auditOrderFilesBizType = computed(() => {
  const bt = props.row?.bizType
  if (bt === 'SALES_ORDER' || bt === 'PURCHASE_ORDER') return bt
  return ''
})

const auditOrderFilesBizId = computed(() => {
  const bt = props.row?.bizType
  if (bt === 'SALES_ORDER' || bt === 'PURCHASE_ORDER') {
    return String(props.row?.businessId || '').trim()
  }
  return ''
})

const auditVendorSource = computed(() => {
  if (props.row?.bizType === 'VENDOR') return auditDetail.value
  if (props.row?.bizType === 'FINANCE_PAYMENT' || props.row?.bizType === 'PURCHASE_ORDER') {
    return auditRelatedVendor.value
  }
  return null
})

const auditCustomerSource = computed(() => {
  if (props.row?.bizType === 'CUSTOMER') return auditDetail.value
  if (props.row?.bizType === 'SALES_ORDER') return auditRelatedCustomer.value
  return null
})

function resolveAuditCustomerPaymentDays(detail: any): number | null {
  if (!detail) return null
  const raw = detail.paymentTerms ?? detail.payment ?? detail.Payment
  if (raw == null || raw === '') return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
}

const auditCustomerTypeLabel = computed(() => {
  const d = auditCustomerSource.value
  if (!d) return '—'
  const type = d.customerType ?? d.type ?? d.Type
  const label = customerDict.typeLabel(type == null ? 0 : Number(type))
  return !label || label === '--' ? '—' : label
})

/** 业务员展示登录账号；不回退显示 GUID */
const auditCustomerSalesPersonLabel = computed(() => {
  const d = auditCustomerSource.value
  if (!d) return '—'
  const name = String(d.salesPersonName ?? d.SalesPersonName ?? '').trim()
  if (name && !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(name)) {
    return name
  }
  return '—'
})

const auditCustomerPaymentDaysLabel = computed(() => {
  const n = resolveAuditCustomerPaymentDays(auditCustomerSource.value)
  return n == null ? '—' : String(n)
})

/** 客户主档无独立账期类型字段：按账期天数推导展示（0=现结，>0=账期） */
const auditCustomerPaymentTermTypeLabel = computed(() => {
  const n = resolveAuditCustomerPaymentDays(auditCustomerSource.value)
  if (n == null) return '—'
  return n <= 0
    ? t('pendingApprovals.customer.paymentTermCash')
    : t('pendingApprovals.customer.paymentTermCredit')
})

const auditCustomerIsCreditTerm = computed(() => {
  const n = resolveAuditCustomerPaymentDays(auditCustomerSource.value)
  return n != null && n > 0
})

const auditCustomerPaymentDaysHighlight = computed(() => {
  const n = resolveAuditCustomerPaymentDays(auditCustomerSource.value)
  return n != null && n > 0
})

const auditCustomerCreditLimitLabel = computed(() => {
  const d = auditCustomerSource.value
  if (!d) return '—'
  const raw = d.creditLimit ?? d.creditLine ?? d.CreditLine
  if (raw == null || raw === '') return '—'
  return String(raw)
})

const auditSalesOrderCustomerNameLabel = computed(() => {
  if (maskSaleSensitiveFields.value) return '—'
  const c = auditRelatedCustomer.value
  const o = auditDetail.value
  return formatCustomerNameReadonly(
    c?.customerName || c?.officialName || o?.customerName || props.row?.counterpartyName,
    c?.englishOfficialName || c?.customerEnglishName || o?.customerEnglishName || o?.englishOfficialName,
  )
})

const auditPurchaseOrderVendorNameLabel = computed(() => {
  const v = auditRelatedVendor.value
  const o = auditDetail.value
  return formatVendorNameReadonly(
    v?.officialName || v?.vendorName || o?.vendorName || props.row?.counterpartyName,
    v?.englishOfficialName || v?.vendorEnglishName || o?.vendorEnglishName,
    { masked: maskPurchaseSensitiveFields.value }
  )
})

function resolveAuditCurrencyLabel(detail: any): string {
  if (!detail) return '—'
  const raw = detail.currency ?? detail.tradeCurrency ?? detail.TradeCurrency
  if (raw == null || raw === '') return '—'
  const n = Number(raw)
  if (!Number.isFinite(n)) return '—'
  return CURRENCY_CODE_TO_TEXT[n] || String(n)
}

const auditCustomerCurrencyLabel = computed(() => resolveAuditCurrencyLabel(auditCustomerSource.value))
const auditVendorCurrencyLabel = computed(() => resolveAuditCurrencyLabel(auditVendorSource.value))

function resolveAuditVendorPaymentDays(detail: any): number | null {
  if (!detail) return null
  const raw = detail.payment ?? detail.paymentDays ?? detail.Payment
  if (raw == null || raw === '') return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
}

const auditVendorPaymentLabel = computed(() => {
  const n = resolveAuditVendorPaymentDays(auditVendorSource.value)
  return n == null ? '—' : String(n)
})

const auditVendorPaymentHighlight = computed(() => {
  const n = resolveAuditVendorPaymentDays(auditVendorSource.value)
  return n != null && n > 0
})

const auditVendorPaymentTermTypeLabel = computed(() => {
  const n = resolveAuditVendorPaymentDays(auditVendorSource.value)
  if (n == null) return '—'
  return n <= 0
    ? t('pendingApprovals.vendor.paymentTermCash')
    : t('pendingApprovals.vendor.paymentTermCredit')
})

const auditVendorIsCreditTerm = computed(() => {
  const n = resolveAuditVendorPaymentDays(auditVendorSource.value)
  return n != null && n > 0
})

const auditVendorIdentityLabel = computed(() => {
  const d = auditVendorSource.value
  if (!d) return '—'
  const credit = d.credit ?? d.Credit
  const label = vendorDict.identityLabel(credit == null ? 0 : Number(credit))
  return !label || label === '--' ? '—' : label
})

const auditVendorPaymentMethodLabel = computed(() => {
  const d = auditVendorSource.value
  if (!d) return '—'
  const raw = d.paymentMethod ?? d.PaymentMethod
  if (raw == null || raw === '') return '—'
  const label = vendorDict.paymentLabel(String(raw))
  return !label || label === '--' ? String(raw) : label
})

const auditVendorPurchaserLabel = computed(() => {
  const d = auditVendorSource.value
  if (!d) return '—'
  const name = String(d.purchaseUserName ?? d.PurchaseUserName ?? d.purchaserName ?? '').trim()
  if (name && !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(name)) {
    return name
  }
  return '—'
})

const getBizTypeText = (type: string) => {
  const key = `pendingApprovals.bizType.${type}` as const
  return te(key) ? t(key) : type
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

const auditMoneyCurrency = (item: PendingApprovalItem, detail: any) =>
  pickDefinedNumber(item.currency) ??
  pickDefinedNumber(detail?.paymentCurrency) ??
  pickDefinedNumber(detail?.PaymentCurrency) ??
  1

/** 付款请款：主数据为 PaymentAmountToBe，待审核阶段 PaymentAmount 多为 0 */
function resolveFinancePaymentAuditAmount(item: PendingApprovalItem, detail: any): number | undefined {
  const d = detail || {}
  return (
    pickDefinedNumber(d.paymentAmountToBe ?? d.PaymentAmountToBe) ??
    pickDefinedNumber(d.paymentAmount ?? d.PaymentAmount) ??
    pickDefinedNumber(item.amount)
  )
}

const auditFinancePaymentCurrency = computed(() =>
  props.row ? auditMoneyCurrency(props.row, auditDetail.value) : 1
)

const auditFinancePaymentAmountText = computed(() => {
  if (!props.row) return '—'
  const amount = resolveFinancePaymentAuditAmount(props.row, auditDetail.value)
  return amount === undefined ? '—' : formatTotalAmountNumber(amount)
})

const auditSalesOrderItems = computed(() => {
  const d = auditDetail.value
  const items = d?.items ?? d?.Items
  return Array.isArray(items) ? items : null
})

const auditPurchaseOrderItems = computed(() => {
  const d = auditDetail.value
  const items = d?.items ?? d?.Items
  return Array.isArray(items) ? items : null
})

const auditPaymentModeLabel = computed(() => {
  const raw = auditDetail.value?.paymentMode ?? auditDetail.value?.PaymentMode
  if (raw == null || raw === '') return '—'
  const n = Number(raw)
  if (!Number.isFinite(n)) return '—'
  return paymentModeLabel(n) || '—'
})

function resolveAuditHeaderAmount(item: PendingApprovalItem | null, detail: any): number | undefined {
  if (!item) return undefined
  if (item.bizType === 'FINANCE_PAYMENT') return resolveFinancePaymentAuditAmount(item, detail)
  if (item.bizType === 'SALES_ORDER' || item.bizType === 'PURCHASE_ORDER') {
    return (
      pickDefinedNumber(detail?.total) ??
      pickDefinedNumber(detail?.Total) ??
      pickDefinedNumber(item.amount)
    )
  }
  if (item.bizType === 'FINANCE_RECEIPT') {
    return (
      pickDefinedNumber(detail?.receiptAmount) ??
      pickDefinedNumber(detail?.ReceiptAmount) ??
      pickDefinedNumber(item.amount)
    )
  }
  return pickDefinedNumber(item.amount)
}

function resolveAuditHeaderCurrency(item: PendingApprovalItem | null, detail: any): number | undefined {
  if (!item) return undefined
  if (item.bizType === 'FINANCE_PAYMENT') return auditMoneyCurrency(item, detail)
  if (item.bizType === 'FINANCE_RECEIPT') {
    return (
      pickDefinedNumber(item.currency) ??
      pickDefinedNumber(detail?.receiptCurrency) ??
      pickDefinedNumber(detail?.ReceiptCurrency)
    )
  }
  return (
    pickDefinedNumber(item.currency) ??
    pickDefinedNumber(detail?.currency) ??
    pickDefinedNumber(detail?.Currency)
  )
}

const auditHeaderAmountMasked = computed(() => {
  const item = props.row
  if (!item) return false
  return (
    maskSaleSensitiveFields.value &&
    (item.bizType === 'SALES_ORDER' || item.bizType === 'FINANCE_RECEIPT')
  )
})

const auditHeaderAmountText = computed(() => {
  if (auditHeaderAmountMasked.value) return '—'
  const amount = resolveAuditHeaderAmount(props.row, auditDetail.value)
  return amount === undefined ? '—' : formatTotalAmountNumber(amount)
})

const auditHeaderCurrency = computed(() => resolveAuditHeaderCurrency(props.row, auditDetail.value))

const auditHeaderCurrencyIso = computed(() => {
  if (auditHeaderAmountMasked.value || auditHeaderAmountText.value === '—') return ''
  const ccy = auditHeaderCurrency.value
  return ccy == null ? '' : listAmountCurrencyIso(ccy)
})

function displayCounterpartyName(item: PendingApprovalItem): string {
  const bt = String(item.bizType || '')
  if (maskPurchaseSensitiveFields.value && (bt === 'VENDOR' || bt === 'PURCHASE_ORDER' || bt === 'FINANCE_PAYMENT'))
    return '—'
  if (maskSaleSensitiveFields.value && (bt === 'CUSTOMER' || bt === 'SALES_ORDER' || bt === 'FINANCE_RECEIPT')) return '—'
  return item.counterpartyName || '—'
}

const getDetailRoute = (item: PendingApprovalItem) => {
  const id = item.businessId
  switch (item.bizType) {
    case 'SALES_ORDER':
      return { name: 'SalesOrderDetail', params: { id } }
    case 'VENDOR':
      return { name: 'VendorDetail', params: { id } }
    case 'CUSTOMER':
      return { name: 'CustomerDetail', params: { id } }
    case 'FINANCE_RECEIPT':
      return { name: 'FinanceReceiptDetail', params: { id } }
    case 'FINANCE_PAYMENT':
      return { name: 'FinancePaymentDetail', params: { id } }
    case 'PURCHASE_ORDER':
      return { name: 'PurchaseOrderDetail', params: { id } }
    default:
      return null
  }
}

const handleViewInNewTab = (item: PendingApprovalItem) => {
  const route = getDetailRoute(item)
  if (!route) {
    ElMessage.warning(t('pendingApprovals.messages.jumpNotSupported'))
    return
  }
  const resolved = router.resolve(route)
  window.open(resolved.href, '_blank', 'noopener,noreferrer')
}

const normalizeApiData = <T = any>(res: any): T => (res?.data ?? res) as T

function emitPartyContext(item: PendingApprovalItem | null) {
  if (!item) {
    emit('context', null)
    return
  }
  const d = auditDetail.value
  const c = auditRelatedCustomer.value
  const v = auditRelatedVendor.value
  const orderItems = Array.isArray(d?.items) ? d.items : Array.isArray(d?.Items) ? d.Items : []

  let customerId: string | null = null
  let customerName: string | null = null
  let vendorId: string | null = null
  let vendorName: string | null = null

  if (item.bizType === 'CUSTOMER') {
    customerId = String(item.businessId || '').trim() || null
    customerName = String(d?.customerName || d?.officialName || item.counterpartyName || '').trim() || null
  } else if (item.bizType === 'VENDOR') {
    vendorId = String(item.businessId || '').trim() || null
    vendorName = String(d?.officialName || d?.vendorName || item.counterpartyName || '').trim() || null
  } else if (item.bizType === 'SALES_ORDER') {
    customerId = String(d?.customerId ?? d?.CustomerId ?? c?.id ?? '').trim() || null
    customerName = String(
      c?.customerName || c?.officialName || d?.customerName || item.counterpartyName || ''
    ).trim() || null
  } else if (item.bizType === 'PURCHASE_ORDER') {
    vendorId = String(d?.vendorId ?? d?.VendorId ?? v?.id ?? '').trim() || null
    vendorName = String(
      v?.officialName || v?.vendorName || d?.vendorName || item.counterpartyName || ''
    ).trim() || null
  } else if (item.bizType === 'FINANCE_RECEIPT') {
    customerId = String(d?.customerId ?? d?.CustomerId ?? '').trim() || null
    customerName = String(d?.customerName || item.counterpartyName || '').trim() || null
  } else if (item.bizType === 'FINANCE_PAYMENT') {
    vendorId = String(d?.vendorId ?? d?.VendorId ?? v?.id ?? '').trim() || null
    vendorName = String(
      v?.officialName || v?.vendorName || d?.vendorName || item.counterpartyName || ''
    ).trim() || null
  }

  emit('context', {
    bizType: item.bizType,
    businessId: item.businessId,
    customerId,
    customerName,
    vendorId,
    vendorName,
    orderItems
  })
}

const loadAuditDetail = async (item: PendingApprovalItem) => {
  auditDetailLoading.value = true
  auditDetailError.value = ''
  auditDetail.value = null
  auditRelatedVendor.value = null
  auditRelatedCustomer.value = null
  emit('context', null)
  try {
    if (item.bizType === 'VENDOR') {
      auditDetail.value = normalizeApiData(await vendorApi.getVendorById(item.businessId))
      await vendorDict.hydrateVendorEditForm({
        credit: auditDetail.value?.credit ?? auditDetail.value?.Credit,
        paymentMethod: auditDetail.value?.paymentMethod ?? ''
      })
    } else if (item.bizType === 'CUSTOMER') {
      auditDetail.value = normalizeApiData(await customerApi.getCustomerById(item.businessId))
      const type = auditDetail.value?.customerType ?? auditDetail.value?.type ?? auditDetail.value?.Type
      await customerDict.hydrateCustomerEditForm({ customerType: type == null ? undefined : Number(type) })
    } else if (item.bizType === 'SALES_ORDER') {
      auditDetail.value = normalizeApiData(await salesOrderApi.getById(item.businessId))
      const customerId = String(auditDetail.value?.customerId ?? auditDetail.value?.CustomerId ?? '').trim()
      if (customerId) {
        try {
          auditRelatedCustomer.value = normalizeApiData(await customerApi.getCustomerById(customerId))
          const type =
            auditRelatedCustomer.value?.customerType ??
            auditRelatedCustomer.value?.type ??
            auditRelatedCustomer.value?.Type
          await customerDict.hydrateCustomerEditForm({
            customerType: type == null ? undefined : Number(type)
          })
        } catch {
          auditRelatedCustomer.value = null
        }
      }
    } else if (item.bizType === 'FINANCE_RECEIPT') {
      auditDetail.value = normalizeApiData(await financeReceiptApi.getById(item.businessId))
    } else if (item.bizType === 'FINANCE_PAYMENT') {
      auditDetail.value = normalizeApiData(await financePaymentApi.getById(item.businessId))
      const vendorId = String(auditDetail.value?.vendorId ?? auditDetail.value?.VendorId ?? '').trim()
      if (vendorId) {
        try {
          auditRelatedVendor.value = normalizeApiData(await vendorApi.getVendorById(vendorId))
          await vendorDict.hydrateVendorEditForm({
            credit: auditRelatedVendor.value?.credit ?? auditRelatedVendor.value?.Credit,
            paymentMethod: auditRelatedVendor.value?.paymentMethod ?? ''
          })
        } catch {
          auditRelatedVendor.value = null
        }
      }
    } else if (item.bizType === 'PURCHASE_ORDER') {
      auditDetail.value = normalizeApiData(await purchaseOrderApi.getById(item.businessId))
      const vendorId = String(auditDetail.value?.vendorId ?? auditDetail.value?.VendorId ?? '').trim()
      if (vendorId) {
        try {
          auditRelatedVendor.value = normalizeApiData(await vendorApi.getVendorById(vendorId))
          await vendorDict.hydrateVendorEditForm({
            credit: auditRelatedVendor.value?.credit ?? auditRelatedVendor.value?.Credit,
            paymentMethod: auditRelatedVendor.value?.paymentMethod ?? ''
          })
        } catch {
          auditRelatedVendor.value = null
        }
      }
    }
    emitPartyContext(item)
  } catch (e: any) {
    auditDetailError.value = e?.message || t('pendingApprovals.messages.detailLoadFailed')
    emit('context', null)
  } finally {
    auditDetailLoading.value = false
  }
}

const loadAuditDocs = async (item: PendingApprovalItem) => {
  auditDocsLoading.value = true
  try {
    if (isAuditAttachmentsRestricted(item)) {
      auditDocs.value = []
      return
    }
    auditDocs.value = await documentApi.getDocuments(item.bizType, item.businessId)
  } catch {
    auditDocs.value = []
  } finally {
    auditDocsLoading.value = false
  }
}

const auditVendorNameLabel = (detail: any, fallback?: string) =>
  formatVendorNameReadonly(
    detail?.officialName || detail?.vendorName || fallback,
    detail?.englishOfficialName || detail?.vendorEnglishName,
    { masked: maskPurchaseSensitiveFields.value }
  )

const auditCustomerNameLabel = (detail: any, fallback?: string) =>
  formatCustomerNameReadonly(
    detail?.customerName || detail?.officialName || fallback,
    detail?.englishOfficialName || detail?.customerEnglishName,
    { masked: maskSaleSensitiveFields.value }
  )

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

const loadAuditHistory = async (item: PendingApprovalItem) => {
  auditHistoryLoading.value = true
  try {
    const list = await approvalsApi.getApprovalHistory({ bizType: item.bizType, businessId: item.businessId })
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

const statusText = (status: number) => {
  if (status === 2 || status === 1) return t('pendingApprovals.rowStatus.pending')
  if (status === 10 || status === 20 || status === 3) return t('pendingApprovals.rowStatus.passed')
  if (status < 0 || status === 4 || status === 5) return t('pendingApprovals.rowStatus.rejected')
  return String(status)
}

async function afterDecide(decision: 'approve' | 'reject') {
  emit('decided', { decision })
  if (!props.embedded) {
    emit('close')
  }
}

const handleApprove = async () => {
  if (!props.row || props.readOnly) return
  try {
    actionLoading.value = true
    await approvalsApi.decidePendingApproval({
      bizType: props.row.bizType,
      businessId: props.row.businessId,
      decision: 'approve',
      remark: auditRemark.value.trim() || undefined
    })
    ElMessage.success(t('pendingApprovals.messages.approveSuccess'))
    await afterDecide('approve')
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('pendingApprovals.messages.operationFailed'))
  } finally {
    actionLoading.value = false
  }
}

const handleReject = async () => {
  if (!props.row || props.readOnly) return
  const needReason = ['SALES_ORDER', 'PURCHASE_ORDER', 'CUSTOMER', 'VENDOR', 'FINANCE_PAYMENT'].includes(
    props.row.bizType
  )
  if (needReason && !auditRemark.value.trim()) {
    ElMessage.warning(t('pendingApprovals.messages.rejectReasonRequired'))
    return
  }
  try {
    actionLoading.value = true
    await approvalsApi.decidePendingApproval({
      bizType: props.row.bizType,
      businessId: props.row.businessId,
      decision: 'reject',
      remark: auditRemark.value.trim() || undefined
    })
    ElMessage.success(t('pendingApprovals.messages.rejectSuccess'))
    await afterDecide('reject')
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('pendingApprovals.messages.operationFailed'))
  } finally {
    actionLoading.value = false
  }
}

watch(
  () => (props.row ? `${props.row.bizType}:${props.row.businessId}` : ''),
  () => {
    const item = props.row
    if (!item) return
    auditRemark.value = ''
    loadAuditDetail(item)
    loadAuditDocs(item)
    loadAuditHistory(item)
  },
  { immediate: true }
)
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.audit-dialog {
  /* 与待审批「审核窗口」弹窗一致：13px 正文、标签弱色、值常规字重 */
  font-family: 'Noto Sans SC', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  font-size: 13px;
  font-weight: 400;
  line-height: 1.5;
  color: $text-primary;

  &.audit-dialog--embedded {
    height: 100%;
    min-height: 0;
    overflow: auto;
  }

  .audit-top {
    border: 1px solid rgba(0, 212, 255, 0.15);
    border-radius: 10px;
    padding: 12px 14px;
    margin-bottom: 12px;
    background: rgba(0, 212, 255, 0.03);
  }

  .audit-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
  }

  .submit-remark {
    font-size: 13px;
    font-weight: 400;
    color: $text-secondary;
    white-space: pre-wrap;
    line-height: 1.5;
  }

  :deep(.el-form-item__label) {
    font-size: 13px;
    font-weight: 400;
    color: $text-secondary;
  }

  :deep(.el-textarea__inner) {
    font-family: inherit;
    font-size: 13px;
    font-weight: 400;
  }

  :deep(.el-button) {
    font-size: 13px;
    font-weight: 500;
  }

  .audit-attachments {
    margin-top: 10px;
    border-top: 1px dashed rgba(255, 255, 255, 0.12);
    padding-top: 10px;
  }

  .attach-header {
    display: flex;
    align-items: center;
    font-size: 13px;
    font-weight: 400;
    color: $text-primary;
    margin-bottom: 8px;
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
    font-size: 13px;
  }

  .attach-item .name {
    color: $text-secondary;
    max-width: 70%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .audit-business {
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 10px;
    padding: 12px 14px;
    margin-bottom: 12px;
    background: rgba(255, 255, 255, 0.01);
  }

  .audit-history {
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 10px;
    padding: 12px 14px;
    background: rgba(255, 255, 255, 0.01);
  }

  .section-title {
    color: $text-primary;
    font-size: 14px;
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
    font-size: 13px;
    font-weight: 400;
  }

  .k {
    color: $text-muted;
    font-size: 13px;
    font-weight: 400;
    width: 70px;
    flex-shrink: 0;
  }

  .v {
    color: $text-primary;
    font-size: 13px;
    font-weight: 400;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .biz-extra {
    margin-top: 12px;
    padding: 12px 14px;
    border-radius: 8px;
    border: 1px solid rgba(230, 180, 40, 0.28);
    background: #fff8e1;

    &--stacked {
      padding: 0;
      border: none;
      background: transparent;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
  }

  .extra-panel {
    padding: 12px 14px;
    border-radius: 8px;

    &--customer {
      border: 1px solid rgba(230, 180, 40, 0.28);
      background: #fff8e1;
    }
  }

  .detail-loading {
    font-size: 13px;
    font-weight: 400;
    color: $text-muted;
    margin-bottom: 8px;
  }

  .detail-error {
    font-size: 13px;
    font-weight: 400;
    color: #ef6a73;
    margin-bottom: 8px;
  }

  .extra-title {
    color: $text-primary;
    font-size: 14px;
    font-weight: 600;
    margin-bottom: 8px;
  }

  .extra-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 6px 14px;
  }

  .extra-line {
    /* 值：正文色常规字重；首段 label span：弱色（对齐审核弹窗） */
    color: $text-primary;
    font-size: 13px;
    font-weight: 400;
    margin-bottom: 6px;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    > span:not(.amount-with-code):not(.dock-tier-ccy):not(.extra-value) {
      color: $text-muted;
      font-weight: 400;
    }
    .amount-with-code > span:not(.dock-tier-ccy) {
      color: $text-primary;
      font-weight: 400;
    }
  }

  .extra-grid .extra-line {
    margin-bottom: 0;
  }

  .extra-grid .extra-line--span {
    grid-column: 1 / -1;
    white-space: normal;
    overflow: visible;
    text-overflow: unset;
  }

  .extra-divider {
    grid-column: 1 / -1;
    height: 0;
    margin: 6px 0;
    border: 0;
    border-top: 1px solid rgba(180, 140, 40, 0.45);
  }

  .extra-value {
    font-style: normal;
    font-size: 13px;
    font-weight: 400;
    color: $text-primary;
  }

  .extra-value--warn {
    color: #8b4513;
  }

  .amount-with-code {
    display: inline-flex;
    align-items: baseline;
    gap: 6px;
    font-size: 13px;
  }

  .amount-with-code__num {
    font-weight: 600;
  }

  .order-files-panel {
    margin-top: 12px;
    padding: 10px 12px;
    border-radius: 8px;
    background: rgba(245, 248, 252, 0.95);
    border: 1px solid rgba(64, 128, 200, 0.18);

    .extra-title {
      margin-bottom: 8px;
    }

    &__restricted {
      padding: 10px 4px;
      font-size: 13px;
      color: $text-muted;
      text-align: center;
    }

    :deep(.document-list-panel) {
      max-height: 180px;
      overflow: auto;
    }

    :deep(.document-list-panel .list.list) {
      flex-direction: column;
      flex-wrap: nowrap;
      gap: 6px;
    }

    :deep(.document-list-panel .doc-card--list) {
      width: 100%;
      max-width: 100%;
    }

    :deep(.document-list-panel .doc-name) {
      max-width: min(280px, 45vw);
    }
  }

  .detail-jump {
    margin-top: 10px;
    display: flex;
    justify-content: flex-end;
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
    font-size: 13px;
    font-weight: 600;
  }

  .history-item .time,
  .history-item .line-2 {
    color: $text-muted;
    font-size: 12px;
    font-weight: 400;
  }

  .detail-jump :deep(.el-button) {
    font-size: 13px;
  }
}
</style>
