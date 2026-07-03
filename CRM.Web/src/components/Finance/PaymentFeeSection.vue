<template>
  <div class="payment-fee-section">
    <div v-if="showTitle" class="payment-fee-section__title">{{ t('financePaymentList.editRequest.feeSection') }}</div>
    <el-row :gutter="12">
      <el-col :span="8">
        <el-form-item :label="t('financePaymentList.editRequest.intermediateBankFee')">
          <SettlementCurrencyAmountInput
            v-if="!readonly"
            :model-value="modelValue.intermediateBankFee"
            :currency="currency"
            :min="0"
            :precision="2"
            :disabled="disabled"
            @update:model-value="patchAmount('intermediateBankFee', $event)"
            @update:currency="emit('update:currency', $event)"
          />
          <span v-else class="payment-fee-section__readonly">{{ formatFee(modelValue.intermediateBankFee) }}</span>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item :label="t('financePaymentList.editRequest.bankCharge')">
          <SettlementCurrencyAmountInput
            v-if="!readonly"
            :model-value="modelValue.bankCharge"
            :currency="currency"
            :min="0"
            :precision="2"
            :disabled="disabled"
            @update:model-value="patchAmount('bankCharge', $event)"
            @update:currency="emit('update:currency', $event)"
          />
          <span v-else class="payment-fee-section__readonly">{{ formatFee(modelValue.bankCharge) }}</span>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item :label="t('financePaymentList.editRequest.freight')">
          <SettlementCurrencyAmountInput
            v-if="!readonly"
            :model-value="modelValue.freight"
            :currency="currency"
            :min="0"
            :precision="2"
            :disabled="disabled"
            @update:model-value="patchAmount('freight', $event)"
            @update:currency="emit('update:currency', $event)"
          />
          <span v-else class="payment-fee-section__readonly">{{ formatFee(modelValue.freight) }}</span>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item :label="t('financePaymentList.editRequest.miscFee')">
          <SettlementCurrencyAmountInput
            v-if="!readonly"
            :model-value="modelValue.miscFee"
            :currency="currency"
            :min="0"
            :precision="2"
            :disabled="disabled"
            @update:model-value="patchAmount('miscFee', $event)"
            @update:currency="emit('update:currency', $event)"
          />
          <span v-else class="payment-fee-section__readonly">{{ formatFee(modelValue.miscFee) }}</span>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item :label="t('financePaymentList.editRequest.rounding')">
          <SettlementCurrencyAmountInput
            v-if="!readonly"
            :model-value="modelValue.rounding"
            :currency="currency"
            :min="0"
            :precision="2"
            :disabled="disabled"
            @update:model-value="patchAmount('rounding', $event)"
            @update:currency="emit('update:currency', $event)"
          />
          <span v-else class="payment-fee-section__readonly">{{ formatFee(modelValue.rounding) }}</span>
        </el-form-item>
      </el-col>
      <el-col :span="8">
        <el-form-item :label="t('financePaymentList.editRequest.feePayer')">
          <el-radio-group
            v-if="!readonly"
            :model-value="modelValue.intermediateBankFeePayer"
            :disabled="disabled"
            @update:model-value="patchPayer($event)"
          >
            <el-radio label="我方">{{ t('financePaymentList.editRequest.payerUs') }}</el-radio>
            <el-radio label="供应商">{{ t('financePaymentList.editRequest.payerVendor') }}</el-radio>
          </el-radio-group>
          <span v-else class="payment-fee-section__readonly">{{ payerDisplay }}</span>
        </el-form-item>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { formatCurrencyTotal } from '@/utils/moneyFormat'

export interface PaymentFeeForm {
  intermediateBankFee: number
  bankCharge: number
  freight: number
  miscFee: number
  rounding: number
  intermediateBankFeePayer: string
}

const props = withDefaults(
  defineProps<{
    modelValue: PaymentFeeForm
    currency: number
    readonly?: boolean
    disabled?: boolean
    showTitle?: boolean
  }>(),
  {
    readonly: false,
    disabled: false,
    showTitle: true,
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: PaymentFeeForm]
  'update:currency': [value: number]
}>()

const { t } = useI18n()

const payerDisplay = computed(() => {
  const p = (props.modelValue.intermediateBankFeePayer || '').trim()
  if (p === '我方') return t('financePaymentList.editRequest.payerUs')
  if (p === '供应商') return t('financePaymentList.editRequest.payerVendor')
  return p || '—'
})

function formatFee(amount: number) {
  return formatCurrencyTotal(amount, props.currency)
}

function patchAmount(
  key: 'intermediateBankFee' | 'bankCharge' | 'freight' | 'miscFee' | 'rounding',
  value: number | undefined
) {
  emit('update:modelValue', { ...props.modelValue, [key]: Number(value ?? 0) })
}

function patchPayer(value: string) {
  emit('update:modelValue', { ...props.modelValue, intermediateBankFeePayer: value })
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.payment-fee-section__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-primary;
  margin: 4px 0 10px;
}

.payment-fee-section__readonly {
  font-size: 13px;
  color: $text-primary;
  line-height: 32px;
}
</style>
