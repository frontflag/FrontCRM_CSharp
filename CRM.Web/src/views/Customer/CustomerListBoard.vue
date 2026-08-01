<script setup lang="ts">
import SalesAnalyticsCustomerPanel from '@/components/Analytics/SalesAnalyticsCustomerPanel.vue'
import type { CustomerListAnalyticsQuery } from '@/api/customerListAnalytics'

defineProps<{
  filters: CustomerListAnalyticsQuery
}>()
</script>

<template>
  <div class="customer-list-board">
    <SalesAnalyticsCustomerPanel mode="list" :filters="filters" :active="true" />
  </div>
</template>

<style scoped lang="scss">
.customer-list-board {
  padding: 8px 0 24px;
}
</style>
