<template>
  <div class="settlement-currency-amount">
    <el-input-number
      :model-value="modelValue"
      :min="min"
      :max="max"
      :precision="precision"
      :controls="controls"
      :size="size"
      class="settlement-currency-amount__num"
      @update:model-value="emit('update:modelValue', $event)"
    />
    <el-select
      :model-value="currency"
      :disabled="currencyDisabled"
      :size="size"
      class="settlement-currency-amount__ccy"
      @update:model-value="emit('update:currency', $event as number)"
    >
      <el-option
        v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
        :key="opt.value"
        :label="opt.label"
        :value="opt.value"
      />
    </el-select>
  </div>
</template>

<script setup lang="ts">
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'

withDefaults(
  defineProps<{
    /** 金额 */
    modelValue: number | null | undefined
    /** 结算币别编码（与 SETTLEMENT_CURRENCY_OPTIONS / 后端 short 一致） */
    currency: number
    min?: number
    max?: number
    precision?: number
    /** 与新建销售订单「销售单价」一致时设为 false */
    controls?: boolean
    currencyDisabled?: boolean
    /** 表格内等场景使用 small；不传则沿用 Element Plus 默认 */
    size?: 'large' | 'default' | 'small'
  }>(),
  {
    precision: 2,
    controls: false,
    currencyDisabled: false
  }
)

const emit = defineEmits<{
  'update:modelValue': [v: number | undefined]
  'update:currency': [v: number]
}>()
</script>

<style scoped lang="scss">
/* 对齐 SalesOrderCreate.vue `.price-currency-row` */
.settlement-currency-amount {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  min-width: 0;
}

.settlement-currency-amount__num {
  flex: 1;
  min-width: 0;
}

.settlement-currency-amount__num :deep(.el-input__wrapper) {
  width: 100%;
}

.settlement-currency-amount__ccy {
  width: 88px;
  flex-shrink: 0;
}
</style>
