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
  splitListMoneyParts
} from '@/utils/moneyFormat'

const props = withDefaults(
  defineProps<{ amount?: number | null; currency: number; masked?: boolean; showCurrency?: boolean }>(),
  { masked: false, showCurrency: true }
)

const parts = computed(() => splitListMoneyParts(Number(props.amount)))
</script>
