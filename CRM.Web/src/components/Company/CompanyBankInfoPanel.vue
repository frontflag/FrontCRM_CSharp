<template>
  <div v-if="bank" class="company-bank-info-panel">
    <button
      type="button"
      class="company-bank-info-panel__header"
      :aria-expanded="expanded"
      @click="expanded = !expanded"
    >
      <span class="company-bank-info-panel__title">{{ t('financePaymentList.paymentBankDetailTitle') }}</span>
      <span class="company-bank-info-panel__toggle">
        {{ expanded ? t('purchaseOrderItemList.paymentDialog.bankDetailCollapse') : t('purchaseOrderItemList.paymentDialog.bankDetailExpand') }}
        <el-icon class="company-bank-info-panel__icon" :class="{ 'is-collapsed': !expanded }">
          <ArrowDown />
        </el-icon>
      </span>
    </button>

    <div v-show="expanded" class="company-bank-info-panel__body">
      <el-row :gutter="16">
        <el-col :span="24">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.bankName') }}</span>
            <span class="company-bank-info-panel__value company-bank-info-panel__value--single-line">{{ display(masked, bank.bankName) }}</span>
          </div>
        </el-col>
        <el-col :span="24">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.accountName') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, bank.accountName) }}</span>
          </div>
        </el-col>
        <el-col :span="24">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.bankAddress') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, bank.bankAddress) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.swift') }}</span>
            <span class="company-bank-info-panel__value company-bank-info-panel__value--code">{{ display(masked, bank.swift) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.iban') }}</span>
            <span class="company-bank-info-panel__value company-bank-info-panel__value--code">{{ display(masked, bank.iban) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.bankCode') }}</span>
            <span class="company-bank-info-panel__value company-bank-info-panel__value--code">{{ display(masked, bank.bankCode) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.accountNumber') }}</span>
            <span class="company-bank-info-panel__value company-bank-info-panel__value--code">{{ display(masked, bank.accountNumber) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.currency') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, bank.currency) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.country') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, bank.country) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.bankType') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, bankTypeText) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.purposeType') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, purposeTypeText) }}</span>
          </div>
        </el-col>
        <el-col v-if="bank.remark?.trim()" :span="24">
          <div class="company-bank-info-panel__item">
            <span class="company-bank-info-panel__label">{{ t('companyInfo.bank.remark') }}</span>
            <span class="company-bank-info-panel__value">{{ display(masked, bank.remark) }}</span>
          </div>
        </el-col>
      </el-row>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown } from '@element-plus/icons-vue'
import type { CompanyBankRow } from '@/api/companyProfile'

const props = withDefaults(
  defineProps<{
    bank?: CompanyBankRow | null
    masked?: boolean
    expandOnBankChange?: boolean
  }>(),
  {
    bank: null,
    masked: false,
    expandOnBankChange: true
  }
)

const { t } = useI18n()
const expanded = ref(true)

watch(
  () => props.bank?.id,
  () => {
    if (props.expandOnBankChange) expanded.value = true
  }
)

const bankTypeText = computed(() => {
  if (props.bank?.bankType === 'foreign') return t('companyInfo.bank.bankTypeForeign')
  if (props.bank?.bankType === 'rmb') return t('companyInfo.bank.bankTypeRmb')
  return ''
})

const purposeTypeText = computed(() => {
  const p = (props.bank?.purposeType || '').trim().toLowerCase()
  if (p === 'receipt') return t('companyInfo.bank.purposeReceipt')
  if (p === 'payment') return t('companyInfo.bank.purposePayment')
  return ''
})

function display(masked: boolean, value?: string | null) {
  if (masked) return '—'
  const text = value?.trim()
  return text || '—'
}
</script>

<style scoped>
.company-bank-info-panel {
  margin-bottom: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-blank);
  overflow: hidden;
}

.company-bank-info-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 10px 14px;
  border: none;
  background: var(--el-fill-color-light);
  cursor: pointer;
  font: inherit;
  text-align: left;
  color: var(--el-text-color-primary);
}

.company-bank-info-panel__header:hover {
  background: var(--el-fill-color);
}

.company-bank-info-panel__title {
  font-size: 13px;
  font-weight: 600;
}

.company-bank-info-panel__toggle {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.company-bank-info-panel__icon {
  transition: transform 0.2s ease;
}

.company-bank-info-panel__icon.is-collapsed {
  transform: rotate(-90deg);
}

.company-bank-info-panel__body {
  padding: 12px 14px 2px;
}

.company-bank-info-panel__item {
  display: flex;
  gap: 8px;
  margin-bottom: 10px;
  line-height: 1.5;
  font-size: 13px;
}

.company-bank-info-panel__label {
  flex: 0 0 88px;
  color: var(--el-text-color-secondary);
}

.company-bank-info-panel__value {
  flex: 1;
  min-width: 0;
  color: var(--el-text-color-primary);
  word-break: break-all;
}

.company-bank-info-panel__value--code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.company-bank-info-panel__value--single-line {
  white-space: nowrap;
}
</style>
