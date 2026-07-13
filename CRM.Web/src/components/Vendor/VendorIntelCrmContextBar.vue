<template>
  <div v-if="context" class="vendor-intel-crm-bar">
    <div class="vendor-intel-crm-bar__row">
      <span class="vendor-intel-crm-bar__company-name">{{ context.companyName }}</span>
    </div>
    <div class="vendor-intel-crm-bar__row vendor-intel-crm-bar__meta">
      <span v-if="context.creditCode">信用代码 {{ context.creditCode }}</span>
      <span v-if="context.purchaserName">{{ context.purchaserName }}</span>
      <el-tag v-if="context.blackList" type="danger" size="small" effect="dark">黑名单</el-tag>
      <el-tag v-if="context.isDisenable" type="warning" size="small" effect="dark">已冻结</el-tag>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { VendorIntelCrmContext } from '@/stores/vendorIntelLookup'

defineProps<{
  context: VendorIntelCrmContext | null
}>()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.vendor-intel-crm-bar {
  margin-bottom: 12px;
  padding: 10px 12px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 8px;
  font-size: 12px;
}

.vendor-intel-crm-bar__row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.vendor-intel-crm-bar__company-name {
  font-size: 14px;
  font-weight: 600;
  color: $color-amber;
}

.vendor-intel-crm-bar__meta {
  margin-top: 6px;
  color: $text-secondary;
  gap: 10px;
}
</style>
