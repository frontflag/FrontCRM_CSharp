<template>
  <CommissionRateList :role-type="COMMISSION_ROLE_PURCHASE" />
</template>

<script setup lang="ts">
import { COMMISSION_ROLE_PURCHASE } from '@/api/commissionRates'
import CommissionRateList from './CommissionRateList.vue'
</script>
