<template>
  <div class="finance-ff-payable-detail-page" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          {{ t('financeFfPayableDetail.back') }}
        </button>
        <div v-if="detail" class="ff-payable-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">
                  {{ t('financeFfPayableDetail.captionPrefix') }} {{ detail.receipt.financeReceiptCode || '—' }}
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption ff-payable-header-meta-row">
              <el-tag effect="dark" :type="statusTagType(detail.payableStatus) as any" size="small">
                {{ statusLabel(detail.payableStatus) }}
              </el-tag>
            </div>
          </div>
        </div>
      </div>
      <div v-if="detail && canWriteFinanceReceipt && detail.pendingAmount > 0" class="header-right">
        <el-button type="warning" @click="payDialogVisible = true">
          {{ t('financeFfPayableDetail.pay') }}
        </el-button>
      </div>
    </div>

    <div class="detail-content">
      <template v-if="detail">
        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeFfPayableDetail.basicInfo') }}</span>
            </div>
            <div class="section-header__meta">
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financeFfPayableDetail.createDate') }}</span>
                <span class="section-header-meta-item__value">{{ basicCreateDateText }}</span>
              </span>
              <span class="section-header-meta-item">
                <span class="section-header-meta-item__label">{{ t('financeFfPayableDetail.createUser') }}</span>
                <span class="section-header-meta-item__value">{{ basicCreateUserText }}</span>
              </span>
            </div>
          </div>
          <div class="info-grid info-grid--inline-labels info-grid--basic">
            <div class="info-item">
              <span class="info-label">{{ t('financeFfPayableDetail.customer') }}</span>
              <span class="info-value">{{ reportCellText(detail.receipt.customerName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeFfPayableDetail.ffCompany') }}</span>
              <span class="info-value">{{ reportCellText(detail.freightForwarderCompanyName) }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeFfPayableDetail.receiptAmount') }}</span>
              <span class="info-value info-value--amount">{{
                formatAmountWithCurrency(detail.receipt.receiptAmount, detail.receipt.receiptCurrency)
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeFfPayableDetail.paidAmount') }}</span>
              <span class="info-value info-value--received">{{
                formatAmountWithCurrency(detail.paidAmount, detail.receipt.receiptCurrency)
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeFfPayableDetail.pendingAmount') }}</span>
              <span class="info-value info-value--pending">{{
                formatAmountWithCurrency(detail.pendingAmount, detail.receipt.receiptCurrency)
              }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">{{ t('financeFfPayableDetail.receiptDate') }}</span>
              <span class="info-value info-value--time">{{
                detail.receipt.receiptDate ? formatDisplayDate(detail.receipt.receiptDate) : '—'
              }}</span>
            </div>
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeFfPayableDetail.paymentLines') }}</span>
              <span v-if="detail.payments.length" class="section-count">{{ detail.payments.length }}</span>
            </div>
          </div>
          <div class="detail-items-table-wrap">
            <CrmDataTable
              v-if="detail.payments.length"
              :data="detail.payments"
              embedded
              :border="false"
              :show-column-settings="false"
              :show-row-density-toggle="false"
              class="items-table detail-panel-list-table"
              size="small"
              stripe
            >
              <el-table-column type="index" width="50" label="#" />
              <el-table-column :label="t('financeFfPayableDetail.colAmount')" width="150" align="right">
                <template #default="{ row }">
                  <span class="amount-text">{{ formatAmountWithCurrency(row.paymentAmount, row.paymentCurrency) }}</span>
                </template>
              </el-table-column>
              <el-table-column
                prop="ffCompanyBankName"
                :label="t('financeFfPayableDetail.colFfBank')"
                min-width="140"
                show-overflow-tooltip
              />
              <el-table-column
                prop="bankSlipNo"
                :label="t('financeFfPayableDetail.colBankSlip')"
                min-width="120"
                show-overflow-tooltip
              />
              <el-table-column prop="paymentUserName" :label="t('financeFfPayableDetail.colPayer')" width="100" show-overflow-tooltip />
              <el-table-column :label="t('financeFfPayableDetail.colPaymentDate')" width="120">
                <template #default="{ row }">{{ row.paymentDate ? formatDisplayDate(row.paymentDate) : '—' }}</template>
              </el-table-column>
              <el-table-column
                prop="remark"
                :label="t('financeFfPayableDetail.colRemark')"
                min-width="160"
                show-overflow-tooltip
              />
            </CrmDataTable>
            <DetailListPanelEmpty v-else size="low" :description="t('financeFfPayableDetail.noPayments')" />
          </div>
        </div>

        <div class="info-section">
          <div class="section-header">
            <div class="section-header__main">
              <div class="section-dot section-dot--cyan"></div>
              <span class="section-title">{{ t('financeFfPayableDetail.related') }}</span>
            </div>
          </div>
          <div class="detail-panel-section-body">
            <div class="info-item info-item--inline">
              <span class="info-label">{{ t('financeFfPayableDetail.receiptCode') }}</span>
              <span class="info-value">
                <router-link class="cell-link" :to="{ name: 'FinanceReceiptDetail', params: { id: detail.receipt.id } }">
                  {{ detail.receipt.financeReceiptCode || '—' }}
                </router-link>
              </span>
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else-if="!loading" :description="t('financeFfPayableDetail.notFound')" />
    </div>

    <FinanceFreightForwarderPaymentPayDialog
      v-model="payDialogVisible"
      :receipt-id="receiptId"
      :pending-amount="detail?.pendingAmount ?? 0"
      :receipt-currency="detail?.receipt.receiptCurrency ?? 1"
      :freight-forwarder-company-id="detail?.receipt.freightForwarderCompanyId || undefined"
      @success="fetchDetail"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  FF_PAYABLE_STATUS,
  financeFreightForwarderPayableApi,
  type FfPayableDetail
} from '@/api/financeFreightForwarderPayable'
import type { FinanceReceipt } from '@/api/finance'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, listAmountCurrencyIso } from '@/utils/moneyFormat'
import CrmDataTable from '@/components/CrmDataTable.vue'
import DetailListPanelEmpty from '@/components/Common/DetailListPanelEmpty.vue'
import FinanceFreightForwarderPaymentPayDialog from '@/components/Finance/FinanceFreightForwarderPaymentPayDialog.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { canWriteFinanceReceipt } = useFinanceWriteGate()

const receiptId = computed(() => String(route.params.id || ''))
const loading = ref(false)
const detail = ref<FfPayableDetail | null>(null)
const payDialogVisible = ref(false)

const captionAvatarChar = computed(() => {
  const c = detail.value?.receipt.financeReceiptCode?.trim()
  return c ? c[0]! : '货'
})

function receiptRowCreatedAt(row: FinanceReceipt | null | undefined): string {
  if (!row) return ''
  const r = row as FinanceReceipt & { createTime?: string }
  const v = r.createdAt ?? r.createTime
  return v != null && String(v).trim() !== '' ? String(v) : ''
}

const basicCreateDateText = computed(() => {
  const raw = receiptRowCreatedAt(detail.value?.receipt)
  if (!raw) return '—'
  const s = formatDisplayDate(raw)
  return s === '--' ? '—' : s
})

const basicCreateUserText = computed(() => detail.value?.receipt.createUserName?.trim() || '—')

function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

function formatAmountWithCurrency(amount: number, currency?: number | null) {
  return `${listAmountCurrencyIso(currency)} ${formatTotalAmountNumber(amount)}`
}

function statusLabel(status: number) {
  if (status === FF_PAYABLE_STATUS.Partial) return t('financeFfPayableList.statusPartial')
  if (status === FF_PAYABLE_STATUS.Completed) return t('financeFfPayableList.statusCompleted')
  return t('financeFfPayableList.statusPending')
}

function statusTagType(status: number) {
  if (status === FF_PAYABLE_STATUS.Completed) return 'success'
  if (status === FF_PAYABLE_STATUS.Partial) return 'warning'
  return 'info'
}

async function fetchDetail() {
  if (!receiptId.value) return
  loading.value = true
  try {
    detail.value = await financeFreightForwarderPayableApi.getDetail(receiptId.value)
  } finally {
    loading.value = false
  }
}

function goBack() {
  router.push({ name: 'FinanceFreightForwarderPayableList' })
}

onMounted(fetchDetail)
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.finance-ff-payable-detail-page {
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

.ff-payable-caption-title-group {
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

.ff-payable-header-meta-row {
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

  &--inline {
    flex-direction: row;
    align-items: center;
    gap: 8px;
    border: none;
    padding: 0;

    .info-label::after {
      content: '：';
    }
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

  &--received {
    font-family: 'Noto Sans SC', sans-serif;
    font-variant-numeric: tabular-nums;
    color: $success-color;
    font-weight: 700;
  }

  &--pending {
    font-family: 'Noto Sans SC', sans-serif;
    font-variant-numeric: tabular-nums;
    color: #e8a838;
    font-weight: 700;
  }
}

.section-count {
  font-size: 11px;
  padding: 1px 7px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.1);
  color: $cyan-primary;
}

.detail-panel-section-body {
  padding: 16px 20px 20px;
}

.detail-items-table-wrap :deep(.items-table) {
  --el-table-border-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
}

.cell-link {
  color: $cyan-primary;
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.amount-text {
  font-variant-numeric: tabular-nums;
}
</style>
