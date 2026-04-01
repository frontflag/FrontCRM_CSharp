<template>
  <div class="finance-detail">
    <div class="detail-header">
      <el-button link @click="router.back()" class="back-btn">
        <el-icon><ArrowLeft /></el-icon> {{ t('financeSellInvoiceDetail.backToList') }}
      </el-button>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item :to="{ name: 'FinanceSellInvoiceList' }">{{ t('financeSellInvoiceDetail.breadcrumb') }}</el-breadcrumb-item>
        <el-breadcrumb-item>{{ detail?.invoiceCode || t('financeSellInvoiceDetail.detail') }}</el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <div v-if="loading" class="loading-wrap">
      <el-skeleton :rows="8" animated />
    </div>

    <template v-else-if="detail">
      <div class="info-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financeSellInvoiceDetail.basicInfo') }}</span>
          <el-tag effect="dark" :type="invoiceStatusTag(detail.invoiceStatus) as any" size="small" style="margin-left: 12px;">
            {{ invoiceStatusLabel(detail.invoiceStatus) }}
          </el-tag>
          <el-tag effect="dark" :type="receiveStatusTag(detail.receiveStatus) as any" size="small" style="margin-left: 4px;">
            {{ receiveStatusLabel(detail.receiveStatus) }}
          </el-tag>
        </div>
        <el-descriptions :column="2" border class="order-desc">
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.code')">
            <span class="order-code">{{ detail.invoiceCode || t('financeSellInvoiceDetail.codeNotGenerated') }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.invoiceNo')">{{ detail.invoiceNo || '-' }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.customer')">{{ detail.customerName }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.amount')">
            <span class="amount">{{ CURRENCY_MAP[detail.currency] }} {{ formatAmount(detail.invoiceTotal) }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.received')">
            <span class="amount">{{ CURRENCY_MAP[detail.currency] }} {{ formatAmount(detail.receiveDone) }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.toReceive')">
            <span style="color:#E8A838; font-weight: 600;">{{ CURRENCY_MAP[detail.currency] }} {{ formatAmount(detail.receiveToBe) }}</span>
          </el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.makeDate')">{{ detail.makeInvoiceDate ? formatDisplayDate(detail.makeInvoiceDate) : '-' }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.invoiceType')">{{ sellInvoiceTypeLabel(detail.sellInvoiceType) }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.blueRed')">{{ invoiceTypeLabel(detail.type) }}</el-descriptions-item>
          <el-descriptions-item :label="t('financeSellInvoiceDetail.labels.remark')" :span="2">{{ detail.remark || '-' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="tab-card">
        <div class="card-title">
          <span class="title-bar"></span>
          <span>{{ t('financeSellInvoiceDetail.invoiceLines') }}</span>
        </div>
        <el-empty v-if="!detail.items?.length" :description="t('financeSellInvoiceDetail.noItems')" :image-size="80" />
        <CrmDataTable v-else :data="detail.items" size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" />
          <el-table-column prop="qty" :label="t('financeSellInvoiceDetail.labels.qty')" width="80" align="right" />
          <el-table-column prop="price" :label="t('financeSellInvoiceDetail.labels.unitPrice')" width="120" align="right">
            <template #default="{ row }">
              ¥ {{ formatAmount(row.price) }}
            </template>
          </el-table-column>
          <el-table-column prop="invoiceTotal" :label="t('financeSellInvoiceDetail.labels.lineTotal')" width="130" align="right">
            <template #default="{ row }">
              ¥ {{ formatAmount(row.invoiceTotal) }}
            </template>
          </el-table-column>
          <el-table-column prop="valueAddedTax" :label="t('financeSellInvoiceDetail.labels.vat')" width="130" align="right">
            <template #default="{ row }">
              ¥ {{ formatAmount(row.valueAddedTax) }}
            </template>
          </el-table-column>
          <el-table-column prop="taxRate" :label="t('financeSellInvoiceDetail.labels.taxRate')" width="80" align="center">
            <template #default="{ row }">
              {{ (row.taxRate * 100).toFixed(0) }}%
            </template>
          </el-table-column>
        </CrmDataTable>
      </div>
    </template>

    <el-empty v-else :description="t('financeSellInvoiceDetail.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ArrowLeft } from '@element-plus/icons-vue'
import {
  financeSellInvoiceApi,
  CURRENCY_MAP,
  type FinanceSellInvoice,
} from '@/api/finance'
import { formatDisplayDate } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const {
  invoiceStatusLabel,
  invoiceStatusTag,
  receiveStatusLabel,
  receiveStatusTag,
  sellInvoiceTypeLabel,
  invoiceTypeLabel,
} = useFinanceEnumLabels()

const loading = ref(false)
const detail = ref<FinanceSellInvoice | null>(null)

const invoiceId = computed(() => route.params.id as string)

onMounted(() => {
  fetchDetail()
})

const fetchDetail = async () => {
  loading.value = true
  try {
    detail.value = await financeSellInvoiceApi.getById(invoiceId.value)
  } catch {
    detail.value = null
  } finally {
    loading.value = false
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
