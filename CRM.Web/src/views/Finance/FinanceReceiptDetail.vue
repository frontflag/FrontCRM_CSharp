<template>
  <div class="finance-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> {{ t('financeReceiptDetail.backToList') }}
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'FinanceReceiptList' }">{{ t('financeReceiptDetail.breadcrumb') }}</el-breadcrumb-item>
        <el-breadcrumb-item>
          <span class="order-code">{{ detail?.financeReceiptCode || t('financeReceiptDetail.detail') }}</span>
        </el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="detail">
      <!-- 基本信息卡片 -->
      <div class="info-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financeReceiptDetail.basicInfo') }}</span>
          <el-tag effect="dark" :type="receiptStatusTag(detail.status) as any" size="small" style="margin-left: 12px;">
            {{ receiptStatusLabel(detail.status) }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item :label="t('financeReceiptDetail.labels.code')">
            <span class="order-code">{{ detail.financeReceiptCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.status')">
            <el-tag effect="dark" :type="receiptStatusTag(detail.status) as any">
              {{ receiptStatusLabel(detail.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.customer')">{{ maskSaleSensitiveFields ? '—' : (detail.customerName || '—') }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.amount')">
            <span class="amount">{{
              maskSaleSensitiveFields ? '—' : `${CURRENCY_MAP[detail.receiptCurrency]} ${formatAmount(detail.receiptAmount)}`
            }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.mode')">{{ paymentModeLabel(detail.receiptMode) }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.date')">{{ detail.receiptDate ? formatDisplayDate(detail.receiptDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.bankSlip')">{{ detail.bankSlipNo || '-' }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeReceiptDetail.labels.remark')" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 收款明细 -->
      <div class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financeReceiptDetail.receiptLines') }}</span>
        </div>
        <el-empty v-if="!detail.items?.length" :description="t('financeReceiptDetail.noItems')" :image-size="80" />
        <CrmDataTable v-else :data="detail.items" size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="pn" :label="t('financeReceiptDetail.labels.pn')" min-width="150" />
          <el-table-column prop="brand" :label="t('financeReceiptDetail.labels.brand')" width="120" />
          <el-table-column prop="receiptAmount" :label="t('financeReceiptDetail.labels.receivedAmount')" width="130" align="right">
            <template #default="{ row }">
              {{ maskSaleSensitiveFields ? '—' : formatAmount(row.receiptAmount) }}
            </template>
          </el-table-column>
          <el-table-column :label="t('financeReceiptDetail.labels.verifyStatus')" width="120" align="center">
            <template #default="{ row }">
              <el-tag effect="dark" size="small" :type="row.verificationStatus === 2 ? 'success' : row.verificationStatus === 1 ? 'warning' : 'info'">
                {{ verificationStatusLabel(row.verificationStatus) }}
              </el-tag>
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>

      <!-- 银行水单附件 -->
      <div v-if="maskSaleSensitiveFields" class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financeReceiptDetail.bankSlip') }}</span>
        </div>
        <el-alert type="info" :closable="false" show-icon :title="t('common.crossSideAttachmentsRestricted')" />
      </div>
      <div v-else class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financeReceiptDetail.bankSlip') }}</span>
        </div>
        <el-empty v-if="!receiptDocs.length" :description="t('financeReceiptDetail.noAttachments')" :image-size="80" />
        <CrmDataTable v-else :data="receiptDocs" size="small">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="originalFileName" :label="t('financeReceiptDetail.labels.fileName')" min-width="260" show-overflow-tooltip />
          <el-table-column prop="remark" :label="t('financeReceiptDetail.labels.remark')" min-width="140" show-overflow-tooltip />
          <el-table-column prop="createTime" :label="t('financeReceiptDetail.labels.uploadTime')" width="170">
            <template #default="{ row }">
              {{ row.createTime ? formatDisplayDateTime(row.createTime) : '-' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('financeReceiptDetail.labels.actions')" width="140" fixed="right" class-name="op-col" label-class-name="op-col">
            <template #default="{ row }">
              <div @click.stop @dblclick.stop>
                <div class="action-btns">
                  <el-button size="small" text type="primary" @click.stop="previewDoc(row)">{{ t('financeReceiptDetail.preview') }}</el-button>
                  <el-button size="small" text type="primary" @click.stop="downloadDoc(row)">{{ t('financeReceiptDetail.download') }}</el-button>
                </div>
              </div>
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>
    </template>

    <el-empty v-else :description="t('financeReceiptDetail.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ArrowLeft } from '@element-plus/icons-vue'
import {
  financeReceiptApi,
  CURRENCY_MAP,
  type FinanceReceipt,
} from '@/api/finance'
import { documentApi, type UploadDocumentDto } from '@/api/document'
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

const receiptId = computed(() => route.params.id as string)

onMounted(() => {
  fetchDetail()
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
  if (val == null) return '-'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.finance-detail {
  padding: 20px;
  min-height: 100%;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
  .back-btn {
    color: $text-secondary;
    &:hover { color: $cyan-primary; }
  }
}

.loading-wrap {
  padding: 20px;
  background: $layer-2;
  border-radius: 8px;
}

.info-card, .tab-card {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 16px 20px;
  margin-bottom: 16px;
}

.card-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 14px;
  .title-bar {
    width: 4px;
    height: 16px;
    background: $cyan-primary;
    border-radius: 2px;
  }
}

.order-desc {
  :deep(.el-descriptions__label) {
    color: $text-muted;
    background: $layer-3;
    width: 100px;
  }
  :deep(.el-descriptions__content) {
    background: $layer-2;
  }
}

.order-code {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  font-variant-numeric: tabular-nums;
  color: $text-primary;
  font-weight: 500;
  letter-spacing: normal;
}

.amount {
  font-family: 'Noto Sans SC', sans-serif;
  font-variant-numeric: tabular-nums;
  color: $cyan-primary;
  font-weight: 600;
}

.items-table {
  // 无外边框，行间细线分隔，对标客户管理列表风格
  --el-table-border-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: rgba(0, 212, 255, 0.04) !important;
      border-bottom: 1px solid rgba(0, 212, 255, 0.1) !important;
      border-right: none !important;
      color: rgba(200, 216, 232, 0.55);
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
  }
  :deep(.el-table__row) {
    background: transparent !important;
    td.el-table__cell {
      background: transparent !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04) !important;
      border-right: none !important;
      color: rgba(224, 244, 255, 0.85);
      font-size: 13px;
    }
    &:last-child td.el-table__cell { border-bottom: none !important; }
    &:hover td.el-table__cell { background: rgba(0, 212, 255, 0.04) !important; }
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
  }
}
</style>
