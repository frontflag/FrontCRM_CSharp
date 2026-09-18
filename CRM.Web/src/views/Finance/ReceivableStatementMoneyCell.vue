<template>
  <span v-if="masked || !listTotalAmountHasValue(amount)" class="dock-tier-empty">—</span>
  <div v-else class="dock-tier-price-line">
    <span class="dock-tier-amt">
      <span class="dock-tier-amt-int">{{ parts.intPart }}</span>
      <span class="dock-tier-amt-frac">{{ parts.fracPart }}</span>
    </span>
    <span v-if="showCurrency" class="dock-tier-ccy-gap">&nbsp;</span>
    <span v-if="showCurrency" :class="['dock-tier-ccy', listAmountCurrencyDockClass(currency)]">
      {{ listAmountCurrencyIso(currency) }}
    </span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts,
  splitUnitPriceDockParts
} from '@/utils/moneyFormat'

const props = withDefaults(
  defineProps<{
    amount?: number | null
    currency: number
    masked?: boolean
    showCurrency?: boolean
    /** 单价用 2–6 位小数；总额仍为两位。 */
    unitPrice?: boolean
  }>(),
  { masked: false, showCurrency: true, unitPrice: false }
)

const parts = computed(() =>
  props.unitPrice ? splitUnitPriceDockParts(Number(props.amount)) : splitListMoneyParts(Number(props.amount))
)
</script>
