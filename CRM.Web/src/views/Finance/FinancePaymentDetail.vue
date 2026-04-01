<template>
  <div class="finance-detail">
    <!-- 面包屑 + 返回 -->
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> {{ t('financePaymentDetail.backToList') }}
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'FinancePaymentList' }">{{ t('financePaymentDetail.breadcrumb') }}</el-breadcrumb-item>
        <el-breadcrumb-item>{{ detail?.financePaymentCode || t('financePaymentDetail.detail') }}</el-breadcrumb-item>
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
          <span>{{ t('financePaymentDetail.basicInfo') }}</span>
          <el-tag effect="dark" :type="paymentStatusTag(detail.status) as any" size="small" style="margin-left: 12px;">
            {{ paymentStatusLabel(detail.status) }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item :label="t('financePaymentDetail.labels.code')">
            <span class="order-code">{{ detail.financePaymentCode }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.status')">
            <el-tag effect="dark" :type="paymentStatusTag(detail.status) as any">
              {{ paymentStatusLabel(detail.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.vendor')">{{ vendorDisplayName }}</el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.amount')">
            <span class="amount">{{ CURRENCY_MAP[detail.paymentCurrency] }} {{ formatAmount(detail.paymentAmount) }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.mode')">{{ paymentModeLabel(detail.paymentMode) }}</el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.date')">{{ detail.paymentDate ? formatDisplayDate(detail.paymentDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.bankSlip')">{{ (detail as any).bankSlipNo || '-' }}</el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.remark')" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 付款明细 -->
      <div class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financePaymentDetail.paymentLines') }}</span>
        </div>
        <el-empty v-if="!detail.items?.length" :description="t('financePaymentDetail.noItems')" :image-size="80" />
        <CrmDataTable v-else :data="paymentLineRows" size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="purchaseOrderCode" :label="t('financePaymentDetail.labels.poCode')" min-width="160" show-overflow-tooltip />
          <el-table-column prop="pn" :label="t('financePaymentDetail.labels.pn')" min-width="150" />
          <el-table-column prop="brand" :label="t('financePaymentDetail.labels.brand')" width="120" />
          <el-table-column prop="qty" :label="t('financePaymentDetail.labels.qty')" width="100" align="right">
            <template #default="{ row }">
              {{ row.qty ?? '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="cost" :label="t('financePaymentDetail.labels.unitPrice')" width="130" align="right">
            <template #default="{ row }">
              {{ row.cost == null ? '-' : formatAmount(Number(row.cost)) }}
            </template>
          </el-table-column>
          <el-table-column prop="paymentAmount" :label="t('financePaymentDetail.labels.paidAmount')" width="130" align="right">
            <template #default="{ row }">
              {{ formatAmount(row.paymentAmount) }}
            </template>
          </el-table-column>
          <el-table-column prop="purchaseOrderCreateTime" :label="t('financePaymentDetail.labels.poCreatedAt')" width="170">
            <template #default="{ row }">
              {{ row.purchaseOrderCreateTime ? formatDisplayDateTime(row.purchaseOrderCreateTime) : '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="purchaseOrderCreateUserName" :label="t('financePaymentDetail.labels.creator')" width="120" show-overflow-tooltip />
          <el-table-column :label="t('financePaymentDetail.labels.verifyStatus')" width="120" align="center">
            <template #default="{ row }">
              <el-tag effect="dark" size="small" :type="row.verificationStatus === 2 ? 'success' : row.verificationStatus === 1 ? 'warning' : 'info'">
                {{ verificationStatusLabel(row.verificationStatus) }}
              </el-tag>
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>

      <!-- 银行水单附件 -->
      <div class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financePaymentDetail.bankSlip') }}</span>
        </div>
        <el-empty v-if="!paymentDocs.length" :description="t('financePaymentDetail.noAttachments')" :image-size="80" />
        <CrmDataTable v-else :data="paymentDocs" size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="originalFileName" :label="t('financePaymentDetail.labels.fileName')" min-width="260" show-overflow-tooltip />
          <el-table-column prop="remark" :label="t('financePaymentDetail.labels.remark')" min-width="140" show-overflow-tooltip />
          <el-table-column prop="createTime" :label="t('financePaymentDetail.labels.uploadTime')" width="170">
            <template #default="{ row }">
              {{ row.createTime ? formatDisplayDateTime(row.createTime) : '-' }}
            </template>
          </el-table-column>
          <el-table-column :label="t('financePaymentDetail.labels.actions')" width="140" fixed="right" class-name="op-col" label-class-name="op-col">
            <template #default="{ row }">
              <div @click.stop @dblclick.stop>
                <div class="action-btns">
                  <el-button size="small" text type="primary" @click.stop="previewDoc(row)">{{ t('financePaymentDetail.preview') }}</el-button>
                  <el-button size="small" text type="primary" @click.stop="downloadDoc(row)">{{ t('financePaymentDetail.download') }}</el-button>
                </div>
              </div>
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>
    </template>

    <el-empty v-else :description="t('financePaymentDetail.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ArrowLeft } from '@element-plus/icons-vue'
import {
  financePaymentApi,
  CURRENCY_MAP,
  type FinancePayment,
} from '@/api/finance'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { vendorApi } from '@/api/vendor'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const { paymentStatusLabel, paymentStatusTag, paymentModeLabel, verificationStatusLabel } = useFinanceEnumLabels()

const loading = ref(false)
const detail = ref<FinancePayment | null>(null)
const paymentLineRows = ref<any[]>([])
const paymentDocs = ref<UploadDocumentDto[]>([])
const vendorDisplayName = ref('-')

const paymentId = computed(() => route.params.id as string)

onMounted(() => {
  fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    // apiClient 拦截器已解包，直接返回业务数据
    detail.value = await financePaymentApi.getById(paymentId.value)
    await buildPaymentLineRows()
    await loadPaymentDocs()
    await resolveVendorDisplayName()
  } catch {
    detail.value = null
    paymentLineRows.value = []
    paymentDocs.value = []
    vendorDisplayName.value = '-'
  } finally {
    loading.value = false
  }
}

const loadPaymentDocs = async () => {
  if (!paymentId.value) {
    paymentDocs.value = []
    return
  }
  try {
    paymentDocs.value = await documentApi.getDocuments('FINANCE_PAYMENT', paymentId.value)
  } catch {
    paymentDocs.value = []
  }
}

const buildPaymentLineRows = async () => {
  const current = detail.value
  const rows = current?.items ?? []
  if (!rows.length) {
    paymentLineRows.value = []
    return
  }

  const poIds = Array.from(new Set(
    rows.map((x: any) => String(x?.purchaseOrderId || '').trim()).filter(Boolean)
  ))
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
      purchaseOrderCode: po?.purchaseOrderCode || po?.PurchaseOrderCode || item?.purchaseOrderCode || '-',
      qty: matchedItem?.qty ?? matchedItem?.Qty ?? null,
      cost: matchedItem?.cost ?? matchedItem?.Cost ?? null,
      purchaseOrderCreateTime: po?.createTime || po?.CreateTime || null,
      purchaseOrderCreateUserName: po?.createUserName || po?.createdBy || po?.purchaseUserName || '-'
    }
  })
}

const resolveVendorDisplayName = async () => {
  const current = detail.value
  if (!current) {
    vendorDisplayName.value = '-'
    return
  }

  const rawName = String(current.vendorName || '').trim()
  const rawId = String(current.vendorId || '').trim()
  const pickVendorName = (v: any) =>
    v?.officialName ||
    v?.OfficialName ||
    v?.nickName ||
    v?.NickName ||
    v?.name ||
    v?.Name ||
    v?.vendorName ||
    v?.VendorName ||
    ''
  const pickVendorCode = (v: any) =>
    String(v?.code || v?.Code || '').trim()

  // 正常名称直接展示；像 VEN0002 这种编码再尝试回查真实名称
  const looksLikeCode = (value: string) => /^VEN[\w-]*$/i.test(value)
  if (rawName && !looksLikeCode(rawName)) {
    vendorDisplayName.value = rawName
    return
  }

  // 如果当前值是供应商编码，优先用编码检索供应商名称
  if (rawName && looksLikeCode(rawName)) {
    try {
      const pageByCode = await vendorApi.searchVendors({ pageNumber: 1, pageSize: 20, keyword: rawName })
      const exactByCode = pageByCode?.items?.find((x: any) => pickVendorCode(x).toUpperCase() === rawName.toUpperCase())
      const nameByCode = pickVendorName(exactByCode || pageByCode?.items?.[0])
      if (nameByCode) {
        vendorDisplayName.value = nameByCode
        return
      }
    } catch {
      // ignored
    }
  }

  if (!rawId) {
    vendorDisplayName.value = rawName || '-'
    return
  }

  try {
    const v = await vendorApi.getVendorById(rawId)
    vendorDisplayName.value = pickVendorName(v) || rawName || rawId
    return
  } catch {
    // ignored
  }

  try {
    const page = await vendorApi.searchVendors({ pageNumber: 1, pageSize: 20, keyword: rawId })
    const first = page?.items?.[0]
    vendorDisplayName.value = pickVendorName(first) || rawName || rawId
  } catch {
    vendorDisplayName.value = rawName || rawId
  }
}

const formatAmount = (val: number) => {
  if (val == null) return '-'
  return val.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const previewDoc = (doc: UploadDocumentDto) => {
  window.open(documentApi.getPreviewPath(doc.id), '_blank')
}

const downloadDoc = async (doc: UploadDocumentDto) => {
  await documentApi.downloadDocument(doc.id, doc.originalFileName)
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
  font-family: 'Courier New', monospace;
  color: $text-secondary;
  font-weight: 600;
}

.amount {
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
