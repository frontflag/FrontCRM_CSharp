<template>
  <div class="finance-detail">
    <!-- 面包屑 + 返回 + 操作 -->
    <div class="detail-header">
      <div class="detail-header__left">
        <el-button link @click="router.back()" class="back-btn">
          <el-icon><ArrowLeft /></el-icon> {{ t('financePaymentDetail.backToList') }}
        </el-button>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item :to="{ name: 'FinancePaymentList' }">{{ t('financePaymentDetail.breadcrumb') }}</el-breadcrumb-item>
          <el-breadcrumb-item>
            <span class="order-code">{{ detail?.financePaymentCode || t('financePaymentDetail.detail') }}</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div v-if="detail" class="detail-header__actions">
        <el-button
          v-if="canShowPayButton"
          type="warning"
          @click="payDialogVisible = true"
        >
          {{ t('financePaymentList.actions.pay') }}
        </el-button>
      </div>
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
          <el-descriptions-item :label="t('financePaymentDetail.labels.vendorReceivingBank')">
            {{ maskPurchaseSensitiveFields ? '—' : (detail.vendorBankName || '—') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.paymentBank')">
            {{ maskPurchaseSensitiveFields ? '—' : (detail.paymentBankName || '—') }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.requestRemark')" :span="2">
            {{ detail.requestRemark || '—' }}
          </el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.feeSummaryLabel')" :span="2">{{ feeSummaryText }}</el-descriptions-item>
          <el-descriptions-item :label="t('financePaymentDetail.labels.remark')" :span="2">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 付款明细 | 文档（采购订单关联文档） -->
      <div class="tab-card payment-lines-card">
        <div class="tabs-section payment-lines-tabs">
          <div class="tabs-nav">
            <button
              class="tab-btn"
              :class="{ 'tab-btn--active': paymentLinesActiveTab === 'items' }"
              @click="paymentLinesActiveTab = 'items'"
            >
              {{ t('financePaymentDetail.paymentLines') }}
            </button>
            <button
              v-if="!maskPurchaseSensitiveFields"
              class="tab-btn"
              :class="{ 'tab-btn--active': paymentLinesActiveTab === 'documents' }"
              @click="paymentLinesActiveTab = 'documents'"
            >
              {{ t('financePaymentDetail.tabDocuments') }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="paymentLinesActiveTab === 'items'">
              <el-empty v-if="!detail.items?.length" :description="t('financePaymentDetail.noItems')" :image-size="80" />
              <CrmDataTable v-else :data="paymentLineRows" size="small">
                <el-table-column type="index" width="50" label="#" />
                <el-table-column prop="purchaseOrderCode" :label="t('financePaymentDetail.labels.poCode')" min-width="160" show-overflow-tooltip />
                <el-table-column
                  prop="freightForwarderOrderNo"
                  :label="t('financePaymentDetail.labels.freightForwarderOrderNo')"
                  min-width="150"
                  show-overflow-tooltip
                />
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
                <el-table-column prop="lineRemark" :label="t('financePaymentDetail.labels.lineRemark')" min-width="120" show-overflow-tooltip />
                <el-table-column :label="t('financePaymentDetail.labels.verifyStatus')" width="120" align="center">
                  <template #default="{ row }">
                    <el-tag effect="dark" size="small" :type="row.verificationStatus === 2 ? 'success' : row.verificationStatus === 1 ? 'warning' : 'info'">
                      {{ verificationStatusLabel(row.verificationStatus) }}
                    </el-tag>
                  </template>
                </el-table-column>
              </CrmDataTable>
            </div>
            <div v-show="paymentLinesActiveTab === 'documents' && !maskPurchaseSensitiveFields" class="doc-tab-content">
              <el-empty
                v-if="!relatedPurchaseOrders.length"
                :description="t('financePaymentDetail.noRelatedPo')"
                :image-size="80"
              />
              <template v-else>
                <div v-if="relatedPurchaseOrders.length > 1" class="po-doc-toolbar">
                  <span class="po-doc-toolbar__label">{{ t('financePaymentDetail.selectPoForDocs') }}</span>
                  <el-select v-model="selectedPoIdForDocs" style="width: 220px">
                    <el-option
                      v-for="po in relatedPurchaseOrders"
                      :key="po.id"
                      :label="po.code"
                      :value="po.id"
                    />
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
                  <span class="po-doc-toolbar__label">{{ t('financePaymentDetail.poDocSource', { code: relatedPurchaseOrders[0].code }) }}</span>
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
            </div>
          </div>
        </div>
      </div>

      <!-- 银行水单附件 -->
      <div v-if="maskPurchaseSensitiveFields" class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financePaymentDetail.bankSlip') }}</span>
        </div>
        <el-alert type="info" :closable="false" show-icon :title="t('common.crossSideAttachmentsRestricted')" />
      </div>
      <div v-else class="tab-card bank-slip-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financePaymentDetail.bankSlip') }}</span>
        </div>
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
          style="margin-top: 16px;"
        />
      </div>
    </template>

    <el-empty v-else :description="t('financePaymentDetail.notFound')" />

    <FinancePaymentPayDialog
      v-model="payDialogVisible"
      :payment="detail"
      @success="onPayDialogSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ArrowLeft } from '@element-plus/icons-vue'
import {
  financePaymentApi,
  CURRENCY_MAP,
  type FinancePayment,
} from '@/api/finance'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { vendorApi } from '@/api/vendor'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import DocumentUploadPanel from '@/components/Document/DocumentUploadPanel.vue'
import DocumentListPanel from '@/components/Document/DocumentListPanel.vue'
import FinancePaymentPayDialog from '@/components/Finance/FinancePaymentPayDialog.vue'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const { paymentStatusLabel, paymentStatusTag, paymentModeLabel, verificationStatusLabel } = useFinanceEnumLabels()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { canWriteFinancePayment: canFinancePaymentWrite } = useFinanceWriteGate()

const loading = ref(false)
const payDialogVisible = ref(false)
const detail = ref<FinancePayment | null>(null)
const paymentLineRows = ref<any[]>([])
const vendorDisplayName = ref('-')
const paymentLinesActiveTab = ref<'items' | 'documents'>('items')
const selectedPoIdForDocs = ref('')
const paymentSlipDocListRef = ref<InstanceType<typeof DocumentListPanel> | null>(null)
const paymentId = computed(() => route.params.id as string)

const canShowPayButton = computed(() => {
  if (!canFinancePaymentWrite.value || !detail.value) return false
  return [1, -1, 10].includes(detail.value.status)
})

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
  if (masked && paymentLinesActiveTab.value === 'documents') {
    paymentLinesActiveTab.value = 'items'
  }
})

const feeSummaryText = computed(() => {
  const d = detail.value
  if (!d) return '—'
  const sym = CURRENCY_MAP[d.paymentCurrency] || ''
  const f = (n: unknown) => formatAmount(Number(n ?? 0))
  const payer = (d.feeIntermediateBankPayer || '—').trim() || '—'
  return `${sym} ${t('financePaymentDetail.labels.feeIntermediateBank')}${f(d.feeIntermediateBank)} · ${t('financePaymentDetail.labels.feeBankCharge')}${f(d.feeBankCharge)} · ${t('financePaymentDetail.labels.feeFreight')}${f(d.feeFreight)} · ${t('financePaymentDetail.labels.feeMisc')}${f(d.feeMisc)} · ${t('financePaymentDetail.labels.feeRounding')}${f(d.feeRounding)} · ${t('financePaymentDetail.labels.feePayer')}${payer}`
})

onMounted(() => {
  fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    // apiClient 拦截器已解包，直接返回业务数据
    detail.value = await financePaymentApi.getById(paymentId.value)
    await buildPaymentLineRows()
    await resolveVendorDisplayName()
  } catch {
    detail.value = null
    paymentLineRows.value = []
    vendorDisplayName.value = '-'
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
      purchaseOrderId: String(item?.purchaseOrderId || po?.id || '').trim(),
      purchaseOrderCode: po?.purchaseOrderCode || po?.PurchaseOrderCode || item?.purchaseOrderCode || '-',
      freightForwarderOrderNo:
        po?.freightForwarderOrderNo || po?.FreightForwarderOrderNo || item?.freightForwarderOrderNo || '—',
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
  if (maskPurchaseSensitiveFields.value) {
    vendorDisplayName.value = '—'
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
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 20px;
  .detail-header__left {
    display: flex;
    align-items: center;
    gap: 12px;
    min-width: 0;
  }
  .detail-header__actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
  }
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

.payment-lines-card {
  padding: 0;
  overflow: hidden;
}

.payment-lines-tabs.tabs-section {
  border: none;
  border-radius: 0;
  padding: 0;
  background: transparent;
}

.payment-lines-tabs .tabs-nav {
  display: flex;
  border-bottom: 1px solid $border-card;
  padding: 0 20px;
  background: $layer-3;
}

.payment-lines-tabs .tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  margin-bottom: -1px;
}

.payment-lines-tabs .tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.payment-lines-tabs .tabs-body {
  padding: 16px 20px 20px;
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
}

.po-doc-toolbar__link:hover {
  text-decoration: underline;
}

</style>
