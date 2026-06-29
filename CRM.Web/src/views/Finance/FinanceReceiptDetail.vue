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
            </div>
          </div>
        </div>
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

        <div class="tabs-section">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'items' }"
              @click="detailActiveTab = 'items'"
            >
              {{ t('financeReceiptDetail.tabs.items') }}
              <span v-if="detail.items?.length" class="tab-count">{{ detail.items.length }}</span>
            </button>
            <button
              class="tab-btn"
              type="button"
              :class="{ 'tab-btn--active': detailActiveTab === 'bankSlip' }"
              @click="detailActiveTab = 'bankSlip'"
            >
              {{ t('financeReceiptDetail.tabs.bankSlip') }}
              <span v-if="!maskSaleSensitiveFields && receiptDocs.length" class="tab-count">{{ receiptDocs.length }}</span>
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="detailActiveTab === 'items'" class="detail-items-table-wrap">
              <el-empty v-if="!detail.items?.length" :description="t('financeReceiptDetail.noItems')" :image-size="80" />
              <CrmDataTable
                v-else
                :data="detail.items"
                class="items-table detail-panel-list-table"
                size="small"
                stripe
              >
                <el-table-column type="index" width="50" label="#" />
                <el-table-column prop="pn" :label="t('financeReceiptDetail.labels.pn')" min-width="150" show-overflow-tooltip />
                <el-table-column prop="brand" :label="t('financeReceiptDetail.labels.brand')" width="120" show-overflow-tooltip />
                <el-table-column
                  prop="receiptAmount"
                  :label="t('financeReceiptDetail.labels.receivedAmount')"
                  width="130"
                  align="right"
                  header-align="right"
                >
                  <template #default="{ row }">
                    {{ maskSaleSensitiveFields ? '—' : formatAmount(row.receiptAmount) }}
                  </template>
                </el-table-column>
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
              </CrmDataTable>
            </div>
            <div v-show="detailActiveTab === 'bankSlip'">
              <el-alert
                v-if="maskSaleSensitiveFields"
                type="info"
                :closable="false"
                show-icon
                :title="t('common.crossSideAttachmentsRestricted')"
              />
              <template v-else>
                <el-empty v-if="!receiptDocs.length" :description="t('financeReceiptDetail.noAttachments')" :image-size="80" />
                <div v-else class="detail-items-table-wrap">
                <CrmDataTable :data="receiptDocs" class="items-table detail-panel-list-table" size="small" stripe>
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
                </div>
              </template>
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="t('financeReceiptDetail.notFound')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { financeReceiptApi, CURRENCY_MAP, type FinanceReceipt } from '@/api/finance'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const route = useRoute()
const { t } = useI18n()
const { receiptStatusLabel, receiptStatusTag, paymentModeLabel, verificationStatusLabel } = useFinanceEnumLabels()

const loading = ref(false)
const detail = ref<FinanceReceipt | null>(null)
const receiptDocs = ref<UploadDocumentDto[]>([])
const detailActiveTab = ref<'items' | 'bankSlip'>('items')

const receiptId = computed(() => route.params.id as string)

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
    await loadReceiptDocs()
  } catch {
    detail.value = null
    receiptDocs.value = []
  } finally {
    loading.value = false
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

function goBack() {
  router.push({ name: 'FinanceReceiptList' })
}
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
</style>
