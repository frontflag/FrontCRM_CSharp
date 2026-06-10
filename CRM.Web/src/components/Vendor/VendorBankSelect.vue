<template>
  <el-select
    :model-value="modelValue"
    :placeholder="placeholder"
    filterable
    :clearable="clearable"
    :disabled="disabled || !options.length"
    style="width: 100%"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-option
      v-for="bank in options"
      :key="bank.id"
      :label="formatVendorBankOptionLabel(bank, paymentBankOptions, masked)"
      :value="bank.id"
    />
  </el-select>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import type { VendorBankInfo } from '@/types/vendor'
import { useFinancePaymentBankOptions } from '@/composables/useFinancePaymentBankOptions'
import { formatVendorBankOptionLabel } from '@/utils/vendorFinancePaymentBank'

withDefaults(
  defineProps<{
    modelValue?: string
    options: VendorBankInfo[]
    placeholder?: string
    clearable?: boolean
    disabled?: boolean
    masked?: boolean
  }>(),
  {
    modelValue: '',
    placeholder: '',
    clearable: false,
    disabled: false,
    masked: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { paymentBankOptions, loadPaymentBankOptions } = useFinancePaymentBankOptions()

onMounted(() => {
  void loadPaymentBankOptions()
})
</script>
