<template>
  <div v-if="bank" class="vendor-bank-info-panel">
    <button
      type="button"
      class="vendor-bank-info-panel__header"
      :aria-expanded="expanded"
      @click="expanded = !expanded"
    >
      <span class="vendor-bank-info-panel__title">{{ t('purchaseOrderItemList.paymentDialog.bankDetailTitle') }}</span>
      <span class="vendor-bank-info-panel__toggle">
        {{ expanded ? t('purchaseOrderItemList.paymentDialog.bankDetailCollapse') : t('purchaseOrderItemList.paymentDialog.bankDetailExpand') }}
        <el-icon class="vendor-bank-info-panel__icon" :class="{ 'is-collapsed': !expanded }">
          <ArrowDown />
        </el-icon>
      </span>
    </button>

    <div v-show="expanded" class="vendor-bank-info-panel__body">
      <el-row :gutter="16">
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.accountName') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, bank.accountName) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.bankName') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, bankName) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.branch') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, bank.bankBranch) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.accountNo') }}</span>
            <span class="vendor-bank-info-panel__value vendor-bank-info-panel__value--code">{{ display(masked, bank.bankAccount) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.currency') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, currencyText) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.country') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, bank.country) }}</span>
          </div>
        </el-col>
        <el-col :span="24">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.bankAddress') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, bank.bankAddress) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.swift') }}</span>
            <span class="vendor-bank-info-panel__value vendor-bank-info-panel__value--code">{{ display(masked, bank.swift) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.iban') }}</span>
            <span class="vendor-bank-info-panel__value vendor-bank-info-panel__value--code">{{ display(masked, bank.iban) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.bankCode') }}</span>
            <span class="vendor-bank-info-panel__value vendor-bank-info-panel__value--code">{{ display(masked, bank.bankCode) }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.bankType') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, accountTypeText) }}</span>
          </div>
        </el-col>
        <el-col v-if="bank.remark?.trim()" :span="24">
          <div class="vendor-bank-info-panel__item">
            <span class="vendor-bank-info-panel__label">{{ t('vendorDetail.banks.remark') }}</span>
            <span class="vendor-bank-info-panel__value">{{ display(masked, bank.remark) }}</span>
          </div>
        </el-col>
      </el-row>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ArrowDown } from '@element-plus/icons-vue'
import type { VendorBankInfo } from '@/types/vendor'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { useFinancePaymentBankOptions } from '@/composables/useFinancePaymentBankOptions'
import { vendorBankLabel } from '@/utils/vendorFinancePaymentBank'

const props = withDefaults(
  defineProps<{
    bank?: VendorBankInfo | null
    masked?: boolean
    /** 切换银行时是否自动展开，默认 true */
    expandOnBankChange?: boolean
  }>(),
  {
    bank: null,
    masked: false,
    expandOnBankChange: true
  }
)

const { t } = useI18n()
const { paymentBankOptions, loadPaymentBankOptions } = useFinancePaymentBankOptions()
const expanded = ref(true)

onMounted(() => {
  void loadPaymentBankOptions()
})

watch(
  () => props.bank?.id,
  () => {
    if (props.expandOnBankChange) expanded.value = true
  }
)

const bankName = computed(() => vendorBankLabel(props.bank, paymentBankOptions.value))

const currencyText = computed(() => {
  const code = props.bank?.currency
  if (code == null) return ''
  return CURRENCY_CODE_TO_TEXT[code] ?? String(code)
})

const accountTypeText = computed(() => {
  if (props.bank?.accountType === 'foreign') return t('vendorDetail.banks.bankTypeForeign')
  if (props.bank?.accountType === 'rmb') return t('vendorDetail.banks.bankTypeRmb')
  return ''
})

function display(masked: boolean, value?: string | null) {
  if (masked) return '—'
  const text = value?.trim()
  return text || '—'
}
</script>

<style scoped>
.vendor-bank-info-panel {
  margin-bottom: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  background: var(--el-fill-color-blank);
  overflow: hidden;
}

.vendor-bank-info-panel__header {
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

.vendor-bank-info-panel__header:hover {
  background: var(--el-fill-color);
}

.vendor-bank-info-panel__title {
  font-size: 13px;
  font-weight: 600;
}

.vendor-bank-info-panel__toggle {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.vendor-bank-info-panel__icon {
  transition: transform 0.2s ease;
}

.vendor-bank-info-panel__icon.is-collapsed {
  transform: rotate(-90deg);
}

.vendor-bank-info-panel__body {
  padding: 12px 14px 2px;
}

.vendor-bank-info-panel__item {
  display: flex;
  gap: 8px;
  margin-bottom: 10px;
  line-height: 1.5;
  font-size: 13px;
}

.vendor-bank-info-panel__label {
  flex: 0 0 88px;
  color: var(--el-text-color-secondary);
}

.vendor-bank-info-panel__value {
  flex: 1;
  min-width: 0;
  color: var(--el-text-color-primary);
  word-break: break-all;
}

.vendor-bank-info-panel__value--code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}
</style>
