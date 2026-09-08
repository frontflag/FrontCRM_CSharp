<template>
  <CommissionRateList :role-type="COMMISSION_ROLE_SALES" />
</template>

<script setup lang="ts">
import { COMMISSION_ROLE_SALES } from '@/api/commissionRates'
import CommissionRateList from './CommissionRateList.vue'
</script>
