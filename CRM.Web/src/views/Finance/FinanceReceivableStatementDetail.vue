<template>
  <div class="crm-biz-list-page stmt-detail-page" v-loading="loading">
    <div class="page-header">
      <div class="header-left">
        <button type="button" class="btn-ghost btn-sm" @click="goBack">
          {{ t('financeReceivableStatement.back') }}
        </button>
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('financeReceivableStatement.detailTitle') }}</h1>
        </div>
        <div v-if="detail" class="count-badge stmt-header-badge">
          <span>{{ customerDisplayName }}</span>
          <span class="stmt-header-badge__sep">·</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(detail.statement.currency)]">
            {{ listAmountCurrencyIso(detail.statement.currency) }}
          </span>
          <span class="stmt-header-badge__sep">·</span>
          <span>{{ t('financeReceivableStatement.lineCount', { n: ledgerLineCount }) }}</span>
        </div>
      </div>
    </div>

    <el-alert v-if="errorMsg" :title="errorMsg" type="error" show-icon class="stmt-alert" />

    <template v-if="detail">
      <div class="info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">{{ t('financeReceivableStatement.customerSection') }}</span>
          </div>
          <div class="section-header__meta">
            <button
              type="button"
              class="btn-primary btn-sm stmt-preview-in-header"
              :disabled="!detail"
              @click="goPreview"
            >
              {{ t('financeReceivableStatement.previewReport') }}
            </button>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('financeReceivableStatement.fields.generatedOn') }}</span>
              <span class="section-header-meta-item__value">{{ detail.statement.generatedOn }}</span>
            </span>
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">{{ t('financeReceivableStatement.fields.currency') }}</span>
              <span class="section-header-meta-item__value">
                <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(detail.statement.currency)]">
                  {{ listAmountCurrencyIso(detail.statement.currency) }}
                </span>
              </span>
            </span>
          </div>
        </div>
        <div class="info-grid info-grid--inline-labels info-grid--basic">
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.customerName') }}</span>
            <span class="info-value">{{ customerDisplayName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.contact') }}</span>
            <span class="info-value">{{ detail.customer.canViewFull ? (detail.customer.contactName || '—') : '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.salesUser') }}</span>
            <span class="info-value">{{ detail.customer.salesUserName || '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.phone') }}</span>
            <span class="info-value">{{ detail.customer.canViewFull ? (detail.customer.contactPhone || '—') : '—' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.paymentDays') }}</span>
            <span class="info-value">{{ paymentDaysText }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">{{ t('financeReceivableStatement.fields.creditLimit') }}</span>
            <span class="info-value">{{ creditLimitText }}</span>
          </div>
        </div>
      </div>
    </template>

    <FinanceReceivableStatementLedgerPanel
      v-if="routeCustomerId"
      ref="ledgerRef"
      :customer-id="routeCustomerId"
      :currency="routeCurrency"
      :embed-loading="false"
      :show-error-alert="false"
      @update:detail="onDetail"
      @update:loading="loading = $event"
      @update:error="errorMsg = $event"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import FinanceReceivableStatementLedgerPanel from '@/components/Finance/FinanceReceivableStatementLedgerPanel.vue'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import type { FinanceReceivableStatementDetail } from '@/api/financeReceivableStatement'
import { formatTotalAmountNumber, listAmountCurrencyDockClass, listAmountCurrencyIso } from '@/utils/moneyFormat'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const loading = ref(false)
const errorMsg = ref('')
const detail = ref<FinanceReceivableStatementDetail | null>(null)
const ledgerRef = ref<{ goPreview: () => void } | null>(null)

const routeCustomerId = computed(() => String(route.params.customerId || ''))
const routeCurrency = computed(() => {
  const n = Number(route.params.currency)
  return Number.isFinite(n) && n >= 1 ? n : 0
})

const customerDisplayName = computed(() => {
  const c = detail.value?.customer
  if (!c) return '—'
  return c.customerName || c.customerEnglishName || c.customerCode || '—'
})

const paymentDaysText = computed(() => {
  const days = detail.value?.customer.paymentDays
  if (days == null || !detail.value?.customer.canViewFull) return '—'
  return t('financeReceivableStatement.paymentDaysValue', { n: days })
})

const creditLimitText = computed(() => {
  const v = detail.value?.customer.creditLimit
  if (v == null || !detail.value?.customer.canViewFull) return '—'
  if (maskSaleSensitiveFields.value) return '—'
  return `${formatTotalAmountNumber(v)}（${t('financeReceivableStatement.creditFromMaster')}）`
})

const ledgerLineCount = computed(() => detail.value?.lines.length ?? 0)

function onDetail(value: FinanceReceivableStatementDetail | null) {
  detail.value = value
}

function goBack() {
  void router.push({ name: 'FinanceReceivableStatementList' })
}

function goPreview() {
  ledgerRef.value?.goPreview()
}
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/business-detail-info-grid.scss';

.stmt-alert {
  margin-bottom: 12px;
}

.stmt-header-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.stmt-header-badge__sep {
  color: $text-muted;
}

.stmt-preview-in-header {
  flex-shrink: 0;
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
}

.section-title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
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
  flex-shrink: 0;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.45);
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
}

.info-grid--inline-labels .info-item {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding: 12px 20px;

  .info-label {
    flex-shrink: 0;
    white-space: nowrap;
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

    &:nth-last-child(-n + 3) {
      border-bottom: none;
    }
  }

  .info-item--basic-spacer {
    border-right: none;
  }
}

.info-label {
  font-size: 11px;
  color: $text-muted;
}

.info-value {
  font-size: 13px;
  color: $text-secondary;
}
</style>
