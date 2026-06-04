<template>
  <el-select
    :model-value="modelValue"
    :placeholder="placeholder"
    filterable
    :clearable="clearable"
    :disabled="disabled"
    :filter-method="onFilterMethod"
    popper-class="finance-payment-bank-select-popper"
    class="finance-payment-bank-select"
    style="width: 100%"
    @update:model-value="onModelUpdate"
    @visible-change="onVisibleChange"
  >
    <template #header>
      <div class="fpb-select-header">
        <span class="fpb-col fpb-col--cn">{{ t('financeParams.colBankName') }}</span>
        <span class="fpb-col fpb-col--en">{{ t('financeParams.colEBankName') }}</span>
        <span class="fpb-col fpb-col--short">{{ t('financeParams.colShortName') }}</span>
        <span class="fpb-col fpb-col--type">{{ t('financeParams.colCurrencyType') }}</span>
      </div>
    </template>
    <el-option
      v-for="b in visibleOptions"
      :key="b.id"
      :label="selectedDisplayLabel(b)"
      :value="b.id"
    >
      <div class="fpb-select-row">
        <span class="fpb-col fpb-col--cn" :title="b.bankName">{{ b.bankName }}</span>
        <span class="fpb-col fpb-col--en" :title="b.eBankName || ''">{{ b.eBankName || '—' }}</span>
        <span class="fpb-col fpb-col--short" :title="b.shortName || ''">{{ b.shortName || '—' }}</span>
        <span class="fpb-col fpb-col--type">{{ currencyTypeLabel(b.currencyType) }}</span>
      </div>
    </el-option>
  </el-select>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { FinancePaymentBankDto } from '@/api/financePaymentBank'
import { useFinancePaymentBankOptions } from '@/composables/useFinancePaymentBankOptions'
import {
  financePaymentBankCurrencyTypeLabel,
  financePaymentBankSelectedDisplayLabel
} from '@/constants/financePaymentBankCurrencyType'
import { filterFinancePaymentBankOptions } from '@/utils/financePaymentBankFilter'

const props = withDefaults(
  defineProps<{
    modelValue?: string
    placeholder?: string
    clearable?: boolean
    disabled?: boolean
    /** 外部已加载的选项；不传则组件自行 listOptions */
    options?: FinancePaymentBankDto[]
  }>(),
  {
    modelValue: '',
    placeholder: '',
    clearable: false,
    disabled: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { t } = useI18n()
const { paymentBankOptions, loadPaymentBankOptions } = useFinancePaymentBankOptions()
const filterQuery = ref('')

const sourceOptions = computed(() =>
  props.options?.length ? props.options : paymentBankOptions.value
)

const visibleOptions = computed(() =>
  filterFinancePaymentBankOptions(sourceOptions.value, filterQuery.value)
)

function currencyTypeLabel(value: number): string {
  return financePaymentBankCurrencyTypeLabel(value, t)
}

function selectedDisplayLabel(bank: FinancePaymentBankDto): string {
  return financePaymentBankSelectedDisplayLabel(bank)
}

function onFilterMethod(query: string) {
  filterQuery.value = query
}

function onModelUpdate(v: string | undefined) {
  emit('update:modelValue', v ? String(v) : '')
}

function onVisibleChange(open: boolean) {
  if (!open) filterQuery.value = ''
}

onMounted(() => {
  if (!props.options?.length) void loadPaymentBankOptions()
})

defineExpose({ loadPaymentBankOptions, paymentBankOptions })
</script>

<style lang="scss">
.finance-payment-bank-select-popper {
  min-width: 640px !important;

  .el-select-dropdown__header {
    padding: 6px 12px;
    border-bottom: 1px solid var(--el-border-color-lighter);
  }

  .el-select-dropdown__item {
    height: auto;
    min-height: 34px;
    line-height: 1.4;
    padding: 6px 12px;
  }
}

.fpb-select-header,
.fpb-select-row {
  display: grid;
  grid-template-columns: 1.1fr 1.2fr 0.75fr 0.85fr;
  gap: 8px;
  align-items: center;
  width: 100%;
  font-size: 13px;
}

.fpb-select-header {
  font-weight: 600;
  color: var(--el-text-color-secondary);
}

.fpb-select-row {
  color: var(--el-text-color-primary);
}

.fpb-col {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.fpb-col--type {
  text-align: center;
}
</style>
