<template>
  <div v-if="context" class="customer-intel-crm-bar">
    <div class="customer-intel-crm-bar__row">
      <span class="customer-intel-crm-bar__company-name">{{ context.companyName }}</span>
    </div>
    <div class="customer-intel-crm-bar__row customer-intel-crm-bar__meta">
      <span v-if="context.creditCode">信用代码 {{ context.creditCode }}</span>
      <span v-if="context.salesPersonName">{{ context.salesPersonName }}</span>
      <el-tag v-if="context.blackList" type="danger" size="small" effect="dark">黑名单</el-tag>
      <el-tag v-if="context.disenableStatus" type="warning" size="small" effect="dark">已冻结</el-tag>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { CustomerIntelCrmContext } from '@/stores/customerIntelLookup'

defineProps<{
  context: CustomerIntelCrmContext | null
}>()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-intel-crm-bar {
  margin-bottom: 12px;
  padding: 10px 12px;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 8px;
  font-size: 12px;
}

.customer-intel-crm-bar__row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.customer-intel-crm-bar__company-name {
  font-size: 14px;
  font-weight: 600;
  color: $color-amber;
}

.customer-intel-crm-bar__meta {
  margin-top: 6px;
  color: $text-secondary;
  gap: 10px;
}
</style>
