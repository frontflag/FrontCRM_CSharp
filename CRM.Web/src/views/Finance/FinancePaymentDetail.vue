<template>
  <div class="finance-payment-detail-page" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('financePaymentDetail.back') }}
        </button>
        <div v-if="detail" class="payment-caption-title-group">
          <div class="caption-avatar-lg">{{ paymentCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': detail.status === -2 }">
                  {{ t('financePaymentDetail.captionPrefix') }} {{ detail.financePaymentCode || '—' }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption payment-header-meta-row">
              <el-tag effect="dark" :type="paymentStatusTag(detail.status) as any" size="small">
                {{ paymentStatusLabel(detail.status) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div v-if="detail" class="header-right">
        <el-button v-if="canEditRequestDetail" type="primary" @click="openEditRequest">
          {{ t('financePaymentList.actions.editRequest') }}
        </el-button>
        <el-button v-if="canWithdrawDetail" @click="handleWithdraw">
          {{ t('financePaymentList.actions.withdraw') }}
        </el-button>
        <el-button v-if="canPayExecuteDetail" type="warning" @click="payDialogVisible = true">
          {{ t('financePaymentList.actions.pay') }}
        </el-button>
        <el-button v-if="canSubmitAuditDetail" type="warning" @click="handleSubmitAudit">
          {{ t('financePaymentList.actions.submitAudit') }}
        </el-button>
        <el-button v-if="canReverseVerificationDetail" type="warning" @click="handleReverseVerification">
          {{ t('financePaymentList.actions.reverseVerification') }}
        </el-button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financePaymentDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financePaymentDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ paymentBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financePaymentDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ paymentBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.vendor') }}</span>
              <span class="info-value">
                <el-tooltip :content="vendorDisplayName" placement="top" :disabled="vendorDisplayTooltipDisabled">
                  <span class="vendor-display-name">{{ vendorDisplayName }}</span>
                </el-tooltip>
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.amount') }}</span>
              <span class="info-value info-value--amount">
                {{ CURRENCY_MAP[detail.paymentCurrency] }} {{ formatAmount(detail.status === 100 ? detail.paymentAmount : (detail.paymentAmountToBe ?? detail.paymentAmount)) }}
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.mode') }}</span>
              <span class="info-value">{{ paymentModeLabel(detail.paymentMode) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.date') }}</span>
              <span class="info-value info-value--time">
                {{ detail.paymentDate ? formatDisplayDate(detail.paymentDate) : '—' }}
              </span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.bankSlip') }}</span>
              <span class="info-value">{{ reportCellText(detail.bankSlipNo) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.vendorReceivingBank') }}</span>
              <span class="info-value">{{ maskPurchaseSensitiveFields ? '—' : reportCellText(detail.vendorBankName) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.paymentBank') }}</span>
              <span class="info-value">{{ maskPurchaseSensitiveFields ? '—' : reportCellText(detail.paymentBankName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financePaymentDetail.labels.freightForwarderOrderNo') }}</span>
              <span class="info-value">{{ reportCellText(detail.freightForwarderOrderNo) }}</span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('financePaymentDetail.labels.requestRemark') }}</span>
              <span class="info-value">{{ reportCellText(detail.requestRemark) }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('financePaymentDetail.labels.remark') }}</span>
              <span class="info-value">{{ reportCellText(detail.remark) }}</span>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financePaymentList.editRequest.feeSection') }}</span>
            </div>
          </div>
          <div class="payment-fee-section-body">
            <PaymentFeeSection
              readonly
              :show-title="false"
              :model-value="detailFeeForm"
              :currency="detail.paymentCurrency"
            />
          </div>
        </div>

        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'items' }"
              @click="detailActiveTab = 'items'"
            >
              {{ t('financePaymentDetail.tabs.items') }}
              <span v-if="paymentLineRows.length" class="tab-count">{{ paymentLineRows.length }}</span>
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'documents' }"
              @click="detailActiveTab = 'documents'"
            >
              {{ t('financePaymentDetail.tabs.documents') }}
              <span v-if="relatedPurchaseOrders.length" class="tab-count">{{ relatedPurchaseOrders.length }}</span>
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'bankSlip' }"
              @click="detailActiveTab = 'bankSlip'"
            >
              {{ t('financePaymentDetail.tabs.bankSlip') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="detailActiveTab === 'items'" class="detail-items-table-wrap">
              <el-empty v-if="!paymentLineRows.length" :description="t('financePaymentDetail.noItems')" :image-size="80" />
              <CrmDataTable
                v-else
                :data="paymentLineRows"
                class="items-table detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column type="index" width="50" label="#" />
                <el-table-column
                  prop="purchaseOrderCode"
                  :label="t('financePaymentDetail.labels.poCode')"
                  min-width="160"
                  show-overflow-tooltip
                />
                <CrmCopyableTableColumn
                  prop="freightForwarderOrderNo"
                  :label="t('financePaymentDetail.labels.freightForwarderOrderNo')"
                  min-width="150"
                />
                <CrmCopyableTableColumn prop="pn" :label="t('financePaymentDetail.labels.pn')" min-width="150" />
                <CrmCopyableTableColumn prop="brand" :label="t('financePaymentDetail.labels.brand')" width="120" />
                <el-table-column prop="qty" :label="t('financePaymentDetail.labels.qty')" width="100" align="right" header-align="right">
                  <template #default="{ row }">{{ row.qty ?? '—' }}</template>
                </el-table-column>
                <el-table-column
                  prop="cost"
                  :label="t('financePaymentDetail.labels.unitPrice')"
                  width="130"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    {{ row.cost == null ? '—' : formatAmount(Number(row.cost)) }}
                  </template>
                </el-table-column>
                <el-table-column
                  prop="paymentAmountToBe"
                  :label="t('financePaymentDetail.labels.requestPaymentAmount')"
                  width="140"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">{{ formatAmount(Number(row.paymentAmountToBe ?? 0)) }}</template>
                </el-table-column>
                <el-table-column
                  prop="paymentAmount"
                  :label="t('financePaymentDetail.labels.paidAmount')"
                  width="130"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">{{ formatAmount(row.paymentAmount) }}</template>
                </el-table-column>
                <el-table-column prop="purchaseOrderCreateTime" :label="t('financePaymentDetail.labels.poCreatedAt')" width="170">
                  <template #default="{ row }">
                    {{ row.purchaseOrderCreateTime ? formatDisplayDateTime(row.purchaseOrderCreateTime) : '—' }}
                  </template>
                </el-table-column>
                <el-table-column
                  prop="purchaseOrderCreateUserName"
                  :label="t('financePaymentDetail.labels.creator')"
                  width="120"
                  show-overflow-tooltip
                />
                <el-table-column
                  prop="lineRemark"
                  :label="t('financePaymentDetail.labels.lineRemark')"
                  min-width="120"
                  show-overflow-tooltip
                />
                <el-table-column :label="t('financePaymentDetail.labels.verifyStatus')" width="120" align="center">
                  <template #default="{ row }">
                    <el-tag
                      effect="dark"
                      size="small"
                      :type="row.verificationStatus === 2 ? 'success' : row.verificationStatus === 1 ? 'warning' : 'info'"
                    >
                      {{ verificationStatusLabel(row.verificationStatus) }}
                    </el-tag>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>

            <div v-show="detailActiveTab === 'documents'">
              <el-alert
                v-if="maskPurchaseSensitiveFields"
                type="info"
                :closable="false"
                show-icon
                :title="t('common.crossSideAttachmentsRestricted')"
              />
              <template v-else>
                <el-empty
                  v-if="!relatedPurchaseOrders.length"
                  :description="t('financePaymentDetail.noRelatedPo')"
                  :image-size="80"
                />
                <template v-else>
                  <div v-if="relatedPurchaseOrders.length > 1" class="po-doc-toolbar">
                    <span class="po-doc-toolbar__label">{{ t('financePaymentDetail.selectPoForDocs') }}</span>
                    <el-select v-model="selectedPoIdForDocs" style="width: 220px">
                      <el-option v-for="po in relatedPurchaseOrders" :key="po.id" :label="po.code" :value="po.id" />
                    </el-select>
                    <router-link
                      v-if="selectedPoIdForDocs"
                      class="po-doc-toolbar__link"
                      :to="{ name: 'PurchaseOrderDetail', params: { id: selectedPoIdForDocs }, query: { tab: 'documents' } }"
                    >
                      {{ t('financePaymentDetail.openPoDocuments') }}
                    </router-link>
                  </div>
                  <div v-else class="po-doc-toolbar po-doc-toolbar--single">
                    <span class="po-doc-toolbar__label">{{
                      t('financePaymentDetail.poDocSource', { code: relatedPurchaseOrders[0].code })
                    }}</span>
                    <router-link
                      class="po-doc-toolbar__link"
                      :to="{ name: 'PurchaseOrderDetail', params: { id: relatedPurchaseOrders[0].id }, query: { tab: 'documents' } }"
                    >
                      {{ t('financePaymentDetail.openPoDocuments') }}
                    </router-link>
                  </div>
                  <DocumentListPanel
                    v-if="selectedPoIdForDocs"
                    biz-type="PURCHASE_ORDER"
                    :biz-id="selectedPoIdForDocs"
                    view-mode="list"
                    readonly
                  />
                </template>
              </template>
            </div>

            <div v-show="detailActiveTab === 'bankSlip'">
              <el-alert
                v-if="maskPurchaseSensitiveFields"
                type="info"
                :closable="false"
                show-icon
                :title="t('common.crossSideAttachmentsRestricted')"
              />
              <template v-else>
                <DocumentUploadPanel
                  v-if="paymentId"
                  biz-type="FINANCE_PAYMENT"
                  :biz-id="paymentId"
                  :max-files="20"
                  :max-size-mb="100"
                  @uploaded="paymentSlipDocListRef?.refresh()"
                />
                <DocumentListPanel
                  v-if="paymentId"
                  ref="paymentSlipDocListRef"
                  biz-type="FINANCE_PAYMENT"
                  :biz-id="paymentId"
                  view-mode="list"
                  style="margin-top: 16px"
                />
              </template>
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="t('financePaymentDetail.notFound')" />
    </div>

    <FinancePaymentRequestEditDialog v-model="editDialogVisible" :payment-id="paymentId" @success="fetchDetail" />
    <FinancePaymentPayDialog v-model="payDialogVisible" :payment="detail" @success="onPayDialogSuccess" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ElMessage, ElMessageBox } from 'element-plus'
import { financePaymentApi, CURRENCY_MAP, type FinancePayment } from '@/api/finance'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { vendorApi } from '@/api/vendor'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import FinancePaymentPayDialog from '@/components/Finance/FinancePaymentPayDialog.vue'
import FinancePaymentRequestEditDialog from '@/components/Finance/FinancePaymentRequestEditDialog.vue'
import PaymentFeeSection, { type PaymentFeeForm } from '@/components/Finance/PaymentFeeSection.vue'
import { useFinanceWriteGate, usePurchaseOrderWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { useAuthStore } from '@/stores/auth'
import { formatVendorNameReadonly } from '@/utils/vendorDisplayName'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const { paymentStatusLabel, paymentStatusTag, paymentModeLabel, verificationStatusLabel } = useFinanceEnumLabels()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const authStore = useAuthStore()
const { canWriteFinancePayment: canFinancePaymentWrite } = useFinanceWriteGate()
const { canWritePo } = usePurchaseOrderWriteGate()

const loading = ref(false)
const payDialogVisible = ref(false)
const editDialogVisible = ref(false)
const detail = ref<FinancePayment | null>(null)
const paymentLineRows = ref<any[]>([])
const vendorDisplayName = ref('—')
const detailActiveTab = ref<'items' | 'documents' | 'bankSlip'>('items')
const selectedPoIdForDocs = ref('')
const paymentSlipDocListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const paymentId = computed(() => route.params.id as string)

const paymentCaptionAvatarChar = computed(() => {
  const c = detail.value?.financePaymentCode?.trim()
  return c ? c[0]! : '付'
})

function paymentRowCreatedAt(row: FinancePayment | null): string {
  if (!row) return ''
  const v = row.createdAt ?? row.createTime
  return v != null && String(v).trim() !== '' ? String(v) : ''
}

const paymentBasicCreateDateText = computed(() => {
  const raw = paymentRowCreatedAt(detail.value)
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

const paymentBasicCreateUserText = computed(() => detail.value?.createUserName?.trim() || '—')

const vendorDisplayTooltipDisabled = computed(() => {
  if (maskPurchaseSensitiveFields.value) return true
  const text = vendorDisplayName.value
  return !text || text === '—' || text === '-'
})

const canPayExecuteDetail = computed(() =>
  !!detail.value && canFinancePaymentWrite.value && detail.value.status === 10
)

const canEditRequestDetail = computed(() => {
  if (!detail.value) return false
  if (detail.value.status !== 1 && detail.value.status !== -1) return false
  return canFinancePaymentWrite.value || canWritePo.value
})

const canSubmitAuditDetail = computed(() =>
  !!detail.value && detail.value.status === 1 && (canFinancePaymentWrite.value || canWritePo.value)
)

const canWithdrawDetail = computed(() => {
  if (!detail.value || detail.value.status !== 10) return false
  if (canFinancePaymentWrite.value) return true
  const uid = String(authStore.user?.id ?? '').trim()
  const creator = String(detail.value.createByUserId ?? '').trim()
  return !!uid && !!creator && uid === creator
})

const canReverseVerificationDetail = computed(
  () => !!detail.value && canFinancePaymentWrite.value && detail.value.status === 100
)

function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

function openEditRequest() {
  editDialogVisible.value = true
}

async function handleWithdraw() {
  if (!detail.value) return
  try {
    await ElMessageBox.confirm(
      t('financePaymentList.messages.withdrawMsg', { code: detail.value.financePaymentCode }),
      t('financePaymentList.messages.withdrawTitle'),
      { type: 'warning' }
    )
  } catch {
    return
  }
  try {
    await financePaymentApi.withdraw(detail.value.id)
    ElMessage.success(t('financePaymentList.messages.withdrawn'))
    await fetchDetail()
    paymentSlipDocListRef.value?.refresh()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.withdrawFailed'))
  }
}

async function handleReverseVerification() {
  if (!detail.value) return
  const code = detail.value.financePaymentCode || ''
  const entered = window.prompt(t('financePaymentList.messages.reverseVerificationPrompt'), code)?.trim() ?? ''
  if (!entered) return
  if (entered !== String(code).trim()) {
    ElMessage.error(t('financePaymentList.messages.reverseVerificationBillMismatch'))
    return
  }
  try {
    await financePaymentApi.reverseVerification(detail.value.id, entered)
    ElMessage.success(t('financePaymentList.messages.reverseVerificationSuccess'))
    await fetchDetail()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.reverseVerificationFailed'))
  }
}

async function handleSubmitAudit() {
  if (!detail.value) return
  try {
    await ElMessageBox.confirm(
      t('financePaymentList.messages.submitAuditMsg', { code: detail.value.financePaymentCode }),
      t('financePaymentList.messages.submitAuditTitle'),
      { type: 'info' }
    )
  } catch {
    return
  }
  try {
    await financePaymentApi.submit(detail.value.id)
    ElMessage.success(t('financePaymentList.messages.submitted'))
    await fetchDetail()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.operationFailed'))
  }
}

const onPayDialogSuccess = async () => {
  await fetchDetail()
  paymentSlipDocListRef.value?.refresh()
}

const relatedPurchaseOrders = computed(() => {
  const map = new Map<string, { id: string; code: string }>()
  for (const row of paymentLineRows.value) {
    const id = String(row?.purchaseOrderId || '').trim()
    if (!id) continue
    const code = String(row?.purchaseOrderCode || id)
    if (!map.has(id)) map.set(id, { id, code })
  }
  return Array.from(map.values())
})

watch(relatedPurchaseOrders, (list) => {
  if (!list.length) {
    selectedPoIdForDocs.value = ''
    return
  }
  const current = selectedPoIdForDocs.value
  if (!current || !list.some((po) => po.id === current)) {
    selectedPoIdForDocs.value = list[0].id
  }
}, { immediate: true })

watch(maskPurchaseSensitiveFields, (masked) => {
  if (masked && (detailActiveTab.value === 'documents' || detailActiveTab.value === 'bankSlip')) {
    detailActiveTab.value = 'items'
  }
})

const detailFeeForm = computed<PaymentFeeForm>(() => {
  const d = detail.value
  if (!d) {
    return {
      intermediateBankFee: 0,
      bankCharge: 0,
      freight: 0,
      miscFee: 0,
      rounding: 0,
      intermediateBankFeePayer: '我方',
    }
  }
  return {
    intermediateBankFee: Number(d.feeIntermediateBank ?? 0),
    bankCharge: Number(d.feeBankCharge ?? 0),
    freight: Number(d.feeFreight ?? 0),
    miscFee: Number(d.feeMisc ?? 0),
    rounding: Number(d.feeRounding ?? 0),
    intermediateBankFeePayer: d.feeIntermediateBankPayer === '供应商' ? '供应商' : '我方',
  }
})

onMounted(() => {
  void fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    detail.value = await financePaymentApi.getById(paymentId.value)
    await buildPaymentLineRows()
    await resolveVendorDisplayName()
  } catch {
    detail.value = null
    paymentLineRows.value = []
    vendorDisplayName.value = '—'
  } finally {
    loading.value = false
  }
}

const buildPaymentLineRows = async () => {
  const current = detail.value
  const rows = current?.items ?? []
  if (!rows.length) {
    paymentLineRows.value = []
    return
  }

  const poIds = Array.from(new Set(rows.map((x: any) => String(x?.purchaseOrderId || '').trim()).filter(Boolean)))
  const poMap = new Map<string, any>()
  if (poIds.length) {
    const results = await Promise.allSettled(poIds.map((id) => purchaseOrderApi.getById(id)))
    results.forEach((r, idx) => {
      if (r.status !== 'fulfilled') return
      poMap.set(poIds[idx], r.value)
    })
  }

  paymentLineRows.value = rows.map((item: any) => {
    const po = poMap.get(String(item?.purchaseOrderId || '').trim()) || {}
    const poItems: any[] = Array.isArray(po?.items) ? po.items : []
    const matchedItem = poItems.find((x: any) => String(x?.id || '') === String(item?.purchaseOrderItemId || '')) || {}
    return {
      ...item,
      purchaseOrderId: String(item?.purchaseOrderId || po?.id || '').trim(),
      purchaseOrderCode: po?.purchaseOrderCode || po?.PurchaseOrderCode || item?.purchaseOrderCode || '—',
      freightForwarderOrderNo:
        po?.freightForwarderOrderNo || po?.FreightForwarderOrderNo || item?.freightForwarderOrderNo || '—',
      qty: matchedItem?.qty ?? matchedItem?.Qty ?? null,
      cost: matchedItem?.cost ?? matchedItem?.Cost ?? null,
      purchaseOrderCreateTime: po?.createTime || po?.CreateTime || null,
      purchaseOrderCreateUserName: po?.createUserName || po?.createdBy || po?.purchaseUserName || '—'
    }
  })
}

const resolveVendorDisplayName = async () => {
  const current = detail.value
  if (!current) {
    vendorDisplayName.value = '—'
    return
  }
  if (maskPurchaseSensitiveFields.value) {
    vendorDisplayName.value = '—'
    return
  }

  const rawName = String(current.vendorName || '').trim()
  const rawEn = String(current.vendorEnglishName || '').trim()
  const rawId = String(current.vendorId || '').trim()
  const pickVendorNameZh = (v: any) =>
    v?.officialName ||
    v?.OfficialName ||
    v?.nickName ||
    v?.NickName ||
    v?.name ||
    v?.Name ||
    v?.vendorName ||
    v?.VendorName ||
    ''
  const pickVendorNameEn = (v: any) => v?.englishOfficialName || v?.EnglishOfficialName || ''
  const pickVendorCode = (v: any) => String(v?.code || v?.Code || '').trim()
  const setDisplay = (zh: string, en?: string) => {
    vendorDisplayName.value = formatVendorNameReadonly(zh, en || rawEn)
  }

  const looksLikeCode = (value: string) => /^VEN[\w-]*$/i.test(value)
  if (rawName && !looksLikeCode(rawName)) {
    setDisplay(rawName)
    return
  }

  if (rawName && looksLikeCode(rawName)) {
    try {
      const pageByCode = await vendorApi.searchVendors({ pageNumber: 1, pageSize: 20, keyword: rawName })
      const exactByCode = pageByCode?.items?.find((x: any) => pickVendorCode(x).toUpperCase() === rawName.toUpperCase())
      const matched = exactByCode || pageByCode?.items?.[0]
      const nameByCode = pickVendorNameZh(matched)
      if (nameByCode) {
        setDisplay(nameByCode, pickVendorNameEn(matched))
        return
      }
    } catch {
      // ignored
    }
  }

  if (!rawId) {
    vendorDisplayName.value = formatVendorNameReadonly(rawName, rawEn) || '—'
    return
  }

  try {
    const v = await vendorApi.getVendorById(rawId)
    setDisplay(pickVendorNameZh(v) || rawName || rawId, pickVendorNameEn(v))
    return
  } catch {
    // ignored
  }

  try {
    const page = await vendorApi.searchVendors({ pageNumber: 1, pageSize: 20, keyword: rawId })
    const first = page?.items?.[0]
    setDisplay(pickVendorNameZh(first) || rawName || rawId, pickVendorNameEn(first))
  } catch {
    vendorDisplayName.value = formatVendorNameReadonly(rawName || rawId, rawEn)
  }
}

const formatAmount = (val: number) => {
  if (val == null) return '—'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function goBack() {
  router.push({ name: 'FinancePaymentList' })
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.finance-payment-detail-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.payment-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;

  &--muted {
    opacity: 0.55;
  }
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.title-meta--caption {
  margin-top: 4px;
}

.payment-header-meta-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 28px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 20px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-left {
  min-width: 0;
}

.header-right {
  flex-shrink: 0;
}

.detail-content {
  min-height: 200px;
}

.payment-fee-section-body {
  padding: 4px 20px 12px;

  :deep(.payment-fee-section .el-form-item) {
    margin-bottom: 12px;
  }

  :deep(.payment-fee-section .el-form-item__label) {
    font-size: 12px;
    color: $text-muted;
  }
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);

  .section-title {
    margin: 0;
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
  }
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-shrink: 0;
  margin-left: auto;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;

  &__label {
    color: $text-muted;

    &::after {
      content: '：';
    }
  }

  &__value {
    color: $text-secondary;
  }
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 5px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
  border-right: 1px solid rgba(255, 255, 255, 0.04);

  &:nth-child(3n) {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
    text-transform: none;
    letter-spacing: 0;
    font-size: 12px;

    &::after {
      content: '：';
    }
  }

  .info-value {
    flex: 1;
    min-width: 0;
    word-break: break-word;
  }
}

.info-grid--basic {
  .info-item {
    &:nth-child(3n) {
      border-right: none;
    }
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-grid--inline-labels .info-item--span-all {
  grid-column: 1 / -1;
  border-right: none;
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;

  &--time {
    font-size: 12px;
    color: $text-muted;
  }

  &--amount {
    font-family: 'Noto Sans SC', sans-serif;
    font-variant-numeric: tabular-nums;
    color: $cyan-primary;
    font-weight: 600;
  }
}

.vendor-display-name {
  word-break: break-word;
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: var(--crm-detail-section-header-bg);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
  display: inline-flex;
  align-items: center;
  gap: 6px;

  &:hover {
    color: $text-secondary;
  }

  &--active {
    color: $cyan-primary;
    border-bottom-color: $cyan-primary;
  }
}

.tab-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
}

.po-doc-toolbar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 14px;
}

.po-doc-toolbar--single {
  justify-content: space-between;
}

.po-doc-toolbar__label {
  font-size: 13px;
  color: $text-secondary;
}

.po-doc-toolbar__link {
  font-size: 13px;
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}
</style>
