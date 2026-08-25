<template>
  <div class="finance-receipt-detail-page" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('financeReceiptDetail.back') }}
        </button>
        <div v-if="detail" class="receipt-caption-title-group">
          <div class="caption-avatar-lg">{{ receiptCaptionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title" :class="{ 'page-title--muted': detail.status === 4 }">
                  {{ t('financeReceiptDetail.captionPrefix') }} {{ detail.financeReceiptCode || '—' }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption receipt-header-meta-row">
              <el-tag effect="dark" :type="receiptStatusTag(detail.status) as any" size="small">
                {{ receiptStatusLabel(detail.status) }}
              </el-tag>
              <el-tag
                v-if="detail.receiptPurpose != null"
                effect="dark"
                size="small"
                :type="detail.receiptPurpose === 20 ? 'warning' : 'primary'"
              >
                {{ receiptPurposeLabel(detail.receiptPurpose) }}
              </el-tag>
              <el-tag
                v-if="detail.isFreightForwarderPayment"
                effect="dark"
                size="small"
                type="success"
              >
                {{ t('financeReceiptList.formFfPayment') }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div v-if="detail" class="header-right">
        <el-button
          v-if="canConfirmDetail"
          type="primary"
          @click="confirmReceiptDetail"
        >
          {{ t('financeReceiptList.actions.confirm') }}
        </el-button>
        <el-button
          v-if="canCancelDetail"
          type="danger"
          @click="cancelReceiptDetail"
        >
          {{ t('financeReceiptList.actions.cancel') }}
        </el-button>
        <el-button v-if="canReverseVerificationDetail" type="warning" @click="handleReverseVerification">
          {{ t('financeReceiptList.actions.reverseVerification') }}
        </el-button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceiptDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financeReceiptDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ receiptBasicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financeReceiptDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ receiptBasicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financeReceiptDetail.labels.customer') }}</span>
              <span class="info-value">{{ maskSaleSensitiveFields ? '—' : reportCellText(detail.customerName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceiptDetail.labels.amount') }}</span>
              <span class="info-value info-value--amount">{{
                maskSaleSensitiveFields
                  ? '—'
                  : `${CURRENCY_MAP[detail.receiptCurrency]} ${formatAmount(detail.receiptAmount)}`
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceiptDetail.labels.mode') }}</span>
              <span class="info-value">{{ paymentModeLabel(detail.receiptMode) }}</span>
            </div>
            <div v-if="detail.isFreightForwarderPayment" class="info-item">
              <span class="info-label">{{ t('financeFfPayableList.colFfCompany') }}</span>
              <span class="info-value">{{ detail.freightForwarderCompanyName || '—' }}</span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financeReceiptDetail.labels.date') }}</span>
              <span class="info-value info-value--time">{{
                detail.receiptDate ? formatDisplayDate(detail.receiptDate) : '—'
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeReceiptDetail.labels.bankSlip') }}</span>
              <span class="info-value">{{ reportCellText(detail.bankSlipNo) }}</span>
            </div>
            <div class="info-item info-item--basic-spacer" aria-hidden="true"></div>
          </div>
          <div class="info-grid info-grid--inline-labels">
            <div class="info-item info-item--span-all">
              <span class="info-label">{{ t('financeReceiptDetail.labels.remark') }}</span>
              <span class="info-value">{{ reportCellText(detail.remark) }}</span>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceiptDetail.tabs.bankSlip') }}</span>
              <span v-if="!maskSaleSensitiveFields && receiptDocs.length" class="section-count">{{ receiptDocs.length }}</span>
            </div>
          </div>
          <div v-if="maskSaleSensitiveFields" class="detail-items-table-wrap">
            <DetailListPanelEmpty size="low" :description="t('common.crossSideAttachmentsRestricted')" />
          </div>
          <div v-else class="detail-items-table-wrap">
            <CrmDataTable
              v-if="receiptDocs.length"
              :data="receiptDocs"
              embedded
              :border="false"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              class="items-table detail-panel-list-table"
              size="small"
              stripe
            >
              <el-table-column type="index" width="50" label="#" />
              <el-table-column
                prop="originalFileName"
                :label="t('financeReceiptDetail.labels.fileName')"
                min-width="260"
                show-overflow-tooltip
              />
              <el-table-column
                prop="remark"
                :label="t('financeReceiptDetail.labels.remark')"
                min-width="140"
                show-overflow-tooltip
              />
              <el-table-column prop="createTime" :label="t('financeReceiptDetail.labels.uploadTime')" width="170">
                <template #default="{ row }">
                  {{ row.createTime ? formatDisplayDateTime(row.createTime) : '—' }}
                </template>
              </el-table-column>
              <el-table-column
                :label="t('financeReceiptDetail.labels.actions')"
                :width="receiptDocsOpColWidth"
                :min-width="receiptDocsOpColMinWidth"
                fixed="right"
                align="center"
                class-name="op-col"
                label-class-name="op-col"
              >
                <template #header>
                  <div class="list-op-col-header--icon-only">
                    <button
                      type="button"
                      class="op-col-toggle-btn list-op-col-toggle"
                      :aria-label="receiptDocsOpColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
                      @click.stop="toggleReceiptDocsOpCol"
                    >
                      {{ receiptDocsOpColExpanded ? '>' : '<' }}
                    </button>
                  </div>
                </template>
                <template #default="{ row }">
                  <div @click.stop @dblclick.stop>
                    <div v-if="receiptDocsOpColExpanded" class="action-btns">
                      <el-button size="small" text type="primary" @click.stop="previewDoc(row)">{{
                        t('financeReceiptDetail.preview')
                      }}</el-button>
                      <el-button size="small" text type="primary" @click.stop="downloadDoc(row)">{{
                        t('financeReceiptDetail.download')
                      }}</el-button>
                    </div>
                    <el-dropdown v-else trigger="click" placement="bottom-end">
                      <div class="op-more-dropdown-trigger">
                        <button type="button" class="op-more-trigger">...</button>
                      </div>
                      <template #dropdown>
                        <el-dropdown-menu>
                          <el-dropdown-item @click.stop="previewDoc(row)">
                            <span class="op-more-item op-more-item--primary">{{ t('financeReceiptDetail.preview') }}</span>
                          </el-dropdown-item>
                          <el-dropdown-item divided @click.stop="downloadDoc(row)">
                            <span class="op-more-item op-more-item--primary">{{ t('financeReceiptDetail.download') }}</span>
                          </el-dropdown-item>
                        </el-dropdown-menu>
                      </template>
                    </el-dropdown>
                  </div>
                </template>
              </el-table-column>
            </CrmDataTable>
            <DetailListPanelEmpty v-else size="low" :description="t('financeReceiptDetail.noAttachments')" />
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceiptDetail.tabs.items') }}</span>
              <span v-if="detail.items?.length" class="section-count">{{ detail.items.length }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div class="detail-items-table-wrap">
              <el-empty v-if="!detail.items?.length" :description="t('financeReceiptDetail.noItems')" :image-size="80" />
              <CrmDataTable
                v-else
                :data="detail.items"
                class="items-table detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column type="index" width="50" label="#" />
                <el-table-column :label="t('financeReceiptDetail.labels.verifyStatus')" width="120" align="center">
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
                <el-table-column
                  prop="receiptConvertAmount"
                  :label="t('financeReceiptDetail.labels.receiptConvertAmount')"
                  width="160"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    {{ maskSaleSensitiveFields ? '—' : formatReceiptItemConvertAmountDisplay(row) }}
                  </template>
                </el-table-column>
                <el-table-column
                  prop="verifiedAmount"
                  :label="t('financeReceiptDetail.labels.verifiedAmount')"
                  width="140"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    {{ maskSaleSensitiveFields ? '—' : formatAmount(row.verifiedAmount) }}
                  </template>
                </el-table-column>
                <el-table-column
                  :label="t('financeReceiptDetail.labels.pendingVerifyAmount')"
                  width="120"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    {{ maskSaleSensitiveFields ? '—' : formatAmount(receiptItemPendingVerifyAmount(row)) }}
                  </template>
                </el-table-column>
                <el-table-column
                  prop="advancePoolAmount"
                  :label="t('financeReceiptDetail.labels.advancePoolAmount')"
                  width="160"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    {{ maskSaleSensitiveFields ? '—' : formatAmount(row.advancePoolAmount) }}
                  </template>
                </el-table-column>
                <el-table-column
                  prop="remark"
                  :label="t('financeReceiptDetail.labels.remark')"
                  min-width="120"
                  show-overflow-tooltip
                >
                  <template #default="{ row }">
                    {{ row.remark?.trim() || '—' }}
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeReceiptDetail.writeOffSection') }}</span>
              <span v-if="writeOffRecords.length" class="section-count">{{ writeOffRecords.length }}</span>
            </div>
          </div>
          <div class="detail-items-table-wrap">
            <CrmDataTable
              v-if="writeOffRecords.length"
              :data="writeOffRecords"
              embedded
              :border="false"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              class="items-table detail-panel-list-table"
              size="small"
              stripe
            >
              <el-table-column type="index" width="50" label="#" />
              <el-table-column :label="t('financeReceiptDetail.writeOffLabels.createTime')" width="170">
                <template #default="{ row }">
                  {{ row.createTime ? formatDisplayDateTime(row.createTime) : '—' }}
                </template>
              </el-table-column>
              <el-table-column :label="t('financeReceiptDetail.writeOffLabels.source')" width="100">
                <template #default="{ row }">
                  {{ writeOffSourceLabel(row.writeOffSource) }}
                </template>
              </el-table-column>
              <el-table-column
                prop="stockOutCode"
                :label="t('financeReceiptDetail.writeOffLabels.stockOutCode')"
                min-width="120"
                show-overflow-tooltip
              />
              <el-table-column
                prop="sellOrderCode"
                :label="t('financeReceiptDetail.writeOffLabels.sellOrderCode')"
                min-width="120"
                show-overflow-tooltip
              />
              <CrmCopyableTableColumn prop="pn" :label="t('financeReceiptDetail.writeOffLabels.pn')" min-width="120" />
              <CrmCopyableTableColumn prop="brand" :label="t('financeReceiptDetail.writeOffLabels.brand')" width="100" />
              <el-table-column
                :label="t('financeReceiptDetail.writeOffLabels.amount')"
                width="150"
                align="right"
                header-align="right"
              >
                <template #default="{ row }">
                  {{ maskSaleSensitiveFields ? '—' : formatWriteOffAmountDisplay(row) }}
                </template>
              </el-table-column>
              <el-table-column
                prop="operatorUserName"
                :label="t('financeReceiptDetail.writeOffLabels.operator')"
                width="110"
                show-overflow-tooltip
              />
            </CrmDataTable>
            <DetailListPanelEmpty v-else size="low" :description="t('financeReceiptDetail.noWriteOffs')" />
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="t('financeReceiptDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { financeReceiptApi, CURRENCY_MAP, isFinanceReceiptNew, isFinanceReceiptConfirmed, type FinanceReceipt, type FinanceReceiptItem, type FinanceReceiptWriteOffRecord } from '@/api/finance'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { useCustomerWorkspacePanelStore } from '@/stores/customerWorkspacePanel'

const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const route = useRoute()
const { t } = useI18n()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const customerWorkspacePanelStore = useCustomerWorkspacePanelStore()
customerWorkspacePanelStore.setSource('financeReceipt')
const { receiptStatusLabel, receiptStatusTag, paymentModeLabel, verificationStatusLabel } = useFinanceEnumLabels()
const { canWriteFinanceReceipt } = useFinanceWriteGate()

const loading = ref(false)
const detail = ref<FinanceReceipt | null>(null)
const receiptDocs = ref<UploadDocumentDto[]>([])
const writeOffRecords = ref<FinanceReceiptWriteOffRecord[]>([])

const receiptId = computed(() => String(route.params.id ?? '').trim())

useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'FinanceReceiptDetail',
  hasSelectedRow: () => !!customerWorkspacePanelStore.boundId,
  setRowOnly: () => {
    if (receiptId.value) customerWorkspacePanelStore.bind('financeReceipt', receiptId.value)
  },
  selectRow: async () => {
    if (!receiptId.value) return
    customerWorkspacePanelStore.bind('financeReceipt', receiptId.value)
    await customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  loadSelected: () => {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  },
  dataTabIds: ['r-customer']
})

function bindCustomerWorkspace() {
  const id = receiptId.value
  if (!id) {
    customerWorkspacePanelStore.clear()
    return
  }
  customerWorkspacePanelStore.bind('financeReceipt', id)
  if (
    workspaceLayout?.rightPanelVisible.value &&
    workspaceLayout.rightActiveTabId.value === 'r-customer'
  ) {
    void customerWorkspacePanelStore.load(t('customerWorkspace.loadFailed'))
  }
}

const canConfirmDetail = computed(
  () => !!detail.value && canWriteFinanceReceipt.value && isFinanceReceiptNew(detail.value.status)
)

const canCancelDetail = computed(() => {
  if (!detail.value || !canWriteFinanceReceipt.value) return false
  if (isFinanceReceiptNew(detail.value.status)) return true
  if (!isFinanceReceiptConfirmed(detail.value.status)) return false
  if (writeOffRecords.value.length > 0) return false
  const items = detail.value.items ?? []
  if (items.some(i => (i.verificationStatus ?? 0) > 0 || (i.verifiedAmount ?? 0) > 0)) return false
  return (detail.value.verificationStatus ?? 0) === 0
})

const canReverseVerificationDetail = computed(
  () => !!detail.value && canWriteFinanceReceipt.value && isFinanceReceiptConfirmed(detail.value.status)
)

const receiptCaptionAvatarChar = computed(() => {
  const c = detail.value?.financeReceiptCode?.trim()
  return c ? c[0]! : '收'
})

function receiptRowCreatedAt(row: FinanceReceipt | null): string {
  if (!row) return ''
  const r = row as FinanceReceipt & { createTime?: string }
  const v = r.createdAt ?? r.createTime
  return v != null && String(v).trim() !== '' ? String(v) : ''
}

const receiptBasicCreateDateText = computed(() => {
  const raw = receiptRowCreatedAt(detail.value)
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

const receiptBasicCreateUserText = computed(() => detail.value?.createUserName?.trim() || '—')

function receiptPurposeLabel(purpose?: number) {
  return purpose === 20 ? t('financeReceiptList.purposeAdvance') : t('financeReceiptList.purposeNormal')
}

function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

/** 《列表操作列规范》：银行水单附件表 */
const receiptDocsOpColExpanded = ref(false)
const RECEIPT_DOCS_OP_COL_COLLAPSED = 43
const RECEIPT_DOCS_OP_COL_EXPANDED = 173
const RECEIPT_DOCS_OP_COL_EXPANDED_MIN = 160
const receiptDocsOpColWidth = computed(() =>
  receiptDocsOpColExpanded.value ? RECEIPT_DOCS_OP_COL_EXPANDED : RECEIPT_DOCS_OP_COL_COLLAPSED
)
const receiptDocsOpColMinWidth = computed(() =>
  receiptDocsOpColExpanded.value ? RECEIPT_DOCS_OP_COL_EXPANDED_MIN : RECEIPT_DOCS_OP_COL_COLLAPSED
)
function toggleReceiptDocsOpCol() {
  receiptDocsOpColExpanded.value = !receiptDocsOpColExpanded.value
}

onMounted(() => {
  void fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    detail.value = await financeReceiptApi.getById(receiptId.value)
    await Promise.all([loadReceiptDocs(), loadWriteOffRecords()])
    bindCustomerWorkspace()
  } catch {
    detail.value = null
    receiptDocs.value = []
    writeOffRecords.value = []
    customerWorkspacePanelStore.clear()
  } finally {
    loading.value = false
  }
}

async function confirmReceiptDetail() {
  if (!detail.value) return
  await ElMessageBox.confirm(
    t('financeReceiptList.messages.confirmMsg', { code: detail.value.financeReceiptCode }),
    t('financeReceiptList.messages.confirmTitle'),
    { type: 'success' }
  )
  await financeReceiptApi.confirm(detail.value.id)
  ElMessage.success(t('financeReceiptList.messages.confirmed'))
  await fetchDetail()
}

async function cancelReceiptDetail() {
  if (!detail.value) return
  await ElMessageBox.confirm(
    t('financeReceiptList.messages.cancelMsg', { code: detail.value.financeReceiptCode }),
    t('financeReceiptList.messages.cancelTitle'),
    { type: 'warning' }
  )
  await financeReceiptApi.cancel(detail.value.id)
  ElMessage.success(t('financeReceiptList.messages.cancelled'))
  await fetchDetail()
}

async function handleReverseVerification() {
  if (!detail.value) return
  const code = detail.value.financeReceiptCode || ''
  const entered = window.prompt(t('financeReceiptList.messages.reverseVerificationPrompt'), code)?.trim() ?? ''
  if (!entered) return
  if (entered !== code.trim()) {
    ElMessage.error(t('financeReceiptList.messages.reverseVerificationBillMismatch'))
    return
  }
  try {
    await financeReceiptApi.reverseVerification(detail.value.id, entered)
    ElMessage.success(t('financeReceiptList.messages.reverseVerificationSuccess'))
    await fetchDetail()
  } catch (e: any) {
    ElMessage.error(e?.message || t('financeReceiptList.messages.reverseVerificationFailed'))
  }
}

const loadWriteOffRecords = async () => {
  if (!receiptId.value) {
    writeOffRecords.value = []
    return
  }
  try {
    writeOffRecords.value = await financeReceiptApi.getWriteOffs(receiptId.value)
  } catch {
    writeOffRecords.value = []
  }
}

const loadReceiptDocs = async () => {
  if (!receiptId.value) {
    receiptDocs.value = []
    return
  }
  if (maskSaleSensitiveFields.value) {
    receiptDocs.value = []
    return
  }
  try {
    receiptDocs.value = await documentApi.getDocuments('FINANCE_RECEIPT', receiptId.value)
  } catch {
    receiptDocs.value = []
  }
}

const previewDoc = (doc: UploadDocumentDto) => {
  window.open(documentApi.getPreviewPath(doc.id), '_blank')
}

const downloadDoc = async (doc: UploadDocumentDto) => {
  await documentApi.downloadDocument(doc.id, doc.originalFileName)
}

const formatAmount = (val: number) => {
  if (val == null) return '—'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function receiptItemConvertAmount(row: FinanceReceiptItem): number {
  const convert = Number(row.receiptConvertAmount) || 0
  if (convert > 0) return convert
  return Number(row.receiptAmount) || 0
}

function formatReceiptItemConvertAmountDisplay(row: FinanceReceiptItem): string {
  const currencyCode = detail.value?.receiptCurrency
  const currency = currencyCode != null ? CURRENCY_MAP[currencyCode] : undefined
  const amount = formatAmount(receiptItemConvertAmount(row))
  return currency ? `${currency} ${amount}` : amount
}

/** 待核销 = 收款折算金额 − 累计已核销 */
function receiptItemPendingVerifyAmount(row: FinanceReceiptItem): number {
  return receiptItemConvertAmount(row) - (Number(row.verifiedAmount) || 0)
}

function writeOffSourceLabel(source?: number) {
  if (source === 20) return t('financeReceiptDetail.writeOffSource.advancePool')
  return t('financeReceiptDetail.writeOffSource.receiptItem')
}

function formatWriteOffAmountDisplay(row: FinanceReceiptWriteOffRecord): string {
  const currency = CURRENCY_MAP[row.currency] ?? ''
  const amount = formatAmount(row.amount)
  return currency ? `${currency} ${amount}` : amount
}

function goBack() {
  router.push({ name: 'FinanceReceiptList' })
}

onBeforeUnmount(() => {
  customerWorkspacePanelStore.clear()
})
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.finance-receipt-detail-page {
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

.receipt-caption-title-group {
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

.receipt-header-meta-row {
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
  margin-bottom: 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.detail-content {
  min-height: 200px;
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

.detail-panel-section-body {
  padding: 16px 20px 20px;
}

.section-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
}
</style>
