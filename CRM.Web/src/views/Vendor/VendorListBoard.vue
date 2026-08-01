<script setup lang="ts">
import PurchaseAnalyticsVendorPanel from '@/components/Analytics/PurchaseAnalyticsVendorPanel.vue'
import type { VendorListAnalyticsQuery } from '@/api/vendorListAnalytics'

defineProps<{
  filters: VendorListAnalyticsQuery
}>()
</script>

<template>
  <div class="vendor-list-board">
    <PurchaseAnalyticsVendorPanel mode="list" :filters="filters" :active="true" />
  </div>
</template>

<style scoped lang="scss">
.vendor-list-board {
  padding: 8px 0 24px;
}
</style>
